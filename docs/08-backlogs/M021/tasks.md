# M021 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: none.
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T001 — Persistence: operation/ledger entities + reviewed EF migration on the M006/M007 chain, revision counter; plan step 1; depends on none; done when model-drift/initialization checks pass on the production chain.
- [ ] T002 — Admission service: identity/validity, M005 validation with pre-match defaults, HMAC fingerprint, single-transaction claim + allowance test + day stamp with violation re-read; AC-005 + policy portions of AC-001–AC-004; depends on T001; done when policy unit tests pass with no HTTP/DB/dispatch.
- [ ] T003 — Endpoints + concurrency/persistence evidence: `POST /api/operations`, `GET /api/operations/{operationId}`, duplicate/conflict/expiry/allowance matrices, parallel same-user/multi-user file-backed cases, two-process restart proof, zero-dispatch counters; AC-001–AC-006; depends on T002; done when real-HTTP + file-backed cases pass.
- [ ] T004 — Contract/auth/privacy evidence: C# DTOs/metadata, OpenAPI regen + TS declarations with drift failure, verified/unverified/anonymous matrix, strict-JSON matrix, sentinel sweep; AC-007; depends on T003; done when `contract.sh check` and sweeps pass with no text/secret in evidence.
- [ ] T005 — Regressions and lock: `backend.sh check`, `contract.sh check`, `context.py check M021` green with revision/environment/UTC time recorded below; all ACs; depends on T004; done when every AC holds and the completion record is filled.

## Completion record
Pending execution. No evidence yet.
