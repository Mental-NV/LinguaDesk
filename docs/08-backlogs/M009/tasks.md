# M009 — Tasks and evidence

Inputs: [spec](spec.md), [plan](plan.md).

## Resume

Next: T001. Blockers: none.
Last check: not run. Changed scope: none.

## Ordered tasks

- [ ] T001 — Reconfirm the locked baseline, selected status, M008 completion, tool/package pins and restore access; add actual C# DTO/endpoint metadata for only bearer sign-in, bearer refresh and current-account read, generate/review OpenAPI 3.1 and TypeScript before handler adoption, and preserve zero-effect generation. AC-001/006; depends on none; done when preflight passes and reviewed artifacts contain exactly the selected shapes/security requirements with no reset/language route.
- [ ] T002 — Register the real Identity bearer scheme with pinned 15-minute/7-day lifetimes and injected-time expiry; implement per-request current-account/security-stamp validation for bearer access and refresh, keep bearer-header-wins selection with no cookie fallback, and reuse the `VerifiedAccount` policy without a package or migration. AC-001–004; depends on T001; done when configuration/expiry/stamp/deletion/precedence checks pass.
- [ ] T003 — Implement strict bearer sign-in, refresh and `me` under `Features/Identity/Bearer/`; share M008 strict JSON/media/error conventions; enforce non-enumeration, availability, method/media/query and secret-safe Problem Details boundaries with no cookies and no antiforgery on bearer calls. AC-001–005; depends on T001/T002; done when focused real HTTP behavior passes.
- [ ] T004 — Add/organize guarded `AccountBearerTests` using migrated file SQLite, persistent keys, real Identity bearer protection/password/security stamps, HTTPS and fake time; cover every AC-001–005 assertion without test-auth handlers, mocked stores, manual tokens, sleeps or secret-bearing reports. AC-001–005; depends on T002/T003; done when the focused suite passes and the aggregate script has a positive suite-count guard.
- [ ] T005 — Run contract generation/check/drift proof, aggregate backend/smoke, frontend type/build/smoke and dependency audits; inspect generated security/path semantics, token handling, pending model, processes/files and logs for forbidden effects/secrets while preserving M001–M008 behavior. AC-006/007; depends on T001–T004; done when every exact plan command passes with sanitized evidence or the failing gate remains recorded.
- [ ] T006 — Reconcile the diff and actual evidence against BI-009, FR-001/FR-036, API-AC-002/003/012/014 and V-004/V-009/V-015; update only affected owner/status/coverage/operating rows and this backlog/tasks record, leaving M010/M013/M026+, full product requirements, Q-004, P-005/P-006 and release gates pending. AC-001–007; depends on T005; done when every selected AC has reproducible evidence, local link/context audits pass and no unsupported completion claim remains.

## Completion record

Not started. No evidence yet.
