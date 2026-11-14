import { DOCUMENT } from '@angular/common';
import { Component, afterNextRender, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { RouteMetadataService } from './services/route-metadata.service';
import { AccountService } from './services/account.service';

@Component({
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  private readonly document = inject(DOCUMENT);
  private readonly routeMetadata = inject(RouteMetadataService);
  protected readonly accounts = inject(AccountService);

  protected readonly title = 'BrokerHub';
  protected readonly menuOpen = signal(false);

  constructor() {
    this.document.documentElement.lang = 'en';
    this.document.documentElement.dir = 'ltr';
    this.document.body.dir = 'ltr';
    this.routeMetadata.start();
    // Only after client rendering; public SSR never initializes a private session.
    afterNextRender(() => this.accounts.start());
  }

  protected toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }

  protected focusRouteHeading(): void {
    this.closeMenu();
    queueMicrotask(() => {
      const target =
        this.document.querySelector<HTMLElement>('#route-heading') ??
        this.document.querySelector<HTMLElement>('#main-content');
      target?.focus();
    });
  }

  protected focusMainContent(): void {
    queueMicrotask(() => this.document.querySelector<HTMLElement>('#main-content')?.focus());
  }
}
