# State transitions

The linked BR rules are Accepted; English names below are Proposed to explain the design,
and are not yet Accepted enums. Policies labelled Proposed need user approval before implementation.

## Common guards

Every mutation requires current role permission AND ownership/assignment as applicable.
Wrong source state rejects a new command. Proposed: stale writes are Conflict; retries of
the same operation return its recorded outcome without another transition, subject to
current access checks. Audit actor/system, entity ID, old/new state, time, reason and
external reference where applicable; rejected/late attempts do not rewrite history.
These guards apply to every table, including rows whose extra invalid behavior is N/A.
Internal notes are never in customer payloads (BR-09).

## — Preview (Viewing) Transitions — BR-07 / BR-08 / BR-09

Accepted Q03 reference start: latest customer proposal in PendingConfirmation, current
alternative in AwaitingCustomerAcceptance, confirmed start in Confirmed. Superseded times
are history only. Use the Q03 expiry clock in CONCURRENCY_AND_IDEMPOTENCY.md:
all pending confirmation/acceptance/change/cancel commands check effective expiry at commit,
even if its worker is late. New proposed/alternative times must be in the future.

| From | Event and Actor | To | Preconditions | Effect | Invalid/Late Behavior | Audit Data |
| --- | --- | --- | --- | --- | --- | --- |
| No request | Customer suggests a date/time | PendingConfirmation | Customer account exists; unit published; future start within 30 days | Proposed start and default 60-minute end recorded | Past/beyond 30 days: reject | Timestamp, customer ID, requested start/end, unit ID |
| PendingConfirmation | Sales employee confirms original after coordination | Confirmed | Developer coordination completed; future final interval; no Unit/Employee overlap | Final start/end and confirming employee recorded; interval reserved | Expired/conflicting interval: reject confirmation and retain pending | Timestamp, employee ID, developer confirmation reference, confirmed interval |
| PendingConfirmation | Sales employee suggests a coordinated alternative | AwaitingCustomerAcceptance | Future coordinated alternative; pending request not expired under Q03 | Not confirmed; alternative recorded; reference start becomes alternative | Effective pending expiry reached: reject | Timestamp, employee ID, old/new date/time, reason |
| AwaitingCustomerAcceptance | Customer accepts the alternative | Confirmed | Customer explicitly accepts alternative | Alternative becomes confirmed | After alternative start time: reject with "cannot accept past alternative" | Timestamp, customer ID, accepted alternative date/time |
| PendingConfirmation / AwaitingCustomerAcceptance | Customer suggests another | PendingConfirmation | Customer owns request; future new time; pending request not expired under Q03 | New coordination cycle started; old proposal preserved | At/after effective pending expiry: reject | Timestamp, customer ID, old/new requested date/time |
| Confirmed | Customer requests rescheduling | PendingConfirmation | Request before start time; viewing confirmed | Old appointment cancelled immediately, saved, employee notified | At/after start time: reject with "cannot reschedule started or completed viewing" | Timestamp, customer ID, old date/time cancelled, new date requested, employee notified |
| PendingConfirmation / AwaitingCustomerAcceptance / Confirmed | Customer cancels | Cancelled | Cancel before start time per relevant date | Appointment stopped, employee notified | At/after start time: reject with "cannot cancel started or completed viewing" | Timestamp, customer ID, cancellation reason, employee notified |
| PendingConfirmation / AwaitingCustomerAcceptance / Confirmed | Assigned employee cancels for broker/developer unavailability | Cancelled | Before applicable start; reason recorded | Appointment stopped, customer notified; confirmed interval released | At/after start or terminal state: reject same cancellation path | Timestamp, employee ID, actor type, reason, customer notified |
| Confirmed | Employee records outcome: Completed | Completed | Actual viewing occurred; employee records outcome | Client state updated; internal notes saved | N/A | Timestamp, employee ID, outcome=Completed, notes |
| Confirmed | Employee records outcome: CustomerNoShow | CustomerNoShow | Customer did not attend; viewing time has passed | Client state updated; internal notes saved | Before start time: reject with "cannot mark no-show before appointment time" | Timestamp, employee ID, outcome=CustomerNoShow, notes |
| PendingConfirmation | System detects Q03 expiry | Expired | Earlier of latest proposed start and seven calendar days since current coordination cycle reached | Preserve history; notify customer | Repeat no-op; old-cycle timer cannot expire a new cycle or state | Timestamp, viewing ID, cycle and effective cutoff |
| AwaitingCustomerAcceptance | System detects Q03 expiry | Expired | Current alternative start reached | Preserve history; notify customer | Old original start/coordination deadline ignored; repeat no-op | Timestamp, viewing ID, alternative start |

