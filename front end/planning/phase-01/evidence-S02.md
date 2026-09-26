# Evidence — F01-S02

Status: Done after user-authorized continuation and review. This is design evidence, not implementation or test evidence.
BusinessRefs: BR-01..BR-16. UseCaseRefs: UC-01..UC-10. ScreenRefs: SC-01..SC-10. AcceptanceRefs: FAC-01..FAC-10.

## Source and decision status

The five changed snapshots (BUSINESS_RULES, USE_CASES, STATE_TRANSITIONS, ACCEPTANCE_SCENARIOS, MVP_SCOPE) were copied from `D:/Practice/docs` into `docs/business/shared` on 2026-09-23; the canonical files were not edited. D015–D022 settle Q02 for agreement scope, commission and lead protection. D023–D030 settle Q03 for viewing duration, proposal window, expiry, conflicts, cancellation, outcomes and UTC storage. Q01, Q04–Q07 remain Open. State enum names and HTTP paths remain Proposed. SC-11 is a separate admin screen by design decision, but lacks canonical UC/FAC coverage and is excluded from this step and the validator.

## AC01 — Screen states

| Screen | Loading / empty / success | Error / forbidden / stale / conflict |
| --- | --- | --- |
| SC-01 catalog | Load published units; empty means no filter matches; success shows results and paging. | Retry load error. Freshness of availability is Q04; never imply stale data confirms a booking. |
| SC-02 unit detail | Load published detail; success shows price and source/time of last confirmation. | Unknown/unpublished unit follows server response. Login is required for request actions; changed price/availability requires a fresh response, not optimistic confirmation. |
| SC-03 account/session | Submit login/register/recovery according to Q01; success resumes intended journey. | Show validation/auth failure; expired session stops protected actions; no automatic replay of a mutation. |
| SC-04 customer viewings | Load own requests; empty means none; success shows current state and confirmed/proposed time distinctly. | 403 hides data; stale version/expired pending action refreshes server state; conflicting confirmation retains pending request. Never show internal notes. |
| SC-05 staff viewings | Load assigned requests or manager scope; empty means none; success shows coordination actions. | 403 after assignment change stops edits; expired pending action or overlapping confirmation refreshes state, leaving the request pending. |
| SC-06 clients/assignment | Load server-filtered assigned/unassigned lists; empty means no matching clients. | 403 prevents disclosure; concurrent reassignment refreshes owner and audit history. Q07 controls former employee historical access. |
| SC-07 catalog administration | Load developers/projects/units/agreements; empty state offers permitted creation; success reflects saved server result. | Show duplicate unit code or overlapping active agreement at field/form level; 403 hides mutations. Publication and availability remain separate. |
| SC-08 bookings | Load own/assigned bookings; empty means none; success distinguishes request from developer-confirmed reservation. | Timeout/duplicate submission requires status lookup before retry; expired deadline never marks unit Available; Q06 cancellation remains undecided. |
| SC-09 deals/commission | Load permitted deals; empty means none; success reads stored commission snapshot. | Agreement out of scope, expired lead protection, stale sale or duplicate collection shows server rejection. Do not recalculate old commission from today's agreement. |
| SC-10 notifications | Load own notifications; empty means none; success links to permitted resource. | Notification failure never reverses primary operation; inaccessible link produces forbidden state without revealing resource data. |

Conditional-state coverage below makes N/A explicit. All screens show a loading indicator while a request is pending, an empty message when a list has no results (or N/A for detail/forms), the server-confirmed success state, and a retryable error that preserves the last known safe state. No error view exposes internal notes or another user's data.

| Screen | Forbidden trigger / message | Stale trigger / message | Conflict trigger / message |
| --- | --- | --- | --- |
| SC-01 | N/A for public published list; unpublished entries are absent. | Q04 age policy Open; show last-confirmed time where supplied, without promising availability. | N/A for read-only list. |
| SC-02 | Protected action without account → login; 403 resource denial → no detail disclosure. | Changed price/availability → show latest server data and ask for explicit retry. | Request rejected after unit change → keep unsent/pending state and show current server result. |
| SC-03 | Failed authentication → generic denial without account disclosure. | Expired session → sign in again; do not replay mutation. | N/A until Q01 session contract defines a distinct concurrency response. |
| SC-04 | Other owner's request or revoked access → forbidden view, no content. | Pending action expired or version changed → refresh and explain current state. | Confirmation overlap → request remains pending; offer another time. |
| SC-05 | Assignment/permission lost → stop action and hide protected data on refresh. | Pending expiry or superseded proposal → refresh authoritative time/state. | Same unit/employee interval already confirmed → retain pending and coordinate alternative. |
| SC-06 | Employee outside assignment → no customer detail. | Transfer during open screen → refresh owner/history; Q07 controls old history. | Competing assignment change → show current owner and require explicit retry. |
| SC-07 | No catalog permission → no mutation form or protected data. | Edited record changed → reload current record before saving. | Duplicate unit code/overlapping active agreement → field/form error, no saved state. |
| SC-08 | Other customer's request or staff outside assignment → no booking data. | Deadline expired or external response changed → refresh status; never infer Available. | Duplicate/uncertain submission → query request status before any retry. |
| SC-09 | Non-manager without sale permission → no deal data. | Agreement/lead eligibility changed → refresh current server result; stored old snapshot stays unchanged. | Concurrent adoption/collection → show recorded outcome, no second effect. |
| SC-10 | Link target no longer owned/assigned → forbidden destination without detail. | Event points to an updated resource → fetch its current state. | N/A for read-only notifications; primary operation remains authoritative. |

