# M011 — Implementation plan

Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Starting from the current planning baseline, replace the `/register`
informational placeholder in the completed M002 shell with the UX §9
registration form, wired to the already-generated `registerLocalAccount`
client shape against the completed M006 contract. Keep the existing route
table, heading-focus/skip-link conventions and signed-out shell; add explicit
submission guards (one unsettled registration suppresses duplicate UI
activation), a live 15–128 Unicode-scalar policy checklist reusing the
shared scalar-counting approach, Show password with caret/selection
preservation, linked errors with first-invalid focus, password clearing on
every failure, in-memory-only email retention and MSG-034 verification-entry
state. No API, migration, package, auth scheme or persistence change is
planned.

The repository pins .NET SDK 10.0.302, Node 24.20.0 and npm 11.11.0.
Execution begins by checking the locked packet, selection, clean/understood
diff, tool versions and locked restore access; a material source/dependency
drift requires impact review and re-lock rather than an assumed pass.

## Changes and order

1. Preflight: confirm the locked packet, the M002/M006 completion links,
   toolchain pins and `registerLocalAccount` presence in
   `frontend/src/api/generated/linguadesk-api.d.ts`; run
   `bash scripts/contract.sh check` to prove the adopted shape is current.
   No generation or handler work is expected.
2. Add a registration feature module under `frontend/src/` (form component
   with Email, Password, Confirm password, checklist, Show password, Create
   account and Sign in link; in-memory submission state machine with
   disabled-fields flight, duplicate click/Enter guard and stale-completion
   discard). Wire `/register` and verification-entry routing in
   `frontend/src/shell/App.tsx`, reusing the existing `Page` heading-focus
   and shell conventions. Client validation mirrors only what UX §9 shows
   (required/shape, M006 scalar policy via shared counting, confirmation
   match); every submit sends exactly `{email, password}` through the
   generated type.
3. Add `frontend/tests/unit/` component suites with Testing Library:
   single-request valid submission with disabled-fields/duplicate-Enter
   guard and MSG-034 entry state; no-request local-validation matrix with
   linked errors, `aria-invalid` and first-invalid focus; server-rejection
   password clearing with retained email; network-failure message with
   single explicit retry; Show-password caret preservation; stale-completion
   discard; storage/history/URL sentinel cases. Update the existing
   `App.test.tsx` placeholder assertions for `/register` to the new form
   contract; `/login` placeholders stay until M013.
4. Extend the published smoke surface (`frontend/tests/e2e/`) with a
   Chromium registration trip against the owned loopback host: valid
   registration reaches verification entry with no protected workspace,
   invalid local input sends no `/api` request, and reload clears password
   state. Keep the existing M002 boundary trips intact.
5. Run focused and aggregate checks, published smoke and dependency audits.
   Inspect rendered output, processes, logs and the diff for echoed
   passwords, stored credentials, new routes or side effects.
6. Reconcile actual behavior/evidence against BI-011 and every AC. Update
   only affected UX/verification/README/current-delivery/coverage owners if
   implementation changes their current truth; keep package
   checkboxes/evidence in `tasks.md`. Do not claim M012–M014, full FR-001,
   live email or release readiness.

If implementation proves a server-contract, generated-shape or
account-lifecycle change indispensable, stop the affected task, update its
design owner and this package, review and re-lock before proceeding.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| M002/M006/current inputs | T001 | `python3 automation/context.py check M011`; inspect `git diff --name-status` and M002/M006 completion links; `node --version`; `npm --version`; `bash scripts/contract.sh check` | Fresh manifest and preflight note in completion record |
| AC-001 | T002/T003 | `npm --prefix frontend test -- tests/unit/<registration-suite>` valid-submission cases: one `registerLocalAccount` call with `{email,password}` only, disabled fields, duplicate Enter guard, MSG-034 entry state | Focused result plus single-request assertions |
| AC-002 | T002/T003 | Same focused suite with missing/invalid-email, 14/129-scalar, malformed-Unicode and mismatched-confirmation cases: zero requests, `Check the highlighted fields.`, linked errors, `aria-invalid`, first-invalid focus | Validation-matrix assertions |
| AC-003 | T002/T003 | Same focused suite with field/policy-rejection and duplicate-submit cases: cleared passwords, retained email, first-error focus, one-request retry, no language operation | Rejection assertions |
| AC-004 | T003 | Same focused suite with failing-transport cases and a resolve-after-navigation case: generic retry message, no false success, no redirect or password restore | Failure-ordering assertions |
| AC-005 | T003/T004 | Focused keyboard/focus cases plus Playwright Chromium registration trip via `bash scripts/frontend.sh smoke`: heading focus, skip link, native labels, Enter-once, caret preservation, 320px/390px geometry | Component plus browser evidence |
| AC-006 | T003/T004 | Sentinel cases (password/secret absent from URL/history/storage/DB/logs/reports) plus reload-clears checks in component and browser runs | Sentinel scan result |
| AC-007 | T005 | `bash scripts/contract.sh check`; `bash scripts/backend.sh check`; `bash scripts/frontend.sh check`; `bash scripts/frontend.sh smoke`; `npm --prefix frontend audit --audit-level=moderate`; `git diff --check`; `python3 automation/context.py audit` | `artifacts/test-results/`, command summaries and final AC dispositions |

## Migration, rollout and rollback

No migration, package, service or configuration change is expected. The form
is a pure frontend increment served through the existing published artifact;
rolling back restores the `/register` placeholder while the M006 API
contract stands unchanged. No account, key, session or text data is created
by the form itself beyond the single registration request the visitor
explicitly submits.

## Context boundaries and risks

Verification/resend/continuation, sign-in/out, safe return, expiry teardown
and recovery forms are referenced only to preserve the later M012–M014
contracts; no login/verify/reset routes or session handling are selected.
M034 owns live delivery/templates/origin. LLM, operation/accounting/cost,
provider, quality and performance domains are irrelevant because the form
cannot submit text or call a provider. No AI specification or accounting
contract belongs in the execution packet.

Principal risks and controls:

- Duplicate click/Enter or a slow network can double-submit. Disable fields
  during flight, gate on one unsettled registration and assert exact
  request counts; never rely on server idempotency the M006 contract does
  not promise for distinct valid attempts.
- Client validation can disagree with the M006 policy. Reuse shared scalar
  counting, test 15/128 boundaries plus malformed Unicode, and treat server
  rejection as the authoritative path with password clearing.
- A late success can overwrite newer navigation or restore cleared
  passwords. Tag the in-flight submission, discard stale completions and
  assert the resolve-after-navigation case.
- Passwords can leak into assertions, logs or fixtures. Use synthetic
  `example.test` addresses and policy fixtures, assert sentinel absence and
  inspect reports before committing evidence.
- The adopted generated type can drift from the contract. Run the
  canonical drift check before adoption and keep the API surface unchanged.

Human actions: None. Routine restores, owned temporary databases/keys and
deterministic local hosts suffice. Missing locked dependencies or an
unavailable deterministic harness blocks only its affected gate and must be
recorded rather than replaced by a fake pass. Branded-browser, device and
assistive-technology checks remain release scope with explicit gaps, not
milestone blockers.

Readiness review: M002 and M006 are satisfied; the selected form contract,
messages and error boundaries are fixed; every AC maps to ordered work, a
runnable deterministic check and evidence target; generated-contract review
precedes client adoption; no blocking question or human gate remains;
omitted domains have explicit owners.
