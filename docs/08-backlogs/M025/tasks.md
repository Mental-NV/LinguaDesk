# M025 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: none.
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T001 — Extend `UsageSnapshot` DTO with the availability signal and add the `GET /api/usage` shape in `OperationsContract.cs`/`OperationsEndpoints.cs`; regenerate/review OpenAPI + TypeScript; AC-008; depends on none; done when `bash scripts/contract.sh check` passes with zero drift.
- [ ] T002 — Extend the `LedgerSnapshot` reader with consistent availability computation and wire it into submit/duplicate/status paths plus the new usage endpoint; AC-001; depends on T001; done when policy unit tests prove the availability matrix with no ledger/revision writes on reads.
- [ ] T003 — Implement `GET /api/usage` (verified-account policy, no-store, zero dispatch, 503-without-values on storage failure); AC-001/AC-007; depends on T002; done when real-HTTP/file-backed read cases pass with auth-matrix coverage.
- [ ] T004 — Add cross-period `OperationUsageTests` (snapshot fields, availability matrix, cross-midnight settle, duplicate/status period reuse, interrupted/failed across midnight); AC-001/AC-002/AC-005/AC-006; depends on T003; done when the new tests pass over file-backed SQLite with a fake server clock.
- [ ] T005 — Add ordering/month/restart `OperationUsageTests` (day/revision ordering, JSON exactness, cross-month carryover composition, two-process restart); AC-003/AC-004/AC-007; depends on T003; done when the new tests pass over file-backed SQLite.
- [ ] T006 — Run full regressions (`backend.sh check`, `contract.sh check`, sentinel sweep, `context.py check M025`) and record the completion record; all ACs; depends on T001–T005; done when every check is green with revision/environment/UTC time recorded.

## Completion record
Not started. No evidence yet.
