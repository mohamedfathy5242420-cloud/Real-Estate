import { Injectable, inject } from '@angular/core';
import { ApiTransport } from './api-transport';
import { ApiFailure } from '../models/api-failure';
import {
  AdminConfirmation,
  AdminImage,
  CatalogEntity,
  CatalogKind,
  AdminUnit,
} from '../models/catalog-admin';
const paths: Record<CatalogKind, string> = {
  developers: '/api/developers',
  projects: '/api/projects',
  units: '/api/units',
  agreements: '/api/marketing-agreements',
};
const keys: Record<CatalogKind, readonly string[]> = {
  developers: [
    'code',
    'name',
    'legalName',
    'externalContactName',
    'externalContactEmail',
    'externalContactPhone',
    'isActive',
  ],
  projects: ['developerId', 'code', 'name', 'area', 'address', 'timeZoneId', 'isActive'],
  units: [
    'projectId',
    'code',
    'title',
    'description',
    'unitType',
    'areaSqm',
    'bedrooms',
    'bathrooms',
    'listPrice',
    'currency',
    'publicationStatus',
    'availabilityStatus',
    'confirmedPrice',
    'confirmedCurrency',
    'priceMatchesAdvertisement',
    'lastPriceConfirmedAtUtc',
    'lastAvailabilityConfirmedAtUtc',
  ],
  agreements: [
    'developerId',
    'agreementNumber',
    'scopeType',
    'startsOn',
    'endsOn',
    'protectionPeriodDays',
    'status',
    'projectIds',
    'commissionRules',
  ],
};
export const GUID_PATTERN = /^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/i;
function id(value: string): string {
  if (!GUID_PATTERN.test(value)) throw new ApiFailure('invalid');
  return value;
}
function select(value: unknown, fields: readonly string[]): Record<string, unknown> {
  if (!value || typeof value !== 'object') throw new ApiFailure('unknown');
  const raw = value as Record<string, unknown>;
  return Object.fromEntries(fields.map((key) => [key, raw[key]]));
}
export function catalogProjection(kind: CatalogKind, value: unknown): CatalogEntity {
  const result = select(value, ['id', 'version', 'createdAtUtc', 'updatedAtUtc', ...keys[kind]]);
  if (
    typeof result['id'] !== 'string' ||
    !GUID_PATTERN.test(result['id']) ||
    typeof result['version'] !== 'string' ||
    !/^[1-9]\d*$/.test(result['version'])
  )
    throw new ApiFailure('unknown');
  if (
    kind === 'units' &&
    (typeof result['areaSqm'] !== 'string' ||
      typeof result['listPrice'] !== 'string' ||
      (result['confirmedPrice'] != null && typeof result['confirmedPrice'] !== 'string'))
  )
    throw new ApiFailure('unknown');
  if (kind === 'agreements') {
    if (!Array.isArray(result['projectIds']) || !Array.isArray(result['commissionRules']))
      throw new ApiFailure('unknown');
    result['projectIds'] = Object.freeze([...result['projectIds']]);
    result['commissionRules'] = Object.freeze(
      result['commissionRules'].map((rule) => {
        const value = select(rule, [
          'projectId',
          'ruleType',
          'percentage',
          'fixedAmount',
          'currency',
        ]);
        if (
          ['percentage', 'fixedAmount'].some(
            (key) => value[key] != null && typeof value[key] !== 'string',
          )
        )
          throw new ApiFailure('unknown');
        return Object.freeze(value);
      }),
    );
  }
  return Object.freeze({ ...result, kind }) as unknown as CatalogEntity;
}
function mutationProjection(kind: CatalogKind, value: unknown): CatalogEntity {
  try {
    return catalogProjection(kind, value);
  } catch {
    throw new ApiFailure('unknown', [], true);
  }
}
function imageProjection(value: unknown): AdminImage {
  const result = select(value, ['id', 'unitId', 'altText', 'sortOrder', 'isPrimary']);
  id(String(result['id']));
  id(String(result['unitId']));
  return Object.freeze(result) as unknown as AdminImage;
}
@Injectable({ providedIn: 'root' })
export class CatalogAdminApi {
  private readonly transport = inject(ApiTransport);
  async list(kind: CatalogKind, skip = 0): Promise<CatalogEntity[]> {
    const query =
      kind === 'agreements'
        ? `?skip=${skip}&take=50`
        : kind === 'units'
          ? ''
          : '?includeInactive=true';
    const value = await this.transport.get<unknown>(paths[kind] + query);
    if (!Array.isArray(value)) throw new ApiFailure('unknown');
    return value.map((item) => catalogProjection(kind, item));
  }
  async get(kind: CatalogKind, recordId: string): Promise<CatalogEntity> {
    return catalogProjection(kind, await this.transport.get(paths[kind] + '/' + id(recordId)));
  }
  async save(
    kind: CatalogKind,
    record: CatalogEntity | null,
    body: Record<string, unknown>,
  ): Promise<CatalogEntity> {
    const value = record
      ? await this.transport.put(paths[kind] + '/' + id(record.id), {
          ...body,
          version: record.version,
        })
      : await this.transport.post(paths[kind], body);
    try {
      return catalogProjection(kind, value);
    } catch {
      throw new ApiFailure('unknown', [], true);
    }
  }
  async status(record: CatalogEntity): Promise<CatalogEntity> {
    const action =
      record.kind === 'agreements'
        ? 'activate'
        : record.kind === 'developers' || record.kind === 'projects'
          ? record.isActive
            ? 'deactivate'
            : 'reactivate'
          : null;
    if (!action) throw new ApiFailure('invalid');
    return mutationProjection(
      record.kind,
      await this.transport.post(`${paths[record.kind]}/${id(record.id)}/${action}`, {
        version: record.version,
      }),
    );
  }
  async publication(unit: AdminUnit, publish: boolean, reason: string): Promise<AdminUnit> {
    const value = await this.transport.post<{ unit: unknown }>(
      `/api/units/${id(unit.id)}/${publish ? 'publish' : 'unpublish'}`,
      { version: unit.version, reason: reason.trim() || null },
    );
    return mutationProjection('units', value?.unit) as AdminUnit;
  }
  async confirm(unit: AdminUnit, body: Record<string, unknown>): Promise<AdminUnit> {
    const value = await this.transport.post<{ unit: unknown }>(
      `/api/units/${id(unit.id)}/market-confirmations`,
      { ...body, version: unit.version },
    );
    return mutationProjection('units', value?.unit) as AdminUnit;
  }
  async confirmations(unitId: string, skip = 0): Promise<AdminConfirmation[]> {
    const value = await this.transport.get<unknown>(
      `/api/units/${id(unitId)}/market-confirmations?skip=${skip}&take=50`,
    );
    if (!Array.isArray(value)) throw new ApiFailure('unknown');
    return value.map(
      (item) =>
        Object.freeze(
          select(item, [
            'id',
            'confirmedAtUtc',
            'recordedAtUtc',
            'source',
            'sourceReference',
            'notes',
            'confirmedPrice',
            'currency',
            'availabilityStatus',
          ]),
        ) as unknown as AdminConfirmation,
    );
  }
  async images(unitId: string): Promise<{ unitVersion: string; images: AdminImage[] }> {
    const value = await this.transport.get<{ unitVersion: string; images: unknown[] }>(
      `/api/units/${id(unitId)}/images`,
    );
    if (typeof value.unitVersion !== 'string' || !Array.isArray(value.images))
      throw new ApiFailure('unknown');
    return { unitVersion: value.unitVersion, images: value.images.map(imageProjection) };
  }
  async upload(unit: AdminUnit, file: File, alt: string, order: number): Promise<void> {
    const body = new FormData();
    body.append('File', file);
    body.append('AltText', alt);
    body.append('SortOrder', String(order));
    body.append('Version', unit.version);
    await this.transport.post(`/api/units/${id(unit.id)}/images`, body);
  }
  async imageMetadata(
    unit: AdminUnit,
    imageId: string,
    altText: string,
    sortOrder: number,
  ): Promise<void> {
    await this.transport.put(`/api/units/${id(unit.id)}/images/${id(imageId)}`, {
      altText: altText.trim() || null,
      sortOrder,
      version: unit.version,
    });
  }
  imageUrl(unitId: string, imageId: string): string {
    return `/api/units/${id(unitId)}/images/${id(imageId)}/content`;
  }
}
