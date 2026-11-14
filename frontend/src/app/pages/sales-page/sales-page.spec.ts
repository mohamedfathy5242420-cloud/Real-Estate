import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { Signal } from '@angular/core';
import { FormControl, FormRecord } from '@angular/forms';
import { vi } from 'vitest';
import { SalesPage } from './sales-page';
import { SalesService } from '../../services/sales.service';
import { SessionContext } from '../../services/session-context.service';
import { ApiFailure } from '../../models/api-failure';
import { ManagerDeal, DealSummary, CommissionSnapshot, SaleOperation } from '../../models/sales';
const id = '11111111-1111-1111-1111-111111111111',
  fact = '22222222-2222-2222-2222-222222222222';
const root = {
  id,
  customerId: id,
  unitId: id,
  projectId: id,
  leadId: id,
  saleOccurredAtUtc: '2026-11-08T00:00:00Z',
  saleLocalDate: '2026-11-08',
  openingTimeZoneId: 'Africa/Cairo',
  finalUnitPrice: '100.25',
  currencyCode: 'EGP',
  adoptedAtUtc: '2026-11-08T00:00:01Z',
  adoptedByAccountId: id,
  bookingId: null,
  version: '9223372036854775807',
} satisfies ManagerDeal;
interface State {
  open(row: DealSummary): Promise<void>;
  beginCollection(): void;
  collect(): Promise<void>;
  recover(): Promise<void>;
  reload(): Promise<void>;
  collectionForm: FormRecord<FormControl<string>>;
  current: Signal<ManagerDeal | null>;
  snapshot: Signal<CommissionSnapshot | null>;
  pending: Signal<SaleOperation | null>;
}
describe('AC-13 current sales UI receipts and private identity fences', () => {
  async function setup() {
    const empty = { items: [], page: 1, pageSize: 20, total: 0 },
      snapshot = {
        id,
        customerId: id,
        currencyCode: 'EGP',
        amount: '2.01',
        openingTimeZoneId: 'Africa/Cairo',
        source: 'private-original-source',
      };
    const service = {
      list: vi.fn().mockResolvedValue({ ...empty, items: [root], total: 1 }),
      detail: vi.fn().mockResolvedValue(root),
      snapshot: vi.fn().mockResolvedValue(snapshot),
      summary: vi.fn().mockResolvedValue({
        dealId: id,
        customerId: id,
        currencyCode: 'EGP',
        entitledAmount: '2.01',
        collectedAmount: '0.00',
        remainingAmount: '2.01',
        collectionCount: 0,
      }),
      collections: vi.fn().mockResolvedValue(empty),
      execute: vi.fn(),
      collection: vi.fn((_deal, _snapshot, fields) =>
        Object.freeze({
          action: 'collect',
          customerId: id,
          dealId: id,
          key: 'original-key',
          revision: 0,
          body: Object.freeze({ ...fields, version: root.version, currencyCode: 'EGP' }),
        }),
      ),
    };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'sales', component: SalesPage }]),
        { provide: SalesService, useValue: service },
      ],
    });
    const session = TestBed.inject(SessionContext);
    session.accept(session.revision(), {
      accountId: id,
      email: 'manager@brokerhub.test',
      roles: ['SalesManager'],
    });
    const harness = await RouterTestingHarness.create('/sales');
    await harness.fixture.whenStable();
    const state = harness.routeDebugElement!.componentInstance as State;
    await state.open(root as unknown as DealSummary);
    return { state, service, session, harness };
  }
  function fill(state: State) {
    state.beginCollection();
    for (const [k, v] of Object.entries({
      amount: '0.01',
      collectedAtUtc: '2026-11-08T12:00:00.123',
      source: 'Source',
      reference: 'Reference',
      notes: '',
    }))
      state.collectionForm.controls[k].setValue(v);
  }
  it('reloads original Deal after an immutable collection receipt without treating its ID/version as the root', async () => {
    const { state, service } = await setup();
    fill(state);
    service.execute.mockResolvedValue({ id: fact, version: '1', isReplay: false });
    await state.collect();
    expect(state.current()?.id).toBe(id);
    expect(state.current()?.version).toBe(root.version);
    expect(service.detail.mock.calls.every((call) => call[1] === id)).toBe(true);
    expect(service.summary).toHaveBeenLastCalledWith(id, id);
  });
  it('recovers only the original primitive collection after edits/reload, with no blind resubmission', async () => {
    const { state, service, harness } = await setup();
    fill(state);
    service.execute
      .mockRejectedValueOnce(new ApiFailure('network', [], true))
      .mockResolvedValueOnce({ id: fact, version: '1', isReplay: true });
    await state.collect();
    const original = state.pending();
    state.collectionForm.controls['amount'].setValue('2.01');
    await state.collect();
    expect(service.execute).toHaveBeenCalledTimes(1);
    await state.reload();
    expect(state.pending()).toBe(original);
    await state.recover();
    expect(service.execute.mock.calls[1][0]).toBe(original);
    expect(original?.body['amount']).toBe('0.01');
    harness.fixture.detectChanges();
    const nextCollection = Array.from(
      harness.routeNativeElement!.querySelectorAll<HTMLButtonElement>('button'),
    ).find((button) => button.textContent?.includes('Record external commission collection'));
    expect(nextCollection?.disabled).toBe(false);
  });
  it('clears private snapshot when identity changes during a held financial summary read', async () => {
    const { state, service, session, harness } = await setup();
    let release!: (value: object) => void;
    service.summary.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          release = resolve;
        }),
    );
    const reading = state.open(root as unknown as DealSummary);
    session.clear('guest');
    harness.fixture.detectChanges();
    release({ dealId: id, customerId: id });
    await reading;
    await harness.fixture.whenStable();
    expect(state.current()).toBeNull();
    expect(state.snapshot()).toBeNull();
    expect(harness.routeNativeElement!.textContent).not.toContain('private-original-source');
  });
  it('removes saved financial state when current scope fails after a committed write', async () => {
    const { state, service } = await setup();
    fill(state);
    service.execute.mockResolvedValue({ id: fact, version: '1', isReplay: false });
    service.detail.mockRejectedValueOnce(new ApiFailure('not-found'));
    await state.collect();
    expect(state.current()).toBeNull();
    expect(state.snapshot()).toBeNull();
    expect(state.pending()).toBeNull();
  });
});
