# M007 — Implementation plan

Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Starting from clean planning baseline `4dafe1d3053b0fe73cee39b8a4dc7b7ae3f2cd18`, extend the existing M006 Identity vertical slice with anonymous confirmation and resend/recovery only. Reuse its strict JSON/Problem Details conventions, current-state authorization, durable database/key readiness, single-process account gate and deterministic delivery seam. Keep the API mutation body-only and secret-free; keep future auth, password recovery, SPA, live mail, LLM and lifecycle scope absent.

The .NET SDK 10.0.302, Node 24.20.0, npm 11.11.0 and Identity EF 10.0.10 pins are present. M006's completion record establishes the prerequisite at the current tree. At execution start, compare intervening changes with the locked manifest, confirm locked restore/tool access and stop only the affected work if a genuine dependency or source conflict appears.

## Changes and order

1. **Declare and review the wire contract first.** Add confirmation/resend request, accepted/status and Problem Details metadata under `Features/Identity/Verification/`; map the two under-construction endpoints from `Program.cs` before the API catch-all. Extend `OpenApiRegistration` to document field constraints and no-store headers, advance the pre-release description/version to M007, then generate and review `docs/05-openapi.yaml` and `frontend/src/api/generated/linguadesk-api.d.ts` before implementing behavior. No auth security scheme or speculative Identity route is emitted.
2. **Pin token and delivery policy.** In `IdentityRegistrationExtensions`, register a dedicated named Data Protection email-confirmation token provider/options type, select it only as `EmailConfirmationTokenProvider`, and pin its lifespan to 24 hours; leave the default/password-reset provider untouched for M010. Replace the string-only sender call with a small structured delivery record containing destination, opaque user ID and base64url code. Add a shared `AccountVerificationDeliveryCoordinator` used by registration and resend. Store the UTC cooldown attempt marker through `UserManager`'s existing authentication-token store before sending, use the injected `TimeProvider`, and serialize registration/resend delivery decisions with the existing account gate. Suppress sending when marker persistence fails and map it through the existing generic delivery-unavailable path. No schema migration is expected; a pending-model check must prove that conclusion.
3. **Implement confirmation and resend coordinators/endpoints.** Parse exact JSON with bounded values and existing UTF-8/media/query/method rules. Decode base64url safely, load the user, and use Identity's confirmation API. Map valid/already-confirmed material to `verified`; map all invalid/expired/unknown combinations to one generic category; map infrastructure loss to sanitized availability. Resend validates syntax before lookup, returns one fixed 202 shape for every valid email, sends only for current unverified accounts outside cooldown, and never issues credentials, redirects, changes security stamps, or invokes a future operation.
4. **Build deterministic account evidence.** Add focused `AccountVerificationTests` using the real file-backed store, Data Protection keys and HTTP host. Extend the M006 factory/senders into shared account test support only where needed. Use controlled concurrent calls and fake time, not sleeps. Cover captured registration round-trip, corruption/mismatch/unknown/expired/oversize cases, restart and idempotence, exact known/unknown/confirmed resend equivalence, durable cooldown boundary/concurrency, failed-delivery recovery, storage loss, wrong methods and privacy sentinels. Keep test adapters service-injected only.
5. **Run regressions and close honestly.** Update positive test-count guards without freezing exact totals. Run focused and aggregate checks, deterministic contract generation/drift detection, backend and published smoke, frontend type/build/smoke, package audits, scope/source review and documentation audit. Update README, stable design/status/coverage owners and this package only where actual evidence requires it; do not mark M012/M034, full FR-002, RG-005 or any release gate complete.

| Target | Intended change |
| --- | --- |
| `Features/Identity/Verification/`, `Registration/`, `IdentityRegistrationExtensions.cs`, `Program.cs` | Selected DTOs, strict endpoints, Identity confirmation, shared structured delivery/cooldown and registration handoff |
| `Features/Capabilities/OpenApiRegistration.cs`, generated YAML/types | M007 pre-release metadata and only the two new anonymous operations |
| `backend/tests/LinguaDesk.Api.Tests/` and `scripts/backend.sh` | Real HTTP/Identity/restart/concurrency/fake-time/privacy evidence and positive suite guard |
| README and affected #3/#5–#7/coverage/package rows | Actual operation/configuration/evidence and explicit later-gate status only after checks |

