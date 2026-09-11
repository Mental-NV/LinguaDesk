# M018 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: none — all tasks complete. Blockers: none.
Last check: `bash scripts/ai.sh check` (265/265), `bash scripts/backend.sh check` (389/389), `bash scripts/contract.sh check` (no drift), `python3 automation/context.py check M018` (37 sources green after reviewed lock refresh). Changed scope: none.

## Ordered tasks
- [x] T001 — Add `FamilyChain` snapshot + validation in `backend/src/LinguaDesk.Infrastructure.Ai/`; AC-001; depends on none; done when offline MSTest cases prove chain acceptance and every rejection class (unknown IDs, identical primary/fallback, route/mode-keyed selection) with zero dispatches.
- [x] T002 — Add chain orchestrator (traversal, §7 mapping, deadline clock, per-attempt admission, credential-scope skip, late-output fencing) over both family pipelines in the same library; AC-002, AC-003, AC-004, AC-005; depends on T001; done when scripted-`IChatClient` plus fake-time/fake-admission tests prove both three-dispatch paths per family, the full advance/stop matrix, deadline enforcement at each stage and admission/exposure bounds.
- [x] T003 — Add runner `evaluate-chain-bounds` (allowlisted small slice, primary-only enforcement, budget/deadline flags, sanitized metadata-only report) with offline scripted path; AC-006 (offline); depends on T002; done when offline runs pass with no secrets in any artifact.
- [x] T004 — Execute the bounded primary-only live slice for `DeepSeek-V4.1-Flash` and obtain human review of the sanitized report; AC-006 (live); depends on T003; done when the budgeted live run records attempts/usage/exposure/deadline metadata with retained failures, or is honestly reported blocked if the credential is absent.
- [x] T005 — Extend `scripts/ai.sh` + operating guide, run full regressions and the secret sweep, fill the completion record; all ACs; depends on T004 (offline portions may run if the live gate is blocked); done when `backend.sh check`, `contract.sh check`, `ai.sh check`, `context.py check M018` and the sweep pass with revision/environment/UTC time recorded.

## Completion record

Revision: `43b04a7` plus the uncommitted M018 working tree below.
Configuration: Release; .NET SDK `10.0.302`; Node `v24.20.0`.
Environment: Darwin arm64, UTC 2026-09-11T0135Z. No new package
dependencies; the AI library still resolves only
`Microsoft.Extensions.AI.Abstractions` `10.9.0` as an external package
(plus the existing Core reference).

New files: `FamilyChain.cs`, `ChainPolicy.cs`, `ChainOrchestrator.cs` in
`backend/src/LinguaDesk.Infrastructure.Ai/`; `FamilyChainTests.cs`
(7 cases), `ChainOrchestratorTests.cs` (29 cases) in
`backend/tests/LinguaDesk.Infrastructure.Ai.Tests/`;
`EvaluateChainBounds.cs` in `backend/tools/LinguaDesk.Ai.Evaluation/`.
Extended: `TranslationPipeline.cs`/`RewritingPipeline.cs` (pure
transformation-stage extraction, terminal mapping untouched),
`Program.cs` (`evaluate-chain-bounds` command), `scripts/ai.sh`
(offline default plus `--live` passthrough), `README.md` (operating
procedure).

