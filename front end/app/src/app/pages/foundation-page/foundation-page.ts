import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PageHeading } from '../../components/page-heading/page-heading';
import { UiState } from '../../components/ui-state/ui-state';

@Component({
  selector: 'app-foundation-page',
  imports: [PageHeading, RouterLink, UiState],
  templateUrl: './foundation-page.html',
  styleUrl: './foundation-page.css',
})
export class FoundationPage {
  private readonly route = inject(ActivatedRoute);
  private readonly data = toSignal(this.route.data, { initialValue: this.route.snapshot.data });

  protected readonly eyebrow = computed(() => this.readData('eyebrow'));
  protected readonly title = computed(() => this.readData('title'));
  protected readonly description = computed(() => this.readData('description'));

  private readData(key: string): string {
    const value: unknown = this.data()[key];
    return typeof value === 'string' ? value : '';
  }
}
