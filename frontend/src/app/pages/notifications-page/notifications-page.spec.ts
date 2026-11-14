import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { Signal } from '@angular/core';
import { vi } from 'vitest';
import { NotificationsPage } from './notifications-page';
import { NotificationService } from '../../services/notification.service';
import { SessionContext } from '../../services/session-context.service';
import { NotificationPage, UserNotification } from '../../models/notification';
import { ApiFailure } from '../../models/api-failure';
const id = '11111111-1111-1111-1111-111111111111';
const at = '2026-11-08T00:00:00Z';
const row: UserNotification = {
  id,
  kind: 'Viewing.Confirmed',
  title: 'Confirm viewing',
  message: 'A viewing confirmation was recorded.',
  occurredAtUtc: at,
  createdAtUtc: at,
  readAtUtc: null,
  resource: { id, kind: 'Viewing', customerId: id, parentId: null, relatedId: null },
};
const page: NotificationPage = { items: [row], totalCount: 1, page: 1, pageSize: 20 };
interface State {
  reload(): Promise<void>;
  open(id: string): Promise<void>;
  markRead(): Promise<void>;
  rows: Signal<NotificationPage | null>;
  count: Signal<number | null>;
  selected: Signal<UserNotification | null>;
}
describe('AC-05/09/14 notification current scope and lost read response', () => {
  async function setup() {
    const service = {
      list: vi.fn().mockResolvedValue(page),
      count: vi.fn().mockResolvedValue(1),
      detail: vi.fn().mockResolvedValue(row),
      markRead: vi.fn().mockResolvedValue({ notificationId: id, readAtUtc: at }),
      destination: () => null,
    };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'notifications', component: NotificationsPage }]),
        { provide: NotificationService, useValue: service },
      ],
    });
    const session = TestBed.inject(SessionContext);
    session.accept(session.revision(), {
      accountId: id,
      email: 'owner@brokerhub.test',
      roles: ['Customer'],
    });
    const harness = await RouterTestingHarness.create('/notifications');
    await harness.fixture.whenStable();
    return {
      state: harness.routeDebugElement!.componentInstance as State,
      service,
      session,
      harness,
    };
  }
  it('scope404 removes previous detail/list/count; unavailable is never rendered as zero', async () => {
    const { state, service, harness } = await setup();
    await state.open(id);
    expect(state.selected()?.id).toBe(id);
    service.detail.mockRejectedValueOnce(new ApiFailure('not-found'));
    await state.open(id);
    harness.fixture.detectChanges();
    expect(state.selected()).toBeNull();
    expect(state.rows()).toBeNull();
    expect(state.count()).toBeNull();
    expect(harness.routeNativeElement!.textContent).toContain('The count is currently unavailable');
  });
  it('lost mark-read response clears data and explicit reload reconciles without replaying the write', async () => {
    const { state, service } = await setup();
    await state.open(id);
    service.markRead.mockRejectedValueOnce(new ApiFailure('network', [], true));
    await state.markRead();
    expect(state.selected()).toBeNull();
    expect(state.count()).toBeNull();
    service.detail.mockResolvedValue({ ...row, readAtUtc: at });
    await state.reload();
    await state.open(id);
    expect(state.selected()?.readAtUtc).toBe(at);
    expect(service.markRead).toHaveBeenCalledTimes(1);
  });
  it('late response after logout cannot restore private notification details or counts', async () => {
    const { state, service, session, harness } = await setup();
    let release!: (value: NotificationPage) => void;
    service.list.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          release = resolve;
        }),
    );
    const pending = state.reload();
    session.clear('guest');
    harness.fixture.detectChanges();
    release(page);
    await pending;
    await harness.fixture.whenStable();
    expect(state.rows()).toBeNull();
    expect(state.selected()).toBeNull();
    expect(state.count()).toBeNull();
    expect(harness.routeNativeElement!.textContent).not.toContain(row.message);
  });
});
