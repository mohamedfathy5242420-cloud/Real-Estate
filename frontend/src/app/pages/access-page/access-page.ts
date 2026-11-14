import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AccountService } from '../../services/account.service';
import { safeReturnUrl } from '../../models/role-navigation';
import { PageHeading } from '../../components/page-heading/page-heading';

@Component({
  selector: 'app-access-page',
  imports: [RouterLink, PageHeading],
  template: `
    <app-page-heading
      eyebrow="Access"
      [title]="forbidden ? 'Access denied' : 'Could not verify session'"
      description=""
    />
    <p role="alert">
      {{
        forbidden
          ? 'Your current account does not have permission for this page.'
          : 'The service is currently unavailable. No request was performed on your behalf.'
      }}
    </p>
    @if (!forbidden) {
      <button type="button" [disabled]="busy()" (click)="retry()">Check again</button>
    }
    <p>
      <a routerLink="/workspace">Workspace</a> · <a routerLink="/auth/login">Sign in</a> ·
      <a routerLink="/catalog">Catalog</a>
    </p>
  `,
  styles: [
    `
      :host {
        display: block;
        max-inline-size: 48rem;
        margin: auto;
        padding: 2rem 1rem;
      }
      button {
        font: inherit;
        padding: 0.8rem;
        min-block-size: 44px;
      }
      a {
        color: #233831;
      }
    `,
  ],
})
export class AccessPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly accounts = inject(AccountService);
  protected readonly forbidden = this.route.snapshot.queryParamMap.get('reason') === 'forbidden';
  protected readonly busy = signal(false);
  protected async retry(): Promise<void> {
    this.busy.set(true);
    try {
      await this.accounts.ensure(true);
      await this.router.navigateByUrl(
        safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl')),
      );
    } catch {
      /* Keep this honest unavailable state; no mutation is replayed. */
    } finally {
      this.busy.set(false);
    }
  }
}
