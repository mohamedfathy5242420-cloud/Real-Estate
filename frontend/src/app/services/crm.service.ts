import { Injectable, inject } from '@angular/core';
import { CrmApi, crmId } from '../api/crm-api';
import { ApiFailure } from '../models/api-failure';
import {
  CrmCommand,
  CrmOperation,
  CrmCustomer,
  CustomerProfile,
  CustomerValues,
  EmployeeProfile,
  Lead,
  LinkCandidate,
  LinkReview,
} from '../models/crm';
import { SessionContext } from './session-context.service';
import { firstValueFrom } from 'rxjs';
import { CatalogReader } from '../api/catalog-reader';
/** Journeys and command snapshots, separate from wire projection and HTTP. */
@Injectable({ providedIn: 'root' })
export class CrmService {
  private readonly api = inject(CrmApi);
  private readonly session = inject(SessionContext);
  private readonly catalog = inject(CatalogReader);
  publicProjects(text: string, page = 1) {
    return firstValueFrom(this.catalog.search({ text, page }));
  }
  own() {
    return this.api.own();
  }
  saveOwn(current: CustomerProfile | null, values: CustomerValues) {
    return this.api.saveOwn(current, values);
  }
  ownLinks(page = 1) {
    return this.api.ownLinks(page);
  }
  submitLink(claim: string) {
    return this.api.submitLink(claim);
  }
  employees(page = 1, isActive?: boolean) {
    return this.api.employees(page, isActive);
  }
  employee(id: string) {
    return this.api.employee(id);
  }
  employeeHistory(id: string, page = 1) {
    return this.api.employeeHistory(id, page);
  }
  saveEmployee(
    current: EmployeeProfile | null,
    values: { accountId: string; employeeNumber: string; displayName: string; isActive: boolean },
  ) {
    return this.api.saveEmployee(current, values);
  }
  customers(page = 1, unassigned = false) {
    return this.api.customers(page, unassigned);
  }
  customer(id: string) {
    return this.api.customer(id);
  }
  candidates(page = 1) {
    return this.api.candidates(page);
  }
  assignments(id: string, page = 1) {
    return this.api.assignments(id, page);
  }
  reviews(page = 1) {
    return this.api.reviews(page);
  }
  review(id: string) {
    return this.api.review(id);
  }
  linkCandidates(page = 1) {
    return this.api.linkCandidates(page);
  }
  decideLink(current: LinkReview, approve: boolean, reason: string, target?: LinkCandidate) {
    return this.api.decideLink(current, approve, reason, target);
  }
  followUps(page = 1, filters: Record<string, string> = {}) {
    return this.api.followUps(page, filters);
  }
  notes(id: string, page = 1, leadId?: string) {
    return this.api.notes(id, page, leadId);
  }
  needsHistory(id: string, page = 1) {
    return this.api.needsHistory(id, page);
  }
  leads(id: string, page = 1, status?: string) {
    return this.api.leads(id, page, status);
  }
  lead(id: string, leadId: string) {
    return this.api.lead(id, leadId);
  }
  leadHistory(id: string, leadId: string, page = 1) {
    return this.api.leadHistory(id, leadId, page);
  }
  command(body: Record<string, unknown>): CrmCommand {
    return Object.freeze({
      key: crypto.randomUUID(),
      body: Object.freeze({ ...body }),
      revision: this.session.revision(),
    });
  }
  intake(values: CustomerValues) {
    return this.command(this.api.contact(values));
  }
  assignment(current: CrmCustomer, employeeId: string, reason: string) {
    return this.command({
      employeeId: crmId(employeeId),
      version: current.version,
      reason: reason.trim() || null,
    });
  }
  needs(current: CrmCustomer, requirements: string) {
    return this.command({ version: current.version, requirements: requirements.trim() || null });
  }
  note(body: string, leadId?: string) {
    return this.command({ body: body.trim(), ...(leadId ? { leadId: crmId(leadId) } : {}) });
  }
  openLead(projectId: string, source: string) {
    return this.command({ projectId: crmId(projectId), source: source.trim() });
  }
  closeLead(lead: Lead, reason: string) {
    return this.command({ version: lead.version, reason: reason.trim() });
  }
  execute(operation: CrmOperation) {
    if (operation.command.revision !== this.session.revision()) throw new ApiFailure('stale');
    return this.api.execute(operation);
  }
}
