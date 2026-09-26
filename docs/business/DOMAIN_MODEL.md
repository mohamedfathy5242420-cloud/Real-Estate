# Domain model — P01-S04

Status: Approved P01 design baseline / DesignOnly; not yet a database implementation.
This is a logical domain model, not a database migration or implemented API.
Accepted behavior is identified by BR/D references. Items marked Proposed/Open remain
changeable without rewriting accepted business history.

## Modeling conventions

- Identifiers are opaque `Guid` values in the domain model.
- Money is `decimal` plus ISO currency code; percentage is a constrained decimal.
- Business instants are stored as UTC. A display time-zone identifier is retained where
  a local appointment meaning is needed (D030).
- Mutable aggregates carry an application-managed numeric `Version` for optimistic concurrency.
- Operational records use `CreatedAtUtc`, `CreatedBy`, and where relevant `UpdatedAtUtc`.
- Historical facts and financial snapshots are not rewritten when current master data changes.
- Enum names are implementation candidates. Their business meaning comes from the state documents.

## Aggregate map

| Aggregate root | Owns / controls | Primary responsibility | References |
| --- | --- | --- | --- |
| Developer | Project, external contact details | External company whose projects and inventory are marketed | BR-01/02, UC-01 |
| Project | Unit | Groups units under one developer | BR-02/03, UC-01/02 |
| Unit | UnitImage, UnitMarketConfirmation | Public listing plus independently confirmed price/availability facts | BR-02/03/16, UC-01/02/07 |
| MarketingAgreement | AgreementProject, CommissionRule | Marketing scope, effective period, protection period and commission rules | BR-14/15, D015–D021, UC-01/09 |
| Customer | CustomerAssignment history, SalesNote | Person followed by the broker; optional verified account link follows D035 | BR-04/05/06/15, UC-02/03 |
| Lead | Viewing and ReservationRequest references | One customer opportunity for one project and the agreement that originated it | D020–D022, BR-05/14, UC-03/04/07/09 |
| Viewing | ViewingScheduleRevision | Proposed/alternative/confirmed appointment lifecycle and outcome | BR-07/08/09/15, D023–D030, UC-04/05/06 |
| ReservationRequest | ReservationCancellation, DepositConfirmation | Customer request and recorded external developer decision | BR-10/11/12/13/16, UC-07/08 |
| Deal | CommissionSnapshot, CommissionCollection | Completed external sale record and immutable broker entitlement | BR-13/14/15, D016–D021, UC-09 |
| AuditEvent | — | Append-only record of important accepted and rejected commands | BR-15, UC-10 |
| OutboxMessage | — | Durable integration/notification intent saved with the business transaction | BR-15, UC-10 |
| Notification | — | User-visible notification/inbox record whose delivery failure cannot undo the business change | BR-08/15, UC-10 |
| CommandReceipt | — | Idempotent command identity and recorded outcome reference | BR-15/16, UC-07/10 |

## Catalog

### Developer

Attributes: `Id`, `Name`, `LegalName?`, `Code`, `IsActive`, external contact fields,
audit timestamps, `Version`.

Rules: has no login in MVP; one developer owns many projects. `Code` is an internal stable
identifier. Deactivation prevents new catalog/agreement work but does not delete history.

### Project

Attributes: `Id`, `DeveloperId`, `Code`, `Name`, `Area`, `Address?`, `TimeZoneId`,
`IsActive`, audit timestamps, `Version`.

Rules: belongs to exactly one developer. `TimeZoneId` provides the local meaning of viewings;
stored appointment instants remain UTC. Project deletion is restricted after dependent history.

### Unit

Attributes: `Id`, `ProjectId`, `Code`, `Title`, `Description?`, `UnitType`, `AreaSqm`,
`Bedrooms?`, `Bathrooms?`, `PublicationStatus`, `AvailabilityStatus`, `ListPrice`, `Currency`,
`LastPriceConfirmedAtUtc?`, `LastPriceSource?`, `LastAvailabilityConfirmedAtUtc?`,
`LastAvailabilitySource?`, audit timestamps, `Version`.

