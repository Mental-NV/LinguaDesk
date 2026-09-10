# M010 — Tasks and evidence

Inputs: [spec](spec.md), [plan](plan.md).

## Resume

Next: T001. Blockers: none.
Last check: not run. Changed scope: none.

## Ordered tasks

- [ ] T001 — Reconfirm the locked baseline, selected status, M009 completion, tool/package pins and restore access; add actual C# DTO/endpoint metadata for only forgot-password request and reset completion, generate/review OpenAPI 3.1 and TypeScript before handler adoption, and preserve zero-effect generation. AC-001/006; depends on none; done when preflight passes and reviewed artifacts contain exactly the selected shapes/security requirements with no browser/live-email route.
- [ ] T002 — Pin the 60-minute default password-reset provider lifespan and add the reset-delivery seam plus dedicated durable 60-second cooldown marker behind the existing account gate with no package or migration. AC-002/003; depends on T001; done when configuration/cooldown/expiry-marker checks pass and the M007 confirmation provider is untouched.
- [ ] T003 — Implement strict forgot-password and reset completion under `Features/Identity/Recovery/`; share strict JSON/media/error conventions; enforce non-enumeration, exact-password policy, generic reset categories, availability and method/media/query boundaries with no credential/cookie/sign-in on success. AC-001–005; depends on T001/T002; done when focused real HTTP behavior passes.
- [ ] T004 — Add guarded `AccountRecoveryTests` using migrated file SQLite, persistent keys, real Identity reset tokens/stamps, HTTPS and fake time; cover every AC-001–005 assertion without test-auth handlers, mocked stores, manual tokens, sleeps or secret-bearing reports. AC-001–005; depends on T002/T003; done when the focused suite passes and the aggregate script has a positive suite-count guard covering it.
- [ ] T005 — Run contract generation/check/drift proof, aggregate backend/smoke, frontend type/build/smoke and dependency audits; inspect generated security/path semantics, token handling, pending model, processes/files and logs for forbidden effects/secrets while preserving M001–M009 behavior. AC-006/007; depends on T001–T004; done when every exact plan command passes with sanitized evidence or the failing gate remains recorded.
- [ ] T006 — Reconcile the diff and actual evidence against BI-010, FR-002, API-AC-003/012/014 and V-004/V-009/V-015; update only affected owner/status/coverage/operating rows and this backlog/tasks record, leaving M014, M034, full product requirements, Q-004, P-005/P-006 and release gates pending. AC-001–007; depends on T005; done when every selected AC has reproducible evidence, local link/context audits pass and no unsupported completion claim remains.

## Completion record

Not started. No production code or tests have been written by planning; the runner owns commits.
