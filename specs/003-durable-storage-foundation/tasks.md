# 003 — Durable Storage Foundation: Execution Tasks

**Version:** 1.0 · **Updated:** 2026-09-09
**State:** Prepared; no execution task complete
**Inputs:** [spec.md](spec.md) v1.0, [plan.md](plan.md) v1.0, [BI-003](../../docs/08-backlogs/M003-durable-storage-foundation.md) v1.0

All tasks serve US-001/BI-003. Execute in dependency order. No parallel agent work is requested. These checkboxes describe implementation work; document review is not execution evidence. No fixed milestone duration limit applies.

## 1. Ordered tasks

- [ ] **T001 — Establish execution readiness.** Re-read recorded authorities, current diff and completed M001/M002 evidence. Verify pinned SDK, EF/package restore, local file access, loopback/process checks and existing publish/browser tooling. Resolve indispensable human access at the beginning. **Targets:** current environment, plan Section 1/5, package/tool manifests and locks. **Depends:** M001 done. **Checks:** AC-001/005/006 prerequisites explicit; no required access silently carried into dependent coding; actual pins and any justified correction recorded.
- [ ] **T002 — Add the real storage boundary.** Implement typed path rules, shared connection policy, scoped context and lazy host registration. Add focused path/runtime-no-create and connection-policy checks. **Targets:** API Infrastructure/Persistence, Program.cs, test project. **Depends:** T001. **Checks:** AC-003/004; no database I/O during ordinary startup, no fake domain model or new public route, configured file outside generated/static outputs.
- [ ] **T003 — Establish explicit migrations.** Add the design-time factory, generate/inspect initial migration/snapshot, establish WAL and implement setup/migrate commands. Add fresh/repeated migration and no-I/O model drift assertions. **Targets:** persistence factory/migrations, scripts/storage.sh, tests. **Depends:** T002. **Checks:** AC-001/003/004/005; explicit quoted target required; history/data preserved on repetition; invalid commands fail without reset; no EnsureCreated/startup migration.
- [ ] **T004 — Prove persistence, isolation and failure behavior.** Add owned file-backed fixtures, separate scopes, commit/rollback/FK/controlled-lock cases, negative target and runtime-open cases, actual host-DI verification and two-process restart evidence. **Targets:** backend tests and owned process helper if needed. **Depends:** T003. **Checks:** AC-002–005; original committed value/history survive, absent runtime file stays absent, only owned resources cleaned, backend suite needs no frontend/live service.
- [ ] **T005 — Run regressions and document verified operation.** Execute locked backend/storage tests, backend smoke and existing published-shell smoke. Verify startup/publish do not initialize storage or include database artifacts. Correct failures; update ignores, meaningful discovery guards and README with tested commands. **Targets:** scripts/tests, README, .gitignore, reports. **Depends:** T004. **Checks:** AC-001–006; actual tests discovered/pass, failures propagate, host/shell boundary retained and all new behavior documented.
- [ ] **T006 — Reconcile acceptance and close.** Review the complete diff and record actual revision/environment, commands/counts/reports, migration/restart/negative-case outcomes and limitations below. Update BI-003/#6/#7 only with supporting evidence; surface any prepared human follow-up at the end. **Targets:** this record, backlog, verification matrix, roadmap and affected documentation. **Depends:** T005. **Checks:** all six scenarios supported before Done; otherwise leave affected scope incomplete with its real blocker/remaining work. M001/M002 history and product/release pending status remain truthful.

## 2. Scenario-to-task mapping

| Scenario | Tasks | Evidence |
| --- | --- | --- |
| AC-001 | T001/T003/T005/T006 | Pending — fresh/repeated explicit migration and metadata/data assertions |
| AC-002 | T004/T005/T006 | Pending — separate scopes, transaction checks and two real host processes with the same file |
| AC-003 | T002/T003/T004/T005/T006 | Pending — WAL, per-connection FK, finite contention and missing-file runtime failure |
| AC-004 | T002/T003/T004/T005/T006 | Pending — negative command targets, no startup mutation and safe output boundaries |
| AC-005 | T001/T003/T004/T005/T006 | Pending — production migrations/provider, drift, repeatability, failure propagation and cleanup |
| AC-006 | T001/T005/T006 | Pending — existing backend/published regressions, actual instructions and evidence reconciliation |

## 3. Completion record

**Not executed.** No M003 build, test, migration, restart or runtime result is claimed. Planning review found no unresolved design blocker; initial tooling/access preflight remains an execution prerequisite.

At execution, replace this paragraph with actual code revision/environment and dependency versions; commands, exit results and discovered counts; report locations; migration/model drift and same-file restart evidence; per-scenario Passed/Failed/Pending status; relevant regression outcomes; cleanup/failure observations and any remaining human evidence. Elapsed time may be recorded for context but never substitutes for acceptance or creates a stopping deadline.
