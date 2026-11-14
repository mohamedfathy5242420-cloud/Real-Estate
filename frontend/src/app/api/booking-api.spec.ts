import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { BookingApi } from './booking-api';
import { ApiTransport } from './api-transport';
import {
  BookingOperation,
  OWN_BOOKING_KEYS,
  OWN_BOOKING_EVENT_KEYS,
  OWN_DEPOSIT_KEYS,
  OWN_REFUND_KEYS,
} from '../models/booking';
const id = '11111111-1111-1111-1111-111111111111',
  factId = '22222222-2222-2222-2222-222222222222';
const root = {
  id,
  unitId: id,
  projectId: id,
  status: 'Confirmed',
  requestedAtUtc: '2026-11-07T00:00:00Z',
  responseDueAtUtc: '2026-11-10T00:00:00Z',
  requestReason: null,
  version: '9223372036854775806',
  rejectionReason: null,
  confirmedAtUtc: '2026-11-07T00:00:01Z',
  confirmedPrice: '9999999999999999.99',
  confirmedCurrencyCode: 'EGP',
  reservationExpiresAtUtc: '2026-11-11T00:00:00Z',
  cancelledAtUtc: null,
  cancellationReason: null,
  completedAtUtc: null,
};
describe('AC-04/10/11/12 booking projections and immutable fact receipts', () => {
  let api: BookingApi;
  const transport = { get: vi.fn(), post: vi.fn() };
  beforeEach(() => {
    vi.resetAllMocks();
    TestBed.configureTestingModule({ providers: [{ provide: ApiTransport, useValue: transport }] });
    api = TestBed.inject(BookingApi);
  });
  it('keeps all16 safe own fields and exact quoted price/version, excluding staff metadata', async () => {
    transport.get.mockResolvedValue({
      ...root,
      customerId: factId,
      leadId: factId,
      source: 'private',
      actorAccountId: factId,
    });
    expect(await api.detail(false, id)).toEqual(root);
    expect(OWN_BOOKING_KEYS).toHaveLength(16);
    expect(transport.get).toHaveBeenCalledWith('/api/bookings/' + id);
    transport.get.mockResolvedValue({ ...root, confirmedPrice: 9999999999999999 });
    await expect(api.detail(false, id)).rejects.toMatchObject({ kind: 'unknown' });
  });
  it('uses12 own history fields without private actors or fact references', async () => {
    const history = {
      id,
      bookingId: id,
      bookingVersion: root.version,
      kind: 'Confirmed',
      atUtc: root.confirmedAtUtc,
      status: 'Confirmed',
      reasonCode: null,
      confirmedPrice: root.confirmedPrice,
      confirmedCurrencyCode: 'EGP',
      reservationExpiresAtUtc: root.reservationExpiresAtUtc,
      cancelledFromStatus: null,
      cancellationReason: null,
    };
    transport.get.mockResolvedValue({
      items: [{ ...history, actorAccountId: factId, developerResponseId: factId }],
      page: 1,
      pageSize: 20,
      total: 1,
    });
    expect((await api.history(false, id)).items[0]).toEqual(history);
    expect(OWN_BOOKING_EVENT_KEYS).toHaveLength(12);
  });
  it('reads own deposit10 and refund8 safely while preserving actual principal summaries', async () => {
    const deposit = {
      id: factId,
      bookingId: id,
      amount: '9999999999999999.99',
      currencyCode: 'EGP',
      paidAtUtc: root.requestedAtUtc,
      recordedAtUtc: root.confirmedAtUtc,
      version: '1',
      recordedRefundAmount: '0.00',
      recordedUnrefundedAmount: '9999999999999999.99',
      recordedRefundStatus: 'None',
    };
    transport.get.mockResolvedValue(deposit);
    expect(await api.deposit(false, id, factId)).toEqual(deposit);
    expect(OWN_DEPOSIT_KEYS).toHaveLength(10);
    const refund = {
      id,
      bookingId: id,
      depositId: factId,
      amount: '0.01',
      currencyCode: 'EGP',
      returnedAtUtc: root.confirmedAtUtc,
      recordedAtUtc: root.confirmedAtUtc,
      version: '1',
    };
    transport.get.mockResolvedValue({
      items: [{ ...refund, source: 'private', notes: 'private', requestedVersion: root.version }],
      page: 1,
      pageSize: 20,
      total: 1,
    });
    expect((await api.refunds(false, id, factId)).items[0]).toEqual(refund);
    expect(OWN_REFUND_KEYS).toHaveLength(8);
  });
  it('targets the original scoped booking/deposit and treats malformed fact success as unknown', async () => {
    const op: BookingOperation = {
      action: 'refunds',
      staff: true,
      bookingId: id,
      customerId: factId,
      depositId: factId,
      key: 'reviewed-key',
      revision: 0,
      body: { version: root.version, amount: '0.01', currencyCode: 'EGP' },
    };
    transport.post.mockResolvedValue({ id: factId, version: '1', isReplay: true });
    expect(await api.execute(op)).toEqual({ id: factId, version: '1', isReplay: true });
    expect(transport.post).toHaveBeenCalledExactlyOnceWith(
      `/api/crm/customers/${factId}/bookings/${id}/deposits/${factId}/refunds`,
      op.body,
      true,
      op.key,
    );
    transport.post.mockResolvedValue({ id: factId, version: 1, isReplay: false });
    await expect(api.execute(op)).rejects.toMatchObject({ outcomeUnknown: true });
  });
  it('rejects a Customer financial write or invalid scoped identifier before HTTP', async () => {
    await expect(
      api.execute({
        action: 'deposits',
        staff: false,
        bookingId: id,
        key: 'key',
        revision: 0,
        body: {},
      }),
    ).rejects.toMatchObject({ kind: 'invalid' });
    await expect(api.detail(true, id, '../accounts')).rejects.toMatchObject({ kind: 'invalid' });
    expect(transport.post).not.toHaveBeenCalled();
    expect(transport.get).not.toHaveBeenCalled();
  });
});
