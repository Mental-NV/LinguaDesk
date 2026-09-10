# M007 — Tasks and evidence

Inputs: [spec](spec.md), [plan](plan.md).

## Resume

Next: complete. Blockers: none.
Last check: all selected package checks passed on the working tree. Changed scope: M007 implementation, generated contract/types, focused/aggregate verification and affected design/status/coverage/operating owners only.

## Ordered tasks

- [x] T001 — Reconfirm the locked baseline, M006 completion, tool/package pins and restore/audit access; declare the two C# endpoint contracts/metadata, map only their under-construction routes, generate/review the M007 OpenAPI 3.1 and TypeScript shapes before handler adoption, and preserve zero-effect generation. AC-007/008; depends on none; done when the context check/preflight passes and reviewed generated artifacts contain exactly the selected shapes with no future auth route.
- [x] T002 — Configure a dedicated named Identity email-confirmation provider/options type with a 24-hour lifetime while leaving default/reset tokens untouched, and refactor registration delivery to a structured destination/user-ID/base64url-code record through one shared coordinator; persist the 60-second UTC attempt marker in the existing Identity token store with injected time and the account gate, with no migration/package/fake-production path. AC-001/003/005/006; depends on T001; done when named-provider, reset-isolation, encoding, pending-model, cooldown-boundary and registration regression checks pass.
- [x] T003 — Implement strict anonymous confirmation/resend parsing, coordinators and response mapping under `Features/Identity/Verification/`; preserve generic invalid/expired and non-enumerating results, no-store/405/media/query behavior, availability sanitization and no auth/redirect/language side effects. AC-001–006; depends on T002; done when the focused real HTTP/Identity suite passes all success, failure, restart, concurrency and privacy boundaries.
- [x] T004 — Add/organize deterministic account test support and a guarded `AccountVerificationTests` suite using file-backed migrated SQLite, persistent keys, real Identity, fake time and capturing/failing senders; cover every AC-001–006 assertion without sleeps, mocked stores or secret-bearing reports. AC-001–006; depends on T002/T003; done when the focused test command passes and the aggregate script has a positive suite-count guard.
- [x] T005 — Run contract generation/check/drift guard, full backend and smoke, frontend type/build/smoke and dependency audits; inspect generated paths, package graph, pending model, processes/files and logs for forbidden effects/secrets and preserve M001–M006 behavior. AC-007/008; depends on T001–T004; done when every exact plan command passes with reproducible sanitized evidence, or the failing gate remains recorded.
- [x] T006 — Reconcile the diff and actual evidence against BI-007, FR-002, API-AC-003/012/014, V-004/V-009/V-015 and the locked sources; update only affected README/design/status/coverage rows and this backlog/tasks record, leaving M008–M014/M034, full FR-002, RG-005, Q-004, P-005/P-006 and release gates pending. AC-001–008; depends on T005; done when all selected ACs have evidence, local link/context audits pass and no unsupported completion claim remains.

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

**Implementation status:** Complete on the working tree based on planning HEAD `c8a62d10be2eabbb79bc00edc8b709ec5ed0f3ce`; the milestone runner owns the eventual commit. **Runtime scenario status:** AC-001–008 passed. **Human action/blocker:** None for M007. **Environment/time:** Darwin 25.6.0 arm64, .NET SDK 10.0.302, Node 24.20.0 and npm 11.11.0; evidence finalized 2026-09-09T23:51:15Z.

Implementation uses ASP.NET Core Identity EF 10.0.10 with the real file-backed SQLite store and existing `LocalAccounts` migration. The named `LinguaDeskEmailConfirmation` Data Protection provider is selected only for email confirmation with a 24-hour lifespan; `TokenOptions.DefaultProvider` remains the password-reset provider. Registration/resend persist the fixed 60-second UTC delivery-attempt marker as the `LinguaDesk` / `VerificationDeliveryAttemptUtc` authentication token before calling the structured sender. No package or migration was added; `StorageMigrationTests` passed its pending-model assertion. Runtime configuration cannot select a fake or live sender and therefore performs no live email.

