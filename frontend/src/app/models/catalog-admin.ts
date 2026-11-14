export type CatalogKind = 'developers' | 'projects' | 'units' | 'agreements';
export type CatalogValues = Record<string, string>;
interface Versioned {
  readonly id: string;
  readonly version: string;
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string | null;
}
export interface AdminDeveloper extends Versioned {
  readonly kind: 'developers';
  readonly code: string;
  readonly name: string;
  readonly legalName: string | null;
  readonly externalContactName: string | null;
  readonly externalContactEmail: string | null;
  readonly externalContactPhone: string | null;
  readonly isActive: boolean;
}
export interface AdminProject extends Versioned {
  readonly kind: 'projects';
  readonly developerId: string;
  readonly code: string;
  readonly name: string;
  readonly area: string;
  readonly address: string | null;
  readonly timeZoneId: string;
  readonly isActive: boolean;
}
export interface AdminUnit extends Versioned {
  readonly kind: 'units';
  readonly projectId: string;
  readonly code: string;
  readonly title: string;
  readonly description: string | null;
  readonly unitType: string;
  readonly areaSqm: string;
  readonly bedrooms: number | null;
  readonly bathrooms: number | null;
  readonly listPrice: string;
  readonly currency: string;
  readonly publicationStatus: string;
  readonly availabilityStatus: string;
  readonly confirmedPrice: string | null;
  readonly confirmedCurrency: string | null;
  readonly priceMatchesAdvertisement: boolean;
  readonly lastPriceConfirmedAtUtc: string | null;
  readonly lastAvailabilityConfirmedAtUtc: string | null;
}
export interface CommissionDraft {
  projectId: string;
  ruleType: 'Percentage' | 'FixedAmount';
  percentage: string;
  fixedAmount: string;
  currency: string;
}
export interface AdminAgreement extends Versioned {
  readonly kind: 'agreements';
  readonly developerId: string;
  readonly agreementNumber: string;
  readonly scopeType: 'AllDeveloperProjects' | 'SelectedProjects';
  readonly startsOn: string;
  readonly endsOn: string;
  readonly protectionPeriodDays: number;
  readonly status: string;
  readonly projectIds: readonly string[];
  readonly commissionRules: readonly {
    projectId: string | null;
    ruleType: 'Percentage' | 'FixedAmount';
    percentage: string | null;
    fixedAmount: string | null;
    currency: string;
  }[];
}
export type CatalogEntity = AdminDeveloper | AdminProject | AdminUnit | AdminAgreement;
export interface AdminImage {
  readonly id: string;
  readonly unitId: string;
  readonly altText: string | null;
  readonly sortOrder: number;
  readonly isPrimary: boolean;
}
export interface AdminConfirmation {
  readonly id: string;
  readonly confirmedAtUtc: string;
  readonly recordedAtUtc: string;
  readonly source: string;
  readonly sourceReference: string | null;
  readonly notes: string | null;
  readonly confirmedPrice: string | null;
  readonly currency: string | null;
  readonly availabilityStatus: string | null;
}
export interface CatalogField {
  key: string;
  label: string;
  type?: 'text' | 'textarea' | 'decimal' | 'integer' | 'date' | 'select';
  required?: boolean;
  max?: number;
  immutable?: boolean;
  choices?: 'developers' | 'projects' | 'scope';
}
export const CATALOG_TITLES: Record<CatalogKind, string> = {
  developers: 'Developers',
  projects: 'Projects',
  units: 'Units',
  agreements: 'Marketing agreements',
};
export const CATALOG_FIELDS: Record<CatalogKind, readonly CatalogField[]> = {
  developers: [
    { key: 'code', label: 'Developer code', required: true, max: 64, immutable: true },
    { key: 'name', label: 'Developer name', required: true, max: 200 },
    { key: 'legalName', label: 'Legal name', max: 300 },
    { key: 'externalContactName', label: 'External contact person', max: 200 },
    { key: 'externalContactEmail', label: 'External contact email', max: 320 },
    { key: 'externalContactPhone', label: 'External contact phone', max: 32 },
  ],
  projects: [
    {
      key: 'developerId',
      label: 'Developer',
      type: 'select',
      choices: 'developers',
      required: true,
      immutable: true,
    },
    { key: 'code', label: 'Project code', required: true, max: 64, immutable: true },
    { key: 'name', label: 'Project name', required: true, max: 200 },
    { key: 'area', label: 'Area', required: true, max: 200 },
    { key: 'address', label: 'Address', max: 500 },
    { key: 'timeZoneId', label: 'Project timezone', required: true, max: 100 },
  ],
  units: [
    {
      key: 'projectId',
      label: 'Project',
      type: 'select',
      choices: 'projects',
      required: true,
      immutable: true,
    },
    { key: 'code', label: 'Unit code', required: true, max: 64, immutable: true },
    { key: 'title', label: 'Listing title', required: true, max: 200 },
    { key: 'description', label: 'Unit description', type: 'textarea', max: 4000 },
    { key: 'unitType', label: 'Unit type', required: true, max: 100 },
    { key: 'areaSqm', label: 'Area in square meters', type: 'decimal', required: true },
    { key: 'bedrooms', label: 'Bedrooms (optional)', type: 'integer' },
    { key: 'bathrooms', label: 'Bathrooms (optional)', type: 'integer' },
    { key: 'listPrice', label: 'List price', type: 'decimal', required: true },
    {
      key: 'currency',
      label: 'Listing currency — Three letters such as EGP',
      required: true,
      max: 3,
    },
  ],
  agreements: [
    {
      key: 'developerId',
      label: 'Developer',
      type: 'select',
      choices: 'developers',
      required: true,
      immutable: true,
    },
    { key: 'agreementNumber', label: 'Agreement number', required: true, max: 64 },
    {
      key: 'scopeType',
      label: 'Agreement scope',
      type: 'select',
      choices: 'scope',
      required: true,
    },
    { key: 'startsOn', label: 'Start date', type: 'date', required: true },
    { key: 'endsOn', label: 'End date (inclusive)', type: 'date', required: true },
    { key: 'protectionPeriodDays', label: 'Lead protection period in days', type: 'integer' },
  ],
};
export function entityTitle(value: CatalogEntity): string {
  return value.kind === 'units'
    ? value.title
    : value.kind === 'agreements'
      ? value.agreementNumber
      : value.name;
}
export function entityLabel(value: CatalogEntity): string {
  return (
    (value.kind === 'agreements' ? value.agreementNumber : value.code) + ' — ' + entityTitle(value)
  );
}
export function formValues(kind: CatalogKind, entity: CatalogEntity | null): CatalogValues {
  const raw = entity as unknown as Record<string, unknown> | null;
  return Object.fromEntries(
    CATALOG_FIELDS[kind].map((field) => [
      field.key,
      raw?.[field.key] === null
        ? ''
        : raw?.[field.key] !== undefined
          ? String(raw[field.key])
          : field.key === 'scopeType'
            ? 'AllDeveloperProjects'
            : field.key === 'timeZoneId'
              ? 'Africa/Cairo'
              : field.key === 'protectionPeriodDays'
                ? '90'
                : '',
    ]),
  );
}
