# Evidence — P01-S03

Status: Done
Evidence revision: 2026-09-21
Review type: implementation self-review plus same-agent verification; not independent approval.

This file contains only the current evidence. Earlier comparison tables and line-number
mappings were removed because later review proved them stale. Section names are used as
stable references.

## L1 — Structural validation

Command from `D:/Practice`:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1
```

Actual result after the evidence/handoff/read-order corrections:

```text
PASS: 41 required workflow files are present and nonempty.
PASS: current state, handoff and active step are consistent.
PASS: at most one implementation step is InProgress.
Scope: document structure only. Manual review and application tests are NOT implied.
PASS: Business linkage - 16 rules, 10 use cases, 14 acceptance scenarios.
```

Exit code: 0. This proves structure/reference consistency only.
`D:/Practice` is not a Git repository, so no git diff or persisted SHA256 inventory is
claimed. Scope was checked from the concrete changed-file list in CURRENT_HANDOFF.md.

## L2/L3 — Manual rule and document comparison

| Check | Current result | Compared sections |
| --- | --- | --- |
| Late developer response | PendingReview response timeout is separate from Confirmed reservation expiry. Late/terminal responses are evidence only and do not reopen or confirm. Policies remain Proposed. | Concurrency / Late Developer Responses; States / Booking; Decisions / Other Proposed Rules |
| Viewing conflict | Both requests may be pending; first valid Confirmed commit reserves the interval. Coordination alone does not win. Losing confirmation is Conflict and retains its pending state. Proposed Q03. | Concurrency / Viewing; States / Conflict policy; Decisions / Q03 |
| Viewing expiry | PendingConfirmation uses earlier of current proposed start and seven-day cycle; alternative uses its own start. Superseded timers cannot expire a newer cycle/state. Proposed Q03. | Concurrency / Q03 proposed expiry clock; States / Viewing; Decisions / Q03 |
| Booking cancellation | Cancellation request records a pending action, not cancellation. External approval may cancel a still-valid booking; rejection closes the action; late response is reconciliation evidence. Proposed Q06. | Concurrency / Stale Writes and Expiration Races; States / Q06 notes; Decisions / Q06 |
| Sale coupling | External sale evidence and valid agreement/commission rule required. Deposit prerequisite is not Accepted. Linked booking completion is Proposed. Exact unit availability transition is Open. | States / Commission-Sale; Concurrency / sale race; Decisions / Other Proposed Rules; UC-09 |
| Retry vs new request | Same operation has no duplicate business effect. A fresh request after a terminal one is distinct. Multiple intentional pending bookings by the same customer remain Open. | Concurrency / Booking, Repeated Commands and Design Notes |
| Permissions | Assignment and role permission are separate. Manager scope does not approve all commands; former employee retry cannot bypass revoked access; catalog actor follows the matrix. | Actors / matrix and Q07; States / common guards/publication; Concurrency / Design Notes |
| Accepted invariants | No Cache confirmation, no automatic Available after expiry, deposit separate from sale/commission, developer external, customer notes private. BR/UC/AC content and implementation status unchanged. | BUSINESS_RULES, USE_CASES, ACCEPTANCE_SCENARIOS and TRACEABILITY vs design documents |
| Discovery | README reading order and TRACEABILITY.businessFiles include both S03 design documents. | Business README; TRACEABILITY.json |

Manual L2/L3 result: the reviewed scenarios are mutually consistent and honestly labelled.
This is not a claim that every Open journey has been resolved or that behavior is implemented.

## L4 — Manual acceptance walkthrough

- AC-05: manager assignment/history retained; Q07 historical access remains undecided.
- AC-06: coordinated alternative still requires customer acceptance.
- AC-07/08: reschedule immediately removes old confirmation; self-cancel after start remains refused.
- AC-09: Completed and CustomerNoShow are distinct; private notes excluded. Developer
  absence/staff cancellation remains an Open exception, not CustomerNoShow.
- AC-10/11: a request never confirms itself; external response and snapshot are required.
- AC-12: reservation expiry never makes unit Available; deposit does not complete sale.
- AC-02 cross-effect: publication and availability remain independent; price-change hiding Proposed.
- AC-13 cross-effect: commission snapshot retained; Q02 formula remains Open.

Result: accepted invariants preserved in a design walkthrough only. No runtime test evidence.

## Acceptance criteria and limits

- SA01: transition rows plus common guards cover actors, preconditions, effects,
  invalid/late handling and audit expectations.
- SA02: role permission is separate from ownership/assignment; Q07 is not approved.
- SA03: covered concurrency recommendations are deterministic; unresolved cases are listed Open.
- SA04: Q03/Q04/Q06/Q07 and additional policies remain Proposed/Open.
- SA05: validator passes; TRACEABILITY implementation stays NotStarted and testEvidence empty.
- SA06: current handoff and this current-only evidence support another review without chat history.

Not run / N/A: application build, unit/integration/browser tests, database checks and Angular
checks—no application/schema exists in this documentation step. No S04/ERD, packages,
frontend edits or acceptance of user decisions occurred.

Open: response-wait duration/calendar; Q03 duration/interval/timers; Q04; Q06; Q07;
multiple intentional same-customer pending bookings; pre-confirmation cancellation;
developer-absence/staff-cancel exceptions; unit availability at sale; alternative sale journeys;
Q02 commission details.

## Changed files for the final evidence correction

- planning/phase-01/evidence-S03.md
- handoffs/CURRENT_HANDOFF.md
- docs/business/README.md
- docs/business/TRACEABILITY.json
- docs/CURRENT_STATE.md

## Review decision

On 2026-09-21 the user explicitly instructed the original reviewer to approve the step.
The reviewer had also implemented the latest fixes, so the review is disclosed as
self-review rather than independent review. The user accepted that disclosed limitation.
Result: P01-S03 Accepted / Done for its documentation-design scope.

This approval does NOT accept Q03/Q04/Q06/Q07 or any other Proposed/Open policy,
does not authorize P01-S04, and does not claim application behavior or tests.