Viewing-specific states (BR-07..09, D023–D030): proposal is pending and reserves no slot; default duration is 60 minutes, while the employee records final start/end on confirmation. PendingConfirmation expires at the earlier of seven days or proposed start; AwaitingCustomerAcceptance expires at alternative start. A confirmed viewing does not expire just because its start passed. First valid confirmation reserves a non-overlapping unit/employee interval; losing attempt retains pending state. A customer reschedule before the start cancels the old appointment immediately. Staff/developer unavailability is Cancelled with actor/reason/notification, never CustomerNoShow. Display times for the project/user; canonical storage is UTC.

## AC02 — Navigation and guards

| From → to | Trigger and displayed result | Guard / data condition |
| --- | --- | --- |
| `/` → `/catalog` → `/catalog/:unitId` | Open catalog, filter, inspect published unit. | Public; server supplies published data only. |
| Detail → `/auth/login` → intended request | Visitor chooses viewing/booking; return after login, then require a fresh explicit submission. | Q01 authentication; no automatic duplicate mutation. |
| Customer navigation → `/my/viewings` → viewing action | Customer opens own list, then proposes/accepts/cancels/reschedules; update from server result. | Customer session and request ownership; resolver loads own requests; before-start/expiry checks apply. |
| Employee navigation → `/employee/viewings` → viewing action | Assigned employee opens work list, then coordinates, confirms or records outcome. | Employee session plus current assignment and specific action permission; resolver loads assigned requests; conflicts refresh state. |
| Manager navigation → `/employee/viewings` | Manager opens all-viewings work list. | Manager session; resolver loads all permitted requests; recording outcomes still Proposed permission. |
| Manager navigation → `/admin/clients` → assignment action | Manager opens client queue, selects a customer and assigns/transfers. | Manager session; resolver loads permitted unassigned/all clients; Q07 history remains Open. |
| Employee navigation → `/employee/clients` | Employee opens assigned client list. | Employee session; resolver loads assigned clients only; no assignment command. |
| Admin navigation → `/admin/developers` → catalog/agreement form | System Admin opens catalog administration and permitted edit form. | System Admin catalog permission; resolver loads developers/projects; Sales Manager mutation remains Proposed. |
| Customer navigation → `/my/bookings` → own request | Customer opens own bookings after explicit request. | Customer session and ownership; resolver loads only own bookings. |
| Employee navigation → `/employee/bookings` → developer response | Assigned employee opens assigned queue and records external response if permitted. | Current assignment plus action permission; resolver loads assigned bookings. |
| Manager navigation → `/admin/bookings` | Manager opens all permitted bookings. | Manager session; resolver loads all; Q06 cancellation permission remains Open. |
| Manager navigation → `/admin/deals` → sale form | Manager reviews sale and historical commission. | Manager role; resolver loads permitted deals; agreement and lead protection checked by server. |
| Any signed-in user's navigation → `/notifications` → target | Open own notification then follow resource link. | Resolver loads recipient's events; recheck target ownership/assignment on navigation. |

Routes and resolver names from F01-S01 are Proposed. These are separate role-specific navigation paths, not links between customer, employee and manager areas. A 401 moves the user to login with safe return context; a 403 shows a forbidden state; neither a client guard nor a hidden button substitutes for server authorization. SC-11 `/admin/employees` stays outside S02 pending UC/FAC and permission decisions.

## AC03 — Form specifications (proposed UI and transport details)

