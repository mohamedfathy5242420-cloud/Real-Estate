import { Component, OnInit, effect, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Account, AccountFailure, AccountFailureKind, AccountField } from '../../models/account';
import { AccountService } from '../../services/account.service';
import { PageHeading } from '../../components/page-heading/page-heading';
import { SessionContext } from '../../services/session-context.service';
import { safeReturnUrl } from '../../models/role-navigation';

type Mode = 'login' | 'register' | 'recovery' | 'confirm-email' | 'reset-password' | 'session';
const TITLES: Record<Mode, string> = {
  login: 'Sign in',
  register: 'Create account',
  recovery: 'Reset password',
  'confirm-email': 'Confirm email',
  'reset-password': 'Set a new password',
  session: 'My account',
};

@Component({
  selector: 'app-account-page',
  imports: [ReactiveFormsModule, RouterLink, PageHeading],
  templateUrl: './account-page.html',
  styleUrl: './account-page.css',
})
export class AccountPage implements OnInit {
  private readonly accounts = inject(AccountService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly session = inject(SessionContext);
  protected readonly mode = (this.route.snapshot.data['mode'] ?? 'login') as Mode;
  protected readonly title = TITLES[this.mode];
  protected readonly busy = signal(false);
  protected readonly message = signal('');
  protected readonly error = signal('');
  protected readonly fieldErrors = signal<string[]>([]);
  protected readonly account = signal<Account | null>(null);
  protected readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true }),
    password: new FormControl('', { nonNullable: true }),
    phoneNumber: new FormControl('', { nonNullable: true }),
    rememberMe: new FormControl(false, { nonNullable: true }),
  });

  constructor() {
    effect(() => {
      this.session.revision();
      this.account.set(null);
      this.form.controls.password.reset();
    });
  }

  async ngOnInit(): Promise<void> {
    if (['login', 'register', 'recovery', 'reset-password'].includes(this.mode)) {
      this.form.controls.email.setValidators([Validators.required, Validators.email]);
    }
    if (['login', 'register', 'reset-password'].includes(this.mode)) {
      this.form.controls.password.setValidators([
        Validators.required,
        ...(this.mode === 'login' ? [] : [Validators.minLength(8)]),
      ]);
    }
    if (this.mode === 'register')
      this.form.controls.phoneNumber.setValidators([Validators.required]);
    this.form.controls.email.setValue(this.route.snapshot.queryParamMap.get('email') ?? '');
    Object.values(this.form.controls).forEach((control) => control.updateValueAndValidity());
    if (this.mode === 'session') await this.loadSession();
  }

  protected async submit(): Promise<void> {
    if (this.busy()) return;
    this.form.markAllAsTouched();
    this.form.updateValueAndValidity();
    if (this.form.invalid) {
      this.error.set(
        'Review email and required fields. The new password must be at least 8 characters.',
      );
      return;
    }
    this.busy.set(true);
    this.error.set('');
    this.fieldErrors.set([]);
    this.message.set('');
    const { email, password, phoneNumber, rememberMe } = this.form.getRawValue();
    try {
      if (this.mode === 'login') {
        await this.accounts.login(email, password, rememberMe);
        this.form.controls.password.reset();
        const candidate = this.route.snapshot.queryParamMap.get('returnUrl') ?? '';
        // Explicit destinations only; destination guards recheck roles, never replay commands.
        const destination = safeReturnUrl(candidate);
        await this.router.navigateByUrl(destination);
      } else if (this.mode === 'register') {
        await this.accounts.register(email, password, phoneNumber);
        this.form.controls.password.reset();
        this.message.set(
          'Account created. Confirm your email before signing in. Locally, the confirmation link is saved in private Identity files; no real email is sent.',
        );
      } else if (this.mode === 'recovery') {
        await this.accounts.forgotPassword(email);
        this.message.set(
          'If the account is eligible, a password reset link was prepared. Check the local delivery method.',
        );
      } else if (this.mode === 'confirm-email') {
        const token = this.route.snapshot.queryParamMap.get('token');
        const accountId = this.route.snapshot.queryParamMap.get('accountId');
        if (!token || !accountId)
          throw new Error('The link is incomplete. Open the full confirmation link.');
        await this.accounts.confirmEmail(accountId, token);
        await this.clearLink();
        this.message.set('Email confirmed. You can sign in now.');
      } else if (this.mode === 'reset-password') {
        const token = this.route.snapshot.queryParamMap.get('token');
        if (!token) throw new Error('The reset link is incomplete. Request a new one.');
        await this.accounts.resetPassword(email, token, password);
        this.form.controls.password.reset();
        await this.clearLink();
        this.message.set('Password changed. Sign in with the new password.');
      }
    } catch (error: unknown) {
      this.showError(error);
    } finally {
      this.busy.set(false);
    }
  }

  protected async resend(): Promise<void> {
    if (this.busy()) return;
    if (!this.form.controls.email.valid || !this.form.controls.email.value) {
      this.error.set('Enter your email first.');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    try {
      await this.accounts.resendConfirmation(this.form.controls.email.value);
      this.message.set('If the account is eligible, a new confirmation link was prepared.');
    } catch (error: unknown) {
      this.showError(error);
    } finally {
      this.busy.set(false);
    }
  }

  protected async logout(): Promise<void> {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    try {
      await this.accounts.logout();
      this.account.set(null);
      await this.router.navigateByUrl('/auth/login');
    } catch (error: unknown) {
      this.showError(error);
    } finally {
      this.busy.set(false);
    }
  }

  private async loadSession(): Promise<void> {
    this.busy.set(true);
    try {
      this.account.set(await this.accounts.me());
    } catch (error: unknown) {
      this.showError(error);
    } finally {
      this.busy.set(false);
    }
  }
  private clearLink(): Promise<boolean> {
    return this.router.navigate([], { relativeTo: this.route, queryParams: {}, replaceUrl: true });
  }
  private showError(error: unknown): void {
    if (error instanceof AccountFailure) {
      const messages: Record<AccountFailureKind, string> = {
        network:
          'Could not reach the server. Try later without repeating any unresolved operation.',
        invalid: 'Invalid data or confirmation link. Check the fields or request a new link.',
        unauthorized: 'Invalid sign-in details or expired session. Sign in again.',
        forbidden: 'Email confirmation is required, or permission is missing.',
        locked: 'The account is temporarily locked after sign-in attempts. Try later.',
        unavailable:
          'The service is currently unavailable. The operation may have been saved; review its state or prepare the confirmation link again after configuring local delivery.',
        stale:
          'The session changed during the request. Previous session data was not displayed; review the result before repeating any operation.',
        conflict:
          'The data changed or the operation conflicts. Read the current state before trying again.',
        'not-found': 'The requested resource was not found.',
        'too-large': 'The request is too large.',
        unknown: 'Could not complete the operation. Review its state before submitting again.',
      };
      this.error.set(messages[error.kind]);
      const labels: Partial<Record<AccountField, string>> = {
        email: 'Email',
        password: 'Password',
        phoneNumber: 'Phone number',
        role: 'Role',
        form: 'Review form data',
      };
      this.fieldErrors.set(error.fields.map((field) => labels[field] ?? 'Form data'));
    } else {
      this.error.set(error instanceof Error ? error.message : 'Could not complete the operation.');
    }
  }
}
