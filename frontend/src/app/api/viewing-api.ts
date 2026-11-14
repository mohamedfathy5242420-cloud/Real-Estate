import { Injectable, inject } from '@angular/core';
import { ApiTransport } from './api-transport';
import { crmId, crmProjection } from './crm-api';
import { ApiFailure } from '../models/api-failure';
import { CrmPage } from '../models/crm';
import {
  OwnViewing,
  StaffViewing,
  OwnViewingEvent,
  StaffViewingEvent,
  ViewingOperation,
  ViewingReceipt,
  VIEWING_STATUSES,
} from '../models/viewing';
const ownKeys = [
  'id',
  'unitId',
  'projectId',
  'status',
  'currentProposedStartUtc',
  'currentProposedEndUtc',
  'timeZoneId',
  'requestedAtUtc',
  'pendingCycleStartedAtUtc',
  'pendingExpiresAtUtc',
  'version',
  'confirmedStartUtc',
  'confirmedEndUtc',
] as const;
const staffKeys = [
  ...ownKeys,
  'customerId',
  'leadId',
  'requestedByAccountId',
  'confirmedEmployeeId',
  'confirmedAtUtc',
  'confirmedByAccountId',
  'coordinatedAtUtc',
  'coordinationSource',
  'coordinationReference',
  'offeredAtUtc',
  'offeredByAccountId',
  'offeredEmployeeId',
  'cancelledAtUtc',
  'cancelledByAccountId',
  'cancellationKind',
  'cancellationReason',
  'cancelledFromStatus',
  'expiredAtUtc',
  'expiredFromStatus',
  'outcomeOccurredAtUtc',
  'outcomeRecordedAtUtc',
  'outcomeRecordedByAccountId',
  'outcomeNoteId',
] as const;
const ownEventKeys = [
  'id',
  'viewingId',
  'viewingVersion',
  'kind',
  'atUtc',
  'proposedStartUtc',
  'proposedEndUtc',
  'confirmedStartUtc',
  'confirmedEndUtc',
] as const;
const staffEventKeys = [
  ...ownEventKeys,
  'customerId',
  'actorAccountId',
  'confirmedEmployeeId',
  'coordinatedAtUtc',
  'coordinationSource',
  'coordinationReference',
  'offeredEmployeeId',
  'previousProposedStartUtc',
  'previousProposedEndUtc',
  'reason',
  'previousStatus',
  'previousConfirmedStartUtc',
  'previousConfirmedEndUtc',
  'previousConfirmedEmployeeId',
  'cancellationKind',
  'actorKind',
  'effectiveCutoffUtc',
  'outcomeOccurredAtUtc',
  'outcomeNoteId',
] as const;
function project(raw: unknown, staff: boolean): OwnViewing | StaffViewing {
  const row = crmProjection<OwnViewing | StaffViewing>(raw, staff ? staffKeys : ownKeys);
  if (!VIEWING_STATUSES.includes(row.status)) throw new ApiFailure('unknown');
  crmId(row.unitId);
  crmId(row.projectId);
  if (staff) crmId((row as StaffViewing).customerId);
  return row;
}
function page<T>(raw: unknown, mapper: (raw: unknown) => T): CrmPage<T> {
  const p = raw as CrmPage<unknown>;
  if (
    !p ||
    !Array.isArray(p.items) ||
    !Number.isSafeInteger(p.page) ||
    p.page < 1 ||
    !Number.isSafeInteger(p.pageSize) ||
    p.pageSize < 1 ||
    !Number.isSafeInteger(p.total) ||
    p.total < 0
  )
    throw new ApiFailure('unknown');
  return Object.freeze({
    items: Object.freeze(p.items.map(mapper)),
    page: p.page,
    pageSize: p.pageSize,
    total: p.total,
  });
}
function paging(n: number) {
  if (!Number.isInteger(n) || n < 1 || n > 1000000) throw new ApiFailure('invalid');
  return new URLSearchParams({ page: String(n), pageSize: '20' });
}
function root(staff: boolean, customerId?: string) {
  return staff ? `/api/crm/customers/${crmId(customerId ?? '')}/viewings` : '/api/viewings';
}
@Injectable({ providedIn: 'root' })
export class ViewingApi {
  private readonly http = inject(ApiTransport);
  async list(staff: boolean, n = 1, status = '') {
    const q = paging(n);
    if (staff && status) q.set('status', status);
    return page(
      await this.http.get((staff ? '/api/crm/viewings' : '/api/viewings/mine') + '?' + q),
      (r) => project(r, staff),
    );
  }
  async detail(staff: boolean, id: string, customerId?: string) {
    return project(await this.http.get(root(staff, customerId) + '/' + crmId(id)), staff);
  }
  async history(staff: boolean, id: string, customerId?: string, n = 1) {
    return page(
      await this.http.get(root(staff, customerId) + '/' + crmId(id) + '/history?' + paging(n)),
      (r) =>
        crmProjection<OwnViewingEvent | StaffViewingEvent>(
          r,
          staff ? staffEventKeys : ownEventKeys,
        ),
    );
  }
  async execute(op: ViewingOperation): Promise<ViewingReceipt> {
    if (
      (op.action === 'submit' && op.staff) ||
      (['confirm', 'alternatives', 'outcome'].includes(op.action) && !op.staff) ||
      (['accept-alternative', 'change-proposal'].includes(op.action) && op.staff)
    )
      throw new ApiFailure('invalid');
    const path =
      op.action === 'submit'
        ? '/api/viewings'
        : root(op.staff, op.customerId) + '/' + crmId(op.id ?? '') + '/' + op.action;
    const raw = await this.http.post(path, op.body, true, op.key);
    try {
      const receipt = crmProjection<ViewingReceipt>(raw, ['id', 'version', 'isReplay']);
      if (typeof receipt.isReplay !== 'boolean') throw new ApiFailure('unknown');
      return receipt;
    } catch {
      throw new ApiFailure('unknown', [], true);
    }
  }
}