Rules: `(ProjectId, Code)` is unique (BR-02). Publication and availability are separate
(BR-03). Availability is local information, never proof of external stock (BR-16).
Q04 remains Open: no automatic hide/expiry policy is encoded as an accepted rule.

### UnitImage

Attributes: `Id`, `UnitId`, `StorageKey`, `AltText?`, `SortOrder`, `IsPrimary`, timestamps.
Binary media is outside the relational aggregate; this row stores metadata only.

### UnitMarketConfirmation

Append-only attributes: `Id`, `UnitId`, `ConfirmedAtUtc`, `RecordedAtUtc`, `RecordedByEmployeeId`,
`SourceType`, `SourceReference?`, `ConfirmedPrice?`, `Currency?`, `AvailabilityStatus?`, `Notes?`.

Purpose: preserves source/time/value of external price or availability confirmation. Updating
the Unit projection does not erase confirmation history (BR-03/11/15).

## Agreements and commission rules

### MarketingAgreement

Attributes: `Id`, `DeveloperId`, `AgreementNumber`, `ScopeType` (`AllDeveloperProjects` or
`SelectedProjects`), `StartsOn`, `EndsOn`, `ProtectionPeriodDays` (default 90, may be zero),
`Status`, audit timestamps, `Version`.

Accepted rules:

- One developer per agreement; explicit all-project or selected-project scope (D015).
- Active date ranges may not overlap for the same covered project (D019).
- Leads created while the agreement covers their project can retain commission eligibility
  through the configured protection period if the sale completes in time (D021).
- New projects join an all-project agreement's scope; selected scope requires explicit rows.

### AgreementProject

Attributes: `MarketingAgreementId`, `ProjectId`, `AddedAtUtc`, `AddedBy`.
Used only for `SelectedProjects`; project must belong to the agreement's developer.

### CommissionRule

Attributes: `Id`, `MarketingAgreementId`, `ProjectId?`, `RuleType` (`Percentage` or
`FixedAmount`), `Percentage?`, `FixedAmount?`, `Currency`, audit timestamps, `Version`.

Rules: one default row has `ProjectId = null`; at most one override per project. Project override
wins over default (D017). Percentage applies to final unit sale price, excluding fees/add-ons
(D016). Fixed rules use their stored currency. Tiered/unit-specific rules are outside MVP.

### Agreement selection for a deal

Accepted facts: a Lead records the agreement covering the project when the opportunity starts;
a Deal snapshots exactly one applied agreement and commission rule. Proposed tie-breaker for
the remaining old-protection/new-agreement edge case:

1. Reuse the existing open Lead and its original agreement; do not auto-switch it.
2. Allow at most one open Lead per `(Customer, Project)`.
3. A new Lead after closure may bind to the agreement effective at its creation.
4. If imported history creates multiple eligible leads, manager selects one with a required
   reason; the Deal still has one Lead/agreement and therefore one commission entitlement.

This policy prevents double commission while preserving D021. It remains Proposed until reviewed.

## Customers, assignment and opportunities

### Customer

Attributes: `Id`, `AccountId?`, `FullName`, `Phone`, `Email?`, `PreferredContactMethod?`,
`Status`, audit timestamps, `Version`.

Rules: browsing account identity and broker CRM identity are separate concepts. `AccountId?`
allows a phone-created CRM customer to remain without a login. D035 forbids automatic linking
from similar phone/email data; a verified link request requires manager review and audit.

### Employee

Attributes: `Id`, `AccountId`, `DisplayName`, `EmployeeNumber`, `Role`, `IsActive`, timestamps,
`Version`. Role/assignment both participate in authorization; neither replaces the other.

### CustomerAssignment

Append-only-period attributes: `Id`, `CustomerId`, `EmployeeId`, `AssignedByManagerId`,
`StartedAtUtc`, `EndedAtUtc?`, `Reason?`, `Version`.

Rules: one current row per customer; transfer closes the old row and opens the new row in one
transaction. The customer, not each Lead, owns the active assignment (D005/D022). Q07 controls
former employee historical read; audit is retained regardless.

### Lead

