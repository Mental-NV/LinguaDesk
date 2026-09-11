# M016 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: none; M016 is complete. The sanitized live translation report is presented at
handoff for human review before M017/M018 consume this path.
Blockers: none.
Last check: `python3 automation/context.py check M016` — pass on
2026-09-11T01:00Z (28 sources, after reviewed lock refresh of the planned runner/script extensions).
Changed scope: none.

## Ordered tasks
- [x] T001 — Add `TranslationEnvelopeParser` + outcome model in `backend/src/LinguaDesk.Infrastructure.Ai/`; AC-001; depends on none; done when offline MSTest cases prove every valid form and every rejection class.
- [x] T002 — Add `translation.v1` prompt resource + `TranslationPrompt` composer + `TranslationPipeline` (M015 eligibility reuse, one transformation dispatch, strict validation, recorded failures) in the same library; AC-002, AC-003; depends on T001; done when scripted-`IChatClient` tests prove 12-direction validated results, prompt-data separation, refusal/invalid handling and zero transformation dispatches for ineligible input.
- [x] T003 — Add runner `evaluate-translation` (allowlisted 12-direction synthetic cases, budget flags, sanitized metadata-only report) with offline synthetic path; AC-004 (offline), AC-005; depends on T002; done when offline runs pass with no secrets in any artifact.
- [x] T004 — Execute the bounded live slice for `DeepSeek-V4.1-Flash` and obtain human review of the sanitized report; AC-004 (live); depends on T003; done when the budgeted live run records usage/exposure with retained failures, or is honestly reported blocked if the credential is absent.
- [x] T005 — Extend `scripts/ai.sh` + operating guide, run full regressions and the secret sweep, fill the completion record; all ACs; depends on T004 (offline portions may run if the live gate is blocked); done when `backend.sh check`, `contract.sh check`, `ai.sh check`, `context.py check M016` and the sweep pass with revision/environment/UTC time recorded.

## Completion record

Revision: `de4a859` plus the uncommitted M016 working tree below.
Configuration: Release; .NET SDK `10.0.302`; Node `v24.20.0`.
Environment: Darwin arm64, UTC 2026-09-11T01:00Z. One embedded prompt
resource added; the AI library still resolves only
`Microsoft.Extensions.AI.Abstractions` `10.9.0` as an external package
(plus the existing Core reference); no new package dependencies.

New files: `TranslationEnvelopeParser.cs`, `TranslationPrompt.cs`,
`TranslationPipeline.cs`, `Prompts/translation.v1.txt` in
`backend/src/LinguaDesk.Infrastructure.Ai/`; `TranslationEnvelopeParserTests.cs`
(28 cases), `TranslationPipelineTests.cs` (33 cases) in
`backend/tests/LinguaDesk.Infrastructure.Ai.Tests/`;
`EvaluateTranslation.cs` in `backend/tools/LinguaDesk.Ai.Evaluation/`.
Extended: `Program.cs` (`evaluate-translation` command), the AI library
project (embedded `translation.v1` resource), `scripts/ai.sh` (offline
default plus `--live` passthrough), `README.md` (operating procedure).

| AC / task | Command / procedure | Result and evidence |
| --- | --- | --- |
| AC-001 / T001 | `bash scripts/ai.sh check` (parser MSTest cases) | Pass: `artifacts/test-results/ai.trx` 162/162 (28 new translation-parser cases); every valid §4.2 form accepted, every rejection class (unknown/duplicate fields, unknown enums, missing fields, wrong types, multiple values, nesting, prose/fences, byte bound, result/refused pairing rules) rejected; refusal wording inside result text never parses as refusal |
| AC-002 / T002 | Same check (pipeline scripted cases) | Pass: all 12 Translation directions yield one validated complete plain-text result through the `translation.v1` prompt/pipeline; prompt carries the original complete source as serialized data with resolved source plus explicit target and no history; only validated plain text returned; Traditional Chinese input translates through the same pipeline |
| AC-003 / T002 | Same check (scripted refusal/invalid cases) | Pass: refusal, malformed/empty/truncated/tool-call/non-text output and provider failure are recorded failures (`refused`/`invalid-envelope`/`truncated`/`provider-failure`) with no salvage, repair or same-candidate retry; ineligible input (empty/oversize/terminal/auto-equal-target) ends with zero transformation dispatches and no charge |
| AC-004 / T003 | `bash scripts/ai.sh evaluate-translation --profile DeepSeek-V4.1-Flash --max-dispatches 34 --max-spend-usd 0.50` (offline) | Pass: exit `0`, status `pass`, 17/17 match the allowlisted reference (12 directions incl. both Chinese input scripts, short-ambiguous, refusal/malformed-scripted, both gates); 29 scripted dispatches; `artifacts/translation/evaluate-translation-20260911T005757Z-offline.json` |
| AC-004 / T004 | `bash scripts/ai.sh evaluate-translation --live --profile DeepSeek-V4.1-Flash --max-dispatches 34 --max-spend-usd 0.50` | Pass: exit `0`, status `success`, 0 failed, 0 mismatches; all 12 directions succeeded live (both Chinese input scripts; three zh-target cases returned validated complete results through the Simplified-mandating prompt), short-ambiguous genuinely `uncertain`, gates zero-dispatch, refusal/malformed-scripted recorded `Skipped/offline-only`; 25 live dispatches through the M019 adapter path with known usage on every dispatch; reserved `$0.0034182`, unresolved `$0`; `artifacts/translation/evaluate-translation-20260911T005830Z-live.json`, presented at handoff for human review |
| AC-005 / T003–T004 | Pattern sweep over new sources/runner/scripts/reports plus credential-value absence check | Pass: 0 files contain the credential value; no key patterns; reports carry `credentialRef`/`credentialPresent`, case IDs, categories and numeric usage/exposure only; no key material, workspace text or hidden reasoning in any artifact |
| Regression / T005 | `bash scripts/backend.sh check` (286/286), `bash scripts/contract.sh check` (no drift), `python3 automation/context.py check M016` (28 sources green after reviewed lock refresh) | Pass |

Fixes during execution: the first live run showed the two scripted edge
cases (refusal/malformed) succeeding live instead of matching their offline
references — expected, since a live provider cannot be forced to refuse or
malform; marked both `OfflineOnly` (proven offline, skipped live with an
explicit report row) and reran clean. Two test-authoring analyzer findings
fixed (`HasCount` on a scalar, `IsNotEmpty` rule). Limitations: the live
slice is development evidence only, not qualification — zh-target
Simplified-output and meaning/style/tone fidelity spot review stay with
M020/M035+; Q-001/Q-007 remain open; DF-001/DF-004/DF-006 stay deferred
with no retry, fallback, routing, rewriting-mode or serving code added.
