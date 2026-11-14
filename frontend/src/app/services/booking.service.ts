import { Injectable, inject } from '@angular/core';
import { BookingApi } from '../api/booking-api';
import { SessionContext } from './session-context.service';
import { ApiFailure } from '../models/api-failure';
import { crmId } from '../api/crm-api';
import { viewingUtc } from './viewing.service';
import {
  BookingAction,
  BookingOperation,
  OwnBooking,
  StaffBooking,
  OwnDeposit,
  StaffDeposit,
} from '../models/booking';
export function bookingMoney(raw: string): string {
  const value = raw.trim();
  if (!/^\d{1,16}(?:\.\d{1,2})?$/.test(value)) throw new ApiFailure('invalid');
  const [whole, fraction = ''] = value.split('.');
  const normalized = whole.replace(/^0+(?=\d)/, '') + '.' + fraction.padEnd(2, '0');
  if (/^0\.00$/.test(normalized)) throw new ApiFailure('invalid');
  return normalized;
}
@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly api = inject(BookingApi);
  private readonly session = inject(SessionContext);
  list(staff: boolean, n = 1, status = '') {
    return this.api.list(staff, n, status);
  }
  detail(staff: boolean, id: string, customerId?: string) {
    return this.api.detail(staff, id, customerId);
  }
  history(staff: boolean, id: string, customerId?: string, n = 1) {
    return this.api.history(staff, id, customerId, n);
  }
  deposits(staff: boolean, id: string, customerId?: string, n = 1) {
    return this.api.deposits(staff, id, customerId, n);
  }
  deposit(staff: boolean, id: string, depositId: string, customerId?: string) {
    return this.api.deposit(staff, id, depositId, customerId);
  }
  refunds(staff: boolean, id: string, depositId: string, customerId?: string, n = 1) {
    return this.api.refunds(staff, id, depositId, customerId, n);
  }
  responses(customerId: string, id: string, n = 1) {
    return this.api.responses(customerId, id, n);
  }
  extensions(customerId: string, id: string, n = 1) {
    return this.api.extensions(customerId, id, n);
  }
  command(
    action: BookingAction,
    staff: boolean,
    current: OwnBooking | StaffBooking | null,
    fields: Record<string, string>,
    deposit?: OwnDeposit | StaffDeposit | null,
  ): BookingOperation {
    const body: Record<string, string | null> = {};
    if (action !== 'submit') {
      if (!current) throw new ApiFailure('invalid');
      body['version'] = current.version;
    }
    const confirmed = fields['responseKind'] === 'Confirmed';
    const fieldNames: Record<BookingAction, readonly string[]> = {
      submit: ['unitId', 'requestReason'],
      cancel: ['reason'],
      responses: [
        'responseKind',
        'developerRespondedAtUtc',
        'source',
        'reference',
        ...(confirmed ? ['offeredPrice', 'currencyCode', 'offeredDeadlineUtc'] : ['reason']),
      ],
      extensions: [
        'developerExtendedAtUtc',
        'newReservationExpiresAtUtc',
        'source',
        'reference',
        'reason',
      ],
      deposits: ['amount', 'currencyCode', 'paidAtUtc', 'source', 'reference', 'notes'],
      refunds: ['amount', 'returnedAtUtc', 'source', 'reference', 'notes'],
    };
    for (const key of fieldNames[action]) {
      const value = (fields[key] ?? '').trim();
      body[key] = key.endsWith('Utc')
        ? viewingUtc(value)
        : key === 'unitId'
          ? crmId(value)
          : ['amount', 'offeredPrice'].includes(key)
            ? bookingMoney(value)
            : key === 'currencyCode'
              ? value.toUpperCase()
              : value || null;
    }
    if (action === 'responses') {
      for (const key of confirmed
        ? ['reason']
        : ['offeredPrice', 'currencyCode', 'offeredDeadlineUtc'])
        body[key] = null;
    }
    if (action === 'refunds') {
      if (!deposit || deposit.bookingId !== current!.id) throw new ApiFailure('invalid');
      body['currencyCode'] = deposit.currencyCode;
    }
    if (
      body['currencyCode'] !== undefined &&
      body['currencyCode'] !== null &&
      !/^[A-Z]{3}$/.test(body['currencyCode'])
    )
      throw new ApiFailure('invalid');
    return Object.freeze({
      action,
      staff,
      ...(current ? { bookingId: current.id } : {}),
      ...(staff && current && 'customerId' in current ? { customerId: current.customerId } : {}),
      ...(action === 'refunds' ? { depositId: deposit!.id } : {}),
      key: crypto.randomUUID(),
      revision: this.session.revision(),
      body: Object.freeze(body),
    });
  }
  execute(op: BookingOperation) {
    if (op.revision !== this.session.revision()) throw new ApiFailure('stale');
    return this.api.execute(op);
  }
}
