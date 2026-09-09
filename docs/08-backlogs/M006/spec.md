# 006 — Register a Local API Account: Selected Specification

**Version:** 1.1 · **Updated:** 2026-09-09
**State:** Implemented and verified; AC-001–008 passed
**Milestone/item:** [M006 / BI-006](backlog.md)

## 1. Selection and authority

Select BI-006 only. The [backlog](backlog.md#1-outcome-and-authoritative-inputs) records the clean Git `4aca2c52` baseline, repository/Git inspection, dependency evidence, current checks and source versions. Apply workflow #0 v1.7, PRD #1 v0.6, UX #2 v1.6, architecture #3 v1.11, AI #4 v1.5, API #5 v1.4, verification #6 v1.11, roadmap #7 v1.11 and ADR-003/004/007/009.

M003 and M005 are complete and supply the file-backed migration/runtime boundary plus generated API-contract lifecycle. M001/M002/M004 remain regression baselines but are not extra formal dependencies. The selected slice resolves Q-006 only for registration and resolves the M003 handoff for database-dependent startup/readiness. Q-004 deletion/backup policy, P-005 safeguards and P-006 compatibility remain unchanged and do not authorize work here.

## 2. Selected stories and scope

**US-001 (P1) — Register without the SPA.** As an independent API consumer, I can submit valid local email/password details and receive a generic verification-required acknowledgment without loading React or receiving authentication credentials.

**US-002 (P1) — Reject invalid or duplicate effects safely.** As an account owner, I receive field-correctable errors for invalid details, while repeated/case-variant/concurrent valid requests cannot create multiple accounts or expose whether a normalized email was already registered.

**US-003 (P1) — Preserve the unverified boundary durably.** As a later language-feature implementer, I can rely on a migrated Identity account that starts unverified and on a named authorization policy that reads current account state and rejects it; registration and status/readiness checks never invoke language/provider work.

M006 adds the minimal Identity account model/schema, registration coordinator/endpoint, durable Data Protection configuration, narrow confirmation-delivery boundary, verified-account policy and database/key readiness needed for account serving. It updates the generated contract/types but does not add a frontend transport wrapper or consume them in React.

## 3. Selected behavior and clarification decisions

### 3.1 Registration operation

`POST /api/accounts/register` has operation ID `registerLocalAccount`. It is anonymous and accepts one UTF-8 JSON object with exactly two required camel-case string fields: `email` and `password`. It accepts no query/route values, confirmation password, account identifier, role, verification state or client-selected policy. Unknown or duplicate properties, malformed JSON and unsupported content types fail before account creation or email work.

A newly acceptable normalized email creates exactly one durable local account and requests one confirmation delivery after persistence. Success returns HTTP `202`, UTF-8 `application/json`, `Cache-Control: no-store` and the required body field `status = verificationRequired`. It returns no email, account/user ID, password data, password-policy diagnostics beyond validation, security/concurrency stamp, confirmation code/link, cookie, bearer/refresh token or `Location`. Registration never signs in, confirms, redirects or calls an LLM. Every response from this operation, including errors, declares `Cache-Control: no-store`.

A syntactically/policy-valid request whose normalized email already exists returns the same `202` status, body and cache/auth headers, creates nothing and requests no registration delivery. Concurrent requests are resolved by the normalized-email uniqueness constraint; exactly one may create/deliver, and every valid caller receives the same acknowledgment. The contract does not promise perfect network timing equivalence, but code must avoid an intentional account-existence field, status, header or log distinction.

Invalid request details return RFC 9457 Problem Details with HTTP `400`, application category `invalidRequest`, a correlation identifier and field errors keyed only as `email` or `password`. The wire uses standard Problem Details members plus required extensions `category` and `correlationId`; field failures add required `errors`, an object mapping an allowed field name to a nonempty array of messages. Error text may state the published syntax/length rule but never echoes either submitted value. Unsupported content uses `415` with category `invalidRequest`; storage/readiness failure uses `503` with category `availability` and no database/path details. `POST` is the only method on this path; unknown `/api` paths remain API 404 and never SPA HTML.

### 3.2 Email and password policy

Email is required, nonempty, no more than 254 Unicode scalar values, contains no surrounding whitespace and must pass .NET's `EmailAddressAttribute` validation. Identity's invariant uppercase normalized lookup supplies case-insensitive uniqueness; the submitted display form is not returned by this operation. No DNS/deliverability check, plus-address rewriting, domain rewriting or external-provider linking is implied.

Password is required and must be well-formed Unicode with 15–128 Unicode scalar values inclusive. Spaces and all otherwise valid Unicode values are allowed. The server does not trim, Unicode-normalize, truncate or require uppercase, lowercase, digit or symbol categories; the exact accepted string is hashed by ASP.NET Core Identity and must be supplied exactly to later sign-in/reset operations. M006 does not claim a breached-password blocklist, short-term attempt limit or NIST conformance. Those absent safeguards cannot be added under P-005 without its owning review.

The API has no confirmation-password field. M011's browser form will compare Password and Confirm password locally before sending this same two-field contract and will render the server's actual policy. Passwords are write-only secrets: request-body logging, traces, errors, generated examples, reports and email payloads must not contain a real or submitted password.

### 3.3 Account, delivery and authorization state

Use ASP.NET Core Identity's local user/store implementation with unique normalized email and the standard framework schema; no role/admin/product-profile behavior is selected. A new account has `EmailConfirmed = false` and no authentication session/token. Account and Identity metadata are durable and survive a host restart. The existing M003 migration remains immutable; a new inspected migration/snapshot adds the coherent account schema.

After successful creation, a narrow injected adapter receives one confirmation-delivery intent containing only the destination and opaque verification material required by M007. Deterministic tests use capturing and failing adapters; production cannot select them. A delivery exception is recorded only as a safe category/correlation, leaves the account unverified, triggers no automatic retry and does not change the generic `202` response. M007 supplies confirmation/resend/cooldown and recovery from this state; M034 supplies a live provider and delivery evidence.

Register a named `VerifiedAccount` authorization policy/handler that resolves the current Identity record rather than trusting a stale claim. It denies a missing/deleted or `EmailConfirmed = false` account and can authorize a confirmed fixture; a future disabled-account state requires its owning lifecycle design. M006 maps no language operation to that policy: its acceptance proves the reusable current-state guard and the complete absence of provider/transform routes, not a fictional LLM endpoint.

### 3.4 Durable serving and contract boundaries

Configure Data Protection with the fixed application name `LinguaDesk` and an explicit absolute `Security:DataProtectionKeysPath` (environment form `Security__DataProtectionKeysPath`), defaulting to `/var/lib/linguadesk/keys`. The directory must already exist, be outside static/publish output and be usable by the process; runtime may create key files inside it but must not create an absent production directory. Key values, confirmation material and paths are never exposed through API responses or routine logs.

Account-serving startup validates before listening that the explicitly configured database exists, opens in runtime read/write mode, has no pending migrations and can enter/roll back a bounded write transaction, and that the key directory is usable. It never migrates, repairs, deletes or falls back automatically. `GET /health/live` remains process-only. Add an OpenAPI-excluded `GET /health/ready` that reports only healthy/unhealthy and checks current database/key availability without account data. Unknown health paths still return 404.

The contract-generation path registers the real account route/DTO metadata but bypasses runtime readiness and resolves no DbContext, Data Protection key write or email adapter. Generation remains non-listening and side-effect-free. The generated document advances its pre-release artifact label to `0.1.0-m006`, contains only implemented capabilities plus registration paths and makes no P-006 compatibility promise. Generated TypeScript declarations compile but remain unused by the UI.

## 4. Selected acceptance

All IDs are local to package 006.

| ID | Story | Given / when | Observable outcome |
| --- | --- | --- | --- |
| AC-001 | US-001 | An independent client posts a unique valid `example.test` email and 15–128-scalar password to the migrated host | One request returns generic `202` verification-required JSON with `no-store`; exactly one durable Identity account exists unconfirmed, password verification succeeds through Identity, one confirmation delivery intent is captured, and no cookie/token/account ID/password/email is returned |
| AC-002 | US-002 | Required fields, email syntax/whitespace/length, password Unicode/14/129 boundaries, JSON shape, encoding or content type is invalid | The documented `400`/`415` Problem Details field/category response occurs, no account/delivery is created, values/secrets are absent from response/log/report, and exactly 15/128-scalar space/Unicode passphrases are accepted without trim/normalization/composition requirements |
| AC-003 | US-002 | The same normalized email is submitted sequentially, with case variants, and concurrently | Every policy-valid caller observes the same `202` shape/headers; the database has one account and registration produces one delivery intent. Uniqueness/transaction failures do not leak framework exception, email membership or a `500` |
| AC-004 | US-003 | The created account is inspected after a new host/context opens the same migrated file and the verified policy is evaluated | The account remains `EmailConfirmed = false`, has no issued auth credential, survives restart, and is denied by current durable account state; a separately confirmed fixture is allowed. No language/provider route or call exists |
| AC-005 | US-001/US-003 | The capturing adapter succeeds or a controlled adapter throws after account creation | Success requests delivery once. Controlled delivery failure leaves one unverified account, returns the same generic acknowledgment, logs only safe category/correlation, triggers no background retry and remains recoverable only through later M007 behavior |
| AC-006 | US-003 | Account-serving startup/readiness uses migrated/missing/stale/locked/unwritable stores and valid/absent/unusable key directories | Valid configuration starts and readiness is healthy; invalid configuration fails before listening or reports unhealthy after loss, with nonzero process/check status and no auto-create/migrate/delete/fallback. Liveness remains process-only; paths/account/key data are not disclosed |
| AC-007 | all | OpenAPI/types generate from the actual route metadata and routing/generation boundaries are challenged | Deterministic `0.1.0-m006` artifacts contain only capabilities plus registration with exact operation/security/request/202/400/415/503/no-store semantics; types compile, drift fails, contract generation performs no database/key/email/listener effect, wrong methods are 405 and unknown API/health/static routes retain their boundaries |
| AC-008 | all | The implementation is reviewed and closed | Identity migration/model drift, focused registration/policy/readiness/secret-sentinel tests and backend/contract/frontend/published regressions pass with positive test guards. README/#3/#5–#7/backlog/tasks record actual revisions/evidence and leave M007–M014, M034, Q-004 and release gates pending |

## 5. Exclusions, verification boundaries and human steps

M006 implements only registration, durable unverified state, its reusable verification guard and indispensable storage/key readiness. No sign-in/auth scheme, bearer/cookie/refresh credential, antiforgery bootstrap, verification/resend/status/reset operation, frontend account form, real email delivery, language/provider call, usage/accounting, deletion/backup behavior or release evidence is included. Google remains DF-007. P-005/NFR-008 and P-006 remain proposed.

Use V-004 for real Identity registration/policy state and non-enumerating HTTP behavior; V-009 for the independent API and generated schema/type portions; V-016 for the new migration/model/startup/readiness portions; V-015 for password/token/email/key/log sentinels; and V-012/V-003 only as retained publish/shell regressions because the UI is unchanged. API-AC-003 is covered only for durable unverified-state denial, not a completed verification journey. RG-005 and FR-001/002 remain partial/pending.

**Human actions: none required at beginning or end.** The executor owns clean-baseline, toolchain/package/audit, local database/key directory and contract-command preflight using existing authorization. Tests use synthetic accounts plus injected capture/failure delivery and need no real mailbox/link click, email credential, certificate, production volume, provider budget or device review. Live email/link evidence remains later and cannot be claimed from the fake.

## 6. Readiness and remaining blockers

The accepted PRD already requires local email/password registration and an unverified LLM gate. The shared-owner updates resolve the necessary password/email, duplicate, response/delivery, Identity schema, Data Protection and readiness details without changing product scope. The selected policy is explicit rather than inferred from framework defaults or #6's former nonbinding fixture note.

Q-006 is resolved only for this operation. Q-004 deletion/backup retention and further revocation guarantees block their later features/launch, not creation; the absence of an M006 deletion promise is explicit. P-005/P-006 remain unaccepted. M007/M008/M009/M011/M034 own the excluded verification, auth, UI and live-email work. Requirements, implementation and consistency reviews pass: every story and acceptance row is bounded and every applicable failure/privacy/migration/generation boundary has passing evidence in [tasks.md](tasks.md#3-completion-record). No M006 blocker remains.
