import { Component, effect, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PageHeading } from '../../components/page-heading/page-heading';
import { CustomerFields } from '../../components/customer-fields/customer-fields';
import { CrmPager } from '../../components/crm-pager/crm-pager';
import { CrmService } from '../../services/crm.service';
import { SessionContext } from '../../services/session-context.service';
import { ApiFailure } from '../../models/api-failure';
import {
  Assignment,
  AssignmentCandidate,
  CrmCustomer,
  CrmOperation,
  CrmPage,
  Lead,
  LeadEvent,
  NeedsChange,
  SalesNote,
} from '../../models/crm';
import { customerForm, crmError } from '../../models/crm-ui';
import { CatalogSearchResult } from '../../models/catalog';
type DetailList = 'notes' | 'needs' | 'assignments' | 'leads';
const requiredText = (max = 2000) =>
  new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(max)],
  });
@Component({
  selector: 'app-crm-page',
  imports: [DatePipe, ReactiveFormsModule, RouterLink, PageHeading, CustomerFields, CrmPager],
  templateUrl: './crm-page.html',
  styleUrls: ['../account-page/account-page.css', '../crm-page.css'],
})
export class CrmPageComponent {
  private readonly service = inject(CrmService);
  protected readonly session = inject(SessionContext);
  protected readonly manager = () =>
    this.session.account()?.roles.includes('SalesManager') ?? false;
  protected readonly allowed = () =>
    this.manager() || (this.session.account()?.roles.includes('SalesEmployee') ?? false);
  protected readonly busy = signal(false);
  protected readonly blocked = signal(false);
  protected readonly error = signal('');
  protected readonly message = signal('');
  protected readonly customers = signal<CrmPage<CrmCustomer> | null>(null);
  protected readonly current = signal<CrmCustomer | null>(null);
  protected readonly queue = new FormControl('customers', { nonNullable: true });
  protected readonly hasRequirements = new FormControl('', { nonNullable: true });
  protected readonly hasNotes = new FormControl('', { nonNullable: true });
  protected readonly listPage = signal(1);
  protected readonly intakeForm = customerForm();
  protected readonly candidates = signal<CrmPage<AssignmentCandidate> | null>(null);
  protected readonly candidatePage = signal(1);
  protected readonly target = new FormControl('', { nonNullable: true });
  protected readonly assignmentReason = new FormControl('', {
    nonNullable: true,
    validators: [Validators.maxLength(2000)],
  });
  protected readonly requirements = new FormControl('', {
    nonNullable: true,
    validators: [Validators.maxLength(2000)],
  });
  protected readonly noteForm = new FormGroup({
    body: requiredText(),
    leadId: new FormControl('', { nonNullable: true }),
  });
  protected readonly correction = signal<SalesNote | null>(null);
  protected readonly notes = signal<CrmPage<SalesNote> | null>(null);
  protected readonly needs = signal<CrmPage<NeedsChange> | null>(null);
  protected readonly assignments = signal<CrmPage<Assignment> | null>(null);
  protected readonly leads = signal<CrmPage<Lead> | null>(null);
  protected readonly pages = signal<Record<DetailList, number>>({
    notes: 1,
    needs: 1,
    assignments: 1,
    leads: 1,
  });
  protected readonly leadForm = new FormGroup({
    projectId: requiredText(36),
    source: requiredText(100),
  });
  protected readonly leadFilter = new FormControl('', { nonNullable: true });
  protected readonly selectedLead = signal<Lead | null>(null);
  protected readonly leadEvents = signal<CrmPage<LeadEvent> | null>(null);
  protected readonly leadEventPage = signal(1);
  protected readonly closeReason = requiredText();
  protected readonly projectSearch = new FormControl('', { nonNullable: true });
  protected readonly publicProjects = signal<CatalogSearchResult | null>(null);
  protected readonly projectPage = signal(1);
  protected readonly pending = signal<CrmOperation | null>(null);
  private turn = 0;
  private revision = this.session.revision();
  constructor() {
    void this.reload();
    effect(() => {
      const revision = this.session.revision();
      if (revision !== this.revision) {
        this.revision = revision;
        this.turn++;
        this.clear();
        this.error.set('');
        this.message.set('');
        this.busy.set(false);
      }
    });
  }
  private clearDetails() {
    this.current.set(null);
    this.notes.set(null);
    this.needs.set(null);
    this.assignments.set(null);
    this.leads.set(null);
    this.selectedLead.set(null);
    this.leadEvents.set(null);
    this.correction.set(null);
    this.noteForm.reset();
    this.requirements.reset();
    this.target.reset();
    this.assignmentReason.reset();
    this.leadForm.reset();
    this.closeReason.reset();
    this.pending.set(null);
    this.blocked.set(false);
  }
  private clear() {
    this.clearDetails();
    this.customers.set(null);
    this.candidates.set(null);
    this.publicProjects.set(null);
    this.intakeForm.reset();
    this.projectSearch.reset();
  }
  private valid(turn: number, revision: number) {
    return turn === this.turn && revision === this.session.revision();
  }
  protected async reload() {
    if (this.busy()) return;
    const id = this.current()?.id;
    const pending = this.pending();
    this.clearDetails();
    this.pending.set(pending);
    this.blocked.set(!!pending);
    this.customers.set(null);
    this.error.set('');
    this.message.set('');
    await this.read(async (turn, revision) => {
      const list = await this.loadList();
      if (!this.valid(turn, revision)) return;
      this.customers.set(list);
      if (id) await this.loadCustomer(id, turn, revision);
    });
  }
  private loadList() {
    return this.queue.value === 'followups'
      ? this.service.followUps(this.listPage(), {
          ...(this.hasRequirements.value ? { hasRequirements: this.hasRequirements.value } : {}),
          ...(this.hasNotes.value ? { hasNotes: this.hasNotes.value } : {}),
        })
      : this.service.customers(
          this.listPage(),
          this.manager() && this.queue.value === 'unassigned',
        );
  }
  protected async changeQueue() {
    if (this.busy() || this.pending()) return;
    this.listPage.set(1);
    this.clearDetails();
    await this.reload();
  }
  protected async changeList(delta: number) {
    if (this.busy() || this.pending()) return;
    this.listPage.update((page) => Math.max(1, page + delta));
    this.clearDetails();
    await this.reload();
  }
  protected async open(id: string) {
    if (this.busy() || this.pending()) return;
    this.clearDetails();
    this.pages.set({ notes: 1, needs: 1, assignments: 1, leads: 1 });
    this.leadFilter.reset();
    this.error.set('');
    this.message.set('');
    await this.read((turn, revision) => this.loadCustomer(id, turn, revision));
  }
  private async loadCustomer(id: string, turn: number, revision: number) {
    const pages = this.pages();
    const [customer, notes, needs, assignments, leads] = await Promise.all([
      this.service.customer(id),
      this.service.notes(id, pages.notes),
      this.service.needsHistory(id, pages.needs),
      this.service.assignments(id, pages.assignments),
      this.service.leads(id, pages.leads, this.leadFilter.value || undefined),
    ]);
    // Recheck scope after related reads; a former assignee must not retain authored notes.
    const latest = await this.service.customer(id);
    if (!this.valid(turn, revision)) return;
    this.current.set(latest);
    this.requirements.setValue(latest.requirements ?? '');
    this.notes.set(notes);
    this.needs.set(needs);
    this.assignments.set(assignments);
    this.leads.set(leads);
    if (customer.version !== latest.version) {
      this.blocked.set(true);
      this.error.set('Customer data changed while reading. Reload and review it before saving.');
    }
  }
  private async read(action: (turn: number, revision: number) => Promise<void>) {
    if (this.busy() || !this.allowed()) return;
    const turn = ++this.turn,
      revision = this.session.revision();
    this.busy.set(true);
    try {
      await action(turn, revision);
    } catch (error) {
      if (this.valid(turn, revision)) this.fail(error);
    } finally {
      if (turn === this.turn) this.busy.set(false);
    }
  }
  protected async detailPage(kind: DetailList, delta: number) {
    if (this.busy() || !this.current()) return;
    this.pages.update((pages) => ({ ...pages, [kind]: Math.max(1, pages[kind] + delta) }));
    await this.reload();
  }
  protected async filterLeads() {
    this.pages.update((pages) => ({ ...pages, leads: 1 }));
    await this.reload();
  }
  protected async getCandidates(delta = 0) {
    if (this.busy() || !this.manager()) return;
    this.candidatePage.update((page) => Math.max(1, page + delta));
    this.candidates.set(null);
    this.target.reset();
    await this.read(async (turn, revision) => {
      const list = await this.service.candidates(this.candidatePage());
      if (this.valid(turn, revision)) this.candidates.set(list);
    });
  }
  protected async intake() {
    if (!this.canMutate() || !this.manager()) return;
    this.intakeForm.markAllAsTouched();
    if (this.intakeForm.invalid) return;
    await this.run({
      action: 'intake',
      command: this.service.intake(this.intakeForm.getRawValue()),
    });
  }
  protected async assign() {
    const customer = this.current();
    if (this.busy() || this.blocked() || this.pending() || !this.manager() || !customer) return;
    if (
      !this.target.value ||
      this.assignmentReason.invalid ||
      (customer.assignedEmployeeId && !this.assignmentReason.value.trim())
    ) {
      this.error.set(
        'Select an eligible employee and enter a transfer reason when changing the assignee.',
      );
      return;
    }
    try {
      await this.run({
        action: customer.assignedEmployeeId ? 'transfer' : 'assign',
        customerId: customer.id,
        command: this.service.assignment(customer, this.target.value, this.assignmentReason.value),
      });
    } catch (error) {
      this.fail(error);
    }
  }
  protected async saveNeeds() {
    const customer = this.current();
    if (!customer || this.requirements.invalid || !this.canMutate()) return;
    await this.run({
      action: 'needs',
      customerId: customer.id,
      command: this.service.needs(customer, this.requirements.value),
    });
  }
  protected startCorrection(note: SalesNote | null) {
    if (this.busy() || this.pending()) return;
    this.correction.set(note);
    this.noteForm.reset();
  }
  protected async addNote() {
    const customer = this.current();
    if (!customer || !this.canMutate()) return;
    this.noteForm.markAllAsTouched();
    if (this.noteForm.invalid) return;
    try {
      const correction = this.correction();
      await this.run({
        action: correction ? 'correctNote' : 'addNote',
        customerId: customer.id,
        ...(correction ? { resourceId: correction.id } : {}),
        command: this.service.note(
          this.noteForm.controls.body.value,
          correction ? undefined : this.noteForm.controls.leadId.value || undefined,
        ),
      });
    } catch (error) {
      this.fail(error);
    }
  }
  protected async openLead() {
    const customer = this.current();
    if (!customer || !this.canMutate()) return;
    this.leadForm.markAllAsTouched();
    if (this.leadForm.invalid) return;
    try {
      await this.run({
        action: 'openLead',
        customerId: customer.id,
        command: this.service.openLead(
          this.leadForm.controls.projectId.value,
          this.leadForm.controls.source.value,
        ),
      });
    } catch (error) {
      this.fail(error);
    }
  }
  protected async inspectLead(lead: Lead, delta = 0) {
    const customer = this.current();
    if (!customer || this.busy()) return;
    if (!delta) this.leadEventPage.set(1);
    else this.leadEventPage.update((page) => Math.max(1, page + delta));
    this.selectedLead.set(null);
    this.leadEvents.set(null);
    this.closeReason.reset();
    await this.read(async (turn, revision) => {
      const [current, events] = await Promise.all([
        this.service.lead(customer.id, lead.id),
        this.service.leadHistory(customer.id, lead.id, this.leadEventPage()),
      ]);
      await this.service.customer(customer.id);
      if (!this.valid(turn, revision)) return;
      this.selectedLead.set(current);
      this.leadEvents.set(events);
    });
  }
  protected async closeLead() {
    const customer = this.current(),
      lead = this.selectedLead();
    if (!customer || !lead || !this.canMutate()) return;
    this.closeReason.markAsTouched();
    if (this.closeReason.invalid) return;
    await this.run({
      action: 'closeLead',
      customerId: customer.id,
      resourceId: lead.id,
      command: this.service.closeLead(lead, this.closeReason.value),
    });
  }
  protected canMutate() {
    return !this.busy() && !this.blocked() && !this.pending() && this.allowed();
  }
  protected async recover() {
    const operation = this.pending();
    if (!operation || this.busy()) return;
    await this.run(operation);
  }
  private async run(operation: CrmOperation) {
    if (this.busy() || !this.allowed()) return;
    const turn = ++this.turn,
      revision = this.session.revision();
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    let saved = false;
    this.pending.set(operation);
    try {
      const reply = await this.service.execute(operation);
      saved = true;
      if (!this.valid(turn, revision)) return;
      this.pending.set(null);
      this.blocked.set(false);
      this.message.set(
        reply.isReplay
          ? 'The server confirmed the previous operation with the same receipt.'
          : operation.action === 'openLead' && reply.created === false
            ? 'The existing open lead for the same customer and project was returned.'
            : 'The operation was saved on the server.',
      );
      this.clearDetails();
      if (operation.action === 'intake') this.intakeForm.reset();
      const list = await this.loadList();
      if (!this.valid(turn, revision)) return;
      this.customers.set(list);
      await this.loadCustomer(
        operation.action === 'intake' ? reply.customerId : operation.customerId!,
        turn,
        revision,
      );
    } catch (error) {
      if (!this.valid(turn, revision)) return;
      if (
        error instanceof ApiFailure &&
        (error.kind === 'forbidden' || error.kind === 'not-found' || error.kind === 'unauthorized')
      )
        this.fail(error);
      else if (saved) {
        this.pending.set(null);
        this.blocked.set(true);
        this.error.set(
          'The operation was saved, but the display could not refresh. Reload the data.',
        );
      } else if (error instanceof ApiFailure && error.outcomeUnknown) {
        this.blocked.set(true);
        this.error.set(
          'The result is uncertain. Review the state or confirm the same operation using its saved key and data before submitting a new operation.',
        );
      } else {
        this.pending.set(null);
        this.fail(error);
      }
    } finally {
      if (turn === this.turn) this.busy.set(false);
    }
  }
  protected async searchProjects(delta = 0) {
    if (this.busy()) return;
    if (delta) this.projectPage.update((page) => Math.max(1, page + delta));
    else this.projectPage.set(1);
    this.publicProjects.set(null);
    await this.read(async (turn, revision) => {
      const list = await this.service.publicProjects(this.projectSearch.value, this.projectPage());
      if (this.valid(turn, revision)) this.publicProjects.set(list);
    });
  }
  protected useProject(id: string | undefined) {
    if (id) this.leadForm.controls.projectId.setValue(id);
  }
  private fail(error: unknown) {
    if (
      error instanceof ApiFailure &&
      ['forbidden', 'not-found', 'unauthorized'].includes(error.kind)
    ) {
      this.clear();
      this.error.set(
        error.kind === 'forbidden'
          ? 'The customer or operation is no longer within your permissions. Previous data was cleared.'
          : crmError(error),
      );
      return;
    }
    if (error instanceof ApiFailure && (error.kind === 'conflict' || error.outcomeUnknown))
      this.blocked.set(true);
    this.error.set(crmError(error));
  }
}
