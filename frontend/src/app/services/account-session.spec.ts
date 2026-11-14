import { TestBed } from '@angular/core/testing';
import { AccountService } from './account.service';
import { AccountApi } from '../api/account-api';
import { SessionContext } from './session-context.service';
import { AccountFailure } from '../models/account';
import { vi } from 'vitest';
describe('FAC-02/11 current session orchestration', () => {
  it('clears peers during identity transitions and notifies them even when login has an unknown outcome', async () => {
    let channel!: FakeChannel;
    class FakeChannel {
      readonly sent: string[] = [];
      onmessage: ((event: MessageEvent<unknown>) => void) | null = null;
      constructor() {
        channel = this;
      }
      postMessage(value: string) {
        this.sent.push(value);
      }
      close() {
        /* No external resource in this test double. */
      }
    }
    vi.stubGlobal('BroadcastChannel', FakeChannel);
    try {
      const me = vi.fn();
      me.mockResolvedValue({
        accountId: 'own',
        email: 'own@brokerhub.test',
        roles: ['SystemAdministrator'],
      });
      TestBed.configureTestingModule({
        providers: [
          {
            provide: AccountApi,
            useValue: {
              me,
              login: async () => {
                throw new AccountFailure('network', [], true);
              },
            },
          },
        ],
      });
      const service = TestBed.inject(AccountService);
      service.start();
      await service.ensure();
      const reads = me.mock.calls.length;
      channel.onmessage?.({ data: 'changing' } as MessageEvent);
      expect(service.account()).toBeNull();
      expect(service.status()).toBe('loading');
      expect(me).toHaveBeenCalledTimes(reads);
      channel.onmessage?.({ data: 'changed' } as MessageEvent);
      await service.ensure();
      expect(service.status()).toBe('authenticated');
      await expect(service.login('other@brokerhub.test', 'unused', false)).rejects.toMatchObject({
        kind: 'network',
      });
      expect(channel.sent).toEqual(['changing', 'changed']);
      expect(service.account()).toBeNull();
    } finally {
      vi.unstubAllGlobals();
    }
  });
  it('deduplicates actual me reads and rejects an old response after another identity is established', async () => {
    let finish!: (value: { accountId: string; email: string; roles: string[] }) => void;
    const me = vi.fn(
      () =>
        new Promise<{ accountId: string; email: string; roles: string[] }>((resolve) => {
          finish = resolve;
        }),
    );
    TestBed.configureTestingModule({
      providers: [
        {
          provide: AccountApi,
          useValue: {
            me,
            login: async () => ({
              accountId: 'new',
              email: 'new@brokerhub.test',
              roles: ['Customer'],
            }),
          },
        },
      ],
    });
    const service = TestBed.inject(AccountService);
    const first = service.ensure().catch((error) => error);
    const second = service.ensure().catch((error) => error);
    expect(me).toHaveBeenCalledTimes(1);
    await service.login('new@brokerhub.test', 'unused', false);
    finish({ accountId: 'old', email: 'old@brokerhub.test', roles: ['SystemAdministrator'] });
    expect(await first).toMatchObject({ kind: 'stale' });
    expect(await second).toMatchObject({ kind: 'stale' });
    expect(service.account()?.accountId).toBe('new');
  });
  it('keeps unavailable and guest distinct, with no retained private identity after failed logout', async () => {
    const api = {
      me: vi.fn().mockRejectedValue(new AccountFailure('unavailable')),
      logout: vi.fn().mockRejectedValue(new AccountFailure('network')),
    };
    TestBed.configureTestingModule({ providers: [{ provide: AccountApi, useValue: api }] });
    const service = TestBed.inject(AccountService);
    const context = TestBed.inject(SessionContext);
    await expect(service.ensure()).rejects.toMatchObject({ kind: 'unavailable' });
    expect(service.status()).toBe('unavailable');
    api.me.mockRejectedValue(new AccountFailure('unauthorized'));
    expect(await service.ensure(true)).toBeNull();
    expect(service.status()).toBe('guest');
    context.accept(context.revision(), {
      accountId: 'own',
      email: 'own@brokerhub.test',
      roles: ['SystemAdministrator'],
    });
    await expect(service.logout()).rejects.toMatchObject({ kind: 'network' });
    expect(service.account()).toBeNull();
    expect(service.status()).toBe('unavailable');
  });
});
