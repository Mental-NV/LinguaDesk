# M011 — Selected specification

Selected items: BI-011. Status: ready; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

M011 selects the web-registration portion of FR-001 and the M011 portions of
UX-AC-006, UX-AC-007, UX-AC-101, UX-AC-102 and V-003/V-010/V-015. M002 is
complete and supplies the signed-out shell, the `/register` route, heading
focus, skip-link and native-link conventions that M011 keeps. M006 is complete
and supplies the strict `POST /api/accounts/register` contract
(`registerLocalAccount`): exactly required `email` and `password` strings, no
confirmation/role/identifier in the payload, the 15–128 Unicode-scalar
password policy, non-enumerating `202 { "status": "verificationRequired" }`
with `Cache-Control: no-store`, and no credential issuance. M011 adopts the
already-generated client shape; it adds no API operation and changes no
server contract.

The milestone replaces the `/register` informational placeholder with the
registration form defined by UX §9: Email, Password, Confirm password, actual
policy checklist, Show password, Create account and Sign in. On success the
visitor reaches the unverified verification route with UX-MSG-034 and the
in-memory email. The email value is kept in memory only for this auth journey;
password values are cleared on every failure and never retained.

Explicit exclusions: verification status/resend/continuation UI (M012),
sign-in/out, safe-return routing and session expiry (M013), browser recovery
forms (M014), confirmation completion, cooldowns, sign-in issuance, language
operations, usage/allowance display, account deletion/backup lifecycle
(Q-004), Google/OAuth (deferred DF-007), live email delivery/templates/origin
(M034), short-term abuse limits (Q-008/P-005) and compatibility promises
(P-006). M011 does not complete full FR-001 or any release gate.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | A valid email, M006-policy password and matching confirmation submits exactly one `registerLocalAccount` request with only `{email, password}`; fields disable during flight, duplicate click/Enter sends nothing further, and success opens the verification route showing `Check your email to verify your account.` with the in-memory email, no credential field, no protected workspace and no language request. | UX-AC-006; UX §9; API §3.4; V-003 |
| AC-002 | Missing/invalid email, policy-invalid password or mismatched confirmation sends no request, shows `Check the highlighted fields.` with linked per-field errors (MSG-038/039/040/041/042), marks fields `aria-invalid` with descriptions, focuses the first invalid field and keeps the live policy checklist truthful, including the 15/128 scalar boundaries and the `Maple!River2026` fixture shape. | UX-AC-007; UX-AC-101; UX §9; V-003/V-010 |
| AC-003 | A server field/policy rejection (including the MSG-043 conflict shape) or a duplicate-submit attempt clears both password values, keeps only the email in memory, focuses the first error, never echoes a password and permits one explicit retry that sends exactly one new account request with no language operation. | UX-AC-101; UX §9; V-003 |
| AC-004 | An account-network failure shows `We couldn’t complete this request. Try again.` with no false success; a stale completion that resolves after the visitor navigates away cannot redirect, replace the newer route or restore cleared passwords. | UX-AC-102; UX §9; V-003 |
| AC-005 | Keyboard completes the whole registration path: route heading focus, skip link, native labeled inputs, visible focus, Enter submits once, Show password preserves caret/selection, and reduced-motion/text-spacing/forced-colors overrides keep errors readable without clipping or animation dependence. | UX-AC-101; UX §9; V-010 |
| AC-006 | No password, confirmation value, request body, query string or credential material appears in URL, history, `localStorage`/`sessionStorage`, application database, logs, traces, test reports or committed fixtures; a reload clears the form and all password state; synthetic sentinel scans prove the boundary. | V-015; UX §9 |
| AC-007 | The generated OpenAPI/TypeScript drift check passes unchanged (no API surface added), aggregate backend/frontend gates and the published Chromium shell smoke pass, and M002 navigation plus M006 registration behavior retain their prior results. | V-009/V-012; #6 §7.1; M002/M006 dependency |

## Constraints and decisions

Use the real generated `registerLocalAccount` client declaration and the real
published host for behavioral evidence; a hand-written provisional request
shape or an intercepted `/api` mock cannot establish AC-001/003/004.
Client-side email/password checks are presentation only and must never widen
the M006 server policy: the authoritative email syntax, 254-scalar limit,
no-whitespace and 15–128 Unicode-scalar password rules stay server-owned, and
the UI must accept everything the server accepts at the boundary it can see.

No response detail, log, trace, filename, report or fixture may contain a
password, email-linked secret, request body or raw Identity error; safe
diagnostics contain only category/correlation/outcome. Registration state is
in-memory React state only; no auth token, account ID or verification
material is stored, cached or placed in the URL.

Q-004 does not block this slice because M011 creates no account/key record
itself and makes no deletion, backup-retention or revocation claim. Q-006's
registration operation/policy details are resolved for this slice by M006;
sign-in, tokens, antiforgery and confirmation/resend remain with their later
packages. Q-009's active registration presentation is specified by UX §9 and
the selected messages; deferred surfaces are not MVP blockers. No human
input, live service, branded browser or device is required; failure to run
the deterministic harness is an execution blocker, not evidence that a gate
passed.
