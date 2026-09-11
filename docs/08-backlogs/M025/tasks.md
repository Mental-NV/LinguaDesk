# M025 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: none — all tasks complete. Blockers: none.
Last check: `bash scripts/backend.sh check` (456 total, 456 passed, 0 failed), `bash scripts/contract.sh check` (zero drift), `python3 automation/context.py check M025` green.
Changed scope: none.

## Ordered tasks
- [x] T001 — Extend `UsageSnapshot` DTO with the availability signal and add the `GET /api/usage` shape in `OperationsContract.cs`/`OperationsEndpoints.cs`; regenerate/review OpenAPI + TypeScript; AC-008; depends on none; done when `bash scripts/contract.sh check` passes with zero drift.
- [x] T002 — Extend the `LedgerSnapshot` reader with consistent availability computation and wire it into submit/duplicate/status paths plus the new usage endpoint; AC-001; depends on T001; done when policy unit tests prove the availability matrix with no ledger/revision writes on reads.
- [x] T003 — Implement `GET /api/usage` (verified-account policy, no-store, zero dispatch, 503-without-values on storage failure); AC-001/AC-007; depends on T002; done when real-HTTP/file-backed read cases pass with auth-matrix coverage.
- [x] T004 — Add cross-period `OperationUsageTests` (snapshot fields, availability matrix, cross-midnight settle, duplicate/status period reuse, interrupted/failed across midnight); AC-001/AC-002/AC-005/AC-006; depends on T003; done when the new tests pass over file-backed SQLite with a fake server clock.
- [x] T005 — Add ordering/month/restart `OperationUsageTests` (day/revision ordering, JSON exactness, cross-month carryover composition, two-process restart); AC-003/AC-004/AC-007; depends on T003; done when the new tests pass over file-backed SQLite.
- [x] T006 — Run full regressions (`backend.sh check`, `contract.sh check`, sentinel sweep, `context.py check M025`) and record the completion record; all ACs; depends on T001–T005; done when every check is green with revision/environment/UTC time recorded.

## Completion record
Revision: `0458098eeb26afecaaf1f030c92d758828664324` plus the uncommitted M025 change set (no commit; the runner owns commits). UTC: 2026-09-11T04:06:36Z at final verification. Environment: .NET SDK 10.0.302, Node v24.20.0, npm 11.11.0.

Evidence (all over file-backed SQLite with the fake server clock unless noted):
- AC-001: `OperationUsageTests.UsageReadAndOperationResponsesCarryAuthoritativeSnapshot` (submit/usage/status carry day, reset, consumed/reserved/allowance/available, revision, `available`; no global counts, monetary amounts or source text) and `AvailabilityMatrixReportsEachExhaustionStateWithDenialPairing` (`user-exhausted`/`service-exhausted`/`monetary-suspended` each paired with the matching 429/denial, `user-exhausted` wins when both allowances are exhausted) and `StorageFailureReportsUnavailableWithoutUsageValues` (dropped ledger table returns 503 `availability` with no usage values).
- AC-002: `CrossMidnightSettlementChargesAdmissionDayWithFreshSnapshot` (admit 2026-09-11, settle after midnight: charge on the admission day, new-day ledgers absent, status returns the admission day with a fresh new-day snapshot and revision 2).
- AC-003: `CrossMonthCarryoverSuspendsUntilAuthoritativeSettlement` (1000-unit September exposure denies an October 500-unit admission with `monetary-suspended`; authoritative settlement attributes 400 to September exactly once with idempotent repeat; October admission then succeeds; known+unresolved totals 900 with no double counting; character snapshot rolls to the current day).
- AC-004: `SnapshotOrderingIsDayFirstRevisionSecond` (older-day and lower same-day revisions rejected, newer-day-smaller-consumed accepted, revision strictly increasing across admission/settlement/recovery, exact JSON integer round-trip, cross-account comparison refused and account-scoped 404).
- AC-005: `PostMidnightDuplicateAndStatusReuseOriginalPeriod` (post-midnight resubmission returns 202 with the stored admission day, a fresh new-day snapshot, unchanged revision/ledgers; changed payload 409).
- AC-006: `InterruptedAndFailedAcrossMidnightReportZeroCharge` (interrupted/failed across midnight report zero charge with the original day and a fresh snapshot; unknown stays 404 asserting nothing; expired stays 410).
- AC-007: `RestartPreservesLedgersRevisionAndAvailability` (same-file restart preserves both days' ledgers, revision 3 and availability; usage/status reads byte-identical and side-effect-free with `outputAvailable: false`).
- AC-008: `UsageMetadataHoldsNoSourceTextOrSecrets` (sentinels absent from submit/status/usage bodies and every non-identity table column); `bash scripts/contract.sh check` passes with zero drift on the reviewed delta (`UsageAvailability` five-value enum, `GET /api/usage` with Bearer+cookie security and required no-store on every response, regenerated TypeScript declarations); `grep` sweep confirms no monetary minor-unit values, global counts or secrets in Problem payloads, log templates or new code paths.

Checks: `bash scripts/backend.sh check` — 456 total (API/storage 175, core 10, independent AI 271), 456 passed, 0 failed; `bash scripts/contract.sh check` — zero drift; `python3 automation/context.py check M025` — selected inputs and spec/plan unchanged. No human/live evidence is required by this slice; none was used.
