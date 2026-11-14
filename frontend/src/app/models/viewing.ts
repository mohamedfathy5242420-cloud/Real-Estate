export const VIEWING_STATUSES = [
  'PendingConfirmation',
  'AwaitingCustomerAcceptance',
  'Confirmed',
  'Cancelled',
  'Expired',
  'Completed',
  'CustomerNoShow',
] as const;
export type ViewingStatus = (typeof VIEWING_STATUSES)[number];
export interface OwnViewing {
  readonly id: string;
  readonly unitId: string;
  readonly projectId: string;
  readonly status: ViewingStatus;
  readonly currentProposedStartUtc: string;
  readonly currentProposedEndUtc: string;
  readonly timeZoneId: string;
  readonly requestedAtUtc: string;
  readonly pendingCycleStartedAtUtc: string;
  readonly pendingExpiresAtUtc: string;
  readonly version: string;
  readonly confirmedStartUtc: string | null;
  readonly confirmedEndUtc: string | null;
}
export interface StaffViewing extends OwnViewing {
  readonly customerId: string;
  readonly leadId: string;
  readonly requestedByAccountId: string;
  readonly confirmedEmployeeId: string | null;
  readonly confirmedAtUtc: string | null;
  readonly confirmedByAccountId: string | null;
  readonly coordinatedAtUtc: string | null;
  readonly coordinationSource: string | null;
  readonly coordinationReference: string | null;
  readonly offeredAtUtc: string | null;
  readonly offeredByAccountId: string | null;
  readonly offeredEmployeeId: string | null;
  readonly cancelledAtUtc: string | null;
  readonly cancelledByAccountId: string | null;
  readonly cancellationKind: string | null;
  readonly cancellationReason: string | null;
  readonly cancelledFromStatus: string | null;
  readonly expiredAtUtc: string | null;
  readonly expiredFromStatus: string | null;
  readonly outcomeOccurredAtUtc: string | null;
  readonly outcomeRecordedAtUtc: string | null;
  readonly outcomeRecordedByAccountId: string | null;
  readonly outcomeNoteId: string | null;
}
export interface OwnViewingEvent {
  readonly id: string;
  readonly viewingId: string;
  readonly viewingVersion: string;
  readonly kind: string;
  readonly atUtc: string;
  readonly proposedStartUtc: string;
  readonly proposedEndUtc: string;
  readonly confirmedStartUtc: string | null;
  readonly confirmedEndUtc: string | null;
}
export interface StaffViewingEvent extends OwnViewingEvent {
  readonly customerId: string;
  readonly actorAccountId: string | null;
  readonly confirmedEmployeeId: string | null;
  readonly coordinatedAtUtc: string | null;
  readonly coordinationSource: string | null;
  readonly coordinationReference: string | null;
  readonly offeredEmployeeId: string | null;
  readonly previousProposedStartUtc: string | null;
  readonly previousProposedEndUtc: string | null;
  readonly reason: string | null;
  readonly previousStatus: string | null;
  readonly previousConfirmedStartUtc: string | null;
  readonly previousConfirmedEndUtc: string | null;
  readonly previousConfirmedEmployeeId: string | null;
  readonly cancellationKind: string | null;
  readonly actorKind: string;
  readonly effectiveCutoffUtc: string | null;
  readonly outcomeOccurredAtUtc: string | null;
  readonly outcomeNoteId: string | null;
}
export interface ViewingReceipt {
  readonly id: string;
  readonly version: string;
  readonly isReplay: boolean;
}
export type ViewingAction =
  | 'submit'
  | 'accept-alternative'
  | 'cancel'
  | 'change-proposal'
  | 'confirm'
  | 'alternatives'
  | 'outcome';
export interface ViewingOperation {
  readonly action: ViewingAction;
  readonly staff: boolean;
  readonly customerId?: string;
  readonly id?: string;
  readonly key: string;
  readonly revision: number;
  readonly body: Readonly<Record<string, string | null>>;
}
const labels: Record<string, string> = {
  PendingConfirmation: 'Awaiting coordination and confirmation',
  AwaitingCustomerAcceptance: 'Awaiting customer acceptance of alternative',
  Confirmed: 'Confirmed',
  Cancelled: 'Cancelled',
  Expired: 'Waiting period expired',
  Completed: 'Viewing completed',
  CustomerNoShow: 'Customer no-show',
  Requested: 'Request submitted',
  AlternativeOffered: 'Alternative proposed',
  AlternativeAccepted: 'Customer accepted alternative',
  Rescheduled: 'Reschedule requested',
};
export function viewingLabel(value: string): string {
  return labels[value] ?? 'Recorded viewing event';
}
