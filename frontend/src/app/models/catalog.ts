/** Frontend display models. These are not Backend DTOs or Business state enums. */
export interface CatalogProperty {
  readonly id: string;
  readonly title: string;
  readonly location: string;
  readonly project: string;
  readonly developer: string;
  readonly projectId?: string;
  readonly developerId?: string;
  readonly bedrooms: number | null;
  readonly areaSquareMetres: number | string;
  readonly price: { readonly amount: number | string; readonly currency: string };
  readonly imageUrl?: string;
  readonly imageAlt?: string;
  readonly availability?: {
    readonly status: string;
    readonly freshness: string;
    readonly confirmedAtUtc: string | null;
  };
}

export interface CatalogSearchQuery {
  readonly text: string;
  readonly page?: number;
  readonly filters?: CatalogFilters;
}
export interface CatalogFilters {
  readonly area?: string;
  readonly unitType?: string;
  readonly projectId?: string;
  readonly developerId?: string;
  readonly bedrooms?: string;
  readonly minPrice?: string;
  readonly maxPrice?: string;
  readonly currency?: string;
  readonly minArea?: string;
  readonly maxArea?: string;
  readonly sort?: string;
  readonly pageSize?: string;
}
export const CATALOG_FILTER_KEYS = [
  'area',
  'unitType',
  'projectId',
  'developerId',
  'bedrooms',
  'minPrice',
  'maxPrice',
  'currency',
  'minArea',
  'maxArea',
  'sort',
  'pageSize',
] as const;
export class CatalogQueryFailure extends Error {}

export interface CatalogSearchResult {
  readonly items: readonly CatalogProperty[];
  readonly total: number;
  readonly page?: number;
  readonly pageSize?: number;
}

export type CatalogSearchState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'invalid' }
  | { readonly kind: 'empty' }
  | { readonly kind: 'ready'; readonly result: CatalogSearchResult };

export interface CatalogUnitDetail extends CatalogProperty {
  readonly description: string;
  readonly features: readonly string[];
}

export type UnitDetailState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'not-found' }
  | { readonly kind: 'ready'; readonly unit: CatalogUnitDetail };
