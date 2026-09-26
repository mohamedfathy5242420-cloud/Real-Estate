# Data constraints — P01-S04

Status: DesignOnly. This file separates accepted invariants from proposed implementation guards.
Exact SQL names/syntax belong to migrations in P02+.

## Common columns and types

- PK: `Guid`, generated once; never reused.
- UTC instant: timezone-aware database timestamp. Local appointment display uses `TimeZoneId`.
- Money: decimal precision sufficient for sale prices/commission, plus three-letter currency.
- Mutable aggregate: `Version bigint > 0` used as optimistic concurrency token.
- Audit timestamps are UTC; important state facts are append-only or represented by history rows.
- Free text has explicit maximum lengths in mapping; internal notes are never projected to customers.

## Keys, uniqueness and checks

| Area | Required / derived constraint (status stated) | Proposed / Open guard |
| --- | --- | --- |
| Developer | `Developer.Code` unique, nonblank | Code format/length chosen in P02 |
| Project | FK to Developer; `(DeveloperId, Code)` unique | restrict deactivation when active agreements exist |
| Unit | FK to Project; `(ProjectId, Code)` unique (BR-02); prices nonnegative; currency required with money | one primary image via filtered unique index; search indexes tuned later |
| Publication/availability | two separate non-null status columns (BR-03) | enum/check values are implementation names; Q04 controls freshness/hiding |
| Unit confirmation | FK Unit/recorder; at least price or availability supplied; append-only | source-reference uniqueness depends on external source |
| Agreement | FK Developer; `EndsOn >= StartsOn`; `ProtectionPeriodDays >= 0`, default 90; `(DeveloperId, AgreementNumber)` unique | activation-time project/date overlap guard described below |
| Agreement scope | `(AgreementId, ProjectId)` unique; selected project belongs to agreement developer | selected scope needs at least one row; all-project scope needs none |
| Commission rule | exactly one default rule per agreement; at most one `(AgreementId, ProjectId)` override; exactly one of Percentage/FixedAmount populated; percentage `>0 and <=100`; fixed amount `>=0` | currency conversion is outside MVP; override project must be covered |
| Customer | phone normalized and required; optional AccountId unique when present | D035: do not auto-link or merge on phone/email match; verified reviewed link only |
| Employee | AccountId and EmployeeNumber unique; inactive employee cannot receive new assignment | account is ASP.NET Core Identity per D033/D034 |
| Assignment | one current row per Customer (`EndedAtUtc is null`); end after start | transfer done transactionally; Q07 controls historical reads |
| Lead | FK Customer/Project/Agreement; close time after open | filtered unique one Open Lead per `(CustomerId, ProjectId)` is Proposed tie-breaker |
| SalesNote | required author/customer/body; immutable creation | correction by append/audit, no customer projection |
| Viewing | future proposal at creation; end after start; status/time combination check | confirmed interval overlap exclusion for Unit and Employee; accepted D023–D030, implementation below |
| Viewing revision | `(ViewingId, Sequence)` unique; end after start; superseded revision same Viewing | immutable append-only |
| Reservation | FK Customer/Unit; confirmed fields all present together; deadlines UTC; confirmed price nonnegative | response-wait deadline/late response remains Open |
| Cancellation | FK Reservation; resolved fields required together | state effects and refund behavior Q06 |
| Deposit | amount `>0`, currency required, external fact only | refund status transitions Q06 |
| Deal | FK Customer/Lead/Unit/Agreement/Rule; final price `>0`; external sale reference required | unique external reference scoped to Developer; Unit active-sale uniqueness waits for reversal/state decision |
| Commission snapshot | PK/FK Deal; immutable; exactly one rule shape; amount `>=0`; currency matches Deal/rule | percentage calculation rounding policy selected in P02 |
| Collection | amount `>0`; currency matches snapshot | sum cannot exceed entitlement without explicit adjustment; lock/check in transaction |
| Audit/outbox | append-only IDs; correlation ID indexed | payload retention/redaction policy P02/security review |
| Notification | exactly one CustomerId/EmployeeId recipient; CreatedAt required; ReadAt after CreatedAt | delivery retry/retention and channel policy P08 |
| Command receipt | unique `(ActorId, Operation, IdempotencyKey)`; RequestHash required | key retention/expiry depends on operation |

## Agreement coverage and overlap

Accepted outcome (D015/D019): at most one active agreement covers a project at any instant.
Activation performs these checks in one serialized transaction:

1. Project belongs to the agreement Developer.
2. Selected-project scope has explicit rows; all-project scope computes all that developer's projects.
3. No Active agreement with an intersecting date range covers any computed project.
4. Default commission rule exists and every override targets a covered project.

Because all-project coverage changes when a developer adds a project, project creation also checks
active all-project agreements and overlap. A database exclusion constraint can enforce explicit
selected rows; all-project expansion still requires transactional/domain enforcement. The system
must never silently choose one of two active agreements.