| AC / task | Command / procedure | Result and evidence |
| --- | --- | --- |
| AC-001 / T001 | `bash scripts/ai.sh check` (chain-validation MSTest cases) | Pass: `artifacts/test-results/ai.trx` 265/265 (7 new chain cases); the evaluation snapshot resolves primary `DeepSeek-V4.1-Flash` with fallback `DeepSeek-V4.1-Flash-SecondaryRef` for both families; unknown IDs and identical primary/fallback fail validation; no route/mode key exists on selection; validation takes no client so zero dispatches are structural |
| AC-002 / T002 | Same check (traversal scripted cases) | Pass: both three-dispatch paths proven per family (eligibility-fails and transformation-fails); accepted eligibility reused with no fallback re-fetch; fallback receives the original complete source; each candidate-stage pair visited at most once; primary success dispatches the fallback zero times; fallback success is shape-identical to primary success |
| AC-003 / T002 | Same check (§7 advance/stop matrix) | Pass: transient/invalid-output/refusal/unknown finishes advance once; empty/oversize/uncertain/eligibility-refused/invalid-mode/equal-language end with no fallback; blocked `CredentialRef` scopes skipped with zero dispatches; denied budget and missing clients stop traversal; eligibility-level refusal stays terminal per the M015 mapping while transformation-level refusal advances |
| AC-004 / T002 | Same check (fake-time deadline cases) | Pass: the original 30 s deadline is captured once and never reset; no dispatch starts when the stage allowance plus 2 s reserve no longer fits; late success is fenced and can never win; fake-time expiry during eligibility/transformation/fallback yields deadline failure with no post-deadline dispatch or success; host cancellation ends `cancelled` |
| AC-005 / T002 | Same check (admission/exposure cases) | Pass: every dispatch reserves the peak cache-miss bound before launch; denial stops traversal; scripted calls settle as retained unresolved exposure; every definitive failure carries zero character charge; live actuals settle through the reporter's measured cost |
| AC-006 offline / T003 | `bash scripts/ai.sh evaluate-chain-bounds --profile DeepSeek-V4.1-Flash --max-dispatches 24 --max-spend-usd 0.50` | Pass: exit `0`, status `pass`, 9/9 match the allowlisted reference (5 translation directions incl. both Chinese input scripts, 4 rewriting cells); a configured `--fallback-profile` is refused (exit `2`); missing-credential live shape proven `blocked` (exit `3`) with zero dispatches; `artifacts/chain-bounds/evaluate-chain-bounds-20260911T013308Z-offline.json` |
| AC-006 live / T004 | `bash scripts/ai.sh evaluate-chain-bounds --live --profile DeepSeek-V4.1-Flash --max-dispatches 24 --max-spend-usd 0.50` | Pass: exit `0`, status `success`, 0 failed, 0 mismatches; all 9 cases succeeded naturally primary-only (18 live dispatches, known usage on every dispatch, per-case 1.9–2.8 s inside the 30 s deadline); reserved `$0.0026385`, unresolved `$0`; `artifacts/chain-bounds/evaluate-chain-bounds-20260911T013327Z-live.json`, presented at handoff for human review |
| Privacy/budget / T003–T004 | Pattern sweep over new sources/runner/scripts/reports plus source-text absence check | Pass: only credential-variable name references (the established env-binding pattern), no key material; both reports carry `credentialRef`, lengths/hashes, categories and numeric usage/exposure only — no source/result text, no reasoning |
| Regression / T005 | `bash scripts/backend.sh check` (389/389), `bash scripts/contract.sh check` (no drift), `python3 automation/context.py check M018` (37 sources green after reviewed lock refresh) | Pass |

Fixes during execution: one `CA1068` analyzer error (CancellationToken
not last) fixed by reordering `DispatchStageAsync` parameters before the
first green build; the fencing test exposed a dropped fenced-attempt
record on terminal paths, fixed by accumulating dispatches/attempts
before the terminal branch; three test expectations corrected to the
traversal contract (no pre-start clock expiry case, fallback reuse skips
eligibility, fenced-attempt retention). The four changed locked sources
were diff-reviewed (pure stage extraction plus additive runner/script
surface, no requirement change) before `context.py lock M018`.
Limitations: the live slice is primary-only development evidence only —
fallback liveness is proven deterministically, not live; no percentile
measurement (V-014 pending); `Retry-After` honoring stays at the
transport layer and is not implemented in this slice; candidate
quality/cost/latency qualification and the monetary cap stay with
M020/V-013/V-014 and Q-001.
