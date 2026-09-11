# M026 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: none.
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T001 — Reuse review of M009/M016/M018/M022–M025 entry points (Bearer + verified gate, translation pipeline, chain traversal, admission/settlement/recovery/snapshot readers); AC-001–008 prerequisites; depends on none; done when touched files/behaviors are listed and regressions identified.
- [ ] T002 — Translation submit/status contract DTOs in `OperationsContract.cs` + OpenAPI generation/review with zero drift; AC-008; depends on T001; done when `scripts/contract.sh` passes and the semantic diff is reviewed.
- [ ] T003 — Deterministic fake translation provider + fault injection (test composition only, production-inaccessible); AC-001/003/005; depends on T001; done when scripted classification/translation/fault paths are asserted offline.
- [ ] T004 — Translation coordinator + endpoint wiring (admission → chain → validation → settle-once; identity-state table; deadline fencing; per-attempt monetary admission; fresh snapshot on every response); AC-001–007; depends on T002, T003; done when file-backed SQLite/HTTP suite passes via `scripts/backend.sh`.
- [ ] T005 — Privacy/drift closeout (no text/secrets/counts in problems/logs/traces/TEXT columns; `no-store`; contract drift clean) + `python3 automation/context.py check M026`; AC-008; depends on T004; done when checks pass and evidence is recorded below.

## Completion record
Not started. No evidence yet.
