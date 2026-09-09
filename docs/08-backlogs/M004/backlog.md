# M004 — Independent AI Development Backlog

**Document:** #8 · **Version:** 1.1 · **Updated:** 2026-09-09
**State:** Done; package AC-001–007 passed
**Roadmap:** [M004 — Independent AI development](../../07-roadmap.md#41-basic-infrastructure)

## 1. Outcome and authoritative inputs

An agent can build and exercise LinguaDesk's shared `IChatClient` boundary with a scripted client and inspect a real versioned prompt using only fixed synthetic data, without starting the API or frontend, opening a database, loading credentials or reaching a provider. This establishes a meaningful independent-AI inner loop before language eligibility, transformations, chain policy, provider adapters or live evaluation are implemented.

Authoring baseline: Git `6f86f9f038fca908c650f5d8121a2ae53ebd8ffc`. At selection, the implementation was Git `4207c2478855f4dcedbe5e7e0a49529d35d6be12` plus the unrelated automation-only commit at that authoring baseline. Inputs read: [workflow #0](../../00-SDD-Planning-Workflow.md) v1.7, [PRD #1](../../01-PRD.md) v0.6, [UX #2](../../02-ux-specification.md) v1.6, [architecture #3](../../03-architecture.md) v1.9, [AI #4](../../04-llm-specification.md) v1.3, [API #5](../../05-api-design.md) v1.1, [verification #6](../../06-verification-plan.md) v1.6, [roadmap #7](../../07-roadmap.md) v1.6 and [ADRs #9](../../09-architecture-decisions.md) v1.7. Selection clarified staged project references in architecture #3 v1.10, updated AI #4 to v1.4 and verification/roadmap #6/#7 to v1.7; completion reconciles verified behavior in AI #4 v1.5, verification #6 v1.8 and roadmap #7 v1.8 without changing a product requirement, proposal disposition or accepted technical direction.

M001, the formal prerequisite, is done. Its [completion record](../M001/tasks.md#3-completion-record) documents the pinned .NET host, locked build/test commands and real-process smoke. M002 and M003 are also done and were inspected as regression baselines, but the selected AI commands do not depend on their frontend or persistence behavior. At selection, the solution contained only the API and API test projects, with no AI/Core project, evaluation runner, AI command, AI package reference, executable prompt resource or provider credential/configuration. M004 adds only the documented AI library/test/runner surface and still adds no Core, API reference, provider credential or live configuration.

## 2. Item

| Item ID | Outcome/title | Priority | Target milestone | Dependencies/blockers | State | Delivery package |
| --- | --- | --- | --- | --- | --- | --- |
| BI-004 | Establish a host-independent AI inner loop | Next / AI-risk enablement | M004 | M001 done; no remaining blocker | done | [M004](spec.md) |

### BI-004 — Establish a host-independent AI inner loop

**Value and scope:** Add the nonempty `LinguaDesk.Infrastructure.Ai` library, its focused MSTest project and the standalone `LinguaDesk.Ai.Evaluation` development runner. Exercise `Microsoft.Extensions.AI.IChatClient` once through a deterministic scripted client and make an `eligibility.v1` prompt snapshot inspectable from the runner using a checked-in synthetic fixture. Basis: architecture Sections 3.2/4.1/8.2; AI Sections 2/4.1/9; ADR-012; the independent-build, prompt-inspection and scripted-client portions of V-007. Detailed acceptance is owned by [package AC-001–007](spec.md#3-selected-acceptance).

**Exclusions:** Product eligibility classification/response parsing (M015), Translation (M016), Rewriting (M017), family-chain fallback/deadline policy (M018), provider adapters or transport conformance/live access (M019), evaluation corpus/budget/report execution (M020), Core/shared counting contracts (M005), API composition, HTTP/OpenAPI/client work, durable attempt admission/accounting, frontend changes and any production secret/configuration. The runner accepts no arbitrary workspace text and has no live mode in M004. This item establishes no product requirement, quality/performance result, candidate qualification or release evidence.

**Boundaries:** The prompt resource and serializer are production-shared inputs, not a copied evaluation-only prompt. The scripted client and fixed source fixture remain in development/test code and cannot be selected by the API. One offline probe call is boundary evidence only: its fixed response is not accepted as proof of eligibility or transformation quality. Do not create empty `LinguaDesk.Core` or provider projects merely to fill architecture's planned tree.

## 3. Human steps and prerequisites

**None required for the selected feature.** At the beginning, the executor checks SDK `10.0.302`, NuGet access for the new locked package graph, local shell permissions and the existing backend/frontend regression toolchains. Package restore is routine autonomous setup; if indispensable package access is unavailable after normal recovery, stop before dependent work and report it rather than weakening the pin or vendoring an unreviewed binary.

No provider account, API key, monetary cap, certificate, email action, production database or device review is needed. No planned end-of-milestone human verification exists. Unexpected nonblocking human follow-up follows [#0](../../00-workflow/planning.md#select-and-gather-bounded-context): prepare the concrete action and leave its evidence pending at handoff; if no safe independent work remains, report the blocker.

## 4. Selection and readiness

BI-004 is selected as the sole item in [package 004](spec.md). The accepted shared design already resolves the library/runner names, layer ownership, `IChatClient` boundary and host-independent verification strategy. Package planning selects the compatible package pin and command/test layout within delegated technical scope; it does not decide Q-001, Q-004, Q-005 or Q-007's later live/provider/checker deliverables.

Seven observable scenarios cover project isolation, the shared prompt snapshot, a single scripted boundary call, deterministic commands/failures, no-secret/no-effect behavior, regressions and truthful closeout. Every scenario maps to ordered tasks and verification in the package. All passed with 10 focused AI cases, 42 retained API/storage cases, backend and published-shell smokes, deterministic `eligibility.v1` inspection and nonzero controlled failures. The [completion record](tasks.md#3-completion-record) owns the exact evidence and limitations. No product requirement, provider qualification, live evaluation or release gate is claimed.
