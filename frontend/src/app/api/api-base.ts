import { InjectionToken } from '@angular/core';

/** Browser paths remain same-origin; only the server provider knows container DNS. */
export const API_BASE = new InjectionToken<string>('API base', {
  providedIn: 'root',
  factory: () => '/api',
});
