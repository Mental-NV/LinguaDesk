# M008 — Implementation plan

Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Starting from planning baseline `bd3ebe70ebd95ce168e3751250c58a1694ad29fd`, extend the completed M007 Identity slice with cookie session access only. Add actual C# DTOs/metadata for antiforgery bootstrap, sign-in, session read and sign-out, generate/review OpenAPI and TypeScript, then implement them with the real Identity cookie and antiforgery services. Preserve strict account parsing, no-store Problem Details, database/key readiness and zero-effect contract generation. Do not add bearer refresh, password recovery, React account UX, language work or new persistence.

The repository pins .NET SDK 10.0.302, Node 24.20.0, npm 11.11.0 and ASP.NET Core Identity EF 10.0.10. M007's completion evidence establishes the prerequisite on the current tree. Execution begins by checking the locked packet, selection, clean/understood diff, tool versions and locked restore access; a material source/dependency drift requires impact review and re-lock rather than an assumed pass.

## Changes and order

1. Add `Features/Identity/Session/SessionContract.cs` and initially under-construction endpoint metadata for exactly the four selected routes. Give every operation its stable ID, typed success/problem shapes, accepted content types, response codes, no-store/cookie descriptions and auth/antiforgery security requirements. Extend `Features/Capabilities/OpenApiRegistration.cs` only as required to describe the two named cookies/header and current cookie security. Map metadata before `/api` fallbacks, generate `docs/05-openapi.yaml` and `frontend/src/api/generated/linguadesk-api.d.ts`, review the semantic diff and keep handlers explicitly incomplete until the reviewed shape is accepted.
2. Extend `Infrastructure/Persistence/PersistenceRegistrationExtensions.cs` and `Features/Identity/IdentityRegistrationExtensions.cs` with the real sign-in manager/authentication cookie configuration and explicit eight-hour, no-sliding, session-only `__Host-LinguaDesk.Session` policy. Add a per-request principal validator that loads the account, compares the security stamp and rejects missing/deleted/stale principals immediately. Configure bearer-header selection so it cannot fall back to cookies while leaving actual bearer issuance/refresh to M009. Add no package or migration.
3. Add `Features/Identity/Session/SessionEndpoints.cs` and a small coordinator/event type only if needed. Configure `__Host-LinguaDesk.Antiforgery` and `X-LinguaDesk-Antiforgery`; integrate authentication and antiforgery middleware/filter ordering in `Program.cs`. Reuse/refactor the strict account JSON/media/error helpers rather than allowing framework binders or HTML redirects to create a looser boundary. Use injected time consistently for ticket issue/expiry and returned UTC expiry.
4. Add `AccountSessionTests.cs` and narrowly shared account-host helpers under `backend/tests/LinguaDesk.Api.Tests/`. Use real HTTPS requests, CookieContainer behavior, Identity password verification, cookie tickets, antiforgery, migrated file-backed SQLite, durable keys and fake time. Cover bootstrap/token rotation, confirmed/unverified/invalid credentials, exact cookie attributes, no renewal, expiry boundary, stamp/deletion invalidation, bearer-no-fallback, idempotent sign-out, strict failures and secret/log sentinels. Update `scripts/backend.sh` with a positive focused-suite minimum so zero/filtered tests cannot pass.
5. Regenerate/review the contract and types, then run focused and aggregate checks, published smoke and dependency audits. Published smoke adds only a deterministic verified-account cookie round trip over owned loopback HTTPS if its fixture can remain test-only and inaccessible in production; it does not implement M013 UI acceptance. Inspect the schema, output, migration model, processes, logs and diff for future routes, secrets or side effects.
6. Reconcile actual behavior/evidence against BI-008 and every AC. Update only affected API/architecture/verification/README/current-delivery/coverage owners if implementation changes their current truth; keep package checkboxes/evidence in `tasks.md`. Do not claim full FR-001/002, API-AC-002/003, RG-005, browser UX or release readiness.

