# 006 — Register a Local API Account: Implementation Plan

**Version:** 1.1 · **Updated:** 2026-09-09
**State:** Implemented and verified
**Inputs:** [spec.md](spec.md) v1.1; [BI-006](../../docs/08-backlogs/M006-register-local-api-account.md) v1.1

## 1. Current state, revisions and preflight

Clean baseline `4aca2c52aa0212ba4612dcc005b7c0bde099b186` contains .NET SDK `10.0.302`, ASP.NET Core/EF `10.0.10`, the Minimal API host, empty application DbContext with immutable `20260908221711_InitialStorage`, explicit storage tooling, public M005 capabilities contract, OpenAPI 3.1/type generation, React shell and focused tests. It has no Identity package/model/store, auth/authorization, account operation, Data Protection path, readiness endpoint or email boundary. Preserve existing Core/Infrastructure.Ai independence and API/static/fallback behavior.

Planning read #0 v1.7, #1 v0.6, #2 v1.6, #3 v1.10, #4 v1.5, #5 v1.3, #6 v1.10, #7 v1.10 and #9 v1.7 at that commit, then reconciled #3/#5–#7 to v1.11/v1.4/v1.11/v1.11 for this package. M003 and M005 completion evidence and Git history satisfy the formal dependencies. Selection checks passed 67 backend/Core/AI cases and 36 frontend cases with clean build/type/lint. `scripts/contract.sh check` repeatedly stalled in its ordinary restore after its npm dependency check, while the same locked restore with `--disable-build-servers` returned immediately. T001 must make this command deterministic before contract-dependent implementation; this is a known tooling risk with a safe bounded remedy, not silently passing evidence.

At execution start, confirm the worktree still matches the planning baseline or assess every intervening change under #0. Check SDK/Node/npm/local EF pins, locked restore and audits. Recheck the current ASP.NET Core 10 [Identity API/store surface](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0) and [Data Protection key configuration](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0), checked for planning on 2026-09-09, before pinning the Identity EF package at the existing framework patch. Package 006 deliberately maps one custom selected registration route instead of exposing the whole framework endpoint set. The password policy is LinguaDesk's selected technical contract, informed by current [NIST SP 800-63B password guidance](https://pages.nist.gov/800-63-4/sp800-63b.html#passwordver) but not a claim of NIST conformance or adoption of P-005.

## 2. Identity, persistence and key design

Add the pinned `Microsoft.AspNetCore.Identity.EntityFrameworkCore` dependency only to `LinguaDesk.Api`; use the standard string-key `IdentityUser`/Identity store and password hasher without roles or a custom user profile. Change `LinguaDeskDbContext` to the appropriate Identity DbContext base and register IdentityCore/store/token providers explicitly. Configure unique normalized email and the package password options/validator; do not use `MapIdentityApi`, because it would expose sign-in, refresh, reset, 2FA and account-management routes outside M006.

Generate one new `LocalAccounts` migration and snapshot through the existing design-time factory. Do not edit/recreate `InitialStorage`. Inspect that only standard selected Identity tables/indexes/constraints appear and that no role/product/ledger/text/outbox table is introduced. Tests migrate fresh and M003-baseline databases, preserve an existing synthetic probe, check pending model changes and exercise case-insensitive unique email concurrency using file-backed SQLite. The account remains durable with no automatic cleanup; this is staging behavior, not resolution of Q-004 deletion/backup retention.

Add typed security options and a path policy beside host infrastructure. `Security:DataProtectionKeysPath` is an explicit absolute existing directory, default `/var/lib/linguadesk/keys`, rejected when absent/unsafe/static/publish-contained/unusable. Configure `AddDataProtection().SetApplicationName("LinguaDesk").PersistKeysToFileSystem(...)`. Do not expose key material or add a database key table. Test-created directories are uniquely owned; production directories are provisioned externally and never recursively removed by application code.

Introduce a startup validator/hosted boundary that, outside contract generation, opens the configured existing database in runtime read/write mode, verifies the applied migration chain has no pending migration and obtains then rolls back a bounded write lock. Validate the key directory before listening. Do not migrate/create/repair/delete. Add a readiness health check over current database/key availability and map `GET /health/ready` outside OpenAPI; retain `/health/live` as dependency-free process liveness and the unknown-health 404 middleware. On post-start loss, readiness reports only unhealthy while account failures use sanitized 503 Problem Details.

Contract build-time startup must bypass only effectful readiness activation through a non-user-facing generation marker controlled by `scripts/contract.sh`; it still registers actual routes/metadata. Tests prove ordinary Production-like startup cannot set a public configuration flag to skip validation. No runtime account-serving configuration may silently select a test delivery adapter.

## 3. Registration slice and wire contract

