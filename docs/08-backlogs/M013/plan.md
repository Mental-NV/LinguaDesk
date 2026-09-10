# M013 — Implementation plan

Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Starting from the current planning baseline, replace the M002 `/login`
informational placeholder with the UX §9 sign-in form wired to the
already-generated antiforgery, sign-in, session and sign-out client
shapes against the completed M008 contracts. Add typed `fetch` wrappers
in `frontend/src/api/accounts.ts` mirroring the M011/M012
result-mapping style (success / classified failure / retry, no material
logged), a login feature module under `frontend/src/auth/` with an
in-flight state machine (one unsettled sign-in/sign-out at a time,
disabled controls during flight, tagged completions with
stale-discard), in-memory safe-return memory restricted to internal
protected paths, immediate in-memory teardown on sign-out or observed
`401 authenticationRequired` with MSG-037, a bounded signed-in
placeholder for `/translate` and `/rewrite` carrying the inline Sign out
control, and a staged no-form `/forgot-password` informational state for
M014 to replace. No API, migration, package, auth scheme or persistence
change is planned.

The repository pins .NET SDK 10.0.302, Node 24.20.0 and npm 11.11.0.
Execution begins by checking the locked packet, selection, clean/understood
diff, tool versions and locked restore access; a material source/dependency
drift requires impact review and re-lock rather than an assumed pass.

## Changes and order

1. Preflight: confirm the locked packet, the M008/M012 completion links,
   toolchain pins and the four operations' presence in
   `frontend/src/api/generated/linguadesk-api.d.ts`; run
   `bash scripts/contract.sh check` to prove the adopted shapes are current.
   No generation or handler work is expected.
2. Extend `frontend/src/api/accounts.ts` with antiforgery-bootstrap,
   sign-in (`{email, password}`-only POST with the
   `X-LinguaDesk-Antiforgery` header) and sign-out wrappers; map 200
   signed-in bodies to verified/verificationRequired, the shared 401
   categories to invalidCredentials/authenticationRequired, and
   transport/5xx to retry; no logging of material.
3. Add `frontend/src/auth/LoginPage.tsx` (UX §9 fields, Show password
   with caret preservation, local validation with linked errors and
   first-error focus, single-flight submit, MSG-028/MSG-037/generic
   states, `pageshow` password reset, in-memory-only state) and rework
   `frontend/src/shell/App.tsx` (session bootstrap read, safe-return
   memory with internal-path guard, verified/unverified/signed-out
   guards for `/`, `/login`, `/register`, `/verify-email`,
   `/translate`, `/rewrite`, signed-in placeholder with inline Sign out
   following the §3.4 pending/failure/success states, staged
   `/forgot-password` informational state, existing heading-focus/shell
   conventions kept).
4. Add `frontend/tests/unit/login.test.tsx` with Testing Library:
   one-request verified sign-in with `{email, password}`-only shape and
   safe-return navigation; MSG-028 invalid matrix with password clearing
   and email focus; unverified routing to `/verify-email`; expiry
   teardown with MSG-037 and late-completion discard; sign-out
   pending/failure/retry/success matrix with auth-only retry and no
   Back exposure; local-validation no-request cases with linked errors;
   Show-password caret preservation; Forgot-password-during-pending
   discard; network-failure retry; safe-return fallback for external
   paths; query/storage/history/URL sentinel cases. Extend `App.test.tsx`
   guard assertions.
5. Extend the published smoke surface (`frontend/tests/e2e/`) with a
   Chromium sign-in trip against the owned loopback host: real verified
   sign-in reaches the remembered protected route; real invalid
   credentials return the real shared 401 and show MSG-028; real sign-out
   returns the real 204 with immediate clearing and no Back exposure;
   expired-cookie entry shows MSG-037; reload clears the form; 320px/390px
   geometry. Seeded verified/unverified `example.test` accounts use only
   the existing deterministic registration/confirmation support with no
   new test endpoint; otherwise record the gap and carry the affected AC
   on component evidence plus the real-API invalid-credential path.
6. Run focused and aggregate checks, published smoke and dependency audits.
   Inspect rendered output, processes, logs and the diff for echoed
   emails, passwords, tokens, new routes or side effects.
7. Reconcile actual behavior/evidence against BI-013 and every AC. Update
   only affected UX/verification/README/current-delivery/coverage owners if
   implementation changes their current truth; keep package
   checkboxes/evidence in `tasks.md`. Do not claim M014, full FR-001/FR-038,
   live email or release readiness.