Attributes: `Id`, `CustomerId`, `ProjectId`, `MarketingAgreementId`, `Status`, `Source`,
`Requirements?`, `BudgetMin?`, `BudgetMax?`, `Currency?`, `OpenedAtUtc`, `ClosedAtUtc?`,
`CloseReason?`, audit timestamps, `Version`.

Rules: one opportunity for a customer/project; employee responsibility derives from the current
CustomerAssignment. `MarketingAgreementId` freezes the originating agreement for protection and
commission eligibility (D020/21). Proposed: one Open Lead per customer/project.

### SalesNote

Attributes: `Id`, `CustomerId`, `LeadId?`, `AuthorEmployeeId`, `Body`, `CreatedAtUtc`.
Internal only; never included in customer responses (BR-09). Corrections append a new note/audit
record rather than silently rewriting important sales history.

## Viewings

### Viewing

Attributes: `Id`, `CustomerId`, `LeadId?`, `UnitId`, `Status`, `CurrentProposedStartUtc`,
`CurrentProposedEndUtc`, `TimeZoneId`, `ConfirmedStartUtc?`, `ConfirmedEndUtc?`,
`ConfirmedEmployeeId?`, `PendingCycleStartedAtUtc`, `CancellationActorType?`,
`CancellationReason?`, `OutcomeNotes?` (internal), audit timestamps, `Version`.

Accepted Q03 design (D023–D030): customer proposes a start; default duration is 60 minutes;
employee records final start/end on confirmation; proposal is at most 30 days ahead;
PendingConfirmation expires at the earlier of seven days/current proposal start;
AwaitingCustomerAcceptance expires at the alternative start; slots are reserved only at
confirmation; confirmed intervals may not overlap for the same Unit or Employee; a conflict
retains the losing request as pending; confirmed viewing awaits Completed/CustomerNoShow;
CustomerNoShow means customer absence only; broker/developer cancellation records Cancelled
with actor/reason; instants are UTC.

### ViewingScheduleRevision

Append-only attributes: `Id`, `ViewingId`, `Sequence`, `Kind` (`CustomerProposal`,
`EmployeeAlternative`, `ConfirmedSlot`, `CancelledSlot`), `StartUtc`, `EndUtc`, `CreatedBy`,
`CreatedAtUtc`, `Reason?`, `SupersedesRevisionId?`.

Purpose: retains proposed, alternative and cancelled-old appointment history. Viewing carries
the current projection. Reschedule writes a cancelled-slot revision and a new proposal atomically.

## Reservations, cancellation and deposit

### ReservationRequest

Attributes: `Id`, `CustomerId`, `LeadId?`, `UnitId`, `Status`, `RequestedAtUtc`,
`DeveloperResponseRecordedAtUtc?`, `DeveloperReference?`, `ConfirmedPrice?`, `Currency?`,
`ReservationDeadlineUtc?`, `ResponseWaitDeadlineUtc?` (Proposed), audit timestamps, `Version`.

Rules: request is not confirmation and needs no viewing (BR-10). External developer confirmation
snapshots reference/price/deadline (BR-11). Expiry never makes Unit Available (BR-12/16).
Response-wait duration and late-response handling remain Proposed/Open.

### ReservationCancellation

Attributes: `Id`, `ReservationRequestId`, `RequestedByCustomerId`, `RequestedAtUtc`, `Reason?`,
`Status`, `DeveloperResponse?`, `DeveloperResponseReference?`, `ResolvedAtUtc?`,
`ResolvedByEmployeeId?`, `Version`.

Purpose: separates cancellation request from booking state change. Q06 remains Open; no schema
constraint treats the proposed external approval flow as accepted behavior.

### DepositConfirmation

Attributes: `Id`, `ReservationRequestId`, `Amount`, `Currency`, `PaidAtUtc?`, `RecordedAtUtc`,
`RecordedByEmployeeId`, `DeveloperReference?`, `RefundStatus?`, `RefundReference?`, `Version`.

Rules: external payment fact only; not broker commission and not automatic sale completion
(BR-13). Q06 controls refund/cancellation details.

