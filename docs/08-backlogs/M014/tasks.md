# M014 — Tasks and evidence

Inputs: [spec](spec.md), [plan](plan.md).

## Resume

Next: none — all tasks done. Blockers: none.
Last check: `bash scripts/frontend.sh smoke` 32/32 Chromium cases passed on the owned published host. Changed scope: none.

## Ordered tasks

- [x] T001 — Add `requestLocalAccountPasswordReset`/`resetLocalAccountPassword` wrappers in `frontend/src/api/accounts.ts` on the generated declarations; AC-008 prerequisite; depends on none; done when Vitest unit tests prove the status-to-kind mapping.
- [x] T002 — Build `frontend/src/auth/ForgotPasswordPage.tsx` and wire `/forgot-password` in `frontend/src/shell/App.tsx`; AC-001, AC-002, AC-003; depends on T001; done when V-003 component tests prove single request, duplicate guard, identical confirmation and failure alerts.
- [x] T003 — Build `frontend/src/auth/ResetPasswordPage.tsx` and wire `/reset-password` in `frontend/src/shell/App.tsx`; AC-004, AC-005, AC-006; depends on T001; done when V-003 component tests prove link variants, validation/focus, success-with-continuation and no-auto-login.
- [x] T004 — Prove AC-007 race/strip/keyboard/reflow/privacy behavior; AC-007; depends on T002, T003; done when the focused V-002/V-010/V-015 browser contracts and sentinel sweep pass on the real published host.
- [x] T005 — Run drift, aggregate regressions and published smoke, confirm M010/M013 retention; AC-008; depends on T004; done when V-009/V-012 checks pass with no OpenAPI diff and the completion record is filled.

## Completion record

Implementation and closing verification ran through 2026-09-10T15:16:50Z on
Darwin arm64, .NET SDK 10.0.302, Node v24.20.0 and npm 11.11.0.
Planning baseline: `001b0ea` (M014 planned); implementation worked
uncommitted on top of `001b0ea`; no commit was created (the runner owns
commits). `python3 automation/context.py check M014` reports the 12 selected
inputs unchanged, and the understood worktree contains only the M014
recovery-form slice plus its focused/component/browser tests.

| Task / AC | Command or procedure | Result / evidence |
| --- | --- | --- |
| T001 / AC-008 prerequisite | Extended `frontend/src/api/accounts.ts` with `FORGOT_PASSWORD_PATH`/`RESET_PASSWORD_PATH` and typed `requestLocalAccountPasswordReset`/`resetLocalAccountPassword` wrappers on the reviewed M010 generated declarations (`sent` with server `retryAfterSeconds`/fixed 60s default, `reset`, `invalid`, `field`, `delivery` for 503, `retry`); email-only and exact-triple JSON bodies with no trim/normalization and no logging | Wrapper mapping proven by 8 Vitest cases in `frontend/tests/unit/forgot-password.test.tsx`/`reset-password.test.tsx`: 202→sent interval/default, field-safe vs malformed 400 split, 503→delivery, invalidOrExpired→invalid, 200→reset, transport failure→retry, exact body keys. Report: `artifacts/test-results/frontend-unit.xml`. |
| T002 / AC-001, AC-002, AC-003 | New `frontend/src/auth/ForgotPasswordPage.tsx` (email form per UX §9: single-flight tagged submit, linked errors, first-error focus, heading focus on the MSG-031 outcome, in-memory state only; MSG-031/038/039, generic retry and MSG-044 delivery alerts each with the specified continuation); `frontend/src/shell/App.tsx` staged stub replaced with the real page | 15 V-003 cases passed: identical known/unknown confirmation with one email-only POST, flight disabling with duplicate click/Enter suppression, missing/malformed/server-field validation with no request, transport→generic retry with one-request retry, 503→MSG-044 with no disclosure, shell presence and the login-pending/forgot-navigation discard race. No language request in any case. Report: `artifacts/test-results/frontend-unit.xml`. |
| T003 / AC-004, AC-005, AC-006 | New `frontend/src/auth/ResetPasswordPage.tsx` (M012 link-material convention: exactly the `userId`/`code` pair stripped after reading; none/malformed/server-invalid → MSG-033 with `Request a new reset link` and no fields; M006-policy checklist plus confirmation, caret-preserving Show password, `pageshow` clearing, cleared secrets, MSG-036 with heading focus and explicit `Go to sign in`, never auto-login/session/workspace); `/reset-password` route added in `frontend/src/shell/App.tsx` | 19 V-003 cases passed: one exact-triple POST with success/no-session/no-navigation, duplicate guard, URL stripping, `pageshow` clearing, four invalid-link variants with zero requests, uniform server rejection resolving to the same alert with no material in the document, required/policy/mismatch/server-policy validation with focus, caret preservation and auth-only retry. Report: `artifacts/test-results/frontend-unit.xml`. |
| T004 / AC-007 | `bash scripts/frontend.sh smoke` against an owned loopback certificate with an isolated migrated database/keys; new `frontend/tests/e2e/recovery.spec.ts` (real 202 acknowledgment, real generic 400, validation no-request, empty-submit focus, 390px/320px reduced-motion reflow of both forms, storage sentinel read); unit race/strip/`pageshow`/sentinel cases; grep sweep of reports/fixtures for synthetic material | Published HTTPS Chromium recovery suite passed 7/7 with zero skips; unit race cases prove late login completions cannot overtake `/forgot-password` or `/reset-password`; query material is stripped after reading; `localStorage`/`sessionStorage` carry no user ID, code or password; synthetic sentinels are confined to test sources and absent from reports. Report: `artifacts/test-results/frontend-e2e.xml`. |
| T005 / AC-008 | `bash scripts/contract.sh check`; `bash scripts/backend.sh check`; `bash scripts/frontend.sh check`; `bash scripts/frontend.sh smoke`; `git diff --check`; `python3 automation/context.py audit`; output/material scans | Contract drift passed unchanged (no API surface added). Backend passed 134/134 (114 API/storage, 10 Core, 10 AI). Frontend passed typecheck, lint, 139 component/policy tests (28 shared input-policy), production build and HTTPS Chromium 32/32 (25 retained M002/M011/M012/M013 trips plus 7 new recovery trips). `git diff --check` and the 108-document link/layout audit passed. Synthetic credentials are confined to test sources; host/JUnit output, URLs, browser storage and retained Playwright artifacts contain no password/token material. M010/M013 behavior retained. |
| T006 / AC-001–008 | Full diff/schema/log/process/privacy review; owner updates (backlog, current delivery, coverage §8.2) | Diff contains the frontend recovery slice (two wrappers, two pages, two routes), focused/component/browser tests and M014 records. There is no API operation, generated shape, migration, package dependency or auth-scheme change. BI-014 and AC-001–008 are done. Live email (M034), language workspaces (M028+), full FR-002 and release gates remain pending. |

Limitations: no live-email delivery proof is claimed (M034 scope; the fixed 60-second acknowledgment is not delivery evidence); no branded browser, device or AT evidence is claimed (release scope); a real 200 reset is proven at the wrapper/component layer with fixed fixtures and at the API layer by M010, not through the published UI (no live email delivers reset material in the smoke environment).
