# M012 — Selected specification

Selected items: BI-012. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

M012 selects the web-verification portion of FR-002 and the M012 portions of
UX-AC-008, UX-AC-009, UX-AC-010, UX-AC-104, UX-AC-102 and V-003/V-004/V-010/V-015.
M011 is complete and supplies the `/register` form, the in-memory verification
email and the MSG-034 `/verify-email` entry state (without resend or
continuation). M007 is complete and supplies the strict anonymous contracts
this slice adopts unchanged: `confirmLocalAccountEmail` (exactly required
`userId`/`code` strings, `200 { "status": "verified" }` on success, generic
`400 invalidOrExpiredVerification` otherwise, no credential/cookie/redirect)
and `resendLocalAccountVerification` (exactly required `email`, fixed
`202 { "status": "verificationRequested", "retryAfterSeconds": 60 }` for every
syntactically valid address, durable 60-second delivery cooldown). The status
check reads `getLocalAccountSession` only. M012 adds no API operation and
changes no server contract; link consumption is a web convention owned here,
while M034 owns public origin, templates, provider selection and live-mail
evidence.

Link variants consumed on the existing `/verify-email` route: no query
material (status/resend/continuation UI), `?userId=…&code=…` (delivered
confirmation material) and anything else malformed (treated as an invalid
link). Delivered material is posted once as a JSON body, never mutated by
GET, and is stripped from the URL/history immediately after being read.

Explicit exclusions: sign-in/out, safe-return routing and session expiry
(M013), browser recovery forms (M014), confirmation-token lifetime/cooldown
policy, sign-in issuance, language operations, usage/allowance display,
account deletion/backup lifecycle (Q-004), Google/OAuth (deferred DF-007),
live email delivery/templates/origin (M034), short-term abuse limits
(Q-008/P-005) and compatibility promises (P-006). M012 does not complete
full FR-002 or any release gate.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | An unverified visitor holding the M011 in-memory email sees the verification status with that email; protected-route navigation while unverified redirects to `/verify-email` with no editor and no transformation request. | UX-AC-008; UX §3.1/§9; V-003/V-004 |
| AC-002 | One explicit resend sends exactly one `resendLocalAccountVerification` request with only `{email}`; success shows `Verification email sent.`, disables resend for the server-provided `retryAfterSeconds` with a visible countdown, then reenables it; a syntactically invalid email sends no request with a linked field error; no language operation occurs. | UX-AC-009; UX §9; API §3.3; V-003/V-004 |
| AC-003 | Opening `/verify-email?userId=…&code=…` posts exactly one `confirmLocalAccountEmail` request with only `{userId, code}`; success offers explicit continuation (Sign in first when signed out, last protected route when signed in) and starts no language work; the query material is removed from the URL/history on read. | UX-AC-010/104; UX §9; API §3.3; V-003/V-004 |
| AC-004 | Invalid, expired, already-consumed or malformed link material shows `This verification link is invalid or has expired.` with a `Send a new verification email` resend path; `I’ve verified my email` sends exactly one `getLocalAccountSession` read and no transformation, and a still-unverified result stays on the verification page with guidance. | UX-AC-104; UX §9; V-003/V-004 |
| AC-005 | An account-network failure shows `We couldn’t complete this request. Try again.` with no false success; one explicit retry sends one account request and no language operation; a stale completion resolving after navigation cannot redirect, replace the newer route or restore cleared query state; direct `/verify-email` entry with neither in-memory email nor query material routes to `/register`. | UX-AC-102; UX §9; V-003 |
| AC-006 | Keyboard completes the verification path: route heading focus, native controls, focus moved to the status/continuation on state change, visible focus, Enter activates once, and 320px/390px reflow with reduced motion keeps status, resend and continuation controls unclipped and operable. | UX §9; V-010 |
| AC-007 | No user ID, confirmation code/token, email-linked secret, request body or query string appears in `localStorage`/`sessionStorage`, history entries after consumption, application database, logs, traces, test reports or committed fixtures; synthetic `example.test` material only; sentinel scans prove the boundary. | V-015; API §3.3 |
| AC-008 | The generated OpenAPI/TypeScript drift check passes unchanged (no API surface added), aggregate backend/frontend gates and the published Chromium smoke pass, and M002/M006/M007/M011 behavior retains its prior results. | V-009/V-012; #6 §7.1; M007/M011 dependency |

## Constraints and decisions

Use the real generated `confirmLocalAccountEmail`,
`resendLocalAccountVerification` and `getLocalAccountSession` client
declarations and the real published host for behavioral evidence; an
intercepted `/api` mock cannot establish AC-002–005. Confirmation material
travels only in the POST JSON body; the query string is a read-once inbox
that is cleared with a history replacement, never stored or logged.

Resend cooldown authority is the server's `retryAfterSeconds`, not a client
timer constant; the countdown is presentation of that value. The status check
is one session read; it must never submit text, retry confirmation or start
language work. Duplicate resend/confirm activation while one is unsettled
sends nothing further.

Q-004 does not block this slice because M012 creates no account/key record
and makes no deletion, backup-retention or revocation claim; the cooldown
marker stays M007's server-side metadata. Q-006's confirm/resend/session
operation shapes are resolved for this slice by M007 and the reviewed
generated client; sign-in issuance, antiforgery and reset details remain
with M013/M014. Q-009's active verification presentation is specified by
UX §9, §3.1 and the selected messages; deferred surfaces are not MVP
blockers. No human input, live service, branded browser or device is
required; failure to run the deterministic harness is an execution blocker,
not evidence that a gate passed.
