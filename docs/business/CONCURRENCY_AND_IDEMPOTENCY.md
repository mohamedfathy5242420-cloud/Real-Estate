# Concurrency and Idempotency

This document covers business-level concurrent and duplicate operation outcomes.
It does not prescribe vendor or database implementation; those are chosen per phase.
Policies below are Proposed / NeedsUserDecision unless an Accepted D-reference is stated.
Sections are the shared policy reference for STATE_TRANSITIONS and DECISION_PACKAGE.

## — Preview (Viewing) Concurrency —
_PendingConfirmation and AwaitingCustomerAcceptance are for viewings ONLY. Do not use these states for booking requests._

| Scenario | Business Outcome |
| --- | --- |
| Two customers submit viewing requests for the same unit at overlapping times | Accepted D026: both enter PendingConfirmation; neither reserves a slot. First valid transition to Confirmed wins, including customer acceptance of an alternative. Coordination alone does not reserve the slot. Check both unit and assigned employee intervals at confirmation. Losing confirmation is Conflict; preserve its existing pending state (PendingConfirmation or AwaitingCustomerAcceptance), and notify that another slot is needed. |
| Same customer submits two viewing requests (duplicate resend of same operation) | Idempotent: the resend is recorded as a duplicate attempt in the audit log. The original PendingConfirmation request is NOT cancelled and does NOT create a second pending viewing. |
| Same customer submits a new intentional viewing request after first is still pending | Not a duplicate: a new intentional request creates a separate PendingConfirmation entry. The customer can have multiple pending viewings. |
| Employee modifies a viewing while another employee views it | A read causes no transition. Only a permitted command follows the state table; the reader sees changes on refresh. A subsequent stale write is Conflict, not an implicit reset of the viewing. |

### Q03 accepted expiry clock (D025)

PendingConfirmation uses the latest customer-proposed start; AwaitingCustomerAcceptance
uses the currently offered alternative start; Confirmed uses its confirmed start.
Superseded dates remain history only. PendingConfirmation expires at the earlier
of that start and seven calendar days from entering the current coordination cycle.
A new customer time proposal starts a new cycle; a retry does not reset the clock.
AwaitingCustomerAcceptance expires at the alternative start, not an old coordination
deadline. A separate customer-response timeout is Open. At/after the effective cutoff,
confirmation is rejected even if the expiry worker is delayed; preserve history as Expired.
These clocks/reference choices are Accepted D025. Default duration is 60 minutes (D023);
the half-open interval convention remains a Proposed technical implementation detail.

## — Booking Request Concurrency —
_Booking requests use PendingReview, Confirmed, Expired, Rejected. Do NOT use PendingConfirmation/AwaitingCustomerAcceptance for bookings._

| Scenario | Business Outcome |
| --- | --- |
| Two different customers submit booking requests for the same unit at the same time | Proposed: both enter PendingReview, not duplicates. First valid external confirmation committed locally wins. A competing confirmation rejects the losing pending request with local-conflict reason and notification; preserve its external response for reconciliation. Merely receiving an external response is not the winning commit. Local exclusion does not guarantee external availability (BR-16). |
| Same customer submits two booking requests for the same unit (duplicate resend) | Idempotent: the resend is recorded as a duplicate attempt in the audit log. The original PendingReview request is NOT cancelled and does NOT create a second PendingReview entry. |
| Same customer submits a new intentional booking request after first was rejected/expired | Not a duplicate: a new intentional request creates a separate PendingReview entry. |
| Employee modifies a Confirmed booking while another employee views it | Reading causes no transition. Only the specific authorized command follows the state table; editing a booking is not sale adoption. Reader refreshes; stale mutation is Conflict. |

## Stale Writes

| Scenario | Business Outcome |
| --- | --- |
| Employee submits a result based on an old cached unit state (e.g., unit still shows Available but was reserved moments before) | The stale write is **rejected as Conflict**. The system logs the attempt in the audit trail with timestamp and actor, then requires a data reload. The unit status does not automatically change; explicit re-confirmation per BR-12 is required. |
| Customer requests cancellation of an old booking while a new request is being processed | Proposed Q06: record a pending cancellation action; old booking remains Confirmed until employee records external approval while still valid. New request has its own ID/state; it is not auto-confirmed/rejected by the cancellation request. Competing confirmations follow Booking Request Concurrency. Late approval after expiry/completion is audited, not applied. |

## Repeated Commands

| Scenario | Business Outcome |
| --- | --- |
| Customer repeatedly clicks "Submit booking request" (resend same operation) | Idempotent: the first click creates the PendingReview request. Subsequent resends are recorded as duplicate attempts in the audit log and do NOT create additional PendingReview entries. The original request continues unchanged. |
| Customer intentionally submits a new booking request after previous was rejected/expired | Not a duplicate: creates a new PendingReview entry. |
| Manager repeatedly assigns the same client to different employees | Proposed: first valid versioned commit wins; competing stale assignment is Conflict. A deliberate reassignment after reload remains allowed. Same operation retry causes no second assignment event; history remains per BR-15. |

