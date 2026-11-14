import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { CatalogSearchQuery, CatalogSearchResult, CatalogUnitDetail } from '../models/catalog';

/** Replace this read boundary with a DTO-mapping HTTP adapter when its contract exists. */
export abstract class CatalogReader {
  abstract search(query: CatalogSearchQuery): Observable<CatalogSearchResult>;
  abstract getById(id: string): Observable<CatalogUnitDetail | null>;
}

/** Safe default: sample public pages must not be indexed. */
export const CATALOG_DATA_MODE = new InjectionToken<'demo' | 'live'>('Catalog data mode', {
  providedIn: 'root',
  factory: () => 'demo',
});
