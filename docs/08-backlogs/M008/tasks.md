# M008 — Tasks and evidence

Inputs: [spec](spec.md), [plan](plan.md).

## Resume

Next: T001. Blockers: none.
Last check: planning artifact/context checks only; implementation verification has not run. Changed scope: none.

## Ordered tasks

- [ ] T001 — Reconfirm the locked baseline, selected status, M007 completion, tool/package pins and restore access; add actual C# DTO/endpoint metadata for only antiforgery bootstrap, cookie sign-in, session read and sign-out, generate/review OpenAPI 3.1 and TypeScript before handler adoption, and preserve zero-effect generation. AC-001/007; depends on none; done when preflight passes and reviewed artifacts contain exactly the selected shapes/security requirements with no bearer/reset/language route.
- [ ] T002 — Register the real Identity sign-in/cookie services and explicit `__Host-LinguaDesk.Session` policy; implement per-request current-account/security-stamp validation, eight-hour fake-time expiry, no sliding/persistence/redirect and bearer-header no-fallback behavior without a package or migration. AC-002–004; depends on T001; done when configuration/ticket/expiry/stamp/deletion/precedence checks pass.
- [ ] T003 — Implement strict bootstrap, sign-in, session and idempotent sign-out under `Features/Identity/Session/`; configure the named antiforgery cookie/header and middleware order; preserve no-store, non-enumeration, availability, method/media/query and secret-safe Problem Details boundaries. AC-001–006; depends on T001/T002; done when focused real HTTP behavior passes.
- [ ] T004 — Add/organize guarded `AccountSessionTests` using migrated file SQLite, persistent keys, real Identity cookies/password/security stamps, CookieContainer, HTTPS and fake time; cover every AC-001–006 assertion without test-auth handlers, mocked stores, manual cookies, sleeps or secret-bearing reports. AC-001–006; depends on T002/T003; done when the focused suite passes and the aggregate script has a positive suite-count guard.
- [ ] T005 — Run contract generation/check/drift proof, aggregate backend/smoke, frontend type/build/smoke and dependency audits; inspect generated security/path semantics, cookie attributes, pending model, processes/files and logs for forbidden effects/secrets while preserving M001–M007 behavior. AC-007/008; depends on T001–T004; done when every exact plan command passes with sanitized evidence or the failing gate remains recorded.
- [ ] T006 — Reconcile the diff and actual evidence against BI-008, FR-001/002, API-AC-002/003/012/014 and V-004/V-009/V-015; update only affected owner/status/coverage/operating rows and this backlog/tasks record, leaving M009/M010/M013/M034, full product requirements, Q-004, P-005/P-006 and release gates pending. AC-001–008; depends on T005; done when every selected AC has reproducible evidence, local link/context audits pass and no unsupported completion claim remains.

## Completion record

Pending. Record each AC/task with planning baseline and implementation revision, configuration, exact command/procedure, environment/tool versions, UTC timestamp, result/evidence link, failures/fixes and limitations. Do not paste logs or include email/password/cookie/antiforgery/security-stamp values.
