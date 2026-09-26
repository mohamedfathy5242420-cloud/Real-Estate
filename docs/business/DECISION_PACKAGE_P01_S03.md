# Decision Package — P01-S03: Q03, Q04, Q06, Q07

Historical decision package updated with later user decisions. Q03 is Accepted in D023–D030;
Q04/Q06/Q07 remain Proposed / NeedsUserDecision.

---

## Q03 — Viewing duration, conflict and timeout

**Decision recorded:** D023–D030 define duration, selection window, pending expiry and conflicts.

| Item | Detail |
| --- | --- |
| **Accepted decision** | Customer proposes start; default duration 60 minutes and employee records final start/end. Maximum selection window 30 days. Both overlapping requests may be Pending; neither reserves a slot. First valid Confirmed wins, checking Unit and Employee interval; loser retains pending state for a new slot. PendingConfirmation expires at earlier of current proposal and seven days; AwaitingCustomerAcceptance at its alternative. Confirmed does not auto-expire and awaits outcome. CustomerNoShow is customer absence only; broker/developer cancellation is Cancelled with actor/reason. Store UTC. Full policy: D023–D030 and CONCURRENCY_AND_IDEMPOTENCY.md. |
| **Alternatives considered** | 1) No timeout — rely on explicit employee/customer action only.<br>2) Fixed 14-day window with automatic cancellation.<br>3) Rolling window from last employee action. |
| **Impacted BR** | BR-07, BR-08, BR-09 |
| **Impacted UC** | UC-04, UC-05 |
| **Impacted AC** | AC-06, AC-07, AC-08 |
| **Status** | Accepted — D023–D030 (2026-09-23). Technical range-boundary/index syntax remains design detail. |

---

## Q04 — Availability data age and re-confirmation policy

**Decision required:** Define how long local availability data is valid, when re-confirmation is required, and what triggers automatic status changes.

| Item | Detail |
| --- | --- |
| **Recommendation** | - Proposed freshness window: seven days from last external availability confirmation (BR-03); seven days is not an Accepted BR-11 rule.<br>- After 7 days, any booking request requires new developer confirmation; the unit does not automatically become Available.<br>- Explicit re-confirmation trigger: (a) deadline expiry of a confirmed booking, (b) developer status change, or (c) manual admin review.<br>- Expired is a booking state; reservation expiry never makes the unit Available without fresh external availability confirmation (BR-12).<br>- **Data freshness does not exempt any booking from external developer confirmation.** |
| **Alternatives considered** | 1) 14-day freshness window, still requiring external confirmation; auto-available is excluded by BR-12.<br>2) Validity tied to specific event (confirmation, expiry) rather than calendar time.<br>3) No time limit — availability persists until explicit change. |
| **Impacted BR** | BR-03, BR-12, BR-16 |
| **Impacted UC** | UC-07, UC-08 |
| **Impacted AC** | AC-10, AC-11, AC-12 |
| **Status** | Proposed — needs user decision before S04. |

---

## Q06 — Deposit and cancellation of confirmed booking

**Decision required:** Define deposit recording, confirmed cancellation procedure, and developer response requirements.

| Item | Detail |
| --- | --- |
| **Recommendation** | - Deposit is paid externally; employee records confirmation internally (BR-13). Recording a deposit does not transition the booking to Completed or apply commission.<br>- Confirmed cancellation flow: Customer requests cancellation → Employee coordinates with developer **externally** → Developer responds (approve/reject) → Employee records developer's response in system → Booking transitions to Cancelled only if still valid/current when approval is recorded; rejection closes the cancellation action while retaining a still-valid Confirmed booking. Late responses after expiry/completion are audited for reconciliation without state overwrite.<br>- **Booking does NOT become Cancelled immediately.** The pending cancellation action does not release the booking. Proposed: block linked sale adoption until that action is resolved; extension/cancellation races check state/version/time at commit.<br>- **Developer has no internal account within the system.** All coordination is external; employee records the response.<br>- Deposit refund policy: details separate from booking system; the system records the refund status per developer agreement. |
| **Alternatives considered** | 1) Immediate local cancellation with later external reconciliation (not recommended; would need explicit Q06 decision, not already prohibited by BR-13).<br>2) Developer response not required (rejected — no way to ensure developer awareness).<br>3) Deposit automatically becomes commission on cancellation (rejected — per BR-14, commission is separate). |
| **Impacted BR** | BR-13, BR-14 |
| **Impacted UC** | UC-08 |
| **Impacted AC** | AC-12 |
| **Status** | Proposed — needs user decision before S04. |

