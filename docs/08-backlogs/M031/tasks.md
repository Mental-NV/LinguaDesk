# M031 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T000. Blockers: none.
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T000 — Confirm M030/M024/M025 Done via their tasks completion records and delivery status, and current workspace/API code state; AC-none (prerequisite); depends on none; done when resume pointer advances with no divergence noted.
- [ ] T001 — Add `fetchOperationStatus` read-only clients for both families against the existing generated status shape; AC-004; depends on T000; done when status-mapping units (all states + 404/410/unavailable) pass.
- [ ] T002 — Extend both workspace reducers with unknown-outcome, status-resolution and usage-unavailable states under existing revision guards; AC-003, AC-004, AC-005, AC-006; depends on T001; done when reducer units pass.
- [ ] T003 — Render Check status / Refresh usage / lost-output disclosure on both pages with §6.4 announcement rules, wired to fenced store flights; AC-003, AC-004, AC-005, AC-006; depends on T002; done when component checks pass.
- [ ] T004 — Publish dedicated-user E2E recovery cases (definitive retry, offline unknown → no-record, usage refresh) with ≥1 real `/login` form path, real-auth fixture reuse, semantic assertions, Smoke-only seeding and password-absence scan; AC-001–AC-007; depends on T003; done when published suite passes and scan is clean.
- [ ] T005 — Prove privacy on new paths and run V-002/V-003/V-005/V-010 focused checks; AC-008; depends on T004; done when sentinel inspection and focused suites pass.
- [ ] T006 — Regenerate OpenAPI/clients per M026/M027 procedure and assert zero drift; rerun M028/M029/M030 published suites as regressions; AC-009; depends on T004; done when generation diff is empty and regressions pass.

## Completion record
Pending — execution has not started.
