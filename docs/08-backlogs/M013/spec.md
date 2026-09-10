# M013 — Selected specification

Selected items: BI-013. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

M013 selects the web sign-in portion of FR-001, the in-memory session
boundary of FR-038, and the M013 portions of UX-AC-013, UX-AC-014,
UX-AC-015, UX-AC-016, UX-AC-101, UX-AC-102, UX-AC-103 and
V-002/V-003/V-010/V-015. M008 is complete and supplies the strict
anonymous `getAccountAntiforgeryToken`, antiforgery-protected
`signInLocalAccount` (exactly required `email`/`password`, `200` signed-in
with `verified`/`verificationRequired` on valid credentials, shared `401
invalidCredentials` otherwise, no cookie on failure), cookie-read
`getLocalAccountSession` (`401 authenticationRequired` on
missing/expired/malformed/deleted/stale credentials, no renewal) and
antiforgery-protected idempotent `signOutLocalAccount` (`204`, caller
cookie cleared, no stamp change). M012 is complete and supplies the
`/verify-email` page, the signed-out/signed-in continuation variants and
the guarded shell entries this slice extends. M013 adds no API operation
and changes no server contract; it adopts the already-generated client
shapes.

The milestone replaces the `/login` informational placeholder with the
sign-in form defined by UX §9 (Email, Password, Show password, Sign in,
Create account, Forgot password), remembers only the safe destination
path for post-login return, exposes inline sign out on the signed-in
surface, and tears down in-memory auth state immediately on sign-out or
observed session expiry.

A verified session on `/translate` or `/rewrite` renders a bounded
signed-in placeholder: heading focus, the inline Sign out control and an
explicit statement that the language workspace arrives with M028/M029.
It contains no editor, submits no operation, shows no usage and makes no
workspace claim; it exists so the safe-return target is a real guarded
route rather than a redirect loop. M028 replaces it with the first real
workspace.

The Forgot password link navigates to a staged `/forgot-password`
informational state with no credential fields and no submission, mirroring
the M002 staged-shell convention; M014 replaces it with the recovery
forms. This keeps UX-AC-103 (leave a pending login for recovery)
observable without claiming M014 behavior.

Explicit exclusions: recovery request/reset forms and logic (M014),
password-reset/stamp mutation (M010), Bearer [REDACTED] (M009), language
operations and usage/allowance display (M026+), workspace
editing/pages/text (M028+), the Start-new-workspace modal, workspace
footer and pagehide workspace-text teardown (M032; full §3.3 restoration
handling arrives with the first real workspace text), account
deletion/backup lifecycle (Q-004), Google/OAuth (deferred DF-007), live
email delivery/templates/origin (M034), short-term abuse limits
(Q-008/P-005) and compatibility promises (P-006). M013 does not complete
full FR-001/FR-038 or any release gate.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | A valid verified email/password submits exactly one `signInLocalAccount` request with only `{email, password}` (after the anonymous antiforgery bootstrap when no current pair exists); fields disable during flight and duplicate click/Enter sends nothing further; success navigates to the remembered safe protected route (default `/translate`) with destination heading focus, no password value retained in the DOM and no language request. | UX-AC-013; UX §9; API §3.6; V-003 |
| AC-002 | Unknown email or wrong password shows `Email or password is incorrect.`, clears the password value, keeps only the email in memory, focuses the email field and reveals nothing account-specific; valid credentials on an unverified account route to `/verify-email` with the safe destination preserved and start no language work. | UX-AC-014; UX §9; API §3.6; V-003 |
| AC-003 | An observed `401 authenticationRequired` session read clears in-memory auth state (pending verification email, passwords, safe-return memory is reset to default) before login renders and shows `Your session expired. Sign in again to continue.`; a late sign-in completion resolving after navigation cannot redirect, replace the newer route or restore cleared passwords. | UX-AC-015; UX §3.4; API §3.2; V-002/V-003 |
| AC-004 | Inline Sign out clears in-memory state immediately and shows `/login` with `Signing out…`; sign-out failure shows `Workspace cleared. Sign-out could not be confirmed. Try again.` with `Try sign-out again`, suppresses authenticated redirect until confirmed, and one explicit retry sends only the sign-out action; success exposes the ordinary login form; browser Back after sign-out cannot expose authenticated content. | UX-AC-016; UX §3.4; API §3.2; V-002/V-003/V-010 |
| AC-005 | Missing/invalid email sends no request with `Check the highlighted fields.`, linked per-field errors, `aria-invalid`/descriptions and first-error focus; Show password preserves caret/selection; navigating to Forgot password while a login is pending cannot be overtaken by the late completion and retains no password; already-signed-in verified visits to `/login` or `/register` redirect to the last protected route unless sign-out confirmation is pending/failed. | UX-AC-101; UX-AC-103; UX §3.1/§9; V-003/V-010 |
| AC-006 | An account-network failure shows `We couldn’t complete this request. Try again.` with no false success; one explicit retry sends one account request and no language operation. | UX-AC-102; UX §9; V-003 |
| AC-007 | No password, request body, query string, token or credential material appears in URL, history entries, `localStorage`/`sessionStorage`, application database, logs, traces, test reports or committed fixtures; safe return admits only internal protected paths (`/translate`, `/rewrite`) and falls back to `/translate` for anything else; reload clears the form and all password state; a defensive `pageshow` reset clears password fields; keyboard (heading focus, native controls, Enter-once, visible focus) and 320px/390px reflow with reduced motion keep the form operable and unclipped; synthetic sentinels prove the boundary. | V-010/V-015; UX §3.3/§3.4 |
| AC-008 | The generated OpenAPI/TypeScript drift check passes unchanged (no API surface added), aggregate backend/frontend gates and the published Chromium smoke pass, and M002/M006/M007/M008/M011/M012 behavior retains its prior results. | V-009/V-012; #6 §7.1; M008/M012 dependency |

## Constraints and decisions

Use the real generated `getAccountAntiforgeryToken`,
`signInLocalAccount`, `getLocalAccountSession` and
`signOutLocalAccount` client declarations and the real published host for
behavioral evidence; an intercepted `/api` mock cannot establish
AC-001–004. The antiforgery token travels only in the
`X-LinguaDesk-Antiforgery` header with its cookie; confirmation-style
query material is never used here. Client-side email checks are
presentation only and must never widen the M006 server policy; the
password is passed exactly as supplied without trim, normalization or
truncation.

Auth state is in-memory React state only: no session, token, account ID,
password or return path is stored, cached or placed in the URL. The
remembered safe destination is an in-memory path, never an external URL.
Duplicate sign-in/sign-out activation while one is unsettled sends
nothing further; every in-flight completion is tagged and stale results
are discarded.

Q-004 does not block this slice because M013 creates no account/key
record and makes no deletion, backup-retention or cross-session
revocation claim; it clears only the caller's in-memory state and the
caller cookie via the M008 contract. Q-006's sign-in/session/sign-out
operation shapes are resolved for this slice by M008 and the reviewed
generated client; Bearer [REDACTED] remain with M009 and reset details
remain with M010.
No human input, live service, branded browser or device is required;
failure to run the deterministic harness is an execution blocker, not
evidence that a gate passed.
