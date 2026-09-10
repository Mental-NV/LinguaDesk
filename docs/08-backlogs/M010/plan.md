# M010 — Implementation plan

Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Starting from the current planning baseline, extend the completed M009 Identity slice with API password recovery only. Add actual C# DTOs/metadata for forgot-password request and reset completion, generate/review OpenAPI and TypeScript, then implement them with the real Identity password-reset provider (explicit 60-minute lifespan), the M006 exact-password policy, a dedicated durable 60-second reset-delivery cooldown, and stamp rotation that the existing per-request M008/M009 revalidation enforces without new auth code. Preserve strict account parsing, byte-equivalent non-enumerating acknowledgments, no-store Problem Details, database/key readiness and zero-effect contract generation. Do not add browser forms, live email, new auth schemes or new persistence.

The repository pins .NET SDK 10.0.302, Node 24.20.0, npm 11.11.0 and ASP.NET Core Identity EF 10.0.10. M009's completion evidence establishes the prerequisite on the current tree. Execution begins by checking the locked packet, selection, clean/understood diff, tool versions and locked restore access; a material source/dependency drift requires impact review and re-lock rather than an assumed pass.

## Changes and order

1. Add `Features/Identity/Recovery/RecoveryContract.cs` and initially under-construction endpoint metadata for exactly the two selected routes (`requestLocalAccountPasswordReset`, `resetLocalAccountPassword`). Give every operation its stable ID, typed success/problem shapes, accepted content types, response codes and no-store descriptions with no security requirement (both anonymous). Extend `Features/Capabilities/OpenApiRegistration.cs` only as required and document the two operations' headers/security. Map metadata before `/api` fallbacks, generate `docs/05-openapi.yaml` and `frontend/src/api/generated/linguadesk-api.d.ts`, review the semantic diff and keep handlers explicitly incomplete until the reviewed shape is accepted.
2. Add the recovery delivery seam mirroring the M007 pattern: `IAccountPasswordResetSender` with `AccountPasswordResetDelivery(Destination, UserId, Code)` (destination, opaque user ID, unpadded base64url code), a production default that sends nothing, and test-only capturing/failing adapters unreachable from production configuration. Pin the default password-reset provider lifespan to 60 minutes in Identity options with the existing `LinguaDesk` application name/key directory; do not alter the M007 email-confirmation provider. Add the dedicated LinguaDesk-owned reset cooldown marker in the existing `AspNetUserTokens` table behind the existing single-process account gate; add no package or migration.
3. Add `Features/Identity/Recovery/RecoveryEndpoints.cs` and a small coordinator only if needed. Implement strict forgot-password (exact email parsing under M006 rules, durable cooldown check, delivery only for existing accounts outside cooldown, byte-equivalent 202 in all valid cases, availability item on storage/key failure) and strict reset (exact userId/code/newPassword parsing with 450/4096 bounds, base64url decode in handler, M006 exact-password policy check before account disclosure, `UserManager.ResetPasswordAsync` for valid material, generic `invalidOrExpiredPasswordReset` otherwise, no credential/cookie/redirect on success). Reuse/refactor the strict account JSON/media/error helpers rather than allowing framework binders to create a looser boundary.
4. Add `AccountRecoveryTests.cs` and narrowly shared account-host helpers under `backend/tests/LinguaDesk.Api.Tests/`. Use real HTTPS requests, real Identity reset tokens/stamps, migrated file-backed SQLite, durable keys and fake time. Cover the happy path with old-password rejection and new-password cookie/Bearer [REDACTED] verification-status preservation, forgot-password equivalence matrices, reset failure/replay/expiry matrices, restart durability, immediate stamp invalidation of pre-reset session/Bearer [REDACTED], password-policy equivalence for known/unknown accounts, strict request matrices and secret/log sentinels. Keep the positive focused-suite guard in `scripts/backend.sh` covering the new suite.
5. Regenerate/review the contract and types, then run focused and aggregate checks, published smoke and dependency audits. Published smoke adds no reset-specific browser trip; it preserves the existing deterministic shell. Inspect the schema, output, migration model, processes, logs and diff for future routes, secrets or side effects.
6. Reconcile actual behavior/evidence against BI-010 and every AC. Update only affected API/architecture/verification/README/current-delivery/coverage owners if implementation changes their current truth; keep package checkboxes/evidence in `tasks.md`. Do not claim full FR-002, API-AC-003, browser recovery UX, live email or release readiness.

