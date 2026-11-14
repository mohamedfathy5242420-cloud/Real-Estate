/** Explicit scalar projections of inspected P09 sales DTOs plus minimal integration sale-unit read. */
export type SaleFieldType =
  'id' | 'text' | 'version' | 'count' | 'money' | 'aggregate' | 'rate' | 'number' | 'boolean';
export type SaleFieldKind = SaleFieldType | `${SaleFieldType}?`;

export interface ManagerDeal {
  readonly id: string;
  readonly customerId: string;
  readonly unitId: string;
  readonly projectId: string;
  readonly leadId: string;
  readonly saleOccurredAtUtc: string;
  readonly saleLocalDate: string;
  readonly openingTimeZoneId: string;
  readonly finalUnitPrice: string;
  readonly currencyCode: string;
  readonly adoptedAtUtc: string;
  readonly adoptedByAccountId: string;
  readonly bookingId: string | null;
  readonly version: string;
}
export const MANAGER_DEAL_FIELDS = {
  id: 'id',
  customerId: 'id',
  unitId: 'id',
  projectId: 'id',
  leadId: 'id',
  saleOccurredAtUtc: 'text',
  saleLocalDate: 'text',
  openingTimeZoneId: 'text',
  finalUnitPrice: 'money',
  currencyCode: 'text',
  adoptedAtUtc: 'text',
  adoptedByAccountId: 'id',
  bookingId: 'id?',
  version: 'version',
} as const;

export interface DealSummary {
  readonly id: string;
  readonly customerId: string;
  readonly unitId: string;
  readonly projectId: string;
  readonly leadId: string;
  readonly agreementId: string;
  readonly saleOccurredAtUtc: string;
  readonly adoptedAtUtc: string;
  readonly finalUnitPrice: string;
  readonly commissionAmount: string;
  readonly currencyCode: string;
  readonly version: string;
}
export const DEAL_SUMMARY_FIELDS = {
  id: 'id',
  customerId: 'id',
  unitId: 'id',
  projectId: 'id',
  leadId: 'id',
  agreementId: 'id',
  saleOccurredAtUtc: 'text',
  adoptedAtUtc: 'text',
  finalUnitPrice: 'money',
  commissionAmount: 'money',
  currencyCode: 'text',
  version: 'version',
} as const;

export interface CommissionSnapshot {
  readonly id: string;
  readonly unitId: string;
  readonly leadId: string;
  readonly leadVersion: string;
  readonly customerId: string;
  readonly projectId: string;
  readonly developerId: string;
  readonly agreementId: string;
  readonly agreementVersion: string;
  readonly agreementNumber: string;
  readonly agreementScope: string;
  readonly agreementStartsOn: string;
  readonly agreementEndsOn: string;
  readonly protectionPeriodDays: number;
  readonly protectionEndsOn: string;
  readonly leadOpenedOnLocalDate: string;
  readonly openingTimeZoneId: string;
  readonly ruleId: string;
  readonly ruleVersion: string;
  readonly ruleProjectId: string | null;
  readonly ruleType: string;
  readonly percentage: string | null;
  readonly fixedAmount: string | null;
  readonly currencyCode: string;
  readonly saleLocalDate: string;
  readonly finalUnitPrice: string;
  readonly amount: string;
  readonly saleOccurredAtUtc: string;
  readonly source: string;
  readonly reference: string;
  readonly selectionReason: string | null;
  readonly adoptedAtUtc: string;
  readonly adoptedByAccountId: string;
  readonly version: string;
}
export const COMMISSION_SNAPSHOT_FIELDS = {
  id: 'id',
  unitId: 'id',
  leadId: 'id',
  leadVersion: 'version',
  customerId: 'id',
  projectId: 'id',
  developerId: 'id',
  agreementId: 'id',
  agreementVersion: 'version',
  agreementNumber: 'text',
  agreementScope: 'text',
  agreementStartsOn: 'text',
  agreementEndsOn: 'text',
  protectionPeriodDays: 'number',
  protectionEndsOn: 'text',
  leadOpenedOnLocalDate: 'text',
  openingTimeZoneId: 'text',
  ruleId: 'id',
  ruleVersion: 'version',
  ruleProjectId: 'id?',
  ruleType: 'text',
  percentage: 'rate?',
  fixedAmount: 'money?',
  currencyCode: 'text',
  saleLocalDate: 'text',
  finalUnitPrice: 'money',
  amount: 'money',
  saleOccurredAtUtc: 'text',
  source: 'text',
  reference: 'text',
  selectionReason: 'text?',
  adoptedAtUtc: 'text',
  adoptedByAccountId: 'id',
  version: 'version',
} as const;

