import { Injectable, inject } from '@angular/core';
import { ApiTransport } from './api-transport';
import { crmId, crmProjection } from './crm-api';
import { ApiFailure } from '../models/api-failure';
import { CrmPage } from '../models/crm';
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
  BookingReceipt,
  BookingOperation,
  BOOKING_STATUSES,
  OWN_BOOKING_KEYS,
  STAFF_BOOKING_KEYS,
  OWN_BOOKING_EVENT_KEYS,
  STAFF_BOOKING_EVENT_KEYS,
  OWN_DEPOSIT_KEYS,
  STAFF_DEPOSIT_KEYS,
  OWN_REFUND_KEYS,
  STAFF_REFUND_KEYS,
  DEVELOPER_RESPONSE_KEYS,
  RESERVATION_EXTENSION_KEYS,
} from '../models/booking';
function projection<T>(raw: unknown, keys: readonly string[]): T {
  const row = crmProjection<Record<string, unknown>>(raw, keys);
  for (const key of keys.filter((k) => /(amount|price)$/i.test(k))) {
    const value = row[key];
    if (
      value !== null &&
      (typeof value !== 'string' || !/^(?:0|[1-9]\d{0,15})(?:\.\d{1,2})?$/.test(value))
    )
      throw new ApiFailure('unknown');
  }
  for (const key of keys.filter((k) => /currencyCode$/i.test(k))) {
    const value = row[key];
    if (value !== null && (typeof value !== 'string' || !/^[A-Z]{3}$/.test(value)))
      throw new ApiFailure('unknown');
  }
  return row as T;
}
function root(staff: boolean, customerId?: string) {
  return staff ? `/api/crm/customers/${crmId(customerId ?? '')}/bookings` : '/api/bookings';
}
function query(n: number, status = '') {
  if (!Number.isInteger(n) || n < 1 || n > 1000000) throw new ApiFailure('invalid');
  return (
    '?' + new URLSearchParams({ page: String(n), pageSize: '20', ...(status ? { status } : {}) })
  );
}
function page<T>(raw: unknown, mapper: (value: unknown) => T): CrmPage<T> {
  const p = raw as CrmPage<unknown>;
  if (
    !p ||
    !Array.isArray(p.items) ||
    !Number.isSafeInteger(p.page) ||
    p.page < 1 ||
    !Number.isSafeInteger(p.pageSize) ||
    p.pageSize < 1 ||
    !Number.isSafeInteger(p.total) ||
    p.total < 0
  )
    throw new ApiFailure('unknown');
  return Object.freeze({
    items: Object.freeze(p.items.map(mapper)),
    page: p.page,
    pageSize: p.pageSize,
    total: p.total,
  });
}
function booking(raw: unknown, staff: boolean): OwnBooking | StaffBooking {
  const row = projection<OwnBooking | StaffBooking>(
    raw,
    staff ? STAFF_BOOKING_KEYS : OWN_BOOKING_KEYS,
  );
  if (!(BOOKING_STATUSES as readonly string[]).includes(row.status))
    throw new ApiFailure('unknown');
  crmId(row.unitId);
  crmId(row.projectId);
  if (staff) crmId((row as StaffBooking).customerId);
  return row;
}
@Injectable({ providedIn: 'root' })
export class BookingApi {
  private readonly http = inject(ApiTransport);
  async customerList(customerId: string, n = 1, status = '') {
    return page(
      await this.http.get(root(true, customerId) + query(n, status)),
      (r) => booking(r, true) as StaffBooking,
    );
  }
  async list(staff: boolean, n = 1, status = '') {
    return page(
      await this.http.get(
        (staff ? '/api/crm/bookings' : '/api/bookings') + query(n, staff ? status : ''),
      ),
      (r) => booking(r, staff),
    );
  }
  async detail(staff: boolean, id: string, customerId?: string) {
    return booking(await this.http.get(root(staff, customerId) + '/' + crmId(id)), staff);
  }
  async history(staff: boolean, id: string, customerId?: string, n = 1) {
    return page(
      await this.http.get(root(staff, customerId) + '/' + crmId(id) + '/history' + query(n)),
      (r) =>
        projection<OwnBookingEvent | StaffBookingEvent>(
          r,
          staff ? STAFF_BOOKING_EVENT_KEYS : OWN_BOOKING_EVENT_KEYS,
        ),
    );
  }
  async deposits(staff: boolean, id: string, customerId?: string, n = 1) {
    return page(
      await this.http.get(root(staff, customerId) + '/' + crmId(id) + '/deposits' + query(n)),
      (r) =>
        projection<OwnDeposit | StaffDeposit>(r, staff ? STAFF_DEPOSIT_KEYS : OWN_DEPOSIT_KEYS),
    );
  }
  async deposit(staff: boolean, id: string, depositId: string, customerId?: string) {
    return projection<OwnDeposit | StaffDeposit>(
      await this.http.get(
        root(staff, customerId) + '/' + crmId(id) + '/deposits/' + crmId(depositId),
      ),
      staff ? STAFF_DEPOSIT_KEYS : OWN_DEPOSIT_KEYS,
    );
  }
  async refunds(staff: boolean, id: string, depositId: string, customerId?: string, n = 1) {
    return page(
      await this.http.get(
        root(staff, customerId) +
          '/' +
          crmId(id) +
          '/deposits/' +
          crmId(depositId) +
          '/refunds' +
          query(n),
      ),
      (r) => projection<OwnRefund | StaffRefund>(r, staff ? STAFF_REFUND_KEYS : OWN_REFUND_KEYS),
    );
  }
  async responses(customerId: string, id: string, n = 1) {
    return page(
      await this.http.get(root(true, customerId) + '/' + crmId(id) + '/responses' + query(n)),
      (r) => projection<DeveloperResponse>(r, DEVELOPER_RESPONSE_KEYS),
    );
  }
  async extensions(customerId: string, id: string, n = 1) {
    return page(
      await this.http.get(root(true, customerId) + '/' + crmId(id) + '/extensions' + query(n)),
      (r) => projection<ReservationExtension>(r, RESERVATION_EXTENSION_KEYS),
    );
  }
  async execute(op: BookingOperation): Promise<BookingReceipt> {
    if (
      (op.staff && (op.action === 'submit' || op.action === 'cancel')) ||
      (!op.staff && ['responses', 'extensions', 'deposits', 'refunds'].includes(op.action))
    )
      throw new ApiFailure('invalid');
    const base =
      op.action === 'submit'
        ? '/api/bookings'
        : root(op.staff, op.customerId) + '/' + crmId(op.bookingId ?? '');
    const path =
      op.action === 'submit'
        ? base
        : op.action === 'refunds'
          ? base + '/deposits/' + crmId(op.depositId ?? '') + '/refunds'
          : base + '/' + op.action;
    const raw = await this.http.post(path, op.body, true, op.key);
    try {
      const reply = projection<BookingReceipt>(raw, ['id', 'version', 'isReplay']);
      if (typeof reply.isReplay !== 'boolean') throw new ApiFailure('unknown');
      return reply;
    } catch {
      throw new ApiFailure('unknown', [], true);
    }
  }
}
