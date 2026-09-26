# Phase branches

Accepted user instruction, 2026-09-26: every Backend and Frontend phase has a descriptive branch.
Repository: https://github.com/mohamedfathy5242420-cloud/Real-Estate.git

- Backend: backend/pNN-short-description. Current P02: backend/p02-technical-foundation.
- Frontend: frontend/fNN-short-description. Current F02: frontend/f02-angular-foundation.
- Preserve existing P/F phase identifiers. All steps of a phase use its branch; commit messages name the step.
- Record branch and verification commit in evidence and handoff. Check the branch before editing.
- Use separate worktrees for simultaneous frontend/backend work; never switch branches over unsaved work.
- Create future branches only when their phase is authorized. Do not fabricate history for completed phases.
- Phase completion still requires evidence and phase review. No automatic merge, deletion or force push.
- Initial branch setup records workflow only; existing application files are not yet committed by this setup.

Verification: git ls-remote succeeded and returned no refs on 2026-09-26. Initial repository had no .git.
BusinessRefs / UseCaseRefs / AcceptanceRefs: N/A; Git organization only.
Review: self-review of naming against P02/F02 planning; no application behavior changed.
Application tests: N/A for this documentation-only change.
