# 003 — Durable Storage Foundation: Execution Tasks

**Version:** 1.1 · **Updated:** 2026-09-09
**State:** Complete; all execution tasks and AC-001–006 verified
**Inputs:** [spec.md](spec.md) v1.1, [plan.md](plan.md) v1.1, [BI-003](../../docs/08-backlogs/M003-durable-storage-foundation.md) v1.1

All tasks serve US-001/BI-003. Execute in dependency order. No parallel agent work is requested. These checkboxes describe implementation work; document review is not execution evidence. No fixed milestone duration limit applies.

## 1. Ordered tasks

- [x] **T001 — Establish execution readiness.** Re-read recorded authorities, current diff and completed M001/M002 evidence. Verify pinned SDK, EF/package restore, local file access, loopback/process checks and existing publish/browser tooling. Resolve indispensable human access at the beginning. **Targets:** current environment, plan Section 1/5, package/tool manifests and locks. **Depends:** M001 done. **Checks:** AC-001/005/006 prerequisites explicit; no required access silently carried into dependent coding; actual pins and any justified correction recorded.
- [x] **T002 — Add the real storage boundary.** Implement typed path rules, shared connection policy, scoped context and lazy host registration. Add focused path/runtime-no-create and connection-policy checks. **Targets:** API Infrastructure/Persistence, Program.cs, test project. **Depends:** T001. **Checks:** AC-003/004; no database I/O during ordinary startup, no fake domain model or new public route, configured file outside generated/static outputs.
- [x] **T003 — Establish explicit migrations.** Add the design-time factory, generate/inspect initial migration/snapshot, establish WAL and implement setup/migrate commands. Add fresh/repeated migration and no-I/O model drift assertions. **Targets:** persistence factory/migrations, scripts/storage.sh, tests. **Depends:** T002. **Checks:** AC-001/003/004/005; explicit quoted target required; history/data preserved on repetition; invalid commands fail without reset; no EnsureCreated/startup migration.
- [x] **T004 — Prove persistence, isolation and failure behavior.** Add owned file-backed fixtures, separate scopes, commit/rollback/FK/controlled-lock cases, negative target and runtime-open cases, actual host-DI verification and two-process restart evidence. **Targets:** backend tests and owned process helper if needed. **Depends:** T003. **Checks:** AC-002–005; original committed value/history survive, absent runtime file stays absent, only owned resources cleaned, backend suite needs no frontend/live service.
- [x] **T005 — Run regressions and document verified operation.** Execute locked backend/storage tests, backend smoke and existing published-shell smoke. Verify startup/publish do not initialize storage or include database artifacts. Correct failures; update ignores, meaningful discovery guards and README with tested commands. **Targets:** scripts/tests, README, .gitignore, reports. **Depends:** T004. **Checks:** AC-001–006; actual tests discovered/pass, failures propagate, host/shell boundary retained and all new behavior documented.
- [x] **T006 — Reconcile acceptance and close.** Review the complete diff and record actual revision/environment, commands/counts/reports, migration/restart/negative-case outcomes and limitations below. Update BI-003/#6/#7 only with supporting evidence; surface any prepared human follow-up at the end. **Targets:** this record, backlog, verification matrix, roadmap and affected documentation. **Depends:** T005. **Checks:** all six scenarios supported before Done; otherwise leave affected scope incomplete with its real blocker/remaining work. M001/M002 history and product/release pending status remain truthful.

## 2. Scenario-to-task mapping

| Scenario | Tasks | Evidence |
| --- | --- | --- |
| AC-001 | T001/T003/T005/T006 | Passed — explicit path-with-spaces migration `20260908221711_InitialStorage`, repeat history/data preservation and metadata-only schema |
| AC-002 | T004/T005/T006 | Passed — separate-scope commit/rollback and two distinct cleanly stopped Kestrel processes retained the same value/history |
| AC-003 | T002/T003/T004/T005/T006 | Passed — WAL, per-connection FK rejection, five-second bounded writer contention and missing-file read/write failure |
| AC-004 | T002/T003/T004/T005/T006 | Passed — negative command/path cases, lazy liveness/startup and database-free static/publish outputs |
| AC-005 | T001/T003/T004/T005/T006 | Passed — 42 backend cases using real file-backed SQLite, no-I/O model-drift assertion, controlled failures and owned cleanup |
| AC-006 | T001/T005/T006 | Passed — backend/published regressions, README commands and package/shared evidence reconciled |

## 3. Completion record

**Implementation status:** Complete. **Scenario disposition:** AC-001–006 passed. **Human action/blocker:** None.

**Revision and environment:** Uncommitted implementation working tree based on Git `09ac34f`; macOS arm64; SDK `10.0.302`; EF Core SQLite/Design and repository-local `dotnet-ef` `10.0.10`. Locked resolution uses `SQLitePCLRaw.bundle_e_sqlite3`, core, provider and native library `2.1.12`; this narrow correction replaced the vulnerable transitive 2.1.11 found by NuGet audit. No production credentials, certificate, volume or live service was used.

**Commands and reports:** `bash scripts/storage.sh setup` exited 0 with the local EF tool and locked graph. `bash scripts/backend.sh check` exited 0 with a warning-free Release build and 42/42 MSTest cases (20 retained HTTP cases plus 22 storage cases); its machine-readable report is [`artifacts/test-results/backend.trx`](../../artifacts/test-results/backend.trx). `bash scripts/backend.sh smoke` passed on an OS-assigned loopback port. `bash scripts/frontend.sh smoke` republished and passed 8/8 component and 6/6 isolated Chromium cases; reports are [`frontend-unit.xml`](../../artifacts/test-results/frontend-unit.xml) and [`frontend-e2e.xml`](../../artifacts/test-results/frontend-e2e.xml).

**Storage evidence:** The documented migration command succeeded from outside the repository against an absolute path containing spaces, recorded only `20260908221711_InitialStorage`, and established WAL. Reapplication reported the database current and preserved an independently inserted synthetic test row. The checked model matched its snapshot without creating a file. Application connections used read/write mode, foreign keys, pooling off and a finite five-second writer wait; controlled contention returned SQLite busy within the harness bound. Invalid missing/relative/URI/in-memory/directory/unsafe-output targets failed without fallback, deletion or file creation. Runtime opening of an absent store failed without creating it.

**Restart, publication and scope:** Independent DI scopes resolved the configured file without opening it. A committed synthetic value and migration history survived two distinct real Kestrel process starts and clean stops using that file; a rolled-back value remained absent. Ordinary liveness/startup left a missing sentinel absent and did not modify an initialized file. Published/static outputs contained no database or sidecar. Fixtures released contexts/processes and deleted only their owned temporary directories. The production migration contains framework metadata only—no account, ledger, text or synthetic probe entity. Power-loss recovery, durable accounting, production migration bundles, backup/restore and database-dependent readiness remain pending under their owning milestones; no product or release gate is claimed.
