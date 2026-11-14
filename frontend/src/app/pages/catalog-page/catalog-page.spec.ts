import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { CatalogPage } from './catalog-page';
import { of, throwError } from 'rxjs';
import { CatalogReader } from '../../api/catalog-reader';
import { DEMO_CATALOG } from '../../api/demo-catalog-data';
import { provideDemoCatalog } from '../../api/demo-catalog-reader';

function inputOn(harness: RouterTestingHarness): HTMLInputElement {
  return harness.routeNativeElement!.querySelector('#catalog-query') as HTMLInputElement;
}

async function enterSearch(harness: RouterTestingHarness, text: string): Promise<void> {
  const input = inputOn(harness);
  input.value = text;
  input.dispatchEvent(new Event('input'));
  harness
    .routeNativeElement!.querySelector('form')!
    .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
  await harness.fixture.whenStable();
  harness.detectChanges();
}

describe('CatalogPage URL search', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'catalog', component: CatalogPage }]),
        ...provideDemoCatalog(),
      ],
    }),
  );

  it('restores a deep-linked query and clearly labels fictional sample data', async () => {
    const harness = await RouterTestingHarness.create('/catalog?q=New%20Cairo');
    expect(inputOn(harness).value).toBe('New Cairo');
    expect(harness.routeNativeElement!.textContent).toContain('Saved search:');
    expect(harness.routeNativeElement!.textContent).toContain('fictional examples');
  });

  it('applies trimmed text to the URL without dropping unrelated parameters', async () => {
    const harness = await RouterTestingHarness.create('/catalog?source=bookmark');
    await enterSearch(harness, '  Garden city  ');
    const router = TestBed.inject(Router);
    expect(router.parseUrl(router.url).queryParams).toEqual({
      source: 'bookmark',
      q: 'Garden city',
    });
    expect(inputOn(harness).value).toBe('Garden city');
  });

  it('restores the input when the query changes on the same route', async () => {
    const harness = await RouterTestingHarness.create('/catalog?q=First');
    await harness.navigateByUrl('/catalog?q=Second', CatalogPage);
    expect(inputOn(harness).value).toBe('Second');
    await harness.navigateByUrl('/catalog?q=First', CatalogPage);
    expect(inputOn(harness).value).toBe('First');
  });

  it('resets both saved and unsaved input while retaining unrelated parameters', async () => {
    const harness = await RouterTestingHarness.create('/catalog?q=Saved&source=bookmark');
    const input = inputOn(harness);
    input.value = 'Draft';
    input.dispatchEvent(new Event('input'));
    (harness.routeNativeElement!.querySelector('.reset-button') as HTMLButtonElement).click();
    await harness.fixture.whenStable();
    harness.detectChanges();
    const router = TestBed.inject(Router);
    expect(router.parseUrl(router.url).queryParams).toEqual({ source: 'bookmark' });
    expect(inputOn(harness).value).toBe('');
    expect(harness.routeNativeElement!.textContent).not.toContain('Saved search:');
  });

  it('rejects excessive text without changing the saved query', async () => {
    const harness = await RouterTestingHarness.create('/catalog?q=Saved');
    await enterSearch(harness, 'x'.repeat(121));
    expect(TestBed.inject(Router).url).toBe('/catalog?q=Saved');
    expect(inputOn(harness).getAttribute('aria-invalid')).toBe('true');
    expect(harness.routeNativeElement!.textContent).toContain('120 characters or fewer');
  });

  it('clears an unsaved draft when reset leaves the URL unchanged', async () => {
    const harness = await RouterTestingHarness.create('/catalog?source=bookmark');
    inputOn(harness).value = 'Draft';
    inputOn(harness).dispatchEvent(new Event('input'));
    (harness.routeNativeElement!.querySelector('.reset-button') as HTMLButtonElement).click();
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(inputOn(harness).value).toBe('');
    expect(TestBed.inject(Router).url).toBe('/catalog?source=bookmark');
    expect(harness.routeNativeElement!.textContent).not.toContain('Could not save search');
  });

  it('preserves the draft and saved query when navigation is cancelled', async () => {
    const harness = await RouterTestingHarness.create('/catalog?q=Saved');
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(false);
    await enterSearch(harness, 'Draft');
    expect(inputOn(harness).value).toBe('Draft');
    expect(TestBed.inject(Router).url).toBe('/catalog?q=Saved');
    expect(harness.routeNativeElement!.textContent).toContain('Could not save search');
    vi.restoreAllMocks();
  });
  it('renders URL text as text and never as markup', async () => {
    const harness = await RouterTestingHarness.create(
      '/catalog?q=%3Cscript%3Ealert(1)%3C%2Fscript%3E',
    );
    expect(inputOn(harness).value).toBe('<script>alert(1)</script>');
    expect(harness.routeNativeElement!.querySelector('script')).toBeNull();
    expect(harness.routeNativeElement!.textContent).toContain('fictional examples');
  });

  it('renders only the location-matching sample cards from an applied deep link', async () => {
    const harness = await RouterTestingHarness.create('/catalog?q=New%20Cairo');
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(harness.routeNativeElement!.querySelectorAll('app-property-card').length).toBe(3);
    expect(harness.routeNativeElement!.textContent).toContain('3 demo properties');
    expect(harness.routeNativeElement!.textContent).toContain('Illustrative price');
    expect(harness.routeNativeElement!.textContent).not.toContain('Coastal chalet');
  });

  it('shows a successful empty state for an unmatched query', async () => {
    const harness = await RouterTestingHarness.create('/catalog?q=unmatched-sample-query');
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(harness.routeNativeElement!.querySelectorAll('app-property-card').length).toBe(0);
    expect(harness.routeNativeElement!.textContent).toContain(
      'No demo properties match your search',
    );
    expect(harness.routeNativeElement!.textContent).not.toContain('Could not load properties');
  });

  it('does not change results for an unsaved draft', async () => {
    const harness = await RouterTestingHarness.create('/catalog');
    await harness.fixture.whenStable();
    inputOn(harness).value = 'New Cairo';
    inputOn(harness).dispatchEvent(new Event('input'));
    harness.detectChanges();
    expect(harness.routeNativeElement!.querySelectorAll('app-property-card').length).toBe(6);
    expect(TestBed.inject(Router).url).toBe('/catalog');
  });

  it('accepts a replacement reader and retries without leaking its error or private fields', async () => {
    const privateResult = {
      ...DEMO_CATALOG[0],
      title: 'Replacement reader result',
      internalNotes: 'private-sales-sentinel',
    };
    const search = vi
      .fn()
      .mockReturnValueOnce(throwError(() => new Error('private-error-sentinel')))
      .mockReturnValueOnce(of({ items: [privateResult], total: 1 }));
    TestBed.overrideProvider(CatalogReader, { useValue: { search } });
    const harness = await RouterTestingHarness.create('/catalog?q=Saved');
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(harness.routeNativeElement!.textContent).toContain('Could not load properties');
    expect(harness.routeNativeElement!.textContent).not.toContain('private-error-sentinel');
    (harness.routeNativeElement!.querySelector('.retry-button') as HTMLButtonElement).click();
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(harness.routeNativeElement!.textContent).toContain('Replacement reader result');
    expect(harness.routeNativeElement!.textContent).not.toContain('private-sales-sentinel');
    expect(TestBed.inject(Router).url).toBe('/catalog?q=Saved');
  });
});
