# M017 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: none; M017 is complete. The sanitized live rewriting report is presented at
handoff for human review before M018 consumes this path.
Blockers: none.
Last check: `python3 automation/context.py check M017` — pass on
2026-09-11T0114Z (29 sources, after reviewed lock refresh of the planned runner/script extensions).
Changed scope: none.

## Ordered tasks
- [x] T001 — Add `RewritingEnvelopeParser` + outcome model in `backend/src/LinguaDesk.Infrastructure.Ai/`; AC-001 (envelope portion); depends on none; done when offline MSTest cases prove every valid §4.2 rewriting form and every rejection class.
- [x] T002 — Add `rewriting.v1` prompt resource + `RewritingPrompt` composer + `RewritingPipeline` (M015 eligibility reuse, exactly-one mode validation, one transformation dispatch, strict validation, recorded failures) in the same library; AC-001, AC-002, AC-003; depends on T001; done when scripted-`IChatClient` tests prove mode acceptance/rejection, 4-language same-language validated results with Simplified zh output, prompt-data separation, refusal/invalid handling and zero transformation dispatches for ineligible input and invalid modes.
- [x] T003 — Add runner `evaluate-rewriting` (allowlisted 36-cell synthetic cases, budget flags, sanitized metadata-only report) with offline synthetic path; AC-004 (offline), AC-005; depends on T002; done when offline runs pass with no secrets in any artifact.
- [x] T004 — Execute the bounded live slice for `DeepSeek-V4.1-Flash` and obtain human review of the sanitized report; AC-004 (live); depends on T003; done when the budgeted live run records usage/exposure with retained failures, or is honestly reported blocked if the credential is absent.
- [x] T005 — Extend `scripts/ai.sh` + operating guide, run full regressions and the secret sweep, fill the completion record; all ACs; depends on T004 (offline portions may run if the live gate is blocked); done when `backend.sh check`, `contract.sh check`, `ai.sh check`, `context.py check M017` and the sweep pass with revision/environment/UTC time recorded.

## Completion record

Revision: `c0aa4d2` plus the uncommitted M017 working tree below.
Configuration: Release; .NET SDK `10.0.302`; Node `v24.20.0`.
Environment: Darwin arm64, UTC 2026-09-11T0114Z. One embedded prompt
resource added; the AI library still resolves only
`Microsoft.Extensions.AI.Abstractions` `10.9.0` as an external package
(plus the existing Core reference); no new package dependencies.

New files: `RewritingEnvelopeParser.cs`, `RewritingPrompt.cs`,
`RewritingPipeline.cs`, `Prompts/rewriting.v1.txt` in
`backend/src/LinguaDesk.Infrastructure.Ai/`; `RewritingEnvelopeParserTests.cs`
(29 cases), `RewritingPipelineTests.cs` (38 cases) in
`backend/tests/LinguaDesk.Infrastructure.Ai.Tests/`;
`EvaluateRewriting.cs` in `backend/tools/LinguaDesk.Ai.Evaluation/`.
Extended: `Program.cs` (`evaluate-rewriting` command), the AI library
project (embedded `rewriting.v1` resource), `scripts/ai.sh` (offline
default plus `--live` passthrough), `README.md` (operating procedure).

Prompt revisions: `rewriting.v1`
`b159cf743d8bbeb135411900fe1f9d9d070fe5332f153aae87c36aab1cd97091`
(plus reused `eligibility.v1`
`4f61eb19baced2342dd1a4b27f3b08846184f202674e90243ad2a635c37eb380`).

| AC / task | Command / procedure | Result and evidence |
| --- | --- | --- |
| AC-001 / T001 | `bash scripts/ai.sh check` (parser MSTest cases) | Pass: `artifacts/test-results/ai.trx` 229/229 (29 new rewriting-parser cases); every valid §4.2 form accepted, every rejection class (unknown/duplicate fields, unknown enums, missing fields, wrong types, multiple values, nesting, prose/fences, byte bound, result/refused pairing rules) rejected; refusal wording inside result text never parses as refusal |
| AC-001 / T002 | Same check (mode scripted cases) | Pass: all 9 catalog modes accepted with one validated result each; omitted mode defaults to `correctionOnly`; unknown/empty/multi-mode inputs rejected `invalid-mode` with zero dispatches; correction-only permits unchanged correct text |
| AC-002 / T002 | Same check (pipeline scripted cases) | Pass: all 4 languages yield one validated same-language complete plain-text result through the `rewriting.v1` prompt/pipeline; prompt carries the original complete source as serialized data with resolved source plus exactly one mode and no history; only validated plain text returned; Traditional Chinese input rewrites through the same pipeline |
| AC-003 / T002 | Same check (scripted refusal/invalid cases) | Pass: refusal, malformed/empty/truncated/tool-call/non-text output and provider failure are recorded failures (`refused`/`invalid-envelope`/`truncated`/`provider-failure`) with no salvage, repair, same-candidate retry or fallback; ineligible input (empty/oversize/terminal) and invalid modes end with zero transformation dispatches and no charge |
| AC-004 / T003 | `bash scripts/ai.sh evaluate-rewriting --profile DeepSeek-V4.1-Flash --max-dispatches 80 --max-spend-usd 0.50` (offline) | Pass: exit `0`, status `pass`, 42/42 match the allowlisted reference (36 language/mode cells incl. both Chinese input scripts, short-ambiguous, refusal/malformed-scripted, both gates, invalid-mode); `artifacts/rewriting/evaluate-rewriting-20260911T011024Z-offline.json` |
| AC-004 / T004 | `bash scripts/ai.sh evaluate-rewriting --live --profile DeepSeek-V4.1-Flash --max-dispatches 80 --max-spend-usd 0.50` | Pass: exit `0`, status `success`, 0 failed, 0 mismatches; all 36 cells succeeded live (both Chinese input scripts; nine zh cells returned validated complete results through the Simplified-mandating prompt), short-ambiguous genuinely `uncertain`, gates and invalid-mode zero-dispatch, refusal/malformed-scripted recorded `Skipped/offline-only`; 73 live dispatches through the M019 adapter path with known usage on every dispatch; reserved `$0.0117504`, unresolved `$0`; `artifacts/rewriting/evaluate-rewriting-20260911T011201Z-live.json`, presented at handoff for human review |
| AC-005 / T003–T004 | Pattern sweep over new sources/runner/scripts/reports plus credential-value absence check | Pass: 0 files contain the credential value; no key patterns; reports carry `credentialRef`/`credentialPresent`, case IDs, categories and numeric usage/exposure only; no key material, workspace text or hidden reasoning in any artifact |
| Regression / T005 | `bash scripts/backend.sh check` (353/353), `bash scripts/contract.sh check` (no drift), `python3 automation/context.py check M017` (29 sources green after reviewed lock refresh) | Pass |

Fixes during execution: two `CA1859` analyzer errors in the new
`EvaluateRewriting.cs` (interface-typed static field/return) fixed by using
concrete `Dictionary`/`List` types before the first green check; no lock
refresh beyond the plan-prescribed runner/script extensions (reviewed diff,
then re-locked). Limitations: the live slice is development evidence only,
not qualification — per-language mode-behavior and zh-cell
Simplified-output spot review stay with the human handoff and M020/M035+;
Q-001/Q-007 remain open; DF-001/DF-002/DF-003/DF-004/DF-006 stay deferred
with no retry, fallback, routing, translation-direction or serving code added.
