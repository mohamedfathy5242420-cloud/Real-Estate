# Evidence — F00-S02
Command: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/validate.ps1
Actual: exit 0; 37 required files; consistent state; 16 BR, 10 UC, 10 SC, 10 FAC;
7 local snapshots match canonical source after LF/trailing-whitespace normalization.

Negative fixture: .artifacts/business-validation-negative (test documents only).
Check 1: replace SC-07 by SC-99 in trace map. Validator returned nonzero with Unknown screens reference: SC-99.
Check 2: restore screen reference, change snapshot title. Validator returned nonzero with Business snapshot drift.
Fixture setup initially hit filesystem permissions; approved retry succeeded.
First drift test used an absolute value in canonicalRootRelative and failed on invalid path rather than drift.
Corrected fixture to relative ../../..; rerun produced the expected drift rejection.
Production manifest remains .., production business unchanged.

| Criterion | Layer | Result | Limit |
| --- | --- | --- | --- |
| SA01 | L1/L3 | Snapshot files present and synchronized | Source absence would be Not verified |
| SA02/SA04 | L1/L3 | IDs covered and negative checks reject errors | Linkage not implementation proof |
| SA01/SA02 | L2 | Manual Business/UX review passed | Self-review |
| SA03 | L4 | Manual incoming-agent reading path reaches rules, screens, current task and next F01 | No separate agent |

No Angular build, browser tests, API integration or business behavior tests performed.
All implementation entries remain NotStarted. User review pending.

## Addendum — sync recheck 2026-09-21 (self-review)
Trigger: user asked to explain the contradiction between the earlier "7 snapshots match" claim and the validator's drift error.
Root cause of the wrong claim: the earlier check used PowerShell `diff` (alias of Compare-Object) on file *paths*, which compares the path strings — not file contents. That check was invalid.
Correct method: same normalized comparison as scripts/validate.ps1 (LF normalization + trailing-whitespace trim), plus scripts/validate.ps1 itself.
Actual result:
- Command 1: powershell -NoProfile -ExecutionPolicy Bypass -File "D:/Practice/front end/scripts/validate.ps1" → structure/linkage checks PASS (37 files, 16 BR, 10 UC, 10 SC, 10 FAC), then nonzero exit with: Business snapshot drift: docs/business/ACTORS_AND_PERMISSIONS.md.
- Command 2 (per-file normalized check via temp script): MATCH for BUSINESS_OVERVIEW, BUSINESS_RULES, USE_CASES, ACCEPTANCE_SCENARIOS, MVP_SCOPE (5 files); DRIFT for ACTORS_AND_PERMISSIONS.md and STATE_TRANSITIONS.md (canonical source contains new content — permission matrix and state tables — absent from local snapshots).
Conclusion: local snapshots are STALE for those 2 files. Do not rely on the local copies of ACTORS_AND_PERMISSIONS.md / STATE_TRANSITIONS.md until they are re-synced and their impact on screens/acceptance/API contract is reviewed.
Incident in this session: a premature F01-S01 draft overwrote planning/phase-00/steps/S01.md (original F00-S01 content). The file was fully restored to its original content; F01-S01 was not started.
Angular/browser checks: Not run (no app exists). Backend files: untouched.

## Addendum — P01-S03 backend sync & impact review 2026-09-21 (self-review)
Trigger: backend P01-S03 completed with corrections to ACTORS_AND_PERMISSIONS.md and STATE_TRANSITIONS.md (CONCURRENCY_AND_IDEMPOTENCY.md, DECISION_PACKAGE_P01_S03.md also updated but outside frontend manifest scope).
Authorization: annotation requires sync of manifest-tracked files + impact review; no code, no new step.
Sync method: Copy-Item FROM D:/Practice/docs/business TO docs/business/shared for the 2 manifest-tracked files only, byte-exact, no edits. Source files were only read (Get-Content) and compared — never written.
Commands and actual results:
- Per-file normalized recheck (temp script): MATCH 7/7 (BUSINESS_OVERVIEW, ACTORS_AND_PERMISSIONS, BUSINESS_RULES, USE_CASES, STATE_TRANSITIONS, ACCEPTANCE_SCENARIOS, MVP_SCOPE).
- powershell -NoProfile -ExecutionPolicy Bypass -File "D:/Practice/front end/scripts/validate.ps1" → exit 0. PASS: 37 files present/nonempty; state/handoff/active-step consistent; at most one InProgress; linkage 16 BR, 10 UC, 10 SC, 10 FAC; PASS: 7 Business snapshots match canonical source.
SOURCES.json: no update required — mappings unchanged, all 7 paths resolve.
Impact review (all new content Proposed/Open — nothing converted to Accepted, no API invented, no enums finalized):
- ACTORS matrix: line 9 (Request/Cancel/Change) now P for assigned employee & manager; Q07 current employee commands require BOTH assignment + specific permission; previous employee CANNOT new commands; manager all-client scope ≠ all commands. Frontend screens: SC-04/05 (owner/assigned), SC-06 (manager assign, Q07 noted), SC-07 (matrix), SC-08 (employee records response) — all consistent.
- STATE_TRANSITIONS: Common guards, Q03 expiry clock + Expired transitions, conflict policy (first Confirmed wins), response-wait timeout (Proposed), expired offer rejection (Proposed), conflicting confirmation (Proposed), Q06 pending cancellation (booking stays Confirmed), extension Proposed, catalog admin actor, sale adoption Proposed linked-booking + deposit Proposed. Frontend: SC-04/05 "الخادم يحسم الحد الزمني" + "تحديث الحالة عند التعارض"; SC-08 "idempotency متفق عليها" + "لا تعلن توفر دون رد حديث"; SC-07 "Actor: الإدارة حسب المصفوفة"; SC-09 "العقد النهائي يحسم" + "snapshot" — all conceptually aligned. Proposed state names (PendingConfirmation, AwaitingCustomerAcceptance, etc.) remain Proposed, not final enums.
- TRACEABILITY.json: all IDs resolve, validator linkage PASS — no mapping changes needed.
- API_CONTRACT.md: already Proposed, requires roles/ownership/server-decided availability/conflict/idempotency — consistent.
No frontend doc edits required. Observation for F01-S01 (unchanged): "Manage employee accounts" screen decision deferred; Q03/Q04/Q06/Q07 remain open for F01/F02.
Self-review (reviewer: assistant, type: self-review): SA01 (readable copy + sync 7/7) met; SA02 (mapping) met; SA03 (startup/handoff path) met; SA04 (validator IDs + sync) met. No blocking issue. F00-S02 conditions met except user approval → stays InReview per instruction.
Angular/browser/backend-integration: Not run — N/A (workflow-docs scope).

