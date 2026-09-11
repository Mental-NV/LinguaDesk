# M020 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: none — all tasks done; package ready for human review at handoff. Blockers: none.
Last check: green — `ai.sh check` 271/271, `backend.sh check` 395/395, `contract.sh check` no drift, `context.py check M020` green after reviewed lock refresh. Changed scope: none.

## Ordered tasks
- [x] T001 — Combined-report model in `backend/tools/LinguaDesk.Ai.Evaluation/` (versioned schema, manifest, disposition labels, aggregates, blocked markers); AC-005 (schema portion); depends on none; done when MSTest schema/aggregate cases pass under `bash scripts/ai.sh check`.
- [x] T002 — Offline aggregation (`evaluate-report --offline` over the four slice paths, networking disabled, no credential read); AC-001; depends on T001; done when the offline combined run passes with zero dispatches and a validated report under `artifacts/evaluation/`.
- [x] T003 — Live aggregation (`--live` primary-only budgeted run, access probe, blocked-without-credential path, exposure totals, secret-safety); AC-002/AC-003/AC-004; depends on T002; done when the live shape is proven (blocked without credential; budgeted run with retained failures and exposure totals) and the secret sweep passes.
- [x] T004 — `scripts/ai.sh evaluate-report` surface (offline/live passthrough, offline credential stripping) plus operating-guide procedure; plan step 4; depends on T003; done when `bash scripts/ai.sh check` covers the new tests and the README documents the reviewed live procedure.
- [x] T005 — Regressions (`backend.sh check`, `contract.sh check`, `context.py check M020`), bounded live development batch against `DeepSeek-V4.1-Flash`, sanitized-report human review with recorded disposition; all ACs; depends on T004 (offline portions may run earlier if the live gate is blocked); done when every AC holds with revision/environment/UTC time recorded below.

## Completion record

Revision: `84b15f8` plus the uncommitted M020 working tree below.
Configuration: Release; .NET SDK `10.0.302`; Node `v24.20.0`.
Environment: Darwin arm64, UTC 2026-09-11T0154–0157Z. No new package
dependencies; the test lock gains only the runner `Project` reference
bookkeeping (no new packages); the AI library still resolves only
`Microsoft.Extensions.AI.Abstractions` `10.9.0` as an external package
(plus the existing Core reference).

New files: `EvaluateReport.cs` in
`backend/tools/LinguaDesk.Ai.Evaluation/` (versioned `evaluation_report`
format 1, run manifest, disposition-labeled sections reusing the four
slice observation shapes, per-family/route/language aggregates with
rewriting mode breakdowns, run/per-family exposure, blocked markers);
`EvaluationReportTests.cs` (6 cases) in
`backend/tests/LinguaDesk.Infrastructure.Ai.Tests/`.
Extended: `EvaluateEligibility.cs`/`EvaluateTranslation.cs`/
`EvaluateRewriting.cs`/`EvaluateChainBounds.cs` (mechanical extraction of
offline/live section cores sharing one `EvaluationBudget` for live runs;
per-slice reports, exit codes and observation shapes unchanged),
`Program.cs` (`evaluate-report` dispatch plus usage),
`LinguaDesk.Infrastructure.Ai.Tests.csproj` (runner `ProjectReference`
for the model tests), `scripts/ai.sh` (offline default with credential
stripping plus `--live` passthrough), `README.md` (operating procedure).

| AC / task | Command / procedure | Result and evidence |
| --- | --- | --- |
| AC-005 schema / T001 | `bash scripts/ai.sh check` (6 report-model MSTest cases) | Pass: `artifacts/test-results/ai.trx` 271/271 (265 existing + 6 new); schema carries every §7.2 field (format version, code/SDK revision incl. commit `84b15f8`, prompt/validator/settings/bundle hashes, candidate/profile/adapter/model/endpoint identity, corpus case IDs, billing snapshot, selection, concurrency 1, timestamps, dispositions), fault/live rows are disjoint in failure counts, aggregates reconcile with section rows, blocked sections carry zero rows, serialization names only `credentialRef`/`credentialPresent` |
| AC-001 / T002 | `bash scripts/ai.sh evaluate-report --profile DeepSeek-V4.1-Flash --max-dispatches 150 --max-spend-usd 2.00` (networking disabled, credential stripped by the script) | Pass: exit `0`, status `pass`, 80/80 rows match (12 eligibility, 17 translation, 42 rewriting, 9 chain-bounds); 4 injected-fault rows labeled `fault_injected`, 76 natural rows `offline_fixture`; run budget shows zero dispatches and no spend; `artifacts/evaluation/evaluate-report-m020-offline.json`, 2026-09-11T0154Z |
| AC-003 / T003 | Same live command with the credential variable unset | Pass: exit `3`, status `blocked`; all four live sections plus `live-access` listed blocked with zero rows/dispatches and no scripted substitution; the four offline fixture sections still complete (`pass`); `artifacts/evaluation/evaluate-report-m020-blocked.json`, 2026-09-11T0154Z |
| AC-002/AC-004 / T003 | `bash scripts/ai.sh evaluate-report --live --profile DeepSeek-V4.1-Flash --max-dispatches 150 --max-spend-usd 2.00` (single bounded batch) | Pass: exit `0`, status `success`, 0 failed, 0 mismatched; `live_access` probe succeeded (1 dispatch, known usage, returned `deepseek-flash`); 76 `live_development` rows all match (12 eligibility/10 dispatches, 15 natural translation/25, 40 natural rewriting/73, 9 chain/18; 4 offline-only fault rows skipped, never relabeled); one shared `EvaluationBudget`: 127/150 dispatches, reserved `$0.0192309`, settled actuals `$0.0192309`, unresolved `$0`; per-family exposure attributed from section deltas; `artifacts/evaluation/evaluate-report-m020-live.json`, 2026-09-11T0154–0156Z |
| Privacy/budget / T003 | Pattern sweep over new/changed sources, runner, scripts, test project and all three reports | Pass: only credential-variable name references (the established env-binding pattern); reports carry `credentialRef`, lengths/hashes, categories and numeric usage/exposure only — no source/result text, no key, header, env dump or raw provider body; the provider-returned model fingerprint is retained as the §5.4 change-detection observation, not key material |
| Surface / T004 | `bash scripts/ai.sh` usage plus README procedure review | Pass: `evaluate-report` follows the existing offline-default/`--live`-passthrough pattern with offline credential stripping; README documents the explicit finite-budget live invocation; offline stays the default |
| Regression / T005 | `bash scripts/backend.sh check` (395/395), `bash scripts/contract.sh check` (no drift), `python3 automation/context.py check M020` (13 sources green after reviewed lock refresh) | Pass |
| Human review / T005 | Sanitized combined live report presented at handoff | Disposition labeling, retained-failure handling and exposure completeness presented for human review as development acceptance (not qualification); recorded here at handoff |

Fixes during execution: the first offline run exposed that expected
`Failed` decisions on `fault_injected` rows were counted as failures;
failure counting now excludes `fault_injected` (and `Skipped`) rows in
both the builder and the exit-code path, keeping injected faults
disjoint from behavior evidence. Three test-analyzer errors
(`MSTEST0037` collection assertions, `CA1861` static array) and two
`CA1859` return-type suggestions were fixed before the first green
build; one test-data expectation (blocked dispatch count) was corrected
to the builder contract. The two changed locked sources (`Program.cs`
dispatch/usage, `scripts/ai.sh` surface) were diff-reviewed as the
planned additive surface with no requirement change before
`context.py lock M020`.
