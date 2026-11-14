import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { Meta, Title } from '@angular/platform-browser';
import { BehaviorSubject, Subject, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { routes } from '../../app.routes';
import { CatalogReader } from '../../api/catalog-reader';
import { provideDemoCatalog } from '../../api/demo-catalog-reader';
import { DEMO_CATALOG } from '../../api/demo-catalog-data';
import { CatalogUnitDetail } from '../../models/catalog';
import { RouteMetadataService } from '../../services/route-metadata.service';
import { UnitDetailPage } from './unit-detail-page';
import { provideHttpClient } from '@angular/common/http';

async function settle(harness: RouterTestingHarness): Promise<void> {
  await harness.fixture.whenStable();
  harness.detectChanges();
}

async function openPage(url: string): Promise<RouterTestingHarness> {
  TestBed.inject(RouteMetadataService).start();
  return RouterTestingHarness.create(url);
}

describe('English Demo unit detail page', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), ...provideDemoCatalog()],
    });
  });

  it('shows an anonymous deep link with public facts, features and matching metadata', async () => {
    const harness = await openPage('/catalog/demo-cedar-01');
    await settle(harness);
    const root = harness.routeNativeElement!;
    expect(root.querySelector('#route-heading')?.textContent).toContain('Garden apartment');
    expect(root.textContent).toContain('165 sq m');
    expect(root.querySelector('.unit-price')?.textContent).toContain('6,400,000');
    expect(root.querySelector('.unit-price')?.textContent).toContain('Egyptian pounds');
    expect(root.querySelector('.features')?.textContent).toContain('Garden views');
    expect(root.textContent).toContain('Unit availability is unconfirmed');
    expect(root.textContent).toContain('A request does not confirm a booking');
    expect(root.querySelector('form')).toBeNull();
    expect(TestBed.inject(Title).getTitle()).toBe('Garden apartment | BrokerHub');
    expect(TestBed.inject(Meta).getTag('name="description"')?.content).toContain(
      DEMO_CATALOG[0].description,
    );
    expect(TestBed.inject(Meta).getTag('name="robots"')?.content).toBe('noindex, nofollow');
  });

  it('updates unit content and metadata when the ID changes on the same route', async () => {
    const harness = await openPage('/catalog/demo-cedar-01');
    await settle(harness);
    await harness.navigateByUrl('/catalog/demo-city-01', UnitDetailPage);
    await settle(harness);
    expect(harness.routeNativeElement!.textContent).toContain('Residential studio');
    expect(harness.routeNativeElement!.textContent).not.toContain('Garden apartment');
    expect(TestBed.inject(Title).getTitle()).toBe('Residential studio | BrokerHub');
    expect(TestBed.inject(Meta).getTag('name="description"')?.content).toContain(
      DEMO_CATALOG[5].description,
    );
  });

  it('renders not-found without stale unit information or metadata', async () => {
    const harness = await openPage('/catalog/demo-cedar-01');
    await settle(harness);
    await harness.navigateByUrl('/catalog/unknown-unit', UnitDetailPage);
    await settle(harness);
    expect(harness.routeNativeElement!.textContent).toContain('Unit not found');
    expect(harness.routeNativeElement!.querySelector('.unit-price')).toBeNull();
    expect(harness.routeNativeElement!.querySelector('.account-link')).toBeNull();
    expect(TestBed.inject(Title).getTitle()).toBe('Unit details | BrokerHub');
    expect(TestBed.inject(Meta).getTag('name="description"')?.content).not.toContain(
      DEMO_CATALOG[0].description,
    );
  });

  it('supports a replacement reader and retries safely without exposing errors or private data', async () => {
    const getById = vi
      .fn()
      .mockReturnValueOnce(throwError(() => new Error('private-error-sentinel')))
      .mockReturnValueOnce(of({ ...DEMO_CATALOG[0], internalNotes: 'private-note-sentinel' }));
    TestBed.overrideProvider(CatalogReader, { useValue: { getById } });
    const harness = await openPage('/catalog/demo-cedar-01');
    await settle(harness);
    expect(harness.routeNativeElement!.textContent).toContain('Could not load unit details');
    expect(harness.routeNativeElement!.textContent).not.toContain('private-error-sentinel');
    (harness.routeNativeElement!.querySelector('.retry-button') as HTMLButtonElement).click();
    await settle(harness);
    expect(harness.routeNativeElement!.textContent).toContain('Garden apartment');
    expect(harness.routeNativeElement!.textContent).not.toContain('private-note-sentinel');
    expect(getById.mock.calls).toEqual([['demo-cedar-01'], ['demo-cedar-01']]);
  });

  it('opens the selected unit from an accessible catalog card', async () => {
    const harness = await openPage('/catalog');
    await settle(harness);
    const link = harness.routeNativeElement!.querySelector(
      '.property-card h3 a',
    ) as HTMLAnchorElement;
    expect(link.getAttribute('href')).toBe('/catalog/demo-cedar-01');
    expect(link.getAttribute('aria-label')).toBe('View details for Garden apartment');
    link.click();
    await settle(harness);
    expect(TestBed.inject(Router).url).toBe('/catalog/demo-cedar-01');
    expect(harness.routeNativeElement!.querySelector('.unit-description')?.textContent).toBe(
      DEMO_CATALOG[0].description,
    );
  });

  it('opens the real account form with safe return context and clears detail metadata', async () => {
    const harness = await openPage('/catalog/demo-cedar-01');
    await settle(harness);
    const link = harness.routeNativeElement!.querySelector(
      '.account-link[href^="/auth/login"]',
    ) as HTMLAnchorElement;
    expect(link.textContent).toContain('Open sign-in page');
    link.click();
    await settle(harness);
    const router = TestBed.inject(Router);
    expect(router.url.startsWith('/auth/login?')).toBe(true);
    expect(router.parseUrl(router.url).queryParams['returnUrl']).toBe('/catalog/demo-cedar-01');
    expect(harness.routeNativeElement!.querySelector('input[type="email"]')).toBeTruthy();
    expect(harness.routeNativeElement!.querySelector('input[type="password"]')).toBeTruthy();
    expect(TestBed.inject(Title).getTitle()).toBe('Sign in | BrokerHub');
  });

  it('renders adapter descriptions and features as text rather than executable markup', async () => {
    TestBed.overrideProvider(CatalogReader, {
      useValue: {
        getById: () =>
          of({
            ...DEMO_CATALOG[0],
            description: '<img src=x onerror=alert(1)>',
            features: ['<script>alert(1)</script>'],
          }),
      },
    });
    const harness = await openPage('/catalog/demo-cedar-01');
    await settle(harness);
    expect(harness.routeNativeElement!.querySelector('img')).toBeNull();
    expect(harness.routeNativeElement!.querySelector('script')).toBeNull();
    expect(harness.routeNativeElement!.textContent).toContain('<script>alert(1)</script>');
  });

  it('removes previous unit content immediately during a new read', () => {
    const params = new BehaviorSubject(convertToParamMap({ unitId: 'demo-cedar-01' }));
    const first = new Subject<CatalogUnitDetail | null>();
    const second = new Subject<CatalogUnitDetail | null>();
    TestBed.overrideProvider(ActivatedRoute, { useValue: { paramMap: params } });
    TestBed.overrideProvider(CatalogReader, {
      useValue: {
        getById: vi.fn().mockReturnValueOnce(first).mockReturnValueOnce(second),
      },
    });
    const fixture = TestBed.createComponent(UnitDetailPage);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Loading unit details');
    first.next(DEMO_CATALOG[0]);
    first.complete();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Garden apartment');
    params.next(convertToParamMap({ unitId: 'demo-city-01' }));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Loading unit details');
    expect(fixture.nativeElement.textContent).not.toContain('Garden apartment');
    expect(fixture.nativeElement.querySelector('.unit-price')).toBeNull();
    second.next(DEMO_CATALOG[5]);
    second.complete();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Residential studio');
  });
});
