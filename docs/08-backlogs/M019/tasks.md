# M019 — Tasks and evidence

Inputs: [spec](spec.md), [plan](plan.md).

## Resume

Next: none; M019 is complete. The sanitized live-access result is presented at
handoff for human review before M015 consumes this path.
Blockers: none.
Last check: `python3 automation/context.py check M019` — pass on
2026-09-10T23:40Z (14 sources). Changed scope: none.

## Ordered tasks

- [x] T001 — Add candidate profile model + registry validation in `backend/src/LinguaDesk.Infrastructure.Ai/`; AC-001; depends on none; done when MSTest offline cases prove multi-profile load, duplicate/invalid rejection and route-neutral selection.
- [x] T002 — Add the OpenAI-compatible Chat Completions adapter (DeepSeek dialect, bounded reads, cancellation, dispatch counting, error/usage mapping) with fake-handler conformance fixtures; AC-002, AC-003; depends on T001; done when offline conformance and fault/cancel/bound/one-dispatch cases pass with sanitized fixtures.
- [x] T003 — Add the credential-resolver boundary (evaluation-only secret mapping, `credentialRef`/`credentialPresent` diagnostics) and prove offline runs read no credential variables with networking disabled; AC-004; depends on T002; done when secret-sweep and env-absence tests pass.
- [x] T004 — Add runner profile selection, offline `conformance` and live `verify-access` (one fixed-synthetic budget-admitted dispatch; blocked without credential); AC-005, AC-006; depends on T003; done when the selectable secondary-reference path passes offline and the primary profile records one sanitized bounded live success.
- [x] T005 — Extend `scripts/ai.sh` + operating guide, run full regressions and the secret sweep, fill the completion record; all ACs; depends on T004 (offline portions may run if the live gate is blocked); done when `backend.sh check`, `contract.sh check`, `ai.sh check`, `context.py check M019` and the sweep pass with revision/environment/UTC time recorded.

## Completion record

Revision: `df50293` plus the uncommitted M019 working tree below.
Configuration: Release; .NET SDK `10.0.302`; Node `v24.20.0`.
Environment: Darwin arm64, UTC 2026-09-10T23:40Z. No new package
dependencies: the AI library still resolves only
`Microsoft.Extensions.AI.Abstractions` `10.9.0`.

New files: `CandidateProfile.cs`, `CandidateRegistry.cs`,
`ChatCompletionsAdapter.cs`, `EvaluationBudget.cs`, `TransportCredential.cs`
in `backend/src/LinguaDesk.Infrastructure.Ai/`; `CandidateRegistryTests.cs`,
`ChatCompletionsAdapterTests.cs`, `CredentialAndBudgetTests.cs` in
`backend/tests/LinguaDesk.Infrastructure.Ai.Tests/`. Extended:
`backend/tools/LinguaDesk.Ai.Evaluation/Program.cs` (`conformance`,
`verify-access`), `scripts/ai.sh` (offline credential stripping,
`conformance` in `check`, live `verify-access` passthrough), `README.md`
(operating procedure).

| AC / task | Command / procedure | Result and evidence |
| --- | --- | --- |
| AC-001 / T001 | `bash scripts/ai.sh check` (10 registry MSTest cases) | Pass: `artifacts/test-results/ai.trx` 45/45; two selectable profiles, duplicate/invalid rejection |
| AC-002 / T002 | Same check (request-settings MSTest cases) + runner `conformance` over sanitized fixtures | Pass: endpoint/model/auth, temperature `0`, no `top_p`/tools, thinking disabled, bounded JSON output |
| AC-003 / T002 | Same check (fault/cancel/bound/dispatch cases) | Pass: status-only error mapping, cancellation bounded with retained reservation, capped reads, exactly one dispatch, unsupported settings rejected |
| AC-004 / T003 | Same check (credential/budget cases) + pattern sweep over sources/fixtures/scripts | Pass: key only in the narrow transport type, `ToString` redacted, reports/diagnostics reference-only, offline paths ignore ambient variables; sweep found no key material |
| AC-005 / T004 | `bash scripts/ai.sh verify-access --profile DeepSeek-V4.1-Flash --max-dispatches 1 --max-spend-usd 0.05` | Pass: exit `0`; one live dispatch; credential reference `deepseek` present without value exposure; returned model `deepseek-flash`; fingerprint `aeb56401ca74e127821c4f9126dcb669`; usage known (365 prompt, 9 completion tokens); reserved `$0.000946`, actual `$0.0001203`, unresolved `$0` |
| AC-006 / T004 | Same `verify-access` with `--profile DeepSeek-V4.1-Flash-SecondaryRef` (missing `...__DEEPSEEK_SECONDARY__APIKEY`) + offline selection cases | Pass: second credential source is independently selectable, resolves its own reference without serving-rule changes, and reaches the correct zero-dispatch blocked report when that distinct credential is absent |
| Regression / T005 | `bash scripts/backend.sh check` (169/169), `bash scripts/contract.sh check` (no drift), `python3 automation/context.py check M019` (14 sources green after reviewed lock refresh) | Pass |

Fixes during execution: narrowed the adapter settle guard so requested
cancellation retains its reservation instead of escaping unsettled;
rejected ambient-variable reads in the adapter (explicit credential only);
made runner budget parsing culture-invariant. Limitations: the live success is
momentary access evidence only; quality/cost/latency qualification stays with
M020+; Q-001/Q-007 remain open; DF-004 stays deferred with no route-table code
added.