## Deal and commission

### Deal

Attributes: `Id`, `CustomerId`, `LeadId`, `UnitId`, `ReservationRequestId?`,
`MarketingAgreementId`, `AppliedCommissionRuleId`, `ExternalSaleReference`, `SoldAtUtc`,
`FinalUnitSalePrice`, `Currency`, `Status`, `RecordedByManagerId`, timestamps, `Version`.

Rules: external sale evidence is required. Reservation is optional because viewing/reservation is
not universally a sale prerequisite. Exact Unit availability transition and linked booking
completion remain Open; creating a Deal must not silently decide them.

### CommissionSnapshot

One-to-one immutable attributes: `DealId`, `RuleType`, `AgreementId`, `RuleId`,
`BasisFinalSalePrice`, `Percentage?`, `FixedAmount?`, `Currency`, `EntitledAmount`,
`CalculatedAtUtc`.

Rules: captures the rule/value at adoption and is unaffected by later agreement changes
(BR-14, D016–D018). For Percentage, entitled amount is percentage × final unit sale price.

### CommissionCollection

Append-only attributes: `Id`, `DealId`, `Amount`, `Currency`, `CollectedAtUtc`, `Reference?`,
`RecordedByEmployeeId`, `CreatedAtUtc`.

Rules: multiple partial collections are supported; total collected may not exceed entitlement
without an explicit adjustment journey. Entitlement and collection are different facts.

## Reliability and audit records

### AuditEvent

Attributes: `Id`, `OccurredAtUtc`, `ActorType`, `ActorId?`, `Action`, `EntityType`, `EntityId`,
`OldState?`, `NewState?`, `Reason?`, `CorrelationId`, `DataJson?`.
Append-only and access-controlled. It records rejected/stale attempts where required by BR-15.

### CommandReceipt

Attributes: `Id`, `ActorId`, `Operation`, `IdempotencyKey`, `RequestHash`, `Status`,
`ResourceType?`, `ResourceId?`, `ResponseReference?`, `CreatedAtUtc`, `ExpiresAtUtc?`.
Transport retention is Proposed; uniqueness prevents a retried command from repeating effects.

### OutboxMessage

Attributes: `Id`, `OccurredAtUtc`, `EventType`, `AggregateType`, `AggregateId`, `PayloadJson`,
`CorrelationId`, `ProcessedAtUtc?`, `AttemptCount`, `LastError?`.
Saved with the aggregate transaction so channel failure does not lose the core operation (AC-14).

### Notification

Attributes: `Id`, exactly one of `CustomerId?` / `EmployeeId?`, `Type`, `Title`, `Body`,
`ResourceType?`, `ResourceId?`, `CreatedAtUtc`, `ReadAtUtc?`, `DeliveryStatus`, `Version`.

Purpose: durable user-visible record created with or from an Outbox event. Channel attempts can
fail/retry independently; the underlying business action and inbox notification remain recorded.
Resource authorization is rechecked when opening a link, especially after assignment transfer.

## Aggregate transaction boundaries

- Agreement activation: agreement, scope rows, commission rules and overlap validation.
- Customer transfer: close current assignment, create successor, audit and outbox notification.
- Viewing confirmation/reschedule: version/conflict check, Viewing projection, schedule revision,
  audit and outbox message.
- Reservation response: version/deadline/conflict check, confirmation snapshot, Unit projection
  only when an accepted rule permits it, audit and outbox.
- Deal adoption: Deal and CommissionSnapshot always atomic; optional reservation/unit transitions
  wait for their Open decisions. Commission collection is a later transaction.
- Every mutation writes audit/outbox with the business change; external publishing is asynchronous.

## Open dependencies retained in the model

| Question | Model seam |
| --- | --- |
| Q04 | Unit confirmation history exists; freshness/hiding automation is policy, not schema truth |
| Q06 | separate ReservationCancellation and DepositConfirmation aggregates |
| Q07 | assignment history retained; query authorization decides former-employee visibility |
| Other | booking response wait, linked booking completion, Unit state after sale and alternate sale paths remain explicit policy seams |
