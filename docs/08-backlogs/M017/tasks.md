# M017 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: none.
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T001 — Add `RewritingEnvelopeParser` + outcome model in `backend/src/LinguaDesk.Infrastructure.Ai/`; AC-001 (envelope portion); depends on none; done when offline MSTest cases prove every valid §4.2 rewriting form and every rejection class.
- [ ] T002 — Add `rewriting.v1` prompt resource + `RewritingPrompt` composer + `RewritingPipeline` (M015 eligibility reuse, exactly-one mode validation, one transformation dispatch, strict validation, recorded failures) in the same library; AC-001, AC-002, AC-003; depends on T001; done when scripted-`IChatClient` tests prove mode acceptance/rejection, 4-language same-language validated results with Simplified zh output, prompt-data separation, refusal/invalid handling and zero transformation dispatches for ineligible input and invalid modes.
- [ ] T003 — Add runner `evaluate-rewriting` (allowlisted 36-cell synthetic cases, budget flags, sanitized metadata-only report) with offline synthetic path; AC-004 (offline), AC-005; depends on T002; done when offline runs pass with no secrets in any artifact.
- [ ] T004 — Execute the bounded live slice for `DeepSeek-V4.1-Flash` and obtain human review of the sanitized report; AC-004 (live); depends on T003; done when the budgeted live run records usage/exposure with retained failures, or is honestly reported blocked if the credential is absent.
- [ ] T005 — Extend `scripts/ai.sh` + operating guide, run full regressions and the secret sweep, fill the completion record; all ACs; depends on T004 (offline portions may run if the live gate is blocked); done when `backend.sh check`, `contract.sh check`, `ai.sh check`, `context.py check M017` and the sweep pass with revision/environment/UTC time recorded.

## Completion record
Pending — execution has not started.