No migration, new package, configuration secret, external service, frontend runtime call, email template/provider, deployment change or compatibility promise is planned. If implementation proves a schema/package change indispensable, stop, update its owning design/package inputs, review risk and re-lock before proceeding rather than silently expanding M007.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| M006/current inputs | T001 | `python3 automation/context.py check M007`; inspect `git diff --name-status` and M006 completion link; `dotnet --version`, `node --version`, `npm --version`; locked restore/audit commands below | Fresh manifest and preflight note in completion record |
| AC-001 | T002/T003/T004 | `dotnet test backend/tests/LinguaDesk.Api.Tests/LinguaDesk.Api.Tests.csproj --configuration Release --filter FullyQualifiedName~AccountVerificationTests` | TRX/focused console result plus one captured delivery/confirmed row/current-policy assertions |
| AC-002 | T003/T004 | Same focused suite with data-driven invalid/expired/size/encoding cases and response/log secret sentinels | Focused test names and sanitized result |
| AC-003 | T002/T003/T004 | Same focused suite with same-file/key restart, named email-provider/options assertion, unchanged default/reset provider, zero-lifetime email-provider expiry fixture, pre-expiry success and repeated-valid confirmation | Focused test names and sanitized result |
| AC-004 | T003/T004 | Same focused suite comparing status/body/cache/auth/location headers for known-unverified/confirmed/unknown requests | Focused test names and sanitized result |
| AC-005 | T002/T003/T004 | Same focused suite with barrier-controlled parallel calls, fake-time 59/60-second boundary and new-host durable token-row check | Focused test names and sanitized result |
| AC-006 | T002/T003/T004 | Same focused suite with failing/capturing sender sequence, cooldown advancement and final confirmation | Focused test names and sanitized result |
| AC-007 | T001/T005 | `bash scripts/contract.sh generate`; semantic diff review; `bash scripts/contract.sh check`; then back up the generated YAML to an owned temporary file, introduce one harmless local YAML drift, require `bash scripts/contract.sh check` to exit nonzero, restore the exact backup and require the check to pass | Generated YAML/types, exact-path assertions and nonzero drift evidence |
| AC-008 | T005/T006 | `bash scripts/backend.sh check`; `bash scripts/backend.sh smoke`; `bash scripts/frontend.sh check`; `bash scripts/frontend.sh smoke`; `dotnet list backend/LinguaDesk.slnx package --vulnerable --include-transitive`; `npm --prefix frontend audit --audit-level=moderate`; `git diff --check`; `python3 automation/context.py audit` | `artifacts/test-results/`, command summaries and final AC dispositions |

## Migration, rollout and rollback

The design reuses `AspNetUserTokens`; implementation must produce no EF migration and the existing migration/model-drift checks must remain green. Cooldown metadata has the same unresolved account lifecycle as the Identity account and adds no text or email body. Data Protection application name/key location remain unchanged so M006-issued links survive compatible restarts. There is no automatic database/key creation, migration or repair.

Deploy only with the same explicit migrated storage and durable key prerequisites as M006. Until M034 supplies a configured real adapter, registration/resend delivery remains unavailable outside deterministic tests even though confirmation can be exercised with captured material; do not advertise a live account service. Rollback removes the two routes/coordinator changes while retaining compatible account/key/token-table data. Never delete accounts, keys or cooldown rows as rollback cleanup.

## Context boundaries and risks

Frontend UX/accessibility/browser acceptance is excluded because M012 owns `/verify-email` rendering, link parsing, status/cooldown messages and continuation. Cookie/bearer/antiforgery/security-stamp work belongs to M008/M009; password reset to M010; real mail/public-origin/template/click evidence to M034. LLM, operation accounting/cost, provider, performance and quality domains are unaffected because these endpoints cannot start language work. Q-004 deletion/backup rules and P-005/P-006 do not become binding through this package.

Principal risks and controls:

- Identity token text is not directly URL-safe. Encode the UTF-8 token as unpadded base64url at the delivery boundary and strictly decode before verification; never log either form.
- A global token-lifetime change would pre-decide M010 reset behavior, while a volatile cooldown would drift across restarts. Use a dedicated named email-confirmation provider/options type, leave default/reset tokens untouched, and store the UTC attempt marker in the existing Identity token table; prove production options and restart behavior.
- Resend can enumerate membership through response variations. Use one fixed valid-input result for known/unknown/confirmed/cooldown/delivery-failure cases and assert byte/header equivalence. Exact network timing equivalence is not claimed.
- Delivery and cooldown storage are not one external transaction. Record the attempt before calling the adapter, serialize local decisions and accept that a crash may suppress resend for at most the fixed 60 seconds; do not add an outbox in M007.
- Contract generation could activate database/keys/email. Preserve the existing generation boundary and assert zero side effects while registering real metadata.
- Framework helpers could expose future account routes. Map only custom selected endpoints and assert the generated path set and absent sign-in/reset/status/language routes.

Human actions: None. Routine local restores, temporary databases/keys and deterministic adapters are sufficient. M012 and M034 human/live evidence remains explicitly pending. A missing locked dependency, incompatible Identity API, or unavoidable schema/public-origin decision blocks only the affected gate and requires package/source review; it must not be replaced by a fake success.

Readiness review: every selected AC maps to an ordered task, a runnable deterministic check and an evidence target; M006 is satisfied; no blocking question or human gate remains; generated-contract ordering precedes handler completion; omissions are assigned to later milestones rather than silently accepted.
