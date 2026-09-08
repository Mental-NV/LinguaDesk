# 001 — Backend Foundation: Execution Tasks

**Version:** 1.0 · **Updated:** 2026-09-08
**State:** Prepared; no execution task completed
**Inputs:** [spec.md](spec.md) v1.0, [plan.md](plan.md) v1.0, [BI-001](../../docs/08-backlogs/M001-backend-foundation.md) v1.0

Tasks are ordered for one agent and one bounded milestone. No parallel delegation is required. Every task serves US-001/BI-001; local T/AC identifiers are qualified by this package. Check a task only after its completion check has evidence. Human steps are handled at initial preflight or at the end under #0; no planned human action is required here.

## 1. Ordered tasks

- [ ] **T001 — Establish execution readiness.** Re-read the selected inputs and current diff; verify SDK/pins, shell/curl, restore access and loopback capability. Confirm the complete scope still fits the 30-minute allowance and record execution start. Resolve or batch any indispensable human request now. **Targets:** plan.md Section 5 and current environment. **Depends:** none. **Checks:** AC-001/004/006 prerequisites are explicit; no unresolved required access is carried into coding; narrow pin corrections are recorded before use.
- [ ] **T002 — Create the minimal buildable host.** Create the pinned SDK/central build/package configuration, solution and Api/Api.Tests projects from plan.md; configure the process-only probe and absent-route behavior. Create package locks and appropriate output ignores. **Targets:** global.json, backend configuration/projects, .gitignore. **Depends:** T001. **Checks:** build/analyzers run; AC-002/003 behavior is present without sample APIs or application-service dependencies. No empty Core/AI/frontend projects or OpenAPI are added.
- [ ] **T003 — Verify actual HTTP behavior in isolation.** Add MSTest checks through WebApplicationFactory for health/method responses and missing routes; own and dispose independent factory instances, including concurrent instances. **Targets:** backend/tests/LinguaDesk.Api.Tests/. **Depends:** T002. **Checks:** AC-002/003/004 assertions run against the real host; test discovery and expected test count are recorded; no arbitrary sleep, paid request or fixed test port.
- [ ] **T004 — Provide repeatable local execution.** Add the thin setup/check/run/smoke command modes, explicit failure propagation, ephemeral-port discovery, readiness bound and cleanup. **Targets:** scripts/backend.sh. **Depends:** T003. **Checks:** AC-001/005; setup/check succeeds when prerequisites hold, controlled failure returns nonzero, and real process smoke cleans only owned resources on success/failure. No certificate prompt or provider/email setup.
- [ ] **T005 — Verify the complete increment and document actual commands.** Run the clean restore/build/test and actual-process smoke path, inspect dependency scope and generated output, and record any corrections. Write README instructions only for verified setup/check/run/smoke behavior, with limits and SDK details. **Targets:** README.md, command/test reports and implementation fixes if needed. **Depends:** T004. **Checks:** AC-001–006, including nonzero-test execution, absent UI/database/service prerequisites, and a bounded failed-smoke cleanup case. Do not broaden into future milestone suites.
- [ ] **T006 — Reconcile acceptance and close or hand off.** Complete the record below with actual commands/test counts/results, revision/environment and elapsed time. Update BI-001/#7 and #6's enabling row with evidence. Review the diff for selected scope; surface any prepared human-only follow-up now. **Targets:** this file, M001 backlog, docs/06-verification-plan.md, docs/07-roadmap.md. **Depends:** T005. **Checks:** every scenario has supporting evidence, statuses are truthful and README matches working behavior. If required evidence is missing or the budget expires, keep affected tasks/milestone incomplete and record the remaining outcome.

## 2. Acceptance-to-task check

| Scenario | Implementation / verification tasks | Current evidence |
| --- | --- | --- |
| AC-001 | T001/T002/T004/T005/T006 | Pending |
| AC-002 | T002/T003/T005 | Pending |
| AC-003 | T002/T003/T005 | Pending |
| AC-004 | T001/T002/T003/T005 | Pending |
| AC-005 | T004/T005 | Pending |
| AC-006 | T005/T006 | Pending |

## 3. Completion record

**Implementation status:** Not started. **Runtime evidence:** None. **Human action:** None required at authoring; execution preflight must confirm this remains true. Read-only SDK/cache inspection supports planning only.

At execution closeout record actual code revision, SDK/runtime and package pins, start/elapsed time, commands/exit codes, discovered/executed test counts, test-report locations, real-process smoke and failure-cleanup evidence, scenario disposition and any pending human action/blocker. Do not fill unknown fields with invented values or mark document review as runtime success. All six tasks remain unchecked at authoring.
