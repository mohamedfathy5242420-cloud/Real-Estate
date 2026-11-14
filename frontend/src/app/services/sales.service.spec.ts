import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { SalesService } from './sales.service';
import { SalesApi } from '../api/sales-api';
import { CrmService } from './crm.service';
import { BookingApi } from '../api/booking-api';
import { SessionContext } from './session-context.service';
import { Lead } from '../models/crm';
import { SaleUnit, ManagerDeal, CommissionSnapshot } from '../models/sales';
import { StaffBooking } from '../models/booking';
const id = '11111111-1111-1111-1111-111111111111',
  other = '22222222-2222-2222-2222-222222222222';
describe('AC-12/13 external sale and collection snapshots', () => {
  function setup() {
    const api = { execute: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        { provide: SalesApi, useValue: api },
        { provide: CrmService, useValue: {} },
        { provide: BookingApi, useValue: {} },
      ],
    });
    return { service: TestBed.inject(SalesService), session: TestBed.inject(SessionContext), api };
  }
  const lead = {
    id,
    customerId: id,
    projectId: id,
    version: '9223372036854775806',
    status: 'Closed',
  } as Lead;
  const unit = {
    id,
    projectId: id,
    version: '9223372036854775807',
    hasAdoptedSale: false,
  } as SaleUnit;
  const fields = {
    finalUnitPrice: '9999999999999999.99',
    currencyCode: 'egp',
    saleOccurredAtUtc: '2026-11-08T12:00:30.123',
    source: 'Fictional sale',
    reference: 'Fictional reference',
    selectionReason: 'Select protected origin',
    amount: 'forged',
    actorAccountId: other,
  };
  it('retains reviewed Unit/Lead versions and Closed original origin, no injected commission or actor', () => {
    const { service } = setup();
    const op = service.adoption(id, lead, unit, null, fields);
    expect(op.body['unitVersion']).toBe(unit.version);
    expect(op.body['leadVersion']).toBe(lead.version);
    expect(op.body['finalUnitPrice']).toBe(fields.finalUnitPrice);
    expect(op.body['currencyCode']).toBe('EGP');
    expect(op.body['bookingId']).toBeNull();
    expect(op.body['amount']).toBeUndefined();
    expect(op.body['actorAccountId']).toBeUndefined();
    expect(Object.isFrozen(op.body)).toBe(true);
  });
  it('never silently drops an invalid or terminal explicit booking link', () => {
    const { service } = setup();
    const booking = {
      id,
      customerId: id,
      unitId: id,
      leadId: id,
      version: '2',
      status: 'Expired',
    } as StaffBooking;
    expect(() => service.adoption(id, lead, unit, booking, fields)).toThrow();
    expect(() => service.adoption(id, lead, { ...unit, projectId: other }, null, fields)).toThrow();
  });
  it('uses original snapshot currency/current Deal version, freezing the actual collection before edits', () => {
    const { service, session, api } = setup();
    const deal = {
        id,
        customerId: id,
        version: '9223372036854775807',
        currencyCode: 'USD',
      } as ManagerDeal,
      snapshot = { id, customerId: id, currencyCode: 'USD' } as CommissionSnapshot;
    const values = {
      amount: '0.01',
      currencyCode: 'EGP',
      collectedAtUtc: '2026-11-08T12:00:30.123',
      source: 'Source',
      reference: 'Reference',
      notes: 'Note',
    };
    const op = service.collection(deal, snapshot, values);
    values.amount = '999.99';
    expect(op.body['amount']).toBe('0.01');
    expect(op.body['currencyCode']).toBe('USD');
    expect(op.body['version']).toBe(deal.version);
    expect(() => service.execute(op)).toThrow();
    session.clear('guest');
    session.accept(session.revision(), {
      accountId: id,
      email: 'manager@brokerhub.test',
      roles: ['SalesManager'],
    });
    expect(() => service.execute(op)).toThrow();
    expect(api.execute).not.toHaveBeenCalled();
  });
});
