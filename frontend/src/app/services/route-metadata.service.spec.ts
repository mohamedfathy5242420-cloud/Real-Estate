import { Component } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { TestBed } from '@angular/core/testing';
import { Router, Routes, provideRouter } from '@angular/router';
import { RoutePresentationData } from '../models/route-presentation';
import { RouteMetadataService } from './route-metadata.service';
import { CATALOG_DATA_MODE } from '../api/catalog-reader';

@Component({ template: '' })
class EmptyPage {}

const publicData: RoutePresentationData = {
  eyebrow: 'Public route',
  title: 'Property catalog',
  description: 'Browse published BrokerHub properties.',
  indexable: true,
};

const privateData: RoutePresentationData = {
  eyebrow: 'Private route',
  title: 'Customer account',
  description: 'A protected customer route.',
  indexable: false,
};

const testRoutes: Routes = [
  { path: 'catalog', component: EmptyPage, data: publicData },
  { path: 'account', component: EmptyPage, data: privateData },
  {
    path: '**',
    component: EmptyPage,
    data: {
      eyebrow: 'Navigation',
      title: 'Page not found',
      description: 'The requested page does not exist.',
      indexable: false,
    } satisfies RoutePresentationData,
  },
];

describe('RouteMetadataService', () => {
  let router: Router;
  let title: Title;
  let meta: Meta;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter(testRoutes), { provide: CATALOG_DATA_MODE, useValue: 'live' }],
    });

    router = TestBed.inject(Router);
    title = TestBed.inject(Title);
    meta = TestBed.inject(Meta);
    TestBed.inject(RouteMetadataService).start();
  });

  it('marks the public catalog route as indexable', async () => {
    await router.navigateByUrl('/catalog');

    expect(title.getTitle()).toBe('Property catalog | BrokerHub');
    expect(meta.getTag('name="description"')?.content).toBe(publicData.description);
    expect(meta.getTag('name="robots"')?.content).toBe('index, follow');
  });

  it('keeps private and unknown routes out of search indexes', async () => {
    await router.navigateByUrl('/account');
    expect(meta.getTag('name="robots"')?.content).toBe('noindex, nofollow');

    await router.navigateByUrl('/missing');
    expect(title.getTitle()).toBe('Page not found | BrokerHub');
    expect(meta.getTag('name="robots"')?.content).toBe('noindex, nofollow');
  });
});

describe('Demo route metadata', () => {
  it('keeps fictional public catalog pages nonindexed in the safe default mode', async () => {
    TestBed.configureTestingModule({ providers: [provideRouter(testRoutes)] });
    TestBed.inject(RouteMetadataService).start();
    await TestBed.inject(Router).navigateByUrl('/catalog');
    expect(TestBed.inject(Meta).getTag('name="robots"')?.content).toBe('noindex, nofollow');
  });
});
