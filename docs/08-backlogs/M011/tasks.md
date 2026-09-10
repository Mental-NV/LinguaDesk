# M011 — Tasks and evidence

Inputs: [spec](spec.md), [plan](plan.md).

## Resume

Next: T001. Blockers: none.
Last check: not run. Changed scope: none.

## Ordered tasks

- [ ] T001 — Reconfirm the locked baseline, selection state, M002/M006 completion, tool pins and restore access; verify `registerLocalAccount` in the generated client and run the canonical drift check with no API change. AC-007; depends on none; done when preflight passes and the adopted shape is current.
- [ ] T002 — Implement the `/register` form, verification-entry state and routing changes under `frontend/src/`; wire exactly `{email, password}` through the generated type with flight disabling, duplicate guards, stale-completion discard, password clearing and in-memory-only email. AC-001–006; depends on T001; done when component behavior matches the spec contract.
- [ ] T003 — Add focused Testing Library suites covering every AC-001–006 assertion without live services, sleeps or secret-bearing reports; update the `/register` placeholder assertions in `App.test.tsx`. AC-001–006; depends on T002; done when the focused suite passes.
- [ ] T004 — Extend the published Chromium smoke with the registration trip, reload-clears and sentinel checks while preserving M002 boundary trips. AC-001/002/005/006; depends on T002/T003; done when the browser trip passes against the owned loopback host.
- [ ] T005 — Run contract drift, aggregate backend/frontend gates, published smoke and dependency audits; inspect output, processes, logs and diff for echoed passwords, stored credentials or side effects while preserving M001–M010 behavior. AC-007; depends on T001–T004; done when every exact plan command passes with sanitized evidence or the failing gate remains recorded.
- [ ] T006 — Reconcile the diff and actual evidence against BI-011, FR-001, UX-AC-006/007/101/102 and V-003/V-010/V-015; update only affected owner/status/coverage/operating rows and this backlog/tasks record, leaving M012–M014, M034, full product requirements, Q-004, P-005/P-006 and release gates pending. AC-001–007; depends on T005; done when every selected AC has reproducible evidence, local link/context audits pass and no unsupported completion claim remains.

## Completion record

Not started. No evidence yet.
