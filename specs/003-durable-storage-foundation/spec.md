# 003 — Durable Storage Foundation: Selected Specification

**Version:** 1.1 · **Updated:** 2026-09-09
**State:** Implemented and verified; AC-001–006 passed
**Milestone/item:** [M003 / BI-003](../../docs/08-backlogs/M003-durable-storage-foundation.md)

## 1. Selection and authority

Select BI-003 only. The [backlog](../../docs/08-backlogs/M003-durable-storage-foundation.md#1-outcome-and-authoritative-inputs) records the inspected baseline and revisions. Apply workflow #0 v1.7, architecture #3 v1.9, verification/roadmap #6/#7 v1.5 and unchanged PRD/UX/API/AI contracts. M001 is complete; M002's published shell is the current host regression baseline.

## 2. Selected story and scope

**US-001 — Develop against durable storage reliably.** As an agent implementing later account and accounting features, I can initialize an explicitly chosen isolated store, preserve committed data across host restarts, and verify persistence using the same database provider and migrations as the application without running the UI.

This necessary technical enablement introduces the storage boundary and migration baseline. Persistent framework migration metadata is sufficient for the initial production schema. Synthetic fixture rows may demonstrate persistence only in owned test databases; no product entity, placeholder account, ledger or source/result storage is invented for a demonstration.

Ordinary shell hosting remains independent of database availability because it has no database-dependent operation. Process liveness continues to mean only process liveness. Storage validation/readiness for database-dependent serving is required when those features are introduced under architecture Section 5.2.

## 3. Selected acceptance

Every scenario belongs to US-001/BI-003; these IDs are local to this package.

| ID | Given / when | Observable outcome |
| --- | --- | --- |
| AC-001 | An explicit initialization command targets a new absolute local file path whose parent is writable, including a path containing spaces | The checked-in production migration chain creates the store and records its baseline. Repeating the command succeeds without duplicate history or loss of committed data. No product account/ledger/text schema is introduced |
| AC-002 | Independent application scopes open the migrated file; a transaction commits a synthetic fixture row, another rolls back, and the host stops and restarts against the same file | The committed value and migration history remain readable through the application persistence boundary; the rolled-back value is absent. Evidence includes two distinct real host processes and separate scopes, rather than only reopening an in-memory connection |
| AC-003 | New runtime connections open the initialized file, or attempt to open a missing file | WAL is active on the initialized store, foreign keys are enforced on each connection and lock waiting has a finite configured bound. Invalid foreign-key writes fail. Runtime access to an absent store fails without creating a replacement file |
| AC-004 | Initialization receives a missing/invalid target or a target it cannot open; an ordinary host starts, builds or publishes | Invalid explicit initialization exits nonzero without falling back to another database or deleting existing data. Normal host startup/build/publish do not create or migrate a store; shell liveness remains dependency-free. Databases and sidecars are outside static/publish outputs |
| AC-005 | Storage checks run repeatedly using independent temporary paths, including a model drift assertion and controlled failure | Checks use the real file-backed provider and production migrations, distinguish passed/discovered tests from zero-test success, return nonzero on failure, and clean up only owned files/processes. The backend/storage check path needs no frontend, credential, certificate or live service |
| AC-006 | Existing backend and published-shell regressions run and the milestone closes | Liveness, API/asset boundaries and signed-out navigation remain correct. README documents verified initialization/configuration/check commands and limitations. Actual commands, counts, versions, revision and reports support all scenarios; M001/M002 history is preserved |

The restart assertion is clean-stop persistence, not proof of power-loss durability, business transaction fencing, monetary reconciliation or backup restoration. Those guarantees retain their own future verification gates. Lock-wait bounds are engineering configuration, not a new user-visible latency SLA.

## 4. Verification boundaries and human steps

Use V-016's fresh migration/model drift portions and V-005's file-backed transaction/persistence foundation. Run relevant V-009/V-012 host/published regressions. The shared strategy remains [#6](../../docs/06-verification-plan.md); this package defines only its selected assertions. Authentication, account isolation, language quality, monetary policy and full accessibility/device review are not applicable because this increment introduces none of those capabilities.

**Human actions: none required, at beginning or end.** The executor owns tooling and local-file/process preflight. If required access cannot be resolved autonomously, batch the indispensable human request at the beginning before dependent work. No production volume, TLS certificate or real email is required. Unexpected dependencies follow #0's end-handoff rule with truthful pending evidence.

## 5. Readiness

Selected behavior, exclusions and dependencies remain resolved. Implementation and runtime evidence in [tasks.md](tasks.md#3-completion-record) supports all six scenarios; no product decision or human action blocks this foundation. Product/release gates outside this enabling package remain pending, and no fixed duration limit applies.
