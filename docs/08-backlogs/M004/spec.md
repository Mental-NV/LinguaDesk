# 004 — Independent AI Development: Selected Specification

**Version:** 1.1 · **Updated:** 2026-09-09
**State:** Implemented and verified; AC-001–007 passed
**Milestone/item:** [M004 / BI-004](backlog.md)

## 1. Selection and authority

Select BI-004 only. The [backlog](backlog.md#1-outcome-and-authoritative-inputs) records the inspected Git baseline, dependency evidence and source revisions. Apply workflow #0 v1.7, architecture #3 v1.10, AI #4 v1.4, verification/roadmap #6/#7 v1.7 and ADR-012; PRD/UX/API decisions remain unchanged.

M001 is complete and supplies the pinned .NET/locked-check foundation. M002/M003 are current regression baselines, not dependencies of the AI inner loop. No unresolved PRD proposal or Q-001/Q-004/Q-005/Q-007 live-provider decision is needed for this offline slice.

## 2. Selected story and scope

**US-001 — Develop the shared AI boundary independently.** As an agent preparing LinguaDesk's language pipeline, I can build and test the shared AI library, execute one controlled scripted `IChatClient` call, and inspect the exact messages for a versioned prompt using fixed synthetic input without running the application host or using provider access.

This necessary technical enablement creates the first nonempty parts of architecture's AI repository shape: `LinguaDesk.Infrastructure.Ai`, `LinguaDesk.Infrastructure.Ai.Tests` and `LinguaDesk.Ai.Evaluation`. The serving-shared library owns the prompt resource/composition and the narrow complete-response call boundary; the development runner and tests supply only synthetic fixtures/scripted responses. The API does not reference or compose the library until a selected consuming feature requires it.

The initial inspected resource is `eligibility.v1`, whose behavior and identifiers already belong to AI #4 Sections 3.1/4.1. M004 demonstrates trusted-system-instruction plus JSON-serialized user-data separation and reproducible bundle metadata/hash. It does not parse or decide language eligibility, transform text, select a provider/candidate, validate product configuration or implement fallback.

## 3. Selected acceptance

Every scenario belongs to US-001/BI-004; these IDs are local to this package.

| ID | Given / when | Observable outcome |
| --- | --- | --- |
| AC-001 | A clean locked setup/build targets the selected AI projects | The .NET 10 solution contains a nonempty Infrastructure-layer AI class library, focused MSTest project and standalone evaluation tool with the architecture-owned names. The library references only framework facilities and the pinned AI abstractions package—not Api, ASP.NET Core, EF/Identity, frontend or provider SDKs. Tests and runner reference the shared library. No empty Core project is added |
| AC-002 | The runner inspects the checked-in synthetic eligibility fixture | Standard output deterministically exposes the exact ordered system/user messages, effective prompt ID and resource hash. The user message is JSON serialized and round-trips the fixed source's quotes, newline and instruction-like text exactly; source data is absent from the trusted system instruction. Repeated runs at the same revision are byte-identical apart from no volatile fields |
| AC-003 | The offline probe runs with the scripted client | The shared complete-response boundary invokes `IChatClient.GetResponseAsync` exactly once using fresh messages/options and the supplied cancellation token, and returns the fixed scripted response to the development runner. Captured request content equals AC-002's composition. No middleware retry, streaming, tool/function call, provider adapter, hidden second dispatch or response-envelope/quality acceptance is introduced |
| AC-004 | Setup/check/inspect/probe commands run noninteractively from the repository and the focused check is repeated | They select SDK `10.0.302`, use locked restore after initial lock generation, build Release with warnings as errors and execute a nonzero number of deterministic AI tests. Unknown command/arguments and a deliberately failing test invocation return nonzero rather than being reported as success; a focused unit case proves a scripted client exception propagates |
| AC-005 | The focused commands run with provider credentials absent and outbound HTTP proxy variables pointed at an unreachable loopback endpoint | Check, inspection and the scripted probe still pass without starting/listening on the API, opening/creating a database, reading frontend assets, making an HTTP/provider request, or writing a persistent evaluation report. The runner exposes no live/provider mode and accepts no arbitrary source text or secret argument in this milestone |
| AC-006 | Existing backend and published-shell regression commands run after the AI projects are added | API liveness, API/asset boundaries, durable-storage checks and signed-out navigation remain correct. The aggregate backend check includes the new AI tests with collision-free machine-readable reports and still rejects zero-test or failed runs; focused AI work does not require npm/browser startup |
| AC-007 | The milestone closes | README documents the verified independent setup/check/inspect/probe commands, the fixed synthetic-only boundary and current exclusions. Actual commands, SDK/package versions, test counts/reports, prompt ID/hash, baseline revision and regression outcomes support all scenarios; #4/#6/#7, backlog and tasks agree without claiming M015–M020 or release evidence |

The inspect output is intentionally allowed to contain only the committed synthetic fixture. It is not a production logging or arbitrary-input utility. A raw fixed scripted response can be observed by the development runner but is not a validated product result.

## 4. Verification boundaries and human steps

Use V-007 only for its independent build, shared prompt inspection and scripted-client call portions, with focused MSTest and command-level failure checks. Run the existing V-009/V-012 host/published-shell regression portions and M003 storage cases because the shared solution/check wiring changes. V-001 and the remaining V-007 parser/validator/config/chain/deadline/report assertions, V-006, V-008 and V-013/V-014 remain pending for their selected milestones.

Authentication, account isolation, public API behavior, durable/cost accounting, provider transport, live model access, language quality, browser accessibility and actual workspace-text privacy are not applicable because M004 introduces none of those runtime capabilities. AC-005 proves the narrower no-secret/no-provider/no-host/no-storage development boundary.

**Human actions: none required, at beginning or end.** The executor owns SDK/package/shell and regression-tool preflight. Routine package restore uses existing authorization. No provider credential, spend approval, certificate, email click, production volume or device review is requested. If an unexpected human-controlled dependency appears, follow #0's batching/end-handoff rule and leave affected evidence pending rather than bypassing it.

## 5. Clarifications and readiness

- **Accepted upstream:** ADR-012 and #3/#4 own the project names, Infrastructure placement, `IChatClient`, shared prompt/composition and independent runner. This package does not reopen those decisions.
- **Delegated technical choice:** M004 uses only `Microsoft.Extensions.AI.Abstractions` because it needs `IChatClient` and exchange types, not caching, telemetry, function invocation or other composition middleware. The exact compatible pin and evidence are in [plan.md](plan.md).
- **Staging clarification:** `eligibility.v1` becomes executable/inspectable here; language classification, its strict response parser and acceptance remain M015. Translation/Rewriting bundles remain M016/M017. This preserves stable #4 identifiers without treating a prompt snapshot or scripted response as accepted language behavior.
- **Unresolved but nonblocking:** Q-001 serving/provider/cost bounds, Q-004 lifecycle details, Q-005 corpus/review evidence and Q-007 adapter/checker/token-bound proof still block their owning live/product gates, not M004.

Ready-for-implementation review passed before execution: selected IDs/exclusions were explicit; seven scenarios were testable; the formal dependency was complete; no behavior/product question was hidden; applicable nonfunctional boundaries and human timing were recorded. [plan.md](plan.md) and [tasks.md](tasks.md) mapped every scenario without adding another milestone's work. All seven scenarios subsequently passed; the [completion record](tasks.md#3-completion-record) owns the runtime commands, counts, prompt hash, no-effect observations, and retained regressions. This remains offline enabling evidence only.
