# M022 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: none.
Last check: not run. Changed scope: none beyond the selected slice.

## Ordered tasks
- [ ] T001 — Persistence: terminal outcome fields on `OperationSubmission` + reviewed additive EF migration on the M021 chain, revision reuse; plan step 1; depends on none; done when model-drift/initialization checks pass on the production chain.
- [ ] T002 — Settlement service: internal `OperationSettlementService` with conditional pending→succeeded/failed transitions, admission-day ledger moves, revision advance, fencing + 409 recheck; AC-001–AC-004 policy portions; depends on T001; done when policy unit tests pass with no HTTP/DB dispatch.
- [ ] T003 — Endpoints + settlement evidence: extended `GET /api/operations/{operationId}` terminal reads, duplicate/late/concurrency/midnight/restart matrices over real HTTP + file-backed SQLite, zero-dispatch counters; AC-001–AC-007; depends on T002; done when real-HTTP + file-backed cases pass.
- [ ] T004 — Contract/auth/privacy evidence: extended C# status DTOs/metadata, OpenAPI regen + TS declarations with drift failure, auth matrix on the extended read, sentinel sweep; AC-008; depends on T003; done when `contract.sh check` and sweeps pass with no text/secret in evidence.
- [ ] T005 — Regressions and lock: `backend.sh check`, `contract.sh check`, `context.py check M022` green with revision/environment/UTC time recorded below; all ACs; depends on T004; done when every AC holds and the completion record is filled.

## Completion record
Not started. No evidence yet.
