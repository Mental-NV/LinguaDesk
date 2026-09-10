# M014 — Selected specification

Selected items: BI-014. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

M014 selects the web-recovery portion of FR-002 and the M014 portions of
UX-AC-017, UX-AC-018, UX-AC-019, UX-AC-020, UX-AC-101, UX-AC-102,
UX-AC-103 and V-003/V-009/V-010/V-012/V-015. M010 is complete and supplies
the anonymous `requestLocalAccountPasswordReset` (exactly required string
`email`; byte-equivalent `202 passwordResetRequested` for known/unknown
accounts; field-safe `400` for malformed email; `503 availability` when
storage/keys are unavailable) and `resetLocalAccountPassword` (exactly
required `userId`/`code`/`newPassword`; `200 passwordReset` on a valid
triple with stamp rotation, no credential, no cookie, no redirect;
uniform `400 invalidOrExpiredPasswordReset` for bad material; field-safe
`400` for policy-violating passwords; 60-minute token lifetime) plus the
reviewed generated TypeScript declarations, which this slice adopts
without adding any API operation. M013 is complete and supplies the
`/login` form conventions (single submission, linked errors, first-error
focus, cleared passwords, safe return) and the staged `/forgot-password`
placeholder this slice replaces.

The milestone builds the `/forgot-password` form (Email, request action,
Back to sign in) and the `/reset-password` form (New password, Confirm
password, policy checklist, Show password, reset action, Go to sign in),
reads reset link material (`userId`/`code` query pair, mirroring the
M012 link-material convention) and strips it from the URL after reading.
Reset success never signs the user in and restores no workspace text.

Explicit exclusions: live email delivery/templates/origin (M034),
verification flows (M012), sign-in/out/session mechanics (M008/M013
behavior, reused unchanged), Bearer [REDACTED] (M009), password-policy
changes (M006 contract reused as-is), language operations and
usage/allowance display (M026+), workspace editing/pages/text (M028+),
account deletion/backup lifecycle (Q-004), Google/OAuth (deferred
DF-007), short-term abuse limits (Q-008/P-005) and compatibility promises
(P-006). M014 does not complete full FR-002 or any release gate.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | A valid email submits exactly one forgot-password request; fields disable during flight and duplicate click/Enter sends nothing further; the response (known or unknown account) replaces the form with `If an account exists for that email, we sent a reset link.`, heading focus and a `Back to sign in` link; no language work starts. | UX-AC-017; UX §9; V-003 |
| AC-002 | Missing/invalid email sends no request with `Check the highlighted fields.`, linked per-field error, `aria-invalid`/description and first-error focus. | UX-AC-018; UX-AC-101; UX §9; V-003 |
| AC-003 | An account-network failure shows `We couldn't complete this request. Try again.` with no false success; one explicit retry sends one account request and no language operation. A delivery-side failure shows `We couldn't send the email. Try again.` with a `Try again` action and no account disclosure. | UX-AC-102; UX §9; V-003 |
| AC-004 | A valid reset link plus an M006-policy password and matching confirmation submits exactly one reset request; success clears both password values, shows `Password updated. Sign in with your new password.` with heading focus and an explicit `Go to sign in` action; no automatic login occurs, no session is created and no workspace text is restored. | UX-AC-019; UX §9; V-003 |
| AC-005 | A missing, malformed, mismatched, consumed or expired reset link shows `This reset link is invalid or has expired.` with a `Request a new reset link` action; no password fields render and no reset submission is possible. A uniform server rejection of reset material resolves to the same alert without disclosing account, token or input state. | UX-AC-020; UX §9; V-003 |
| AC-006 | Reset password validation mirrors registration: policy checklist, `Password must meet all requirements.` for policy failure, `Passwords do not match.` with confirmation focus, per-field errors with first-error focus, no request until valid, Show password preserves caret/selection, and duplicate Enter/click while unsettled sends nothing further. A field-safe server password rejection surfaces as the policy error identically for known and unknown accounts. | UX-AC-101; UX §9; V-003/V-010 |
| AC-007 | Navigating to recovery while a login is pending cannot be overtaken by the late login completion: it neither redirects, replaces the newer route nor restores cleared passwords; reset query material is stripped from the URL after reading; a defensive `pageshow` reset clears password fields; keyboard (heading focus, native controls, Enter-once, visible focus) and 320px/390px reflow with reduced motion keep both forms operable and unclipped; no password, user ID, code, query string, token or credential material appears in URL remnants, history entries, `localStorage`/`sessionStorage`, application database, logs, traces, test reports or committed fixtures; safe return admits only internal protected paths. Synthetic sentinels prove the boundary. | UX-AC-103; UX §3.1/§3.3/§3.4; V-002/V-010/V-015 |
| AC-008 | The generated OpenAPI/TypeScript drift check passes unchanged (no API surface added), aggregate backend/frontend gates and the published Chromium smoke pass, and M010/M013 behavior retains its prior results. | V-009/V-012; #6 §7.1; M010/M013 dependency |

## Constraints and decisions

Use the real generated `requestLocalAccountPasswordReset` and
`resetLocalAccountPassword` client declarations and the real published
host for behavioral evidence; an intercepted `/api` mock cannot
establish AC-001/AC-003–AC-005. Reset material travels in a JSON body,
never a query string mutation or automatic GET; the query pair is
read-only link input. Client-side email/password checks are presentation
only and must never widen or narrow the M006/M010 server policies; the
password is passed exactly as supplied without trim, normalization or
truncation.

Auth state stays in-memory React state only. Every in-flight completion
is tagged and stale results are discarded. The fixed 60-second
`retryAfterSeconds` acknowledgment reveals nothing; the web layer makes
no cooldown/countdown claim beyond the server shape.

Q-004 does not block this slice because M014 creates no account/key
record and makes no deletion, backup-retention or revocation claim.
Q-006's reset operation shapes are resolved for this slice by M010 and
the reviewed generated client. No human input, live service,
branded browser or device is required; failure to run the deterministic
harness is an execution blocker, not evidence that a gate passed.
