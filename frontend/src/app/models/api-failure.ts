export type ApiFailureKind =
  | 'network'
  | 'invalid'
  | 'unauthorized'
  | 'forbidden'
  | 'locked'
  | 'unavailable'
  | 'stale'
  | 'conflict'
  | 'not-found'
  | 'too-large'
  | 'unknown';
export const API_FIELD_LABELS = {
  email: 'Email',
  password: 'Password',
  phoneNumber: 'Phone number',
  role: 'Role',
  form: 'Form data',
  code: 'Code',
  name: 'Name',
  legalName: 'Legal name',
  externalContactName: 'Contact person',
  externalContactEmail: 'Contact email',
  externalContactPhone: 'Contact phone',
  developerId: 'Developer',
  projectId: 'Project',
  projectIds: 'Covered projects',
  area: 'Area',
  address: 'Address',
  timeZoneId: 'Timezone',
  title: 'Listing title',
  description: 'Description',
  unitType: 'Unit type',
  areaSqm: 'Area',
  bedrooms: 'Bedrooms',
  bathrooms: 'Bathrooms',
  listPrice: 'List price',
  currency: 'Currency',
  version: 'Current version',
  agreementNumber: 'Agreement number',
  scopeType: 'Agreement scope',
  startsOn: 'Start date',
  endsOn: 'End date',
  protectionPeriodDays: 'Protection period',
  commissionRules: 'Commission rules',
  confirmedAtUtc: 'Confirmation time UTC',
  source: 'Confirmation source',
  sourceReference: 'Confirmation reference',
  notes: 'Notes',
  confirmedPrice: 'Confirmed price',
  availabilityStatus: 'Confirmed availability',
  altText: 'Image description',
  sortOrder: 'Image order',
  file: 'Image file',
} as const;
export type ApiField = keyof typeof API_FIELD_LABELS;

/** Only safe categories cross the HTTP boundary; raw diagnostics never reach views. */
export class ApiFailure extends Error {
  constructor(
    readonly kind: ApiFailureKind,
    readonly fields: readonly ApiField[] = [],
    readonly outcomeUnknown = false,
  ) {
    super('API operation failed');
  }
}
