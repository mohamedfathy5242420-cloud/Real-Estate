# BrokerHub working contract
Current authorization: continue the backend following the recorded workflow. P01 is Done;
P02 owns the technical foundation and transport-neutral identity/authorization boundaries.
Follow docs/CURRENT_STATE.md for the active step.
Read in order:
1. docs/PROJECT_CONTEXT.md
2. docs/DECISIONS.md, docs/RISKS.md and docs/MVP_SCOPE.md
3. docs/CURRENT_STATE.md
4. docs/WORKFLOW.md and docs/VALIDATION.md
5. handoffs/CURRENT_HANDOFF.md
6. docs/business/README.md and its required reading order (overview, actors, rules, use cases, states, acceptance and traceability).
7. Active phase/step files and their BusinessRefs / UseCaseRefs / AcceptanceRefs.

Rules:
- docs/business is the detailed Business source of truth. PROJECT_CONTEXT is only a summary.
- Every business implementation step must name BR/UC/AC IDs and the applicable open decisions before editing code.
- Changes to rules must update use cases, acceptance, traceability and downstream API contracts; never silently change approved rules.
- Proposed state names/permissions and Open questions are not approval. Verify actual implementation status from testEvidence, not documentation presence.
- One bounded active step, with explicit scope and acceptance criteria.
- Approved business decisions are not re-asked. New assumptions remain labeled.
- Clean Architecture by layer/type; no Vertical Slice.
- Planning, implementation and review are roles, not fixed models.
- No automatic agent delegation. A handoff does not authorize spawning agents.
- Existing roadmap is not authorization to implement it.
- Preserve unrelated user edits.
- Record actual verification commands/results, not expected outcomes.
- Mark unrun checks Not run and inapplicable checks N/A with reasons.
- A step requires evidence, review, current state and handoff updates to be Done.
- A phase additionally requires a phase review.
- Be explicit when review is self-review, not independent.
- Do not publish, deploy or message others because a template says so.
- Communicate in Egyptian Arabic; keep work educational and reviewable.
