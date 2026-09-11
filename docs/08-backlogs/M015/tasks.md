# M015 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: none.
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T001 — Add `EligibilityEnvelopeParser` + outcome model in `backend/src/LinguaDesk.Infrastructure.Ai/`; AC-001; depends on none; done when offline MSTest cases prove every valid form and every rejection class.
- [ ] T002 — Add operation-aware `EligibilityPipeline` (Core counting reuse, zero-dispatch gates, one classification dispatch, terminal mapping); AC-002, AC-003, AC-004; depends on T001; done when scripted-client tests prove zero dispatches, negative/failure handling and resolved-language reuse with no transformation dispatch.
- [ ] T003 — Add runner `evaluate-eligibility` (allowlisted synthetic cases, budget flags, sanitized metadata-only report) with offline synthetic path; AC-005 (offline), AC-006; depends on T002; done when offline runs pass with no secrets in any artifact.
- [ ] T004 — Execute the bounded live slice for `DeepSeek-V4.1-Flash` and obtain human review of the sanitized report; AC-005 (live); depends on T003; done when the budgeted live run records usage/exposure with retained failures, or is honestly reported blocked if the credential is absent.
- [ ] T005 — Extend `scripts/ai.sh` + operating guide, run full regressions and the secret sweep, fill the completion record; all ACs; depends on T004 (offline portions may run if the live gate is blocked); done when `backend.sh check`, `contract.sh check`, `ai.sh check`, `context.py check M015` and the sweep pass with revision/environment/UTC time recorded.

## Completion record
Pending execution. Record per AC/task: revision/configuration, actual
command/procedure, environment, UTC timestamp, result/evidence link,
failures/fixes and limitations. No pasted command logs.
