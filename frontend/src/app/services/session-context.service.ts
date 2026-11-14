import { Injectable, signal } from '@angular/core';
import { Subject } from 'rxjs';
import { Account } from '../models/account';

export type SessionStatus =
  'unknown' | 'loading' | 'authenticated' | 'guest' | 'expired' | 'forbidden' | 'unavailable';

/** Network-free identity boundary, shared by adapters and account orchestration. */
@Injectable({ providedIn: 'root' })
export class SessionContext {
  private readonly current = signal<Account | null>(null);
  private readonly phase = signal<SessionStatus>('unknown');
  private readonly epoch = signal(0);
  private readonly invalidated = new Subject<void>();
  readonly account = this.current.asReadonly();
  readonly status = this.phase.asReadonly();
  readonly revision = this.epoch.asReadonly();
  readonly changes = this.invalidated.asObservable();

  clear(status: SessionStatus = 'unknown'): number {
    this.current.set(null);
    this.phase.set(status);
    this.epoch.update((value) => value + 1);
    this.invalidated.next();
    return this.epoch();
  }
  accept(revision: number, account: Account): void {
    if (revision !== this.epoch()) return;
    this.current.set(
      Object.freeze({
        accountId: account.accountId,
        email: account.email,
        roles: Object.freeze([...account.roles]),
      }),
    );
    this.phase.set('authenticated');
  }
  fail(revision: number, status: SessionStatus): void {
    if (revision !== this.epoch()) return;
    this.current.set(null);
    this.phase.set(status);
  }
}
