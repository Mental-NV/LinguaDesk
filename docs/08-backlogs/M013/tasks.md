# M013 — Tasks and evidence

Inputs: [spec](spec.md), [plan](plan.md).

## Resume

Next: T001. Blockers: none.
Last check: not run. Changed scope: none.

## Ordered tasks

- [ ] T001 — Reconfirm the locked baseline, selection state, M008/M012 completion, tool pins and restore access; verify `getAccountAntiforgeryToken`, `signInLocalAccount`, `getLocalAccountSession` and `signOutLocalAccount` in the generated client and run the canonical drift check with no API change. AC-008; depends on none; done when preflight passes and the adopted shapes are current.
- [ ] T002 — Implement the `/login` form, safe-return memory, session bootstrap, signed-in placeholder with inline sign out, staged `/forgot-password` state and routing guards under `frontend/src/`; wire bootstrap plus exactly `{email, password}` sign-in and sign-out posts through the generated types with flight disabling, duplicate guards, stale-completion discard, immediate teardown and in-memory-only state. AC-001–007; depends on T001; done when component behavior matches the spec contract.
- [ ] T003 — Add focused Testing Library suites covering every AC-001–007 assertion without live services, sleeps or secret-bearing reports; extend the guard assertions in `App.test.tsx`. AC-001–007; depends on T002; done when the focused suite passes.
- [ ] T004 — Extend the published Chromium smoke with the sign-in trip (real verified return navigation, real invalid-credential 401 with MSG-028, real 204 sign-out with no Back exposure, expiry MSG-037 entry, reload-clears, geometry) while preserving M002/M011/M012 trips; seed accounts only through existing deterministic support with no new test endpoint. AC-001–004/007; depends on T002/T003; done when the browser trip passes against the owned loopback host or the seeding gap is recorded.
- [ ] T005 — Run contract drift, aggregate backend/frontend gates, published smoke and dependency audits; inspect output, processes, logs and diff for echoed emails, passwords, tokens, stored credentials or side effects while preserving M001–M012 behavior. AC-008; depends on T001–T004; done when every exact plan command passes with sanitized evidence or the failing gate remains recorded.
- [ ] T006 — Reconcile the diff and actual evidence against BI-013, FR-001/FR-038, UX-AC-013/014/015/016/101/102/103 and V-002/V-003/V-010/V-015; update only affected owner/status/coverage/operating rows and this backlog/tasks record, leaving M014, M009/M010, M028+, full product requirements, Q-004, P-005/P-006 and release gates pending. AC-001–008; depends on T005; done when every selected AC has reproducible evidence, local link/context audits pass and no unsupported completion claim remains.

## Completion record

Not started. No evidence recorded.
