import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { BookingService, bookingMoney } from './booking.service';
import { BookingApi } from '../api/booking-api';
import { SessionContext } from './session-context.service';
import { StaffBooking, OwnDeposit } from '../models/booking';
const id = '11111111-1111-1111-1111-111111111111';
const current = { id, customerId: id, version: '9223372036854775806' } as StaffBooking;
describe('AC-11/12 exact recorded money and command identity', () => {
  it('normalizes last-cent/max/leading-zero input without Number conversion and rejects unsafe representations', () => {
    expect(bookingMoney('9999999999999999.99')).toBe('9999999999999999.99');
    expect(bookingMoney('000.1')).toBe('0.10');
    for (const value of ['0', '0.000', '1e5', '10000000000000000', '-1', '1,000.00', '1.001'])
      expect(() => bookingMoney(value)).toThrow();
  });
  it('uses original deposit currency and current booking version, freezes original facts and blocks prior identity', () => {
    const api = { execute: vi.fn() };
    TestBed.configureTestingModule({ providers: [{ provide: BookingApi, useValue: api }] });
    const service = TestBed.inject(BookingService);
    const deposit = { id, bookingId: id, currencyCode: 'USD', version: '1' } as OwnDeposit;
    const fields = {
      amount: '9999999999999999.99',
      currencyCode: 'EGP',
      returnedAtUtc: '2026-11-07T12:00:30.123',
      source: 'Source',
      reference: 'Reference',
      notes: 'Notes',
      actorAccountId: 'forged',
    };
    const op = service.command('refunds', true, current, fields, deposit);
    expect(op.body['currencyCode']).toBe('USD');
    expect(op.body['version']).toBe(current.version);
    expect(op.body['amount']).toBe(fields.amount);
    expect(op.body['actorAccountId']).toBeUndefined();
    fields.amount = '1';
    expect(op.body['amount']).not.toBe(fields.amount);
    expect(Object.isFrozen(op.body)).toBe(true);
    TestBed.inject(SessionContext).clear('guest');
    expect(() => service.execute(op)).toThrow();
    expect(api.execute).not.toHaveBeenCalled();
  });
  it('keeps rejection distinct from confirmation, omitting stale offer fields', () => {
    TestBed.configureTestingModule({ providers: [{ provide: BookingApi, useValue: {} }] });
    const command = TestBed.inject(BookingService).command('responses', true, current, {
      responseKind: 'Rejected',
      reason: 'Actual rejection',
      developerRespondedAtUtc: '2026-11-07T12:00:30.123',
      source: 'Source',
      reference: 'Reference',
      offeredPrice: '9999999999999999.99',
      offeredDeadlineUtc: '2030-01-01T00:00',
      currencyCode: 'USD',
    });
    expect(command.body['offeredPrice']).toBeNull();
    expect(command.body['offeredDeadlineUtc']).toBeNull();
    expect(command.body['currencyCode']).toBeNull();
    expect(command.body['reason']).toBe('Actual rejection');
  });
});
