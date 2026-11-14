import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { CatalogReader } from '../api/catalog-reader';
import { DEMO_CATALOG } from '../api/demo-catalog-data';
import { CatalogUnitDetail } from '../models/catalog';
import { UnitDetailService } from './unit-detail.service';

describe('UnitDetailService read states', () => {
  it('clears previous content and accepts only the latest unit read', () => {
    const first = new Subject<CatalogUnitDetail | null>();
    const second = new Subject<CatalogUnitDetail | null>();
    const getById = vi.fn().mockReturnValueOnce(first).mockReturnValueOnce(second);
    TestBed.configureTestingModule({
      providers: [UnitDetailService, { provide: CatalogReader, useValue: { getById } }],
    });
    const service = TestBed.inject(UnitDetailService);
    service.load(DEMO_CATALOG[0].id);
    first.next(DEMO_CATALOG[0]);
    expect(service.state()).toEqual({ kind: 'ready', unit: DEMO_CATALOG[0] });
    service.load(DEMO_CATALOG[1].id);
    expect(service.state()).toEqual({ kind: 'loading' });
    expect(first.observed).toBe(false);
    second.next(DEMO_CATALOG[1]);
    first.next(DEMO_CATALOG[0]);
    expect(service.state()).toEqual({ kind: 'ready', unit: DEMO_CATALOG[1] });
    second.complete();
  });

  it('distinguishes not-found from read failure and retries the current ID', () => {
    const getById = vi
      .fn()
      .mockReturnValueOnce(throwError(() => new Error('internal-transport-sentinel')))
      .mockReturnValueOnce(of(null));
    TestBed.configureTestingModule({
      providers: [UnitDetailService, { provide: CatalogReader, useValue: { getById } }],
    });
    const service = TestBed.inject(UnitDetailService);
    service.load('missing-unit');
    expect(service.state()).toEqual({ kind: 'error' });
    service.retry();
    expect(service.state()).toEqual({ kind: 'not-found' });
    expect(getById.mock.calls).toEqual([['missing-unit'], ['missing-unit']]);
  });

  it('projects only public fields into state, including nested price fields', () => {
    const injected = {
      ...DEMO_CATALOG[0],
      internalNotes: 'private-note',
      price: { ...DEMO_CATALOG[0].price, internalCommission: 999 },
    };
    TestBed.configureTestingModule({
      providers: [
        UnitDetailService,
        { provide: CatalogReader, useValue: { getById: () => of(injected) } },
      ],
    });
    const service = TestBed.inject(UnitDetailService);
    service.load(injected.id);
    expect(service.state()).toEqual({ kind: 'ready', unit: DEMO_CATALOG[0] });
    expect(JSON.stringify(service.state())).not.toContain('internalNotes');
    expect(JSON.stringify(service.state())).not.toContain('internalCommission');
  });
});
