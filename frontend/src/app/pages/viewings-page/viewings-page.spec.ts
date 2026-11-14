import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { vi } from 'vitest';
import { Signal } from '@angular/core';
import { FormRecord, FormControl } from '@angular/forms';
import { ViewingsPage } from './viewings-page';
import { ViewingService } from '../../services/viewing.service';
import { SessionContext } from '../../services/session-context.service';
import { ApiFailure } from '../../models/api-failure';
import { OwnViewing, ViewingOperation } from '../../models/viewing';
const id = '11111111-1111-1111-1111-111111111111';
const row: OwnViewing = {
  id,
  unitId: id,
  projectId: id,
  status: 'PendingConfirmation',
  version: '9223372036854775806',
  timeZoneId: 'Africa/Cairo',
  currentProposedStartUtc: '2026-11-08T10:00:00Z',
  currentProposedEndUtc: '2026-11-08T11:00:00Z',
  requestedAtUtc: '2026-11-07T10:00:00Z',
  pendingCycleStartedAtUtc: '2026-11-07T10:00:00Z',
  pendingExpiresAtUtc: '2026-11-08T10:00:00Z',
  confirmedStartUtc: null,
  confirmedEndUtc: null,
};
interface State {
  open(item: OwnViewing): Promise<void>;
  choose(action: 'cancel'): void;
  submit(): Promise<void>;
  recover(): Promise<void>;
  reload(): Promise<void>;
  form: FormRecord<FormControl<string>>;
  current: Signal<OwnViewing | null>;
  pending: Signal<ViewingOperation | null>;
  blocked: Signal<boolean>;
}
describe('AC-04/06/07 viewing page identity/scope and unknown outcome', () => {
  async function setup() {
    const service = {
      list: vi.fn().mockResolvedValue({ items: [row], page: 1, pageSize: 20, total: 1 }),
      detail: vi.fn().mockResolvedValue(row),
      history: vi.fn().mockResolvedValue({ items: [], page: 1, pageSize: 20, total: 0 }),
      command: vi.fn((_action, _staff, _current, fields) =>
        Object.freeze({
          action: 'cancel',
          staff: false,
          id,
          key: 'original-viewing-key',
          revision: 0,
          body: Object.freeze({ version: row.version, reason: fields.reason }),
        }),
      ),
      execute: vi.fn(),
    };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'my/viewings', component: ViewingsPage, data: { staff: false } }]),
        { provide: ViewingService, useValue: service },
      ],
    });
    const session = TestBed.inject(SessionContext);
    session.accept(session.revision(), {
      accountId: id,
      email: 'own@brokerhub.test',
      roles: ['Customer'],
    });
    const harness = await RouterTestingHarness.create('/my/viewings');
    await harness.fixture.whenStable();
    const state = harness.routeDebugElement!.componentInstance as State;
    await state.open(row);
    return { state, service, session, harness };
  }
  it('explicitly recovers the original body/key even after editing and reloading', async () => {
    const { state, service } = await setup();
    service.execute
      .mockRejectedValueOnce(new ApiFailure('network', [], true))
      .mockResolvedValueOnce({ id, version: '2', isReplay: true });
    state.choose('cancel');
    state.form.controls['reason'].setValue('Original reason');
    await state.submit();
    const original = state.pending();
    expect(original).not.toBeNull();
    state.form.controls['reason'].setValue('Different draft');
    await state.submit();
    expect(service.execute).toHaveBeenCalledTimes(1);
    await state.reload();
    expect(state.pending()).toBe(original);
    await state.recover();
    expect(service.execute.mock.calls[1][0]).toBe(original);
    expect(service.execute.mock.calls[1][0].body.reason).toBe('Original reason');
    expect(state.pending()).toBeNull();
  });
  it('clears selected detail/history after a real save result followed by scoped404', async () => {
    const { state, service } = await setup();
    state.choose('cancel');
    state.form.controls['reason'].setValue('Reason');
    service.execute.mockResolvedValue({ id, version: '2', isReplay: false });
    service.list.mockRejectedValue(new ApiFailure('not-found'));
    await state.submit();
    expect(state.current()).toBeNull();
    expect(state.pending()).toBeNull();
  });
  it('cannot restore a held old detail after the identity changes', async () => {
    const { state, service, session, harness } = await setup();
    let release!: (item: OwnViewing) => void;
    service.detail.mockImplementationOnce(
      () =>
        new Promise<OwnViewing>((resolve) => {
          release = resolve;
        }),
    );
    const reading = state.open(row);
    session.clear('guest');
    harness.fixture.detectChanges();
    release(row);
    await reading;
    await harness.fixture.whenStable();
    expect(state.current()).toBeNull();
    expect(harness.routeNativeElement!.textContent).not.toContain(row.id);
  });
});
