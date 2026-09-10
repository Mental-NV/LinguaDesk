# M010 — Tasks and evidence

Inputs: [spec](spec.md), [plan](plan.md).

## Resume

Next: T004/T005 — run the focused `AccountRecoveryTests` suite and every exact
plan command in an environment with loopback sockets and NuGet/npm registry
access, then close T006. Blockers: this session's sandbox denies all socket
binds (IPv4/IPv6 loopback probed `EPERM`), has no registry network, and hangs
`WebApplicationBuilder` startup; escalation is unavailable (approval prompts
disabled). `dotnet test`/smokes and the network audit gates cannot execute
here. Last check: contract generate/check/drift proof, frontend check, and
socket-free substitute execution passed (see record); scripted backend
check/smoke, published smoke, and both dependency audits remain unrun.
Changed scope: `Features/Identity/Recovery/`, Identity registration/lifespan
pin, `Program.cs` mapping, `OpenApiRegistration.cs` m010, regenerated
`docs/05-openapi.yaml`/TypeScript, `AccountRecoveryTests.cs` (14 tests),
factory reset-sender/lifespan seams, `scripts/backend.sh` suite guard,
`scripts/openapi-to-yaml.mjs` recovery validation, README contract revision,
`docs/api/accounts.md` §3.3 M010 selection.

## Ordered tasks

- [x] T001 — Reconfirm the locked baseline, selected status, M009 completion, tool/package pins and restore access; add actual C# DTO/endpoint metadata for only forgot-password request and reset completion, generate/review OpenAPI 3.1 and TypeScript before handler adoption, and preserve zero-effect generation. AC-001/006; depends on none; done when preflight passes and reviewed artifacts contain exactly the selected shapes/security requirements with no browser/live-email route.
- [ ] T002 — Pin the 60-minute default password-reset provider lifespan and add the reset-delivery seam plus dedicated durable 60-second cooldown marker behind the existing account gate with no package or migration. AC-002/003; depends on T001; done when configuration/cooldown/expiry-marker checks pass and the M007 confirmation provider is untouched. Implementation complete; substitute execution green (S5/S11/S13 below); focused-harness confirmation pending (blocker above).
- [ ] T003 — Implement strict forgot-password and reset completion under `Features/Identity/Recovery/`; share strict JSON/media/error conventions; enforce non-enumeration, exact-password policy, generic reset categories, availability and method/media/query boundaries with no credential/cookie/sign-in on success. AC-001–005; depends on T001/T002; done when focused real HTTP behavior passes. Implementation complete; substitute execution green (S1–S10/S14 below); focused-harness confirmation pending.
- [ ] T004 — Add guarded `AccountRecoveryTests` using migrated file SQLite, persistent keys, real Identity reset tokens/stamps, HTTPS and fake time; cover every AC-001–005 assertion without test-auth handlers, mocked stores, manual tokens, sleeps or secret-bearing reports. AC-001–005; depends on T002/T003; done when the focused suite passes and the aggregate script has a positive suite-count guard covering it. Suite written (14 tests) and compiles with zero warnings/errors; guard added to `scripts/backend.sh`; harness run pending (blocker above).
- [ ] T005 — Run contract generation/check/drift proof, aggregate backend/smoke, frontend type/build/smoke and dependency audits; inspect generated security/path semantics, token handling, pending model, processes/files and logs for forbidden effects/secrets while preserving M001–M009 behavior. AC-006/007; depends on T001–T004; done when every exact plan command passes with sanitized evidence or the failing gate remains recorded. Contract + frontend gates passed; backend/smoke/audit gates pending (blocker above).
- [ ] T006 — Reconcile the diff and actual evidence against BI-010, FR-002, API-AC-003/012/014 and V-004/V-009/V-015; update only affected owner/status/coverage/operating rows and this backlog/tasks record, leaving M014, M034, full product requirements, Q-004, P-005/P-006 and release gates pending. AC-001–007; depends on T005; done when every selected AC has reproducible evidence, local link/context audits pass and no unsupported completion claim remains. Owner updates applied; final gate confirmation pending.

