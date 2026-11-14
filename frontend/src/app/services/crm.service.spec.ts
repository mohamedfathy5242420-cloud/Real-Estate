import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { CrmService } from './crm.service';
import { CrmApi } from '../api/crm-api';
import { CatalogReader } from '../api/catalog-reader';
import { SessionContext } from './session-context.service';
describe('AC-04/05 in-memory CRM commands', () => {
  it('freezes reviewed primitive values, uses a fresh key for a new action and rejects a previous identity', () => {
    const api = { execute: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        { provide: CrmApi, useValue: api },
        { provide: CatalogReader, useValue: {} },
      ],
    });
    const service = TestBed.inject(CrmService),
      session = TestBed.inject(SessionContext);
    const values = { version: '9223372036854775807', requirements: 'Before' };
    const command = service.command(values);
    values.requirements = 'After';
    expect(command.body['requirements']).toBe('Before');
    expect(Object.isFrozen(command.body)).toBe(true);
    expect(command.key).toMatch(/^[a-z0-9-]{36}$/);
    expect(service.command(values).key).not.toBe(command.key);
    session.clear('guest');
    expect(() => service.execute({ action: 'intake', command })).toThrow();
    expect(api.execute).not.toHaveBeenCalled();
  });
});
