# M003 — Durable Storage Foundation Backlog

**Document:** #8 · **Version:** 1.1 · **Updated:** 2026-09-09
**State:** Done; package AC-001–006 passed
**Roadmap:** [M003 — Durable storage foundation](../../07-roadmap.md#41-basic-infrastructure)

## 1. Outcome and authoritative inputs

An agent can explicitly initialize an isolated file-backed store using the application's migrations, retain committed data across host restarts, and repeat meaningful persistence checks independently of the frontend. This enables later account and accounting features without introducing their schemas or claiming their acceptance.

Authoring baseline: Git `003f355b4f9073e5d9ab6e3792c046a92e44a378`. Inputs: [workflow #0](../../00-SDD-Planning-Workflow.md) v1.6, [PRD #1](../../01-PRD.md) v0.6, [UX #2](../../02-ux-specification.md) v1.6, [architecture #3](../../03-architecture.md) v1.8, [AI #4](../../04-llm-specification.md) v1.3, [API #5](../../05-api-design.md) v1.1, [verification #6](../../06-verification-plan.md) v1.4, [roadmap #7](../../07-roadmap.md) v1.4 and [ADRs #9](../../09-architecture-decisions.md) v1.7. This preparation applies the user's removal of the duration limit in #0 v1.7, clarifies staged persistence in #3 v1.9 and records selection in #6/#7 v1.5. All other product/design decisions retain their existing authority.

M001, the formal dependency, is done. The user also confirmed M002 completion; its [record](../M002/tasks.md#3-completion-record) reports 8 component, 20 HTTP and 6 published Chromium cases. Current host, tests, dependency configuration and build/publish commands were inspected. They contain no application database yet. Existing evidence is read, not freshly rerun during document generation. M002 supplies the current integration baseline; it is not an added dependency on the independent storage outcome.

## 2. Item

| Item ID | Outcome/title | Priority | Target milestone | Dependencies/blockers | State | Delivery package |
| --- | --- | --- | --- | --- | --- | --- |
| BI-003 | Initialize and verify durable local storage explicitly | Next / persistence enablement | M003 | M001 done; no remaining blocker | done | [M003](spec.md) |

### BI-003 — Initialize and verify durable local storage

**Value and scope:** Establish the real persistence and migration boundary before account or ledger work relies on it. Prove explicit initialization, repeatability, durable committed data, connection policy, isolated checks and unchanged shell/HTTP behavior. Basis: architecture Sections 4.1/5.2/6.1; enabling portions of V-005/V-016. Detailed acceptance is owned by [package AC-001–006](spec.md#3-selected-acceptance).

**Exclusions:** Identity/account and ledger schemas, user text persistence, paid calls, backup/restore qualification, production deployment, new readiness/product endpoints, OpenAPI/client generation and new infrastructure projects. Production migration bundles and recovery remain M040; transaction accounting/reconciliation belongs to its selected feature milestones. No full PRD requirement or release gate passes from this enabling item.

**Boundaries:** Ordinary hosting and build/contract generation must not initialize or migrate storage. Existing data is never silently reset after failure. Only explicitly owned temporary storage may be deleted by checks. Database files live outside published/static content and replaceable build outputs.

## 3. Human steps and prerequisites

**None required for the selected feature.** The executor checks the pinned SDK, NuGet/local EF tool restore, temporary local-file access and process-test capabilities at the beginning. Use routine authorized setup. Batch any indispensable human-controlled access request then, before dependent work. No certificate, credential, email, production volume or deployment approval is needed for local synthetic checks.

No planned end-of-milestone human action exists. Unexpected nonblocking human follow-up follows [#0](../../00-workflow/planning.md#select-and-gather-bounded-context): prepare concrete instructions and report pending evidence at the end; if no safe independent work remains, report the real blocker. Existing authorization is sufficient for ordinary setup.

## 4. Selection and completion

The user requested M003 after M002 completion and removed the fixed milestone duration limit. BI-003 was delivered as one coherent outcome; no successor is selected. The [specification](spec.md), [plan](plan.md) and [tasks/evidence](tasks.md#3-completion-record) record the verified implementation. There is no duration gate or time-based stop/split requirement.

All six scenarios passed with locked backend checks, reproducible explicit migration and same-file restart evidence, passing published-shell regressions, verified README instructions and consistent backlog/#6/#7/task states. M003 remains enabling scope only and claims no complete product requirement or release gate.
