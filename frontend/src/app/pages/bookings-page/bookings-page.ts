import { Component, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormControl, FormRecord, ReactiveFormsModule } from '@angular/forms';
import { PageHeading } from '../../components/page-heading/page-heading';
import { CrmPager } from '../../components/crm-pager/crm-pager';
import { DisplayNumberPipe, DisplayCurrencyPipe } from '../../pipes/display-format.pipe';
import { BookingService } from '../../services/booking.service';
import { SessionContext } from '../../services/session-context.service';
import { ApiFailure } from '../../models/api-failure';
import { crmError } from '../../models/crm-ui';
import { CrmPage } from '../../models/crm';
import { formatProjectTime } from '../../models/project-time';
import {
  OwnBooking,
  StaffBooking,
  OwnBookingEvent,
  StaffBookingEvent,
  OwnDeposit,
  StaffDeposit,
  OwnRefund,
  StaffRefund,
  DeveloperResponse,
  ReservationExtension,
  BookingAction,
  BookingOperation,
  BOOKING_STATUSES,
  bookingLabel,
} from '../../models/booking';
import { BookingFields } from '../../components/booking-fields/booking-fields';
type Field = { key: string; label: string; type?: string; max?: number; optional?: boolean };
type Section = 'history' | 'deposits' | 'responses' | 'extensions' | 'refunds';
const reason: Field = { key: 'reason', label: 'Reason', max: 1000, optional: true };
const evidence: Field[] = [
  { key: 'source', label: 'External event source', max: 200 },
  { key: 'reference', label: 'External event reference', max: 500 },
];
const notes: Field = {
  key: 'notes',
  label: 'Internal notes (optional)',
  max: 1000,
  optional: true,
};
const actions: Record<BookingAction, string> = {
  submit: 'Submit booking request',
  cancel: 'Cancel booking',
  responses: 'Record developer response',
  extensions: 'Record external extension',
  deposits: 'Record external deposit',
  refunds: 'Record external deposit refund',
};
const fields: Record<BookingAction, Field[]> = {
  submit: [
    { key: 'unitId', label: 'Published unit reference', max: 36 },
    {
      key: 'requestReason',
      label: 'Booking request information (optional)',
      max: 1000,
      optional: true,
    },
  ],
  cancel: [reason],
  responses: [
    {
      key: 'developerRespondedAtUtc',
      label: 'Actual developer response time',
      type: 'datetime-local',
    },
    ...evidence,
  ],
  extensions: [
    {
      key: 'developerExtendedAtUtc',
      label: 'Actual external extension time',
      type: 'datetime-local',
    },
    {
      key: 'newReservationExpiresAtUtc',
      label: 'New external booking deadline',
      type: 'datetime-local',
    },
    ...evidence,
    reason,
  ],
  deposits: [
    { key: 'amount', label: 'Amount actually paid', max: 20 },
    { key: 'currencyCode', label: 'Deposit currency (three letters)', max: 3 },
    { key: 'paidAtUtc', label: 'Actual external payment time', type: 'datetime-local' },
    ...evidence,
    notes,
  ],
  refunds: [
    { key: 'amount', label: 'Amount actually refunded', max: 20 },
    { key: 'returnedAtUtc', label: 'Actual external refund time', type: 'datetime-local' },
    ...evidence,
    notes,
  ],
};
const titles: Record<string, string> = {
  id: 'Record reference',
  bookingId: 'Booking reference',
  depositId: 'Deposit reference',
  customerId: 'Customer reference',
  unitId: 'Unit reference',
  leadId: 'Original lead',
  submissionAgreementId: 'Marketing agreement at submission',
  submissionLocalDate: 'Local submission date',
  timeZoneId: 'Project time',
  requestedByAccountId: 'Requester',
  requestedAtUtc: 'Request submission time',
  responseDueAtUtc: 'Developer response deadline',
  requestReason: 'Request information',
  rejectionReason: 'Booking rejection reason',
  confirmedAtUtc: 'Booking confirmation time',
  confirmedPrice: 'Saved booking price',
  confirmedCurrencyCode: 'Booking price currency',
  reservationExpiresAtUtc: 'Confirmed booking deadline',
  cancelledAtUtc: 'Cancellation time',
  cancellationReason: 'Cancellation reason',
  completedAtUtc: 'Booking completion time with deal',
  version: 'Record version',
  bookingVersion: 'Booking version',
  requestedVersion: 'Booking version at event recording',
  bookingVersionAfter: 'Booking version after event',
  amount: 'Actual amount',
  currencyCode: 'Currency',
  paidAtUtc: 'External payment time',
  returnedAtUtc: 'External refund time',
  depositPaidAtUtc: 'Original deposit payment time',
  recordedAtUtc: 'Record creation time',
  source: 'Source',
  reference: 'Reference',
  notes: 'Internal notes',
  recorderAccountId: 'Recorder account',
  recorderEmployeeId: 'Recording employee profile',
  developerRespondedAtUtc: 'Developer response time',
  offeredPrice: 'Developer offered price',
  offeredDeadlineUtc: 'Developer offered deadline',
  reason: 'Recorded reason',
  decision: 'System response decision',
  responseKind: 'Developer response type',
  statusAfter: 'Booking state after event',
  developerExtendedAtUtc: 'External extension time',
  previousReservationExpiresAtUtc: 'Previous deadline',
  newReservationExpiresAtUtc: 'New deadline',
  actorAccountId: 'Actor',
  actorKind: 'Actor type',
  effectiveCutoffUtc: 'Actual time boundary',
  cancelledFromStatus: 'State before cancellation',
  dealId: 'Deal reference',
  completedDealId: 'Deal completing the booking',
  recordedRefundAmount: 'Amount with a recorded refund',
  recordedUnrefundedAmount: 'Deposit principal with no recorded refund',
  recordedRefundStatus: 'Recorded refund status',
};
@Component({
  selector: 'app-bookings-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    PageHeading,
    CrmPager,
    BookingFields,
    DisplayNumberPipe,
    DisplayCurrencyPipe,
  ],
  templateUrl: './bookings-page.html',
  styleUrls: ['../account-page/account-page.css', '../crm-page.css'],
})
export class BookingsPage {
  private readonly service = inject(BookingService);
  private readonly route = inject(ActivatedRoute);
  protected readonly session = inject(SessionContext);
  protected readonly staff = this.route.snapshot.data['staff'] === true;
  protected readonly allowed = () =>
    this.session
      .account()
      ?.roles.some((r) =>
        this.staff ? ['SalesEmployee', 'SalesManager'].includes(r) : r === 'Customer',
      ) ?? false;
  protected readonly current = signal<OwnBooking | StaffBooking | null>(null);
  protected readonly list = signal<CrmPage<OwnBooking | StaffBooking> | null>(null);
  protected readonly history = signal<CrmPage<OwnBookingEvent | StaffBookingEvent> | null>(null);
  protected readonly deposits = signal<CrmPage<OwnDeposit | StaffDeposit> | null>(null);
  protected readonly deposit = signal<OwnDeposit | StaffDeposit | null>(null);
  protected readonly refunds = signal<CrmPage<OwnRefund | StaffRefund> | null>(null);
  protected readonly responses = signal<CrmPage<DeveloperResponse> | null>(null);
  protected readonly extensions = signal<CrmPage<ReservationExtension> | null>(null);
  protected readonly page = signal(1);
  protected readonly pages = signal<Record<Section, number>>({
    history: 1,
    deposits: 1,
    responses: 1,
    extensions: 1,
    refunds: 1,
  });
  protected readonly status = new FormControl('', { nonNullable: true });
  protected readonly responseKind = new FormControl('Confirmed', { nonNullable: true });
  protected readonly statuses = BOOKING_STATUSES;
  protected readonly label = bookingLabel;
  protected readonly actionName = (a: BookingAction) => actions[a];
  protected readonly busy = signal(false);
  protected readonly blocked = signal(false);
  protected readonly pending = signal<BookingOperation | null>(null);
  protected readonly action = signal<BookingAction | null>(null);
  protected readonly error = signal('');
  protected readonly message = signal('');
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
    const value = this.current();
    return this.staff && value && 'customerId' in value ? value : null;
  }
  protected zone() {
    return this.staffRecord()?.timeZoneId ?? this.deviceZone;
  }
  protected rowZone(item: OwnBooking | StaffBooking) {
    return 'timeZoneId' in item ? item.timeZoneId : this.deviceZone;
  }
  protected actor(item: OwnBookingEvent | StaffBookingEvent) {
    return 'actorAccountId' in item ? (item.actorAccountId ?? 'System') : null;
  }
  protected date(value: string | null, zone?: string) {
    return formatProjectTime(value, zone ?? this.zone());
  }
  protected entries(row: object, keys: readonly string[]) {
    const r = row as Record<string, string | null>;
    return keys.map((key) => ({
      key,
      label: titles[key] ?? 'Saved reference',
      value: r[key] ?? null,
      type: key.endsWith('Utc')
        ? 'time'
        : /(amount|price)$/i.test(key)
          ? 'money'
          : [
                'decision',
                'responseKind',
                'statusAfter',
                'rejectionReason',
                'recordedRefundStatus',
                'cancelledFromStatus',
              ].includes(key)
            ? 'label'
            : 'text',
    }));
  }
  protected detailFields() {
    const current = this.current();
    return current
      ? this.entries(current, [
          'requestedAtUtc',
          'responseDueAtUtc',
          'requestReason',
          'rejectionReason',
          'confirmedAtUtc',
          'reservationExpiresAtUtc',
          'cancelledAtUtc',
          'cancellationReason',
          'completedAtUtc',
        ])
      : [];
  }
  protected privateFields() {
    const current = this.staffRecord();
    return current
      ? this.entries(current, [
          'customerId',
          'leadId',
          'submissionAgreementId',
          'submissionLocalDate',
          'timeZoneId',
          'requestedByAccountId',
          'completedDealId',
        ])
      : [];
  }
  protected factFields(row: DeveloperResponse | ReservationExtension) {
    return 'decision' in row
      ? this.entries(row, [
          'responseKind',
          'decision',
          'statusAfter',
          'developerRespondedAtUtc',
          'offeredPrice',
          'currencyCode',
          'offeredDeadlineUtc',
          'source',
          'reference',
          'reason',
          'recordedAtUtc',
          'recorderAccountId',
          'requestedVersion',
          'bookingVersionAfter',
        ])
      : this.entries(row, [
          'previousReservationExpiresAtUtc',
          'newReservationExpiresAtUtc',
          'developerExtendedAtUtc',
          'source',
          'reference',
          'reason',
          'recordedAtUtc',
          'recorderAccountId',
          'requestedVersion',
          'bookingVersionAfter',
        ]);
  }
  protected financialFields(row: OwnDeposit | StaffDeposit | OwnRefund | StaffRefund) {
    return this.entries(row, [
      ...('returnedAtUtc' in row ? ['returnedAtUtc'] : ['paidAtUtc']),
      'recordedAtUtc',
      ...('source' in row
        ? ['source', 'reference', 'notes', 'recorderAccountId', 'requestedVersion']
        : []),
    ]);
  }
  protected fields() {
    const action = this.action();
    if (!action) return [];
    if (action !== 'responses') return fields[action];
    return [
      ...fields.responses,
      ...(this.responseKind.value === 'Confirmed'
        ? [
            { key: 'offeredPrice', label: 'Developer confirmed price', max: 20 },
            { key: 'currencyCode', label: 'Price currency (three letters)', max: 3 },
            {
              key: 'offeredDeadlineUtc',
              label: 'Developer confirmed booking deadline',
              type: 'datetime-local',
            },
          ]
        : [{ ...reason, optional: false }]),
    ];
  }
  private valid(t: number, r: number) {
    return t === this.turn && r === this.session.revision();
  }
  private resetForm() {
    for (const k of Object.keys(this.form.controls)) this.form.removeControl(k);
    this.action.set(null);
  }
  private clearDetails() {
    this.current.set(null);
    this.history.set(null);
    this.deposits.set(null);
    this.deposit.set(null);
    this.refunds.set(null);
    this.responses.set(null);
    this.extensions.set(null);
    this.resetForm();
    this.blocked.set(false);
  }
  private clear() {
    this.clearDetails();
    this.list.set(null);
    this.pending.set(null);
  }
  private async read(work: (t: number, r: number) => Promise<void>) {
    const t = ++this.turn,
      r = this.session.revision();
    this.busy.set(true);
    try {
      await work(t, r);
    } catch (e) {
      if (this.valid(t, r)) this.fail(e);
    } finally {
      if (t === this.turn) this.busy.set(false);
    }
  }
  protected async reload() {
    if (this.busy() || !this.allowed()) return;
    const item = this.current(),
      depositId = this.deposit()?.id,
      pending = this.pending();
    this.clearDetails();
    this.list.set(null);
    this.blocked.set(!!pending);
    this.error.set('');
    this.message.set('');
    await this.read(async (t, r) => {
      const list = await this.service.list(this.staff, this.page(), this.status.value);
      if (!this.valid(t, r)) return;
      this.list.set(list);
      if (item) await this.load(item, t, r, depositId);
    });
  }
  private async load(item: OwnBooking | StaffBooking, t: number, r: number, depositId?: string) {
    const customer = 'customerId' in item ? item.customerId : undefined,
      p = this.pages();
    const [current, history, deposits, responses, extensions] = await Promise.all([
      this.service.detail(this.staff, item.id, customer),
      this.service.history(this.staff, item.id, customer, p.history),
      this.service.deposits(this.staff, item.id, customer, p.deposits),
      customer ? this.service.responses(customer, item.id, p.responses) : Promise.resolve(null),
      customer ? this.service.extensions(customer, item.id, p.extensions) : Promise.resolve(null),
    ]);
    const latest = await this.service.detail(this.staff, item.id, customer);
    if (!this.valid(t, r)) return;
    this.current.set(latest);
    this.history.set(history);
    this.deposits.set(deposits);
    this.responses.set(responses);
    this.extensions.set(extensions);
    if (depositId) await this.readDeposit(latest, depositId, t, r);
    if (current.version !== latest.version) {
      this.blocked.set(true);
      this.error.set(
        'The booking changed while reading. Reload and review it before taking an action.',
      );
    }
  }
  protected async open(item: OwnBooking | StaffBooking) {
    if (this.busy() || this.pending()) return;
    this.clearDetails();
    this.pages.set({ history: 1, deposits: 1, responses: 1, extensions: 1, refunds: 1 });
    this.error.set('');
    this.message.set('');
    await this.read((t, r) => this.load(item, t, r));
  }
  private async readDeposit(
    item: OwnBooking | StaffBooking,
    depositId: string,
    t: number,
    r: number,
  ) {
    const customer = 'customerId' in item ? item.customerId : undefined;
    const [deposit, refunds] = await Promise.all([
      this.service.deposit(this.staff, item.id, depositId, customer),
      this.service.refunds(this.staff, item.id, depositId, customer, this.pages().refunds),
    ]);
    const latest = await this.service.detail(this.staff, item.id, customer);
    if (!this.valid(t, r)) return;
    this.current.set(latest);
    this.deposit.set(deposit);
    this.refunds.set(refunds);
    if (item.version !== latest.version) {
      this.blocked.set(true);
      this.error.set(
        'The booking changed while reading. Reload and review it before taking an action.',
      );
    }
  }
  protected async openDeposit(value: OwnDeposit | StaffDeposit) {
    const item = this.current();
    if (this.busy() || this.pending() || !item) return;
    this.deposit.set(null);
    this.refunds.set(null);
    this.resetForm();
    this.pages.update((p) => ({ ...p, refunds: 1 }));
    await this.read((t, r) => this.readDeposit(item, value.id, t, r));
  }
  protected async changePage(delta: number) {
    if (this.busy() || this.pending()) return;
    this.page.update((n) => Math.max(1, n + delta));
    this.clearDetails();
    await this.reload();
  }
  protected async changeFilter() {
    if (this.busy() || this.pending()) return;
    this.page.set(1);
    this.clearDetails();
    await this.reload();
  }
  protected async sectionPage(kind: Section, delta: number) {
    if (this.busy() || this.pending()) return;
    this.pages.update((p) => ({ ...p, [kind]: Math.max(1, p[kind] + delta) }));
    await this.reload();
  }
  protected actions(): BookingAction[] {
    const item = this.current();
    if (!item) return [];
    if (this.staff)
      return [
        'responses',
        'deposits',
        ...(item.status === 'Confirmed' ? ['extensions' as const] : []),
      ];
    return ['PendingReview', 'Confirmed'].includes(item.status) ? ['cancel'] : [];
  }
  protected choose(action: BookingAction) {
    if (this.busy() || this.blocked() || this.pending()) return;
    if (
      action === 'submit'
        ? this.staff
        : action === 'refunds'
          ? !(this.staff && this.deposit())
          : !this.actions().includes(action)
    )
      return;
    this.resetForm();
    this.action.set(action);
    for (const field of this.fields())
      this.form.addControl(field.key, new FormControl('', { nonNullable: true }));
    if (action === 'submit') {
      const unitId = this.route.snapshot.queryParamMap.get('unitId');
      if (unitId) this.form.controls['unitId'].setValue(unitId);
    }
    this.error.set('');
    this.message.set('');
  }
  protected async submit() {
    const action = this.action();
    if (!action || this.busy() || this.blocked() || this.pending()) return;
    const values = this.form.getRawValue();
    if (
      this.fields().some(
        (f) =>
          (!f.optional && !values[f.key]?.trim()) ||
          (f.max && (values[f.key]?.length ?? 0) > f.max),
      )
    ) {
      this.error.set('Complete the required fields and check text lengths.');
      return;
    }
    try {
      await this.run(
        this.service.command(
          action,
          this.staff,
          this.current(),
          { ...values, responseKind: this.responseKind.value },
          this.deposit(),
        ),
      );
    } catch (e) {
      this.fail(e);
    }
  }
  protected async recover() {
    const op = this.pending();
    if (op && !this.busy()) await this.run(op);
  }
  private async run(op: BookingOperation) {
    if (this.busy() || !this.allowed()) return;
    const t = ++this.turn,
      r = this.session.revision(),
      oldDeposit = this.deposit()?.id;
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    this.pending.set(op);
    let saved = false;
    try {
      const receipt = await this.service.execute(op);
      saved = true;
      if (!this.valid(t, r)) return;
      this.pending.set(null);
      this.clearDetails();
      this.message.set(
        receipt.isReplay
          ? 'The previous operation was confirmed without repeating its effect.'
          : 'Operation recorded. Review booking state, response decision and saved history.',
      );
      const list = await this.service.list(this.staff, this.page(), this.status.value);
      if (!this.valid(t, r)) return;
      this.list.set(list);
      const bookingId = op.action === 'submit' ? receipt.id : op.bookingId!;
      const item = await this.service.detail(this.staff, bookingId, op.customerId);
      await this.load(
        item,
        t,
        r,
        op.action === 'deposits' ? receipt.id : (op.depositId ?? oldDeposit),
      );
    } catch (e) {
      if (!this.valid(t, r)) return;
      if (e instanceof ApiFailure && ['forbidden', 'unauthorized', 'not-found'].includes(e.kind))
        this.fail(e);
      else if (saved) {
        this.pending.set(null);
        this.blocked.set(true);
        this.error.set(
          'The operation was saved, but the display could not refresh. Reload before a new operation.',
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
      if (t === this.turn) this.busy.set(false);
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
