import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { ViewingApi } from '../api/viewing-api';
import { ViewingService, viewingUtc } from './viewing.service';
import { SessionContext } from './session-context.service';
import { StaffViewing } from '../models/viewing';
describe('AC-06/07 viewing command time and identity boundary', () => {
  it('confirms the saved proposed start exactly without replacing its seven-digit precision', () => {
    TestBed.configureTestingModule({ providers: [{ provide: ViewingApi, useValue: {} }] });
    const current = {
      id: '11111111-1111-1111-1111-111111111111',
      customerId: '22222222-2222-2222-2222-222222222222',
      version: '9223372036854775806',
      currentProposedStartUtc: '2026-11-08T10:00:00.1234567Z',
    } as StaffViewing;
    const command = TestBed.inject(ViewingService).command('confirm', true, current, {
      confirmedStartUtc: '2026-11-10T20:00',
      confirmedEndUtc: '2026-11-08T14:00',
      coordinatedAtUtc: '2026-11-07T10:00:30.123',
      coordinationSource: 'Fictional coordination',
    });
    expect(command.body['confirmedStartUtc']).toBe(current.currentProposedStartUtc);
    expect(command.body['version']).toBe(current.version);
  });
  it('converts an explicitly local input to UTC and rejects missing/normalized dates', () => {
    expect(viewingUtc('2026-11-08T10:00')).toBe(new Date(2026, 10, 8, 10, 0).toISOString());
    expect(viewingUtc('2026-11-08T10:00:30.123')).toBe(
      new Date(2026, 10, 8, 10, 0, 30, 123).toISOString(),
    );
    expect(() => viewingUtc('2026-02-30T10:00')).toThrow();
    expect(() => viewingUtc('2026-11-08T10:00Z')).toThrow();
  });
  it('freezes only declared primitive fields and rejects the old identity before sending', () => {
    const api = { execute: vi.fn() };
    TestBed.configureTestingModule({ providers: [{ provide: ViewingApi, useValue: api }] });
    const service = TestBed.inject(ViewingService);
    const session = TestBed.inject(SessionContext);
    const fields = {
      unitId: '11111111-1111-1111-1111-111111111111',
      proposedStartUtc: '2026-11-08T10:00',
      customerId: 'forbidden',
      notes: 'private',
    };
    const op = service.command('submit', false, null, fields);
    fields.unitId = 'edited';
    expect(Object.keys(op.body)).toEqual(['unitId', 'proposedStartUtc']);
    expect(op.body['unitId']).not.toBe(fields.unitId);
    expect(Object.isFrozen(op.body)).toBe(true);
    expect(Object.isFrozen(op)).toBe(true);
    expect(
      service.command('submit', false, null, { ...fields, unitId: op.body['unitId']! }).key,
    ).not.toBe(op.key);
    session.clear('guest');
    expect(() => service.execute(op)).toThrow();
    expect(api.execute).not.toHaveBeenCalled();
  });
});