Place the vertical slice under `backend/src/LinguaDesk.Api/Features/Identity/Registration/` (or a consolidated `Features/Identity/` layout that keeps this single operation explicit). Use thin endpoint parsing/typed results and a concrete coordinator around `UserManager` plus a narrow `IAccountConfirmationSender`. The endpoint owns strict JSON/content handling, DTO metadata and response mapping; Identity owns hashing/normalized lookup/store writes. Do not add a repository, generic result framework, UI client or auth schemes.

Implement operation ID `registerLocalAccount` at `POST /api/accounts/register` before the `/api` catch-all. Request has only required email/password. Add explicit validation for strict shape, email rules and well-formed 15–128 Unicode-scalar password length with no trim/normalization/composition. Configure Identity defaults consistently so later sign-in/reset reuse the exact password. Confirm-password remains client-only.

The coordinator first validates without effects, then attempts creation. Map successful creation and normalized-email duplicate/race to the same typed `202 { status: "verificationRequired" }` with `Cache-Control: no-store`, no auth material and no account identifier. For a duplicate, perform no confirmation delivery. Do not promise timing equivalence, but avoid caller-visible membership distinctions. Map policy validation to RFC 9457 `400`, incompatible media to `415`/`invalidRequest` and unavailable storage to sanitized `503`/`availability`; use required `category`/`correlationId` extensions and field-only `errors` where applicable. Apply `no-store` to every response and avoid raw Identity errors/exception details.

After a new account commits, generate opaque confirmation material using Identity/Data Protection and call the injected sender exactly once. The boundary accepts only the destination plus verification material and cancellation; it never accepts a password. A delivery exception leaves the account unverified, emits only an allowlisted category/correlation and returns the same generic acknowledgment without automatic retry. In-process tests inject capturing/failing fakes. The application has no production sender in M006; production-like serving must not accidentally select a fake, and documentation must state that live registration delivery remains unavailable until M034 even though deterministic registration behavior is verifiable now.

Add authorization services and a named `VerifiedAccount` policy whose handler loads current Identity state for a principal and denies missing/unconfirmed accounts. Do not map a test or placeholder language endpoint. Exercise the handler directly/in integration against persisted unconfirmed/confirmed fixtures and inspect that no provider reference/route/call is introduced.

## 4. Generated contract and affected components

At implementation start, declare the selected C# request/response/Problem Details metadata, add registration to the existing `linguadesk` document, and advance only the pre-release artifact version/description to `0.1.0-m006`. Regenerate `docs/05-openapi.yaml` and `frontend/src/api/generated/linguadesk-api.d.ts` before completing handler behavior, then review the exact route/operation/request/202/400/415/503/no-store/security semantics. Capabilities remains unchanged. No auth security scheme is attached to anonymous registration; no future Identity route appears.

Make the contract wrapper's restore behavior bounded and reproducible by applying the observed `--disable-build-servers` remedy to its locked restore, then validate normal and controlled failure runs before relying on it. Retain atomic output replacement, zero-path/drift failure, real entry-point generation and no migration/key/email/listener side effects. Do not add a frontend runtime fetch dependency merely because new types exist.

| Target | Intended change |
| --- | --- |
| Central packages/API project and lockfiles | Pin Identity EF at the existing patch; no Core/Ai/frontend runtime dependency |
| `Infrastructure/Persistence/` and new security/readiness infrastructure | Identity DbContext/store migration, durable key path, startup validation and readiness health check |
| `Features/Identity/Registration/` and `Program.cs` | Strict selected DTO/endpoint/coordinator, confirmation sender boundary and verified-account policy |
| OpenAPI registration, `scripts/contract.sh`, generated YAML/types | Add only registration semantics, `0.1.0-m006`, deterministic/no-effect generation and wrapper recovery |
| API tests/storage fixtures and scripts | Real Identity/file-backed HTTP, migration/readiness/concurrency/secret-sentinel tests and positive count guards |
| README and #3/#5–#7 closeout | Actual setup/migration/key/check behavior, evidence and explicit M007+/M034/Q-004 limitations |

## 5. Verification and scenario coverage

Use real ASP.NET Core Identity, EF SQLite and Data Protection; do not mock `UserManager`, stores, `DbSet` or hashing to claim registration behavior. Most HTTP cases use `WebApplicationFactory` with unique migrated file/key directories and injected confirmation senders. Concurrency/restart/migration/readiness use file-backed stores, distinct contexts and barriers rather than sleeps. Test passwords and emails are synthetic; sentinel assertions inspect responses, logs, reports and key filenames/content boundaries without printing secrets.

