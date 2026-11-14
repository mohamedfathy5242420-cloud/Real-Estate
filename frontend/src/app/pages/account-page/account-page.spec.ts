import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { vi } from 'vitest';
import { routes } from '../../app.routes';
import { AccountService } from '../../services/account.service';
import { AccountFailure } from '../../models/account';

function fill(root: HTMLElement, selector: string, value: string): void {
  const input = root.querySelector(selector) as HTMLInputElement;
  input.value = value;
  input.dispatchEvent(new Event('input'));
}
function submit(root: HTMLElement): void {
  root
    .querySelector('form')!
    .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
}

describe('Account UI boundaries', () => {
  const operations = {
    login: vi.fn(),
    register: vi.fn(),
    confirmEmail: vi.fn(),
    forgotPassword: vi.fn(),
    me: vi.fn(),
  };
  beforeEach(() => {
    Object.values(operations).forEach((operation) => {
      operation.mockReset();
      operation.mockResolvedValue(undefined);
    });
    operations.login.mockResolvedValue({
      accountId: 'own',
      email: 'own@brokerhub.test',
      roles: ['Customer'],
    });
    operations.me.mockResolvedValue({
      accountId: 'own',
      email: 'own@brokerhub.test',
      roles: ['Customer'],
    });
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        {
          provide: AccountService,
          useValue: operations,
        },
      ],
    });
  });

  it('rejects an external login return URL and never replays a Business command', async () => {
    const harness = await RouterTestingHarness.create(
      '/auth/login?returnUrl=https://hostile.invalid',
    );
    const root = harness.routeNativeElement!;
    fill(root, '#account-email', 'own@brokerhub.test');
    fill(root, '#account-password', 'a-password');
    submit(root);
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(TestBed.inject(Router).url).toBe('/auth/session');
    expect(operations.login).toHaveBeenCalledExactlyOnceWith(
      'own@brokerhub.test',
      'a-password',
      false,
    );
  });

  it('requires an explicit confirmation action and removes secret query parameters on success', async () => {
    const harness = await RouterTestingHarness.create(
      '/auth/confirm-email?accountId=own&token=local-test-token',
    );
    expect(operations.confirmEmail).not.toHaveBeenCalled();
    submit(harness.routeNativeElement!);
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(operations.confirmEmail).toHaveBeenCalledExactlyOnceWith('own', 'local-test-token');
    expect(TestBed.inject(Router).url).toBe('/auth/confirm-email');
    expect(harness.routeNativeElement!.textContent).toContain('Email confirmed');
  });

  it('shows a safe recovery error without rendering server diagnostics', async () => {
    operations.forgotPassword.mockRejectedValue(
      Object.assign(new AccountFailure('unavailable'), { diagnostics: 'private-sql-diagnostic' }),
    );
    const harness = await RouterTestingHarness.create('/auth/recovery');
    fill(harness.routeNativeElement!, '#account-email', 'own@brokerhub.test');
    submit(harness.routeNativeElement!);
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(harness.routeNativeElement!.textContent).toContain(
      'The service is currently unavailable',
    );
    expect(harness.routeNativeElement!.textContent).not.toContain('private-sql-diagnostic');
  });

  it('blocks duplicate register submits while the original outcome is pending', async () => {
    let finish!: () => void;
    operations.register.mockReturnValue(
      new Promise<void>((resolve) => {
        finish = resolve;
      }),
    );
    const harness = await RouterTestingHarness.create('/auth/register');
    const root = harness.routeNativeElement!;
    fill(root, '#account-email', 'own@brokerhub.test');
    fill(root, '#account-password', 'Valid!Password1');
    fill(root, '#account-phone', '+201000000001');
    submit(root);
    submit(root);
    expect(operations.register).toHaveBeenCalledTimes(1);
    finish();
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(root.textContent).toContain('Account created');
  });
});
