# F01-S03 UX design — proposed visual system

Status: Proposed visual design; English LTR first is Accepted (FE-D007). This file describes future UI behavior. No page or component has been built.

## Language, direction and formatting

- English text and left-to-right page flow are the first-release choice. Page language and direction should be declared explicitly when built. Arabic/RTL support later is open; no translation package or second locale is selected here.
- Layout uses logical start/end concepts so a later RTL design can be reviewed without changing Business meaning. Keep unit codes, external references and mixed-language user data readable in their natural direction; isolate them from surrounding text where needed. Labels stay separate from values.
- Business D030 stores viewing instants in UTC and displays them in the appropriate project/user timezone. The UI must distinguish a proposed start, final confirmed interval, expiry and original cancelled appointment. The exact display timezone selection, locale formatting and currency display require F01-S04 contracts and later user choice; do not infer currency from the workstation locale.
- Keep visible copy literal and distinct: “Viewing request sent” is not “Viewing confirmed”; “Booking request received” is not “Reservation confirmed”. Show “Pending”, “Confirmed”, “Cancelled” and “Expired” as human-readable copy only where the server state warrants it. State enum identifiers remain Proposed.
- Future localization preparation: avoid text baked into artwork, allow long labels and status text to wrap, and review date/number formatting through locale-aware facilities. Angular i18n can be evaluated in F02; no dependency decision is made now. References: https://angular.dev/guide/i18n and https://www.w3.org/International/questions/qa-html-dir .

## Visual hierarchy and proposed tokens

BrokerHub needs public discovery pages and dense staff/admin work screens. Propose one restrained system: light surfaces, dark navy text and deep teal primary actions. This is a reviewable visual recommendation, not an approved brand identity or logo.

| Token | Proposed value | Use |
| --- | --- | --- |
| Text | `#172B4D` | Main text, headings and labels on white/light surfaces |
| Primary | `#0B5C66` | Main action background with white text |
| Danger | `#A61B1B` | Error/destructive emphasis with white text; pair with words/icon |
| Canvas | `#F5F7F8` | Page background |
| Surface | `#FFFFFF` | Cards and forms |
| Strong boundary | `#6B7D86` | Input borders and visible separators |
| Spacing | 4, 8, 12, 16, 24, 32 CSS px | Consistent density; final tokens tested in F02 |
| Type | System UI font stack initially | Avoid dependency until final typography review; test mixed-script readability if Arabic is added |

Contrast calculation using WCAG relative luminance: main text/white 14.10:1, primary/white 7.67:1, danger/white 7.52:1, boundary/white 4.28:1. These are calculations for proposed colors, not browser or visual QA. Text and non-text contrast, focus visibility, reflow and keyboard behavior remain implementation checks. Target for future review: WCAG 2.2 AA, with additional focus indicator strength as a design preference. The 24×24 CSS px target-size rule has exceptions; use comfortably larger controls where practical. Reference: https://www.w3.org/TR/WCAG22/ .

Hierarchy: page title → short task context → primary content/actions → secondary details/history. Show at most one primary action per task region. Status never relies on color alone: use a word, icon where useful, and timestamp/source for time-sensitive data. Keep internal sales notes in a separate staff-only section and never in customer payload or view model (BR-09, FAC-06).

## Responsive layout proposal

Design review widths: 320 and 375 CSS px phone, 768 tablet, 1024 and 1440 desktop. These are review cases, not fixed device categories. Use one-column flow on narrow screens and expand only when space supports the task. Preserve content order and all actions under zoom/reflow. Two-dimensional admin data may need a deliberate horizontal-scroll region with persistent row identity; normal forms and journeys should not depend on page-wide horizontal scrolling.

| Screens | Narrow layout | Wide layout and priority |
| --- | --- | --- |
| SC-01 catalog, SC-02 detail | Filters open as an explicit panel; results/cards in one column; unit facts and request action follow title/price/status. | Filters beside grid; detail gallery beside facts. Always show availability source/time without implying reservation. |
| SC-03 auth | One-column labelled fields; validation next to field and in summary. | Narrow centered form; long recovery instructions remain readable. |
| SC-04/05 viewings | Each request is a card with current state, proposed/confirmed time and one task action; history can expand. | Queue/list plus detail pane. Staff coordination and internal notes remain distinct from customer content. |
| SC-06 clients, SC-07 catalog admin | Search and selected record first; edit form uses stacked fields; audit history collapses to a readable section. | Table/list plus edit panel; agreement scope and overlap errors stay adjacent to the relevant fields. |
| SC-08 bookings | Request state and developer deadline visible before actions; deposit and external confirmation separated. | Queue and detail; do not visually imply that a pending request reserved a unit. |
| SC-09 deals, SC-10 notifications | Deal snapshot summarized as labelled values; notification links have descriptive names. | Dense comparison/list may use a constrained table; historical snapshot stays visible on detail. |

SC-11 remains a separate admin screen design decision with no canonical UC/FAC coverage. Its layout and behavior await Business traceability and permissions; it is not a deliverable of F01-S03.

## Keyboard, focus and error behavior

- All actions must have a visible label and keyboard path in reading order; a user can enter and leave dialogs, filters and menus without a trap. Opening a dialog places focus at its title or first meaningful control; closing returns focus to the trigger. After route change, focus moves to the page heading or an equivalent landmark rather than silently staying on removed content.
- Inputs have persistent labels, instructions and field-level error text. A failed submission produces a short error summary and moves focus to it or the first invalid field. Error text explains the next action without exposing protected information. Do not clear valid fields when the server rejects a later step.
- Loading, success, expired, forbidden and conflict states include text, not color alone. Non-disruptive status updates are announced to assistive technology; urgent errors are announced without repeated noise. Server data remains authoritative after a conflict.
- Keyboard review covers catalog filters, detail action, login, viewing date choice and alternative acceptance, booking submission, admin assignment, agreement edit, sale adoption and notification links. Touch targets are reviewed on mobile; visual focus must be visible and not hidden by overlays.
- Future testing: keyboard only, screen reader smoke test, 320 CSS px reflow, zoom, contrast calculation on final colors, and long text. These checks are Not run while no app exists.

## FAC coverage and acceptance review cases

| FAC / screens | UX review case |
| --- | --- |
| FAC-01 / SC-07 | Duplicate code and agreement overlap errors sit beside fields; publication and availability have distinct controls and labels. |
| FAC-02 / SC-01..03 | Public browsing works without sign-in; protected action prompts login and returns without double submission. |
| FAC-03 / SC-06 | Manager assignment controls are clear; staff sees only assigned clients and current owner after transfer. |
| FAC-04 / SC-04/05 | Alternative remains pending until explicit customer acceptance and valid server confirmation; conflict prompts a fresh server state. |
| FAC-05 / SC-04/05 | Reschedule removes the old confirmed time after success, shows new pending time, and handles expiry without a false confirmation. |
| FAC-06 / SC-04/05 | Customer sees outcome but no internal notes; staff cancellation and customer no-show use different words. |
| FAC-07 / SC-02/08 | Booking request wording avoids a guarantee and works without a preceding viewing. |
| FAC-08 / SC-08 | Expiry and deposit never imply unit availability or completed sale. |
| FAC-09 / SC-09 | Stored rule, final price and commission value appear as historical snapshot, not a recalculated number. |
| FAC-10 / SC-10 | Failed notification load is local; linked resource rechecks access. |

All cases are design review cases, not passing application tests. Accepted Business rules D015–D030 remain distinct from proposed visuals, route/guard details, API wire contracts and later Arabic support.
