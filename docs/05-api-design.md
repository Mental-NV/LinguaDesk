# LinguaDesk — API Behavioral Design

**Document:** #5 · **Version:** 1.4 · **Status:** Shared design; M005 verified; M006 registration selected and pending
**Updated:** 2026-09-09

## 1. Authority, sources, and artifact lifecycle

| Input | Revision read | Authority |
| --- | --- | --- |
| [Planning workflow](00-SDD-Planning-Workflow.md) | v1.2 at `24ffe3b60ee5dc5918aa17579ed73740e25dafc5`; amended to v1.3 first in this change | Design ownership, selection and generation gates |
| [PRD](01-PRD.md) | v0.4 at the same commit | Product scope, limits, outcomes, proposal status and release gates |
| [UX specification](02-ux-specification.md) | v1.3 at the same commit | Explicit submission, workspace lifetime, status recovery and messages |
| [Architecture](03-architecture.md) | v1.5 at the same commit | Identity, component boundaries, durable reservations, data lifecycle and testing |
| [LLM specification](04-llm-specification.md) | v1.1 at the same commit | Eligibility, validated output, attempts, provider failures and monetary exposure |
| [Architecture decisions](09-architecture-decisions.md) | v1.5 at the same commit | ADR-004 generation and review; ADR-008 accounting; ADR-012 independent AI infrastructure |
| User approval | 2026-09-08 | Adopt behavioral API design now; generate OpenAPI at the start of selected API implementation slices |

This document owns shared observable API behavior under Q-003/Q-006, coordinated with #3 for identity, accounting and privacy. The technical choices below use #0's delegated design authority. They do not accept P-005/NFR-008, P-006, a new product feature or a provider privacy requirement. Product values remain canonical in PRD Sections 5–8; configuration exposes those values rather than creating independent defaults here.

M001–M004 established the backend host, published shell, durable-storage boundary and independent AI boundary. [M005 / package 005](../specs/005-shared-input-capability-contract/spec.md) has now implemented and verified the first product API slice: its exact public capability operation/fields, shared scalar fixtures and slice-local artifact mechanics are represented by actual C# metadata, generated [`docs/05-openapi.yaml`](05-openapi.yaml) and generated TypeScript declarations. [Verification plan #6](06-verification-plan.md) supplies shared verification methods and coverage. The YAML remains a generated artifact, not a hand-edited source. This document supplies cross-operation rules and behavioral examples, not endpoint signatures or a complete DTO catalog. #6 owns the requirement-to-test/evidence matrix. M005 evidence does not make any language-operation, auth, usage/status, accounting or release behavior complete.

For each selected API slice:

1. Its delivery `spec.md` identifies operations, inputs, outputs, applicable shared decisions and acceptance scenarios. Resolve slice-blocking questions before handlers are written.
2. At the start of implementation, add actual typed C# contracts and endpoint metadata/descriptions; generate OpenAPI and review it against the design and selected specification. Do not create a separate fake contract host or copy of the DTOs.
3. Generate dependent client types from that reviewed artifact. Implement handlers against the reviewed shape, then test behavior and schema/client drift independently.
4. Commit the generated artifacts with the slice. Keep unfinished handler status explicit and prevent contract scaffolding from shipping as working functionality. Add other MVP operations when selected; document numbering does not require an all-MVP schema first.

Document #0 owns this lifecycle. C# owns editable wire structure; generated OpenAPI is the machine-readable contract view. A generated change cannot redefine a shared decision silently. Paths, methods, operation IDs, JSON names/nullability, exact response/header schemas and client-facing examples are fixed in the selected slice and then represented in C# metadata/OpenAPI. Markdown retains shared semantics and scenario references, avoiding a second schema to maintain.

**M005 selection — 2026-09-09:** Package 005 fixes `GET /api/capabilities`, its public no-auth/no-store behavior, catalog/policy/recovery field names and values, unversioned route, and pre-release `info.version` for this slice. It also selects shared C#/TypeScript scalar fixtures and generated TypeScript declarations. These are delegated slice decisions under the accepted design. They do not accept P-006, add a compatibility promise, select an auth/usage/language-operation wire shape, or make unimplemented operations appear in the generated paths.

