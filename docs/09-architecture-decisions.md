# LinguaDesk — Architecture Decision Records

**Document:** #9 · **Version:** 1.5 · **Status:** Reviewed technical decisions; implementation evidence pending
**Updated:** 2026-09-08

Updated against user-approved PRD v0.4, UX v1.3 and architecture v1.5. Provider-policy review used baseline `b5c01a1`; ADR-012 uses baseline `1646094` and the 2026-09-08 LLM specification request and Infrastructure naming clarification. Current design is in [architecture](03-architecture.md) and [LLM specification](04-llm-specification.md); generated contract governance is in [document #0](00-SDD-Planning-Workflow.md). These technical decisions preserve P-005/P-006 proposal status. Amended decisions record the original choice and the correction; superseded text is historical.

## ADR-001: Combined Hosting and SPA Fallback

**Status:** Accepted; clarified 2026-09-07.

**Context:** A React SPA and API need a simple deployable artifact without a production Node service.

**Alternatives:** Separate frontend static hosting or a Node rendering server. Both introduce extra deployment/configuration surfaces without a current requirement for them.

**Decision:** Build Vite assets before ASP.NET Core publish discovers static content. Serve the SPA and API from one host. Explicitly exclude `/api` and missing assets from SPA fallback. Use Vite's API proxy in development.

**Consequences:** One release artifact and same-origin official client. Two build toolchains remain. A published-host smoke must catch packaging/routing errors that an in-memory HTTP host cannot. This clarifies the original unrestricted fallback description; see #3 Section 3.

## ADR-002: SQLite WAL on Durable Volume

**Status:** Accepted; clarified 2026-09-07.

**Context:** Account/usage metadata needs durable relational integrity with low operational overhead.

**Alternatives:** PostgreSQL adds a service but better supports concurrent writers and multi-instance growth; rollback-journal SQLite offers less reader/writer overlap.

**Decision:** EF Core with SQLite WAL on a local durable volume, foreign keys, bounded contention handling, and short transactions. Use one application instance. Reassess PostgreSQL when measured contention or deployment requirements justify it.

**Consequences:** No database daemon, but one writer at a time and no multi-host network volume. WAL alone does not make long writes safe. The single-instance choice is an architecture default, not a confirmed PRD constraint as the original record claimed. See #3 Sections 6–7.

## ADR-003: Vertical Feature Slices without Generic Abstractions

**Status:** Accepted; refined 2026-09-07.

**Context:** Agents need locally understandable changes and testable policy without boilerplate layers.

**Alternatives:** Generic repositories/Unit of Work plus MediatR; four-layer Clean Architecture; all business logic inline in endpoints.

**Decision:** Group by feature. Use EF directly for data access, thin transport endpoints, concrete coordinators for multi-step effects, and framework-independent Core policies. Introduce narrow provider/email adapters and injected time for external effects.

**Consequences:** Few projects and explicit call paths. Units cover most policy without mocking EF or starting a host; real SQL behavior gets integration coverage. Avoid interfaces for every class and avoid concentrating all business logic in endpoints. See #3 Sections 4 and 8.

## ADR-004: Generated and Reviewed OpenAPI Contract

**Status:** Accepted; amended 2026-09-07 with document #0 Section 2 updated first.

**Context:** Manual synchronization of C#, YAML, and client types creates drift. The original decision also removed document #5's canonical path and made runtime code the authority for behavior, contradicting document #0.

**Alternatives:** Hand-maintained schema plus DTOs; a contract-first server generator; runtime-only schema with no reviewed artifact.

**Decision:** Generate version-controlled `docs/05-openapi.yaml` from C# metadata with native ASP.NET Core build-time OpenAPI 3.1 JSON generation and deterministic YAML serialization in one command. Generate TypeScript definitions with pinned `openapi-typescript`; use a small `openapi-fetch` wrapper. Keep selected behavior design and schema diff review before client adoption. Generation starts no real services or migrations; CI rejects schema/client drift.

**Consequences:** One editable schema source and a reviewable contract artifact; no mandatory runtime server or React state library for code generation. Typed schemas still need semantic descriptions and contract tests. P-006 compatibility guarantees are not accepted by this tooling decision. See #3 Section 8.3.

## ADR-005: In-Process HTTP Integration with Explicit Isolation

**Status:** Accepted; narrowed 2026-09-07.

**Context:** Fixed listening ports create collisions, but the original assertion that every integration test must use TestServer and becomes inherently deterministic/parallel-safe was too broad.

**Alternatives:** Full HTTP server for every test; mocked controllers/endpoints; in-process HTTP host plus direct persistence tests and a small published-host suite.

**Decision:** Use `WebApplicationFactory`/TestServer for HTTP integration by default. Test pure policy without a host and persistence directly where HTTP adds no evidence. Use unique migrated SQLite files, independent contexts, fake external adapters/time, and explicit response barriers. Allow an owned real Kestrel process on a discovered ephemeral port for browser/publish/process-boundary checks.

**Consequences:** Most HTTP tests have no TCP dependency. Isolation is explicit; TestServer is not an in-memory database and cannot serve Playwright's browser. The small real-host exception catches wiring and lifecycle failures. See #3 Section 9.

## ADR-006: Stateless Schema Iteration via EnsureCreated

**Status:** Superseded on 2026-09-07 by ADR-007. The original record below is historical and must not guide implementation.

**Context:** EF Core Migrations are highly stateful, requiring CLI commands to add, apply, or remove migration files. When an AI agent rapidly iterates on a feature and makes incremental changes to entity models, it often botches the sequential migration chain, leading to broken snapshots and failed rollbacks that severely disrupt the autonomous workflow.

**Decision:** For local rapid development and integration testing environments, agents must use `context.Database.EnsureDeleted()` and `context.Database.EnsureCreated()` to continuously synchronize the database with the C# models statelessly. Generating an official EF Migration file (`dotnet ef migrations add`) is deferred until the **absolute final step** of the feature's development lifecycle.

**Consequences:**
* Agents can fluidly rename, add, or drop columns without fighting the EF CLI.
* Integration tests can instantly build the freshest schema without tracking migration history.
* The final generated migration file is clean, consolidated, and immutable, keeping the production migration history pristine.
* Production startup strictly uses `MigrateAsync()`; `EnsureCreated()` is forbidden in the production pathway.


## ADR-007: Migration Parity before Feature Completion

**Status:** Accepted 2026-09-07; supersedes ADR-006.

**Context:** EnsureCreated bypasses migration history and cannot smoothly transition to migrations. Deferring migrations until the absolute final step leaves upgrade behavior untested and creates late rework.

**Alternatives:** ADR-006's mandatory database resets; runtime startup migrations everywhere; migrations in normal development/tests with an explicit deployment step.

**Decision:** Use migrations from the first persistent schema. Consolidate only unshared/unreleased changes during iteration, then inspect and test before completion. Never rewrite applied/shared migrations. Test fresh creation, relevant prior-schema upgrades, and pending-model drift. Restrict EnsureCreated to explicitly disposable non-migration cases; no ordinary automatic database deletion. Deploy a tested migration bundle while the single instance is stopped/drained.

**Consequences:** Slightly more deliberate schema iteration, substantially less late drift and data-loss risk. Agents get the same schema path locally and in CI/production. Backup/restore and failure recovery remain explicit. See #3 Section 6.1 and its EF guidance links.

## ADR-008: Short Durable Reservations around Provider Calls

**Status:** Accepted 2026-09-07; API edge semantics remain the scoped dependencies in #3 Section 11.

**Context:** SQLite has one writer. Holding a transaction over LLM latency serializes unrelated work, and rollback cannot preserve an idempotency record. External provider effects cannot participate atomically in the local database transaction.

**Alternatives:** Long transaction over HTTP; completion-only charging without capacity reservations; distributed queue/transaction infrastructure.

**Decision:** Commit metadata-only operation claims and user/global/cost reservations before dispatch. Execute HTTP outside transactions; conditionally finalize and charge once afterward. Enforce unique operation charges and include outstanding reservations in admission. Fence late completion and reconcile crash windows conservatively. No automatic redispatch of ambiguous paid attempts or persisted text for replay.

**Consequences:** A small durable operation state machine is necessary integrity work. Capacity may remain conservatively reserved while outcome/cost is unknown. Exactly-once provider execution and lost-result recovery are not promised. #5 must settle replay, rollover, cancellation, and output-unavailable UX mapping before dependent implementation. See #3 Section 7.

## ADR-009: Unit-Heavy Pyramid with Focused Boundary Evidence

**Status:** Accepted 2026-09-07; scope amended 2026-09-08 for PRD D-17 and UX v1.2 Sections 10–14.

**Context:** Moving every backend policy into HTTP integration produces a wide middle of the pyramid. Moving UI assertions entirely into server tests leaves coverage gaps. DOM tests cannot replace layout or native-browser evidence.

**Alternatives:** Full browser scenario matrix; all backend behavior through WebApplicationFactory; lowest-sufficient-layer assertion ownership with a small integrated browser suite.

**Decision:** Most cases are pure MSTest/Vitest units and focused DOM components. Real SQLite/API tests cover integrity/contracts; Playwright covers browser-sensitive behavior, seven current curated visual baselines, and explicit Translation/full-Rewrite journeys with real frontend/API/local auth/database and fake external adapters. Preserve all UX IDs with active/deferred/retired status and manual evidence for current supported-browser/AT journeys. #6 maps compound scenarios by assertion, without replaying them at every layer.

**Consequences:** Faster deterministic feedback and fewer fragile UI fixtures without losing key browser/SQL boundaries. Browser automation remains a small top layer; live-provider evaluation stays separate. No arbitrary percentage or line-coverage target replaces requirement/risk coverage. See #3 Section 9 and #2 Sections 12–14.


## ADR-010: Explicit Whole-Text MVP with Deferred Assistance

**Status:** Accepted 2026-09-08; derived from user approval of all ten scope dispositions in PRD D-17 / Section 3.2. Product authority remains the PRD, not this ADR.

**Context:** Automatic submissions, editable sentence correspondence, assistance caches, custom controls, route-rule configuration, prefix processing and dual sign-in methods multiply UI/API/accounting/test branches around the core Translation and Rewriting value. The user elected to remove targeted preservation and defer the larger optional capabilities while retaining independent API access.

**Alternatives:** Keep all discovery scope; merely reduce browser test coverage while retaining behavior; hide future features behind flags; or ship a smaller active product with explicit later intent. Lower test coverage alone would not remove the implementation burden and would leave behavior unverified.

**Decision:** Use explicit Translate/Rewrite actions, complete plain editable/copyable results, one native Writing mode dropdown, inline controls, rejection of oversized whole text, local email/password accounts and two simple provider chains (one primary plus at most one fallback per family). Keep cookie and independent-client bearer contracts, canonical API generation, full-input accounting, durable reservations, stale/edit protection, privacy and current accessibility evidence. No simplified diff or alternatives operation ships yet.

PRD DF-001–DF-007 record deferred alternatives, change review, automatic processing, advanced routing, bespoke UI, prefix processing and Google. No placeholder sentence schemas, rich editor, timer subsystem, route-rule DSL or Google configuration is built now. Targeted sentence preservation and duplicate correction-toggle restoration are retired, not automatically included in later assistance.

**Consequences:** Fewer state combinations and external integrations; users explicitly submit each transformation, shorten long inputs and refine output manually. API-first capability remains demonstrable. Simple chain candidates must still meet quality requirements across every route they serve. Most tests remain units/components with focused integrity/API checks and two small integrated feature journeys. Scope cuts do not justify weakening accounting, privacy, authentication, migrations or accessibility.

**Reactivation:** The PRD register governs later selection; refine only the selected capability in its normal delivery package, re-evaluate the old UX at `54343c3`, and update owners/tests. Deferred work does not block current implementation or release readiness. See #3 Sections 1, 5, 8–9 and UX #2 Sections 11–15.


## ADR-011: Provider Retention and No-Training Are Not Eligibility Gates

**Status:** Accepted 2026-09-08; product decision PRD D-18. Does not change the independent API, quality targets or monetary safeguards.

**Context:** The owner wants low-cost LLM serving and cached responses, and explicitly removed provider retention/no-training eligibility requirements from Q-001. Leaving the same restrictions in NFR-004, release gates or downstream design would make that change ineffective.

**Alternatives:** Remove only the Q-001 wording while leaving contradictory gates; or remove the provider requirements consistently while keeping application text-lifecycle decisions separately owned.

**Decision:** Provider selection and launch no longer require a no-training guarantee or a verified retention limit. Q-001 still resolves serving access/settings, quality/performance, provider-managed caching capabilities/pricing and the monetary cap. Align PRD NFR-004/RG-007 and UX/architecture handoffs. Do not make unsupported privacy claims about the chosen provider.

**Consequences:** Low-cost models and provider-managed caching are permitted if they meet the remaining product criteria. LinguaDesk's no-text-logging, workspace teardown and current application-storage rules remain. The user clarified provider-managed caching: no LinguaDesk completed-response cache or new application cache-lifetime blocker is introduced. #4 verifies provider cache capabilities/pricing for cost bounds; successful-character accounting and current text-lifecycle behavior stay unchanged. No additional blocking provider-privacy review is introduced elsewhere.

## ADR-012: Shared AI Infrastructure Library with IChatClient and Independent Evaluation

**Status:** Selected technical design, 2026-09-08, under document #0's delegated shared-design authority; amended the same day for the user's Infrastructure-layer naming clarification. User requested consideration of `Microsoft.Extensions.AI` and AI development/validation independent of API/UI. Implementation and live qualification are pending.

**Context:** Keeping prompts, provider transport and orchestration inside Api would make language experiments depend on its host/auth/database. A separate prototype with copied prompts would not validate serving behavior. Provider fallbacks and middleware can also hide paid requests from the durable cost coordinator.

**Alternatives:** Provider SDK calls embedded in endpoints; an unrelated script/notebook for evaluation; a custom general provider abstraction; a deployed AI service or agent framework. These respectively couple feedback to the host, allow behavior drift, duplicate ecosystem interfaces, or add infrastructure beyond the two whole-text operations.

**Decision:** Use the host-independent Infrastructure-layer library `LinguaDesk.Infrastructure.Ai` with `Microsoft.Extensions.AI.IChatClient` at the provider boundary. The API and standalone evaluation runner share its prompts, settings validation, eligibility, output validation and explicit family traversal. Provider-specific transport/settings stay in adapters; do not assume wire compatibility from the common interface. Keep Core independent of AI packages. A narrow attempt-admission boundary reserves each paid call through the API's durable accounting or an explicit isolated evaluation substitute.

**Naming amendment:** The original proposed library name was `LinguaDesk.Ai`. The user clarified Infrastructure ownership, so the library and its tests are named `LinguaDesk.Infrastructure.Ai` and `LinguaDesk.Infrastructure.Ai.Tests`. `LinguaDesk.Ai.Evaluation` remains the standalone development runner. This refines project naming and layer ownership while preserving the shared implementation and host-independent testing boundary.

Perform classification before transformation so invalid language input is not transformed. Each family visits its primary and at most one fallback; accepted eligibility is reused and abandoned candidates are never revisited. The bound is three provider dispatches including eligibility, with no hidden retries, repair calls or hedging. #4 owns the detailed prompt/output contracts, timeout policy, error classification and capability evidence.

**Consequences:** One additional library and a development runner isolate a real dependency boundary without another deployed service. Fast tests use scripted clients, fake time/admission and synthetic transport fixtures; live evaluation is explicit and budgeted. Standalone success does not establish HTTP/auth, durable settlement, UI behavior or the API latency gate. Production prompt/result text is not persisted in logs or evaluation artifacts, and provider caching does not enable application response caching. Provider/model/token-bound qualification, #5's accounting/API semantics and #6's rubric/evidence remain scoped dependencies. See [#3 Sections 3–4 and 8–9](03-architecture.md) and [#4](04-llm-specification.md).
