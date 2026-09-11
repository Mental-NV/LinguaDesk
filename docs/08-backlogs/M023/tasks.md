# M023 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: none.
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T001 — Persistence: add `MonetaryCostLedger` + `MonetaryAttemptReservation` entities, `LinguaDeskDbContext` sets/configuration, reviewed additive EF migration; AC-007 (durable rows); depends on none; done when model-drift/initialization checks pass.
- [ ] T002 — Options: add `MonetaryAdmissionOptions` + startup validator, wire local/test configuration with small explicit caps; spec §Constraints; depends on none; done when options-validation unit tests pass.
- [ ] T003 — Admission: implement ceiling check-and-reserve transaction, cost-month stamp, `BillingProfile` upper-bound computation with ceiling rounding, ineligible path, duplicate observation, plus file-backed SQLite tests for AC-001/002/003/006/007; depends on T001, T002; done when new `MonetaryAdmissionTests` pass.
- [ ] T004 — Reconciliation: implement settle-once/release transaction with evidence rules, plus tests for AC-004/005 and the settlement half of AC-006; depends on T003; done when reconciliation tests pass.
- [ ] T005 — Endpoints + contract/privacy: map denial/ineligible outcomes to 503/422 problem shapes with no monetary values, regenerate/review OpenAPI delta + TypeScript if shapes change, sentinel sweep; AC-008; depends on T003, T004; done when `contract.sh check` and sweeps pass.
- [ ] T006 — Regressions: full `backend.sh check`, `contract.sh check`, `context.py check M023`; depends on T005; done when all green and the completion record below is filled.

## Completion record
Not run. No evidence yet.
