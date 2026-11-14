import { Injectable, PendingTasks, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ReplaySubject, catchError, defer, finalize, map, of, startWith, switchMap } from 'rxjs';
import { CatalogReader } from '../api/catalog-reader';
import { CatalogUnitDetail, UnitDetailState } from '../models/catalog';

@Injectable()
export class UnitDetailService {
  private readonly reader = inject(CatalogReader);
  private readonly pendingTasks = inject(PendingTasks);
  private readonly requests = new ReplaySubject<string>(1);
  private lastId = '';

  readonly state = toSignal(
    this.requests.pipe(
      switchMap((id) =>
        defer(() => {
          const done = this.pendingTasks.add();
          return defer(() => this.reader.getById(id)).pipe(
            map((unit): UnitDetailState =>
              unit ? { kind: 'ready', unit: publicDetail(unit) } : { kind: 'not-found' },
            ),
            catchError(() => of<UnitDetailState>({ kind: 'error' })),
            finalize(done),
            startWith<UnitDetailState>({ kind: 'loading' }),
          );
        }),
      ),
    ),
    { initialValue: { kind: 'loading' } as UnitDetailState },
  );

  load(id: string): void {
    this.lastId = id;
    this.requests.next(id);
  }

  retry(): void {
    this.requests.next(this.lastId);
  }
}

/** Keep unexpected/internal adapter fields out of the public page state. */
function publicDetail(unit: CatalogUnitDetail): CatalogUnitDetail {
  return {
    id: unit.id,
    title: unit.title,
    location: unit.location,
    project: unit.project,
    developer: unit.developer,
    bedrooms: unit.bedrooms,
    areaSquareMetres: unit.areaSquareMetres,
    price: { amount: unit.price.amount, currency: unit.price.currency },
    description: unit.description,
    features: [...unit.features],
    ...(unit.imageUrl ? { imageUrl: unit.imageUrl } : {}),
    ...(unit.imageAlt ? { imageAlt: unit.imageAlt } : {}),
    ...(unit.availability ? { availability: { ...unit.availability } } : {}),
  };
}
