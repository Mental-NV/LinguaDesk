# M034 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: none (all tasks done). No human or external-service blocker.
Blockers: none. Real SMTP/mailbox work moved to DF-008 and is outside this package.
Last check: backend 507/507, frontend 261/261, contract check, backend smoke and published Chromium 61/61 passed; context fresh. Changed scope: M034 replaces live email with automatic verification for newly created accounts.

## Ordered tasks
- [x] T001 — Remove the unfinished SMTP adapter/configuration and retain the unavailable production senders; AC-005; depends on none.
- [x] T002 — Implement backend automatic verification, no registration delivery and privacy-preserving duplicate behavior; update focused backend tests; AC-001/AC-002/AC-004/AC-005; depends on T001.
- [x] T003 — Implement the in-place registration success/sign-in UI and update unit/published E2E coverage; AC-003; depends on T002.
- [x] T004 — Update canonical requirements, generated contract revision/artifacts and inspect intentional versus unrelated drift; AC-006; depends on T002/T003.
- [x] T005 — Run focused and aggregate backend, contract and frontend verification; all ACs; depends on T004.
- [x] T006 — Refresh the M034 context lock and write the completion record; all ACs; depends on T005.

## Completion record

Revision: base `1e03aee` plus the uncommitted M034 implementation and documentation updates (runner owns commits; no commit made here). Environment: .NET SDK 10.0.302, Node v24.20.0/npm 11.11.0, macOS loopback host. UTC: 2026-09-13 14:48.

- T001/AC-005: the unfinished `Features/Identity/Email/` SMTP implementation and its adapter test were removed. `AddLinguaDeskAccounts` still registers `UnavailableAccountConfirmationSender` and `UnavailableAccountPasswordResetSender`; no SMTP configuration, credential or external dependency remains. Retained resend/forgot behavior passed the account regression suites.
- T002/AC-001/002/004/005: `RegistrationCoordinator` now sets `EmailConfirmed = true` only on the new-account creation branch and no longer calls initial verification delivery. The response is `202 {"status":"signInRequired"}` with the existing no-store/no-credential boundary. Focused registration/verification tests passed 23/23, including zero sender/token-marker effects, durable restart state, identical sequential/case/concurrent duplicates, a seeded unverified duplicate that remains unchanged, current-state authorization, and registration followed by verified bearer sign-in.
- T003/AC-003: registration success stays on `/register`, clears password fields, focuses `Account registration accepted. Sign in to continue.`, echoes no address and offers `Go to sign in`. App/unit tests install no pending-verification guard. Historical verification tests now enter through the retained unverified-session path rather than manufacturing an unverified account through registration.
- T004/AC-006: generated OpenAPI and TypeScript advance from `0.1.0-m010` to `0.1.0-m034`; semantic diff is limited to document description/revision, automatic-verification registration description and `RegistrationStatus: signInRequired`. `env UseSharedCompilation=false BuildInParallel=false bash scripts/contract.sh check` passed with current artifacts.
- T005/all ACs: `env UseSharedCompilation=false BuildInParallel=false bash scripts/backend.sh check` passed 507/507 (API/storage 226, core 10, AI 271); `bash scripts/frontend.sh check` passed typecheck/lint/build and 261/261 tests (one pre-existing Fast Refresh warning and existing React test warnings remain non-failing); the bounded backend liveness/readiness smoke passed. Published-app Chromium passed 61/61, including real API registration offering sign-in, explicit verified sign-in and legacy-unverified verification/resend coverage. Standalone loopback smokes required execution outside the tool sandbox; the application produced no corresponding failure once run in its supported environment.
- T006: canonical PRD, UX, architecture, API, roadmap, deferred-scope, readiness/coverage and README text now identify DF-008 and the temporary risk that address ownership is not proven. `python3 automation/context.py lock M034` locked 20 reviewed sources; final `check` is recorded below.

AC verdicts: AC-001 passed, AC-002 passed, AC-003 passed, AC-004 passed, AC-005 passed, AC-006 passed. Human action required: none; the optional app walkthrough in plan.md remains available.
