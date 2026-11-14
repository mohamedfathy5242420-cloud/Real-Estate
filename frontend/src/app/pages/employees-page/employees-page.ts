import { Component, effect, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { PageHeading } from '../../components/page-heading/page-heading';
import { CrmService } from '../../services/crm.service';
import { SessionContext } from '../../services/session-context.service';
import { EmployeeProfile, EmployeeRevision, CrmPage } from '../../models/crm';
import { ApiFailure } from '../../models/api-failure';
import { crmError } from '../../models/crm-ui';
@Component({
  selector: 'app-employees-page',
  imports: [ReactiveFormsModule, RouterLink, DatePipe, PageHeading],
  styleUrls: ['../account-page/account-page.css', '../crm-page.css'],
  template: `
    <app-page-heading
      eyebrow="System administration"
      title="Employee profiles"
      description="Link a confirmed staff account to its employee profile. Profile status is separate from sign-in status and does not transfer customers automatically."
    />
    @if (allowed()) {
      <section class="account-panel" [attr.aria-busy]="busy()">
        @if (busy()) {
          <p role="status">Contacting the server…</p>
        }
        @if (error()) {
          <p role="alert" class="message error">{{ error() }}</p>
        }
        @if (message()) {
          <p role="status" class="message success">{{ message() }}</p>
        }
        <nav aria-label="Employee tools">
          <button type="button" [disabled]="busy()" (click)="reload()">Reload employees</button
          ><button type="button" [disabled]="busy()" (click)="newEmployee()">
            New employee profile</button
          ><a routerLink="/admin/staff-accounts">Create sign-in account</a>
        </nav>
        <h2>Profile list</h2>
        <ul>
          @for (item of employees()?.items ?? []; track item.id) {
            <li>
              <button type="button" [disabled]="busy()" (click)="open(item.id)">
                {{ item.employeeNumber }} — {{ item.displayName }} ({{
                  item.isActive ? 'Active' : 'Inactive'
                }})
              </button>
            </li>
          } @empty {
            <li>There are no profiles on this page.</li>
          }
        </ul>
        <nav aria-label="Employee pages">
          <button type="button" [disabled]="busy() || page() <= 1" (click)="listPage(-1)">
            Previous</button
          ><button
            type="button"
            [disabled]="busy() || page() * 20 >= (employees()?.total ?? 0)"
            (click)="listPage(1)"
          >
            Next
          </button>
        </nav>
        <form [formGroup]="form" (ngSubmit)="save()">
          <h2>{{ current() ? 'Edit employee profile' : 'Create employee profile' }}</h2>
          <p class="hint">
            The account must be active, email-confirmed and have a sales employee or manager role.
            Its reference comes from account creation or its owner; there is no public sign-in
            account list.
          </p>
          <label for="employee-account">Sign-in account reference</label
          ><input id="employee-account" formControlName="accountId" dir="ltr" required />
          <label for="employee-number">Employee number</label
          ><input
            id="employee-number"
            formControlName="employeeNumber"
            maxlength="64"
            dir="ltr"
            required
          />
          <label for="employee-name">Employee display name</label
          ><input id="employee-name" formControlName="displayName" maxlength="200" required />
          @if (current(); as selected) {
            <p>
              Profile version: <bdi>{{ selected.version }}</bdi>
            </p>
            <label class="checkbox"
              ><input type="checkbox" formControlName="isActive" />Active profile eligible for new
              assignments</label
            >
          }
          @if (form.touched && form.invalid) {
            <p role="alert">Review account reference, number and name.</p>
          }
          <button type="submit" [disabled]="busy() || blocked()">
            {{ current() ? 'Save employee profile' : 'Create employee profile' }}
          </button>
        </form>
        @if (current()) {
          <h2>Profile change history</h2>
          @for (entry of history()?.items ?? []; track entry.id) {
            <article>
              <p>
                {{ entry.displayName }} · {{ entry.isActive ? 'Active' : 'Inactive' }} · Version
                <bdi>{{ entry.version }}</bdi>
              </p>
              <p>
                {{ entry.atUtc | date: 'medium' }} · Actor <bdi>{{ entry.actorAccountId }}</bdi>
              </p>
            </article>
          } @empty {
            <p>There are no changes on this page.</p>
          }
          <nav aria-label="Employee history pages">
            <button
              type="button"
              [disabled]="busy() || historyPage() <= 1"
              (click)="changeHistory(-1)"
            >
              Previous history</button
            ><button
              type="button"
              [disabled]="busy() || historyPage() * 20 >= (history()?.total ?? 0)"
              (click)="changeHistory(1)"
            >
              Next history
            </button>
          </nav>
        }
        <nav aria-label="Administration navigation"><a routerLink="/workspace">Workspace</a></nav>
      </section>
    } @else {
      <p role="alert">
        Employee profile management session or permission is no longer available. Previous data was
        cleared.
      </p>
      <a routerLink="/auth/login">Sign in</a>
    }
  `,
})
export class EmployeesPage {
  private readonly service = inject(CrmService);
  private readonly route = inject(ActivatedRoute);
  protected readonly session = inject(SessionContext);
  protected readonly allowed = () =>
    this.session.account()?.roles.includes('SystemAdministrator') ?? false;
  protected readonly employees = signal<CrmPage<EmployeeProfile> | null>(null);
  protected readonly current = signal<EmployeeProfile | null>(null);
  protected readonly history = signal<CrmPage<EmployeeRevision> | null>(null);
  protected readonly page = signal(1);
  protected readonly historyPage = signal(1);
  protected readonly busy = signal(false);
  protected readonly blocked = signal(false);
  protected readonly error = signal('');
  protected readonly message = signal('');
  protected readonly form = new FormGroup({
    accountId: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.pattern(/^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/i),
      ],
    }),
    employeeNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(64)],
    }),
    displayName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    isActive: new FormControl(true, { nonNullable: true }),
  });
  private turn = 0;
  private revision = this.session.revision();
  constructor() {
    const accountId = this.route.snapshot.queryParamMap.get('accountId');
    if (accountId) this.form.controls.accountId.setValue(accountId);
    void this.reload();
    effect(() => {
      const revision = this.session.revision();
      if (revision !== this.revision) {
        this.revision = revision;
        this.turn++;
        this.employees.set(null);
        this.current.set(null);
        this.history.set(null);
        this.form.reset();
        this.error.set('');
        this.message.set('');
        this.busy.set(false);
      }
    });
  }
  protected newEmployee(): void {
    if (this.busy()) return;
    this.turn++;
    this.current.set(null);
    this.history.set(null);
    this.historyPage.set(1);
    this.form.enable();
    this.form.reset();
    this.blocked.set(false);
    this.error.set('');
    this.message.set('');
  }
  protected async reload(): Promise<void> {
    if (this.busy()) return;
    const turn = ++this.turn,
      revision = this.session.revision(),
      id = this.current()?.id;
    this.busy.set(true);
    this.employees.set(null);
    this.current.set(null);
    this.history.set(null);
    this.error.set('');
    this.message.set('');
    this.blocked.set(false);
    if (id) {
      this.form.enable();
      this.form.reset();
    }
    try {
      const items = await this.service.employees(this.page());
      if (turn !== this.turn || revision !== this.session.revision()) return;
      this.employees.set(items);
      if (id) await this.readEmployee(id, turn, revision);
    } catch (error) {
      if (turn === this.turn) this.fail(error);
    } finally {
      if (turn === this.turn) this.busy.set(false);
    }
  }
  protected async open(id: string): Promise<void> {
    if (this.busy()) return;
    const turn = ++this.turn,
      revision = this.session.revision();
    this.busy.set(true);
    this.current.set(null);
    this.history.set(null);
    this.form.reset();
    this.error.set('');
    this.message.set('');
    this.blocked.set(false);
    this.historyPage.set(1);
    try {
      await this.readEmployee(id, turn, revision);
    } catch (error) {
      if (turn === this.turn) this.fail(error);
    } finally {
      if (turn === this.turn) this.busy.set(false);
    }
  }
  private async readEmployee(id: string, turn: number, revision: number) {
    const [profile, history] = await Promise.all([
      this.service.employee(id),
      this.service.employeeHistory(id, this.historyPage()),
    ]);
    if (turn !== this.turn || revision !== this.session.revision()) return;
    this.accept(profile);
    this.history.set(history);
  }
  private accept(profile: EmployeeProfile) {
    this.current.set(profile);
    this.form.reset({
      accountId: profile.accountId,
      employeeNumber: profile.employeeNumber,
      displayName: profile.displayName,
      isActive: profile.isActive,
    });
    this.form.controls.accountId.disable();
    this.form.controls.employeeNumber.disable();
  }
  protected async save(): Promise<void> {
    if (this.busy() || this.blocked()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const turn = this.turn,
      revision = this.session.revision();
    let saved = false;
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    try {
      const profile = await this.service.saveEmployee(this.current(), this.form.getRawValue());
      saved = true;
      if (turn !== this.turn || revision !== this.session.revision()) return;
      this.accept(profile);
      this.message.set(
        'The employee profile was saved on the server; the sign-in account did not change.',
      );
      const [items, history] = await Promise.all([
        this.service.employees(this.page()),
        this.service.employeeHistory(profile.id, this.historyPage()),
      ]);
      if (turn === this.turn && revision === this.session.revision()) {
        this.employees.set(items);
        this.history.set(history);
      }
    } catch (error) {
      if (turn === this.turn) {
        if (error instanceof ApiFailure && error.kind === 'forbidden') this.fail(error);
        else if (saved) {
          this.blocked.set(true);
          this.error.set(
            'The profile was saved, but the list could not refresh. Reload before another edit.',
          );
        } else this.fail(error);
      }
    } finally {
      if (turn === this.turn) this.busy.set(false);
    }
  }
  protected async listPage(delta: number) {
    this.page.update((value) => Math.max(1, value + delta));
    await this.reload();
  }
  protected async changeHistory(delta: number) {
    this.historyPage.update((value) => Math.max(1, value + delta));
    await this.reload();
  }
  private fail(error: unknown) {
    if (error instanceof ApiFailure && error.kind === 'forbidden') {
      this.session.clear('forbidden');
      return;
    }
    if (error instanceof ApiFailure && (error.kind === 'conflict' || error.outcomeUnknown))
      this.blocked.set(true);
    this.error.set(crmError(error));
  }
}
