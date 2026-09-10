# M012 — Implementation plan

Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Starting from the current planning baseline, extend the M011
`/verify-email` entry state into the full verification page defined by
UX §9 and the §3.1 route table, wired to the already-generated
`confirmLocalAccountEmail`, `resendLocalAccountVerification` and
`getLocalAccountSession` client shapes against the completed M007
contracts. Add typed `fetch` wrappers in `frontend/src/api/accounts.ts`
mirroring the M011 result-mapping style (success / field-safe invalid /
retry), a verification feature module under `frontend/src/auth/` with an
in-flight state machine (one unsettled resend/confirm/status read at a
time, disabled controls during flight, stale-completion discard), query
consumption on `/verify-email` with immediate history replacement, and
explicit continuation honoring signed-out versus signed-in state. No API,
migration, package, auth scheme or persistence change is planned.

The repository pins .NET SDK 10.0.302, Node 24.20.0 and npm 11.11.0.
Execution begins by checking the locked packet, selection, clean/understood
diff, tool versions and locked restore access; a material source/dependency
drift requires impact review and re-lock rather than an assumed pass.

## Changes and order

1. Preflight: confirm the locked packet, the M007/M011 completion links,
   toolchain pins and the three operations' presence in
   `frontend/src/api/generated/linguadesk-api.d.ts`; run
   `bash scripts/contract.sh check` to prove the adopted shapes are current.
   No generation or handler work is expected.
2. Extend `frontend/src/api/accounts.ts` with resend, confirm and session
   wrappers sending exactly `{email}`, `{userId, code}` and a bodyless GET;
   map 202/200 `status` bodies to success, the generic 400 categories to
   invalid-or-expired, field-safe 400s to field errors, and transport/5xx
   to retry; no logging of material.
3. Rework `frontend/src/auth/VerifyEmailEntryPage.tsx` (status display with
   in-memory email, resend with server-driven countdown and MSG-035,
   `I’ve verified my email` single-read status check, link consumption on
   mount with query stripping, MSG-032 invalid path with resend action,
   explicit continuation variants, generic network-retry state) and
   `frontend/src/shell/App.tsx` (unverified protected-route redirect to
   `/verify-email`, no-material direct entry back to `/register`, existing
   heading-focus/shell conventions kept). Verification state stays in
   in-memory React state only.
4. Add `frontend/tests/unit/verify-email.test.tsx` with Testing Library:
   status/resend rendering; one-request resend with `{email}`-only shape,
   countdown disable/reenable and MSG-035; no-request invalid-email case;
   one-request confirm with `{userId, code}`-only shape and continuation
   variants; MSG-032 invalid/expired/malformed matrix; single session read
   with no transformation; network-failure retry; resolve-after-navigation
   discard; query-stripping and storage/history/URL sentinel cases. Extend
   `App.test.tsx` redirect/no-material assertions.
5. Extend the published smoke surface (`frontend/tests/e2e/`) with a
   Chromium verification trip against the owned loopback host: real
   invalid-link consumption returns the real generic 400 and shows MSG-032;
   real resend returns the real 202 with server cooldown behavior; direct
   no-material entry routes away; reload clears query state; 320px/390px
   geometry. Valid-link success over the real API is attempted through the
   same host only if delivered material can be sourced without a new
   test-only endpoint; otherwise component evidence plus the real-API
   invalid path carries AC-003 with the gap recorded (see risks).
6. Run focused and aggregate checks, published smoke and dependency audits.
   Inspect rendered output, processes, logs and the diff for echoed user
   IDs, codes, emails, stored credentials, new routes or side effects.
7. Reconcile actual behavior/evidence against BI-012 and every AC. Update
   only affected UX/verification/README/current-delivery/coverage owners if
   implementation changes their current truth; keep package
   checkboxes/evidence in `tasks.md`. Do not claim M013–M014, full FR-002,
   live email or release readiness.

