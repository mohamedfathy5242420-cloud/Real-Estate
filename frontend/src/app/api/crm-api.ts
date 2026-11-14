import { Injectable, inject } from '@angular/core';
import { ApiTransport } from './api-transport';
import { ApiFailure } from '../models/api-failure';
import {
  Assignment,
  AssignmentCandidate,
  CrmCommand,
  CrmOperation,
  CrmCustomer,
  CrmPage,
  CustomerProfile,
  CustomerValues,
  EmployeeProfile,
  EmployeeRevision,
  Lead,
  LeadEvent,
  LinkCandidate,
  LinkEvent,
  LinkReview,
  MutationReply,
  NeedsChange,
  OwnLink,
  SalesNote,
} from '../models/crm';
const GUID = /^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/i;
export function crmId(value: string): string {
  if (!GUID.test(value) || value === '00000000-0000-0000-0000-000000000000')
    throw new ApiFailure('invalid');
  return value;
}
const profileKeys = [
  'id',
  'fullName',
  'phone',
  'email',
  'preferredContactMethod',
  'requirements',
  'version',
  'createdAtUtc',
  'updatedAtUtc',
] as const;
const employeeKeys = [
  'id',
  'accountId',
  'employeeNumber',
  'displayName',
  'isActive',
  'version',
  'createdAtUtc',
  'createdByAccountId',
  'updatedAtUtc',
  'updatedByAccountId',
] as const;
const ownLinkKeys = ['id', 'claim', 'status', 'createdAtUtc', 'decidedAtUtc', 'version'] as const;
const leadKeys = [
  'id',
  'customerId',
  'projectId',
  'marketingAgreementId',
  'agreementVersionAtOpening',
  'agreementStartsOn',
  'agreementEndsOn',
  'protectionPeriodDays',
  'protectionEndsOn',
  'openedOnLocalDate',
  'openingTimeZoneId',
  'openedAtUtc',
  'openedByAccountId',
  'source',
  'status',
  'version',
  'closedAtUtc',
  'closedByAccountId',
  'closeReason',
] as const;
/** Every reader has its own declared projection; Customer never receives staff models. */
export function crmProjection<T>(value: unknown, keys: readonly string[]): T {
  if (!value || typeof value !== 'object') throw new ApiFailure('unknown');
  const raw = value as Record<string, unknown>;
  const selected = Object.fromEntries(keys.map((key) => [key, raw[key]]));
  for (const key of keys.filter((key) => /version/i.test(key))) {
    const version = selected[key];
    if (typeof version !== 'string' || !/^[1-9]\d*$/.test(version)) throw new ApiFailure('unknown');
  }
  if ('id' in selected) crmId(String(selected['id']));
  return Object.freeze(selected) as T;
}
function mutation<T>(project: () => T): T {
  try {
    return project();
  } catch {
    throw new ApiFailure('unknown', [], true);
  }
}
function page<T>(value: unknown, project: (row: unknown) => T): CrmPage<T> {
  const raw = value as { items: unknown[]; page: number; pageSize: number; total: number };
  if (
    !raw ||
    !Array.isArray(raw.items) ||
    !Number.isInteger(raw.page) ||
    !Number.isInteger(raw.total)
  )
    throw new ApiFailure('unknown');
  return Object.freeze({
    items: Object.freeze(raw.items.map(project)),
    page: raw.page,
    pageSize: raw.pageSize,
    total: raw.total,
  });
}
const ownProfile = (raw: unknown) => crmProjection<CustomerProfile>(raw, profileKeys);
const customer = (raw: unknown) =>
  crmProjection<CrmCustomer>(raw, [...profileKeys, 'assignedEmployeeId']);
const employee = (raw: unknown) => crmProjection<EmployeeProfile>(raw, employeeKeys);
const ownLink = (raw: unknown) => crmProjection<OwnLink>(raw, ownLinkKeys);
const candidate = (raw: unknown) =>
  crmProjection<AssignmentCandidate>(raw, ['id', 'employeeNumber', 'displayName']);