**M005 completion — 2026-09-09:** The implemented public route, Core/TypeScript `unicode-scalar-v1` policy and generated OpenAPI/type views passed package AC-001–008. The first publish smoke exposed and the implementation corrected an omitted OpenAPI runtime assembly; the corrected isolated artifact passed. Exact commands and failure/regression evidence are in the [completion record](../specs/005-shared-input-capability-contract/tasks.md#3-completion-record).

**M006 selection — 2026-09-09:** [Package 006](../specs/006-register-local-api-account/spec.md) selects only anonymous `POST /api/accounts/register`, the local email/password policy, generic duplicate behavior, durable unverified Identity state, confirmation-delivery intent, current-state verified-account policy and indispensable database/key readiness. It advances the generated pre-release artifact to `0.1.0-m006` during implementation. Sign-in, cookies/bearer/refresh, antiforgery, confirmation/resend/status/reset, frontend adoption, real email, language admission and deletion remain later slices. P-005/P-006 and Q-004's deletion/backup lifecycle retain their status.

## 2. Capability and transport boundaries

| Capability | Required observable behavior | Principal dependencies |
| --- | --- | --- |
| Local accounts | Register, sign in/out, confirm email, resend confirmation, recover/reset password and read current account/verification status | FR-001/002; UX #2 Section 9; identity design in Section 3 |
| Translation | Submit one complete source and an explicit target; automatic/manual source choice; return complete validated target text | FR-003–011; #4 eligibility/output contracts |
| Rewriting | Submit complete source and exactly one current PRD writing mode; default to Correction only when omitted | FR-012–018; no combined translation-and-rewrite operation |
| Capabilities | Read supported languages/modes, limits, counting policy, operation deadline and retry-identity validity | FR-036; accessible without SPA execution |
| Usage/availability | Read authenticated user usage and shared-service availability, including pending reservations | FR-027/028/036; Section 7 |
| Operation status | Read the outcome of a known submission identity with ownership checks; no provider call or saved-text retrieval | FR-026/035/037; Sections 5–6 |

Capabilities may be public because they contain no account or operational secrets. Account-specific usage and operation status require an authenticated, currently valid account. A verified account is required for paid language admission. Reading account/status information needed to recover or verify access is not itself a paid transformation.

Use same-origin HTTPS under the architecture's `/api` boundary, JSON requests/responses and UTF-8. Text-bearing inputs use request bodies, never URLs. Return one complete result from the original transformation request; no streaming text, background result queue or provisional output becomes editable user output. Normal submission waits for bounded processing and durable settlement. Status reads are a separate recovery capability, not an async job system with persisted text.

Do not expose Google, alternatives, sentence/version identities, comparison, automatic submission, prefix translation, routing administration or file/voice processing. Do not expose all framework-provided account endpoints merely because a helper maps them; select the confirmed account surface. The independent client uses the same validation, identity and allowance rules as the SPA. Backend endpoints never depend on React revisions or client-reported charges.

Unknown API paths return an API 404, never SPA HTML. Requests with incompatible content types, malformed JSON or invalid text encoding fail before provider work. Request-size bounds must accommodate every legal source, JSON escaping and required metadata; the selected transport slice defines and tests finite body/header limits without creating a smaller hidden text limit.

## 3. Authentication, verification, and account lifecycle

### 3.1 Cookie and bearer access

Use ASP.NET Core Identity for local accounts and select its protected opaque bearer-token format for independent clients. The SPA uses a Secure, HttpOnly session cookie with SameSite=Lax, server-side expiry and no persistent “remember me” option. Browser JavaScript does not receive or persist access/refresh tokens. Independent clients receive bearer access and refresh tokens and treat them as opaque secrets; no JWT parsing, OIDC discovery, OAuth delegation or client-registration server is promised. Microsoft documents both Identity modes and that its bearer tokens are proprietary rather than JWTs. [Identity API authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0)

Each sign-in selects one credential mode explicitly. If a request contains a bearer Authorization header, authenticate that scheme only; an invalid token must not fall back to an ambient cookie. Without that header, cookie authentication may apply. Do not combine principals from different accounts. The selected auth slice documents this choice in OpenAPI security requirements and tests the actual handlers.

Cookie-authenticated state changes, including sign-in/sign-out where relevant, require antiforgery validation. Supply an anonymous bootstrap mechanism for the SPA to obtain the request token and renew it after identity changes. SameSite and JSON content type alone are not the antiforgery contract. A bearer-only call that ignores cookies does not require a browser antiforgery token. The precise bootstrap operation/header/cookie names belong to the auth slice. [ASP.NET Core antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0)

### 3.2 Expiry, refresh, and revocation

Initial engineering settings are an 8-hour server cookie lifetime with sliding renewal disabled, 15-minute bearer access lifetime and 7-day refresh lifetime. Pin them explicitly in the auth configuration and disclose expiry information relevant to the client; changing them requires auth/UX regression checks. They are technical settings, not a new product SLA. Browser session-cookie removal on close is browser-controlled; the existing per-tab text teardown applies regardless of whether authentication is restored.

Refresh validates the protected refresh credential, expiry, current account and security stamp before issuing replacement credentials. Clients replace their held pair on success. Do not claim single-use refresh tokens or replay detection from standard Identity behavior; additional token-rotation policy would need its own design. Refresh never replays a language operation. Invalid/expired refresh requires sign-in. Concurrent refresh handling and the exact response fields are verified in the selected auth slice.

Every authenticated API access checks that the account still exists/is enabled and that its credential's security stamp is current. This is a LinguaDesk authorization requirement beyond merely validating the token's signature/expiry; implement and test it for both cookie and bearer schemes. Password reset or administrative account revocation changes the stamp, making previous access/refresh credentials unacceptable to subsequent protected requests. Do not rely on a default cookie validation interval or bearer expiry to meet this check. An already admitted operation may still settle within its existing deadline under #3; revocation does not restore its text or retroactively reverse a successful charge.

Cookie sign-out clears the caller's cookie; the SPA immediately clears workspace text even if server confirmation is lost, as UX #2 requires. Bearer clients end ordinary local use by discarding their credentials; this is not server revocation of a copied token. A per-device session/token-revocation API and account deletion UI are not added here. Q-004's selected account design must define deletion and any further logout/revocation guarantees before claiming them. Account-wide revocation and password-reset invalidation follow the security-stamp rule above.

### 3.3 Verification and recovery

Successful sign-in may authenticate an unverified account for verification/account status; paid operations reject it with the verification category. A valid password must precede disclosure of that account's unverified status. Verification is enforced server-side at language admission, and fresh account status is read after confirmation rather than trusting stale token claims. Do not configure the application such that an unverified user cannot reach the confirmed verification journey.

Registration creates a local account and starts confirmation delivery. Confirmation and reset do not automatically sign the user in or start LLM work. Invalid/expired links return meaningful generic categories. Known/unknown-account forgot-password and resend requests return indistinguishable acknowledgments. An email-service failure may be exposed only without identifying account existence; do not return a delivery failure exclusively for existing accounts. Logs and telemetry redact link tokens and query values.

M006 fixes the shared local password policy at 15–128 well-formed Unicode scalar values inclusive. Spaces and otherwise valid Unicode are allowed; do not trim, normalize or truncate, and require no uppercase/lowercase/digit/symbol composition. Hash the exact accepted string through Identity. This necessary credential contract neither selects a breached-password blocklist/attempt rate nor claims NIST conformance; proposed abuse controls remain P-005 work. Confirmation/reset lifetimes and resend cooldown remain later auth-slice dependencies. The UI uses this actual policy rather than framework sample defaults. API authentication failures return machine-readable 401/403 responses, never login-page redirects; UX controls navigation and workspace clearing.

### 3.4 M006 registration contract

M006 maps only anonymous `POST /api/accounts/register` (`registerLocalAccount`). The request contains exactly required `email` and `password` strings; no API confirmation-password, role, account ID or requested verification state exists. Email is at most 254 Unicode scalar values, has no surrounding whitespace, passes .NET's `EmailAddressAttribute` check and is unique by Identity's invariant uppercase normalized lookup. No DNS/deliverability or plus-address/domain rewriting is implied. Unknown/duplicate JSON properties, invalid encoding/shape/content type, invalid email and password-policy failures create no account or delivery. Field errors expose only `email`/`password`, never submitted values. M006 errors use standard Problem Details members plus extensions `category`, `correlationId` and, for field failures, `errors` mapping each allowed field name to an array of messages. All registration responses declare `Cache-Control: no-store`.

A new email creates one durable standard Identity account with `EmailConfirmed = false`, then requests one confirmation-delivery intent through the injected narrow adapter. Return `202` JSON with only `status = verificationRequired` and `Cache-Control: no-store`; issue no cookie, bearer/refresh credential, account ID, email or confirmation material. Registration never signs in, redirects, confirms or invokes language work.

A sequential, case-variant or concurrent request for an existing normalized email returns the identical `202` status/body/cache/auth shape, creates no second account and requests no second registration delivery. The contract does not guarantee perfect transport timing equivalence, but no intentional response/header/log distinction may enumerate account membership. If delivery throws after a new account commits, keep it unverified, log only a safe category/correlation, perform no automatic retry and retain the same acknowledgment; M007's resend is the recovery path. This staging preserves the existing rule that email failures cannot identify account existence.

The named `VerifiedAccount` policy loads current Identity state and denies missing/deleted or unconfirmed accounts; it does not trust a stale verification claim. Any future disabled-account state needs its owning lifecycle design. M006 tests the selected guard without mapping a placeholder language route. Data Protection/key persistence and database/key startup/readiness are owned by architecture Section 5.1. Real email and confirmation completion remain M034/M007.

## 4. Complete input and canonical counting

### 4.1 Counting decision: `unicode-scalar-v1`

Count Unicode scalar values in the decoded submitted source. Validate well-formed Unicode first; reject malformed UTF-8 and unpaired UTF-16 surrogate escapes rather than substituting replacement characters. Do not count bytes, JSON escape characters, UTF-16 code units or grapheme clusters. A scalar is distinct from a rendered character cluster; .NET's `Rune` represents scalar values. [Microsoft character encoding guide](https://learn.microsoft.com/en-us/dotnet/standard/base-types/character-encoding-introduction)

Preserve the exact decoded source: no trim, NFC/NFKC conversion, case folding or newline normalization before counting, fingerprinting or provider submission. Spaces, tabs and all line terminators count individually; CRLF counts as two scalars and LF as one. A browser generally submits its textarea value with LF; independent clients submitting CRLF therefore submit a different string. The same string always has the same count across clients.

Whitespace-only eligibility uses this fixed set: U+0009–U+000D, U+0020, U+0085, U+00A0, U+1680, U+2000–U+200A, U+2028, U+2029, U+202F, U+205F and U+3000. Empty or entirely such whitespace is rejected with no operation/provider dispatch. All scalars still count when non-whitespace text is present. Zero-width joiners and combining marks are not removed as whitespace; language eligibility may reject text with insufficient linguistic evidence under #4.

| Decoded input, shown with escapes where useful | Count | Boundary demonstrated |
| --- | ---: | --- |
| `A` | 1 | ASCII |
| `é` (U+00E9) | 1 | Precomposed letter |
| `e\u0301` | 2 | Combining mark; no normalization |
| `😀` | 1 | Surrogate pair in UTF-16, one scalar |
| `👩🏽‍🚒` | 4 | Multi-scalar emoji cluster |
| `中文` | 2 | Chinese |
| `a\r\nb` | 4 | CRLF remains two scalars |
| `a\nb` | 3 | LF |
| ` a\t` | 3 | Leading/trailing whitespace retained |
| `\u00A0\t` | 2 | Whitespace-only; count exists but input is rejected |
| Unpaired `\uD800` | — | Invalid Unicode; no replacement/count-based admission |

The transport spelling `"a"` versus `"\u0061"` yields the same decoded source and count. Shared executable C#/TypeScript fixtures must include these cases and each PRD limit at L−1/L/L+1. Implementations must validate before scalar enumeration if their runtime would otherwise replace invalid surrogates. HTML `maxlength` and JavaScript/C# string length are not the server's count contract.

### 4.2 Validation and settings

Reject an oversized whole source before paid detection; do not process a prefix. On a legal successful operation, charge the full original submitted scalar count once, independent of output length or script conversion. Do not cap output at the input-character limit. Its token/transport bounds and quality checks belong to #4.

Both operations default to automatic source detection when source selection is omitted. Translation requires an explicit supported target and rejects a selected equal source/target before paid dispatch. Rewriting defaults to Correction only and accepts exactly one of the PRD's modes; combinations or unknown values fail validation. Manual source selection never bypasses eligibility. Unsupported, uncertain, mixed or source-mismatched classifications and detected same-language translation produce the categories in Section 8, with no transformation/character charge; paid detection exposure remains private accounting metadata.

The API accepts only the source and settings defined by the selected operation. Unknown/duplicate JSON request properties are rejected, avoiding unnoticed misspellings or ignored mode combinations. Missing optional values resolve to documented defaults before payload matching; explicit null is valid only where the generated contract allows it. Client counts, usage, provider settings and workspace revisions cannot override server decisions.

## 5. Submission identity, duplicates, and replay

### 5.1 Identity and bounded validity

One explicit language submission uses one client-generated UUIDv7 as its submission/operation identity, unique within the account across both families. It is created at activation and reused for transport retries/status recovery. A new explicit request after terminal failure or a source/settings change gets a new identity. Use cryptographic randomness and the RFC version/variant layout; UUID time ordering is not an authorization mechanism. [UUIDv7 specification](https://www.rfc-editor.org/rfc/rfc9562.html#section-5.7)

The initial retry/status validity window is **24 hours after the UUID's embedded timestamp**, with at most **5 minutes of future clock skew** accepted. These are technical recovery/metadata bounds. The server publishes the window and its current time; clients with a bad clock correct it before creating a new submission. Server time controls admission, deadlines and ledger periods; the client timestamp never chooses a quota day or proves ownership.

At or after identity expiry, reject submission/replay as expired even if its database record has been removed. This prevents a forgotten retry identity from silently becoming a new paid operation. Apply the same age validation before first dispatch and within atomic admission after waits. An admitted operation finishing near expiry still settles under its original deadline; expiry never resets that deadline or reverses its charge. Status availability ends at the documented identity expiry.

Identity/fingerprint/terminal recovery metadata is retained through that window and at least until in-flight ownership is fenced. Remove the payload fingerprint afterward; retain only required accounting/reconciliation metadata under #3/Q-004. Fingerprint keys must remain available for their full matching window. Detailed backup, aggregate and unresolved-cost retention remains Q-004; this bounded replay decision does not authorize source/result persistence.

### 5.2 Payload matching and atomic admission

Authenticate before revealing account-scoped operation metadata. A unique `(account, submission identity)` record claims the operation in the same short transaction as character/cost admission. Resolve existing identity state before reapplying current allowance/configuration admission: a previously successful operation must not become a quota error because today's allowance is exhausted.

Payload matching covers family, exact decoded full source and effective source/target/mode settings, with the contract/counting policy revision. JSON property order and equivalent escapes do not change identity; a CRLF/LF change or another writing mode does. Use a versioned server-keyed fingerprint of canonical serialization, not stored source or an unkeyed content hash. Exclude transport credentials, trace IDs and UI revisions. A code/config deployment does not rebind an existing operation to a new prompt/model or recompute its source count. Retain the matching/default-resolution policy needed for unexpired identities, so deployment cannot reinterpret an omitted mode or invalidate an otherwise identical replay merely by using new defaults.

| Existing identity state | Same effective payload | Different effective payload |
| --- | --- | --- |
| No record, identity valid | Normal validation/admission may claim it once | No earlier payload is known; normal admission applies |
| Pending | Return pending metadata; do not enqueue, wait for a second execution or dispatch a provider | Identity conflict; no execution |
| Succeeded | Return original operation outcome/charge metadata and fresh usage; output is unavailable for replay | Identity conflict; no execution |
| Failed/interrupted, terminal | Return recorded failure/zero character charge and fresh usage; do not retry internally | Identity conflict; no execution |
| Expired identity | Expired recovery category; no execution | Expired recovery category; no execution |

Pre-admission malformed/empty/oversized or rejected requests need not leave an operation record. Their definitive response states that no operation was admitted. Missing metadata after a transport failure is weaker evidence and follows Section 6. Identity headers/fields and the precise status-operation URL are fixed once in the first operation slice's generated contract.

Fallback provider calls reuse the same logical operation identity and reservation; each call still has separate monetary admission under #4. Database uniqueness and conditional transitions establish at-most-one character settlement. They cannot guarantee exactly-once provider execution or recover lost text. Disable automatic paid-POST retry/hedging outside the documented coordinator. A controlled retransmission of an ambiguous HTTP request uses the same identity and immutable payload within its validity window; it never allocates a new identity silently.

## 6. Completion, cancellation, and lost-response recovery

### 6.1 Outcomes and finality

| Observable state | Meaning | Character charge and recovery |
| --- | --- | --- |
| Pending | Admitted work has not reached a durable terminal state | Reserved capacity is not committed usage; status read only |
| Succeeded, output delivered | Valid complete output and success charge committed before the original response | Exactly one full-source charge; UI may discard stale text independently |
| Succeeded, output unavailable | Success committed, but this is a replay/status read or transient output was lost | Same original charge; never call the model to reconstruct output |
| Failed | Durable validation/provider/deadline failure fenced against success | Zero character charge; new explicit submission is permitted |
| Interrupted, terminal | Recovery fenced abandoned execution and established no committed success | Zero character charge; potentially spent provider exposure may remain |
| Unknown/not found | No visible record, unavailable status storage or unresolved delivery/ownership | Do not assert success or zero charge and do not silently create a new operation |
| Expired recovery window | Status guarantee has ended | Expiry proves neither failure nor zero charge; identity cannot dispatch again |

`Unknown/not found` is not a new successful/failed ledger state. A status lookup returning no record cannot rule out an original request still arriving or admission committing concurrently. Status reads do not create claims, refresh replay lifetime, dispatch providers or extend deadlines. Clients keep the original identity for safe transport recovery; the official UI's Check status performs only a read. No operation-history list or source/result retrieval endpoint is implied.

The original synchronous success response includes complete validated text, operation outcome/charged count and its accounting period, plus an authoritative usage snapshot when available. A later status/replay returns metadata only, even if transient output might still be in memory. This deliberate rule avoids a second result-cache lifecycle. It reports `succeeded` and output unavailability distinctly from processing failure so the UI can disclose the confirmed charge. A new generation requires an explicit new operation, never an automatic “recover output” call.

Failure to refresh current usage after durable success does not convert the operation into failure: preserve successful text and its committed charge, mark the usage snapshot unavailable and allow a usage-only read. Conversely, provider output before durable success commit must not be returned as successful text. Uncertain persistence/delivery returns unknown recovery guidance, not a zero-charge assertion.

### 6.2 Deadlines and cancellation

The PRD overall deadline starts at server receipt of the language submission and includes admission, eligibility, transformation, validation and settlement. Record it on admitted operations and carry it unchanged through #4. Do not reset it for duplicate HTTP delivery, provider fallback, midnight or status reads. Monotonic elapsed-time enforcement and conditional finalization fence late successes; the stored UTC deadline supports restart recovery. Network delivery can still fail independently, leaving the client with an unknown outcome.

The MVP has **no public operation-cancellation endpoint**. Aborting an HTTP connection stops the client's wait, not a guaranteed paid-call cancellation or charge rollback. The server may finish and commit success within its existing deadline. Internal timeout/shutdown/recovery cancellation follows #3/#4 and is fenced before releasing reservations. A client must never infer a zero charge from a cancelled fetch.

Source/settings/manual-result edits and workspace teardown affect text application only. An older success can charge and update usage without changing current text. The frontend matches its own captured revisions; the server does not maintain editor/sentence state. Auth revocation blocks later requests but does not turn an already admitted success into a failed operation.

**Examples:** If the primary fails and the fallback succeeds, the original operation charges once. If success commits and the connection breaks, Check status reports success/output unavailable with that charge. If the process stops before success commits, recovery fences it and records interrupted with no character charge; another request uses a new identity only after that terminal outcome is known. If storage/status is unavailable, preserve the workspace and show unknown outcome rather than retrying automatically.

## 7. Allowances, UTC boundaries, and usage ordering

### 7.1 Daily character ledger

Admission atomically compares committed usage plus active reservations plus the full submitted count with both the user and global PRD limits. Assign the operation to the **UTC day in which its first admission transaction succeeds**. Persist that day with its reservation and charge. The server's transaction-time clock decides the day; client UUID time and completion time do not.

A success after midnight settles against the original admission day. Releasing a failure restores that day's reserved capacity. A new operation admitted after midnight uses the new day. Midnight does not delete old reservations or move an old charge into today's counter. The global reservation uses the same day as the user reservation. Duplicate/status requests reuse the original operation period, while their fresh usage snapshot describes the current day.

Example: an operation admitted just before UTC midnight completes just afterward. Its confirmed count belongs to yesterday; the response also carries today's usage snapshot. The UI can disclose an earlier-period charge without incrementing today's consumed count. Concurrent duplicate requests produce one charge; concurrent different operations include each other's reservations when testing remaining capacity.

### 7.2 Monetary admission and month boundaries

Monetary accounting stays private and is independent of the character day/charge. Attribute each provider attempt's exposure to the **UTC month of its own cost-admission transaction**; a fallback admitted after month rollover belongs to the new month even if the logical operation started earlier. Freeze the applicable verified tariff/bound with each reservation, as #4 requires.

Month rollover opens a new known-spend bucket but never forgets unresolved exposure. Conservatively include outstanding exposure from earlier months in new-month admission until authoritative settlement permits its release or attribution. Once settled, do not count the same cost as both known spend and unresolved exposure. This is an application admission/accrual policy; it does not assert that a provider invoice uses the same timezone, month or recognition convention. Q-001 must verify billing bounds and any required attribution mapping before paid serving. Unknown bounds remain ineligible.

Provider cancellation, classification rejection, invalid output and timeouts may cost money without a character charge. A budget denial stops further dispatch, including fallback; status and usage reads remain available. A midnight/month reset can restore availability but never automatically submits text. No numeric monetary cap is invented here.

### 7.3 Usage snapshots

Every authoritative snapshot identifies the account context, UTC day, next UTC reset, consumed characters, currently reserved characters, allowance, available characters and service availability. Available capacity is the nonnegative allowance minus consumed and reserved. Reservations can fall on failure, so available capacity can legitimately rise without a reset. Current user usage may be shown; global consumed/reserved counts, monetary amounts and provider routing remain private.

Include a monotonically increasing **durable snapshot revision** alongside the period. For the single-instance SQLite design, use a database-backed revision advanced by transactions changing user/global admission, settlement or service availability. Read revision, ledger values and availability consistently. A process restart does not reset it. Since global availability affects everyone, a per-user charge counter alone is not sufficient. API serialization of potentially large revisions must preserve exact ordering in JavaScript; the selected schema may encode them as decimal strings.

Within an account, compare snapshots by UTC day first and revision second. Ignore an older day or a lower same-day revision; accept a newer day even when its consumed count is smaller. When the order is missing/inconsistent, request fresh usage instead of guessing. Never compare snapshots across accounts or apply them after the workspace/account context is invalidated. A cached replay envelope must not carry an old “current” usage snapshot; obtain a fresh snapshot or mark it unavailable.

Operation-specific charged count/day is distinct from current usage. It explains stale successes and cross-midnight settlement but must not be added locally to the server counter. Reads do not charge allowances. Exact field names, readiness categories and optionality are wire-contract work for the selected slice.

## 8. Error semantics and client recovery

Use RFC 9457 Problem Details for unsuccessful HTTP responses. Provide a stable machine-readable application category, correlation, relevant field/limit details and operation identity/outcome only when known and authorized. The HTTP status and problem status agree. Do not leak provider errors, stack traces, credentials or submitted text in details. RFC 9457 supplies the envelope, not LinguaDesk's retry/accounting meaning. [Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc9457.html)

The categories below are semantic names, not final JSON enum spellings. Fix wire names once in C#/OpenAPI. Success/pending operation-status representations are distinct from Problem Details, and a successful status read can describe a failed operation.

| Category / typical HTTP result | Required distinction and recovery | UX owner |
| --- | --- | --- |
| Invalid request / 400; unsupported media type / 415 | Malformed JSON, unknown/duplicate fields, invalid Unicode or selectors; correct request, no provider dispatch | Field validation |
| Authentication / 401 | Missing/expired/revoked credentials; authenticate again, preserve retry identity for any earlier unknown operation | UX #2 session/access rules |
| Verification / 403 | Authenticated account cannot yet process; verify and explicitly submit | UX-MSG-019 |
| Antiforgery / 403 | Invalid/missing cookie request token; renew bootstrap, never imply language failure | Auth slice with #2 |
| Input eligibility / 422 | Empty, oversize, unsupported/mixed/uncertain/mismatched or same-language source; machine-readable reason and applicable counts/limits; no character success | UX-MSG-006/007/008/011/045 |
| Identity conflict / 409 | Known identity used with a different payload; no dispatch or charge change | Selected recovery UI |
| Identity expired / 410 | Replay/status window ended; no replay dispatch; no claim about historic charge | Selected recovery UI |
| Invalid identity time / 400 | Malformed identity or clock too far ahead; disclose server time, correct before a new submission | Selected client contract |
| Pending submission / 202 | Duplicate of admitted pending work; return status reference, no second dispatch | Existing busy/status behavior |
| User or global allowance / 429 | Insufficient reservable capacity; distinguish user/global, provide reliable reset guidance; do not expose global counts | UX-MSG-015/016 and insufficient-remaining copy |
| Monetary suspension / 503 | No further paid dispatch; distinguish from provider outage; no invented resume time | UX-MSG-017 |
| Processing unavailable/failure / 503 | Classified provider/refusal/invalid-output exhaustion; definitive failure is zero charge | UX-MSG-013 |
| Deadline / 504 | Server-established terminal deadline failure; zero character charge | UX-MSG-014 |
| Internal/status unavailable / 500 or 503 | Outcome may be unknown; only state zero charge when durably established | Check status / unknown-outcome copy |
| Not found / 404 | Unknown API route/resource or no account-owned operation record; never reveal another account's operation | Account-safe recovery |

Distinguish application allowance 429 from a provider's throttling code: provider errors enter #4's fallback policy and do not expose the provider's HTTP status directly. Transport byte-size rejection may use 413, while legal JSON with an over-limit source uses the input-eligibility category. A budget failure is not a language validation failure. Language source mismatch uses source-selection guidance without silently overriding the user's choice.

GET status/usage reads must not generate work. A response may provide `Retry-After` only when meaningful, but no header authorizes automatic creation of a new language operation. A gateway-generated timeout or dropped connection is not the same as the application's confirmed deadline Problem Details; clients lacking authoritative operation outcome use Check status. These choices apply HTTP semantics to the product's stronger operation-identity rules. [HTTP semantics](https://www.rfc-editor.org/rfc/rfc9110.html)

Authentication and structural parsing precede provider work. Existing-operation recovery precedes fresh quota admission. Locally knowable input errors precede a new reservation; paid eligibility runs only after admission under #4. If multiple admission limits fail, return a consistent category order selected by the implementation slice and still disclose only permitted user information. No error path overwrites source/previous output, or calls an LLM to explain its failure.

## 9. Privacy, compatibility, and contract generation

Source/result exist only during bounded processing and original delivery. All text-bearing and auth/status/usage responses use `Cache-Control: no-store`; exclude them from server response/output-cache middleware and service-worker caches. Production logs/traces are metadata-only. Store no text for operation replay, exceptions or generated examples. Provider-managed cache behavior from #4 does not change these application rules.

Client workspace revisions and operation identities may live in active memory, with no text restoration after the teardown boundaries in #2. UUID timestamps reveal approximate submission timing, so identifiers are operational metadata and still require ownership checks. Server-keyed payload fingerprints are private bounded matching data, not analytics identifiers. Public status access is by a known account-scoped operation identity, not a history browser.

Retain automatic OpenAPI generation per ADR-004. During the selected implementation slice, pin OpenAPI 3.1, native ASP.NET Core generation, deterministic YAML serialization and client generation as #3 specifies. Generation must run without migrations, live database/provider/email access, production secrets or frontend startup. ASP.NET Core's build-time generator invokes the application entry point with a mock server; isolate startup effects while preserving the real route/contract registrations. [ASP.NET Core generation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0)

Generation parity proves reproducibility of the artifact, not correctness of handlers. Review semantic schema diffs and test documented validation, status codes, ownership, auth/antiforgery, accounting and recovery. A test that merely reproduces current handler behavior does not discharge this design. Unexpected code-plus-schema changes must be compared with the selected acceptance and shared rules, even when generation is clean.

P-006 remains proposed: this document does not introduce published-version compatibility, deprecation periods or external client stability guarantees. Schema `info.version`, OpenAPI format version and a URL API-version prefix are separate decisions. Use a truthful artifact revision when generation begins; the selected contract slice fixes naming/version mechanics without assuming a product support promise. Revisit schema-first server generation only if future independently developed consumers or external contract obligations justify changing ADR-004.

## 10. Acceptance scenarios and readiness

These shared scenarios guide selected packages; [#6 Section 8](06-verification-plan.md#8-canonical-coverage-and-acceptance-allocation) owns the canonical coverage matrix and check allocation. They are not an all-MVP implementation task list. Only the M005 portions of API-AC-001 and API-AC-014 have passing runtime evidence; all other portions remain pending until selected.

| ID | Observable outcome | Upstream |
| --- | --- | --- |
| API-AC-001 | The scalar fixtures and L−1/L/L+1 agree in C#/TypeScript; invalid Unicode and oversize input never reach a provider | FR-007/024 |
| API-AC-002 | Both auth modes access the same API; invalid bearer cannot fall back to cookie; cookie mutations require antiforgery | FR-001/036, #3 |
| API-AC-003 | Unverified account can complete its verification journey but cannot invoke an LLM; reset/revocation invalidates subsequent protected access/refresh | FR-002, Q-004/Q-006 |
| API-AC-004 | Concurrent identical identity/payload claims dispatch one logical operation; changed payload conflicts; no extra charge | FR-026/027 |
| API-AC-005 | Equivalent JSON escapes match; changed newline/mode does not; expired UUID cannot re-dispatch after recovery metadata cleanup | FR-026/035, Q-003 |
| API-AC-006 | Success lost in transport is recovered as success/output unavailable with original charge; no result storage/regeneration | FR-026/028/037, NFR-004 |
| API-AC-007 | Pre-commit crash is fenced as interrupted with zero character charge; unknown/missing status never falsely asserts zero | NFR-003, RG-004 |
| API-AC-008 | Disconnect/aborted fetch may still settle success; late output cannot resurrect terminal failure; status reads never call the model | FR-026, NFR-002/003 |
| API-AC-009 | Cross-midnight success charges the original day; current-day usage and stale-charge disclosure remain correct | FR-024/027/028 |
| API-AC-010 | Cross-month fallback has its own monetary period; unresolved old exposure survives rollover; no double counting on settlement | NFR-006 |
| API-AC-011 | Snapshot ordering rejects older same-day/global availability states, accepts a new UTC day and survives restart; current usage failure does not erase success | FR-028, NFR-003 |
| API-AC-012 | Registration duplicates and known/unknown reset/resend acknowledgments reveal no account existence through intentional status/body/header distinctions; no error/log contains source, result or secret | UX #2, NFR-004 |
| API-AC-013 | Two independent-client operations work without SPA execution; one complete result, correct mode/language, usage and classified errors | FR-003/035–037, RG-005 |
| API-AC-014 | Generated schema/client match actual contract metadata with no live effects; review/tests catch behavior drift despite clean generation | ADR-004, #0 |

| Owner / question | Resolved here | Remaining decision and blocking stage |
| --- | --- | --- |
| Q-003, #5/#3 | Scalar/whitespace/newline count; identity matching/expiry; interrupted outcomes; daily/monthly attribution and ordered usage. M005 fixes the public count-policy/retry-bound fields and cross-runtime fixtures for its capability slice | Language-operation/accounting wire fields, fingerprint/key cleanup implementation and concurrency evidence before their selected slice completion |
| Q-006, #5/#3 | Cookie plus Identity opaque bearer; auth precedence/lifecycle; recovery/error semantics; no public cancellation endpoint. M005 fixes capabilities/artifacts; M006 fixes registration wire, email/password policy, generic duplicate/delivery intent and unverified-state guard | Confirmation/resend/status/sign-in/token/antiforgery/usage/language/status operations and any compatibility/versioning policy before their selected handlers/clients; P-006 remains proposed |
| Q-004, #3/account design/#10 | Bounded operation recovery/stamp behavior; M006 creates durable account/key records without deletion/retention claims | Account deletion, backup/aggregate/unresolved-exposure retention and any further logout guarantees before those related features/launch; not a blocker to M006 creation |
| Q-001, #4/#3 | Cost-month attribution and conservative unresolved carryover | Serving/billing bounds, attribution verification and actual cap before paid serving |
| Q-005, #6 | Local acceptance scenarios retained here; [#6](06-verification-plan.md) specifies coverage and workloads | Executable release evidence |
| Q-008/Q-010, PRD then #3/#10 | Proposal status preserved | No new abuse rate or compatibility obligation until its owning proposal is accepted |

**M005 implementation review:** The implementation working tree based on planning HEAD `75647db` was checked against the selected fields, count fixtures, generation stages and proposal status. Locked checks passed 47 API/storage, 10 Core, 10 AI, 36 frontend unit and 6 published Chromium cases; deterministic contract regeneration and deliberate drift/CLI/zero-test failures behaved correctly. The generated OpenAPI 3.1 document currently contains only `GET /api/capabilities`, and TypeScript declarations compile. The [package completion record](../specs/005-shared-input-capability-contract/tasks.md#3-completion-record) owns exact commands, artifact hashes, audit and no-effect evidence. M006 artifacts and runtime behavior remain pending.

The shared design and completed M005 package now provide the contract-generation baseline for later selected API slices. Unrelated wire/auth/accounting questions remain explicit and block only their affected handlers/clients. Shared rules continue to preserve whole-source processing, success-only charging, no saved text, simple LLM chains and the PRD's deferred/proposed distinctions; M005 did not implement those later behaviors.
