# M018 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: none.
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T001 — Add `FamilyChain` snapshot + validation in `backend/src/LinguaDesk.Infrastructure.Ai/`; AC-001; depends on none; done when offline MSTest cases prove chain acceptance and every rejection class (unknown IDs, identical primary/fallback, route/mode-keyed selection) with zero dispatches.
- [ ] T002 — Add chain orchestrator (traversal, §7 mapping, deadline clock, per-attempt admission, credential-scope skip, late-output fencing) over both family pipelines in the same library; AC-002, AC-003, AC-004, AC-005; depends on T001; done when scripted-`IChatClient` plus fake-time/fake-admission tests prove both three-dispatch paths per family, the full advance/stop matrix, deadline enforcement at each stage and admission/exposure bounds.
- [ ] T003 — Add runner `evaluate-chain-bounds` (allowlisted small slice, primary-only enforcement, budget/deadline flags, sanitized metadata-only report) with offline scripted path; AC-006 (offline); depends on T002; done when offline runs pass with no secrets in any artifact.
- [ ] T004 — Execute the bounded primary-only live slice for `DeepSeek-V4.1-Flash` and obtain human review of the sanitized report; AC-006 (live); depends on T003; done when the budgeted live run records attempts/usage/exposure/deadline metadata with retained failures, or is honestly reported blocked if the credential is absent.
- [ ] T005 — Extend `scripts/ai.sh` + operating guide, run full regressions and the secret sweep, fill the completion record; all ACs; depends on T004 (offline portions may run if the live gate is blocked); done when `backend.sh check`, `contract.sh check`, `ai.sh check`, `context.py check M018` and the sweep pass with revision/environment/UTC time recorded.

## Completion record
Not started; no evidence yet.
