# M007 — Tasks and evidence

Inputs: [spec](spec.md), [plan](plan.md).

## Resume

Next: T001. Blockers: none.
Last check: planning-only; no production tests or implementation commands run. Changed scope: none.

## Ordered tasks

- [ ] T001 — Reconfirm the locked baseline, M006 completion, tool/package pins and restore/audit access; declare the two C# endpoint contracts/metadata, map only their under-construction routes, generate/review the M007 OpenAPI 3.1 and TypeScript shapes before handler adoption, and preserve zero-effect generation. AC-007/008; depends on none; done when the context check/preflight passes and reviewed generated artifacts contain exactly the selected shapes with no future auth route.
- [ ] T002 — Configure a dedicated named Identity email-confirmation provider/options type with a 24-hour lifetime while leaving default/reset tokens untouched, and refactor registration delivery to a structured destination/user-ID/base64url-code record through one shared coordinator; persist the 60-second UTC attempt marker in the existing Identity token store with injected time and the account gate, with no migration/package/fake-production path. AC-001/003/005/006; depends on T001; done when named-provider, reset-isolation, encoding, pending-model, cooldown-boundary and registration regression checks pass.
- [ ] T003 — Implement strict anonymous confirmation/resend parsing, coordinators and response mapping under `Features/Identity/Verification/`; preserve generic invalid/expired and non-enumerating results, no-store/405/media/query behavior, availability sanitization and no auth/redirect/language side effects. AC-001–006; depends on T002; done when the focused real HTTP/Identity suite passes all success, failure, restart, concurrency and privacy boundaries.
- [ ] T004 — Add/organize deterministic account test support and a guarded `AccountVerificationTests` suite using file-backed migrated SQLite, persistent keys, real Identity, fake time and capturing/failing senders; cover every AC-001–006 assertion without sleeps, mocked stores or secret-bearing reports. AC-001–006; depends on T002/T003; done when the focused test command passes and the aggregate script has a positive suite-count guard.
- [ ] T005 — Run contract generation/check/drift guard, full backend and smoke, frontend type/build/smoke and dependency audits; inspect generated paths, package graph, pending model, processes/files and logs for forbidden effects/secrets and preserve M001–M006 behavior. AC-007/008; depends on T001–T004; done when every exact plan command passes with reproducible sanitized evidence, or the failing gate remains recorded.
- [ ] T006 — Reconcile the diff and actual evidence against BI-007, FR-002, API-AC-003/012/014, V-004/V-009/V-015 and the locked sources; update only affected README/design/status/coverage rows and this backlog/tasks record, leaving M008–M014/M034, full FR-002, RG-005, Q-004, P-005/P-006 and release gates pending. AC-001–008; depends on T005; done when all selected ACs have evidence, local link/context audits pass and no unsupported completion claim remains.

## Scenario-to-task mapping

| Scenario | Tasks | Planned evidence |
| --- | --- | --- |
| AC-001 | T001–T004/T006 | Captured structured registration delivery submitted through real HTTP; durable confirmed row/current policy; no credential/language side effect |
| AC-002 | T003/T004/T006 | Generic invalid/expired matrix, bounded strict parsing and response/log/report token/account sentinels |
| AC-003 | T002–T004/T006 | Pinned option, same-file/key restart, deterministic expiry boundary and idempotent valid replay |
| AC-004 | T003/T004/T006 | Byte/header-equivalent known/unknown/confirmed resend results plus field-safe invalid email |
| AC-005 | T002–T004/T006 | Durable token-row cooldown, fake-time boundary, restart and barrier-controlled concurrency with adapter counts |
| AC-006 | T002–T004/T006 | Failed registration/resend delivery, bounded retry after cooldown and final successful confirmation |
| AC-007 | T001/T005/T006 | Reviewed generated YAML/types, exact path/status/header metadata, drift failure and no-effect generation |
| AC-008 | T001–T006 | Focused/aggregate/smoke/audit reports, dependency and scope inspection, and final acceptance reconciliation |

## Completion record

Pending implementation. Record the implementation revision/configuration, environment and UTC timestamp; exact commands/results/report paths; Identity/package settings; generated contract version/hash; focused scenario dispositions; failures/fixes; human/live limitations; and affected owner updates here. Do not paste logs or mark M012/M034/live-email/release evidence passed.
