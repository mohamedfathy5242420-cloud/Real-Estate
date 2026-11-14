import { Injectable, Provider } from '@angular/core';
import { Observable, delay, of } from 'rxjs';
import { CatalogSearchQuery, CatalogSearchResult, CatalogUnitDetail } from '../models/catalog';
import { CatalogReader, CATALOG_DATA_MODE } from './catalog-reader';
import { DEMO_CATALOG } from './demo-catalog-data';

// Private aliases preserve earlier Demo bookmarks while keeping display values separate.
const LEGACY_SEARCH_ALIASES: Readonly<Record<string, string>> = {
  'new cairo': 'New Cairo',
  'sheikh zayed': 'Sheikh Zayed',
  'north coast': 'North Coast',
  cedar: 'Cedar',
  'cedar gardens': 'Cedar Gardens',
  'park avenue': 'Garden Avenue',
  'blue shore': 'Blue Shore',
  'city court': 'City Square',
};

@Injectable()
export class DemoCatalogReader implements CatalogReader {
  getById(id: string): Observable<CatalogUnitDetail | null> {
    return of(DEMO_CATALOG.find((unit) => unit.id === id) ?? null).pipe(delay(150));
  }

  search(query: CatalogSearchQuery): Observable<CatalogSearchResult> {
    const normalized = query.text.trim().toLowerCase();
    const text = (LEGACY_SEARCH_ALIASES[normalized] ?? normalized).toLowerCase();
    const items = DEMO_CATALOG.filter((property) =>
      [property.title, property.location, property.project, property.developer].some((value) =>
        value.toLowerCase().includes(text),
      ),
    );
    return of({ items, total: items.length }).pipe(delay(150));
  }
}

export function provideDemoCatalog(): Provider[] {
  return [
    { provide: CATALOG_DATA_MODE, useValue: 'demo' },
    { provide: CatalogReader, useClass: DemoCatalogReader },
  ];
}
