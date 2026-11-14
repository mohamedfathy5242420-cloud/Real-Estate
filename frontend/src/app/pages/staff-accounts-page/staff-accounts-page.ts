import { Component, effect, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PageHeading } from '../../components/page-heading/page-heading';
import { StaffAccountsService } from '../../services/staff-accounts.service';
import { SessionContext } from '../../services/session-context.service';
import { ApiFailure, ApiField } from '../../models/api-failure';
import { ROLE_LABELS, STAFF_ROLES, StaffRole, canVisit } from '../../models/role-navigation';
import { StaffAccountCreated } from '../../models/staff-account';

@Component({
  selector: 'app-staff-accounts-page',
  imports: [PageHeading, ReactiveFormsModule, RouterLink],
  templateUrl: './staff-accounts-page.html',
  styleUrl: '../account-page/account-page.css',
})
export class StaffAccountsPage {
  private readonly service = inject(StaffAccountsService);
  protected readonly session = inject(SessionContext);
  protected readonly roles = STAFF_ROLES;
  protected readonly labels = ROLE_LABELS;
  protected readonly canCreate = () => canVisit(this.session.account(), ['SystemAdministrator']);
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly fields = signal<string[]>([]);
  protected readonly result = signal<StaffAccountCreated | null>(null);
  protected readonly form = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(8)],
    }),
    phoneNumber: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    role: new FormControl<StaffRole>('SalesEmployee', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });
  constructor() {
    effect(() => {
      this.session.revision();
      this.result.set(null);
      this.error.set('');
      this.fields.set([]);
      this.form.reset();
    });
  }
  protected async submit(): Promise<void> {
    if (this.busy() || !this.canCreate()) return;
    this.error.set('');
    this.fields.set([]);
    this.result.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('Review email, phone, password and role.');
      return;
    }
    this.busy.set(true);
    const revision = this.session.revision();
    try {
      const result = await this.service.create(this.form.getRawValue());
      if (revision === this.session.revision()) this.result.set(result);
    } catch (error: unknown) {
      if (revision !== this.session.revision()) return;
      const labels: Partial<Record<ApiField, string>> = {
        email: 'Email',
        password: 'Password',
        phoneNumber: 'Phone number',
        role: 'Role',
        form: 'Form data',
      };
      if (error instanceof ApiFailure) {
        this.fields.set(error.fields.map((field) => labels[field] ?? 'Form data'));
        this.error.set(
          error.outcomeUnknown
            ? 'Account creation result is uncertain. The account may have been saved; review its confirmation link or prepare one from sign-in before submitting again.'
            : error.kind === 'forbidden'
              ? 'Your account cannot create staff accounts.'
              : error.kind === 'unauthorized'
                ? 'Your session expired. Sign in again.'
                : error.kind === 'invalid'
                  ? 'The server rejected the data. Review the fields; the email may already be in use.'
                  : 'Could not connect or complete the operation. Try later.',
        );
      } else
        this.error.set('Could not determine the creation result. Review before submitting again.');
    } finally {
      this.form.controls.password.reset();
      this.busy.set(false);
    }
  }
}
