# M001 — Backend Foundation Backlog

**Document:** #8 · **Version:** 1.2 · **Updated:** 2026-09-09
**State:** Done; implementation and package evidence complete
**Roadmap:** [M001 — Backend foundation](../07-roadmap.md#41-basic-infrastructure)

**Policy maintenance — 2026-09-09:** Removed superseded milestone duration rules under #0 v1.7. Completed scope, acceptance results and measured durations are unchanged.

## 1. Outcome, scope and authority

An agent can build and start the minimal backend and run an isolated meaningful smoke check with reproducible commands. This is necessary enablement for the independent API and later features, not a completed product capability.

Inputs were read at Git `2cd06b1333c710f9db36fddb47137658ce4f112b`: [workflow #0](../00-SDD-Planning-Workflow.md) v1.5, [roadmap #7](../07-roadmap.md) v1.0, [architecture #3](../03-architecture.md) v1.7, [API design #5](../05-api-design.md) v1.1, and [verification #6](../06-verification-plan.md) v1.0. This authoring change applies #0 v1.6's human-step timing and #3 v1.8's process-only probe. [PRD #1](../01-PRD.md) v0.6 retains product scope and thresholds.

Only the host, process liveness, absent-route behavior and repeatable backend verification are selected. Frontend publishing, persistence, independent AI, capability contracts, accounts and language operations remain later roadmap outcomes. No empty project layers, generated OpenAPI, provider/email adapter, certificate provisioning or production deployment is part of this milestone.

## 2. Items

| Item ID | Outcome/title | Priority | Target milestone | Dependencies/blockers | State | Delivery package |
| --- | --- | --- | --- | --- | --- | --- |
| BI-001 | Start and verify a minimal backend without application services | First / necessary enablement | M001 | No prior milestone | done | [001-backend-foundation](../../specs/001-backend-foundation/spec.md) |

### BI-001 — Start and verify a minimal backend

**Value:** A repeatable backend feedback path lets later API features build on a real host rather than an unverified template. Upstream: architecture Sections 2–3/5/8 and ADR-005; enabling relationship to FR-035/036 and RG-005, without satisfying those product outcomes yet.

**Acceptance summary:** The selected package owns detailed [AC-001–AC-006](../../specs/001-backend-foundation/spec.md#3-selected-acceptance). They cover a clean build/check path, truthful process liveness, missing-route failures, isolation/no application-service requirements, real loopback process startup/cleanup, and reproducible evidence/documentation. Boundary cases include wrong probe method, missing API paths, repeated isolated runs and command failure propagation. Do not copy the scenarios into this backlog.

**Dependencies and questions:** .NET 10 and restore access are execution prerequisites. Execution on 2026-09-08 verified SDK `10.0.302`, ASP.NET runtime `10.0.10`, locked restoration, clean Release compilation, nine HTTP integration cases and real loopback process execution. No blocking product question applies; Q-001/Q-004/Q-006/Q-007 decisions for later services remain open.

**Nonfunctional scope:** Apply backend dependency isolation, deterministic HTTP tests, clean error behavior, no credentials/text processing and owned-process cleanup. UI/accessibility, storage/concurrency, auth, language quality, cost accounting and live-service evidence are not applicable to this increment because it introduces none of those capabilities. Their release obligations remain pending in #6.

## 3. Human steps and execution boundary

**None required for the selected scope.** No certificate, provider key, account, real email, browser/device review or deployment action is needed. The executor checks SDK/package access and local process permissions at the beginning. If the environment needs a human-controlled installation/access change that cannot be handled with existing authorization, request it then, before dependent execution. Do not request a certificate merely to test a process-only loopback host.

There is no planned human step at the end either. Unexpected human-only dependencies follow [#0's human-step rule](../00-SDD-Planning-Workflow.md#selecting-work-and-creating-a-package): continue independent authorized work, prepare a precise end handoff, and keep affected completion pending. Do not interrupt for foreseeable setup or replace required evidence with an assumption.

## 4. Selection and completion

The user requested the first milestone's #8 artifacts and then implementation on 2026-09-08. BI-001 was delivered through package `001-backend-foundation`; no other item or milestone was selected. The [plan](../../specs/001-backend-foundation/plan.md) and [tasks/evidence](../../specs/001-backend-foundation/tasks.md#3-completion-record) record the completed increment.

Completion review: all AC-001–006 scenarios passed. Locked setup, clean-output and repeated Release checks, nine discovered tests, successful real-process smoke, controlled failure propagation/cleanup, dependency inspection and implemented operating instructions are recorded in the [completion record](../../specs/001-backend-foundation/tasks.md#3-completion-record). This closes only the selected enabling scope; product and release evidence remains pending in #6.
