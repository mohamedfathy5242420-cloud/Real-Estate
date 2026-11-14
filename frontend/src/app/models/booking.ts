/** Explicit projections from delivered P09 booking DTOs, inspected before implementation. */
export const BOOKING_STATUSES = [
  'PendingReview',
  'Confirmed',
  'Rejected',
  'Cancelled',
  'Expired',
  'Completed',
] as const;
export type BookingAction =
  'submit' | 'cancel' | 'responses' | 'extensions' | 'deposits' | 'refunds';
export interface BookingOperation {
  readonly action: BookingAction;
  readonly staff: boolean;
  readonly bookingId?: string;
  readonly customerId?: string;
  readonly depositId?: string;
  readonly key: string;
  readonly revision: number;
  readonly body: Readonly<Record<string, string | null>>;
}
export interface BookingReceipt {
  readonly id: string;
  readonly version: string;
  readonly isReplay: boolean;
}
const labels: Record<string, string> = {
  PendingReview: 'Awaiting booking review',
  Confirmed: 'Confirmed',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
  Expired: 'Booking deadline expired',
  Completed: 'Completed with a recorded deal',
  Requested: 'Request submitted',
  Extended: 'Deadline extended',
  DeveloperRejected: 'Developer rejected',
  InvalidDeadline: 'Invalid developer deadline',
  LocalConflict: 'Local booking conflict',
  ResponseTimeout: 'Developer response deadline expired',
  ReservationExpired: 'Confirmed booking deadline expired',
  StateUnchanged: 'Response recorded without changing the current state',
  'response-timeout': 'Developer response deadline expired',
  'reservation-expired': 'Confirmed booking deadline expired',
  'developer-rejected': 'Developer rejected',
  'invalid-reservation-deadline': 'Invalid booking deadline',
  'local-conflict': 'Local booking conflict',
  None: 'No refund recorded',
  Partial: 'Partial refund recorded',
  Full: 'Full recorded deposit principal refunded',
};
export function bookingLabel(value: string | null): string {
  return value ? (labels[value] ?? 'Recorded state') : 'Not recorded';
}
export interface OwnDeposit {
  readonly id: string;
  readonly bookingId: string;
  readonly amount: string;
  readonly currencyCode: string;
  readonly paidAtUtc: string;
  readonly recordedAtUtc: string;
  readonly version: string;
  readonly recordedRefundAmount: string;
  readonly recordedUnrefundedAmount: string;
  readonly recordedRefundStatus: string;
}
export const OWN_DEPOSIT_KEYS = [
  'id',
  'bookingId',
  'amount',
  'currencyCode',
  'paidAtUtc',
  'recordedAtUtc',
  'version',
  'recordedRefundAmount',
  'recordedUnrefundedAmount',
  'recordedRefundStatus',
] as const;
export interface OwnBooking {
  readonly id: string;
  readonly unitId: string;
  readonly projectId: string;
  readonly status: string;
  readonly requestedAtUtc: string;
  readonly responseDueAtUtc: string;
  readonly requestReason: string | null;
  readonly version: string;
  readonly rejectionReason: string | null;
  readonly confirmedAtUtc: string | null;
  readonly confirmedPrice: string | null;
  readonly confirmedCurrencyCode: string | null;
  readonly reservationExpiresAtUtc: string | null;
  readonly cancelledAtUtc: string | null;
  readonly cancellationReason: string | null;
  readonly completedAtUtc: string | null;
}
export const OWN_BOOKING_KEYS = [
  'id',
  'unitId',
  'projectId',
  'status',
  'requestedAtUtc',
  'responseDueAtUtc',
  'requestReason',
  'version',
  'rejectionReason',
  'confirmedAtUtc',
  'confirmedPrice',
  'confirmedCurrencyCode',
  'reservationExpiresAtUtc',
  'cancelledAtUtc',
  'cancellationReason',
  'completedAtUtc',
] as const;
export interface OwnBookingEvent {
  readonly id: string;
  readonly bookingId: string;
  readonly bookingVersion: string;
  readonly kind: string;
  readonly atUtc: string;
  readonly status: string;
  readonly reasonCode: string | null;
  readonly confirmedPrice: string | null;
  readonly confirmedCurrencyCode: string | null;
  readonly reservationExpiresAtUtc: string | null;
  readonly cancelledFromStatus: string | null;
  readonly cancellationReason: string | null;
}
export const OWN_BOOKING_EVENT_KEYS = [
  'id',
  'bookingId',
  'bookingVersion',
  'kind',
  'atUtc',
  'status',
  'reasonCode',
  'confirmedPrice',
  'confirmedCurrencyCode',
  'reservationExpiresAtUtc',
  'cancelledFromStatus',
  'cancellationReason',
] as const;
export interface OwnRefund {
  readonly id: string;
  readonly bookingId: string;
  readonly depositId: string;
  readonly amount: string;
  readonly currencyCode: string;
  readonly returnedAtUtc: string;
  readonly recordedAtUtc: string;
  readonly version: string;
}
export const OWN_REFUND_KEYS = [
  'id',
  'bookingId',
  'depositId',
  'amount',
  'currencyCode',
  'returnedAtUtc',
  'recordedAtUtc',
  'version',
] as const;
export interface StaffDeposit {
  readonly id: string;
  readonly bookingId: string;
  readonly customerId: string;
  readonly unitId: string;
  readonly requestedVersion: string;
  readonly amount: string;
  readonly currencyCode: string;
  readonly paidAtUtc: string;
  readonly recordedAtUtc: string;
  readonly recorderAccountId: string;
  readonly recorderEmployeeId: string | null;
  readonly source: string;
  readonly reference: string;
  readonly notes: string | null;
  readonly version: string;
  readonly recordedRefundAmount: string;
  readonly recordedUnrefundedAmount: string;
  readonly recordedRefundStatus: string;
}
export const STAFF_DEPOSIT_KEYS = [
  'id',
  'bookingId',
  'customerId',
  'unitId',
  'requestedVersion',
  'amount',
  'currencyCode',
  'paidAtUtc',
  'recordedAtUtc',
  'recorderAccountId',
  'recorderEmployeeId',
  'source',
  'reference',
  'notes',
  'version',
  'recordedRefundAmount',
  'recordedUnrefundedAmount',
  'recordedRefundStatus',
] as const;
export interface StaffBooking {
  readonly id: string;
  readonly customerId: string;
  readonly leadId: string;
  readonly unitId: string;
  readonly projectId: string;
  readonly status: string;
  readonly requestedAtUtc: string;
  readonly requestedByAccountId: string;
  readonly responseDueAtUtc: string;
  readonly submissionAgreementId: string;
  readonly submissionLocalDate: string;
  readonly timeZoneId: string;
  readonly requestReason: string | null;
  readonly version: string;
  readonly rejectedAtUtc: string | null;
  readonly rejectionReason: string | null;
  readonly rejectedByAccountId: string | null;
  readonly rejectionActorKind: string | null;
  readonly confirmedAtUtc: string | null;
  readonly confirmedByAccountId: string | null;
  readonly confirmedPrice: string | null;
  readonly confirmedCurrencyCode: string | null;
  readonly reservationExpiresAtUtc: string | null;
  readonly confirmedResponseId: string | null;
  readonly expiredAtUtc: string | null;
  readonly lastExtendedAtUtc: string | null;
  readonly lastExtensionId: string | null;
  readonly cancelledAtUtc: string | null;
  readonly cancelledByAccountId: string | null;
  readonly cancellationReason: string | null;
  readonly cancelledFromStatus: string | null;
  readonly completedAtUtc: string | null;
  readonly completedByAccountId: string | null;
  readonly completedDealId: string | null;
}
export const STAFF_BOOKING_KEYS = [
  'id',
  'customerId',
  'leadId',
  'unitId',
  'projectId',
  'status',
  'requestedAtUtc',
  'requestedByAccountId',
  'responseDueAtUtc',
  'submissionAgreementId',
  'submissionLocalDate',
  'timeZoneId',
  'requestReason',
  'version',
  'rejectedAtUtc',
  'rejectionReason',
  'rejectedByAccountId',
  'rejectionActorKind',
  'confirmedAtUtc',
  'confirmedByAccountId',
  'confirmedPrice',
  'confirmedCurrencyCode',
  'reservationExpiresAtUtc',
  'confirmedResponseId',
  'expiredAtUtc',
  'lastExtendedAtUtc',
  'lastExtensionId',
  'cancelledAtUtc',
  'cancelledByAccountId',
  'cancellationReason',
  'cancelledFromStatus',
  'completedAtUtc',
  'completedByAccountId',
  'completedDealId',
] as const;
export interface StaffBookingEvent {
  readonly id: string;
  readonly bookingId: string;
  readonly customerId: string;
  readonly bookingVersion: string;
  readonly kind: string;
  readonly actorKind: string;
  readonly actorAccountId: string | null;
  readonly atUtc: string;
  readonly status: string;
  readonly reasonCode: string | null;
  readonly effectiveCutoffUtc: string | null;
  readonly confirmedPrice: string | null;
  readonly confirmedCurrencyCode: string | null;
  readonly reservationExpiresAtUtc: string | null;
  readonly developerResponseId: string | null;
  readonly previousReservationExpiresAtUtc: string | null;
  readonly extensionId: string | null;
  readonly cancelledFromStatus: string | null;
  readonly cancellationReason: string | null;
  readonly dealId: string | null;
}
export const STAFF_BOOKING_EVENT_KEYS = [
  'id',
  'bookingId',
  'customerId',
  'bookingVersion',
  'kind',
  'actorKind',
  'actorAccountId',
  'atUtc',
  'status',
  'reasonCode',
  'effectiveCutoffUtc',
  'confirmedPrice',
  'confirmedCurrencyCode',
  'reservationExpiresAtUtc',
  'developerResponseId',
  'previousReservationExpiresAtUtc',
  'extensionId',
  'cancelledFromStatus',
  'cancellationReason',
  'dealId',
] as const;
export interface ReservationExtension {
  readonly id: string;
  readonly bookingId: string;
  readonly customerId: string;
  readonly unitId: string;
  readonly requestedVersion: string;
  readonly bookingVersionAfter: string;
  readonly previousReservationExpiresAtUtc: string;
  readonly newReservationExpiresAtUtc: string;
  readonly developerExtendedAtUtc: string;
  readonly recordedAtUtc: string;
  readonly recorderAccountId: string;
  readonly recorderEmployeeId: string | null;
  readonly source: string;
  readonly reference: string;
  readonly reason: string | null;
  readonly version: string;
}
export const RESERVATION_EXTENSION_KEYS = [
  'id',
  'bookingId',
  'customerId',
  'unitId',
  'requestedVersion',
  'bookingVersionAfter',
  'previousReservationExpiresAtUtc',
  'newReservationExpiresAtUtc',
  'developerExtendedAtUtc',
  'recordedAtUtc',
  'recorderAccountId',
  'recorderEmployeeId',
  'source',
  'reference',
  'reason',
  'version',
] as const;
export interface StaffRefund {
  readonly id: string;
  readonly bookingId: string;
  readonly depositId: string;
  readonly customerId: string;
  readonly unitId: string;
  readonly requestedVersion: string;
  readonly amount: string;
  readonly currencyCode: string;
  readonly depositPaidAtUtc: string;
  readonly returnedAtUtc: string;
  readonly recordedAtUtc: string;
  readonly recorderAccountId: string;
  readonly recorderEmployeeId: string | null;
  readonly source: string;
  readonly reference: string;
  readonly notes: string | null;
  readonly version: string;
}
export const STAFF_REFUND_KEYS = [
  'id',
  'bookingId',
  'depositId',
  'customerId',
  'unitId',
  'requestedVersion',
  'amount',
  'currencyCode',
  'depositPaidAtUtc',
  'returnedAtUtc',
  'recordedAtUtc',
  'recorderAccountId',
  'recorderEmployeeId',
  'source',
  'reference',
  'notes',
  'version',
] as const;
export interface DeveloperResponse {
  readonly id: string;
  readonly bookingId: string;
  readonly customerId: string;
  readonly unitId: string;
  readonly requestedVersion: string;
  readonly bookingVersionAfter: string;
  readonly responseKind: string;
  readonly decision: string;
  readonly statusAfter: string;
  readonly developerRespondedAtUtc: string;
  readonly recordedAtUtc: string;
  readonly recorderAccountId: string;
  readonly recorderEmployeeId: string | null;
  readonly source: string;
  readonly reference: string;
  readonly offeredPrice: string | null;
  readonly currencyCode: string | null;
  readonly offeredDeadlineUtc: string | null;
  readonly reason: string | null;
  readonly version: string;
}
export const DEVELOPER_RESPONSE_KEYS = [
  'id',
  'bookingId',
  'customerId',
  'unitId',
  'requestedVersion',
  'bookingVersionAfter',
  'responseKind',
  'decision',
  'statusAfter',
  'developerRespondedAtUtc',
  'recordedAtUtc',
  'recorderAccountId',
  'recorderEmployeeId',
  'source',
  'reference',
  'offeredPrice',
  'currencyCode',
  'offeredDeadlineUtc',
  'reason',
  'version',
] as const;