| Screen / form | Inputs and client validation | Submit and response rule |
| --- | --- | --- |
| SC-01 filters | Optional: area (text/choice), minimum/maximum price and size (numbers), rooms (number), sort (choice), page (positive integer). Reject negative values and inverted ranges. | Update URL query and reload list; no minimum-one-filter rule. |
| SC-02 request entry | Required: selected unit ID (route identifier), explicit viewing or booking action (choice). Booking needs no viewing ID. Any reason field is contract-dependent. | Authenticate first; booking request shows review state, never Confirmed from submission success. |
| SC-03 authentication | Later FE-D008 selects required email and password for customer sign-in; verification and recovery inputs remain Q01 Open in canonical Business. Do not assume token/cookie transport or storage. | Preserve intended route; do not replay failed mutation automatically. |
| SC-04 propose/reschedule | Required: proposed start (date/time) in the future and within 30 days; request ID (identifier) for reschedule. Display default 60-minute duration; employee sets final end. | Server checks effective expiry/start and returns pending state; successful reschedule cancels old confirmed appointment. |
| SC-04 accept/cancel | Required: current request/alternative ID (identifier); optional cancellation reason pending contract. Validate action is before applicable start. | Acceptance at/after alternative start or cancellation at/after start is rejected; refresh state. |
| SC-05 confirm/alternative | Required: viewing ID, coordinated future start/end (date/time); optional external coordination reference per contract. Reject end at/before start locally. | Server checks final interval overlap for unit and assigned employee; conflict leaves request pending. |
| SC-05 outcome/cancel | Required: viewing ID and outcome choice (Completed/CustomerNoShow); internal notes optional text. Staff/developer cancellation requires reason (text). | CustomerNoShow only for customer absence; customer payload excludes notes; cancellation stores actor/reason and notifies customer. |
| SC-06 assign/transfer | Required: customer and target employee IDs (identifiers); reason for transfer is required when server policy demands it. | Manager only; server returns current owner/history. |
| SC-07 catalog/agreement | Required per operation: developer/project reference (identifier), project-scoped unit code (text), price (nonnegative amount), availability source/time; agreement developer (identifier), scope (all projects or specified IDs), effective dates, commission kind (percent/fixed) and value. Optional: project override and lead protection days (default 90, zero allowed). | Reject duplicate code and overlapping active agreement coverage; publication and availability save independently. No invented currency/tier/unit-level commission rule. |
| SC-08 booking response/deposit | Required for external confirmation: booking ID, developer reference (text), confirmed price (amount), future deadline (date/time). Deposit confirmation is external; exact fields and Q06 cancellation details await contract. | On uncertain submission result, look up status before retry; expiry does not set unit Available. |
| SC-09 sale/collection | Required for adoption: external sale reference, final sale price (amount), applicable agreement and project/lead identity when relevant; collection data depends on contract. | Server locks commission basis, price and amount snapshot; project override precedes agreement default; lead protection applies. |
| SC-10 notifications | No required submission form in current FAC-10; resource link is a server-provided identifier. | Link navigation rechecks ownership. |

HTTP method/path names in F01-S01 are illustrative API needs, not implemented or approved endpoints. Final field optionality, wire types and error codes belong to F01-S04 contract work.
The proposed operation/path inventory for SC-01..SC-10 is in `evidence-S01.md` (Screen Inventory, API Needs column). Two S02 actions extend that preliminary inventory: staff/developer-unavailability cancellation on SC-05 needs a proposed command such as `POST /viewings/:id/staff-cancellation`; recording external deposit confirmation on SC-08 needs a proposed command such as `POST /bookings/:id/deposit-confirmations`. These are design placeholders for F01-S04 review, not approved or implemented endpoints. Q06 still controls who may record deposit/cancellation details. No live endpoint has been verified.

## AC04 — Action permission checks

| Screen / action | Actor and permission | Boundary / review status |
| --- | --- | --- |
| SC-01/02 read published catalog | Visitor, customer, employee, manager and admin: A | Only published detail; Q04 freshness policy Open. |
| SC-02 request viewing/booking | Customer account owner: A | Login required; request is not confirmation. |
| SC-03 login/register/recovery | Customer registration/access; staff sign-in details unresolved | Q01 authentication and verification method Open; no broader resource permission follows from login. |
| SC-04 read own viewings and outcome | Customer request owner: A | Never includes internal notes. |
| SC-04 propose/accept alternative/cancel/reschedule | Customer request owner: A for own action | D023–D030 time/state guards still checked by server; Q01 transport Open. |
| SC-05 read assigned requests and internal notes | Current assigned employee: A; manager: A for all | Former employee active access denied; historical read Q07 Open. |
| SC-05 record developer result/outcome | Current assigned employee: A; manager: P | Assignment alone does not grant every command; manager command needs approval. |
| SC-05 initiate customer request/cancel/change as proxy | Current assigned employee: P; manager: P | ACTORS matrix proxy permission unresolved; do not render as approved command. |
| SC-06 read client/assignment history | Current employee: A for assigned clients; manager: A for all | Previous employee history Q07 Open; no new commands after transfer. |
| SC-06 assign/transfer client | Sales Manager: A | Employee and System Admin have no implicit assignment command. |
| SC-07 manage developers/units/agreements | System Admin: A approved draft; Sales Manager: P | Q02 Business rules Accepted, manager catalog permission still P. |
| SC-08 submit/read own booking | Customer request owner: A | No client access to other customers or staff notes. |
| SC-08 record developer response/deposit/cancel | Current assigned employee per role and assignment; manager only with specific permission | Q06 cancellation/deposit details Open; do not infer broad permission from read-all. |
| SC-09 adopt sale/commission | Sales Manager: A approved draft; employee preparation: P | Linked-booking/deposit prerequisites Proposed; D015–D021 rules Accepted. |
| SC-10 read notifications/follow link | Event recipient: A for own event | Recheck destination ownership/assignment; Q07 may revoke target access. |

