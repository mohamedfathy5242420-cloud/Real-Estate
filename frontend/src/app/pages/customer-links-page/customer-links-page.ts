import { Component, effect, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PageHeading } from '../../components/page-heading/page-heading';
import { CrmPager } from '../../components/crm-pager/crm-pager';
import { CrmService } from '../../services/crm.service';
import { SessionContext } from '../../services/session-context.service';
import { ApiFailure } from '../../models/api-failure';
import { CrmPage, LinkCandidate, LinkReview } from '../../models/crm';
import { crmError } from '../../models/crm-ui';
@Component({
  selector: 'app-customer-links-page',
  imports: [DatePipe, ReactiveFormsModule, RouterLink, PageHeading, CrmPager],
  template: `
    <app-page-heading
      eyebrow="Sales manager"
      title="Review account link requests"
      description="Verify the customer claim and select an unlinked profile. Matching phone numbers or emails do not grant automatic approval."
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
        <button type="button" [disabled]="busy()" (click)="reload()">Reload link requests</button>
        <h2>Requests awaiting review</h2>
        <ul>
          @for (item of reviews()?.items ?? []; track item.id) {
            <li>
              <button type="button" [disabled]="busy()" (click)="open(item.id)">
                {{ item.claim }}
              </button>
            </li>
          } @empty {
            <li>There are no pending requests on this page.</li>
          }
        </ul>
        <app-crm-pager
          label="Link review pages"
          [page]="page()"
          [total]="reviews()?.total ?? 0"
          [busy]="busy()"
          (change)="changePage($event)"
        />
        @if (current(); as review) {
          <h2>Customer claim</h2>
          <p>{{ review.claim }}</p>
          <p>
            Status:
            {{
              review.status === 'Pending'
                ? 'Awaiting review'
                : review.status === 'Approved'
                  ? 'Approved'
                  : 'Rejected'
            }}
          </p>
          <p>
            Account reference <bdi>{{ review.accountId }}</bdi> · Version {{ review.version }}
          </p>
          @if (review.status === 'Pending') {
            <button type="button" [disabled]="busy()" (click)="loadCandidates()">
              Load candidate customer profiles
            </button>
            <p>
              Candidates are profiles without linked accounts. The server checks eligibility and
              version when deciding.
            </p>
            <label for="link-target">Candidate customer profile</label
            ><select id="link-target" [formControl]="target">
              <option value="">Select a customer profile after verification</option>
              @for (candidate of candidates()?.items ?? []; track candidate.id) {
                <option [value]="candidate.id">
                  {{ candidate.fullName }} — {{ candidate.phone }} —
                  {{ candidate.email ?? 'No email' }}
                </option>
              }</select
            ><app-crm-pager
              label="Candidate profile pages"
              [page]="candidatePage()"
              [total]="candidates()?.total ?? 0"
              [busy]="busy()"
              (change)="loadCandidates($event)"
            />
            <label for="link-reason">Review decision reason</label
            ><textarea id="link-reason" [formControl]="reason" maxlength="2000" required></textarea>
            <div class="actions">
              <button type="button" [disabled]="busy() || blocked()" (click)="decide(true)">
                Approve and link selected profile</button
              ><button type="button" [disabled]="busy() || blocked()" (click)="decide(false)">
                Reject link request
              </button>
            </div>
          } @else {
            <p>Decision reason: {{ review.decisionReason }}</p>
            <p>Decision date: {{ review.decidedAtUtc | date: 'medium' }}</p>
          }
          <h3>Review history</h3>
          @for (event of review.history; track event.id) {
            <article>
              <p>
                {{
                  event.kind === 'Submitted'
                    ? 'Request submitted'
                    : event.kind === 'Approved'
                      ? 'Link approved'
                      : event.kind === 'Rejected'
                        ? 'Link rejected'
                        : 'Recorded review event'
                }}
                · {{ event.reason ?? 'No additional reason' }} ·
                {{ event.atUtc | date: 'medium' }}
              </p>
              <p>
                Actor <bdi>{{ event.actorAccountId }}</bdi>
              </p>
            </article>
          }
        }
        <nav aria-label="Manager navigation">
          <a routerLink="/admin/clients">Customers and assignment</a
          ><a routerLink="/workspace">Workspace</a>
        </nav>
      </section>
    } @else {
      <p role="alert">
        Link review session or permission is no longer available. Previous data was cleared.
      </p>
    }
  `,
  styleUrls: ['../account-page/account-page.css', '../crm-page.css'],
})
export class CustomerLinksPage {
  private readonly service = inject(CrmService);
  protected readonly session = inject(SessionContext);
  protected readonly allowed = () =>
    this.session.account()?.roles.includes('SalesManager') ?? false;
  protected readonly reviews = signal<CrmPage<LinkReview> | null>(null);
  protected readonly current = signal<LinkReview | null>(null);
  protected readonly candidates = signal<CrmPage<LinkCandidate> | null>(null);
  protected readonly page = signal(1);
  protected readonly candidatePage = signal(1);
  protected readonly busy = signal(false);
  protected readonly blocked = signal(false);
  protected readonly error = signal('');
  protected readonly message = signal('');
  protected readonly target = new FormControl('', { nonNullable: true });
  protected readonly reason = new FormControl('', {
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
        this.clear();
        this.error.set('');
        this.message.set('');
        this.busy.set(false);
      }
    });
  }
  private clear() {
    this.reviews.set(null);
    this.current.set(null);
    this.candidates.set(null);
    this.target.reset();
    this.reason.reset();
    this.blocked.set(false);
  }
  private valid(turn: number, revision: number) {
    return turn === this.turn && revision === this.session.revision();
  }
  protected async reload() {
    if (this.busy()) return;
    const id = this.current()?.id;
    this.clear();
    const turn = ++this.turn,
      revision = this.session.revision();
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    try {
      const [reviews, current] = await Promise.all([
        this.service.reviews(this.page()),
        id ? this.service.review(id) : Promise.resolve(null),
      ]);
      if (!this.valid(turn, revision)) return;
      this.reviews.set(reviews);
      this.current.set(current);
    } catch (error) {
      if (this.valid(turn, revision)) this.fail(error);
    } finally {
      if (turn === this.turn) this.busy.set(false);
    }
  }
  protected async open(id: string) {
    if (this.busy()) return;
    const turn = ++this.turn,
      revision = this.session.revision();
    this.current.set(null);
    this.candidates.set(null);
    this.target.reset();
    this.reason.reset();
    this.blocked.set(false);
    this.error.set('');
    this.message.set('');
    this.candidatePage.set(1);
    this.busy.set(true);
    try {
      const current = await this.service.review(id);
      if (this.valid(turn, revision)) this.current.set(current);
    } catch (error) {
      if (this.valid(turn, revision)) this.fail(error);
    } finally {
      if (turn === this.turn) this.busy.set(false);
    }
  }
  protected async changePage(delta: number) {
    if (this.busy()) return;
    this.page.update((page) => Math.max(1, page + delta));
    await this.reload();
  }
  protected async loadCandidates(delta = 0) {
    if (this.busy()) return;
    this.candidatePage.update((page) => Math.max(1, page + delta));
    this.candidates.set(null);
    this.target.reset();
    const turn = ++this.turn,
      revision = this.session.revision();
    this.busy.set(true);
    this.error.set('');
    try {
      const candidates = await this.service.linkCandidates(this.candidatePage());
      if (this.valid(turn, revision)) this.candidates.set(candidates);
    } catch (error) {
      if (this.valid(turn, revision)) this.fail(error);
    } finally {
      if (turn === this.turn) this.busy.set(false);
    }
  }
  protected async decide(approve: boolean) {
    const current = this.current();
    if (this.busy() || this.blocked() || !current || current.status !== 'Pending') return;
    this.reason.markAsTouched();
    const target = this.candidates()?.items.find((item) => item.id === this.target.value);
    if (this.reason.invalid || !this.reason.value.trim() || (approve && !target)) {
      this.error.set('Enter the decision reason and select a customer profile when approving.');
      return;
    }
    const turn = this.turn,
      revision = this.session.revision();
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    let saved = false;
    try {
      const review = await this.service.decideLink(current, approve, this.reason.value, target);
      saved = true;
      if (!this.valid(turn, revision)) return;
      this.current.set(review);
      this.candidates.set(null);
      this.reason.reset();
      this.target.reset();
      this.message.set(
        approve
          ? 'The profile was approved and linked to the account on the server.'
          : 'The request was rejected and the review reason was saved.',
      );
      const reviews = await this.service.reviews(this.page());
      if (this.valid(turn, revision)) this.reviews.set(reviews);
    } catch (error) {
      if (!this.valid(turn, revision)) return;
      if (
        error instanceof ApiFailure &&
        ['forbidden', 'not-found', 'unauthorized'].includes(error.kind)
      )
        this.fail(error);
      else if (saved) {
        this.blocked.set(true);
        this.error.set('The decision was saved, but the list could not refresh. Reload.');
      } else this.fail(error);
    } finally {
      if (turn === this.turn) this.busy.set(false);
    }
  }
  private fail(error: unknown) {
    if (error instanceof ApiFailure && error.kind === 'forbidden') {
      this.clear();
      this.session.clear('forbidden');
      return;
    }
    if (error instanceof ApiFailure && error.kind === 'not-found') this.clear();
    if (error instanceof ApiFailure && (error.kind === 'conflict' || error.outcomeUnknown))
      this.blocked.set(true);
    this.error.set(crmError(error));
  }
}