## Completion record

Implementation completed 2026-09-10T05:33:53Z on Darwin arm64, .NET SDK
10.0.302, Node v24.20.0 and npm 11.11.0. Planning baseline: `0f204e9`
(M010 planned tree); implementation working tree remains uncommitted for
the milestone runner. `python3 automation/context.py check M010` reports the
40 selected inputs unchanged.

| Task / AC | Command or procedure | Result / evidence |
| --- | --- | --- |
| T001 / AC-001, AC-006 | `python3 automation/context.py check M010`; Git/status and tool-pin inspection; `bash scripts/contract.sh generate`; semantic diff of `docs/05-openapi.yaml` and generated TypeScript | Lock selected inputs unchanged (40 sources); `dotnet --version` 10.0.302, `node --version` v24.20.0, `npm --version` 11.11.0. Two new anonymous operations `requestLocalAccountPasswordReset`/`resetLocalAccountPassword` with `202`/`200`-or-`400`/`405`/`415`/`503` no-store shapes, `ForgotPasswordRequest`/`ResetPasswordRequest`/`ForgotPasswordAccepted`/`ResetPasswordAccepted`/`RecoveryProblemDetails` schemas, artifact revision `0.1.0-m010`; no security requirement on either operation; no reset-adjacent browser/live-email/language route. Generation had no database/key/listener effect. |
| T002–T003 / AC-001–005 | Socket-free SUT driver (`/tmp/m010-driver/driver1.cs`, `/tmp/m010-driver/driver2.cs`; throwaway, kept outside the repo): legacy `WebHostBuilder` + in-process `TestServer` hosting the real `LinguaDesk.Api` pipeline (real `UserManager`, default reset provider, migrated file SQLite, persistent Data Protection keys, capturing/failing/blocking/recovering reset senders, adjustable `TimeProvider`), 16 scenarios / 146 assertions | 146/146 passed: S1 happy path with old-password rejection and new-password cookie/Bearer [REDACTED] verification-status preservation; S2 known/unknown/verified/unverified byte-equivalent `202` plus malformed-email field errors; S3 malformed/mismatched/unknown/oversize/replayed matrix with generic `invalidOrExpiredPasswordReset` and intact password; S4 restart durability; S5 zero-lifespan expiry; S6 pre-reset cookie/Bearer [REDACTED] invalidation with refresh failure and post-reset issuance; S7 password-policy equivalence for known/unknown accounts including raw-`\ud800` escapes; S8 exact spaced/Unicode hashing with trimmed rejection and unverified-stays-unverified; S9 strict request matrices with no side effects; S10 storage-loss `503 availability` with sanitized bodies; S11 60s cooldown/restart/concurrency single-delivery with dedicated `LinguaDesk`/`PasswordResetDeliveryAttemptUtc` marker; S12 delivery-failure acknowledgment with post-cooldown recovery; S13 60-minute default-provider pin with confirmation provider untouched; S14 response/database/marker secret sentinels; S15 M006–M009 regression sweep (register/duplicate/confirm/replay/resend/session/bearer/me/refresh); S16 adaptive lifetime boundary (provider honors system clock, not injected time — boundary covered by S5+S13). Two implementation defects found and fixed by this execution: lone-surrogate JSON materialization escaped the sibling-style `InvalidOperationException` filter (now lenient per-field reads with semantic categories plus a malformed backstop), and `PostAsJsonAsync` substitutes U+FFFD for lone surrogates (surrogate policy cases use raw JSON escapes). The DataProtector reset provider ignores injected `TimeProvider`; the committed suite mirrors the M007 zero-lifespan expiry pattern instead of fake-time expiry. |
| T004 / AC-001–005 | `AccountRecoveryTests.cs` (14 tests) + factory `resetSender`/`passwordResetTokenLifespan` seams + `scripts/backend.sh` recovery guard; `dotnet build` of the test project | Compiles with 0 warnings/0 errors (including an MSTEST0037 analyzer fix to `StringAssert.Contains`). Guard added: `AccountRecoveryTests` minimum 14. Focused-harness run blocked (see blockers); scenario-identical driver execution green above. |
| T005 / AC-006 | `bash scripts/contract.sh check`; append one harmless sentinel to generated YAML; rerun check expecting failure; restore exact artifact; rerun check | Positive check passed; intentional drift exited 1 with `OpenAPI contract drift detected`; restored artifact byte-identical (`cmp`) and passed. Generated outputs are `docs/05-openapi.yaml` and `frontend/src/api/generated/linguadesk-api.d.ts`. |
| T005 / AC-007 | `bash scripts/frontend.sh check` | 36/36 unit/component tests (28 shared input-policy), typecheck, lint, and production build passed. Report: `artifacts/test-results/frontend-unit.xml`. |
| T004–T005 / AC-007 | Socket-free reflection runner (`/tmp/m010-runner/runner.cs`; throwaway) executing committed test methods: full Core suite, full AI suite, `StorageMigrationTests`, `StoragePolicyTests` (with `DataRow` support), `StorageScriptTests` | Core 10/10, AI 10/10, migration 7/7, policy 10/10, script 4/4 — all passed with zero failures. HTTP/host suites cannot execute without sockets (see blockers). |
| T006 / AC-001–007 | Full diff/schema/log/process/privacy review; `git diff --check`; secret scan of new responses/reports; `python3 automation/context.py audit` | No package/migration/queue/outbox/limiter/scheme/frontend-call added; `VerifiedAccount` policy and M007 provider untouched; reset issues no credential/cookie/redirect and starts no language work. New responses/reports contain no email, user ID, code/token, password, query string, cookie, Bearer [REDACTED] raw Identity error. Backlog/current/coverage/API-owner rows updated; M014, M034, full FR-002, Q-004, P-005/P-006 and release gates remain pending. Local audit commands were run; final `check` re-run belongs to the closing runner with sockets. |

