# 002 — Published Web Shell: Execution Tasks

**Version:** 1.2 · **Updated:** 2026-09-09
**State:** Complete; AC-001–008 verified
**Inputs:** [spec.md](spec.md) v1.1, [plan.md](plan.md) v1.1, [BI-002](backlog.md) v1.1

All tasks serve US-001/BI-002. One agent executed dependency order; no parallel delegation was requested. Runtime results below, rather than document review, provide execution evidence.

**Policy maintenance — 2026-09-09:** Removed superseded milestone duration rules under #0 v1.7. Completed scope, acceptance results and measured durations are unchanged.

## 1. Ordered tasks

- [x] **T001 — Resolve readiness at the beginning.** Inspect current code and M001 evidence, provision/verify the selected Node/npm/package/Chromium versions, resolve exact manifest pins and confirm scope and verification readiness. Batch indispensable human-only access requests now if autonomous setup cannot resolve them. **Targets:** current environment, plan.md Section 1/4, initial frontend manifest/lock preparation. **Depends:** completed M001. **Checks:** AC-001/007/008 prerequisites explicit; matching runtime and install access confirmed; split/replan before dependent coding if too large.
- [x] **T002 — Build the bounded signed-out shell.** Add strict frontend configuration, router, specified informational pages/native links, heading focus and scoped responsive CSS. Add focused DOM route/content/semantic checks. **Targets:** frontend configuration, src/shell, tests/unit. **Depends:** T001. **Checks:** AC-002/003/005 content assertions pass; no auth simulation, credential form, editor or business request.
- [x] **T003 — Integrate real publishing and route boundaries.** Build/synchronize generated assets before invoking publish; configure static serving and eligible navigation fallback with explicit API/health/asset exclusions. Keep backend-only fixtures independent of asset presence and preserve relevant M001 checks. **Targets:** Program.cs, HTTP tests, scripts/publish.sh, generated-output ignores. **Depends:** T002. **Checks:** AC-001/004/006; first clean publish contains generated files, API/assets never return shell HTML, and liveness/method behavior remains correct.
- [x] **T004 — Add repeatable frontend and published-host checks.** Implement planned setup/check/dev/smoke modes and small Playwright suite, using an owned ephemeral Kestrel process serving the published artifact. **Targets:** scripts/frontend.sh, test launcher/config, tests/e2e. **Depends:** T003. **Checks:** AC-003/004/005/007; no Vite server or API interception in published smoke, actual JS/CSS loading, bounded readiness and owned cleanup on success/failure.
- [x] **T005 — Verify regressions and document actual behavior.** Run scoped frontend/backend checks, clean and repeated publish, off-repository artifact smoke, desktop/narrow/320px geometry/focus, and controlled stale-asset/failure cleanup checks. Write README commands only after verification. **Targets:** implementation corrections, reports/screenshots, README.md. **Depends:** T004. **Checks:** AC-001–008 have reproducible results; zero-test success is rejected, stale assets are absent, backend checks still need no frontend tooling and M001 history stays intact.
- [x] **T006 — Close or hand off honestly.** Record actual elapsed time, versions, counts/results, artifact locations and scenario status below; update BI-002, #6 and #7. Surface any prepared human-only follow-up at the end. **Targets:** this record, backlog and shared evidence/roadmap links. **Depends:** T005. **Checks:** all selected acceptance is supported before Done; otherwise leave affected scope pending/blocked and record the remaining outcome with truthful acceptance status.

## 2. Scenario-to-task mapping

| Scenario | Tasks | Evidence |
| --- | --- | --- |
| AC-001 | T001/T002/T003/T005 | Passed — locked install, checks, build and complete publish artifact |
| AC-002 | T002/T004/T005 | Passed — 8 DOM checks and published-route browser coverage |
| AC-003 | T002/T004/T005 | Passed — native history, deep reload, not-found and heading focus in Chromium |
| AC-004 | T003/T004/T005 | Passed — 20 HTTP cases and published-host reserved-boundary probes |
| AC-005 | T002/T004/T005 | Passed — 1440, 390 and 320 CSS px browser checks; screenshots inspected |
| AC-006 | T003/T005 | Passed — repeat publish removed injected stale assets; isolated artifact served hashed assets |
| AC-007 | T001/T003/T004/T005 | Passed — backend check/smoke without webroot; owned Kestrel/Chromium smoke |
| AC-008 | T005/T006 | Passed — README commands and package/shared evidence reconciled |

## 3. Completion record

**Implementation status:** Complete. **Scenario disposition:** AC-001–008 passed. **Human action/blocker:** None. **Dependency:** M001 evidence remains in [package 001](../M001/tasks.md#3-completion-record).

**Revision and environment:** Uncommitted implementation working tree based on Git `a4fbc3ff8623a0bc3e9aea85f1dd9864b4938cfa`; macOS arm64; .NET SDK `10.0.302`; Node `24.20.0`; npm `11.11.0`; Playwright `1.63.0`; Chromium for Testing `153.0.8010.12` (revision 1243). The measured edit/build/check interval was 29 minutes 6 seconds, and the complete turn including initial specification/tooling preflight took approximately 35 minutes. All acceptance work was complete before closeout. These measured durations are historical evidence; the former planning time limit was removed on 2026-09-09. Direct dependencies and their transitive graph are locked in `frontend/package-lock.json`.

**Commands and results:** `bash scripts/frontend.sh check` exited 0 with strict typecheck/lint, 8/8 Vitest component cases and a Vite production build. `bash scripts/backend.sh check` exited 0 with 20/20 MSTest HTTP cases and 0 warnings/errors; `bash scripts/backend.sh smoke` passed on an OS-assigned loopback port while the generated webroot was moved aside. `bash scripts/frontend.sh smoke` exited 0 after a clean publish, copied the artifact outside the repository into an owned temporary directory, started its Kestrel process on an OS-assigned port, and passed 6/6 Chromium cases. Reports are [`frontend-unit.xml`](../../../artifacts/test-results/frontend-unit.xml), [`backend.trx`](../../../artifacts/test-results/backend.trx), and [`frontend-e2e.xml`](../../../artifacts/test-results/frontend-e2e.xml); 390 px and 320 px screenshots under `artifacts/playwright/` were inspected for clipping and overflow.

**Boundary and cleanup evidence:** Browser checks covered redirects, native Back/Forward, reload, unknown client paths, heading/skip focus, reduced motion, 1440/390/320 geometry, referenced hashed JS/CSS, API Problem Details, missing assets/files, health GET/POST and unknown health paths. Injected stale asset fixtures in both the generated webroot and prior publish output were absent after repeat publication. The published smoke served only its isolated artifact without Vite, source assets, API interception or Node runtime, and its owned Kestrel process/temporary copy were removed on exit. Account forms, authentication, editors, business requests and external runtime resources remain absent.
