import { Component, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormControl, FormRecord, ReactiveFormsModule } from '@angular/forms';
import { PageHeading } from '../../components/page-heading/page-heading';
import { CrmPager } from '../../components/crm-pager/crm-pager';
import { SalesFields } from '../../components/sales-fields/sales-fields';
import { DisplayCurrencyPipe } from '../../pipes/display-format.pipe';
import { SalesService } from '../../services/sales.service';
import { SessionContext } from '../../services/session-context.service';
import { ApiFailure } from '../../models/api-failure';
import { crmError } from '../../models/crm-ui';
import { CrmPage, CrmCustomer, Lead } from '../../models/crm';
import { StaffBooking } from '../../models/booking';
import {
  ManagerDeal,
  DealSummary,
  SaleUnit,
  CommissionSnapshot,
  CollectionSummary,
  CommissionCollection,
  SaleOperation,
} from '../../models/sales';
import { formatProjectTime } from '../../models/project-time';

type Entry = { key: string; label: string; type?: string; max: number; optional?: boolean };
const evidence: Entry[] = [
  { key: 'source', label: 'External sale source', max: 200 },
  { key: 'reference', label: 'External sale reference', max: 500 },
];
function form(keys: readonly string[]) {
  return new FormRecord(
    Object.fromEntries(keys.map((k) => [k, new FormControl('', { nonNullable: true })])),
  );
}
@Component({
  selector: 'app-sales-page',
  imports: [
    RouterLink,
    ReactiveFormsModule,
    PageHeading,
    CrmPager,
    SalesFields,
    DisplayCurrencyPipe,
  ],
  templateUrl: './sales-page.html',
  styleUrls: ['../account-page/account-page.css', '../crm-page.css'],
})
export class SalesPage {
  private readonly service = inject(SalesService);
  protected readonly session = inject(SessionContext);
  protected readonly allowed = () =>
    this.session.account()?.roles.includes('SalesManager') ?? false;
  protected readonly busy = signal(false);
  protected readonly blocked = signal(false);
  protected readonly error = signal('');
  protected readonly message = signal('');
  protected readonly pending = signal<SaleOperation | null>(null);
  protected readonly list = signal<CrmPage<DealSummary> | null>(null);
  protected readonly page = signal(1);
  protected readonly current = signal<ManagerDeal | null>(null);
  protected readonly snapshot = signal<CommissionSnapshot | null>(null);
  protected readonly summary = signal<CollectionSummary | null>(null);
  protected readonly collections = signal<CrmPage<CommissionCollection> | null>(null);
  protected readonly collectionPage = signal(1);
  protected readonly collectMode = signal(false);
  protected readonly creating = signal(false);
  protected readonly customers = signal<CrmPage<CrmCustomer> | null>(null);
  protected readonly customer = signal<CrmCustomer | null>(null);
  protected readonly leads = signal<CrmPage<Lead> | null>(null);
  protected readonly lead = signal<Lead | null>(null);
  protected readonly units = signal<CrmPage<SaleUnit> | null>(null);
  protected readonly unit = signal<SaleUnit | null>(null);
  protected readonly bookings = signal<CrmPage<StaffBooking> | null>(null);
  protected readonly linked = signal<StaffBooking | null>(null);
  protected readonly linkedId = new FormControl('', { nonNullable: true });
  protected readonly customerPage = signal(1);
  protected readonly leadPage = signal(1);
  protected readonly unitPage = signal(1);
  protected readonly bookingPage = signal(1);
  protected readonly unitSearch = new FormControl('', { nonNullable: true });
  protected readonly filters = form([
    'customerId',
    'projectId',
    'unitId',
    'developerId',
    'agreementId',
    'currencyCode',
    'saleLocalFrom',
    'saleLocalThrough',
  ]);
  protected readonly filterFields = [
    ['customerId', 'Customer reference'],
    ['projectId', 'Project reference'],
    ['unitId', 'Unit reference'],
    ['developerId', 'Developer reference'],
    ['agreementId', 'Agreement reference'],
    ['currencyCode', 'Currency'],
    ['saleLocalFrom', 'From local sale day'],
    ['saleLocalThrough', 'To local sale day'],
  ] as const;
  protected readonly saleFields: Entry[] = [
    { key: 'finalUnitPrice', label: 'Final unit sale price excluding fees and extras', max: 20 },
    { key: 'currencyCode', label: 'Actual sale currency (three letters)', max: 3 },
    {
      key: 'saleOccurredAtUtc',
      label: 'Actual external sale time',
      type: 'datetime-local',
      max: 40,
    },
    ...evidence,
    {
      key: 'selectionReason',
      label: 'Reason for choosing the original lead when multiple origins are eligible (optional)',
      max: 1000,
      optional: true,
    },
  ];
  protected readonly collectionFields: Entry[] = [
    { key: 'amount', label: 'Commission actually collected', max: 20 },
    {
      key: 'collectedAtUtc',
      label: 'Actual external collection time',
      type: 'datetime-local',
      max: 40,
    },
    { key: 'source', label: 'External collection source', max: 200 },
    { key: 'reference', label: 'External collection reference', max: 500 },
    { key: 'notes', label: 'Internal collection notes (optional)', max: 2000, optional: true },
  ];
  protected readonly saleForm = form(this.saleFields.map((x) => x.key));
  protected readonly collectionForm = form(this.collectionFields.map((x) => x.key));
  protected readonly deviceZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  private turn = 0;
  private revision = this.session.revision();
  private target: { customer: string; id: string } | null = null;
  constructor() {
    void this.reload();
    effect(() => {
      const r = this.session.revision();
      if (r !== this.revision) {
        this.revision = r;
        this.turn++;
        this.clearAll();
        this.error.set('');
        this.message.set('');
        this.busy.set(false);
      }
    });
  }
  protected date(value: string, zone: string) {
    return formatProjectTime(value, zone);
  }
  protected canWrite() {
    return !this.busy() && !this.pending() && !this.blocked();
  }
  private valid(t: number, r: number) {
    return t === this.turn && r === this.session.revision();
  }
  private clearDetails() {
    this.current.set(null);
    this.snapshot.set(null);
    this.summary.set(null);
    this.collections.set(null);
    this.collectMode.set(false);
  }
  private clearOrigin() {
    this.customer.set(null);
    this.leads.set(null);
    this.lead.set(null);
    this.units.set(null);
    this.unit.set(null);
    this.bookings.set(null);
    this.linked.set(null);
    this.linkedId.setValue('');
  }
  private clearAll() {
    this.clearDetails();
    this.clearOrigin();
    this.list.set(null);
    this.customers.set(null);
    this.pending.set(null);
    this.target = null;
    this.creating.set(false);
    this.blocked.set(false);
    this.saleForm.reset();
    this.collectionForm.reset();
  }
  private fail(e: unknown) {
    if (e instanceof ApiFailure && ['unauthorized', 'forbidden', 'not-found'].includes(e.kind))
      this.clearAll();
    else if (e instanceof ApiFailure && (e.kind === 'conflict' || e.outcomeUnknown))
      this.blocked.set(true);
    this.error.set(crmError(e));
  }
  private async read(work: (t: number, r: number) => Promise<void>) {
    if (this.busy() || !this.allowed()) return;
    const t = ++this.turn,
      r = this.session.revision();
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
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
    const target = this.target,
      customer = this.customer(),
      lead = this.lead(),
      unit = this.unit(),
      linked = this.linkedId.value;
    this.clearDetails();
    this.list.set(null);
    this.blocked.set(!!this.pending());
    await this.read(async (t, r) => {
      const list = await this.service.list(this.filters.getRawValue(), this.page());
      if (!this.valid(t, r)) return;
      this.list.set(list);
      if (target) await this.loadDeal(target.customer, target.id, t, r);
      else if (this.creating()) {
        const customers = await this.service.customers(this.customerPage());
        if (!this.valid(t, r)) return;
        this.customers.set(customers);
        if (customer) {
          const leads = await this.service.leads(customer.id, this.leadPage());
          if (!this.valid(t, r)) return;
          this.leads.set(leads);
          if (lead) {
            const latest = await this.service.lead(customer.id, lead.id),
              units = await this.service.units(
                customer.id,
                lead.id,
                this.unitPage(),
                this.unitSearch.value,
              );
            if (!this.valid(t, r)) return;
            this.lead.set(latest);
            this.units.set(units);
            if (unit) {
              const fresh = await this.service.unit(customer.id, lead.id, unit.id),
                bookings = await this.service.bookingCandidates(customer.id, this.bookingPage());
              if (!this.valid(t, r)) return;
              this.unit.set(fresh);
              this.bookings.set(bookings);
              if (linked) await this.loadLinked(linked, t, r);
            }
          }
        }
      }
    });
  }
  protected async filter() {
    if (!this.canWrite()) return;
    this.target = null;
    this.page.set(1);
    this.creating.set(false);
    this.clearOrigin();
    await this.reload();
  }
  protected async listPage(delta: number) {
    if (!this.canWrite()) return;
    this.target = null;
    this.page.update((n) => Math.max(1, n + delta));
    await this.reload();
  }
  protected async open(row: DealSummary) {
    if (!this.canWrite()) return;
    this.creating.set(false);
    this.clearOrigin();
    this.collectionPage.set(1);
    this.target = { customer: row.customerId, id: row.id };
    this.clearDetails();
    this.blocked.set(false);
    await this.read((t, r) => this.loadDeal(row.customerId, row.id, t, r));
  }
  private async loadDeal(customer: string, id: string, t: number, r: number) {
    this.clearDetails();
    const [detail, snapshot, summary, collections] = await Promise.all([
      this.service.detail(customer, id),
      this.service.snapshot(customer, id),
      this.service.summary(customer, id),
      this.service.collections(customer, id, this.collectionPage()),
    ]);
    const latest = await this.service.detail(customer, id);
    if (!this.valid(t, r)) return;
    if (
      snapshot.id !== id ||
      snapshot.customerId !== customer ||
      summary.dealId !== id ||
      summary.customerId !== customer
    )
      throw new ApiFailure('unknown');
    this.current.set(latest);
    this.snapshot.set(snapshot);
    this.summary.set(summary);
    this.collections.set(collections);
    this.target = { customer, id };
    if (detail.version !== latest.version) {
      this.blocked.set(true);
      this.error.set('The deal changed while reading. Reload and review it.');
    }
  }
  protected async start() {
    if (!this.canWrite()) return;
    this.clearDetails();
    this.clearOrigin();
    this.target = null;
    this.creating.set(true);
    this.customerPage.set(1);
    this.saleForm.reset();
    await this.read(async (t, r) => {
      const data = await this.service.customers();
      if (this.valid(t, r)) this.customers.set(data);
    });
  }
  protected async chooseCustomer(value: CrmCustomer) {
    if (!this.canWrite()) return;
    this.saleForm.reset();
    this.clearOrigin();
    this.customer.set(value);
    this.leadPage.set(1);
    await this.read(async (t, r) => {
      const leads = await this.service.leads(value.id);
      if (this.valid(t, r)) this.leads.set(leads);
    });
  }
  protected async chooseLead(value: Lead) {
    if (!this.canWrite()) return;
    this.saleForm.reset();
    const customer = this.customer();
    if (!customer) return;
    this.unit.set(null);
    this.bookings.set(null);
    this.linked.set(null);
    this.linkedId.setValue('');
    this.unitPage.set(1);
    this.unitSearch.setValue('');
    await this.read(async (t, r) => {
      const [lead, units] = await Promise.all([
        this.service.lead(customer.id, value.id),
        this.service.units(customer.id, value.id),
      ]);
      if (!this.valid(t, r)) return;
      this.lead.set(lead);
      this.units.set(units);
    });
  }
  protected async chooseUnit(value: SaleUnit) {
    if (!this.canWrite()) return;
    this.saleForm.reset();
    const customer = this.customer(),
      lead = this.lead();
    if (!customer || !lead) return;
    this.linked.set(null);
    this.linkedId.setValue('');
    this.bookingPage.set(1);
    await this.read(async (t, r) => {
      const [unit, currentLead, bookings] = await Promise.all([
        this.service.unit(customer.id, lead.id, value.id),
        this.service.lead(customer.id, lead.id),
        this.service.bookingCandidates(customer.id),
      ]);
      if (!this.valid(t, r)) return;
      this.unit.set(unit);
      this.lead.set(currentLead);
      this.bookings.set(bookings);
    });
  }
  protected matchingBookings() {
    return (
      this.bookings()?.items.filter(
        (b) => b.unitId === this.unit()?.id && b.leadId === this.lead()?.id,
      ) ?? []
    );
  }
  protected linkListed() {
    return this.matchingBookings().some((item) => item.id === this.linkedId.value);
  }
  protected discardDraft() {
    if (this.busy() || this.pending()) return;
    this.clearOrigin();
    this.saleForm.reset();
    this.creating.set(false);
    this.customers.set(null);
    this.blocked.set(false);
    this.error.set('');
    this.message.set(
      'The draft was cleared. Start a new preparation and review its inputs before approval.',
    );
  }
  private async loadLinked(id: string, t: number, r: number) {
    const customer = this.customer(),
      unit = this.unit(),
      lead = this.lead();
    if (!customer || !unit || !lead) return;
    const booking = await this.service.booking(customer.id, id);
    if (!this.valid(t, r)) return;
    this.linked.set(booking);
    if (booking.unitId !== unit.id || booking.leadId !== lead.id || booking.status !== 'Confirmed')
      throw new ApiFailure('conflict');
  }
  protected async chooseBooking() {
    if (!this.canWrite()) return;
    const id = this.linkedId.value;
    this.linked.set(null);
    if (id) await this.read((t, r) => this.loadLinked(id, t, r));
  }
  protected async originPage(kind: 'customers' | 'leads' | 'units' | 'bookings', delta: number) {
    if (!this.canWrite()) return;
    const target = {
      customers: this.customerPage,
      leads: this.leadPage,
      units: this.unitPage,
      bookings: this.bookingPage,
    }[kind];
    target.update((n) => Math.max(1, n + delta));
    await this.reload();
  }
  protected async searchUnits() {
    if (!this.canWrite()) return;
    this.unitPage.set(1);
    await this.reload();
  }
  protected async collectionsPage(delta: number) {
    if (!this.canWrite()) return;
    this.collectionPage.update((n) => Math.max(1, n + delta));
    await this.reload();
  }
  private fieldsValid(entries: Entry[], values: Record<string, string>) {
    return !entries.some(
      (f) => (!f.optional && !values[f.key]?.trim()) || (values[f.key]?.length ?? 0) > f.max,
    );
  }
  protected async adopt() {
    if (!this.canWrite()) return;
    const customer = this.customer(),
      lead = this.lead(),
      unit = this.unit(),
      values = this.saleForm.getRawValue();
    if (!customer || !lead || !unit || !this.fieldsValid(this.saleFields, values)) {
      this.error.set('Select the customer, lead and unit, and enter actual sale details.');
      return;
    }
    if (this.linkedId.value !== this.linked()?.id && this.linkedId.value) {
      this.error.set('Review the explicitly selected booking before approval.');
      return;
    }
    try {
      await this.run(this.service.adoption(customer.id, lead, unit, this.linked(), values));
    } catch (e) {
      this.fail(e);
    }
  }
  protected beginCollection() {
    if (!this.canWrite() || !this.current() || !this.snapshot()) return;
    this.collectionForm.reset();
    this.collectMode.set(true);
    this.message.set('');
  }
  protected async collect() {
    if (!this.canWrite()) return;
    const deal = this.current(),
      snapshot = this.snapshot(),
      values = this.collectionForm.getRawValue();
    if (!deal || !snapshot || !this.fieldsValid(this.collectionFields, values)) {
      this.error.set('Enter the actual collection amount, time, source and reference.');
      return;
    }
    try {
      await this.run(this.service.collection(deal, snapshot, values));
    } catch (e) {
      this.fail(e);
    }
  }
  protected async recover() {
    const op = this.pending();
    if (op && !this.busy()) await this.run(op);
  }
  private async run(op: SaleOperation) {
    if (this.busy() || !this.allowed()) return;
    const t = ++this.turn,
      r = this.session.revision();
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
      this.blocked.set(false);
      this.creating.set(false);
      this.clearOrigin();
      this.collectionPage.set(1);
      const id = op.action === 'adopt' ? receipt.id : op.dealId!;
      await this.loadDeal(op.customerId, id, t, r);
      const list = await this.service.list(this.filters.getRawValue(), this.page());
      if (!this.valid(t, r)) return;
      this.list.set(list);
      this.message.set(
        receipt.isReplay
          ? 'The previous operation was confirmed without repeating its effect.'
          : 'The fact was saved. Review the deal, commission snapshot and recorded collection.',
      );
    } catch (e) {
      if (!this.valid(t, r)) return;
      if (e instanceof ApiFailure && ['unauthorized', 'forbidden', 'not-found'].includes(e.kind))
        this.fail(e);
      else if (saved) {
        this.pending.set(null);
        this.clearDetails();
        this.blocked.set(true);
        this.error.set(
          'The operation was saved, but the display could not refresh. Reload before another action.',
        );
      } else if (e instanceof ApiFailure && e.outcomeUnknown) {
        this.blocked.set(true);
        this.error.set(
          'The result is uncertain and may have been saved. Review the state or confirm the same saved operation; do not submit a new draft.',
        );
      } else {
        this.pending.set(null);
        this.fail(e);
      }
    } finally {
      if (t === this.turn) this.busy.set(false);
    }
  }
}
