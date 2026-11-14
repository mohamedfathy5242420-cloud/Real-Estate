import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { HttpCatalogReader } from './http-catalog-reader';

const id = '10000000-0000-0000-0000-000000000001';
const publicUnit = {
  id,
  title: 'Unit',
  projectName: 'Project',
  developerName: 'Developer',
  area: 'Cairo',
  bedrooms: null,
  areaSqm: '123.45',
  listPrice: '9999999999999999.99',
  currency: 'EGP',
  availability: {
    status: 'Unknown',
    freshness: 'unconfirmed',
    confirmedAtUtc: null,
    requiresExternalConfirmation: true,
  },
  salesNotes: 'private unexpected data',
  version: '9007199254740993',
};

describe('Live catalog contract and privacy', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), HttpCatalogReader],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('keeps maximum money exact, nullable bedrooms and only public display fields', async () => {
    const result = firstValueFrom(
      TestBed.inject(HttpCatalogReader).search({ text: 'Cairo', page: 2 }),
    );
    const request = http.expectOne(
      (r) => r.url === '/api/catalog/units' && r.params.get('page') === '2',
    );
    expect(request.request.credentials).toBe('omit');
    expect(request.request.transferCache).toBe(false);
    request.flush({ items: [publicUnit], totalCount: 21, page: 2, pageSize: 20 });
    const page = await result;
    expect(page.items[0].price.amount).toBe('9999999999999999.99');
    expect(page.items[0].bedrooms).toBeNull();
    expect(JSON.stringify(page)).not.toContain('private unexpected data');
    expect(JSON.stringify(page)).not.toContain('9007199254740993');
    expect(page.total).toBe(21);
  });

  it('maps a hidden/missing detail to null while retaining outage as a retryable error', async () => {
    const reader = TestBed.inject(HttpCatalogReader);
    const missing = firstValueFrom(reader.getById(id));
    http.expectOne(`/api/catalog/units/${id}`).flush({}, { status: 404, statusText: 'Not Found' });
    expect(await missing).toBeNull();
    const failed = firstValueFrom(reader.getById(id));
    const rejection = expect(failed).rejects.toMatchObject({ status: 503 });
    http
      .expectOne(`/api/catalog/units/${id}`)
      .flush({}, { status: 503, statusText: 'Unavailable' });
    await rejection;
  });

  it('rejects a lossy numeric-money contract and arbitrary image URLs', async () => {
    const reader = TestBed.inject(HttpCatalogReader);
    const failed = firstValueFrom(reader.getById(id));
    const rejection = expect(failed).rejects.toThrow('Invalid public catalog contract');
    http
      .expectOne(`/api/catalog/units/${id}`)
      .flush({ ...publicUnit, listPrice: 9999999999999999 });
    await rejection;
    const valid = firstValueFrom(reader.getById(id));
    http
      .expectOne(`/api/catalog/units/${id}`)
      .flush({ ...publicUnit, images: [{ id, contentUrl: 'https://untrusted.invalid/image' }] });
    expect((await valid)?.imageUrl).toBeUndefined();
  });
});
