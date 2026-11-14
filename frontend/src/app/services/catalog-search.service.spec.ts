import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { CatalogReader } from '../api/catalog-reader';
import { CatalogSearchResult } from '../models/catalog';
import { CatalogSearchService } from './catalog-search.service';

const result: CatalogSearchResult = {
  total: 1,
  items: [
    {
      id: 'test-unit',
      title: 'Injected result',
      location: 'Test area',
      project: 'Test project',
      developer: 'Test developer',
      bedrooms: 2,
      areaSquareMetres: 100,
      price: { amount: 100, currency: 'EGP' },
    },
  ],
};

describe('CatalogSearchService reader boundary', () => {
  it('shows loading and accepts only the latest search response', () => {
    const first = new Subject<CatalogSearchResult>();
    const second = new Subject<CatalogSearchResult>();
    const search = vi.fn().mockReturnValueOnce(first).mockReturnValueOnce(second);
    TestBed.configureTestingModule({
      providers: [CatalogSearchService, { provide: CatalogReader, useValue: { search } }],
    });
    const service = TestBed.inject(CatalogSearchService);
    service.search('First');
    expect(service.state().kind).toBe('loading');
    service.search('Second');
    expect(first.observed).toBe(false);
    second.next(result);
    expect(service.state()).toEqual({ kind: 'ready', result });
    first.next({ items: [], total: 0 });
    expect(service.state()).toEqual({ kind: 'ready', result });
    second.complete();
    expect(search.mock.calls).toEqual([[{ text: 'First' }], [{ text: 'Second' }]]);
  });

  it('distinguishes failed reads from empty results and retries the applied query', () => {
    const search = vi
      .fn()
      .mockReturnValueOnce(throwError(() => new Error('private transport detail')))
      .mockReturnValueOnce(of({ items: [], total: 0 }));
    TestBed.configureTestingModule({
      providers: [CatalogSearchService, { provide: CatalogReader, useValue: { search } }],
    });
    const service = TestBed.inject(CatalogSearchService);
    service.search('Saved query');
    expect(service.state()).toEqual({ kind: 'error' });
    service.retry();
    expect(service.state()).toEqual({ kind: 'empty' });
    expect(search.mock.calls).toEqual([[{ text: 'Saved query' }], [{ text: 'Saved query' }]]);
  });
});