---

## Q07 — Employee access after client transfer

**Decision required:** Define revocation of old employee access and retention of audit history after client reassignment.

| Item | Detail |
| --- | --- |
| **Recommendation** | - After client transfer from Employee A to Employee B:<br>  - Employee A's **current** access to the client's active requests is revoked immediately (cannot read or modify active viewings/bookings).<br>  - Employee A's **historical** access is determined by Q07 policy (Proposed):<br>    - Option A: No access — all prior assignments and notes visible only to Manager in audit log.<br>    - Option B: Read-only access to their own prior assignments and notes — no access to new data created after transfer.<br>  - Employee A **CANNOT perform ANY new commands** (create viewing, booking, assignment changes, record outcomes, complete tasks) on the transferred client.<br>  - Sales Manager retains full access to all historical data and audit log.<br>- The Q07 policy is Proposed / NeedsUserDecision. Not Accepted. |
| **Alternatives considered** | 1) Broader historical read for previous employee (not recommended; remains a Q07 policy choice, not a new Accepted prohibition).<br>2) Complete erasure of prior assignments (rejected — violates BR-15 audit requirements).<br>3) Read-only historical access (Proposed Option B). |
| **Impacted BR** | BR-05, BR-06, BR-15 |
| **Impacted UC** | UC-03 |
| **Impacted AC** | AC-05 |
| **Status** | Proposed — needs user decision before S04. |

---

## Other Proposed Rules (Not Yet Accepted)

| Rule | Status | Source |
| --- | --- | --- |
| Hiding announcement on price change | Proposed / NeedsUserDecision | STATE_TRANSITIONS.md Unit Publication table |
| Deposit recording required before every sale adoption | Proposed / NeedsUserDecision | STATE_TRANSITIONS.md Commission/Sale table |
| Time slot reserved at confirmation (not at request) | Accepted — D026 | STATE_TRANSITIONS.md Q03 section; CONCURRENCY_AND_IDEMPOTENCY.md |
| Duplicate resend = idempotent (no new entry, no cancellation) | Proposed / NeedsUserDecision | CONCURRENCY_AND_IDEMPOTENCY.md |
| Booking response-wait timeout / late-response handling | Proposed / NeedsUserDecision; duration and business calendar Open, three business days only candidate | CONCURRENCY_AND_IDEMPOTENCY.md Late Developer Responses; STATE_TRANSITIONS booking table; BR-10/11/12/16, UC-07/08, AC-10/11/12 |
| Linked-booking completion at sale and cancellation-pending guard | Proposed / NeedsUserDecision; not a booking prerequisite for every sale | STATE_TRANSITIONS Commission/Sale; BR-13/14/15, UC-08/09, AC-12/13 |
| Unit availability transition at sale | Open; no automatic Reserved/Sold rule approved | STATE_TRANSITIONS entity summary; BR-03/16, UC-09, AC-13 |
| Previous employee cannot execute commands after transfer | Proposed / NeedsUserDecision | ACTORS_AND_PERMISSIONS.md; CONCURRENCY_AND_IDEMPOTENCY.md |

---

## Summary of Open Decisions for P01-S03

| Question | Status | Primary BR/UC/AC affected |
| --- | --- | --- |
| Q03 | Accepted — D023–D030 | BR-07, BR-08, BR-09 ; UC-04, UC-05, UC-06 ; AC-06, AC-07, AC-08, AC-09 |
| Q04 | Proposed | BR-03, BR-12, BR-16 ; UC-07, UC-08 ; AC-10, AC-11, AC-12 |
| Q06 | Proposed | BR-13, BR-14 ; UC-08 ; AC-12 |
| Q07 | Proposed | BR-05, BR-06, BR-15 ; UC-03 ; AC-05 |

Only rows explicitly marked Accepted have user approval recorded in DECISIONS.md. Q04/Q06/Q07
and every other Proposed/Open row remain unapproved.
