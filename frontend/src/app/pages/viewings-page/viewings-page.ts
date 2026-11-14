import { Component, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormControl, FormRecord, ReactiveFormsModule } from '@angular/forms';
import { PageHeading } from '../../components/page-heading/page-heading';
import { CrmPager } from '../../components/crm-pager/crm-pager';
import { ViewingService } from '../../services/viewing.service';
import { SessionContext } from '../../services/session-context.service';
import { ApiFailure } from '../../models/api-failure';
import { crmError } from '../../models/crm-ui';
import { CrmPage } from '../../models/crm';
import { formatProjectTime } from '../../models/project-time';
import {
  OwnViewing,
  StaffViewing,
  OwnViewingEvent,
  StaffViewingEvent,
  ViewingAction,
  ViewingOperation,
  VIEWING_STATUSES,
  viewingLabel,
} from '../../models/viewing';
type Field = { key: string; label: string; type?: string; max?: number; optional?: boolean };
const start: Field = { key: 'proposedStartUtc', label: 'Proposed start', type: 'datetime-local' };
const reason: Field = { key: 'reason', label: 'Reason', max: 1000 };
const coordination: Field[] = [
  { key: 'coordinatedAtUtc', label: 'Actual external coordination time', type: 'datetime-local' },
  { key: 'coordinationSource', label: 'External coordination source', max: 200 },
  {
    key: 'coordinationReference',
    label: 'Coordination reference (optional)',
    max: 500,
    optional: true,
  },
];
const formFields: Record<ViewingAction, Field[]> = {
  submit: [{ key: 'unitId', label: 'Published unit reference', max: 36 }, start],
  'accept-alternative': [],
  cancel: [reason],
  'change-proposal': [start, reason],
  confirm: [
    { key: 'confirmedEndUtc', label: 'Coordinated end time', type: 'datetime-local' },
    ...coordination,
  ],
  alternatives: [
    start,
    { key: 'proposedEndUtc', label: 'Alternative end time', type: 'datetime-local' },
    ...coordination,
    reason,
  ],
  outcome: [
    { key: 'occurredAtUtc', label: 'Actual outcome time', type: 'datetime-local' },
    { key: 'notes', label: 'Internal follow-up notes', max: 2000 },
  ],
};
const actionNames: Record<ViewingAction, string> = {
  submit: 'Submit viewing request',
  'accept-alternative': 'Accept alternative',
  cancel: 'Cancel viewing',
  'change-proposal': 'Request reschedule',
  confirm: 'Confirm coordinated time',
  alternatives: 'Propose an alternative to the customer',
  outcome: 'Record viewing outcome',
};
@Component({
  selector: 'app-viewings-page',
  imports: [ReactiveFormsModule, RouterLink, PageHeading, CrmPager],
  templateUrl: './viewings-page.html',
  styleUrls: ['../account-page/account-page.css', '../crm-page.css'],
})
export class ViewingsPage {
  private readonly service = inject(ViewingService);
  private readonly route = inject(ActivatedRoute);
  protected readonly session = inject(SessionContext);
  protected readonly staff = this.route.snapshot.data['staff'] === true;
  protected readonly allowed = () =>
    this.session
      .account()
      ?.roles.some((r) =>
        this.staff ? ['SalesEmployee', 'SalesManager'].includes(r) : r === 'Customer',
      ) ?? false;
  protected readonly list = signal<CrmPage<OwnViewing | StaffViewing> | null>(null);
  protected readonly current = signal<OwnViewing | StaffViewing | null>(null);
  protected readonly history = signal<CrmPage<OwnViewingEvent | StaffViewingEvent> | null>(null);
  protected readonly page = signal(1);
  protected readonly historyPage = signal(1);
  protected readonly statuses = VIEWING_STATUSES;
  protected readonly label = viewingLabel;
  protected readonly actionName = (a: ViewingAction) => actionNames[a];
  protected readonly busy = signal(false);
  protected readonly blocked = signal(false);
  protected readonly pending = signal<ViewingOperation | null>(null);
  protected readonly error = signal('');
  protected readonly message = signal('');
  protected readonly action = signal<ViewingAction | null>(null);
  protected readonly status = new FormControl('', { nonNullable: true });
  protected readonly cancellationKind = new FormControl('Broker', { nonNullable: true });
  protected readonly outcome = new FormControl('Completed', { nonNullable: true });
  protected readonly form = new FormRecord<FormControl<string>>({});
  protected readonly deviceZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
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
  protected staffRecord() {
    const item = this.current();
    return this.staff && item && 'customerId' in item ? item : null;
  }
  protected staffEvent(item: OwnViewingEvent | StaffViewingEvent) {
    return this.staff && 'customerId' in item ? item : null;
  }
  protected fields() {
    const a = this.action();
    return a ? formFields[a] : [];
  }
  protected date(value: string | null, zone?: string) {
    return formatProjectTime(value, zone ?? this.current()?.timeZoneId ?? 'UTC');
  }
  private valid(turn: number, revision: number) {
    return turn === this.turn && revision === this.session.revision();
  }
  private resetForm() {
    for (const key of Object.keys(this.form.controls)) this.form.removeControl(key);
    this.action.set(null);
  }
  private clear() {
    this.list.set(null);
    this.current.set(null);
    this.history.set(null);
    this.pending.set(null);
    this.blocked.set(false);
    this.resetForm();
  }
  protected async reload() {
    if (this.busy() || !this.allowed()) return;
    const selected = this.current();
    const pending = this.pending();
    this.current.set(null);
    this.history.set(null);
    this.list.set(null);
    this.resetForm();
    this.blocked.set(!!pending);
    this.error.set('');
    this.message.set('');
    await this.read(async (t, r) => {
      const list = await this.service.list(this.staff, this.page(), this.status.value);
      if (!this.valid(t, r)) return;
      this.list.set(list);
      if (selected) await this.load(selected, t, r);
    });
  }
  private async read(work: (t: number, r: number) => Promise<void>) {
    const turn = ++this.turn,
      revision = this.session.revision();
    this.busy.set(true);
    try {
      await work(turn, revision);
    } catch (e) {
      if (this.valid(turn, revision)) this.fail(e);
    } finally {
      if (turn === this.turn) this.busy.set(false);
    }
  }
  private async load(item: OwnViewing | StaffViewing, t: number, r: number) {
    const customer = 'customerId' in item ? item.customerId : undefined;
    const [current, history] = await Promise.all([
      this.service.detail(this.staff, item.id, customer),
      this.service.history(this.staff, item.id, customer, this.historyPage()),
    ]);
    const latest = await this.service.detail(this.staff, item.id, customer);
    if (!this.valid(t, r)) return;
    this.current.set(latest);
    this.history.set(history);
    if (current.version !== latest.version) {
      this.blocked.set(true);
      this.error.set('The viewing changed while reading. Reload before taking another action.');
    }
  }
  protected async open(item: OwnViewing | StaffViewing) {
    if (this.busy() || this.pending()) return;
    this.current.set(null);
    this.history.set(null);
    this.resetForm();
    this.blocked.set(false);
    this.error.set('');
    this.message.set('');
    this.historyPage.set(1);
    await this.read((t, r) => this.load(item, t, r));
  }
  protected async changePage(delta: number) {
    if (this.busy() || this.pending()) return;
    this.page.update((n) => Math.max(1, n + delta));
    this.current.set(null);
    await this.reload();
  }
  protected async changeFilter() {
    if (this.busy() || this.pending()) return;
    this.page.set(1);
    this.current.set(null);
    await this.reload();
  }
  protected async changeHistory(delta: number) {
    if (this.busy() || this.pending()) return;
    this.historyPage.update((n) => Math.max(1, n + delta));
    await this.reload();
  }
  protected actions(): ViewingAction[] {
    const item = this.current();
    if (!item) return [];
    if (this.staff)
      return item.status === 'PendingConfirmation'
        ? ['confirm', 'alternatives', 'cancel']
        : item.status === 'AwaitingCustomerAcceptance'
          ? ['alternatives', 'cancel']
          : item.status === 'Confirmed'
            ? ['cancel', 'outcome']
            : [];
    return item.status === 'AwaitingCustomerAcceptance'
      ? ['accept-alternative', 'change-proposal', 'cancel']
      : ['PendingConfirmation', 'Confirmed'].includes(item.status)
        ? ['change-proposal', 'cancel']
        : [];
  }
  protected choose(a: ViewingAction) {
    if (this.busy() || this.blocked() || this.pending()) return;
    if (a === 'submit' ? this.staff : !this.actions().includes(a)) return;
    this.resetForm();
    this.action.set(a);
    for (const field of formFields[a])
      this.form.addControl(field.key, new FormControl('', { nonNullable: true }));
    if (a === 'submit') {
      const id = this.route.snapshot.queryParamMap.get('unitId');
      if (id) this.form.controls['unitId'].setValue(id);
    }
    this.error.set('');
    this.message.set('');
  }
  protected async submit() {
    const action = this.action();
    if (!action || this.busy() || this.blocked() || this.pending()) return;
    const fields = this.form.getRawValue();
    if (
      this.fields().some(
        (f) =>
          (!f.optional && !fields[f.key]?.trim()) ||
          (f.max && (fields[f.key]?.length ?? 0) > f.max),
      )
    ) {
      this.error.set('Complete the required fields and check text lengths.');
      return;
    }
    try {
      const op = this.service.command(action, this.staff, this.current(), {
        ...fields,
        cancellationKind: this.cancellationKind.value,
        outcome: this.outcome.value,
      });
      await this.run(op);
    } catch (e) {
      this.fail(e);
    }
  }
  protected async recover() {
    const op = this.pending();
    if (op && !this.busy()) await this.run(op);
  }
  private async run(op: ViewingOperation) {
    if (this.busy() || !this.allowed()) return;
    const turn = ++this.turn,
      revision = this.session.revision();
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    this.pending.set(op);
    let saved = false;
    try {
      const reply = await this.service.execute(op);
      saved = true;
      if (!this.valid(turn, revision)) return;
      this.pending.set(null);
      this.blocked.set(false);
      this.current.set(null);
      this.history.set(null);
      this.resetForm();
      this.message.set(
        reply.isReplay
          ? 'The server confirmed the previous operation without repeating its effect.'
          : 'The operation was saved on the server.',
      );
      const list = await this.service.list(this.staff, this.page(), this.status.value);
      if (!this.valid(turn, revision)) return;
      this.list.set(list);
      const actual = await this.service.detail(this.staff, reply.id, op.customerId);
      await this.load(actual, turn, revision);
    } catch (e) {
      if (!this.valid(turn, revision)) return;
      if (e instanceof ApiFailure && ['forbidden', 'unauthorized', 'not-found'].includes(e.kind))
        this.fail(e);
      else if (saved) {
        this.pending.set(null);
        this.blocked.set(true);
        this.error.set(
          'The operation was saved, but the display could not refresh. Reload and review the current state.',
        );
      } else if (e instanceof ApiFailure && e.outcomeUnknown) {
        this.blocked.set(true);
        this.error.set(
          'The result is uncertain and may have been saved. Review the current state or confirm the same saved operation before submitting again.',
        );
      } else {
        this.pending.set(null);
        this.fail(e);
      }
    } finally {
      if (turn === this.turn) this.busy.set(false);
    }
  }
  private fail(e: unknown) {
    if (e instanceof ApiFailure && ['forbidden', 'unauthorized', 'not-found'].includes(e.kind)) {
      this.clear();
      this.error.set(crmError(e));
      return;
    }
    if (e instanceof ApiFailure && (e.kind === 'conflict' || e.outcomeUnknown))
      this.blocked.set(true);
    this.error.set(crmError(e));
  }
}