| Scenario | Verification method and evidence target |
| --- | --- |
| AC-001 | HTTP integration through real Identity/store/hasher; inspect one unconfirmed row and one captured delivery, headers/body and absent auth material |
| AC-002 | Data-driven strict JSON/email/password scalar boundaries plus Problem Details/schema assertions and response/log/report secret sentinels |
| AC-003 | Sequential case variants and barrier-controlled parallel HTTP requests against file-backed SQLite; one normalized row/delivery and identical external outcomes |
| AC-004 | Same-file new host/context plus direct named-policy evaluation of unconfirmed/confirmed/missing fixtures; route/reference inspection proves no LLM surface |
| AC-005 | Capturing and throwing sender fixtures; durable row remains, generic response matches, one attempted effect, no retry or secret diagnostics |
| AC-006 | Fresh/stale/missing/locked/unusable DB and key-directory process/readiness checks; startup nonzero/no listener, liveness distinction and no implicit mutation |
| AC-007 | Contract generate/check and semantic assertions; generated TypeScript compilation, deliberate drift/CLI/restore failure, no-effect/key/storage/email/process inspection |
| AC-008 | New migration/upgrade/model-drift tests, focused/aggregate backend checks, backend/contract/frontend/published smoke and full scope/evidence review |

This allocates selected V-004, V-009, V-015 and V-016 assertions. Retain M003/M005 V-005/V-012 and frontend V-003 regressions where touched. No browser registration, live email, confirmation completion, auth token/cookie, antiforgery, LLM, accessibility or release evidence is applicable. Add minimum-positive guards for registration/migration/readiness tests without freezing a forever exact aggregate count.

## 6. Migration, rollout, operations and risks

**Migration/rollback:** Add one forward Identity schema migration after the immutable M003 baseline. Verify clean and upgrade application before changing the package state. Rollback before release stops the host and applies the tested down migration only to an installation that has not acquired needed account data; otherwise preserve the database and restore code/migration compatibility rather than deleting users. Data Protection keys are durable deployment data and are not removed by rollback. Production bundle/backup/restore evidence remains M040.

**Rollout/operation:** Account serving requires explicit `Storage:DatabasePath`, explicit migration, an existing restricted key directory and a non-test confirmation sender in a later deployment. Startup/readiness cannot manufacture these. M006's deterministic adapter is test-only, so no live registration service or email acceptance is claimed. README closeout records verified local commands and the current limitation.

**Observability/privacy/security:** Log only correlation, operation category/outcome and safe duration; never email, password, normalized email, confirmation token/link, key contents/path or request body. Responses use no-store and expose no account identifier. Confirmation material exists only in bounded memory/test capture. The password policy is required auth behavior; it does not adopt short-term abuse controls, a breach list or compatibility promises.

**Principal risks and mitigations:**

- Identity helper APIs could expose future routes or framework error shape. Register only the custom operation and assert generated paths/statuses.
- Unicode length and Identity defaults can diverge. Use one explicit scalar validator, configure composition flags off and test exact 14/15/128/129 and decomposed/space cases.
- Concurrent duplicate registration can surface a SQLite uniqueness exception or duplicate email. Resolve the normalized-email race to the generic result and assert one row/delivery.
- Email succeeds outside the database transaction and cannot be atomic with account creation. Persist first, keep the account unverified, avoid automatic retry and leave recovery to M007.
- Data Protection/default key discovery can write outside owned storage or invalidate future links. Require the explicit directory/application name and test across host restart.
- Startup validation can break contract generation or M001/M002 smokes. Separate real route registration from effectful serving activation and prove both ordinary failure and generation no-effect paths.
- The current contract wrapper can stall on an MSBuild-server restore. Correct/bound it in T001 and retain nonzero failure diagnostics before relying on generated artifacts.
- Q-004 lifecycle is unresolved. Make no deletion/backup-retention promise, preserve durable records/keys and keep the later gate visible.

## 7. Dependencies, human steps and final consistency

M003/M005 are satisfied. Package restore, writable owned temp paths and local process permissions are the only execution prerequisites. **No human action is required at the beginning or end.** No real email address, link click, credential, TLS certificate, production volume, browser/device or product approval is requested. An unexpected human-only dependency follows #0: finish independent work, prepare the exact handoff and keep affected evidence pending; stop if nothing safe remains.

No blocking technical question remains. #3 owns the selected Identity/key/readiness staging; #5 owns the registration/password/email/duplicate/delivery wire behavior; #6 owns execution/evidence. Existing ADR-003/004/007/009 cover the vertical slice, generated contract, migration parity and unit-heavy boundary verification; no consequential new architecture direction requires an ADR. Q-004, P-005 and P-006 retain their prior status.

Consistency review passed: BI-006 → US-001–003 → AC-001–008 → the approach, verification table and T001–T008 are complete and contain no orphan scenario or unsupported production work. Every touched component is justified by registration or its indispensable migration/key/readiness/contract prerequisite. Implementation and runtime verification completed with exact evidence in [tasks.md](tasks.md#3-completion-record).
