import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeading } from '../../components/page-heading/page-heading';
import { AccountService } from '../../services/account.service';
import { WORK_DESTINATIONS, ROLE_LABELS, canVisit } from '../../models/role-navigation';

@Component({
  selector: 'app-workspace-page',
  imports: [RouterLink, PageHeading],
  template: `
    <app-page-heading
      eyebrow="BrokerHub account"
      title="Workspace"
      description="Choose a task for your account."
    />
    @if (accounts.account(); as account) {
      <p>
        Welcome <bdi>{{ account.email }}</bdi>
      </p>
      <p>Role: {{ roleNames() }}</p>
      <nav aria-label="My account tasks">
        @for (item of destinations(); track item.path) {
          <a [routerLink]="item.path"
            ><strong>{{ item.title }}</strong
            ><span>{{ item.ready ? 'Available' : 'Coming soon' }}</span></a
          >
        } @empty {
          <p>No tasks are available for your current account roles.</p>
        }
      </nav>
    } @else {
      <p role="status">
        {{
          accounts.status() === 'loading'
            ? 'Checking session…'
            : 'Session unavailable. Sign in or check again.'
        }}
      </p>
      <a routerLink="/auth/login">Sign in</a>
    }
    <p><a routerLink="/auth/session">My account and sign out</a></p>
  `,
  styles: [
    `
      :host {
        display: block;
        max-inline-size: 64rem;
        margin: auto;
        padding: 2rem 1rem;
      }
      nav {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(min(100%, 15rem), 1fr));
        gap: 1rem;
      }
      nav a {
        display: grid;
        gap: 0.75rem;
        padding: 1.25rem;
        border: 1px solid #b5c2b9;
        border-radius: 1rem;
        background: white;
        color: #233831;
      }
      span {
        font-size: 0.9rem;
      }
      bdi {
        overflow-wrap: anywhere;
      }
      a:focus-visible {
        outline: 3px solid #947329;
        outline-offset: 3px;
      }
    `,
  ],
})
export class WorkspacePage {
  protected readonly accounts = inject(AccountService);
  protected readonly destinations = computed(() =>
    WORK_DESTINATIONS.filter((item) => canVisit(this.accounts.account(), item.roles)),
  );
  protected readonly roleNames = computed(() =>
    this.accounts
      .account()
      ?.roles.map((role) => ROLE_LABELS[role] ?? 'Unknown role')
      .join(','),
  );
}
