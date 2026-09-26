# Evidence — P01-S04

Status: Done
Review type: implementation self-review accepted by the user on 2026-09-23; not an independent-agent review.
Scope: documentation/design only. No application, schema, migrations, packages or frontend edits.

## Deliverables reviewed
- DOMAIN_MODEL.md: 22 ERD-visible entities plus AuditEvent, CommandReceipt and OutboxMessage;
  properties, ownership, aggregate transactions and Q-dependencies.
- ERD.md: Mermaid relationships/cardinalities and open-policy notes.
- CONSTRAINTS.md: keys, checks, agreement overlap, viewing intervals, concurrency,
  idempotency, retention, indexes and transaction boundaries.
- P02_TECHNICAL_FOUNDATION.md: proposed .NET/PostgreSQL/EF foundation, layered/type
  structure, course-topic placement, dependency rules and P02-S01.

## L1 — actual checks
Command from D:/Practice: powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1

Result / exit 0:
- PASS: 47 required workflow files present and nonempty.
- PASS: state, handoff and active step consistent.
- PASS: at most one implementation step InProgress.
- PASS: Business linkage — 16 rules, 10 use cases, 14 acceptance scenarios.
The validator proves structure/linkage only.

Additional read-only document check:
- PASS: TRACEABILITY.json parses.
- PASS: ERD defines 22 entities and every relationship endpoint has an entity definition.
- PASS: Mermaid fence is present.

## L2 — rule review
- Catalog: Developer to Project to Unit; Unit code unique within Project; publication and
  availability independent; external confirmation history retained (BR-01/02/03/16).
- Agreements: D015–D021 represented by scope rows, default/project commission rules,
  overlap activation check, Lead agreement attribution and immutable commission snapshot.
- Assignment: exactly one current CustomerAssignment; Lead does not own a separate employee
  assignment (BR-05/06, D022).
- Viewing: D023–D030 represented by proposal/final intervals, revisions, pending cutoffs,
  Unit/Employee conflict and correct cancellation/no-show distinction.
- Reservation: request differs from confirmation; developer price/reference/deadline
  snapshot; expiry never makes Unit Available; deposit separate (BR-10–13/16).
- Deal: external sale record and commission snapshot are atomic; unresolved Unit/reservation
  transitions remain explicit seams rather than invented accepted rules (BR-14/15).
- Privacy/reliability: SalesNote internal; Audit/Outbox/CommandReceipt boundaries documented.

Result: PASS for documented design alignment. Proposed/Open items are labelled.

## L3 — cross-document review
- Every main ERD entity has a corresponding DOMAIN_MODEL definition.
- Key/FK/unique and cardinality expectations agree between ERD and CONSTRAINTS.
- DOMAIN_MODEL aggregate transaction boundaries agree with CONSTRAINTS SaveChanges ownership.
- P02 project dependency direction agrees with ARCHITECTURE and D002; no Vertical Slice.
- TRACEABILITY lists the S04 design files; Q02/Q03 were removed from openQuestions while
  Q04/Q05/Q06/Q07 remain where applicable.
- PostgreSQL interval exclusion is an implementation proposal for accepted conflict behavior,
  not a claim that a database exists.

Result: PASS for design-document consistency.

## L4 — AC-01 through AC-14 data walkthrough
| AC | Required data/constraint |
| --- | --- |
| AC-01 | Unit ProjectId+Code uniqueness; agreement coverage/overlap guard |
| AC-02 | publication separated from availability; append-only market confirmation |
| AC-03 | published catalog projection independent from Customer account |
| AC-04 | Customer ownership plus authorization boundary; Account link seam |
| AC-05 | one current assignment and immutable transfer periods/audit |
| AC-06 | alternative schedule revision and customer acceptance before Confirmed |
| AC-07 | cancelled old slot revision plus new pending cycle atomically |
| AC-08 | cutoff fields; Confirmed does not auto-expire |
| AC-09 | private SalesNote/outcome notes excluded from customer projection |
| AC-10 | PendingReview request; Cache cannot produce confirmation |
| AC-11 | external reference/price/deadline snapshot |
| AC-12 | Expired booking without Unit Available; DepositConfirmation separate |
| AC-13 | immutable CommissionSnapshot, project override/default rule and protection attribution |
| AC-14 | Notification and Outbox are saved with the business change; retry identity documented |

Result: PASS as manual design review. No scenario has runtime testEvidence.

## Acceptance criteria
- SA01 PASS: entities include BR/UC trace and Open dependencies.
- SA02 PASS: ERD entities/cardinalities plus Open-policy notes.
- SA03 PASS: integrity/concurrency/idempotency/audit constraints documented and labelled.
- SA04 PASS: proposed technical choices, rationale, layered/type structure and P02-S01.
- SA05 PASS: validator/linkage; all TRACEABILITY implementation values remain NotStarted.
- SA06 PASS for handoff completeness; the user accepted the disclosed self-review for closure.

## Limits and closure
No runtime, database, build, package, browser or Angular check was run: N/A for design-only S04.
No P02 code or migration was created. Frontend synchronization was not performed.
The proposed agreement tie-breaker, Q01/Q04/Q05/Q06/Q07 and sale/booking policy seams remain
explicitly deferred and must be resolved before their dependent implementation steps.
