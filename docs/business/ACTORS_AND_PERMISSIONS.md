# Actors and permissions

This is a Business matrix, not final claims or role names.
A = Approved; P = Proposed detail needing review before execution; — = Not within role.

| Process | Visitor | Customer | Sales Employee (Current) | Sales Employee (Previous) | Sales Manager | System Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| Read published catalog | A | A | A | A | A | A |
| Request/Cancel/Change personal viewing | — | A: Only their own | P: Assigned client | — | P: All clients | — |
| Accept alternative date | — | A: Request owner | — | — | — | — |
| Assign and transfer client | — | — | — | — | A | — |
| Follow requests | — | A: Their requests | A: Assigned to | — | A: All | — |
| Internal notes | — | — | A: Assigned to | — | A | — |
| Record developer result and outcome | — | — | A: Assigned to | — | P | — |
| Manage developers, units and agreements | — | — | — | — | P | A: Approved draft |
| Adopt sale and commission | — | — | P: Preparation | — | A: Approved draft | — |
| Manage employee accounts and permissions | — | — | — | — | P | A: Approved draft |

No permission in the table is implied. Sales manager and proxy role details
are resolved before API. Ownership is verified on server. Hiding a button or route guard is not sufficient.

## Q07 — Employee Access After Client Transfer (Proposed / NeedsUserDecision)

**Current (Assigned) Employee:** May read assigned client's requests and internal notes. Commands require BOTH assignment and the specific role permission above; assignment does not grant manager commands or Proposed proxy permissions.

**Previous Employee (After Transfer):**
- **Current requests:** No access (cannot read or modify active viewings/bookings)
- **Historical data:** Access determined by Q07 policy (Proposed):
  - Option A (Proposed): No access — all prior assignments and notes visible only to Manager in audit log
  - Option B (Proposed): Read-only access to their own prior assignments and notes — no access to new data created after transfer
- **Command execution:** CANNOT perform new commands (create viewing, booking, assignment changes, record outcomes, complete tasks) on the transferred client
- **Audit log:** All prior assignments, notes, and timestamps remain visible to Manager in audit log per BR-15

**Sales Manager:** May read all clients and historical assignment/internal-note records. Commands still require the specific permission above; all-client scope is not permission to execute every command.

**System Administrator:** System-level access only; no automatic access to sales internal notes or client data. Access granted explicitly per role, not by virtue of being Admin.

**Decision required:** The Q07 policy above is Proposed / NeedsUserDecision. Not Accepted. Implementation must enforce the chosen policy at server level.