Protection after expiry does not make the old agreement Active. Lead.MarketingAgreementId locks
the originating agreement. The proposed single-open Lead guard and exactly one Deal/Lead plus one
CommissionSnapshot/Deal prevent double entitlement. Imported ambiguity requires audited manager
selection before Deal creation.

## Viewing interval constraints

Accepted D023–D030:

- Default proposed end is start + 60 minutes; confirmed employee may record another positive end.
- Customer proposal start is no more than 30 days ahead and must be future at submission.
- PendingConfirmation cutoff is `min(cycle start + 7 days, current proposed start)`.
- AwaitingCustomerAcceptance cutoff is its current alternative start.
- Only Confirmed rows reserve intervals.
- A Confirmed interval cannot overlap another Confirmed interval for the same Unit or the same
  ConfirmedEmployee. Boundary convention is half-open `[start,end)`, allowing back-to-back slots.
- Confirmation uses a transaction that checks current state/version, effective cutoff and both
  conflicts. First committed confirmation wins. Conflict leaves the request pending.
- Confirmed rows are not auto-expired; outcome/cancellation actor and reason determine terminal state.

SQL Server implementation for P05: perform the overlap query and confirmation write in one
appropriately isolated transaction, lock the scheduling key/range as designed in that step, and
back it with indexes on Unit/Employee plus confirmed time bounds. A plain application pre-check
outside the transaction is insufficient. The exact locking implementation must be proven against
two concurrent confirmations before P05 is Done.

Indexes: `(UnitId, Status, ConfirmedStartUtc, ConfirmedEndUtc)`,
`(ConfirmedEmployeeId, Status, ConfirmedStartUtc, ConfirmedEndUtc)`, and pending cutoff columns.

## Reservation and sale constraints

Accepted:

- A request cannot enter Confirmed without external reference, price, currency and future deadline.
- Advertisement/cache availability never confirms a reservation (BR-10/16).
- Expiry never changes Unit to Available automatically (BR-12).
- Deposit is neither sale completion nor commission (BR-13).
- Deal and CommissionSnapshot are saved atomically; later agreement edits do not change snapshot.

Open/Proposed:

- response-wait deadline, late response state and same-customer simultaneous intentional requests;
- external cancellation approval/refund flow (Q06);
- whether a Deal completes its linked Reservation;
- exact Unit availability transition on external sale;
- whether every external sale must reference a preceding Reservation (current model says optional).

Until accepted, database checks must permit these policy seams while forbidding partial financial
records. A failed Deal transaction creates neither Deal nor CommissionSnapshot nor outbox event.

## Delete, archive and retention

- No hard delete for Developer, Project, Unit, Agreement, Customer, Employee, Lead, Viewing,
  Reservation, Deal, financial snapshots, assignments, confirmation history or audit/outbox after
  business use. Use active/archive status and retain foreign keys.
- Draft objects with no dependents may be hard-deleted by an administrator; exact retention is a
  P02 operational decision and must not cascade into history.
- FK delete behavior defaults to Restrict. Owned metadata such as unreferenced draft UnitImage may
  use explicit cascade only before publication/history.
- Customer privacy handling must pseudonymize where legally required while retaining financial/audit
  integrity; legal retention details are outside current scope.

## Transaction and concurrency boundaries

| Operation | Atomic writes / guards |
| --- | --- |
| Activate agreement | status + scope/rules validation + overlap check + audit + outbox |
| Transfer customer | close current assignment + open next + version check + audit + notification outbox |
| Confirm viewing | state/cutoff/version + Unit/Employee interval exclusion + confirmed revision + audit/outbox |
| Reschedule viewing | cancel old slot revision + update projection/new cycle + audit/outbox |
| Record developer confirmation | reservation snapshot + conflict/deadline/version + audit/outbox; Unit update only by accepted policy |
| Expire reservation | deadline/state/version + state history/audit/outbox; no automatic Available |
| Adopt deal | agreement/lead eligibility + Deal + CommissionSnapshot + audit/outbox; unresolved linked transitions excluded |
| Record collection | lock Deal/snapshot + cumulative-total guard + collection + audit |

Repositories never call SaveChanges independently. Application orchestration owns one transaction
per command. External messaging happens after commit through Outbox.

## Index plan

- Catalog: published filter plus Project/area/price/areaSqm/rooms as query needs prove.
- Customer work queues: current Assignment employee/customer; Lead status; request status/time.
- Deadlines: pending viewing cutoff, reservation response/deadline, unprocessed Outbox.
- Audit: `(EntityType, EntityId, OccurredAtUtc)` and `(ActorId, OccurredAtUtc)`.
- Notification: recipient plus `(ReadAtUtc, CreatedAtUtc)` for inbox/unread queries.
- Notifications/outbox: `(ProcessedAtUtc, OccurredAtUtc)`; idempotent consumer message ID unique.

Indexes are workload hypotheses until query plans exist; only uniqueness/integrity indexes are
mandatory at first migration.
