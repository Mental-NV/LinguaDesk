# M026 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: review and commit the recovered implementation; all implementation tasks and runner regression gates pass. Blockers: none.
Last check: `bash scripts/verify-milestone.sh M026` passed all gates at 2026-09-11T14:55Z (473 backend tests, 271 focused AI tests, 139 frontend unit tests, 32 published browser tests, backend smoke, zero contract drift, context/audit and diff checks). Automation regression suite: 37 passed.
Changed scope: none (spec/plan untouched; wire 201/504 and category names are the plan's "wire-fixed once in C#/OpenAPI" latitude).

## Ordered tasks
- [x] T001 — Reuse review of M009/M016/M018/M022–M025 entry points (verified gate, translation pipeline, chain traversal, admission/settlement/recovery/snapshot readers); AC-001–008 prerequisites; depends on none; done when touched files/behaviors are listed and regressions identified.
- [x] T002 — Translation submit/status contract DTOs in `OperationsContract.cs` + OpenAPI generation/review with zero drift; AC-008; depends on T001; done when `scripts/contract.sh` passes and the semantic diff is reviewed.
- [x] T003 — Deterministic fake translation provider + fault injection (test composition only, production-inaccessible); AC-001/003/005; depends on T001; done when scripted classification/translation/fault paths are asserted through the real HTTP host pipeline.
- [x] T004 — Translation coordinator + endpoint wiring (admission → chain → validation → settle-once; identity-state table; deadline fencing; per-attempt monetary admission; fresh snapshot on every response); AC-001–007; depends on T002, T003; done when file-backed SQLite/HTTP suite passes via `scripts/backend.sh`.
- [x] T005 — Privacy/drift closeout (no text/secrets/counts in problems/logs/traces/TEXT columns; `no-store`; contract drift clean) + `python3 automation/context.py check M026`; AC-008; depends on T004; done when checks pass and evidence is recorded below.

## Completion record
Revision: `d5f56864226941b4a4b58cf5a6cd8c70aaf0635d` plus the uncommitted M026 change set (no commit; the runner owns commits). UTC: 2026-09-11T13:12:50Z at final verification. Environment: .NET SDK 10.0.302, Node v24.20.0, npm 11.11.0.

Evidence (all over file-backed SQLite with the fake server clock and the deterministic fake translation provider unless noted):
- AC-001: `OperationTranslationTests.TranslationSubmitExecutesSynchronouslyWithChargeAndSnapshot` (verified-account submit returns 201 with complete validated text, charged scalar count, admission day, 30 s deadline and a fresh current-day snapshot with `no-store`; exactly 2 provider dispatches; 2 released monetary reservations with zero known spend and zero unresolved exposure; status read reports succeeded/output-unavailable with the same charge and no new dispatch; usage read consistent) and `TranslationWithoutConfiguredProviderRemainsPending` (unconfigured production composition keeps the 202 pending reservation path).
- AC-002: `TranslationValidationMatrixRejectsWithoutDispatchOrCharge` (malformed, unknown/duplicate fields, unsupported/missing target, equal selected source/target, empty, whitespace-only and oversized sources rejected with 400/422 input categories, zero dispatches, no admitted operation, unchanged ledgers).
- AC-003: `EligibilityRejectionsEndWithoutTransformationOrCharge` (uncertain/unsupported/mixed/source-mismatch end as 422 eligibility rejections with no transformation dispatch and no character charge; failed status reads report zero charge) and `DetectedSameLanguageRejectsWithoutCharacterCharge` (auto-detected equal source/target ends as 422 sameLanguage with one dispatch).
- AC-004: `DuplicateSamePayloadReplaysMetadataWithoutNewChargeOrDispatch` (same-identity/same-payload replay returns succeeded/output-unavailable metadata with the original charge day, fresh usage, no new dispatch or charge; same-identity/different-payload conflicts with 409; one stored submission and one full-source charge).
- AC-005: `TransientProviderFailuresExhaustToProcessingFailure`, `InvalidProviderOutputExhaustsToProcessingFailure` and `ProviderRefusalsExhaustToProcessingFailure` (bounded chain exhausts to terminal 503 processing failure with zero charge; dispatch bounds 2/3/2 observed); `FallbackReceivesOriginalSourceAndSettings` (fallback receives the original complete source with source/target languages); `DeadlineExpiryFencesExecutionWithZeroCharge` (fake-clock advance past the stored deadline ends as terminal 504 with zero charge and no transformation dispatch); `DeniedMonetaryAdmissionStopsDispatchWithSuspension` (over-cap denial stops dispatch with 503 monetary suspension, no reservation writes, zero charge) and `MissingMonetaryConfigurationSuspendsDispatch` (unconfigured monetary admission suspends dispatch with zero provider calls).
- AC-006: `SettledTranslationSurvivesRestartWithDispatchFreeReads` (settled translation success survives same-file restart with output unavailable and the original charge; unknown stays 404 asserting nothing; reads dispatch nothing); pre-commit crash fencing reuses the M024 orphan-scan path (regression suites green, no new pending-execution state introduced).
- AC-007: `ExhaustedUserAllowanceDeniesWithoutDispatch` (exhausted user allowance denied with 429 userAllowance distinction and UTC-midnight reset guidance, zero dispatch on denial, snapshot reports user-exhausted with the M025 rules unchanged).
- AC-008: `TranslationFailureCarriesNoSourceTextOrSecrets` and `TranslationSuccessOmitsSourceTextOutsideTranslatedText` (sentinels absent from problem bodies and every TEXT-bearing store column; provider internals never leak); `bash scripts/contract.sh check` passes with zero drift on the reviewed delta (`TranslationSuccessResponse` with 201 success and 504 deadline responses, `processingFailure`/`deadlineExceeded` categories fixed in C#, shared-enum description noise reviewed, regenerated TypeScript declarations, required no-store on every new response); log templates carry counts/day/dispatches only.

Checks: `bash scripts/backend.sh check` — 473 total (API/storage 192 including 17 new translation tests, core 10, independent AI 271), 473 passed, 0 failed; `bash scripts/contract.sh check` — zero drift; `python3 automation/context.py check M026` — selected inputs and spec/plan unchanged after reviewed re-lock of the two plan-authorized source edits; frontend `npm run typecheck` — clean against the regenerated declarations. No human/live evidence is required by this slice; none was used.

Environmental repair (pre-existing, proven on the untouched base at 2026-09-11T13:00Z): the operation HTTP suites hard-freeze the server clock at 2026-09-11T08:30:00Z while most tests minted real-time UUIDv7 identities, so every submit fails with 400 future-skew once real UTC passes 08:35Z. Fresh test identities now use `OperationIdentity.CreateForTime` against the harness clock (the convention M025 already used), with deliberate skew/expiry/malformed cases untouched; assertions unchanged. This repair is what lets the package and regression gates run at any hour.

## Runner verification recovery — 2026-09-11

The original runner stopped after contract verification because `scripts/ai.sh
check` still unconditionally prohibited the API from referencing the M004 AI
library. M026 selects precisely that consuming integration. Removed the expired
consumer prohibition while retaining the library's host/storage/middleware
dependency restrictions. No product scope or selected spec/plan changed.

Recovery verification at 2026-09-11T14:55:54Z used revision
`d5f56864226941b4a4b58cf5a6cd8c70aaf0635d` plus the uncommitted implementation
and automation repair, with the same SDK/Node/npm versions recorded above.
`bash scripts/verify-milestone.sh M026` passed the complete runner gate,
including all tests and smoke checks listed in Resume. Log:
`artifacts/m026-recovery-verification.log`. The initial restricted-sandbox
attempt stalled in restore and was stopped; the successful run used registry
access and local loopback sockets. No live provider calls were required.

`python3 -m unittest discover -s automation/tests -v` passed 37 tests, including
isolated-repository repair success, exhausted repairs, BLOCKED handling and
stale implementation context. Log: `artifacts/automation-recovery-tests.log`.
The runner now shares its regression command with the implementation prompt,
retains diagnostics, and allows three bounded verification repairs followed by
independent full rechecks before commit. No commit was created during recovery.
