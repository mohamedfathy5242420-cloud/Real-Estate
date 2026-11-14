import { Component, effect, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { PageHeading } from '../../components/page-heading/page-heading';
import { CustomerFields } from '../../components/customer-fields/customer-fields';
import { CrmService } from '../../services/crm.service';
import { SessionContext } from '../../services/session-context.service';
import { ApiFailure } from '../../models/api-failure';
import { CustomerProfile, CrmPage, OwnLink, customerValues } from '../../models/crm';
import { customerForm, crmError } from '../../models/crm-ui';
@Component({
  selector: 'app-customer-profile-page',
  imports: [ReactiveFormsModule, RouterLink, DatePipe, PageHeading, CustomerFields],
  styleUrls: ['../account-page/account-page.css', '../crm-page.css'],
  template: `
    <app-page-heading
      eyebrow="Customer account"
      title="My profile and account links"
      description="Your contact profile is separate from your sign-in account. Linking an existing profile requires manager review and is never automatic."
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
        <button type="button" [disabled]="busy()" (click)="reload()">Reload my profile</button>
        @if (loaded()) {
          @if (profile(); as current) {
            <p>
              Your current profile: {{ current.fullName }} · Version
              <bdi>{{ current.version }}</bdi>
            </p>
          } @else {
            <p>
              No customer profile is linked to your account. Create a profile or request review of
              an existing profile link.
            </p>
          }
          <form [formGroup]="form" (ngSubmit)="save()">
            <h2>{{ profile() ? 'Edit my details' : 'Create my profile' }}</h2>
            <app-customer-fields [form]="form" /><button
              type="submit"
              [disabled]="busy() || blocked()"
            >
              {{ profile() ? 'Save my details' : 'Create customer profile' }}
            </button>
          </form>
          @if (!profile()) {
            <section>
              <h2>Request link to an existing profile</h2>
              <p>
                Provide information for the manager to verify. Matching phone numbers or emails do
                not create a link.
              </p>
              <label for="customer-link-claim">Link request information</label
              ><textarea
                id="customer-link-claim"
                [formControl]="claim"
                maxlength="2000"
                rows="4"
              ></textarea
              ><button type="button" [disabled]="busy() || blocked()" (click)="submitLink()">
                Submit link request for review
              </button>
            </section>
          }
          <section>
            <h2>My link requests</h2>
            @for (item of links()?.items ?? []; track item.id) {
              <article>
                <p>{{ item.claim }}</p>
                <p>
                  Status:
                  {{
                    item.status === 'Pending'
                      ? 'Awaiting review'
                      : item.status === 'Approved'
                        ? 'Approved'
                        : 'Rejected'
                  }}
                </p>
                <p>{{ item.createdAtUtc | date: 'medium' }}</p>
              </article>
            } @empty {
              <p>There are no link requests on this page.</p>
            }
            <nav aria-label="Link request pages">
              <button type="button" [disabled]="busy() || page() <= 1" (click)="linkPage(-1)">
                Previous</button
              ><button
                type="button"
                [disabled]="busy() || page() * 20 >= (links()?.total ?? 0)"
                (click)="linkPage(1)"
              >
                Next
              </button>
            </nav>
          </section>
        }
        <nav aria-label="Customer navigation">
          <a routerLink="/workspace">Workspace</a><a routerLink="/catalog">Catalog</a>
        </nav>
      </section>
    } @else {
      <p role="alert">
        Customer session or permission is no longer available. Previous data was cleared.
      </p>
      <a routerLink="/auth/login">Sign in</a>
    }
  `,
})
export class CustomerProfilePage {
  private readonly service = inject(CrmService);
  protected readonly session = inject(SessionContext);
  protected readonly allowed = () => this.session.account()?.roles.includes('Customer') ?? false;
  protected readonly profile = signal<CustomerProfile | null>(null);
  protected readonly links = signal<CrmPage<OwnLink> | null>(null);
  protected readonly busy = signal(false);
  protected readonly loaded = signal(false);
  protected readonly error = signal('');
  protected readonly message = signal('');
  protected readonly blocked = signal(false);
  protected readonly page = signal(1);
  protected readonly form = customerForm();
  protected readonly claim = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(2000)],
  });
  private turn = 0;
  private revision = this.session.revision();
  constructor() {
    void this.reload();
    effect(() => {
      const revision = this.session.revision();
      if (revision !== this.revision) {
        this.revision = revision;
        this.turn++;
        this.profile.set(null);
        this.links.set(null);
        this.form.reset();
        this.claim.reset();
        this.loaded.set(false);
        this.busy.set(false);
        this.message.set('');
        this.error.set('');
      }
    });
  }
  protected async reload(): Promise<void> {
    const turn = ++this.turn,
      revision = this.session.revision();
    this.busy.set(true);
    this.profile.set(null);
    this.links.set(null);
    this.loaded.set(false);
    this.error.set('');
    this.message.set('');
    this.blocked.set(false);
    this.form.reset();
    try {
      const [profile, links] = await Promise.all([
        this.service.own(),
        this.service.ownLinks(this.page()),
      ]);
      if (turn !== this.turn || revision !== this.session.revision()) return;
      this.profile.set(profile);
      this.form.reset(customerValues(profile));
      this.links.set(links);
      this.loaded.set(true);
    } catch (error) {
      if (turn === this.turn) this.fail(error);
    } finally {
      if (turn === this.turn) this.busy.set(false);
    }
  }
  protected async save(): Promise<void> {
    if (this.busy() || this.blocked()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const turn = this.turn,
      revision = this.session.revision();
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    try {
      const profile = await this.service.saveOwn(this.profile(), this.form.getRawValue());
      if (turn !== this.turn || revision !== this.session.revision()) return;
      this.profile.set(profile);
      this.form.reset(customerValues(profile));
      this.message.set('Your profile was saved on the server.');
    } catch (error) {
      if (turn === this.turn) this.fail(error);
    } finally {
      if (turn === this.turn) this.busy.set(false);
    }
  }
  protected async submitLink(): Promise<void> {
    if (this.busy() || this.blocked()) return;
    this.claim.markAsTouched();
    if (this.claim.invalid) {
      this.error.set('Enter link request information, up to 2,000 characters.');
      return;
    }
    const turn = this.turn,
      revision = this.session.revision();
    let saved = false;
    this.busy.set(true);
    this.error.set('');
    try {
      await this.service.submitLink(this.claim.value);
      saved = true;
      if (turn !== this.turn || revision !== this.session.revision()) return;
      this.claim.reset();
      this.message.set(
        'Link request submitted. The manager decides approval and selects the candidate profile.',
      );
      const links = await this.service.ownLinks(this.page());
      if (turn === this.turn && revision === this.session.revision()) this.links.set(links);
    } catch (error) {
      if (turn === this.turn) {
        if (error instanceof ApiFailure && error.kind === 'forbidden') this.fail(error);
        else if (saved) {
          this.blocked.set(true);
          this.error.set('The link request was saved, but the list could not refresh. Reload.');
        } else this.fail(error);
      }
    } finally {
      if (turn === this.turn) this.busy.set(false);
    }
  }
  protected async linkPage(delta: number): Promise<void> {
    this.page.update((value) => Math.max(1, value + delta));
    await this.reload();
  }
  private fail(error: unknown): void {
    if (error instanceof ApiFailure && error.kind === 'forbidden') {
      this.session.clear('forbidden');
      return;
    }
    if (error instanceof ApiFailure && (error.kind === 'conflict' || error.outcomeUnknown))
      this.blocked.set(true);
    this.error.set(crmError(error));
  }
}