export interface CommissionCollection {
  readonly id: string;
  readonly dealId: string;
  readonly customerId: string;
  readonly unitId: string;
  readonly requestedVersion: string;
  readonly amount: string;
  readonly currencyCode: string;
  readonly entitledAmount: string;
  readonly saleOccurredAtUtc: string;
  readonly collectedAtUtc: string;
  readonly recordedAtUtc: string;
  readonly recorderAccountId: string;
  readonly source: string;
  readonly reference: string;
  readonly notes: string | null;
  readonly version: string;
}
export const COMMISSION_COLLECTION_FIELDS = {
  id: 'id',
  dealId: 'id',
  customerId: 'id',
  unitId: 'id',
  requestedVersion: 'version',
  amount: 'money',
  currencyCode: 'text',
  entitledAmount: 'money',
  saleOccurredAtUtc: 'text',
  collectedAtUtc: 'text',
  recordedAtUtc: 'text',
  recorderAccountId: 'id',
  source: 'text',
  reference: 'text',
  notes: 'text?',
  version: 'version',
} as const;

export interface CollectionSummary {
  readonly dealId: string;
  readonly customerId: string;
  readonly unitId: string;
  readonly currencyCode: string;
  readonly entitledAmount: string;
  readonly collectedAmount: string;
  readonly remainingAmount: string;
  readonly collectionCount: number;
  readonly lastRecordedAtUtc: string | null;
}
export const COLLECTION_SUMMARY_FIELDS = {
  dealId: 'id',
  customerId: 'id',
  unitId: 'id',
  currencyCode: 'text',
  entitledAmount: 'money',
  collectedAmount: 'money',
  remainingAmount: 'money',
  collectionCount: 'number',
  lastRecordedAtUtc: 'text?',
} as const;

export interface ActivityReport {
  readonly observedAtUtc: string;
  readonly scope: string;
  readonly customerCount: string;
  readonly unassignedCustomerCount: string;
  readonly leadCount: string;
  readonly openLeadCount: string;
  readonly unitCount: string;
  readonly viewingCount: string;
  readonly awaitingViewingCount: string;
  readonly confirmedViewingCount: string;
  readonly bookingCount: string;
  readonly pendingBookingCount: string;
  readonly confirmedBookingCount: string;
  readonly dealCount: string;
}
export const ACTIVITY_REPORT_FIELDS = {
  observedAtUtc: 'text',
  scope: 'text',
  customerCount: 'count',
  unassignedCustomerCount: 'count',
  leadCount: 'count',
  openLeadCount: 'count',
  unitCount: 'count',
  viewingCount: 'count',
  awaitingViewingCount: 'count',
  confirmedViewingCount: 'count',
  bookingCount: 'count',
  pendingBookingCount: 'count',
  confirmedBookingCount: 'count',
  dealCount: 'count',
} as const;

export interface CurrencyTotal {
  readonly currencyCode: string;
  readonly dealCount: string;
  readonly collectionCount: string;
  readonly entitledAmount: string;
  readonly collectedAmount: string;
  readonly remainingAmount: string;
}
export const CURRENCY_TOTAL_FIELDS = {
  currencyCode: 'text',
  dealCount: 'count',
  collectionCount: 'count',
  entitledAmount: 'aggregate',
  collectedAmount: 'aggregate',
  remainingAmount: 'aggregate',
} as const;

export interface SaleUnit {
  readonly id: string;
  readonly projectId: string;
  readonly code: string;
  readonly title: string;
  readonly publicationStatus: string;
  readonly availabilityStatus: string;
  readonly hasAdoptedSale: boolean;
  readonly version: string;
}
export const SALE_UNIT_FIELDS = {
  id: 'id',
  projectId: 'id',
  code: 'text',
  title: 'text',
  publicationStatus: 'text',
  availabilityStatus: 'text',
  hasAdoptedSale: 'boolean',
  version: 'version',
} as const;

export interface SaleOperation {
  readonly action: 'adopt' | 'collect';
  readonly customerId: string;
  readonly dealId?: string;
  readonly key: string;
  readonly revision: number;
  readonly body: Readonly<Record<string, string | null>>;
}
export interface SaleReceipt {
  readonly id: string;
  readonly version: string;
  readonly isReplay: boolean;
}
export type SaleFilters = Readonly<Record<string, string>>;
