import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { DestroyRef, Injectable, PLATFORM_ID, inject } from '@angular/core';
import { AccountApi } from '../api/account-api';
import { Account, AccountFailure } from '../models/account';
import { SessionContext } from './session-context.service';

/** Account journeys own the current session; no private state is persisted. */
@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly api = inject(AccountApi);
  private readonly context = inject(SessionContext);
  private readonly browser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly document = inject(DOCUMENT);
  private readonly destroy = inject(DestroyRef);
  private pending: { revision: number; promise: Promise<Account | null> } | null = null;
  private channel: BroadcastChannel | null = null;
  readonly account = this.context.account;
  readonly status = this.context.status;
  readonly revision = this.context.revision;

  start(): void {
    if (!this.browser) return;
    if (!this.channel && this.document.defaultView && typeof BroadcastChannel !== 'undefined') {
      this.channel = new BroadcastChannel('brokerhub.session');
      this.channel.onmessage = (event: MessageEvent<unknown>) => {
        if (event.data !== 'changing' && event.data !== 'changed') return;
        this.context.clear(event.data === 'changing' ? 'loading' : 'unknown');
        if (event.data === 'changed') void this.ensure().catch(() => undefined);
      };
      this.destroy.onDestroy(() => this.channel?.close());
    }
    void this.ensure().catch(() => undefined);
  }
  ensure(force = false): Promise<Account | null> {
    if (!this.browser) return Promise.resolve(null);
    if (this.pending?.revision === this.revision()) return this.pending.promise;
    if (!force && this.status() === 'authenticated') return Promise.resolve(this.account());
    if (!force && ['guest', 'expired'].includes(this.status())) return Promise.resolve(null);
    const revision = this.context.clear('loading');
    const promise = this.api
      .me()
      .then((dto) => {
        this.assertCurrent(revision);
        const account = publicAccount(dto);
        this.context.accept(revision, account);
        return account;
      })
      .catch((error: unknown) => {
        this.assertCurrent(revision);
        const kind = error instanceof AccountFailure ? error.kind : 'unknown';
        this.context.fail(
          revision,
          kind === 'unauthorized' ? 'guest' : kind === 'forbidden' ? 'forbidden' : 'unavailable',
        );
        if (kind === 'unauthorized') return null;
        throw error;
      })
      .finally(() => {
        if (this.pending?.promise === promise) this.pending = null;
      });
    this.pending = { revision, promise };
    return promise;
  }
  async me(): Promise<Account> {
    const value = await this.ensure(true);
    if (!value) throw new AccountFailure('unauthorized');
    return value;
  }
  async login(email: string, password: string, rememberMe: boolean): Promise<Account> {
    const revision = this.context.clear('loading');
    // Clear peer-tab private views even if the new login has an unknown outcome.
    this.channel?.postMessage('changing');
    try {
      const account = publicAccount(await this.api.login(email, password, rememberMe));
      this.assertCurrent(revision);
      this.context.accept(revision, account);
      return account;
    } catch (error: unknown) {
      this.context.fail(
        revision,
        error instanceof AccountFailure && error.kind === 'unauthorized' ? 'guest' : 'unavailable',
      );
      throw error;
    } finally {
      this.channel?.postMessage('changed');
    }
  }
  async register(email: string, password: string, phoneNumber: string): Promise<void> {
    await this.api.register(email, password, phoneNumber);
  }
  confirmEmail(accountId: string, token: string): Promise<void> {
    return this.api.confirmEmail(accountId, token);
  }
  async logout(): Promise<void> {
    const revision = this.context.clear('loading');
    this.channel?.postMessage('changing');
    try {
      await this.api.logout();
      this.context.fail(revision, 'guest');
    } catch (error: unknown) {
      this.context.fail(revision, 'unavailable');
      throw error;
    } finally {
      this.channel?.postMessage('changed');
    }
  }
  async forgotPassword(email: string): Promise<void> {
    await this.api.forgotPassword(email);
  }
  async resetPassword(email: string, token: string, password: string): Promise<void> {
    const revision = this.context.clear('loading');
    this.channel?.postMessage('changing');
    try {
      await this.api.resetPassword(email, token, password);
      this.context.fail(revision, 'guest');
    } catch (error: unknown) {
      this.context.fail(revision, 'unavailable');
      throw error;
    } finally {
      this.channel?.postMessage('changed');
    }
  }
  async resendConfirmation(email: string): Promise<void> {
    await this.api.resendConfirmation(email);
  }
  private assertCurrent(revision: number): void {
    if (revision !== this.revision()) throw new AccountFailure('stale');
  }
}
function publicAccount(value: Account): Account {
  if (
    !value ||
    typeof value.accountId !== 'string' ||
    typeof value.email !== 'string' ||
    !Array.isArray(value.roles) ||
    value.roles.some((role) => typeof role !== 'string')
  )
    throw new AccountFailure('unknown');
  return Object.freeze({
    accountId: value.accountId,
    email: value.email,
    roles: Object.freeze([...value.roles]),
  });
}
