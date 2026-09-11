# M015 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: none; M015 is complete. The sanitized live eligibility report is presented at
handoff for human review before M016–M018 consume this path.
Blockers: none.
Last check: `python3 automation/context.py check M015` — pass on
2026-09-11T00:43Z (22 sources, after reviewed lock refresh of the planned runner/script extensions).
Changed scope: none.

## Ordered tasks
- [x] T001 — Add `EligibilityEnvelopeParser` + outcome model in `backend/src/LinguaDesk.Infrastructure.Ai/`; AC-001; depends on none; done when offline MSTest cases prove every valid form and every rejection class.
- [x] T002 — Add operation-aware `EligibilityPipeline` (Core counting reuse, zero-dispatch gates, one classification dispatch, terminal mapping); AC-002, AC-003, AC-004; depends on T001; done when scripted-client tests prove zero dispatches, negative/failure handling and resolved-language reuse with no transformation dispatch.
- [x] T003 — Add runner `evaluate-eligibility` (allowlisted synthetic cases, budget flags, sanitized metadata-only report) with offline synthetic path; AC-005 (offline), AC-006; depends on T002; done when offline runs pass with no secrets in any artifact.
- [x] T004 — Execute the bounded live slice for `DeepSeek-V4.1-Flash` and obtain human review of the sanitized report; AC-005 (live); depends on T003; done when the budgeted live run records usage/exposure with retained failures, or is honestly reported blocked if the credential is absent.
- [x] T005 — Extend `scripts/ai.sh` + operating guide, run full regressions and the secret sweep, fill the completion record; all ACs; depends on T004 (offline portions may run if the live gate is blocked); done when `backend.sh check`, `contract.sh check`, `ai.sh check`, `context.py check M015` and the sweep pass with revision/environment/UTC time recorded.

## Completion record

Revision: `c9d011b` plus the uncommitted M015 working tree below.
Configuration: Release; .NET SDK `10.0.302`; Node `v24.20.0`.
Environment: Darwin arm64, UTC 2026-09-11T00:43Z. One new project
reference: the AI library references `LinguaDesk.Core` for the shared
`unicode-scalar-v1` policy; the AI library still resolves only
`Microsoft.Extensions.AI.Abstractions` `10.9.0` as an external package.

New files: `EligibilityEnvelopeParser.cs`, `EligibilityPipeline.cs` in
`backend/src/LinguaDesk.Infrastructure.Ai/`; `EligibilityEnvelopeParserTests.cs`
(39 cases), `EligibilityPipelineTests.cs` (17 cases) in
`backend/tests/LinguaDesk.Infrastructure.Ai.Tests/`;
`EvaluateEligibility.cs` in `backend/tools/LinguaDesk.Ai.Evaluation/`.
Extended: `Program.cs` (`evaluate-eligibility` command), the runner project
(Core reference), the AI library project (Core reference), `scripts/ai.sh`
(offline default plus `--live` passthrough), `README.md` (operating procedure).

| AC / task | Command / procedure | Result and evidence |
| --- | --- | --- |
| AC-001 / T001 | `bash scripts/ai.sh check` (parser MSTest cases) | Pass: `artifacts/test-results/ai.trx` 101/101; every valid §4.2 form accepted, every rejection class (unknown/duplicate fields, unknown enums, missing fields, wrong types, multiple values, nesting, prose/fences, byte bound, hint-contradiction) rejected |
| AC-002 / T002 | Same check (pipeline gate MSTest cases) | Pass: empty/oversize (translation 5000/rewriting 2000, excess reported)/equal-source-target/invalid selectors end locally with zero dispatches and no charge; exact limits reach classification |
| AC-003 / T002 | Same check (scripted mapping cases) | Pass: uncertain/unsupported/mixed/source-mismatch/refused end terminally with no transformation; hint-contradiction, malformed and provider failure record `invalid-envelope`/`provider-failure`, never eligible |
| AC-004 / T002 | Same check (scripted eligible cases) | Pass: accepted classification returns the resolved language for reuse with exactly one classification dispatch and no transformation dispatch |
| AC-005 / T003 | `bash scripts/ai.sh evaluate-eligibility --profile DeepSeek-V4.1-Flash --max-dispatches 12 --max-spend-usd 0.50` (offline) | Pass: exit `0`, status `pass`, 12/12 match the allowlisted reference; gate cases prove zero dispatches offline; `artifacts/eligibility/evaluate-eligibility-20260911T004250Z-offline.json` |
| AC-005 / T004 | `bash scripts/ai.sh evaluate-eligibility --live --profile DeepSeek-V4.1-Flash --max-dispatches 12 --max-spend-usd 0.50` | Pass: exit `0`, status `success`, 12/12 match the development reference with zero failures; 10 live dispatches through the M019 adapter path; known usage on every dispatch; reserved `$0.0013143` settled to actual, unresolved `$0`; rejected inputs not transformed; `artifacts/eligibility/evaluate-eligibility-20260911T004212Z-live.json`, presented at handoff for human review |
| AC-006 / T003–T004 | Pattern sweep over new sources/runner/scripts/reports plus credential-value absence check | Pass: key only in the narrow transport type; reports carry `credentialRef`/`credentialPresent`, case IDs, categories and numeric usage/exposure only; no key material, workspace text or hidden reasoning in any artifact |
| Regression / T005 | `bash scripts/backend.sh check` (225/225), `bash scripts/contract.sh check` (no drift), `python3 automation/context.py check M015` (22 sources green after reviewed lock refresh) | Pass |

Fixes during execution: tightened the parser so `source_mismatch` without a
non-null differing hint is invalid (the checked-in prompt requires a non-null
hint); corrected a test that asserted unescaped JSON source text instead of
decoding it. Limitations: the live slice is development evidence only, not
qualification — ambiguous/mixed judgments and all quality/cost/latency gates
stay with M020/M035+; Q-001/Q-005/Q-007 remain open; DF-001/DF-004/DF-006 stay
deferred with no transformation, fallback, routing or serving code added.