**Conflict policy (Accepted D026):** Both overlapping requests can enter PendingConfirmation.
First valid transition to Confirmed reserves the unit/assigned-employee interval, including
customer acceptance of an alternative. Coordination alone does not win. Losing confirmation
is Conflict and retains its existing pending state (PendingConfirmation or AwaitingCustomerAcceptance).
No slot at request; release old confirmed slot on cancellation/rescheduling per BR-08.
Default duration is 60 minutes and the employee records final start/end. Proposed technical
boundary convention is half-open `[start,end)`. See Viewing Concurrency for the same policy.

Q03 is Accepted in D023–D030. Do not allow Cancelled, Completed, CustomerNoShow, or Expired
to become Confirmed without an explicit future decision/journey.
Staff/developer-unavailability cancellation is Cancelled with actor/reason (D029); neither is
CustomerNoShow or Completed. A Confirmed appointment does not auto-expire after its start (D028).

## — Booking Request Transitions — BR-10 / BR-11 / BR-12 / BR-13

| From | Event and Actor | To | Preconditions | Effect | Invalid/Late Behavior | Audit Data |
| --- | --- | --- | --- | --- | --- | --- |
| No request | Customer requests booking | PendingReview | Customer account exists; unit published | Booking request recorded | N/A | Timestamp, customer ID, unit ID, request reason |
| PendingReview | Employee records developer rejection | Rejected | Developer rejected | Reason and date recorded | If already Confirmed/Expired: reject with "cannot reject non-pending booking" | Timestamp, employee ID, rejection reason |
| PendingReview | Employee records developer confirmation | Confirmed | Developer confirmed with reference, price, deadline | Reference, price, deadline preserved | If already Confirmed/Expired/Rejected: reject with "invalid transition" | Timestamp, employee ID, developer reference, price, deadline |
| PendingReview | System/employee detects response-wait timeout (Proposed) | Rejected | Defined response-wait deadline reached, separate from reservation deadline | Reason response-timeout; preserve history, notify | Late confirmation audited, never confirms; terminal request unchanged | Timestamp, request ID, response cutoff and late response reference |
| PendingReview | Employee records expired offer (Proposed) | Rejected | Response is timely but supplied reservation deadline has elapsed | Reason invalid-reservation-deadline; never Confirmed | Terminal request unchanged; audit only | Timestamp, request ID, offered deadline and reference |
| PendingReview | Employee attempts conflicting confirmation (Proposed) | Rejected | Another valid local confirmation already committed | Local-conflict reason; preserve external response for reconciliation | Cannot overwrite winning booking | Timestamp, competing requests, external reference |
| Confirmed | Deadline expires | Expired | Deadline passed; system or employee records expiry | No automatic unit status change to Available | If already Expired/Completed/Cancelled: no-op | Timestamp, unit ID, original deadline |
| Confirmed | Employee cancels per Q06 policy | Cancelled | Cancel per Q06 policy; developer response recorded | Booking cancelled | If already Expired/Completed: reject with "cannot cancel expired or completed booking" | Timestamp, employee ID, cancellation reason, developer response reference |

Proposed late-response guards: confirmation requires a future reservation deadline and
an unelapsed response-wait deadline if that timeout policy is approved. At/after reservation
deadline, a Confirmed booking expires even if worker is late. Late responses for terminal
bookings are audited without reopening. See Late Developer Responses in CONCURRENCY_AND_IDEMPOTENCY.md.

Q06 Proposed cancellation: customer records a pending cancellation action; booking stays
Confirmed. Assigned employee coordinates externally, then records explicit approval to
cancel a still-valid booking, or rejection to close the action while retaining Confirmed.
Late approval after expiry/completion is reconciliation evidence only. No inferred unit
availability. Pending cancellation blocks linked sale adoption only as a Proposed guard.