If implementation proves a server-contract, generated-shape or
account-lifecycle change indispensable, stop the affected task, update its
design owner and this package, review and re-lock before proceeding.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| M008/M012/current inputs | T001 | `python3 automation/context.py check M013`; inspect `git diff --name-status` and M008/M012 completion links; `node --version`; `npm --version`; `bash scripts/contract.sh check` | Fresh manifest and preflight note in completion record |
| AC-001 | T002/T003 | `npm --prefix frontend test -- tests/unit/login.test.tsx` sign-in cases: bootstrap plus one `{email, password}`-only POST, safe-return navigation with heading focus, no retained password, zero language requests | Focused result plus single-request assertions |
| AC-002 | T002/T003 | Same focused suite with invalid-credential matrix (MSG-028, cleared password, email focus, no account detail) and unverified cases (route to `/verify-email`, safe return preserved, no language work) | Invalid-matrix plus continuation assertions |
| AC-003 | T002/T003 | Same focused suite with expiry cases: `authenticationRequired` clears in-memory state before login renders with MSG-037; late completion after navigation causes no redirect or restore | Expiry-teardown plus discard assertions |
| AC-004 | T002/T003/T004 | Focused sign-out matrix (immediate clear, `Signing out…`, failure text with auth-only retry, success login form, no Back exposure) plus browser sign-out trip via `bash scripts/frontend.sh smoke` | Component assertions plus browser evidence |
| AC-005 | T002/T003 | Same focused suite with local-validation no-request cases, caret-preservation, pending-login Forgot-password discard and signed-in redirect cases | Validation/ordering assertions |
| AC-006 | T003 | Same focused suite with failing-transport and one-request-retry cases: generic message, no false success, no language operation | Failure-retry assertions |
| AC-007 | T003/T004 | Sentinel cases (password/body/query/token absent from storage/history/URL/DB/logs/reports), safe-return fallback, `pageshow` clearing and keyboard/geometry cases plus Playwright Chromium trip | Sentinel scan result plus browser evidence |
| AC-008 | T005 | `bash scripts/contract.sh check`; `bash scripts/backend.sh check`; `bash scripts/frontend.sh check`; `bash scripts/frontend.sh smoke`; `npm --prefix frontend audit --audit-level=moderate`; `git diff --check`; `python3 automation/context.py audit` | `artifacts/test-results/`, command summaries and final AC dispositions |

## Migration, rollout and rollback

No migration, package, service or configuration change is expected. The
slice is a pure frontend increment served through the existing published
artifact; rolling back restores the M012 informational `/login` state
while the M008 API contracts stand unchanged. No account, key, session
or text data is created by the slice itself beyond the explicit
bootstrap/sign-in/sign-out/session requests the visitor triggers.

## Context boundaries and risks

Recovery forms, Bearer [REDACTED], reset/stamp mutation, language
operations, allowances and provider/quality/performance domains are
referenced only to preserve their later contracts; no reset issuance,
token handling beyond the M008 cookie pair, transformation submission or
usage display is selected. No AI specification or accounting contract
belongs in the execution packet.

Principal risks and controls:

- Double sign-in/sign-out or a slow network can double-submit. Gate on
  one unsettled request per action, disable controls during flight and
  assert exact request counts; never rely on server idempotency beyond
  the M008 contract (idempotent sign-out only).
- The antiforgery pair is bound to the current identity. Bootstrap a
  fresh anonymous pair before sign-in and a fresh post-sign-in pair
  before sign-out; an identity-stale pair must surface the shared
  secret-free 400, never an auth mutation.
- Safe return must never become an open redirect. Admit only the
  in-memory internal protected paths and fall back to `/translate`;
  assert the external/`//`/scheme fallback case.
- A late sign-in/session completion can overwrite newer navigation or
  restore cleared passwords. Tag in-flight work, discard stale
  completions and assert the resolve-after-navigation case.
- Credential material can leak into assertions, logs or fixtures. Keep
  auth state in memory only, strip nothing into the URL in the first
  place, use synthetic `example.test` material, assert sentinel absence
  and inspect reports before committing evidence.
- The adopted generated types can drift from the contracts. Run the
  canonical drift check before adoption and keep the API surface unchanged.
- Real-API browser evidence needs seeded verified/unverified accounts.
  Attempt it only through existing deterministic support with no new
  endpoint; otherwise record the gap and carry the affected AC on
  component evidence plus the real-API invalid-credential browser path.

Human actions: None. Routine restores, owned temporary databases/keys and
deterministic local hosts suffice. Missing locked dependencies or an
unavailable deterministic harness blocks only its affected gate and must be
recorded rather than replaced by a fake pass. Branded-browser, device and
assistive-technology checks remain release scope with explicit gaps, not
milestone blockers.

Readiness review: M008 and M012 are satisfied; the selected form, routing,
safe-return, teardown and message contracts are fixed; every AC maps to
ordered work, a runnable deterministic check and evidence target;
generated-contract review precedes client adoption; no blocking question
or human gate remains; omitted domains have explicit owners.
