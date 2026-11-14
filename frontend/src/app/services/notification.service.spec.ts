import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { NotificationService } from './notification.service';
import { NotificationApi } from '../api/notification-api';
import { SessionContext } from './session-context.service';
import { NotificationPage } from '../models/notification';
const id = '11111111-1111-1111-1111-111111111111';
describe('AC-05/09 notification role and late identity boundary', () => {
  it('technical administration alone never calls private notification APIs', async () => {
    const list = vi.fn();
    TestBed.configureTestingModule({
      providers: [{ provide: NotificationApi, useValue: { list } }],
    });
    const session = TestBed.inject(SessionContext);
    session.accept(session.revision(), {
      accountId: id,
      email: 'admin@brokerhub.test',
      roles: ['SystemAdministrator'],
    });
    await expect(TestBed.inject(NotificationService).list(1, '')).rejects.toMatchObject({
      kind: 'forbidden',
    });
    expect(list).not.toHaveBeenCalled();
  });
  it('a completed private read cannot cross an identity revision', async () => {
    let release!: (value: NotificationPage) => void;
    const list = vi.fn(
      () =>
        new Promise<NotificationPage>((resolve) => {
          release = resolve;
        }),
    );
    TestBed.configureTestingModule({
      providers: [{ provide: NotificationApi, useValue: { list } }],
    });
    const session = TestBed.inject(SessionContext);
    session.accept(session.revision(), {
      accountId: id,
      email: 'owner@brokerhub.test',
      roles: ['Customer'],
    });
    const pending = TestBed.inject(NotificationService).list(1, '');
    session.clear('guest');
    release({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    await expect(pending).rejects.toMatchObject({ kind: 'stale' });
  });
});
