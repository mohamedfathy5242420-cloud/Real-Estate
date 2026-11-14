import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { CrmPageComponent } from './crm-page';
import { CrmService } from '../../services/crm.service';
import { SessionContext } from '../../services/session-context.service';
import { ApiFailure } from '../../models/api-failure';
import { CrmCommand, CrmCustomer, CrmOperation, CrmPage, SalesNote } from '../../models/crm';
import { FormControl } from '@angular/forms';
import { Signal } from '@angular/core';
const id = '11111111-1111-1111-1111-111111111111',
  actor = '22222222-2222-2222-2222-222222222222';
const customer: CrmCustomer = {
  id,
  fullName: 'Fictional customer',
  phone: '123',
  email: null,
  preferredContactMethod: null,
  requirements: 'Before',
  version: '9223372036854775806',
  assignedEmployeeId: actor,
  createdAtUtc: '2026-11-05T00:00:00Z',
  updatedAtUtc: null,
};
function page<T>(...items: T[]): CrmPage<T> {
  return { items, page: 1, pageSize: 20, total: items.length };
}
interface State {
  open(id: string): Promise<void>;
  reload(): Promise<void>;
  saveNeeds(): Promise<void>;
  recover(): Promise<void>;
  requirements: FormControl<string>;
  current: Signal<CrmCustomer | null>;
  notes: Signal<CrmPage<SalesNote> | null>;
  pending: Signal<CrmOperation | null>;
  blocked: Signal<boolean>;
}
describe('AC-04/05 current CRM scope and uncertain commands', () => {
  function setup() {
    const service = {
      customers: vi.fn().mockResolvedValue(page(customer)),
      customer: vi.fn().mockResolvedValue(customer),
      notes: vi.fn().mockResolvedValue(page({ id, body: 'Private note', customerId: id })),
      needsHistory: vi.fn().mockResolvedValue(page()),
      assignments: vi.fn().mockResolvedValue(page()),
      leads: vi.fn().mockResolvedValue(page()),
      needs: (record: CrmCustomer, text: string): CrmCommand => ({
        key: 'owned-key',
        revision: 0,
        body: Object.freeze({ version: record.version, requirements: text }),
      }),
      execute: vi.fn(),
    };
    TestBed.configureTestingModule({
      imports: [CrmPageComponent],
      providers: [provideRouter([]), { provide: CrmService, useValue: service }],
    });
    const session = TestBed.inject(SessionContext);
    session.accept(session.revision(), {
      accountId: actor,
      email: 'employee@brokerhub.test',
      roles: ['SalesEmployee'],
    });
    const fixture = TestBed.createComponent(CrmPageComponent);
    return { fixture, state: fixture.componentInstance as unknown as State, service, session };
  }
  it('recovers an unknown write only on explicit action with the original snapshot/key', async () => {
    const { fixture, state, service } = setup();
    await fixture.whenStable();
    await state.open(id);
    service.execute
      .mockRejectedValueOnce(new ApiFailure('network', [], true))
      .mockResolvedValueOnce({ id, customerId: id, version: '1', isReplay: true });
    state.requirements.setValue('Reviewed');
    await state.saveNeeds();
    expect(service.execute).toHaveBeenCalledTimes(1);
    expect(state.pending()).not.toBeNull();
    const pending = state.pending();
    state.requirements.setValue('Edit after interruption');
    await state.reload();
    expect(state.pending()).toBe(pending);
    await state.recover();
    expect(service.execute.mock.calls[1][0]).toBe(pending);
    expect(service.execute.mock.calls[1][0].command.body['requirements']).toBe('Reviewed');
    expect(state.pending()).toBeNull();
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain(
      'The server confirmed the previous operation',
    );
  });
  it('clears the whole selected resource after a successful write followed by scope403', async () => {
    const { fixture, state, service, session } = setup();
    await fixture.whenStable();
    await state.open(id);
    expect(state.notes()?.items).toHaveLength(1);
    service.execute.mockResolvedValue({ id, customerId: id, version: '1', isReplay: false });
    service.customers.mockRejectedValue(new ApiFailure('forbidden'));
    await state.saveNeeds();
    await fixture.whenStable();
    expect(state.current()).toBeNull();
    expect(state.notes()).toBeNull();
    expect(state.pending()).toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('Private note');
    expect(session.account()?.accountId).toBe(actor);
  });
  it('never restores an old selected customer after the identity changes mid-read', async () => {
    const { fixture, state, service, session } = setup();
    await fixture.whenStable();
    let release!: (value: CrmCustomer) => void;
    service.customer.mockImplementationOnce(
      () =>
        new Promise<CrmCustomer>((resolve) => {
          release = resolve;
        }),
    );
    const reading = state.open(id);
    session.clear('guest');
    fixture.detectChanges();
    release(customer);
    await reading;
    await fixture.whenStable();
    expect(state.current()).toBeNull();
    expect(state.notes()).toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('Private note');
  });
  it('requires review after409 without retaining a recovery command or automatically retrying', async () => {
    const { fixture, state, service } = setup();
    await fixture.whenStable();
    await state.open(id);
    service.execute.mockRejectedValue(new ApiFailure('conflict'));
    await state.saveNeeds();
    await state.saveNeeds();
    expect(service.execute).toHaveBeenCalledTimes(1);
    expect(state.blocked()).toBe(true);
    expect(state.pending()).toBeNull();
    await state.reload();
    expect(state.blocked()).toBe(false);
  });
});
