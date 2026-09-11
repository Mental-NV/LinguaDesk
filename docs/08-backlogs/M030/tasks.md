# M030 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T000. Blockers: none.
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T000 — Confirm M029 Done via its tasks completion record and delivery status, and current workspace code state; AC-none (prerequisite); depends on none; done when resume pointer advances with no divergence noted.
- [ ] T001 — Lift per-feature workspace state above the router with per-page text/settings/result/scroll preservation, destination `h1` focus, and no submit on navigation; AC-001; depends on T000; done when cross-page E2E case + store units pass.
- [ ] T002 — Fence hidden-page settlement to its owning store entry by captured revision; visible page focus/text untouched; owning page reconciles authoritative usage on return; AC-002; depends on T001; done when navigate-during-pending E2E cases + flight/store units pass.
- [ ] T003 — Regression-harden in-page stale guards on both pages (source/settings/result-edit defeat, MSG-020/021 stale-charge notice, MSG-022 manual-edit protection, no auto-submit, per-page duplicate suppression); AC-003, AC-004; depends on T001; done when V-002/V-003/V-010 focused checks pass.
- [ ] T004 — Publish dedicated-user E2E cases with ≥1 real `/login` form path, real-auth fixture reuse, semantic visible-outcome assertions, Smoke-only seeding and password-absence scan; AC-005; depends on T001, T002; done when published suite passes and scan is clean.
- [ ] T005 — Prove privacy: no source/result text, credentials, global counts, monetary values or provider internals in problems/logs/traces/TEXT columns; AC-006; depends on T002, T003; done when V-015 sentinel inspection passes.
- [ ] T006 — Regenerate OpenAPI/clients per M026/M027 procedure and assert zero drift; rerun M028/M029 published suites as regressions; AC-007; depends on T004; done when generation diff is empty and regressions pass.

## Completion record
Pending. Record AC/task, revision/configuration, actual command/procedure, environment, UTC timestamp, result/evidence link, failures/fixes, and limitations here. No pasted command logs.
