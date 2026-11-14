import { Injectable, inject } from '@angular/core';
import { SalesApi } from '../api/sales-api';
import { CrmService } from './crm.service';
import { BookingApi } from '../api/booking-api';
import { SessionContext } from './session-context.service';
import { ApiFailure } from '../models/api-failure';
import { Lead } from '../models/crm';
import { StaffBooking } from '../models/booking';
import {
  SaleFilters,
  SaleUnit,
  ManagerDeal,
  CommissionSnapshot,
  SaleOperation,
} from '../models/sales';
import { bookingMoney } from './booking.service';
import { viewingUtc } from './viewing.service';
import { crmId } from '../api/crm-api';

@Injectable({ providedIn: 'root' })
export class SalesService {
  private readonly api = inject(SalesApi);
  private readonly crm = inject(CrmService);
  private readonly bookings = inject(BookingApi);
  private readonly session = inject(SessionContext);
  customers(n = 1) {
    return this.crm.customers(n);
  }
  leads(customer: string, n = 1) {
    return this.crm.leads(customer, n);
  }
  lead(customer: string, id: string) {
    return this.crm.lead(customer, id);
  }
  units(customer: string, lead: string, n = 1, q = '') {
    return this.api.units(customer, lead, n, q);
  }
  unit(customer: string, lead: string, id: string) {
    return this.api.unit(customer, lead, id);
  }
  bookingCandidates(customer: string, n = 1) {
    return this.bookings.customerList(customer, n, 'Confirmed');
  }
  booking(customer: string, id: string) {
    return this.bookings.detail(true, id, customer) as Promise<StaffBooking>;
  }
  list(filters: SaleFilters = {}, n = 1) {
    return this.api.list(filters, n);
  }
  detail(customer: string, id: string) {
    return this.api.detail(customer, id);
  }
  snapshot(customer: string, id: string) {
    return this.api.snapshot(customer, id);
  }
  summary(customer: string, id: string) {
    return this.api.summary(customer, id);
  }
  collections(customer: string, id: string, n = 1) {
    return this.api.collections(customer, id, n);
  }
  activity(filters: SaleFilters = {}) {
    return this.api.activity(filters);
  }
  commissions(filters: SaleFilters = {}, n = 1) {
    return this.api.commissions(filters, n);
  }
  adoption(
    customer: string,
    lead: Lead,
    unit: SaleUnit,
    booking: StaffBooking | null,
    fields: Record<string, string>,
  ): SaleOperation {
    if (
      lead.customerId !== customer ||
      lead.projectId !== unit.projectId ||
      unit.hasAdoptedSale ||
      !['Open', 'Closed'].includes(lead.status)
    )
      throw new ApiFailure('invalid');
    if (
      booking &&
      (booking.customerId !== customer ||
        booking.unitId !== unit.id ||
        booking.leadId !== lead.id ||
        booking.status !== 'Confirmed')
    )
      throw new ApiFailure('conflict');
    const currency = fields['currencyCode']?.trim().toUpperCase();
    if (!/^[A-Z]{3}$/.test(currency ?? '')) throw new ApiFailure('invalid');
    const body = Object.freeze({
      unitId: unit.id,
      unitVersion: unit.version,
      leadId: lead.id,
      leadVersion: lead.version,
      finalUnitPrice: bookingMoney(fields['finalUnitPrice'] ?? ''),
      currencyCode: currency,
      saleOccurredAtUtc: viewingUtc(fields['saleOccurredAtUtc'] ?? ''),
      source: fields['source']?.trim() || null,
      reference: fields['reference']?.trim() || null,
      selectionReason: fields['selectionReason']?.trim() || null,
      bookingId: booking?.id ?? null,
      bookingVersion: booking?.version ?? null,
    });
    return Object.freeze({
      action: 'adopt',
      customerId: crmId(customer),
      body,
      key: crypto.randomUUID(),
      revision: this.session.revision(),
    });
  }
  collection(
    deal: ManagerDeal,
    snapshot: CommissionSnapshot,
    fields: Record<string, string>,
  ): SaleOperation {
    if (
      deal.id !== snapshot.id ||
      deal.customerId !== snapshot.customerId ||
      deal.currencyCode !== snapshot.currencyCode
    )
      throw new ApiFailure('invalid');
    return Object.freeze({
      action: 'collect',
      customerId: deal.customerId,
      dealId: deal.id,
      key: crypto.randomUUID(),
      revision: this.session.revision(),
      body: Object.freeze({
        version: deal.version,
        amount: bookingMoney(fields['amount'] ?? ''),
        currencyCode: snapshot.currencyCode,
        collectedAtUtc: viewingUtc(fields['collectedAtUtc'] ?? ''),
        source: fields['source']?.trim() || null,
        reference: fields['reference']?.trim() || null,
        notes: fields['notes']?.trim() || null,
      }),
    });
  }
  execute(op: SaleOperation) {
    if (op.revision !== this.session.revision()) throw new ApiFailure('stale');
    if (!this.session.account()?.roles.includes('SalesManager')) throw new ApiFailure('forbidden');
    return this.api.execute(op);
  }
}
