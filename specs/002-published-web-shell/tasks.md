# 002 — Published Web Shell: Execution Tasks

**Version:** 1.0 · **Updated:** 2026-09-08
**State:** Prepared; implementation not started
**Inputs:** [spec.md](spec.md) v1.0, [plan.md](plan.md) v1.0, [BI-002](../../docs/08-backlogs/M002-published-web-shell.md) v1.0

All tasks serve US-001/BI-002. One agent executes dependency order; no parallel delegation is requested. Runtime results are pending; document review is not execution evidence.

## 1. Ordered tasks

- [ ] **T001 — Resolve readiness at the beginning.** Inspect current code and M001 evidence, provision/verify the selected Node/npm/package/Chromium versions, resolve exact manifest pins and confirm the full 30-minute envelope. Batch indispensable human-only access requests now if autonomous setup cannot resolve them. **Targets:** current environment, plan.md Section 1/4, initial frontend manifest/lock preparation. **Depends:** completed M001. **Checks:** AC-001/007/008 prerequisites explicit; matching runtime and install access confirmed; split/replan before dependent coding if too large.
- [ ] **T002 — Build the bounded signed-out shell.** Add strict frontend configuration, router, specified informational pages/native links, heading focus and scoped responsive CSS. Add focused DOM route/content/semantic checks. **Targets:** frontend configuration, src/shell, tests/unit. **Depends:** T001. **Checks:** AC-002/003/005 content assertions pass; no auth simulation, credential form, editor or business request.
- [ ] **T003 — Integrate real publishing and route boundaries.** Build/synchronize generated assets before invoking publish; configure static serving and eligible navigation fallback with explicit API/health/asset exclusions. Keep backend-only fixtures independent of asset presence and preserve relevant M001 checks. **Targets:** Program.cs, HTTP tests, scripts/publish.sh, generated-output ignores. **Depends:** T002. **Checks:** AC-001/004/006; first clean publish contains generated files, API/assets never return shell HTML, and liveness/method behavior remains correct.
- [ ] **T004 — Add repeatable frontend and published-host checks.** Implement planned setup/check/dev/smoke modes and small Playwright suite, using an owned ephemeral Kestrel process serving the published artifact. **Targets:** scripts/frontend.sh, test launcher/config, tests/e2e. **Depends:** T003. **Checks:** AC-003/004/005/007; no Vite server or API interception in published smoke, actual JS/CSS loading, bounded readiness and owned cleanup on success/failure.
- [ ] **T005 — Verify regressions and document actual behavior.** Run scoped frontend/backend checks, clean and repeated publish, off-repository artifact smoke, desktop/narrow/320px geometry/focus, and controlled stale-asset/failure cleanup checks. Write README commands only after verification. **Targets:** implementation corrections, reports/screenshots, README.md. **Depends:** T004. **Checks:** AC-001–008 have reproducible results; zero-test success is rejected, stale assets are absent, backend checks still need no frontend tooling and M001 history stays intact.
- [ ] **T006 — Close or hand off honestly.** Record actual elapsed time, versions, counts/results, artifact locations and scenario status below; update BI-002, #6 and #7. Surface any prepared human-only follow-up at the end. **Targets:** this record, backlog and shared evidence/roadmap links. **Depends:** T005. **Checks:** all selected acceptance is supported before Done; otherwise leave affected scope pending/blocked and record the remaining outcome without extending the budget silently.

## 2. Scenario-to-task mapping

| Scenario | Tasks | Evidence |
| --- | --- | --- |
| AC-001 | T001/T002/T003/T005 | Pending |
| AC-002 | T002/T004/T005 | Pending |
| AC-003 | T002/T004/T005 | Pending |
| AC-004 | T003/T004/T005 | Pending |
| AC-005 | T002/T004/T005 | Pending |
| AC-006 | T003/T005 | Pending |
| AC-007 | T001/T003/T004/T005 | Pending |
| AC-008 | T005/T006 | Pending |

## 3. Completion record

**Implementation:** Not started. **M002 runtime evidence:** None. **Human actions:** None feature-specific; tooling provisioning/access must be confirmed in initial preflight. **Dependency:** M001 done, evidence retained in [package 001](../001-backend-foundation/tasks.md#3-completion-record).

At execution closeout record code revision, actual Node/npm/.NET/package/browser versions, start/elapsed time, commands/exit codes, discovered/executed test counts, reports/screenshots, publish and stale-asset checks, off-repository serving, process cleanup, scenario dispositions and remaining human action/blocker. Earlier M001 evidence does not prove the new shell. All tasks remain unchecked at authoring.
