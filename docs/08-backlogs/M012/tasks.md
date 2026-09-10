# M012 — Tasks and evidence

Inputs: [spec](spec.md), [plan](plan.md).

## Resume

Next: T001. Blockers: none.
Last check: not run. Changed scope: none.

## Ordered tasks

- [ ] T001 — Reconfirm the locked baseline, selection state, M007/M011 completion, tool pins and restore access; verify `confirmLocalAccountEmail`, `resendLocalAccountVerification` and `getLocalAccountSession` in the generated client and run the canonical drift check with no API change. AC-008; depends on none; done when preflight passes and the adopted shapes are current.
- [ ] T002 — Implement the `/verify-email` status/resend/link-consumption/continuation page and routing changes under `frontend/src/`; wire exactly `{email}`, `{userId, code}` and the bodyless session GET through the generated types with flight disabling, duplicate guards, stale-completion discard, query stripping and in-memory-only state. AC-001–007; depends on T001; done when component behavior matches the spec contract.
- [ ] T003 — Add focused Testing Library suites covering every AC-001–007 assertion without live services, sleeps or secret-bearing reports; extend the redirect/no-material assertions in `App.test.tsx`. AC-001–007; depends on T002; done when the focused suite passes.
- [ ] T004 — Extend the published Chromium smoke with the verification trip (real invalid-link 400, real resend 202, no-material routing, reload-clears, geometry) while preserving M002/M011 trips; attempt the real-API valid-link trip only without a new test endpoint. AC-001–003/006–007; depends on T002/T003; done when the browser trip passes against the owned loopback host or the valid-link gap is recorded.
- [ ] T005 — Run contract drift, aggregate backend/frontend gates, published smoke and dependency audits; inspect output, processes, logs and diff for echoed user IDs, codes, stored credentials or side effects while preserving M001–M011 behavior. AC-008; depends on T001–T004; done when every exact plan command passes with sanitized evidence or the failing gate remains recorded.
- [ ] T006 — Reconcile the diff and actual evidence against BI-012, FR-002, UX-AC-008/009/010/102/104 and V-003/V-004/V-010/V-015; update only affected owner/status/coverage/operating rows and this backlog/tasks record, leaving M013–M014, M034, full product requirements, Q-004, P-005/P-006 and release gates pending. AC-001–008; depends on T005; done when every selected AC has reproducible evidence, local link/context audits pass and no unsupported completion claim remains.

## Completion record

Not started. No evidence yet.