## Addendum — authorized sync completion 2026-09-21 (self-review)
Authorization: user approved completing F00-S02 only (sync 2 files, impact review, validate, self-review, handoff, stop; no F01-S01).
Sync method: Copy-Item FROM D:/Practice/docs/business TO docs/business/shared for the 2 drifted files only, byte-exact, no edits. Source files were only read (Get-Content) and compared — never written.
Commands and actual results:
- powershell -NoProfile -ExecutionPolicy Bypass -File "D:/Practice/front end/scripts/validate.ps1" → exit 0. PASS: 37 files present/nonempty; state/handoff/active-step consistent; at most one InProgress; linkage 16 BR, 10 UC, 10 SC, 10 FAC; PASS: 7 Business snapshots match canonical source.
- Per-file normalized recheck (temp script, removed afterwards): MATCH 7/7 (BUSINESS_OVERVIEW, ACTORS_AND_PERMISSIONS, BUSINESS_RULES, USE_CASES, STATE_TRANSITIONS, ACCEPTANCE_SCENARIOS, MVP_SCOPE).
SOURCES.json: no update required — it stores only source→snapshot mappings (no hashes); mappings are unchanged and all 7 paths resolve. Canonical files outside the manifest (CONCURRENCY_AND_IDEMPOTENCY.md, DECISION_PACKAGE_P01_S03.md) are out of frontend sync scope by design; noted, not synced.
Impact review (new content is explicitly Proposed/Open — nothing converted to Accepted, no API invented):
- Permission matrix vs screens: catalog read by all → SC-01/SC-02 ("الجميع") consistent; request/cancel/change scoped (own/assigned/all) → SC-04/SC-05 consistent; alternative accept by owner → FAC-04 consistent; assign/transfer manager-only → SC-06 consistent; internal notes excluded from customer → FAC-06 consistent; record-developer-result (employee assigned A, manager P) → SC-05/SC-08 consistent; sale/commission adoption (manager approved draft) → SC-09 consistent; manage developers/units (Admin approved draft, manager P) → SC-07 ("حسب المصفوفة") consistent.
- Observation for F01-S01 (not a change now): matrix row "Manage employee accounts and permissions" has no dedicated screen; screen inventory in F01-S01 must decide whether it needs one. Recorded, not resolved.
- Q07 section (Previous-employee access Options A/B, Proposed/NeedsUserDecision, server-level enforcement required) → SC-06 already references open Q07; FAC-03 unchanged. Q07 stays open; neither option adopted.
- State tables vs screens/acceptance: reschedule cancels old immediately + server decides time → FAC-05/SC-04 consistent; late/invalid behaviors (reject after start) → SC-04 "الخادم يحسم الحد الزمني" consistent; Expired never auto-restores availability + deposit never auto-completes → FAC-07/FAC-08/SC-08 consistent; Completed/CustomerNoShow outcomes with internal notes → SC-04/SC-05 + FAC-06 consistent; publication/availability independence → SC-07/FAC-01 consistent; commission snapshot locked → SC-09/FAC-09 consistent.
- Proposed English state names (PendingConfirmation, Confirmed, …) appear ONLY in the synced snapshot and the negative-test fixture; no other frontend doc adopts them as final enums. Removed PendingCustomerSuggestion is referenced nowhere in frontend docs. TRACEABILITY.json IDs (BR/UC/SC/FAC/phases) all still resolve — validator linkage PASS confirms it. API_CONTRACT.md (Status: Proposed) already requires roles/ownership/server-decided availability in the contract checklist — no change needed.
Self-review (reviewer: assistant, type: self-review, no independent reviewer): SA01 (readable copy + sync 7/7) met; SA02 (mapping) met; SA03 (startup/handoff path) met; SA04 (validator IDs + sync) met. No blocking issue found. F00-S02 meets all completion conditions EXCEPT user approval → status stays InReview per instruction; no Done claimed.
Angular build / unit / browser / backend-integration checks: Not run — N/A (no Angular app exists yet; workflow-docs scope).
