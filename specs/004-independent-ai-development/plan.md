# 004 — Independent AI Development: Implementation Plan

**Version:** 1.1 · **Updated:** 2026-09-09
**State:** Implemented and verified
**Inputs:** [spec.md](spec.md) v1.1; [BI-004](../../docs/08-backlogs/M004-independent-ai-development.md) v1.1

## 1. Current state, dependency and tooling

At planning, baseline `6f86f9f038fca908c650f5d8121a2ae53ebd8ffc` was clean and pointed at the implemented M003 tree plus one later automation-only change. Git history and package evidence showed M001–M003 done. The repository used SDK `10.0.302`, target framework `net10.0`, central package management, locked restores, warnings-as-errors and MSTest. The solution then had one API and one API-test project; `scripts/backend.sh check` expected one `backend.trx` with at least 42 cases, and no AI project/package/runner/script or executable prompt existed. The implemented result and revised report layout are recorded in [tasks.md](tasks.md#3-completion-record).

Pin `Microsoft.Extensions.AI.Abstractions` **10.9.0** centrally and reference it only from `LinguaDesk.Infrastructure.Ai`. NuGet lists 10.9.0 as the current stable package on 2026-09-09, with a `net10.0` asset and no dependencies for that target; Microsoft documents that this package supplies `IChatClient` and core exchange types. Use the Abstractions package rather than `Microsoft.Extensions.AI`: the full package's caching, telemetry and middleware utilities are unnecessary and several are explicitly out of scope under AI #4. See the official [10.9.0 package](https://www.nuget.org/packages/Microsoft.Extensions.AI.Abstractions/10.9.0) and [`IChatClient` API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.ichatclient?view=net-10.0-pp).

Generate and commit lock files for all new projects after ordinary package restore, then prove `--locked-mode`. Do not add `Microsoft.Extensions.AI.Evaluation`, a provider SDK, an HTTP client package, command framework or a test mocking library. A small hand-written scripted `IChatClient` belongs in test/development code. Recheck NuGet audit during implementation; any vulnerability or incompatible resolved graph is a blocker to the selected pin and must be addressed explicitly, not ignored.

## 2. Project, prompt and boundary design

Add the selected paths from architecture Section 3.2:

- `backend/src/LinguaDesk.Infrastructure.Ai/` — class library containing the embedded `eligibility.v1` system resource, immutable prompt snapshot/metadata, deterministic JSON user-data composition and the narrow complete-response call.
- `backend/tests/LinguaDesk.Infrastructure.Ai.Tests/` — MSTest cases with a capturing scripted `IChatClient`; no API factory, SQLite, provider transport or network fixture.
- `backend/tools/LinguaDesk.Ai.Evaluation/` — console development runner with only `inspect` and `probe` modes in M004. It references the shared library and supplies the fixed fixture/scripted client.

Add all three projects to `backend/LinguaDesk.slnx` under their existing `src`, `tests` and `tools` folders. The API has no project reference to Ai yet. Do not create `LinguaDesk.Core`: architecture's tree is staged and explicitly prohibits empty layers; M004 has no shared Core value/policy to host. A later selected slice adds Core/reference edges when it has non-placeholder content.

Implement `eligibility.v1` as an embedded UTF-8 resource owned by the Ai library. Its system instruction follows AI #4 Sections 3.1/4.1: classification only, fixed supported language/status values, exact response shape and small synthetic examples. Compose a fresh ordered system/user message list per call. Serialize a typed user-data object with `System.Text.Json`; never concatenate or delimiter-wrap source text. The fixed development fixture contains a quote, newline and instruction-like phrase so inspection proves escaping and instruction/data separation. The resource SHA-256 (over defined raw UTF-8 bytes) and prompt ID are stable metadata; exclude timestamps, paths, machine data and source-derived hashes from output.

Keep prompt inspection deterministic: `inspect` accepts no source/config/credential input and writes one canonical JSON document with prompt ID/hash and ordered role/content messages to standard output using explicit serializer options and LF termination. Repeated execution at one revision must be byte-identical. It deliberately displays the known repository fixture only; do not add generic production text logging.

The narrow boundary accepts an immutable prompt snapshot, an injected `IChatClient` and cancellation token, calls the interface's complete-response `GetResponseAsync` exactly once using fresh `ChatOptions`, and returns the raw `ChatResponse` to its caller. It performs no response parsing, retry, fallback, admission, deadline orchestration, streaming, tool registration or quality decision. This boundary is safe only for the scripted M004 probe; later paid composition must add #4/#5's admission and pipeline rules before any provider call. Tests capture invocation count, message/options object identity/content and token propagation. A controlled scripted exception propagates as non-success.

The `probe` runner mode composes the same snapshot used by `inspect`, supplies a fixed scripted response, invokes the shared boundary and emits a deterministic synthetic observation. The observation must state that it is raw/scripted and must not label the payload eligible, successful transformation, qualified or live. Both runner modes reject extra/unknown arguments with usage on standard error and a nonzero exit.

## 3. Commands and affected components

These were the implementation targets; their verified results and commands are linked from the completion record.

| Target | Intended change |
| --- | --- |
| `backend/Directory.Packages.props`, solution, new project/lock files | Pin the single AI abstractions dependency; add the three selected nonempty projects with correct one-way references and locked graphs |
| Ai prompt resource/composition/boundary | Shared `eligibility.v1` message snapshot, stable hash metadata, typed JSON data serialization and exactly-one-call complete-response boundary |
| AI MSTest project | Resource/hash/round-trip/determinism, message isolation, one-call/fresh-options/cancellation, scripted-failure and dependency-boundary assertions |
| Evaluation runner | Fixed synthetic `inspect` and scripted `probe`; deterministic standard output; strict argument/failure behavior; no live/config/arbitrary-text surface |
| `scripts/ai.sh` | `setup`, `check`, `inspect`, `probe`; SDK check, locked restore, targeted Release build/test/run, no host/frontend/database startup and explicit nonzero propagation |
| `scripts/backend.sh` | Retain aggregate backend build/regression behavior while producing collision-free TRX reports for multiple test projects and summing/validating discovered, passed, failed and skipped counts |
| `README.md`, `.gitignore` if needed | Document verified commands, synthetic-only output and limitations; ignore only new build/report artifacts if current rules do not already cover them |

`scripts/ai.sh setup` may access NuGet and performs the initial locked restore once lock files exist. `check`, `inspect` and `probe` run with `--no-restore` after setup. The focused check targets only the Ai library, runner and AI tests; it does not invoke API, storage, npm or Playwright commands. Run commands from any current directory by resolving repository-relative paths. Set common HTTP/HTTPS proxy variables to an unreachable loopback endpoint and clear known provider credential variables during selected offline verification; because no provider/HTTP implementation is in the graph, any accidental dependency still fails visibly.

For controlled failure evidence, use a documented test filter or runner test hook confined to development code; never add a production environment switch that makes assertions pass/fail. The normal test guard reads the focused TRX and rejects absent/zero/failed results. Refactor the aggregate backend report naming/aggregation before adding the second test project so concurrent or sequential test outputs cannot overwrite one another. Preserve the existing minimum 42 API/storage cases and require a positive separately identified AI count; do not merely lower or replace the existing guard.

## 4. Verification and evidence

[#6](../../docs/06-verification-plan.md) remains the strategy and matrix owner. Implement the selected V-007 portions with MSTest and noninteractive commands:

- Project graph: inspect project assets/references and build the three targets without Api. Assert the serving library graph excludes ASP.NET Core, EF/Identity, provider SDKs and the full AI middleware package. Runner/tests may reference only the shared library plus test framework where applicable.
- Prompt: compare exact message roles/order/content and canonical inspect bytes; deserialize the user JSON and verify exact source round trip. Prove the trusted instruction contains none of the changing fixture source and the hash changes only when resource bytes change.
- Boundary: use a capturing scripted `IChatClient` for one complete-response call, fresh request/options, cancellation propagation, raw fixed response and exception propagation. No product parser assertions or live network substitute.
- Effects/failures: run without credentials with unreachable proxies; inspect process/listener and owned sentinel paths before/after as needed to show no API, database or report effect. Exercise strict CLI argument errors, scripted failure and a test-command controlled failure; record nonzero codes without leaving altered files/processes.
- Regression: run `bash scripts/backend.sh check`, `bash scripts/backend.sh smoke`, `bash scripts/frontend.sh check` and `bash scripts/frontend.sh smoke`. Record API/storage/AI/component/HTTP/browser totals and machine-readable report paths. No new browser case is required because no UI behavior changes.

README becomes #10's actual command owner only after the commands work. Closeout records baseline/implementation revision, OS/SDK, exact package/resolved graph, commands/exit codes, AI and retained regression counts, prompt ID/hash, report paths, no-effect observations and limitations. Test output may include only the fixed synthetic source/response. Generated `bin`/`obj` and reports remain ignored; locks and prompt resource are committed.

## 5. Risks, human steps and readiness review

**Human actions: none required for this feature.** At the beginning, verify SDK, package access and existing shell/regression prerequisites. Use routine authorized setup and batch any genuinely indispensable access request before dependent work. There is no provider credential, spend, certificate, email, production storage or device action. No planned end human check exists; unexpected nonblocking human work follows #0's prepared end-handoff rule.

Main risks are accidentally coupling Ai to Api/storage, introducing a throwaway evaluation-only prompt, leaking arbitrary text through inspection, mistaking a scripted raw response for product validation, allowing hidden retries/middleware, and weakening multi-project test-count guards. One-way references, the shared embedded resource, fixed-only runner surface, explicit raw labeling, direct single-call abstraction and per-suite report accounting address them. Package restore availability and a clean NuGet audit are preflight risks; a real incompatibility remains blocking rather than authorizing a silent version change.

Review outcome before execution: the design was feasible on the installed .NET 10 SDK, used the accepted boundary and stable compatible abstraction package, respected staged architecture, and mapped all seven scenarios to verification/tasks. Implementation then followed this design without requiring a product or shared-design correction. The [completion record](tasks.md#3-completion-record) reports passing focused and regression evidence. M015–M020, public API/UI integration, provider access, cost/quality/performance and release gates remain explicitly pending.
