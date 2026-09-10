# M014 — Implementation plan

Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Replace the staged `/forgot-password` placeholder with a real recovery
request form and add `/reset-password`, both wired to the completed M010
operations through new typed wrappers in `frontend/src/api/accounts.ts`
built on the already-generated declarations. Reuse the M013 form
conventions (single-flight guard with tagged completions, linked errors,
first-error focus, cleared secrets, heading focus on outcome, in-memory
state only) and the M012 link-material convention (exactly the
`userId`/`code` query pair, stripped after reading, malformed material
never submitted). Add no API operation, change no server contract, send
no email, create no session, and restore no workspace text.

## Changes and order

1. `frontend/src/api/accounts.ts`: add `requestLocalAccountPasswordReset`
   and `resetLocalAccountPassword` wrappers following the existing
   result-kind pattern (`sent`/`invalid`/`field`/`retry`), mapping HTTP
   shapes to UI-safe outcomes only (no bodies, codes or user IDs leak
   into results). Depends on nothing; done when Vitest unit tests prove
   the status-to-kind mapping with fixed fixtures.
2. `frontend/src/auth/ForgotPasswordPage.tsx` (new): email form per
   AC-001–AC-003; success replaces the form with MSG-031, heading focus
   and `Back to sign in`. Depends on step 1; done when component tests
   prove single request, duplicate guard, identical known/unknown
   confirmation and failure alerts.
3. `frontend/src/auth/ResetPasswordPage.tsx` (new): link classification
   (`none`/`delivered`/`malformed`), policy checklist plus confirmation
   per AC-004–AC-006, MSG-033 invalid state with no fields, success state
   with `Go to sign in` and cleared secrets. Depends on step 1; done when
   component tests prove the link variants, validation/focus behavior and
   no-auto-login.
4. `frontend/src/shell/App.tsx`: replace the staged `ForgotPasswordPage`
   stub with the real page; add the `/reset-password` route; keep safe
   return, expiry teardown and sign-out phases untouched. Depends on steps
   2–3; done when routing tests prove both routes render under the
   guarded shell and the login pending/recovery race (AC-007) holds.
5. Privacy/keyboard/reflow hardening (V-010/V-015): query stripping,
   `pageshow` clearing, sentinel exclusion sweep, 320px/390px and
   reduced-motion checks. Depends on step 4; done when the focused
   browser contracts pass.
6. Drift, regressions and published smoke (V-009/V-012) plus M010/M013
   retention. Depends on step 5; done when the exact commands below pass
   and the completion record names revision, environment and UTC time.

No migration, rollout, rollback, configuration or access change applies:
frontend-only slice against the frozen M010 contract. If the generated
declarations drift, stop: the drift owns the blocker and no provisional
client shape may be hand-written.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001, AC-002, AC-003 | T002 | V-003 component tests: `npm run test --prefix frontend -- run <forgot specs>` (exact spec names fixed at execution); assert one request, duplicate guard, identical confirmation, linked errors, retry semantics | tasks.md completion record |
| AC-004, AC-005, AC-006 | T003 | V-003 component tests: `npm run test --prefix frontend -- run <reset specs>`; assert link variants, policy/confirmation focus, success with explicit continuation, no session creation | tasks.md completion record |
| AC-007 | T004 | V-002/V-010/V-015 focused browser contracts: real Chromium via the repo's published-host harness (`scripts/` entry fixed at execution), sentinel grep over reports/fixtures, 320px/390px reflow, keyboard walkthrough | tasks.md completion record |
| AC-008, M010/M013 retention | T005 | V-009 drift check + aggregate gates + published smoke: repo-defined `scripts/` commands fixed at execution; no new OpenAPI diff | tasks.md completion record |
| Client wrappers | T001 | Vitest unit mapping tests for both wrappers with fixed known/unknown/invalid/retry fixtures | tasks.md completion record |

## Context boundaries and risks

Omitted domains and why: language eligibility/Translation/Rewriting
(M015–M018, no operation is submitted here); allowances, cost and
recovery accounting (M021–M025, recovery forms move no allowance);
workspaces and editing guards (M028–M032, no editor exists yet);
evaluation corpora, performance workloads and visual baselines
(M035–M039, only the M014 geometry/keyboard subset applies); live email
delivery (M034, only synthetic link fixtures are used); account
deletion/backup retention (Q-004, no such claim); abuse limits and
compatibility (P-005/P-006, still proposed). On-demand sources: M010/M013
packages for regression questions only; backend recovery endpoints only
if a contract doubt arises (none expected — generated client is final).

Risks: query material lingering in history if stripping regresses
(covered by AC-007 sentinels); password-policy drift from M006 (client
checks are presentation only; server owns the verdict); mistaking the
fixed acknowledgment for delivery proof (no delivery claim is made).

Human steps: None required. Nonblocking end verification (branded
browsers, devices, AT, live email) stays release scope with no gate
claimed here. If a deterministic-harness dependency is unrestorable,
report it as the blocker without claiming its gate passed.