No data model, EF migration, new package, external service, secret, frontend runtime call or deployment configuration is planned. Cookie and antiforgery configuration use the existing persistent Data Protection boundary. If implementation proves a schema/package/public-origin or account-deletion decision indispensable, stop the affected task, update its design owner and this package, review and re-lock before proceeding.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| M007/current inputs | T001 | `python3 automation/context.py check M008`; inspect `git diff --name-status` and M007 completion link; `dotnet --version`; `node --version`; `npm --version` | Fresh manifest and preflight note in completion record |
| AC-001 | T001/T003/T004 | `dotnet test backend/tests/LinguaDesk.Api.Tests/LinguaDesk.Api.Tests.csproj --configuration Release --filter FullyQualifiedName~AccountSessionTests` with bootstrap/cookie/header/missing/mismatch/identity-change cases | Focused result plus sanitized response/cookie assertions |
| AC-002 | T002–T004 | Same focused suite with a confirmed real account, exact password, CookieContainer, ticket inspection and unchanged-cookie assertion | Focused test names and expiry/cookie evidence |
| AC-003 | T002–T004 | Same focused suite comparing unknown/wrong-password responses and testing unverified real sign-in/current policy | Byte/header/category equivalence and policy assertions |
| AC-004 | T002/T004 | Same focused suite with fake-time just-before/at eight hours, request-counting principal validation, stamp change, deletion and valid-cookie-plus-Bearer cases | Focused invalidation test names/result |
| AC-005 | T003/T004 | Same focused suite with post-login bootstrap, sign-out/replay, expired Set-Cookie parsing, old-cookie session rejection and unchanged stamp/second-cookie assertions | Focused sign-out test names/result |
| AC-006 | T003/T004 | Same focused suite with strict request matrices and post-start storage failure; retained M006 key-readiness tests; selected email/password/cookie/token sentinels across response/log/report output | Sanitized focused result and sentinel inspection |
| AC-007 | T001/T005 | `bash scripts/contract.sh generate`; semantic diff review; `bash scripts/contract.sh check`; back up generated YAML to an owned temporary file, introduce one harmless local drift, require the check to fail, restore the exact backup and require it to pass | Generated YAML/types, path/security review and nonzero drift evidence |
| AC-008 | T005/T006 | `bash scripts/backend.sh check`; `bash scripts/backend.sh smoke`; `bash scripts/frontend.sh check`; `bash scripts/frontend.sh smoke`; `dotnet list backend/LinguaDesk.slnx package --vulnerable --include-transitive`; `npm --prefix frontend audit --audit-level=moderate`; `git diff --check`; `python3 automation/context.py audit` | `artifacts/test-results/`, command summaries and final AC dispositions |

## Migration, rollout and rollback

No migration is expected; pending-model checks must remain green. Session and antiforgery tickets are protected with the existing stable application name/keys and add no database rows or text retention. Serving retains M006 readiness: an explicitly migrated database and usable persistent key directory are required; contract generation remains free of database/key/listener effects.

Deploy only behind HTTPS because both cookies use the `__Host-` prefix and Secure attribute. Configuration is fixed in code for this milestone rather than exposed as a weakening runtime switch. Existing sessions created by this first cookie contract have an eight-hour maximum and become invalid if keys are deliberately rotated or stamps change. Rollback removes the four routes/auth configuration while retaining compatible account/key data; never delete accounts or keys as rollback cleanup. M013 remains responsible for client-side immediate teardown even if the sign-out response is lost.

## Context boundaries and risks

UX route, focus, messages, safe-return handling, password DOM/storage behavior and workspace clearing are referenced only to preserve the later M013 contract; no React or browser acceptance is selected. M009 owns bearer/refresh issuance and full dual-mode behavior; M010 owns reset and its stamp mutation; Q-004 owns deletion/backup/further revocation guarantees. LLM, operation/accounting/cost, provider, quality and performance domains are irrelevant because these routes cannot submit text or call a provider. No AI specification or accounting contract belongs in the execution packet.

Principal risks and controls:

- Framework defaults can silently create persistent/sliding cookies, delayed stamp checks or HTML redirects. Pin every cookie option, validate current account/stamp on each request, inspect tickets/headers and assert API-only machine responses.
- Antiforgery can be weakened by JSON/SameSite assumptions or become unusable after identity changes. Require the double-submit pair for both cookie mutations, return the request token only in the bootstrap body, and prove re-bootstrap after sign-in plus stale-pair rejection.
- Sign-in can enumerate accounts or leak secrets. Use one invalid-credential result for missing/wrong credentials, disclose verification only after password success, redact all auth material and test response/header/log equivalence.
- Default authentication could accept a cookie despite an invalid bearer header. Add explicit scheme selection/rejection now without exposing M009 token operations, and test no fallback.
- Contract generation could instantiate storage/keys or omit nonstandard cookie/antiforgery requirements. Preserve the existing generation branch, review actual security metadata and assert zero effects separately from runtime semantics.
- Sign-out cannot revoke copied/other cookies without a stamp change. State that boundary explicitly; test caller-cookie clearing and leave account-wide/per-device revocation to its owner.

Human actions: None. Routine restores, owned temporary databases/keys/certificates and deterministic local hosts suffice. Missing locked dependencies, an unavailable real antiforgery/cookie integration path or a required lifecycle decision blocks only its affected gate and must be recorded rather than replaced by a fake pass.

Readiness review: M007 is satisfied; exact selected operations and error/security boundaries are fixed; every AC maps to ordered work, a runnable deterministic check and evidence target; contract generation precedes handler completion; no blocking question or human gate remains; omitted domains have explicit owners.