Verification evidence:

- `python3 automation/context.py check M007` passed before implementation review; planning HEAD, M006 completion, .NET/Node/npm pins and the Identity 10.0.10 package pin were confirmed. The affected selected references and owner excerpts were reviewed before their manifest hashes were reconciled.
- `dotnet test backend/tests/LinguaDesk.Api.Tests/LinguaDesk.Api.Tests.csproj --configuration Release --filter FullyQualifiedName~AccountVerificationTests` passed 11/11 focused cases. They use real HTTP, Identity, migrated file SQLite and persistent keys to prove captured registration-to-confirmation, current-state policy, generic corrupt/mismatched/unknown/expired/bounded input, strict JSON/UTF-8/media/query/method boundaries, restart/idempotence, dedicated-provider/reset isolation, fixed resend equivalence, fake-time 59/60-second durable cooldown, barrier-controlled concurrency, failed-delivery recovery and sanitized post-start storage loss. The aggregate script now requires all 11 cases and at least 78 API tests.
- `bash scripts/backend.sh check` passed 98/98: 78 API/storage/Identity/readiness, 10 Core and 10 independent AI cases. TRX evidence is under `artifacts/test-results/`. `bash scripts/backend.sh smoke` migrated an owned database through `InitialStorage` and `LocalAccounts` and passed process liveness plus database/key readiness on an OS-assigned loopback port.
- `bash scripts/contract.sh generate` regenerated the actual M007 metadata; `bash scripts/contract.sh check` passed before and after the drift probe. A temporary title drift in the canonical YAML made the check exit 1 with `OpenAPI contract drift detected`; the exact temporary backup was restored and the check passed. The OpenAPI 3.1 artifact version is `0.1.0-m007` and exposes exactly capabilities, registration, confirmation and resend. SHA-256: `f6734e262cbd213122dad296ba3c251676cfa02722ecaef87a58966d9cf9e001` for `docs/05-openapi.yaml`; `032a8625ba9914f7f522c1824e55c3b674e8eeb5585418aab95fca2223a9a9e5` for the TypeScript declaration.
- `bash scripts/frontend.sh check` passed typecheck, lint, production build and 36/36 unit/component cases. `bash scripts/frontend.sh smoke` republished and passed 6/6 Chromium cases while retaining API/health/static routing. `dotnet list backend/LinguaDesk.slnx package --vulnerable --include-transitive` reported no vulnerable packages; `npm --prefix frontend audit --audit-level=moderate` reported 0 vulnerabilities.
- The initial sandboxed focused .NET and contract-generation attempts could not create MSBuild IPC or reach NuGet advisory metadata. Authorized reruns outside that restriction passed; these were environment failures, not product failures. `git diff --check`, `bash -n scripts/backend.sh scripts/contract.sh scripts/frontend.sh`, `python3 automation/context.py check M007` and `python3 automation/context.py audit` passed. Test artifacts contained none of the selected email/password/token sentinels. Full non-generated diff review plus generated semantic/path review found no out-of-scope route, migration, package, credential, text persistence or accidental artifact; no owned smoke/API process remained.

**Acceptance disposition:** AC-001 passed by captured structured delivery, real confirmation and fresh policy evaluation; AC-002 by the generic invalid/expired and strict secret-safe boundary matrix; AC-003 by named-provider options, reset isolation, restart, expiry and valid replay; AC-004 by byte/header-equivalent known/confirmed/unknown resend; AC-005 by persistent marker, fake-time boundary and controlled concurrency; AC-006 by failing/recovering sender evidence; AC-007 by reviewed generation, exact paths/shapes, no-effect boundary and deliberate drift failure; AC-008 by the real-store aggregate, pending-model, audits and published regressions.

M007 does not complete full FR-002 or RG-005. M008–M010 retain cookie/bearer/password recovery, M012 retains browser verification and human UX evidence, and M034 retains public-origin/template/provider/live mailbox delivery/click evidence. Account deletion/backup policy (Q-004), P-005/P-006, language work and every release gate remain pending.
