# M014 — Tasks and evidence

Inputs: [spec](spec.md), [plan](plan.md).

## Resume

Next: T001. Blockers: none.
Last check: not run. Changed scope: none.

## Ordered tasks

- [ ] T001 — Add `requestLocalAccountPasswordReset`/`resetLocalAccountPassword` wrappers in `frontend/src/api/accounts.ts` on the generated declarations; AC-008 prerequisite; depends on none; done when Vitest unit tests prove the status-to-kind mapping.
- [ ] T002 — Build `frontend/src/auth/ForgotPasswordPage.tsx` and wire `/forgot-password` in `frontend/src/shell/App.tsx`; AC-001, AC-002, AC-003; depends on T001; done when V-003 component tests prove single request, duplicate guard, identical confirmation and failure alerts.
- [ ] T003 — Build `frontend/src/auth/ResetPasswordPage.tsx` and wire `/reset-password` in `frontend/src/shell/App.tsx`; AC-004, AC-005, AC-006; depends on T001; done when V-003 component tests prove link variants, validation/focus, success-with-continuation and no-auto-login.
- [ ] T004 — Prove AC-007 race/strip/keyboard/reflow/privacy behavior; AC-007; depends on T002, T003; done when the focused V-002/V-010/V-015 browser contracts and sentinel sweep pass on the real published host.
- [ ] T005 — Run drift, aggregate regressions and published smoke, confirm M010/M013 retention; AC-008; depends on T004; done when V-009/V-012 checks pass with no OpenAPI diff and the completion record is filled.

## Completion record

Not run. (Execution fills: AC/task outcomes, revision, configuration,
actual commands/procedures, environment, UTC timestamp, evidence links,
failures/fixes and limitations. No pasted command logs.)
