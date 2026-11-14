import { DisplayNumberPipe } from '../../pipes/display-format.pipe';
import {
  Component,
  DestroyRef,
  RESPONSE_INIT,
  effect,
  inject,
  signal,
  computed,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PageHeading } from '../../components/page-heading/page-heading';
import { PropertyVisual } from '../../components/property-visual/property-visual';
import { UiState } from '../../components/ui-state/ui-state';
import { PropertyCard } from '../../components/property-card/property-card';
import { CatalogSearchService } from '../../services/catalog-search.service';
import { CATALOG_DATA_MODE } from '../../api/catalog-reader';
import { CATALOG_FILTER_KEYS, CatalogFilters } from '../../models/catalog';
import { validateCatalogFilters } from '../../models/catalog-filters';

@Component({
  selector: 'app-catalog-page',
  imports: [
    PageHeading,
    UiState,
    PropertyCard,
    PropertyVisual,
    ReactiveFormsModule,
    DisplayNumberPipe,
    RouterLink,
  ],
  providers: [CatalogSearchService],
  templateUrl: './catalog-page.html',
  styleUrl: './catalog-page.css',
})
export class CatalogPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly catalog = inject(CatalogSearchService);
  protected readonly demo = inject(CATALOG_DATA_MODE) === 'demo';
  protected readonly searchForm = new FormGroup({
    query: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(this.demo ? 120 : 200)],
    }),
  });
  protected readonly filterForm = new FormGroup(
    Object.fromEntries(
      CATALOG_FILTER_KEYS.map((key) => [key, new FormControl('', { nonNullable: true })]),
    ),
  );
  protected readonly filterError = signal('');
  protected readonly projectOptions = computed(() => {
    const state = this.catalog.state();
    return state.kind === 'ready'
      ? [
          ...new Map(
            state.result.items
              .filter((item) => item.projectId)
              .map((item) => [item.projectId!, item.project]),
          ).entries(),
        ]
      : [];
  });
  protected readonly developerOptions = computed(() => {
    const state = this.catalog.state();
    return state.kind === 'ready'
      ? [
          ...new Map(
            state.result.items
              .filter((item) => item.developerId)
              .map((item) => [item.developerId!, item.developer]),
          ).entries(),
        ]
      : [];
  });
  protected readonly appliedQuery = signal('');
  protected readonly submitted = signal(false);
  protected readonly navigationError = signal(false);
  protected readonly page = signal(1);
  private readonly response = inject(RESPONSE_INIT, { optional: true });

  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const query = (params.get('q') ?? '').trim();
      const page = Number(params.get('page') ?? '1');
      this.page.set(Number.isInteger(page) && page >= 1 && page <= 10000 ? page : 1);
      this.searchForm.controls.query.setValue(query);
      this.appliedQuery.set(query);
      this.submitted.set(false);
      const filters = Object.fromEntries(
        CATALOG_FILTER_KEYS.map((key) => [key, (params.get(key) ?? '').trim()]).filter(
          ([, value]) => value,
        ),
      ) as CatalogFilters;
      this.filterForm.reset(
        Object.fromEntries(CATALOG_FILTER_KEYS.map((key) => [key, filters[key] ?? ''])),
      );
      const duplicate = ['q', 'page', ...CATALOG_FILTER_KEYS].some(
        (key) => params.getAll(key).length > 1,
      );
      const error = validateCatalogFilters(filters);
      this.filterError.set(error);
      if (
        !this.demo &&
        (duplicate ||
          query.length > 200 ||
          error ||
          !Number.isInteger(page) ||
          page < 1 ||
          page > 10000)
      )
        this.catalog.rejectQuery();
      else this.catalog.search(query, this.page(), this.demo ? {} : filters);
    });
    effect(() => {
      if (this.response && this.catalog.state().kind === 'error') this.response.status = 503;
      if (this.response && this.catalog.state().kind === 'invalid') this.response.status = 400;
    });
  }

  protected applySearch(): void {
    this.submitted.set(true);
    if (this.searchForm.invalid) return;
    this.filterError.set(this.demo ? '' : validateCatalogFilters(this.filterForm.getRawValue()));
    if (this.filterError()) return;
    void this.navigateQuery(this.searchForm.controls.query.value.trim() || null);
  }

  protected resetSearch(): void {
    void this.navigateQuery(null, true);
  }

  private async navigateQuery(query: string | null, reset = false): Promise<void> {
    this.navigationError.set(false);
    const filters = this.demo
      ? {}
      : Object.fromEntries(
          CATALOG_FILTER_KEYS.map((key) => [
            key,
            reset ? null : this.filterForm.controls[key].value.trim() || null,
          ]),
        );
    const queryParams = { ...filters, q: query, page: null };
    const target = this.router.createUrlTree([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
    });
    if (this.router.serializeUrl(target) === this.router.url) {
      this.searchForm.controls.query.setValue(query ?? '');
      this.appliedQuery.set(query ?? '');
      this.submitted.set(false);
      if (reset) {
        this.filterForm.reset();
        this.filterError.set('');
      }
      return;
    }
    try {
      const navigated = await this.router.navigate([], {
        relativeTo: this.route,
        queryParams,
        queryParamsHandling: 'merge',
      });
      if (navigated) {
        this.searchForm.controls.query.setValue(query ?? '');
        this.appliedQuery.set(query ?? '');
        this.submitted.set(false);
      } else {
        this.navigationError.set(true);
      }
    } catch {
      this.navigationError.set(true);
    }
  }
}
