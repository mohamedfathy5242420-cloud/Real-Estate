import { Injectable, inject } from '@angular/core';
import { CatalogAdminApi, GUID_PATTERN } from '../api/catalog-admin-api';
import { ApiFailure } from '../models/api-failure';
import {
  AdminUnit,
  CatalogEntity,
  CatalogKind,
  CatalogValues,
  CommissionDraft,
} from '../models/catalog-admin';
const optional = (value: string | undefined) => value?.trim() || null;
const money = (value: string, positive = false) => {
  if (
    !/^\d{1,16}(?:\.\d{1,2})?$/.test(value) ||
    (positive && BigInt(value.replace('.', '').replace(/^0+/, '') || '0') === 0n)
  )
    throw new ApiFailure('invalid', ['form']);
  return value;
};
function integer(value: string, nullable = false): number | null {
  if (!value && nullable) return null;
  if (!/^\d+$/.test(value) || Number(value) > 2147483647) throw new ApiFailure('invalid', ['form']);
  return Number(value);
}
function parent(value: string): string {
  if (!GUID_PATTERN.test(value)) throw new ApiFailure('invalid', ['form']);
  return value;
}
@Injectable()
export class CatalogAdminService {
  private readonly api = inject(CatalogAdminApi);
  list(kind: CatalogKind, skip = 0) {
    return this.api.list(kind, skip);
  }
  get(kind: CatalogKind, id: string) {
    return this.api.get(kind, id);
  }
  async references() {
    const [developers, projects] = await Promise.all([
      this.api.list('developers'),
      this.api.list('projects'),
    ]);
    return { developers, projects };
  }
  save(
    kind: CatalogKind,
    record: CatalogEntity | null,
    v: CatalogValues,
    projectIds: readonly string[],
    rules: readonly CommissionDraft[],
  ): Promise<CatalogEntity> {
    let body: Record<string, unknown>;
    if (kind === 'developers')
      body = {
        name: v['name'].trim(),
        legalName: optional(v['legalName']),
        externalContactName: optional(v['externalContactName']),
        externalContactEmail: optional(v['externalContactEmail']),
        externalContactPhone: optional(v['externalContactPhone']),
        ...(!record ? { code: v['code'].trim() } : {}),
      };
    else if (kind === 'projects')
      body = {
        name: v['name'].trim(),
        area: v['area'].trim(),
        address: optional(v['address']),
        timeZoneId: v['timeZoneId'].trim(),
        ...(!record ? { developerId: parent(v['developerId']), code: v['code'].trim() } : {}),
      };
    else if (kind === 'units')
      body = {
        title: v['title'].trim(),
        description: optional(v['description']),
        unitType: v['unitType'].trim(),
        areaSqm: money(v['areaSqm'].trim(), true),
        bedrooms: integer(v['bedrooms'], true),
        bathrooms: integer(v['bathrooms'], true),
        listPrice: money(v['listPrice'].trim()),
        currency: v['currency'].trim().toUpperCase(),
        ...(!record ? { projectId: parent(v['projectId']), code: v['code'].trim() } : {}),
      };
    else
      body = {
        agreementNumber: v['agreementNumber'].trim(),
        scopeType: v['scopeType'],
        startsOn: v['startsOn'],
        endsOn: v['endsOn'],
        protectionPeriodDays: integer(v['protectionPeriodDays'], true),
        projectIds: v['scopeType'] === 'SelectedProjects' ? projectIds : [],
        commissionRules: rules.map((rule) => ({
          projectId: optional(rule.projectId),
          ruleType: rule.ruleType,
          percentage: rule.ruleType === 'Percentage' ? rule.percentage.trim() : null,
          fixedAmount: rule.ruleType === 'FixedAmount' ? money(rule.fixedAmount.trim()) : null,
          currency: rule.currency.trim().toUpperCase(),
        })),
        ...(!record ? { developerId: parent(v['developerId']) } : {}),
      };
    return this.api.save(kind, record, body);
  }
  status(record: CatalogEntity) {
    return this.api.status(record);
  }
  publication(unit: AdminUnit, publish: boolean, reason: string) {
    return this.api.publication(unit, publish, reason);
  }
  confirm(unit: AdminUnit, values: CatalogValues) {
    const instant = values['confirmedAtUtc'].trim();
    if (
      !/^\d{4}-\d\d-\d\dT\d\d:\d\d(?::\d\d(?:\.\d+)?)?Z$/.test(instant) ||
      !Number.isFinite(Date.parse(instant))
    )
      throw new ApiFailure('invalid', ['form']);
    return this.api.confirm(unit, {
      confirmedAtUtc: instant,
      source: values['source'].trim(),
      sourceReference: optional(values['sourceReference']),
      notes: optional(values['notes']),
      confirmedPrice: values['confirmedPrice'].trim()
        ? money(values['confirmedPrice'].trim())
        : null,
      currency: optional(values['currency'])?.toUpperCase() ?? null,
      availabilityStatus: optional(values['availabilityStatus']),
    });
  }
  confirmations(id: string, skip = 0) {
    return this.api.confirmations(id, skip);
  }
  images(id: string) {
    return this.api.images(id);
  }
  upload(unit: AdminUnit, file: File | null, alt: string, order: string) {
    if (!file || file.size > 5 * 1024 * 1024) throw new ApiFailure('invalid', ['form']);
    return this.api.upload(unit, file, alt, integer(order)!);
  }
  imageMetadata(unit: AdminUnit, imageId: string, alt: string, order: string) {
    return this.api.imageMetadata(unit, imageId, alt, integer(order)!);
  }
  imageUrl(unitId: string, imageId: string) {
    return this.api.imageUrl(unitId, imageId);
  }
}
