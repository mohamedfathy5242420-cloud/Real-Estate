import { Component, computed, effect, inject, signal, DestroyRef } from '@angular/core';
import {
  FormArray,
  FormControl,
  FormGroup,
  FormRecord,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { CatalogFields } from '../../components/catalog-fields/catalog-fields';
import { PageHeading } from '../../components/page-heading/page-heading';
import { CatalogAdminService } from '../../services/catalog-admin.service';
import { SessionContext } from '../../services/session-context.service';
import { ApiFailure, API_FIELD_LABELS } from '../../models/api-failure';
import {
  CATALOG_FIELDS,
  CATALOG_TITLES,
  CatalogKind,
  CatalogEntity,
  CatalogValues,
  CommissionDraft,
  AdminImage,
  AdminConfirmation,
  AdminUnit,
  formValues,
  entityLabel,
  entityTitle,
} from '../../models/catalog-admin';
import { canVisit } from '../../models/role-navigation';

function ruleForm(value?: CommissionDraft) {
  return new FormGroup({
    projectId: new FormControl(value?.projectId ?? '', { nonNullable: true }),
    ruleType: new FormControl<'Percentage' | 'FixedAmount'>(value?.ruleType ?? 'Percentage', {
      nonNullable: true,
    }),
    percentage: new FormControl(value?.percentage ?? '', { nonNullable: true }),
    fixedAmount: new FormControl(value?.fixedAmount ?? '', { nonNullable: true }),
    currency: new FormControl(value?.currency ?? '', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[A-Za-z]{3}$/)],
    }),
  });
}
@Component({
  selector: 'app-catalog-admin-page',
  imports: [ReactiveFormsModule, RouterLink, PageHeading, CatalogFields, DatePipe],
  providers: [CatalogAdminService],
  templateUrl: './catalog-admin-page.html',
  styleUrl: './catalog-admin-page.css',
})
export class CatalogAdminPage {
  private readonly service = inject(CatalogAdminService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroy = inject(DestroyRef);
  protected readonly session = inject(SessionContext);
  protected readonly allowed = computed(() =>
    canVisit(this.session.account(), ['SystemAdministrator']),
  );
  protected readonly kinds = Object.keys(CATALOG_TITLES) as CatalogKind[];
  protected readonly titles = CATALOG_TITLES;
  protected readonly kind = signal<CatalogKind>('developers');
  protected readonly fields = computed(() => CATALOG_FIELDS[this.kind()]);
  protected readonly records = signal<CatalogEntity[]>([]);
  protected readonly developers = signal<CatalogEntity[]>([]);
  protected readonly projects = signal<CatalogEntity[]>([]);
  protected readonly selected = signal<CatalogEntity | null>(null);
  protected readonly unit = computed(() =>
    this.selected()?.kind === 'units' ? (this.selected() as AdminUnit) : null,
  );
  protected readonly agreement = computed(() => {
    const record = this.selected();
    return record?.kind === 'agreements' ? record : null;
  });
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly fieldErrors = signal<readonly string[]>([]);
  protected readonly notice = signal('');
  protected readonly blocked = signal(false);
  protected readonly showInactive = signal(true);
  protected readonly listSearch = signal('');
  protected readonly skip = signal(0);
  protected readonly historySkip = signal(0);
  protected readonly images = signal<AdminImage[]>([]);
  protected readonly history = signal<AdminConfirmation[]>([]);
  protected readonly selectedProjects = signal<string[]>([]);
  protected readonly visibleRecords = computed(() =>
    this.records()
      .filter((record) => this.showInactive() || !('isActive' in record) || record.isActive)
      .filter((record) =>
        entityLabel(record).toLocaleLowerCase().includes(this.listSearch().toLocaleLowerCase()),
      ),
  );
  protected readonly form = new FormRecord<FormControl<string>>({});
  protected readonly rules = new FormArray<ReturnType<typeof ruleForm>>([]);
  protected readonly confirmation = new FormGroup({
    confirmedAtUtc: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    source: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    sourceReference: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(500)],
    }),
    notes: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(4000)] }),
    confirmedPrice: new FormControl('', { nonNullable: true }),
    currency: new FormControl('', { nonNullable: true }),
    availabilityStatus: new FormControl('', { nonNullable: true }),
  });
  protected readonly imageForm = new FormGroup({
    altText: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(500)] }),
    sortOrder: new FormControl('0', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^\d+$/)],
    }),
  });
  protected readonly reason = new FormControl('', {
    nonNullable: true,
    validators: [Validators.maxLength(500)],
  });
  protected readonly editingImage = signal<AdminImage | null>(null);
  protected readonly fileName = signal('');
  private fileInput: HTMLInputElement | null = null;
  private file: File | null = null;
  private turn = 0;
  private revision = this.session.revision();
  protected readonly titleOf = entityTitle;
  protected readonly labelOf = entityLabel;
  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroy)).subscribe((params) => {
      const kind = params.get('section') as CatalogKind;
      this.kind.set(this.kinds.includes(kind) ? kind : 'developers');
      this.skip.set(0);
      void this.load(params.get('id'));
    });
    effect(() => {
      const revision = this.session.revision();
      if (revision !== this.revision) {
        this.revision = revision;
        this.turn++;
        this.records.set([]);
        this.developers.set([]);
        this.projects.set([]);
        this.clearEditor();
        this.notice.set('');
        this.error.set('');
        this.busy.set(false);
      }
    });
  }
  protected developerProjects(): CatalogEntity[] {
    const owner = this.form.controls['developerId']?.value;
    return this.projects().filter((item) => item.kind === 'projects' && item.developerId === owner);
  }
  protected async switchKind(kind: CatalogKind): Promise<void> {
    if (this.busy()) return;
    await this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { section: kind, id: null },
    });
  }
  protected async open(record: CatalogEntity): Promise<void> {
    if (this.busy()) return;
    await this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { section: this.kind(), id: record.id },
    });
  }
  protected newRecord(): void {
    if (this.busy()) return;
    this.clearEditor();
    this.notice.set('');
    this.error.set('');
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { section: this.kind(), id: null },
    });
  }
  protected async reload(): Promise<void> {
    await this.load(this.selected()?.id ?? this.route.snapshot.queryParamMap.get('id'));
  }
  protected async listPage(delta: number): Promise<void> {
    if (this.busy()) return;
    this.skip.update((value) => Math.max(0, value + delta));
    await this.load(null);
  }
  private async load(id: string | null): Promise<void> {
    const turn = ++this.turn;
    const revision = this.session.revision();
    const kind = this.kind();
    this.busy.set(true);
    this.records.set([]);
    this.developers.set([]);
    this.projects.set([]);
    this.clearEditor();
    this.error.set('');
    this.notice.set('');
    this.blocked.set(false);
    try {
      const [refs, records, selected] = await Promise.all([
        this.service.references(),
        this.service.list(kind, this.skip()),
        id ? this.service.get(kind, id) : Promise.resolve(null),
      ]);
      if (turn !== this.turn || revision !== this.session.revision()) return;
      this.developers.set(refs.developers);
      this.projects.set(refs.projects);
      this.records.set(records);
      if (selected) {
        this.accept(selected);
        if (selected.kind === 'units') await this.loadUnitExtras(selected, turn, revision);
      }
    } catch (error) {
      if (turn === this.turn && revision === this.session.revision()) this.fail(error);
    } finally {
      if (turn === this.turn) this.busy.set(false);
    }
  }
  private clearEditor(): void {
    this.selected.set(null);
    this.images.set([]);
    this.history.set([]);
    this.historySkip.set(0);
    this.selectedProjects.set([]);
    this.rules.clear();
    this.rules.enable();
    this.editingImage.set(null);
    this.file = null;
    this.fileName.set('');
    if (this.fileInput) this.fileInput.value = '';
    this.confirmation.reset();
    this.imageForm.reset();
    this.reason.reset();
    this.blocked.set(false);
    this.fieldErrors.set([]);
    for (const key of Object.keys(this.form.controls)) this.form.removeControl(key);
    const defaults = formValues(this.kind(), null);
    for (const field of this.fields())
      this.form.addControl(
        field.key,
        new FormControl(defaults[field.key], {
          nonNullable: true,
          validators: [
            ...(field.required ? [Validators.required] : []),
            ...(field.max ? [Validators.maxLength(field.max)] : []),
          ],
        }),
      );
    if (this.kind() === 'agreements') this.rules.push(ruleForm());
  }
  private accept(record: CatalogEntity): void {
    this.selected.set(record);
    this.form.reset(formValues(record.kind, record));
    this.blocked.set(false);
    this.fieldErrors.set([]);
    for (const field of CATALOG_FIELDS[record.kind]) {
      const control = this.form.controls[field.key];
      if (field.immutable || (record.kind === 'agreements' && record.status !== 'Draft'))
        control.disable();
      else control.enable();
    }
    const rows = this.records();
    this.records.set(
      rows.some((item) => item.id === record.id)
        ? rows.map((item) => (item.id === record.id ? record : item))
        : [record, ...rows],
    );
    if (record.kind === 'developers')
      this.developers.update((rows) => [record, ...rows.filter((item) => item.id !== record.id)]);
    if (record.kind === 'projects')
      this.projects.update((rows) => [record, ...rows.filter((item) => item.id !== record.id)]);
    if (record.kind === 'agreements') {
      this.selectedProjects.set([...record.projectIds]);
      this.rules.clear();
      for (const rule of record.commissionRules)
        this.rules.push(
          ruleForm({
            projectId: rule.projectId ?? '',
            ruleType: rule.ruleType,
            percentage: rule.percentage ?? '',
            fixedAmount: rule.fixedAmount ?? '',
            currency: rule.currency,
          }),
        );
      if (record.status !== 'Draft') this.rules.disable();
      else this.rules.enable();
      this.rules.markAsPristine();
      this.rules.markAsUntouched();
    }
  }
  protected async save(): Promise<void> {
    this.form.markAllAsTouched();
    this.rules.markAllAsTouched();
    if (this.form.invalid || (this.kind() === 'agreements' && this.rules.invalid)) {
      this.error.set('Review required fields and currency format.');
      return;
    }
    await this.mutate(() =>
      this.service.save(
        this.kind(),
        this.selected(),
        this.form.getRawValue(),
        this.selectedProjects(),
        this.rules.getRawValue(),
      ),
    );
  }
  protected async changeStatus(): Promise<void> {
    const record = this.selected();
    if (record) await this.mutate(() => this.service.status(record));
  }
  protected async publish(publish: boolean): Promise<void> {
    const unit = this.unit();
    if (unit && this.reason.valid)
      await this.mutate(() => this.service.publication(unit, publish, this.reason.value));
  }
  protected async recordConfirmation(): Promise<void> {
    const unit = this.unit();
    this.confirmation.markAllAsTouched();
    if (!unit || this.confirmation.invalid) {
      this.error.set('Enter the confirmation time UTC and its source.');
      return;
    }
    await this.mutate(() => this.service.confirm(unit, this.confirmation.getRawValue()));
  }
  protected toggleProject(id: string, checked: boolean): void {
    this.selectedProjects.update((ids) =>
      checked ? [...new Set([...ids, id])] : ids.filter((value) => value !== id),
    );
    this.form.markAsDirty();
  }
  protected addRule(): void {
    if (this.rules.length < 101) {
      this.rules.push(ruleForm());
      this.form.markAsDirty();
    }
  }
  protected removeRule(index: number): void {
    this.rules.removeAt(index);
    this.form.markAsDirty();
  }
  protected chooseFile(event: Event): void {
    this.fileInput = event.target as HTMLInputElement;
    this.file = this.fileInput.files?.[0] ?? null;
    this.fileName.set(this.file?.name ?? '');
  }
  protected editImage(image: AdminImage): void {
    this.editingImage.set(image);
    this.imageForm.reset({ altText: image.altText ?? '', sortOrder: String(image.sortOrder) });
  }
  protected newImage(): void {
    this.editingImage.set(null);
    this.imageForm.reset();
    this.file = null;
    this.fileName.set('');
    if (this.fileInput) this.fileInput.value = '';
  }
  protected async saveImage(): Promise<void> {
    const unit = this.unit();
    if (!unit) return;
    this.imageForm.markAllAsTouched();
    if (this.imageForm.invalid) {
      this.error.set('Review image description and order.');
      return;
    }
    const { altText, sortOrder } = this.imageForm.getRawValue();
    const image = this.editingImage();
    await this.mutate(() =>
      image
        ? this.service.imageMetadata(unit, image.id, altText, sortOrder)
        : this.service.upload(unit, this.file, altText, sortOrder),
    );
  }
  protected imageUrl(image: AdminImage): string {
    return this.service.imageUrl(image.unitId, image.id);
  }
  protected async historyPage(delta: number): Promise<void> {
    const unit = this.unit();
    if (!unit || this.busy()) return;
    this.busy.set(true);
    const turn = this.turn;
    this.historySkip.update((value) => Math.max(0, value + delta));
    try {
      const rows = await this.service.confirmations(unit.id, this.historySkip());
      if (turn === this.turn) this.history.set(rows);
    } catch (error) {
      if (turn === this.turn) this.fail(error);
    } finally {
      if (turn === this.turn) this.busy.set(false);
    }
  }
  private async loadUnitExtras(unit: AdminUnit, turn: number, revision: number): Promise<void> {
    this.images.set([]);
    this.history.set([]);
    const [images, history] = await Promise.all([
      this.service.images(unit.id),
      this.service.confirmations(unit.id, this.historySkip()),
    ]);
    if (turn !== this.turn || revision !== this.session.revision()) return;
    if (images.unitVersion !== unit.version) {
      this.blocked.set(true);
      this.error.set('The unit changed while reading. Reload before editing.');
      return;
    }
    this.images.set(images.images);
    this.history.set(history);
  }
  private async mutate(action: () => Promise<CatalogEntity | void>): Promise<void> {
    if (this.busy() || this.blocked() || !this.allowed()) return;
    const revision = this.session.revision();
    const turn = this.turn;
    let saved = false;
    this.busy.set(true);
    this.error.set('');
    this.fieldErrors.set([]);
    this.notice.set('');
    try {
      const result = await action();
      saved = true;
      if (turn !== this.turn || revision !== this.session.revision()) return;
      if (result) this.accept(result);
      this.notice.set('The operation was saved on the server.');
      const unit = this.unit();
      if (unit) {
        const current = await this.service.get('units', unit.id);
        if (turn !== this.turn || revision !== this.session.revision()) return;
        this.accept(current);
        await this.loadUnitExtras(current as AdminUnit, turn, revision);
        this.editingImage.set(null);
        this.file = null;
        this.fileName.set('');
        if (this.fileInput) this.fileInput.value = '';
      }
    } catch (error) {
      if (turn !== this.turn || revision !== this.session.revision()) return;
      if (error instanceof ApiFailure && error.kind === 'forbidden') this.fail(error);
      else if (saved) {
        this.blocked.set(true);
        this.error.set(
          'The operation was saved, but the display could not refresh. Reload data before a new operation.',
        );
      } else this.fail(error);
    } finally {
      if (turn === this.turn) this.busy.set(false);
    }
  }
  private fail(error: unknown): void {
    if (error instanceof ApiFailure) {
      // Every catalog endpoint has the same global Administrator policy.
      if (error.kind === 'forbidden') {
        this.session.clear('forbidden');
        return;
      }
      this.fieldErrors.set(error.fields.map((field) => API_FIELD_LABELS[field]));
      if (error.kind === 'conflict') {
        this.blocked.set(true);
        this.error.set(
          'Version or eligibility conflict. Reload and review data before saving again. The request was not repeated.',
        );
      } else if (error.outcomeUnknown) {
        this.blocked.set(true);
        this.error.set(
          'The result is uncertain and may have been saved. Reload and review before submitting again.',
        );
      } else
        this.error.set(
          error.kind === 'invalid'
            ? 'Invalid data. Review required fields and values.'
            : error.kind === 'not-found'
              ? 'The record no longer exists or is unavailable.'
              : error.kind === 'unauthorized'
                ? 'Session expired. Sign in.'
                : error.kind === 'too-large'
                  ? 'The file is too large.'
                  : 'Could not load data or complete the operation. Try later.',
        );
    } else
      this.error.set('Could not complete the operation. Review the state before submitting again.');
  }
}
