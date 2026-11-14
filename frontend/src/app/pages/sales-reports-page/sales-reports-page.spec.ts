import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { Signal } from '@angular/core';
import { vi } from 'vitest';
import { SalesReportsPage } from './sales-reports-page';
import { SalesService } from '../../services/sales.service';
import { SessionContext } from '../../services/session-context.service';
import { ApiFailure } from '../../models/api-failure';
import { ActivityReport, CurrencyTotal, ACTIVITY_REPORT_FIELDS } from '../../models/sales';
import { CrmPage } from '../../models/crm';
interface State {
  reload(): Promise<void>;
  activity: Signal<ActivityReport | null>;
  totals: Signal<CrmPage<CurrencyTotal> | null>;
}
const id = '11111111-1111-1111-1111-111111111111';
describe('AC-05/13 report scopes and ledger failure privacy', () => {
  async function setup(manager: boolean) {
    const activity = Object.fromEntries(
      Object.keys(ACTIVITY_REPORT_FIELDS).map((key) => [
        key,
        key === 'scope'
          ? manager
            ? 'AllCustomersAndCatalog'
            : 'AssignedCustomers'
          : key === 'observedAtUtc'
            ? '2026-11-08T00:00:00Z'
            : '9007199254740993',
      ]),
    );
    const service = {
      activity: vi.fn().mockResolvedValue(activity),
      commissions: vi.fn().mockResolvedValue({
        items: [
          {
            currencyCode: 'EGP',
            dealCount: '2',
            collectionCount: '0',
            entitledAmount: '19999999999999999.98',
            collectedAmount: '0.00',
            remainingAmount: '19999999999999999.98',
          },
        ],
        page: 1,
        pageSize: 20,
        total: 1,
      }),
    };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'reports', component: SalesReportsPage, data: { manager } }]),
        { provide: SalesService, useValue: service },
      ],
    });
    const session = TestBed.inject(SessionContext);
    session.accept(session.revision(), {
      accountId: id,
      email: 'report@brokerhub.test',
      roles: [manager ? 'SalesManager' : 'SalesEmployee'],
    });
    const harness = await RouterTestingHarness.create('/reports');
    await harness.fixture.whenStable();
    return {
      state: harness.routeDebugElement!.componentInstance as State,
      service,
      session,
      harness,
    };
  }
  it('Employee only requests current activity and never requests or renders commission totals', async () => {
    const { state, service, harness } = await setup(false);
    expect(service.commissions).not.toHaveBeenCalled();
    expect(state.activity()?.customerCount).toBe('9007199254740993');
    expect(state.totals()).toBeNull();
    expect(harness.routeNativeElement!.textContent).not.toContain('Filter saved commission');
  });
  it('a reconciliaton409 removes previous totals rather than presenting a zero balance', async () => {
    const { state, service, harness } = await setup(true);
    expect(state.totals()?.items[0].entitledAmount).toBe('19999999999999999.98');
    service.commissions.mockRejectedValueOnce(new ApiFailure('conflict'));
    await state.reload();
    harness.fixture.detectChanges();
    expect(state.totals()).toBeNull();
    expect(state.activity()).toBeNull();
    expect(harness.routeNativeElement!.textContent).toContain('this is not a zero balance');
  });
  it('late report results cannot restore another identity financial data', async () => {
    const { state, service, session, harness } = await setup(true);
    let release!: (value: unknown) => void;
    service.commissions.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          release = resolve;
        }),
    );
    const reading = state.reload();
    session.clear('guest');
    harness.fixture.detectChanges();
    release({
      items: [{ currencyCode: 'USD', entitledAmount: '1.00' }],
      total: 1,
      page: 1,
      pageSize: 20,
    });
    await reading;
    await harness.fixture.whenStable();
    expect(state.activity()).toBeNull();
    expect(state.totals()).toBeNull();
  });
});
