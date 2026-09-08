# M001 — Backend Foundation Backlog

**Document:** #8 · **Version:** 1.0 · **Updated:** 2026-09-08
**State:** Selected for the first delivery package; implementation not started
**Roadmap:** [M001 — Backend foundation](../07-roadmap.md#41-basic-infrastructure)

## 1. Outcome, scope and authority

An agent can build and start the minimal backend and run an isolated meaningful smoke check with reproducible commands. This is necessary enablement for the independent API and later features, not a completed product capability.

Inputs were read at Git `2cd06b1333c710f9db36fddb47137658ce4f112b`: [workflow #0](../00-SDD-Planning-Workflow.md) v1.5, [roadmap #7](../07-roadmap.md) v1.0, [architecture #3](../03-architecture.md) v1.7, [API design #5](../05-api-design.md) v1.1, and [verification #6](../06-verification-plan.md) v1.0. This authoring change applies #0 v1.6's human-step timing and #3 v1.8's process-only probe. [PRD #1](../01-PRD.md) v0.6 retains product scope and thresholds.

Only the host, process liveness, absent-route behavior and repeatable backend verification are selected. Frontend publishing, persistence, independent AI, capability contracts, accounts and language operations remain later roadmap outcomes. No empty project layers, generated OpenAPI, provider/email adapter, certificate provisioning or production deployment is part of this milestone.

## 2. Items

| Item ID | Outcome/title | Priority | Target milestone | Dependencies/blockers | State | Delivery package |
| --- | --- | --- | --- | --- | --- | --- |
| BI-001 | Start and verify a minimal backend without application services | First / necessary enablement | M001 | No prior milestone; execution preflight below | selected | [001-backend-foundation](../../specs/001-backend-foundation/spec.md) |

### BI-001 — Start and verify a minimal backend

**Value:** A repeatable backend feedback path lets later API features build on a real host rather than an unverified template. Upstream: architecture Sections 2–3/5/8 and ADR-005; enabling relationship to FR-035/036 and RG-005, without satisfying those product outcomes yet.

**Acceptance summary:** The selected package owns detailed [AC-001–AC-006](../../specs/001-backend-foundation/spec.md#3-selected-acceptance). They cover a clean build/check path, truthful process liveness, missing-route failures, isolation/no application-service requirements, real loopback process startup/cleanup, and reproducible evidence/documentation. Boundary cases include wrong probe method, missing API paths, repeated isolated runs and command failure propagation. Do not copy the scenarios into this backlog.

**Dependencies and questions:** .NET 10 and restore access are execution prerequisites. Local observation on 2026-09-08 found SDK `10.0.302` and ASP.NET runtime `10.0.10`; relevant package versions were present in the local NuGet cache. Restore completeness, actual build, test discovery and socket execution remain unverified. These are preflight checks, not successful implementation evidence. No blocking product question applies; Q-001/Q-004/Q-006/Q-007 decisions for later services remain open.

**Nonfunctional scope:** Apply backend dependency isolation, deterministic HTTP tests, clean error behavior, no credentials/text processing and owned-process cleanup. UI/accessibility, storage/concurrency, auth, language quality, cost accounting and live-service evidence are not applicable to this increment because it introduces none of those capabilities. Their release obligations remain pending in #6.

## 3. Human steps and execution boundary

**None required for the selected scope.** No certificate, provider key, account, real email, browser/device review or deployment action is needed. The executor checks SDK/package access and local process permissions at the beginning. If the environment needs a human-controlled installation/access change that cannot be handled with existing authorization, request it then, before dependent execution. Do not request a certificate merely to test a process-only loopback host.

There is no planned human step at the end either. Unexpected human-only dependencies follow [#0's human-step rule](../00-SDD-Planning-Workflow.md#selecting-work-and-creating-a-package): continue independent authorized work, prepare a precise end handoff, and keep affected completion pending. Do not interrupt for foreseeable setup or replace required evidence with an assumption.

## 4. Selection and completion

The user requested the first milestone's #8 artifacts on 2026-09-08. BI-001 is selected into package `001-backend-foundation`; no other item or milestone is selected. The [plan](../../specs/001-backend-foundation/plan.md) and [tasks](../../specs/001-backend-foundation/tasks.md) prepare this immediate next increment; this request does not execute them.

Selection review: scope and authoritative behavior are coherent; no product clarification is required. Implementation begins only after environment preflight and the [30-minute feasibility check](../../specs/001-backend-foundation/plan.md#5-budget-risks-and-human-handoff). Split before starting if the complete increment cannot fit. Mark BI-001/M001 done only after all selected scenarios pass, tasks/evidence agree, and #6/#7 plus implemented operating instructions are updated. Current evidence: **Pending — no runtime checks executed**.
