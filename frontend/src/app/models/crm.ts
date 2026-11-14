export interface CrmPage<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly total: number;
}
export interface CustomerProfile {
  readonly id: string;
  readonly fullName: string;
  readonly phone: string;
  readonly email: string | null;
  readonly preferredContactMethod: string | null;
  readonly requirements: string | null;
  readonly version: string;
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string | null;
}
export interface CrmCustomer extends CustomerProfile {
  readonly assignedEmployeeId: string | null;
}
export interface CustomerValues {
  fullName: string;
  phone: string;
  email: string;
  preferredContactMethod: string;
  requirements: string;
}
export interface EmployeeProfile {
  readonly id: string;
  readonly accountId: string;
  readonly employeeNumber: string;
  readonly displayName: string;
  readonly isActive: boolean;
  readonly version: string;
  readonly createdAtUtc: string;
  readonly createdByAccountId: string;
  readonly updatedAtUtc: string | null;
  readonly updatedByAccountId: string | null;
}
export interface EmployeeRevision {
  readonly id: string;
  readonly version: string;
  readonly displayName: string;
  readonly isActive: boolean;
  readonly atUtc: string;
  readonly actorAccountId: string;
}
export interface AssignmentCandidate {
  readonly id: string;
  readonly employeeNumber: string;
  readonly displayName: string;
}
export interface Assignment {
  readonly id: string;
  readonly customerId: string;
  readonly previousEmployeeId: string | null;
  readonly newEmployeeId: string;
  readonly actorAccountId: string;
  readonly changedAtUtc: string;
  readonly reason: string | null;
  readonly customerVersion: string;
}
export interface OwnLink {
  readonly id: string;
  readonly claim: string;
  readonly status: string;
  readonly createdAtUtc: string;
  readonly decidedAtUtc: string | null;
  readonly version: string;
}
export interface LinkEvent {
  readonly id: string;
  readonly kind: string;
  readonly actorAccountId: string;
  readonly atUtc: string;
  readonly reason: string | null;
  readonly customerId: string | null;
}
export interface LinkReview extends OwnLink {
  readonly accountId: string;
  readonly decisionReason: string | null;
  readonly decidedByAccountId: string | null;
  readonly customerId: string | null;
  readonly history: readonly LinkEvent[];
}
export interface LinkCandidate {
  readonly id: string;
  readonly fullName: string;
  readonly phone: string;
  readonly email: string | null;
  readonly version: string;
}
export interface SalesNote {
  readonly id: string;
  readonly customerId: string;
  readonly leadId: string | null;
  readonly authorAccountId: string;
  readonly authorEmployeeId: string | null;
  readonly body: string;
  readonly createdAtUtc: string;
  readonly correctsNoteId: string | null;
}
export interface NeedsChange {
  readonly id: string;
  readonly customerId: string;
  readonly previousRequirements: string | null;
  readonly requirements: string | null;
  readonly customerVersion: string;
  readonly actorAccountId: string;
  readonly atUtc: string;
}
export interface Lead {
  readonly id: string;
  readonly customerId: string;
  readonly projectId: string;
  readonly marketingAgreementId: string;
  readonly agreementVersionAtOpening: string;
  readonly agreementStartsOn: string;
  readonly agreementEndsOn: string;
  readonly protectionPeriodDays: number;
  readonly protectionEndsOn: string;
  readonly openedOnLocalDate: string;
  readonly openingTimeZoneId: string;
  readonly openedAtUtc: string;
  readonly openedByAccountId: string;
  readonly source: string;
  readonly status: string;
  readonly version: string;
  readonly closedAtUtc: string | null;
  readonly closedByAccountId: string | null;
  readonly closeReason: string | null;
}
export interface LeadEvent {
  readonly id: string;
  readonly leadId: string;
  readonly customerId: string;
  readonly leadVersion: string;
  readonly eventType: string;
  readonly actorAccountId: string;
  readonly atUtc: string;
  readonly reason: string | null;
}
export interface MutationReply {
  readonly id: string;
  readonly customerId: string;
  readonly version?: string;
  readonly customerVersion?: string;
  readonly leadVersion?: string;
  readonly isReplay: boolean;
  readonly created?: boolean;
}
export interface CrmCommand {
  readonly key: string;
  readonly body: Readonly<Record<string, unknown>>;
  readonly revision: number;
}
export type CrmAction =
  'intake' | 'assign' | 'transfer' | 'needs' | 'addNote' | 'correctNote' | 'openLead' | 'closeLead';
export interface CrmOperation {
  readonly action: CrmAction;
  readonly customerId?: string;
  readonly resourceId?: string;
  readonly command: CrmCommand;
}
export function customerValues(record: CustomerProfile | null): CustomerValues {
  return {
    fullName: record?.fullName ?? '',
    phone: record?.phone ?? '',
    email: record?.email ?? '',
    preferredContactMethod: record?.preferredContactMethod ?? '',
    requirements: record?.requirements ?? '',
  };
}