Proposed extension: Confirmed → Confirmed, employee records fresh external confirmation
and future new deadline before old expiry, preserving old/new deadline/reference. Stale
write is Conflict; expiry timer rechecks current deadline. Expired is not reopened.

Recording deposit is not an automatic transition to Completed.

Open for approval: extension, late-response and competition policies above. Cancellation
before confirmation and multiple intentional pending bookings by same customer remain Open.

## — Unit Publication Transitions — BR-03 / BR-16

| From | Event and Actor | To | Preconditions | Effect | Invalid/Late Behavior | Audit Data |
| --- | --- | --- | --- | --- | --- | --- |
| Draft | Authorized catalog administrator publishes | Published | Unit data complete; catalog permission per ACTORS matrix | Announcement published; Publication and Availability independent | Wrong state/incomplete data: reject | Timestamp, actor ID, publish reason |
| Published | Authorized catalog administrator hides; system only under approved policy | Hidden | Explicit hide action; automatic data-loss/price-change triggers Proposed Q04 | Unit hidden; availability unchanged | Wrong/stale state: reject | Timestamp, actor ID, hide reason/policy |
| Hidden | Authorized catalog administrator re-publishes | Published | Data fixed; catalog permission | Unit returned to published state | Wrong state: reject | Timestamp, actor ID, fix reason |

Draft/Published/Hidden and Unknown/Available/Reserved/Sold enums are Proposed.
**Hiding announcement on price change is Proposed / NeedsUserDecision** (not an Accepted rule).
An expired reservation alone does not transition to Available; new external confirmation is required.
Unit publication or data-lost update and price change rules Q04.

## — Commission / Sale Transitions — BR-14 / BR-15

| From | Event and Actor | To | Preconditions | Effect | Invalid/Late Behavior | Audit Data |
| --- | --- | --- | --- | --- | --- | --- |
| Confirmed (booking) — Proposed linked-booking journey | Manager adopts sale | Separate completed sale + Booking→Completed (Proposed coupling) | Documented external SALE confirmation (UC-09), manager permission, applicable agreement/commission rule under D015–D021; booking still valid/current version; no pending cancellation (Proposed Q06 guard). Deposit prerequisite is Proposed ONLY, not unconditional | Create sale and lock commission snapshot; complete linked booking as one operation (Proposed). Unit availability transition remains Open, not automatic Reserved/Sold | Expired/wrong state/stale version: reject without partial effects; repeated same operation follows idempotency | Timestamp, manager, external sale reference, booking/sale IDs, price, commission snapshot; deposit reference only if applicable |
| — | (Proposed) Deposit recording required before sale adoption | — | — | **Proposed / NeedsUserDecision**: Recording external deposit confirmation is a prerequisite for sale adoption | — | — |

**Entity state summary at sale adoption:**
- Booking: Confirmed → Completed (Proposed linked-booking journey)
- Sale: Created (Completed)
- Unit: availability transition Open; preserve external sale evidence, do not infer a state rule from BR-16
- Commission: Entitlement locked (per BR-14, BR-15)

Open: Q02 commission formula details; Q06 deposit/cancellation policy.
This proposed linked-booking journey does not require every sale to have a booking.
Other sale journeys and exact unit-state update need decisions before implementation.

## — Terminal States —

| State | Domain | Transitions Out | Notes |
| --- | --- | --- | --- |
| Cancelled | Viewing / Booking | None | Final state; audit trail preserved |
| Completed (viewing) | Viewing | None | Final state; outcome recorded |
| CustomerNoShow | Viewing | None | Final state; outcome recorded |
| Expired | Viewing / Booking | None in proposed model | New booking uses new request + fresh external review; no implicit reopening |
| Rejected | Booking | None in proposed model | New request has a new identity |
| Completed (booking) | Booking | None in proposed linked-sale journey | Later expiry is no-op; corrections/reversals are separate Open journeys |
| Completed (sale) | Sale | None | Final state; commission tracked |

Open decisions affecting this step: Q04, Q06, Q07. Q01/Q05 remain open; Q02/Q03 are
Accepted in D015–D030. Other explicitly Open sale/booking policies remain unresolved.