## Late Developer Responses — Booking (Separate from Q03 Viewing Timeout)

| Scenario | Business Outcome |
| --- | --- |
| Confirmation arrives at/after the PendingReview response-wait deadline | Proposed: record late response, reject PendingReview with reason response-timeout; never confirm. Already terminal request stays terminal. New attempt needs a new request and fresh review. |
| Developer fails to respond within the agreed response window | Proposed: PendingReview becomes Rejected with reason response-timeout. Notify customer; preserve history. |
| Timely confirmation carries an already elapsed reservation deadline | Proposed: PendingReview becomes Rejected with reason invalid-reservation-deadline. Preserve the external response; never create Confirmed with an elapsed deadline. |
| Confirmed reservation reaches its external reservation deadline | Confirmed becomes Expired (BR-12); unit is not automatically Available. |
| External response arrives for Expired/Rejected/Cancelled/Completed booking | Proposed: audit only; no reopening/overwrite. New booking requires fresh request and external review. |
| **Note** | Response-wait deadline belongs to PendingReview; reservation deadline belongs to Confirmed (BR-11). Neither is Q03. Response-wait duration is Open: three business days is only a candidate requiring calendar/start-time definition, not an authorized default. |

## Assignment Changes During Work

| Scenario | Business Outcome |
| --- | --- |
| Client is assigned to Employee A, then reassigned to Employee B while Employee A is working the case | Employee A's access after transfer is defined by Q07 (Proposed/NeedsUserDecision): current access revoked, historical access per Q07 policy, NO new command execution on transferred client. Historical assignments and actor timestamps preserved per BR-15. |
| Transfer includes historical access policy (Q07) | After transfer, Employee A's access to the client's current and historical data is determined by the Q07 policy (Proposed/NeedsUserDecision). All prior assignments and notes remain in the audit log for the manager. |

## Expiration Races

| Scenario | Business Outcome |
| --- | --- |
| A booking's deadline expires while the customer is viewing the request | The system records the expiration. The **booking** transitions from Confirmed to Expired. The unit does NOT automatically become Available; new developer confirmation per BR-12 is required. |
| Multiple concurrent expiry checks for the same booking | Only the first expiry transition takes effect. Subsequent checks find the booking already in Expired state and log the race as informational. |
| Viewing pending expiry reached | Accepted D025: apply the Q03 expiry clock above. Expired retains history; notify customer. Old original time cannot expire a later offered alternative. |
| Booking expires concurrently with linked sale adoption | Proposed linked-booking journey: adoption needs documented external sale confirmation, manager permission, valid agreement/commission rule and unexpired Confirmed booking. Check time/state/version at commit. At/after deadline, expiry wins even if worker is late. Adoption committed earlier completes booking; subsequent expiry is no-op. Other sale journeys remain Open; not every sale is required to have a booking. |
| Deadline extension requested before expiry | Proposed: employee records fresh external confirmation with future deadline before old deadline elapses; preserve old/new reference and deadline. Stale extension is Conflict. After expiry, new request is required; no reopening. An old expiry timer must recheck the updated deadline. |
| Cancellation approval races with extension or linked sale adoption | Proposed Q06: check current state/version/time at commit; first valid commit wins, stale mutation is Conflict. A pending cancellation action blocks linked sale adoption until resolved (Proposed guard). External cancellation rejection closes the action without cancelling the booking. Late approval after Expired/Completed is retained for reconciliation only. |

## Design Notes

- Cache is never the authority for booking confirmation (per BR-16). All business outcomes are based on the source of truth recorded in the system.
- Listed resolved recommendations have explicit outcomes; Open scenarios still need decisions. Nothing here proves implemented concurrency safety.
- Idempotency keys are recommended at the API level to enable safe retry without application-level duplicate detection.
- Recheck current role AND ownership/assignment for mutations and retry responses. A retry after transfer must not reveal a result the previous employee can no longer read (Q07). Ownership alone never grants every command.
- Same-customer multiple intentional pending bookings for the same unit, and cancellation before confirmation, remain Open; do not confuse these with retries.
- **Preview (viewing) and Booking (reservation) flows have separate state paths. Do not mix PendingConfirmation/AwaitingCustomerAcceptance with booking states.**
- **Duplicate resend vs new intentional request distinction is Proposed / NeedsUserDecision.**
- **Booking response timeout is separate from Q03 viewing coordination timeout.**

Open decisions affecting this step: Q04, Q06, Q07 plus explicitly listed booking/sale policies.
Q03 viewing first-confirmed/expiry behavior is Accepted D023–D030; unrelated first/last policies
remain Proposed unless separately accepted.
