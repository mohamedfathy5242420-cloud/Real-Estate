import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of, throwError } from 'rxjs';
import { API_BASE } from './api-base';
import { CatalogReader } from './catalog-reader';
import {
  CatalogProperty,
  CatalogSearchQuery,
  CatalogSearchResult,
  CatalogUnitDetail,
  CatalogQueryFailure,
  CATALOG_FILTER_KEYS,
} from '../models/catalog';

interface PublicImage {
  id: string;
  altText: string | null;
  contentUrl: string;
}
interface PublicUnit {
  id: string;
  title: string;
  projectName: string;
  developerName: string;
  projectId: string;
  developerId: string;
  area: string;
  bedrooms: number | null;
  areaSqm: string;
  listPrice: string;
  currency: string;
  description?: string | null;
  availability: {
    status: string;
    freshness: string;
    confirmedAtUtc: string | null;
    requiresExternalConfirmation: boolean;
  };
  previewImage?: PublicImage | null;
  images?: PublicImage[];
}
const GUID = /^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/i;
const DECIMAL = /^\d+(?:\.\d+)?$/;

/** Explicit public projection: unexpected management/private fields never enter view state. */
function project(unit: PublicUnit): CatalogProperty {
  if (
    !GUID.test(unit.id) ||
    typeof unit.listPrice !== 'string' ||
    !DECIMAL.test(unit.listPrice) ||
    typeof unit.areaSqm !== 'string' ||
    !DECIMAL.test(unit.areaSqm) ||
    !/^[A-Z]{3}$/.test(unit.currency)
  ) {
    throw new Error('Invalid public catalog contract');
  }
  return {
    id: unit.id,
    title: unit.title,
    location: unit.area,
    project: unit.projectName,
    developer: unit.developerName,
    projectId: GUID.test(unit.projectId) ? unit.projectId : undefined,
    developerId: GUID.test(unit.developerId) ? unit.developerId : undefined,
    bedrooms: unit.bedrooms,
    areaSquareMetres: unit.areaSqm,
    price: { amount: unit.listPrice, currency: unit.currency },
    availability: {
      status: unit.availability.status,
      freshness: unit.availability.freshness,
      confirmedAtUtc: unit.availability.confirmedAtUtc,
    },
    imageUrl: imagePath(unit.id, unit.previewImage),
    imageAlt: unit.previewImage?.altText ?? undefined,
  };
}
function imagePath(unitId: string, image: PublicImage | null | undefined): string | undefined {
  if (!image || !GUID.test(image.id)) return undefined;
  const expected = `/api/catalog/units/${unitId}/images/${image.id}/content`;
  return image.contentUrl === expected ? expected : undefined;
}

@Injectable()
export class HttpCatalogReader implements CatalogReader {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE);

  search(query: CatalogSearchQuery): Observable<CatalogSearchResult> {
    let params = new HttpParams()
      .set('q', query.text)
      .set('page', query.page ?? 1)
      .set('pageSize', query.filters?.pageSize ?? 20);
    for (const key of CATALOG_FILTER_KEYS) {
      const value = query.filters?.[key];
      if (key !== 'pageSize' && value) params = params.set(key, value);
    }
    return this.http
      .get<{ items: PublicUnit[]; totalCount: number; page: number; pageSize: number }>(
        `${this.base}/catalog/units`,
        { params, credentials: 'omit', transferCache: false },
      )
      .pipe(
        map((page) => ({
          items: page.items.map(project),
          total: page.totalCount,
          page: page.page,
          pageSize: page.pageSize,
        })),
        catchError((error: unknown) =>
          throwError(() =>
            error instanceof HttpErrorResponse && error.status === 400
              ? new CatalogQueryFailure()
              : error,
          ),
        ),
      );
  }

  getById(id: string): Observable<CatalogUnitDetail | null> {
    if (!GUID.test(id)) return of(null);
    return this.http
      .get<PublicUnit>(`${this.base}/catalog/units/${id}`, {
        credentials: 'omit',
        transferCache: false,
      })
      .pipe(
        map((unit) => ({
          ...project(unit),
          description: unit.description ?? '',
          features: [],
          imageUrl: imagePath(unit.id, unit.images?.[0]),
          imageAlt: unit.images?.[0]?.altText ?? undefined,
        })),
        catchError((error: unknown) =>
          error instanceof HttpErrorResponse && error.status === 404
            ? of(null)
            : throwError(() => error),
        ),
      );
  }
}
