# M002 — Published Web Shell Backlog

**Document:** #8 · **Version:** 1.2 · **Updated:** 2026-09-09
**State:** Done; package AC-001–008 passed
**Roadmap:** [M002 — Published web shell](../../07-roadmap.md#41-basic-infrastructure)

**Policy maintenance — 2026-09-09:** Removed superseded milestone duration rules under #0 v1.7. Completed scope, acceptance results and measured durations are unchanged.

## 1. Outcome and authoritative inputs

A visitor can open a basic React SPA through the published ASP.NET host, navigate its signed-out shell, and receive correct missing-API/asset responses. This is the web-hosting prerequisite for later account and language features, not a working account system or editor.

Authoring baseline: Git `ac1552780a0a4114308c39385c67d2a9bec7d446`. Read [#0](../../00-SDD-Planning-Workflow.md) v1.6, [#1](../../01-PRD.md) v0.6, [#2](../../02-ux-specification.md) v1.5, [#3](../../03-architecture.md) v1.8, [#5](../../05-api-design.md) v1.1, [#6](../../06-verification-plan.md) v1.2, [#7](../../07-roadmap.md) v1.2 and [#9](../../09-architecture-decisions.md) v1.7. This change records the bounded pre-account shell in #2 v1.6 and the selected package/evidence links in #6/#7 v1.3. No product scope or threshold changes.

M001 is done: [its completion record](../M001/tasks.md#3-completion-record) records nine HTTP cases and real-process smoke, completed in 22 minutes 18 seconds. Current Program.cs, project, tests, script and README were inspected. There is no frontend or publish command yet. This review relies on recorded M001 evidence; it is not a fresh execution of that milestone.

## 2. Item

| Item ID | Outcome/title | Priority | Target milestone | Dependencies/blockers | State | Delivery package |
| --- | --- | --- | --- | --- | --- | --- |
| BI-002 | Open and navigate the signed-out shell from one published artifact | Delivered / hosting enablement | M002 | M001 done; no remaining blocker | done | [M002](spec.md) |

### BI-002 — Open and navigate the published shell

**Value and scope:** Prove the real UI-to-host packaging boundary before implementing account forms and editors. Deliver the shell, clear unavailable-feature states, route navigation, basic responsive/keyboard semantics and repeatable publish/browser checks. Upstream: FR-003 and enabling portions of NFR-005/007; architecture Section 3.1, UX Sections 3–5 and ADR-001/005. Detailed acceptance belongs only to the package's [AC-001–008](spec.md#3-selected-acceptance).

**Exclusions:** Account APIs/forms/session simulation, editable source/results, local or paid language work, usage/database/AI layers, generated product OpenAPI/client, deployment, TLS certificates and broad visual/device/AT qualification. No feature becomes public merely because its route name exists.

**Boundaries:** Refresh/deep linking and native Back/Forward must work; API/health/asset namespaces must never fall through to HTML. Publishing must work from a clean checkout and remove stale generated assets on repetition. Ordinary backend checks remain independent of npm and browsers. The selected scope does not pass all FR-003 or RG-006 criteria.

## 3. Human steps and prerequisites

**No feature-specific human action required.** No certificate, email, credential, domain or hosting account is needed for local loopback verification. At the beginning, the executor must resolve the supported Node version, locked npm restore and the pinned Chromium runtime before dependent work. Inspection found Node 25.8.1/npm 11.11.0 and a browser cache; these do not prove the selected Node 24 or Playwright runtime is available. The [plan](plan.md#1-current-state-and-tooling) records exact pins and evidence limits.

Use autonomous setup within existing authorization. If an installation/download requires human-controlled access, batch that request at the beginning and keep execution pending until resolved. There is no planned end-of-milestone human check. Unforeseen human-only follow-up is prepared and surfaced at the end while independent authorized work continues, as required by [#0](../../00-workflow/planning.md#select-and-gather-bounded-context); affected acceptance cannot be marked passed.

## 4. Selection and completion

The user requested M002 generation after confirming M001 completion. BI-002 is selected as one cohesive small package; no other milestone is selected. The shell's narrow content keeps focus on publishing/navigation. Selection considers prerequisites, coherent scope and complete verification. Historical durations inform planning; there is no fixed execution-duration limit.

Completion required AC-001–008, recorded real-published-host/browser results, scoped regression evidence, working README instructions and consistent #6/#7/backlog/task states. Current status: **Done — AC-001–008 passed.** The [completion record](tasks.md#3-completion-record) records 8 component, 20 HTTP and 6 published Chromium cases, repeated stale-asset cleanup, isolated artifact serving, backend-only regression checks and inspected narrow screenshots. No product or release gate is claimed.