If implementation proves a server-contract, generated-shape or
account-lifecycle change indispensable, stop the affected task, update its
design owner and this package, review and re-lock before proceeding.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| M007/M011/current inputs | T001 | `python3 automation/context.py check M012`; inspect `git diff --name-status` and M007/M011 completion links; `node --version`; `npm --version`; `bash scripts/contract.sh check` | Fresh manifest and preflight note in completion record |
| AC-001 | T002/T003 | `npm --prefix frontend test -- tests/unit/verify-email.test.tsx` status/redirect cases: in-memory email shown, unverified protected navigation redirects, zero transformation requests | Focused result plus redirect assertions |
| AC-002 | T002/T003 | Same focused suite with resend cases: one `{email}`-only request, MSG-035, server-cooldown disable/countdown/reenable, no-request invalid email, no language operation | Single-request plus cooldown assertions |
| AC-003 | T002/T003/T004 | Focused confirm cases (`{userId, code}`-only single POST, continuation variants, query stripped from URL/history) plus browser invalid-link trip via `bash scripts/frontend.sh smoke`; real-API valid-link trip attempted only without a new test endpoint | Component assertions plus browser evidence or recorded gap |
| AC-004 | T002/T003 | Same focused suite with invalid/expired/malformed matrix (MSG-032 plus resend path) and status-check cases (one session GET, no transformation, unverified stays with guidance) | Invalid-matrix plus single-read assertions |
| AC-005 | T003 | Same focused suite with failing-transport and resolve-after-navigation cases: generic retry message, one-request retry, no redirect or query restore, no-material direct entry routes to `/register` | Failure-ordering assertions |
| AC-006 | T003/T004 | Focused keyboard/focus cases plus Playwright Chromium verification trip: heading focus, native controls, status-change focus, Enter-once, 320px/390px geometry with reduced motion | Component plus browser evidence |
| AC-007 | T003/T004 | Sentinel cases (user ID/code/email/query absent from storage/history-after-read/DB/logs/reports) plus reload-clears checks in component and browser runs | Sentinel scan result |
| AC-008 | T005 | `bash scripts/contract.sh check`; `bash scripts/backend.sh check`; `bash scripts/frontend.sh check`; `bash scripts/frontend.sh smoke`; `npm --prefix frontend audit --audit-level=moderate`; `git diff --check`; `python3 automation/context.py audit` | `artifacts/test-results/`, command summaries and final AC dispositions |

## Migration, rollout and rollback

No migration, package, service or configuration change is expected. The
page is a pure frontend increment served through the existing published
artifact; rolling back restores the M011 entry state while the M007 API
contracts stand unchanged. No account, key, session or text data is created
by the page itself beyond the explicit resend/confirm/status requests the
visitor triggers.

## Context boundaries and risks

Sign-in/out, safe return, expiry teardown and recovery forms are referenced
only to preserve the later M013–M014 contracts; no session issuance,
cookie handling or reset routes are selected. M034 owns origin, templates
and live delivery. LLM, operation/accounting/cost, provider, quality and
performance domains are irrelevant because verification actions cannot
submit text or call a provider. No AI specification or accounting contract
belongs in the execution packet.

Principal risks and controls:

- Double resend/confirm or a slow network can double-submit. Gate on one
  unsettled request per action, disable controls during flight and assert
  exact request counts; never rely on server idempotency beyond the M007
  contract (harmless valid replay only).
- The 60-second cooldown is server truth. Render the returned
  `retryAfterSeconds`, test boundary reenable with fake timers only, and
  treat a fresh 202 as authoritative on retry.
- A late confirm/status completion can overwrite newer navigation or
  restore stripped query state. Tag in-flight work, discard stale
  completions and assert the resolve-after-navigation case.
- Delivered material can leak into assertions, logs or fixtures. Strip the
  query with a history replacement on read, use synthetic `example.test`
  material, assert sentinel absence and inspect reports before committing
  evidence.
- The adopted generated types can drift from the contracts. Run the
  canonical drift check before adoption and keep the API surface unchanged.
- Real-API valid-link browser evidence needs delivered material the
  runtime sender intentionally does not produce. Attempt it only through
  existing deterministic support against the smoke store/keys with no new
  endpoint; otherwise record the gap and carry AC-003 on component
  evidence plus the real-API invalid-link browser path.

Human actions: None. Routine restores, owned temporary databases/keys and
deterministic local hosts suffice. Missing locked dependencies or an
unavailable deterministic harness blocks only its affected gate and must be
recorded rather than replaced by a fake pass. Branded-browser, device and
assistive-technology checks remain release scope with explicit gaps, not
milestone blockers.

Readiness review: M007 and M011 are satisfied; the selected page contract,
messages, link variants and error boundaries are fixed; every AC maps to
ordered work, a runnable deterministic check and evidence target;
generated-contract review precedes client adoption; no blocking question
or human gate remains; omitted domains have explicit owners.