function review(raw: unknown): LinkReview {
  const record = crmProjection<LinkReview>(raw, [
    ...ownLinkKeys,
    'accountId',
    'decisionReason',
    'decidedByAccountId',
    'customerId',
    'history',
  ]);
  if (!Array.isArray(record.history)) throw new ApiFailure('unknown');
  return Object.freeze({
    ...record,
    history: Object.freeze(
      record.history.map((event) =>
        crmProjection<LinkEvent>(event, [
          'id',
          'kind',
          'actorAccountId',
          'atUtc',
          'reason',
          'customerId',
        ]),
      ),
    ),
  });
}
function query(pageNumber = 1, other: Record<string, string> = {}): string {
  if (!Number.isInteger(pageNumber) || pageNumber < 1 || pageNumber > 1000000)
    throw new ApiFailure('invalid');
  return (
    '?' + new URLSearchParams({ page: String(pageNumber), pageSize: '20', ...other }).toString()
  );
}
function contact(values: CustomerValues) {
  return {
    fullName: values.fullName.trim(),
    phone: values.phone.trim(),
    email: values.email.trim() || null,
    preferredContactMethod: values.preferredContactMethod.trim() || null,
    requirements: values.requirements.trim() || null,
  };
}
@Injectable({ providedIn: 'root' })
export class CrmApi {
  private readonly transport = inject(ApiTransport);
  async own(): Promise<CustomerProfile | null> {
    try {
      return ownProfile(await this.transport.get('/api/customers/me'));
    } catch (error) {
      if (error instanceof ApiFailure && error.kind === 'not-found') return null;
      throw error;
    }
  }
  async saveOwn(current: CustomerProfile | null, values: CustomerValues): Promise<CustomerProfile> {
    const raw = current
      ? await this.transport.put('/api/customers/' + crmId(current.id), {
          ...contact(values),
          version: current.version,
        })
      : await this.transport.post('/api/customers', contact(values));
    return mutation(() => ownProfile(raw));
  }
  async ownLinks(pageNumber = 1) {
    return page(
      await this.transport.get('/api/customer-link-requests/mine' + query(pageNumber)),
      ownLink,
    );
  }
  async submitLink(claim: string) {
    const raw = await this.transport.post('/api/customer-link-requests', { claim: claim.trim() });
    return mutation(() => ownLink(raw));
  }
  async employees(pageNumber = 1, isActive?: boolean) {
    return page(
      await this.transport.get(
        '/api/employees' +
          query(pageNumber, isActive === undefined ? {} : { isActive: String(isActive) }),
      ),
      employee,
    );
  }
  async employee(id: string) {
    return employee(await this.transport.get('/api/employees/' + crmId(id)));
  }
  async employeeHistory(id: string, pageNumber = 1) {
    return page(
      await this.transport.get(`/api/employees/${crmId(id)}/history` + query(pageNumber)),
      (raw) =>
        crmProjection<EmployeeRevision>(raw, [
          'id',
          'version',
          'displayName',
          'isActive',
          'atUtc',
          'actorAccountId',
        ]),
    );
  }
  async saveEmployee(
    current: EmployeeProfile | null,
    values: { accountId: string; employeeNumber: string; displayName: string; isActive: boolean },
  ) {
    const raw = current
      ? await this.transport.put('/api/employees/' + crmId(current.id), {
          displayName: values.displayName.trim(),
          isActive: values.isActive,
          version: current.version,
        })
      : await this.transport.post('/api/employees', {
          accountId: crmId(values.accountId),
          employeeNumber: values.employeeNumber.trim(),
          displayName: values.displayName.trim(),
        });
    return mutation(() => employee(raw));
  }
  async customers(pageNumber = 1, unassigned = false) {
    return page(
      await this.transport.get(
        '/api/crm/customers' + (unassigned ? '/unassigned' : '') + query(pageNumber),
      ),
      customer,
    );
  }
  async customer(id: string) {
    return customer(await this.transport.get('/api/crm/customers/' + crmId(id)));
  }
  async candidates(pageNumber = 1) {
    return page(
      await this.transport.get('/api/crm/assignment-candidates' + query(pageNumber)),
      candidate,
    );
  }
  async assignments(id: string, pageNumber = 1) {
    return page(
      await this.transport.get(`/api/crm/customers/${crmId(id)}/assignments` + query(pageNumber)),
      (raw) =>
        crmProjection<Assignment>(raw, [
          'id',
          'customerId',
          'previousEmployeeId',
          'newEmployeeId',
          'actorAccountId',
          'changedAtUtc',
          'reason',
          'customerVersion',
        ]),
    );
  }
  async reviews(pageNumber = 1) {
    return page(await this.transport.get('/api/customer-link-reviews' + query(pageNumber)), review);
  }
  async review(id: string) {
    return review(await this.transport.get('/api/customer-link-reviews/' + crmId(id)));
  }
  async linkCandidates(pageNumber = 1) {
    return page(
      await this.transport.get('/api/customer-link-reviews/candidates' + query(pageNumber)),
      (raw) => crmProjection<LinkCandidate>(raw, ['id', 'fullName', 'phone', 'email', 'version']),
    );
  }
  async decideLink(current: LinkReview, approve: boolean, reason: string, target?: LinkCandidate) {
    const raw = await this.transport.post(
      `/api/customer-link-reviews/${crmId(current.id)}/${approve ? 'approve' : 'reject'}`,
      {
        version: current.version,
        reason: reason.trim(),
        ...(approve && target ? { customerId: target.id, customerVersion: target.version } : {}),
      },
    );
    return mutation(() => review(raw));
  }
  async followUps(pageNumber = 1, filters: Record<string, string> = {}) {
    return page(
      await this.transport.get('/api/crm/follow-ups' + query(pageNumber, filters)),
      customer,
    );
  }
  async notes(id: string, pageNumber = 1, leadId?: string) {
    return page(
      await this.transport.get(
        `/api/crm/customers/${crmId(id)}/notes` +
          query(pageNumber, leadId ? { leadId: crmId(leadId) } : {}),
      ),
      (raw) =>
        crmProjection<SalesNote>(raw, [
          'id',
          'customerId',
          'leadId',
          'authorAccountId',
          'authorEmployeeId',
          'body',
          'createdAtUtc',
          'correctsNoteId',
        ]),
    );
  }
  async needsHistory(id: string, pageNumber = 1) {
    return page(
      await this.transport.get(`/api/crm/customers/${crmId(id)}/needs-history` + query(pageNumber)),
      (raw) =>
        crmProjection<NeedsChange>(raw, [
          'id',
          'customerId',
          'previousRequirements',
          'requirements',
          'customerVersion',
          'actorAccountId',
          'atUtc',
        ]),
    );
  }
  async leads(id: string, pageNumber = 1, status?: string) {
    return page(
      await this.transport.get(
        `/api/crm/customers/${crmId(id)}/leads` + query(pageNumber, status ? { status } : {}),
      ),
      (raw) => crmProjection<Lead>(raw, leadKeys),
    );
  }
  async lead(id: string, leadId: string) {
    return crmProjection<Lead>(
      await this.transport.get(`/api/crm/customers/${crmId(id)}/leads/${crmId(leadId)}`),
      leadKeys,
    );
  }
  async leadHistory(id: string, leadId: string, pageNumber = 1) {
    return page(
      await this.transport.get(
        `/api/crm/customers/${crmId(id)}/leads/${crmId(leadId)}/history` + query(pageNumber),
      ),
      (raw) =>
        crmProjection<LeadEvent>(raw, [
          'id',
          'leadId',
          'customerId',
          'leadVersion',
          'eventType',
          'actorAccountId',
          'atUtc',
          'reason',
        ]),
    );
  }
  /** Command payload/key originate in an in-memory reviewed operation, never an automatic retry. */
  async execute(operation: CrmOperation): Promise<MutationReply> {
    if (operation.action === 'intake')
      return this.command('/api/crm/customers', 'POST', operation.command);
    const customer = '/api/crm/customers/' + crmId(operation.customerId ?? '');
    const suffix = {
      assign: '/assign',
      transfer: '/transfer',
      needs: '/requirements',
      addNote: '/notes',
      correctNote: `/notes/${operation.action === 'correctNote' ? crmId(operation.resourceId ?? '') : ''}/corrections`,
      openLead: '/leads',
      closeLead: `/leads/${operation.action === 'closeLead' ? crmId(operation.resourceId ?? '') : ''}/close`,
    }[operation.action];
    return this.command(
      customer + suffix,
      operation.action === 'needs' ? 'PUT' : 'POST',
      operation.command,
    );
  }
  private async command(
    path: string,
    method: 'POST' | 'PUT',
    command: CrmCommand,
  ): Promise<MutationReply> {
    const raw = await (method === 'POST'
      ? this.transport.post<Record<string, unknown>>(path, command.body, true, command.key)
      : this.transport.put<Record<string, unknown>>(path, command.body, true, command.key));
    return mutation(() => {
      if (!raw || typeof raw !== 'object') throw new ApiFailure('unknown');
      if (
        typeof raw['isReplay'] !== 'boolean' ||
        !['version', 'customerVersion', 'leadVersion'].some((key) => key in raw)
      )
        throw new ApiFailure('unknown');
      crmId(String(raw['customerId']));
      const keys = [
        'id',
        'customerId',
        'isReplay',
        ...['version', 'customerVersion', 'leadVersion', 'created'].filter((key) => key in raw),
      ];
      return crmProjection<MutationReply>(raw, keys);
    });
  }
  contact(values: CustomerValues): Record<string, unknown> {
    return contact(values);
  }
}
