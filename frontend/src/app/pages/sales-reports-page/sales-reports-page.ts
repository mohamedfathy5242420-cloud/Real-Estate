import { Component, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormControl, FormRecord, ReactiveFormsModule } from '@angular/forms';
import { PageHeading } from '../../components/page-heading/page-heading';
import { CrmPager } from '../../components/crm-pager/crm-pager';
import { SalesFields } from '../../components/sales-fields/sales-fields';
import { SalesService } from '../../services/sales.service';
import { SessionContext } from '../../services/session-context.service';
import { CrmPage } from '../../models/crm';
import { ActivityReport, CurrencyTotal } from '../../models/sales';
import { crmError } from '../../models/crm-ui';

function filters(keys: readonly string[]) {
  return new FormRecord(
    Object.fromEntries(keys.map((key) => [key, new FormControl('', { nonNullable: true })])),
  );
}
@Component({
  selector: 'app-sales-reports-page',
  imports: [RouterLink, ReactiveFormsModule, PageHeading, CrmPager, SalesFields],
  templateUrl: './sales-reports-page.html',
  styleUrls: ['../account-page/account-page.css', '../crm-page.css'],
})
export class SalesReportsPage {
  private readonly service = inject(SalesService);
  protected readonly session = inject(SessionContext);
  protected readonly manager = inject(ActivatedRoute).snapshot.data['manager'] === true;
  protected readonly allowed = () =>
    this.session.account()?.roles.includes(this.manager ? 'SalesManager' : 'SalesEmployee') ??
    false;
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly activity = signal<ActivityReport | null>(null);
  protected readonly totals = signal<CrmPage<CurrencyTotal> | null>(null);
  protected readonly page = signal(1);
  protected readonly activityFilters = filters(['fromUtcDate', 'throughUtcDate']);
  protected readonly commissionFilters = filters([
    'customerId',
    'projectId',
    'developerId',
    'currencyCode',
    'saleLocalFrom',
    'saleLocalThrough',
  ]);
  protected readonly fields = [
    ['currencyCode', 'Currency', 'text'],
    ['saleLocalFrom', 'From saved local sale day', 'date'],
    ['saleLocalThrough', 'To saved local sale day', 'date'],
    ['customerId', 'Customer reference', 'text'],
    ['projectId', 'Project reference', 'text'],
    ['developerId', 'Developer reference', 'text'],
  ] as const;
  private turn = 0;
  private revision = this.session.revision();
  constructor() {
    void this.reload();
    effect(() => {
      const r = this.session.revision();
      if (r !== this.revision) {
        this.revision = r;
        this.turn++;
        this.activity.set(null);
        this.totals.set(null);
        this.error.set('');
        this.busy.set(false);
      }
    });
  }
  protected async reload() {
    if (this.busy() || !this.allowed()) return;
    const t = ++this.turn,
      r = this.session.revision();
    this.busy.set(true);
    this.error.set('');
    this.activity.set(null);
    this.totals.set(null);
    try {
      const [activity, totals] = await Promise.all([
        this.service.activity(this.activityFilters.getRawValue()),
        this.manager
          ? this.service.commissions(this.commissionFilters.getRawValue(), this.page())
          : Promise.resolve(null),
      ]);
      if (t !== this.turn || r !== this.session.revision()) return;
      this.activity.set(activity);
      this.totals.set(totals);
    } catch (e) {
      if (t === this.turn && r === this.session.revision()) {
        this.activity.set(null);
        this.totals.set(null);
        this.error.set(crmError(e));
      }
    } finally {
      if (t === this.turn) this.busy.set(false);
    }
  }
  protected async apply() {
    if (this.busy()) return;
    this.page.set(1);
    await this.reload();
  }
  protected async move(delta: number) {
    if (this.busy()) return;
    this.page.update((n) => Math.max(1, n + delta));
    await this.reload();
  }
}
