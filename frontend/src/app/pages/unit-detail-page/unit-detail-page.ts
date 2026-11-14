import {
  Component,
  DestroyRef,
  RESPONSE_INIT,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PageHeading } from '../../components/page-heading/page-heading';
import { PropertyVisual } from '../../components/property-visual/property-visual';
import { UiState } from '../../components/ui-state/ui-state';
import { DisplayCurrencyPipe, DisplayNumberPipe } from '../../pipes/display-format.pipe';
import { UnitDetailService } from '../../services/unit-detail.service';
import { RouteMetadataService } from '../../services/route-metadata.service';
import { CATALOG_DATA_MODE } from '../../api/catalog-reader';

@Component({
  selector: 'app-unit-detail-page',
  imports: [
    RouterLink,
    PageHeading,
    PropertyVisual,
    UiState,
    DisplayCurrencyPipe,
    DisplayNumberPipe,
  ],
  providers: [UnitDetailService],
  templateUrl: './unit-detail-page.html',
  styleUrl: './unit-detail-page.css',
})
export class UnitDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly metadata = inject(RouteMetadataService);
  private readonly unitId = signal('');
  private readonly response = inject(RESPONSE_INIT, { optional: true });
  protected readonly detail = inject(UnitDetailService);
  protected readonly demo = inject(CATALOG_DATA_MODE) === 'demo';
  protected readonly title = computed(() => {
    const state = this.detail.state();
    return state.kind === 'ready' ? state.unit.title : 'Unit details';
  });
  protected readonly subtitle = computed(() => {
    const state = this.detail.state();
    return state.kind === 'ready'
      ? state.unit.location + ' · ' + state.unit.project
      : 'Review property details before making a request.';
  });
  protected readonly unitRoute = computed(() => '/catalog/' + encodeURIComponent(this.unitId()));

  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const id = params.get('unitId') ?? '';
      this.unitId.set(id);
      this.detail.load(id);
    });
    effect(() => {
      const state = this.detail.state();
      if (this.response && state.kind === 'not-found') this.response.status = 404;
      if (this.response && state.kind === 'error') this.response.status = 503;
      this.metadata.setUnitDetail(this.unitId(), state.kind === 'ready' ? state.unit : null);
    });
  }
}
