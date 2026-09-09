# API input, operations, accounting and recovery

Authoritative continuation of [05-api-design.md](../05-api-design.md); section numbers refer to that document; read only when relevant to the selected scope.

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
