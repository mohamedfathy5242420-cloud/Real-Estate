import { Meta, Title } from '@angular/platform-browser';
import { DestroyRef, Injectable, inject } from '@angular/core';
import { ActivatedRouteSnapshot, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RoutePresentationData, isRoutePresentationData } from '../models/route-presentation';

import { CatalogUnitDetail } from '../models/catalog';
import { CATALOG_DATA_MODE } from '../api/catalog-reader';

const APP_NAME = 'BrokerHub';
const DEFAULT_DESCRIPTION = 'Discover properties and manage your requests with BrokerHub.';
const INDEXABLE_ROBOTS = 'index, follow';
const NONINDEXABLE_ROBOTS = 'noindex, nofollow';

@Injectable({ providedIn: 'root' })
export class RouteMetadataService {
  private readonly router = inject(Router);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dataMode = inject(CATALOG_DATA_MODE);
  private started = false;
  private unitPresentation: {
    id: string;
    title: string;
    description: string;
    indexable: boolean;
  } | null = null;

  start(): void {
    if (this.started) {
      return;
    }

    this.started = true;
    this.applyMetadata(this.router.routerState.snapshot.root);
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.applyMetadata(this.router.routerState.snapshot.root));
  }

  setUnitDetail(id: string, unit: CatalogUnitDetail | null): void {
    this.unitPresentation = {
      id,
      title: unit?.title || 'Unit details',
      description: unit
        ? (this.dataMode === 'demo' ? 'Demo data: ' : '') + unit.description
        : 'Explore this unit. Demo data does not confirm actual price or availability.',
      indexable: unit !== null,
    };
    this.applyMetadata(this.router.routerState.snapshot.root);
  }

  private applyMetadata(root: ActivatedRouteSnapshot): void {
    let routeData = this.findDeepestPresentationData(root);
    let leaf = root;
    while (leaf.firstChild) leaf = leaf.firstChild;
    if (
      routeData &&
      leaf.routeConfig?.path === 'catalog/:unitId' &&
      this.unitPresentation?.id === leaf.paramMap.get('unitId')
    ) {
      routeData = { ...routeData, ...this.unitPresentation };
    }
    const pageTitle = routeData?.title.trim();
    const description = routeData?.description.trim() || DEFAULT_DESCRIPTION;
    const robots =
      routeData?.indexable && this.dataMode === 'live' ? INDEXABLE_ROBOTS : NONINDEXABLE_ROBOTS;

    this.title.setTitle(pageTitle ? `${pageTitle} | ${APP_NAME}` : APP_NAME);
    this.meta.updateTag({ name: 'description', content: description }, 'name="description"');
    this.meta.updateTag({ name: 'robots', content: robots }, 'name="robots"');
  }

  private findDeepestPresentationData(root: ActivatedRouteSnapshot): RoutePresentationData | null {
    let route = root;
    while (route.firstChild) {
      route = route.firstChild;
    }

    return isRoutePresentationData(route.data) ? route.data : null;
  }
}
