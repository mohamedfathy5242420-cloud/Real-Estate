import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { vi } from 'vitest';
import { Signal } from '@angular/core';
import { FormRecord, FormControl } from '@angular/forms';
import { BookingsPage } from './bookings-page';
import { BookingService } from '../../services/booking.service';
import { SessionContext } from '../../services/session-context.service';
import { ApiFailure } from '../../models/api-failure';
import {
  OwnBooking,
  StaffBooking,
  OwnDeposit,
  BookingAction,
  BookingOperation,
} from '../../models/booking';
const id = '11111111-1111-1111-1111-111111111111',
  factId = '22222222-2222-2222-2222-222222222222';
const root: OwnBooking = {
  id,
  unitId: id,
  projectId: id,
  status: 'Confirmed',
  requestedAtUtc: '2026-11-07T00:00:00Z',
  responseDueAtUtc: '2026-11-10T00:00:00Z',
  requestReason: null,
  version: '9223372036854775806',
  rejectionReason: null,
  confirmedAtUtc: '2026-11-07T00:01:00Z',
  confirmedPrice: '9999999999999999.99',
  confirmedCurrencyCode: 'EGP',
  reservationExpiresAtUtc: '2026-11-11T00:00:00Z',
  cancelledAtUtc: null,
  cancellationReason: null,
  completedAtUtc: null,
};
const deposit: OwnDeposit = {
  id: factId,
  bookingId: id,
  amount: '9999999999999999.99',
  currencyCode: 'EGP',
  paidAtUtc: root.confirmedAtUtc!,
  recordedAtUtc: root.confirmedAtUtc!,
  version: '1',
  recordedRefundAmount: '0.00',
  recordedUnrefundedAmount: '9999999999999999.99',
  recordedRefundStatus: 'None',
};
interface State {
  open(item: OwnBooking | StaffBooking): Promise<void>;
  choose(action: BookingAction): void;
  submit(): Promise<void>;
  reload(): Promise<void>;
  recover(): Promise<void>;
  openDeposit(item: OwnDeposit): Promise<void>;
  actions(): BookingAction[];
  blocked: Signal<boolean>;
  form: FormRecord<FormControl<string>>;
  current: Signal<OwnBooking | null>;
  deposit: Signal<OwnDeposit | null>;
  pending: Signal<BookingOperation | null>;
}
describe('AC-04/11/12 booking UI fact receipts and private scope fences', () => {
  async function setup(staff = false) {
    const item = staff ? { ...root, customerId: id, timeZoneId: 'Africa/Cairo' } : root;
    const empty = { items: [], page: 1, pageSize: 20, total: 0 };
    const service = {
      list: vi.fn().mockResolvedValue({ items: [item], page: 1, pageSize: 20, total: 1 }),
      detail: vi.fn().mockResolvedValue(item),
      history: vi.fn().mockResolvedValue(empty),
      deposits: vi.fn().mockResolvedValue(empty),
      deposit: vi.fn().mockResolvedValue(deposit),
      refunds: vi.fn().mockResolvedValue(empty),
      responses: vi.fn().mockResolvedValue(empty),
      extensions: vi.fn().mockResolvedValue(empty),
      execute: vi.fn(),
      command: vi.fn((action, staff, _current, fields) =>
        Object.freeze({
          action,
          staff,
          bookingId: id,
          ...(staff ? { customerId: id } : {}),
          key: 'reviewed-key',
          revision: 0,
          body: Object.freeze({ ...fields, version: root.version }),
        }),
      ),
    };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'bookings', component: BookingsPage, data: { staff } }]),
        { provide: BookingService, useValue: service },
      ],
    });
    const session = TestBed.inject(SessionContext);
    session.accept(session.revision(), {
      accountId: id,
      email: 'own@brokerhub.test',
      roles: [staff ? 'SalesEmployee' : 'Customer'],
    });
    const harness = await RouterTestingHarness.create('/bookings');
    await harness.fixture.whenStable();
    const state = harness.routeDebugElement!.componentInstance as State;
    await state.open(item as OwnBooking | StaffBooking);
    return { state, service, session, harness };
  }
  it('reloads original booking/version rather than treating a financial fact receipt as the booking', async () => {
    const { state, service } = await setup(true);
    state.choose('deposits');
    for (const [key, value] of Object.entries({
      amount: '9999999999999999.99',
      currencyCode: 'EGP',
      paidAtUtc: '2026-11-07T12:00',
      source: 'Source',
      reference: 'Reference',
    }))
      state.form.controls[key].setValue(value);
    service.execute.mockResolvedValue({ id: factId, version: '1', isReplay: false });
    await state.submit();
    expect(state.current()?.id).toBe(id);
    expect(state.current()?.version).toBe(root.version);
    expect(state.deposit()?.id).toBe(factId);
    expect(service.detail.mock.calls.every((call) => call[1] === id)).toBe(true);
    expect(service.deposit).toHaveBeenCalledWith(true, id, factId, id);
  });
  it('only explicitly recovers the original body/key after edits and reload, never a new draft', async () => {
    const { state, service } = await setup();
    state.choose('cancel');
    state.form.controls['reason'].setValue('Original reason');
    service.execute
      .mockRejectedValueOnce(new ApiFailure('network', [], true))
      .mockResolvedValueOnce({ id, version: '2', isReplay: true });
    await state.submit();
    const original = state.pending();
    state.form.controls['reason'].setValue('Different draft');
    await state.submit();
    expect(service.execute).toHaveBeenCalledTimes(1);
    await state.reload();
    expect(state.pending()).toBe(original);
    await state.recover();
    expect(service.execute.mock.calls[1][0]).toBe(original);
    expect(service.execute.mock.calls[1][0].body.reason).toBe('Original reason');
  });
  it('adopts effective expiry during deposit read and blocks a concurrently changed root version', async () => {
    const { state, service } = await setup(true);
    const expired = { ...root, customerId: id, timeZoneId: 'Africa/Cairo', status: 'Expired' };
    service.detail.mockResolvedValue(expired);
    await state.openDeposit(deposit);
    expect(state.current()?.status).toBe('Expired');
    expect(state.actions()).not.toContain('extensions');
    expect(state.blocked()).toBe(false);
    service.detail.mockResolvedValue({ ...expired, version: '9223372036854775807' });
    await state.openDeposit(deposit);
    expect(state.current()?.version).toBe('9223372036854775807');
    expect(state.blocked()).toBe(true);
    expect(state.deposit()?.id).toBe(factId);
  });
  it('removes the selected root/financial facts when identity changes during a held detail read', async () => {
    const { state, service, session, harness } = await setup();
    let release!: (value: OwnBooking) => void;
    service.detail.mockImplementationOnce(
      () =>
        new Promise<OwnBooking>((resolve) => {
          release = resolve;
        }),
    );
    const reading = state.open(root);
    session.clear('guest');
    harness.fixture.detectChanges();
    release(root);
    await reading;
    await harness.fixture.whenStable();
    expect(state.current()).toBeNull();
    expect(state.deposit()).toBeNull();
    expect(harness.routeNativeElement!.textContent).not.toContain(id);
  });
});
