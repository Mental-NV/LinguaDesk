# 001 — Backend Foundation: Execution Tasks

**Version:** 1.2 · **Updated:** 2026-09-09
**State:** Complete; all execution tasks and AC-001–006 verified
**Inputs:** [spec.md](spec.md) v1.1, [plan.md](plan.md) v1.1, [BI-001](../../docs/08-backlogs/M001-backend-foundation.md) v1.1

Tasks are ordered for one agent and one bounded milestone. No parallel delegation is required. Every task serves US-001/BI-001; local T/AC identifiers are qualified by this package. Check a task only after its completion check has evidence. Human steps are handled at initial preflight or at the end under #0; no planned human action is required here.

**Policy maintenance — 2026-09-09:** Removed superseded milestone duration rules under #0 v1.7. Completed scope, acceptance results and measured durations are unchanged.

## 1. Ordered tasks

- [x] **T001 — Establish execution readiness.** Re-read the selected inputs and current diff; verify SDK/pins, shell/curl, restore access and loopback capability. Confirm scope and prerequisites remain coherent and record execution start. Resolve or batch any indispensable human request now. **Targets:** plan.md Section 5 and current environment. **Depends:** none. **Checks:** AC-001/004/006 prerequisites are explicit; no unresolved required access is carried into coding; narrow pin corrections are recorded before use.
- [x] **T002 — Create the minimal buildable host.** Create the pinned SDK/central build/package configuration, solution and Api/Api.Tests projects from plan.md; configure the process-only probe and absent-route behavior. Create package locks and appropriate output ignores. **Targets:** global.json, backend configuration/projects, .gitignore. **Depends:** T001. **Checks:** build/analyzers run; AC-002/003 behavior is present without sample APIs or application-service dependencies. No empty Core/AI/frontend projects or OpenAPI are added.
- [x] **T003 — Verify actual HTTP behavior in isolation.** Add MSTest checks through WebApplicationFactory for health/method responses and missing routes; own and dispose independent factory instances, including concurrent instances. **Targets:** backend/tests/LinguaDesk.Api.Tests/. **Depends:** T002. **Checks:** AC-002/003/004 assertions run against the real host; test discovery and expected test count are recorded; no arbitrary sleep, paid request or fixed test port.
- [x] **T004 — Provide repeatable local execution.** Add the thin setup/check/run/smoke command modes, explicit failure propagation, ephemeral-port discovery, readiness bound and cleanup. **Targets:** scripts/backend.sh. **Depends:** T003. **Checks:** AC-001/005; setup/check succeeds when prerequisites hold, controlled failure returns nonzero, and real process smoke cleans only owned resources on success/failure. No certificate prompt or provider/email setup.
- [x] **T005 — Verify the complete increment and document actual commands.** Run the clean restore/build/test and actual-process smoke path, inspect dependency scope and generated output, and record any corrections. Write README instructions only for verified setup/check/run/smoke behavior, with limits and SDK details. **Targets:** README.md, command/test reports and implementation fixes if needed. **Depends:** T004. **Checks:** AC-001–006, including nonzero-test execution, absent UI/database/service prerequisites, and a bounded failed-smoke cleanup case. Do not broaden into future milestone suites.
- [x] **T006 — Reconcile acceptance and close or hand off.** Complete the record below with actual commands/test counts/results, revision/environment and elapsed time. Update BI-001/#7 and #6's enabling row with evidence. Review the diff for selected scope; surface any prepared human-only follow-up now. **Targets:** this file, M001 backlog, docs/06-verification-plan.md, docs/07-roadmap.md. **Depends:** T005. **Checks:** every scenario has supporting evidence, statuses are truthful and README matches working behavior. If required evidence is missing, keep affected tasks/milestone incomplete and record the remaining outcome.

## 2. Acceptance-to-task check

| Scenario | Implementation / verification tasks | Current evidence |
| --- | --- | --- |
| AC-001 | T001/T002/T004/T005/T006 | Passed — locked setup and clean/repeated Release checks |
| AC-002 | T002/T003/T005 | Passed — liveness body/content type/no-cookie and POST 405 cases |
| AC-003 | T002/T003/T005 | Passed — API Problem Details and non-API 404 cases |
| AC-004 | T001/T002/T003/T005 | Passed — concurrent independently disposed factories; no application dependencies |
| AC-005 | T004/T005 | Passed — ephemeral Kestrel success and controlled-failure cleanup |
| AC-006 | T005/T006 | Passed — README commands and reconciled package/shared evidence |

## 3. Completion record

**Implementation status:** Complete. **Scenario disposition:** AC-001–006 passed. **Human action/blocker:** None.

**Revision and environment:** Uncommitted implementation working tree based on Git `3a41cd8ca6835c619dbfbc94c1c960ff3ba62050`; macOS arm64; SDK `10.0.302`; ASP.NET runtime `10.0.10`. Execution ran from `2026-09-08T13:05:35Z` through `2026-09-08T13:27:53Z` (22 minutes 18 seconds). Package pins are `Microsoft.AspNetCore.Mvc.Testing` `10.0.10`, `Microsoft.NET.Test.Sdk` `18.0.1`, and `MSTest.TestFramework`/`MSTest.TestAdapter` `4.0.2`; both project lock files are included implementation artifacts.

**Commands and results:** `bash /Users/mental/Projects/LinguaDesk/scripts/backend.sh setup` from `/private/tmp` exited 0. `bash scripts/backend.sh check` exited 0 on repeated runs and after `dotnet clean backend/LinguaDesk.slnx --configuration Release`; Release compilation reported 0 warnings/errors and MSTest discovered/executed 9 tests with 9 passed, 0 failed and 0 skipped. The machine-readable report is [`artifacts/test-results/backend.trx`](../../artifacts/test-results/backend.trx). `bash scripts/backend.sh smoke` exited 0 after a real Kestrel process listened on an OS-assigned loopback port. A controlled run with `LINGUADESK_SMOKE_PROBE_PATH=/health/missing` exited 1 on HTTP 404 and a process-table check found no remaining `LinguaDesk.Api.dll` host. `LINGUADESK_URL=http://127.0.0.1:5097 bash scripts/backend.sh run` served `Healthy` and shut down cleanly on interruption.

**Scope inspection:** The resolved graph contains only the planned host-testing/test dependencies; source/package declarations contain no EF Core, SQLite, Identity, OpenAPI/Swagger, provider or email dependency. No frontend, product endpoint, credential, certificate or retained application data was introduced. Generated build/test output remains ignored, and the smoke removes only its owned temporary directory and process tree.