Every mutation requires server-side role and current ownership/assignment. Proposed role details do not become approved through this design.

## AC05 — Frontend acceptance mapping

| FAC / screen | State and form | Navigation / authorization result; no test has run |
| --- | --- | --- |
| FAC-01 / SC-07 | Unit form shows duplicate-code error; agreement form rejects overlapping active coverage. All-project scope includes future developer projects; selected-project scope does not. Publication and availability fields stay separate. | Remain on edit form after rejection; authorized admin only. |
| FAC-02 / SC-01..03 | Public catalog/detail success; protected request prompts login; expired session shows re-entry state. | Login returns to intended unit without replaying mutation; owner-only resource remains server guarded. |
| FAC-03 / SC-06 | Assignment form saves manager-selected employee; later request shows same customer owner. Lead is linked to customer/project and does not get a new employee automatically. | Manager may transfer; employee list remains server-filtered; outside-scope link is forbidden. |
| FAC-04 / SC-04/05 | Alternative form shows AwaitingCustomerAcceptance; overlap at confirmation yields conflict and retains pending request. | Customer explicitly accepts; only server-confirmed state navigates to confirmed detail. |
| FAC-05 / SC-04/05 | Reschedule form before start cancels old slot and displays new PendingConfirmation; pending expires at earlier of seven days or proposed start; alternative expires at its start. Confirmed does not auto-expire. | Late/expired action refreshes state; old confirmed card is removed after successful reschedule. |
| FAC-06 / SC-04/05 | Outcome form keeps internal notes separate. Staff/developer absence or staff cancellation becomes Cancelled with actor/reason, never CustomerNoShow. | Customer sees status without notes in payload/view model/DOM; outsider and former employee active-resource links are forbidden. |
| FAC-07 / SC-02/08 | Booking request form needs no prior viewing; submission success shows PendingReview, not reservation. Duplicate/timeout queries status before retry. | Return from login to unit; never navigate directly to Confirmed from request submission. |
| FAC-08 / SC-08 | External deposit confirmation remains separate; expired booking shows its saved state but never auto-Available or sold/commissioned. | Staff view may record confirmed developer result; customer view contains only allowed fields. |
| FAC-09 / SC-09 | Sale form uses applicable agreement and lead protection; percent excludes fees/additions or fixed amount; project override precedes default. Saved snapshot holds rule, price and value. | Manager sees historical snapshot after agreement edit; duplicate adoption/collection shows recorded server outcome. |
| FAC-10 / SC-10 | Notification load error is local to notification panel; primary operation keeps saved state. | Following event link rechecks ownership; forbidden target reveals no resource data. |

## Verification, review and limits

- L1 command: `powershell -NoProfile -ExecutionPolicy Bypass -File "D:/Practice/front end/scripts/validate.ps1"`. First observed result: exit 1, drift in BUSINESS_RULES snapshot; the draft's earlier `exit 0` claim was false. After reviewing and copying the five changed canonical snapshots, the command returned exit 0: 37 required workflow files present, current state/handoff/step consistent, one or fewer InProgress steps, 16 BR / 10 UC / 10 SC / 10 FAC linked, and all 7 Business snapshots matched. This checks documentation structure and links only.
- L2 manual review: compared BR-07..09 and BR-14, D015–D030, UC-01/04/05/06/09, AC-01/06/08/09/13 with the screen, form and acceptance tables above. Result: Pass for design consistency; Q01/Q04–Q07 and SC-11 remain explicitly unresolved.
- L3 manual review: API_CONTRACT is Proposed; no HTTP endpoint or integration claimed. Result: Pass for scope/contract boundary.
- L4 application/browser acceptance: Not run — N/A, no Angular application exists.
- Angular build, unit and integration checks: Not run — N/A, documentation phase.
- Review: self-review of the corrected design plus an independent read-only agent review. That review found four coverage gaps and then one navigation ambiguity; all were addressed in this file. AC01..AC05 have design evidence above, and AC06 has the recorded validator result. F01-S02 is Done on the user's continuation authorization; no application behavior is claimed.
- TRACEABILITY.json implementation values remain NotStarted and testEvidence arrays remain empty; design documentation is not test evidence.
