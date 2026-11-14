import { Injectable, PendingTasks, inject, signal, computed } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ReplaySubject, catchError, defer, finalize, map, of, startWith, switchMap } from 'rxjs';
import { CatalogReader } from '../api/catalog-reader';
import { CatalogSearchState, CatalogFilters, CatalogQueryFailure } from '../models/catalog';

@Injectable()
export class CatalogSearchService {
  private readonly reader = inject(CatalogReader);
  private readonly pendingTasks = inject(PendingTasks);
  private readonly requests = new ReplaySubject<{
    text: string;
    page: number;
    filters: CatalogFilters;
  }>(1);
  private lastQuery = '';
  private lastPage = 1;
  private lastFilters: CatalogFilters = {};

  private readonly invalid = signal(false);
  private readonly loadedState = toSignal(
    this.requests.pipe(
      switchMap(({ text, page, filters }) =>
        defer(() => {
          const done = this.pendingTasks.add();
          return defer(() =>
            this.reader.search({
              text,
              ...(page > 1 ? { page } : {}),
              ...(Object.keys(filters).length ? { filters } : {}),
            }),
          ).pipe(
            map((result): CatalogSearchState =>
              result.items.length ? { kind: 'ready', result } : { kind: 'empty' },
            ),
            catchError((error: unknown) =>
              of<CatalogSearchState>({
                kind: error instanceof CatalogQueryFailure ? 'invalid' : 'error',
              }),
            ),
            finalize(done),
            startWith<CatalogSearchState>({ kind: 'loading' }),
          );
        }),
      ),
    ),
    { initialValue: { kind: 'loading' } as CatalogSearchState },
  );
  readonly state = computed<CatalogSearchState>(() =>
    this.invalid() ? { kind: 'invalid' } : this.loadedState(),
  );
  rejectQuery(): void {
    this.invalid.set(true);
  }

  search(text: string, page = 1, filters: CatalogFilters = {}): void {
    this.invalid.set(false);
    this.lastQuery = text;
    this.lastPage = page;
    this.lastFilters = filters;
    this.requests.next({ text, page, filters });
  }

  retry(): void {
    this.requests.next({ text: this.lastQuery, page: this.lastPage, filters: this.lastFilters });
  }
}
