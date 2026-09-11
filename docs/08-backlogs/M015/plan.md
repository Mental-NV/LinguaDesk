# M015 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Add the eligibility slice to the host-independent AI surface: a strict
`eligibility.v1` envelope parser, an operation-aware eligibility pipeline
(local zero-dispatch gates, one classification dispatch through the M019
adapter path, terminal outcome mapping), and an explicit budgeted runner
workflow executing a reviewed synthetic live slice. Nothing here transforms
text, retries, falls back, or touches API/UI/serving. Rejected input is never
transformed; a validation outcome is never overturned by fallback; provider
failure is recorded failure, not silent eligibility.

Selected canonical excerpts arrive through the context packet. Do not copy
them all here. The essential local invariants: parser implements exactly the
§4.2 table (eligible/hint-contradiction rule included); counting reuses Core
`unicode-scalar-v1` with no normalization; live runs need an explicit finite
dispatch+spend budget and a present credential, otherwise they report
blocked, never faked.

## Changes and order

1. `backend/src/LinguaDesk.Infrastructure.Ai/` — `EligibilityEnvelopeParser`
   plus outcome model: strict §4.2 acceptance/rejection classes, raw-byte
   bound before deserialization, hint-contradiction invalid rule. Depends on
   nothing; done when new MSTest offline cases prove AC-001 (every valid
   form, every rejection class).
2. Same library — `EligibilityPipeline` (operation-aware: translation 5000 /
   rewriting 2000 limits, explicit equal source/target invalid for
   translation): local gates via Core `ScalarInputPolicy` with zero
   dispatches, then one classification call through `EligibilityPrompt` +
   complete-response boundary, then terminal mapping per AC-003/AC-004.
   Depends on step 1; done when scripted-`IChatClient` tests prove AC-002
   (zero-dispatch counting), AC-003 (negatives, contradiction-invalid,
   malformed/failure handling) and AC-004 (resolved-language reuse, no
   transformation dispatch).
3. `backend/tools/LinguaDesk.Ai.Evaluation/` — `evaluate-eligibility`
   workflow: allowlisted synthetic case set (all supported languages, both
   Chinese scripts, short/ambiguous, mixed/unsupported, source-mismatch),
   `--profile/--max-dispatches/--max-spend-usd` budget flags reusing
   `EvaluationBudget`, metadata-only sanitized report under `artifacts/`
   with per-case outcome/category/usage/exposure and retained failures.
   Depends on steps 1–2; done when offline runs (scripted) and the live
   slice shape pass without secrets (AC-005/AC-006 offline portions).
4. `scripts/ai.sh` + operating guide — extend the AI command surface
   (`check` covers new tests, live `evaluate-eligibility` passthrough with
   credential stripping for offline modes) and document the explicit live
   invocation with finite budget flags; keep offline the default. Depends on
   step 3; done when `bash scripts/ai.sh check` covers the new tests and the
   README documents the reviewed procedure (AC-005/AC-006).
5. Regressions + live slice + human review — run `backend.sh check`,
   `contract.sh check`, secret sweep and `context.py check M015`; execute
   the bounded live slice; present the sanitized report for human review.
   Depends on step 4 (offline portions may run if the live gate is blocked);
   done when all ACs hold with revision/environment/UTC time recorded.

No migration, rollout or rollback: no storage, config-file or serving change.
No new package dependencies expected; the AI library keeps its single
abstractions reference.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 | T001 | V-007: `bash scripts/ai.sh check` (parser MSTest cases) | `artifacts/test-results/ai.trx` |
| AC-002 | T002 | V-001/V-007: same check (gate + zero-dispatch MSTest cases, shared fixtures) | same TRX + fixture report |
| AC-003/AC-004 | T002 | V-007: same check (scripted pipeline-mapping cases) | same TRX |
| AC-005 offline | T003 | V-007: same check + runner offline synthetic run | TRX + offline report |
| AC-005 live | T004 | V-008: `bash scripts/ai.sh evaluate-eligibility --profile DeepSeek-V4.1-Flash --max-dispatches <n> --max-spend-usd <amount>`; human reviews sanitized report | `artifacts/` sanitized report |
| AC-006 | T003/T004 | Secret sweep over sources/fixtures/scripts/reports; `credentialRef`-only diagnostics assertion | sweep result + report |
| Regression | T005 | `bash scripts/backend.sh check`, `bash scripts/contract.sh check`, `python3 automation/context.py check M015` | task completion record |

## Context boundaries and risks

Omitted domains and why: UX/browser (no UI surface), API handlers/auth
(no serving integration), durable accounting/settlement (evaluation budget
only; character charge lives with later API milestones), storage/migrations,
email, performance workloads (V-014 pending for M037), corpus qualification
and grading (V-013 pending for M020/M035/M036). Open on-demand: M016–M018
packages when transformation/fallback work starts; the DeepSeek research
snapshot only on adapter wire drift.

Dependencies and risks: M019 adapter/budget/credential path is Done and
reused; live-slice cost is bounded by explicit flags plus conservative
reservation; model classification of ambiguous/mixed input is development
evidence only, not qualification — the reviewed report records limitations.
If the credential is absent, AC-005 stays blocked and the remaining ACs
still complete.
