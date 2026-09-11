# M024 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: none.
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T001 — State + persistence: add `OperationStates.Interrupted`, verify model drift (reviewed additive migration only if required); plan step 1; depends on none; done when drift checks pass with interrupted round-tripping.
- [ ] T002 — Recovery service: implement `OperationRecoveryService.InterruptAsync` + startup `ReconcileOrphansAsync` (deadline-passed only) with conditional claiming, reservation release, revision advance, race-reread; plan step 2; depends on T001; done when policy unit tests pass.
- [ ] T003 — Endpoints + contract/privacy: extend `OperationStatus`, map interrupted/unknown/expired reads, regenerate/review OpenAPI delta + TypeScript, auth matrix, sentinel sweep; AC-008; depends on T001, T002; done when `contract.sh check` and sweeps pass.
- [ ] T004 — Recovery/restart/disconnect tests: new `OperationRecoveryTests` for AC-001/002/005/007 over file-backed SQLite with zero-dispatch counters; depends on T002, T003; done when the new tests pass.
- [ ] T005 — Fencing/composition tests: AC-003/004/006 (late-outcome fencing, 409, idempotency, abort-then-success, exposure retention with original-month attribution); depends on T004; done when the new tests pass.
- [ ] T006 — Regressions: full `backend.sh check`, `contract.sh check`, `context.py check M024`; depends on T005; done when all green and the completion record below is filled.

## Completion record
Pending execution. No evidence yet.