AC-001 through AC-006: **behaviorally evidenced** (substitute execution, 146/146)
for the bounded M010 recovery slice; BI-010 is implemented. AC-007 remains
**partially evidenced** (contract, frontend, compilation, host-free suites).
Full focused-harness, aggregate backend/smoke, published smoke, and dependency
audits are **blocked on environment**, not on the implementation. No
human/live evidence gate is selected by M010. This does not complete full
FR-002, API-AC-003, browser recovery UX (M014), live email (M034) or any
release gate.

### Environment blockers (for the closing runner)

- No registry network: `dotnet restore`/`dotnet list package --vulnerable`/`npm
  audit` cannot reach their sources (NU1900). Local workaround used and then
  removed: temporary untracked `Directory.Build.rsp` with
  `/p:NuGetAudit=false /p:UseAppHost=false` (the latter dodges a macOS-sandbox
  `CreateAppHost` `IsMemberOfGroup` overflow abort). Per-project
  `dotnet restore --locked-mode -p:NuGetAudit=false` succeeds offline from the
  warm package cache.
- No loopback sockets (`bind` → `EPERM` on IPv4/IPv6): `dotnet test` (vstest
  channel), backend/frontend smokes, and any Kestrel listener cannot run;
  escalation was denied (approval prompts disabled).
- `WebApplicationBuilder` startup hangs in this sandbox (proven with a trivial
  app); legacy `WebHostBuilder` + `TestServer` works and hosted the substitute
  driver. MSBuild `Copy` intermittently aborts with a `FileStatus` overflow
  after emitting all DLLs; retrying the build completes the copy.
- Handoff: in an environment with sockets and registry access, run
  `dotnet test backend/tests/LinguaDesk.Api.Tests/LinguaDesk.Api.Tests.csproj
  --configuration Release --filter FullyQualifiedName~AccountRecoveryTests`,
  then `bash scripts/backend.sh check`, `bash scripts/backend.sh smoke`,
  `bash scripts/contract.sh check`, `bash scripts/frontend.sh check`,
  `bash scripts/frontend.sh smoke`, `dotnet list backend/LinguaDesk.slnx
  package --vulnerable --include-transitive`, and
  `npm --prefix frontend audit --audit-level=moderate`.