No data model, EF migration, new package, external service, secret, frontend runtime call or deployment configuration is planned. Reset tokens are protected with the existing stable application name/keys. If implementation proves a schema/package or account-lifecycle decision indispensable, stop the affected task, update its design owner and this package, review and re-lock before proceeding.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| M009/current inputs | T001 | `python3 automation/context.py check M010`; inspect `git diff --name-status` and M009 completion link; `dotnet --version`; `node --version`; `npm --version` | Fresh manifest and preflight note in completion record |
| AC-001 | T001/T003/T004 | `dotnet test backend/tests/LinguaDesk.Api.Tests/LinguaDesk.Api.Tests.csproj --configuration Release --filter FullyQualifiedName~AccountRecoveryTests` with forgot/reset happy-path, old/new password sign-in and verification-status cases | Focused result plus reset-shape assertions |
| AC-002 | T003/T004 | Same focused suite with known/unknown/verified/unverified equivalence, cooldown-suppression and delivery-failure matrices | Equivalence assertions, no-enumeration evidence |
| AC-003 | T002–T004 | Same focused suite with malformed/mismatched/unknown/replayed/expired matrices plus a restart-durability case | Focused invalidation test names/result |
| AC-004 | T003/T004 | Same focused suite with pre-reset session/Bearer [REDACTED] immediately after reset plus refresh-failure and post-reset issuance cases | Stamp-invalidation assertions |
| AC-005 | T003/T004 | Same focused suite with password-policy matrices for known/unknown accounts and exact-string hashing cases | Policy assertions |
| AC-006 | T001/T005 | `bash scripts/contract.sh generate`; semantic diff review; `bash scripts/contract.sh check`; back up generated YAML to an owned temporary file, introduce one harmless local drift, require the check to fail, restore the exact backup and require it to pass | Generated YAML/types, path/security review and nonzero drift evidence |
| AC-007 | T005/T006 | `bash scripts/backend.sh check`; `bash scripts/backend.sh smoke`; `bash scripts/frontend.sh check`; `bash scripts/frontend.sh smoke`; `dotnet list backend/LinguaDesk.slnx package --vulnerable --include-transitive`; `npm --prefix frontend audit --audit-level=moderate`; `git diff --check`; `python3 automation/context.py audit` | `artifacts/test-results/`, command summaries and final AC dispositions |

## Migration, rollout and rollback

No migration is expected; pending-model checks must remain green. Reset tokens are protected with the existing stable application name/keys and add no account, session or text data beyond the dedicated cooldown marker row. Serving retains M006 readiness: an explicitly migrated database and usable persistent key directory are required; contract generation remains free of database/key/listener effects.

Rollback removes the two routes/recovery registration while retaining compatible account/key/cookie/Bearer [REDACTED]; never delete accounts or keys as rollback cleanup. A completed reset is a durable password/stamp change and cannot be rolled back by removing the routes; affected users re-establish access through a fresh reset or existing credentials issued after the change.

## Context boundaries and risks

Browser reset forms, focus, messages and safe-return handling are referenced only to preserve the later M014 contract; no React or browser acceptance is selected. M034 owns live delivery/templates/origin. LLM, operation/accounting/cost, provider, quality and performance domains are irrelevant because these routes cannot submit text or call a provider. No AI specification or accounting contract belongs in the execution packet.

Principal risks and controls:

- `ResetPasswordAsync` may not rotate the stamp or the auth handlers may cache it. Assert immediate post-reset 401s on pre-reset session/Bearer [REDACTED] through the real handlers; never add a parallel stamp check when the existing per-request rule already covers it.
- Forgot-password acknowledgments can diverge in headers/timing/shape. Assert byte-equivalent bodies plus auth/cache/location header equivalence, and keep delivery strictly after the durable marker so suppression paths stay identical.
- Token lifetime options can silently apply to the wrong provider. Pin the lifespan on the default password-reset provider type only, leave the M007 confirmation provider untouched, and assert pre-expiry success and post-expiry failure with fake time.
- Password policy can trim or normalize before hashing. Pass the exact supplied string to Identity, test leading/trailing-space and Unicode passwords, and apply policy validation identically for unknown accounts.
- Contract generation could instantiate storage/keys or omit anonymous security metadata. Preserve the existing generation branch, review actual security metadata and assert zero effects separately from runtime semantics.
- Reset is not revocation of copied Bearer [REDACTED] State that boundary explicitly; test stamp/deletion invalidation and leave per-device revocation to its owner.

Human actions: None. Routine restores, owned temporary databases/keys/certificates and deterministic local hosts suffice. Missing locked dependencies, an unavailable real reset-token path or a required lifecycle decision blocks only its affected gate and must be recorded rather than replaced by a fake pass.

Readiness review: M009 is satisfied; exact selected operations and error/security boundaries are fixed; every AC maps to ordered work, a runnable deterministic check and evidence target; contract generation precedes handler completion; no blocking question or human gate remains; omitted domains have explicit owners.
