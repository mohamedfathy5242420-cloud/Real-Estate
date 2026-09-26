import { DOCUMENT } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  private readonly document = inject(DOCUMENT);

  protected readonly title = 'BrokerHub';
  protected readonly menuOpen = signal(false);

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
