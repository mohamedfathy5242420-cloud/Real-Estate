import { Component, input } from '@angular/core';

export type UiStateTone =
  'foundation' | 'loading' | 'empty' | 'success' | 'error' | 'forbidden' | 'stale' | 'conflict';

export type UiStateAnnouncement = 'off' | 'polite' | 'assertive';

@Component({
  selector: 'app-ui-state',
  templateUrl: './ui-state.html',
  styleUrl: './ui-state.css',
})
export class UiState {
  readonly tone = input<UiStateTone>('foundation');
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly announcement = input<UiStateAnnouncement>('off');

  protected role(): 'alert' | 'status' | null {
    if (this.announcement() === 'assertive') {
      return 'alert';
    }

    return this.announcement() === 'polite' ? 'status' : null;
  }
}
