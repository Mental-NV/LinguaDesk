# Generation prompt — Architecture and engineering principles

**Artifact:** Prompt for document #3 · **Status:** Ready to use; architecture direction and targeted UX verification amendments confirmed by the user · **Updated:** 2026-09-07

**Scope:** Generate `docs/03-architecture.md`, without implementing the application. This prompt is an authoring aid, not another governing specification.

**Preparation sources:** [Workflow v1.0](../00-SDD-Planning-Workflow.md), [PRD v0.2](../01-PRD.md), and [UX specification v1.0](../02-ux-specification.md), at repository commit `f86a7c1cdc186a47b11de7a0718a8ed076529e4e`. Read their current contents when executing this prompt; do not assume this recorded revision remains current.

---

You are the lead software architect for LinguaDesk. Create `docs/03-architecture.md`: a concrete, proportionate architecture and engineering specification that an autonomous coding agent can use to implement small, independently verifiable increments with little interpretive guesswork or avoidable rework.

Optimize for reliable delivery and maintainability by frontier AI agents. Favor explicit behavior, small dependency graphs, ordinary framework capabilities, deterministic application logic, and a few effective verification mechanisms. Do not optimize for the number of patterns, projects, tests, or tools adopted.

## 1. Establish authority and handle decisions

Read, in order:

1. `docs/00-SDD-Planning-Workflow.md`.
2. `docs/01-PRD.md`.
3. `docs/02-ux-specification.md`.
4. Applicable `AGENTS.md` instructions, repository structure, and any existing relevant specifications, ADRs, configuration, or implementation.

Record actual source versions and commit or hashes, and distinguish existing files from planned destinations. Preserve user work and accepted decisions. Document #0 governs process, #1 product intent, and #2 UX. The current PRD baseline and its proposal dispositions govern over stale descriptions of its review status in another document.

Preserve accepted/amended P-001–P-004 and the existing UX decisions. P-005/NFR-008 and P-006 remain proposals unless an explicit later decision accepted them. Do not silently approve additional abuse-control scope, compatibility guarantees, product features, or release targets. Distinguish baseline controls needed to implement confirmed access/privacy/cost requirements from proposed additional safeguards.

Document #3 owns architecture for Q-001, Q-004, and Q-008. Coordinate Q-002 sentence mechanics with UX, API, and selected feature design; Q-003/Q-006 with #5; Q-007 with #4; and verification/evaluation with #6. Preserve their ownership rather than declaring every cross-document question resolved here.

Make routine technical decisions within the delegated scope and explain the reasoning. Ask only about unresolved consequential choices: product/UX changes, durable deployment constraints, material service costs, privacy/retention commitments, or a substantial departure from the requested stack. Ask at most 1–3 related questions at once, preferably through interactive choice tools, with 2–3 meaningful options, a recommendation, and free-text entry. Reuse answers already given; do not ask again. Continue independent drafting while awaiting a required answer. Silence is not approval; mark unresolved decisions with owner, affected IDs, and the specific stage they block.

The user explicitly confirmed the following choices during prompt preparation on 2026-09-07. They are accepted inputs to this generation task; do not ask for approval again:

| Choice | Confirmed direction | Consequence |
| --- | --- | --- |
| Backend boundaries | Minimal API and feature slices in `LinguaDesk.Api`, plus a small framework-independent `LinguaDesk.Core` project | Keep these two production projects; additional layers need concrete justification |
| Deployment | One application instance, SQLite on durable local storage, brief controlled deployment downtime acceptable | No multi-instance or zero-downtime requirement; do not introduce infrastructure for either |
| Frontend hosting | Build React/Vite into static files and serve them from `LinguaDesk.Api/wwwroot`, with SPA fallback to `index.html` | One production host/artifact; no separate frontend server or hosting service |
| Existing UX verification | Targeted amendments reducing duplicated browser/screenshot checks while preserving observable behavior, acceptance coverage, supported browsers, and required human release checks | Apply minimal amendments to the owning UX verification sections during architecture generation; preserve scenario IDs and acceptance outcomes |

The user permits architectural simplification, including changes to the requested stack where clearly beneficial. That is not a reason to change technology gratuitously. Keep .NET, React, SQLite, MSTest, Vitest, and Playwright unless a concrete requirement makes another choice materially better. Record any departure, alternatives, operational consequences, and acceptance provenance.

## 2. Requested baseline and repository layout

Use forward slashes for portable paths; they represent the requested Windows-style locations too.

- Backend host: `backend/src/LinguaDesk.Api`, .NET 10, ASP.NET Core, Minimal APIs, vertical feature slices.
- Backend engineering: pragmatic Clean Architecture boundaries, centralized NuGet package versions, compiler/analyzer enforcement, deterministic application policy.
- Persistence: EF Core with its matching SQLite provider and versioned migrations; durable database location independent of application releases.
- Backend tests: `backend/tests`, MSTest for unit, API integration, and contract verification.
- Frontend: `frontend/src`, React, TypeScript, Vite, npm, vertical feature slices. Interpret “Vite tests” as **Vitest** for unit/component behavior. Build static assets into the backend's published `wwwroot` for production hosting by `LinguaDesk.Api`.
- Frontend tests: `frontend/tests/unit`, optional `frontend/tests/component`, and `frontend/tests/e2e`; Playwright verifies browser journeys against UX story/scenario IDs.

Provide one concrete proposed tree, including the solution, Core if selected, feature directories, persistence/migrations, test projects, shared fixtures, frontend configuration, and dependency lockfiles. Keep frontend package/configuration files under `frontend`, outside `frontend/src`.

Use the confirmed two production .NET projects: `LinguaDesk.Api` and `LinguaDesk.Core`. Core contains only framework-independent policy/types that benefit from isolation; API contains endpoint slices, orchestration, EF persistence, identity, and provider adapters. API references Core; Core cannot reference ASP.NET Core or EF. Infrastructure implements Core ports where policy actually needs them. Slice handlers may use EF directly where an extra abstraction adds no value. Explain that this is a pragmatic dependency boundary, not a claim of a fully separated four-layer architecture.

Do not add generic repositories over EF, a second unit-of-work abstraction, mandatory interfaces for every class, mediator/event-bus frameworks, automatic mapping, separate read/write stores, microservices, distributed caches, or background job infrastructure without a demonstrated need. Keep transaction-sensitive accounting in a focused owner instead of duplicating it across slices. Show allowed dependencies and how cross-feature collaboration works without cycles.

Describe one representative slice, such as full rewriting, through endpoint, validation, policy, persistence/operation coordinator, provider adapter, response, frontend state transition, and the smallest useful test seam. Use concise pseudocode or a sequence diagram only where it removes ambiguity. Do not generate application scaffolding or file-by-file implementation tasks.

## 3. Resolve the architecture's difficult behavior

### Runtime topology and independent API use

Use the confirmed topology: build React/Vite into static files, include those files in `LinguaDesk.Api`'s published `wwwroot`, and serve the SPA and API from one origin and deployment artifact. Use an ASP.NET Core SPA fallback such as `app.MapFallbackToFile("index.html")`. A Vite development server/proxy is permitted locally; production has no Node server, SSR service, or separate frontend hosting. Preserve API-first behavior: an independent client must authenticate and complete translation, rewriting, alternatives, and usage access without React logic.

Specify the reproducible build/publish chain (`npm ci`, frontend checks/build, then copying the build output into the API publish artifact's `wwwroot`, with the exact orchestration chosen and documented). Prefer a conventional `frontend/dist` intermediate output and a controlled publish copy; never hand-edit generated `wwwroot` assets or use a broad cleanup that could touch the durable database/keys. Publish matching frontend/backend versions together, remove obsolete assets from the new artifact, and leave durable state outside it.

Align copy timing with the static-serving mechanism: if using build-discovered static asset endpoints, include frontend assets before the relevant .NET asset discovery/publish steps; if copying after publish, explicitly use and verify a serving mechanism that discovers those physical files. Do not assume a generated static-assets manifest includes files copied later. Verify at least one browser journey against the actual published artifact so the development proxy cannot conceal packaging/fallback failures.

Define the appropriate .NET 10 static-asset middleware/endpoints and ordering, with verified behavior for direct `/translate` and `/rewrite` navigation, refresh, missing assets, unmatched frontend routes, auth callbacks, and API errors. Ensure unknown `/api/*` routes and unsupported API methods return API 404/405 responses rather than a successful HTML shell; a generic SPA fallback alone does not establish that boundary. Server-handled auth callbacks must take precedence. Choose explicit API namespace and fallback exclusions in coordination with #5. Keep frontend unknown-route UX from #2, use suitable cache behavior for the shell versus fingerprinted assets, and keep sensitive API responses out of caches. Static SPA availability does not authorize protected API operations.

Define process, browser, database, identity/email, and LLM provider boundaries. Explain whether operations complete over ordinary HTTP and how interrupted calls obtain status without blindly repeating paid work. Complete-result UX does not require token streaming, WebSockets, or a queue. Add such infrastructure only for an identified requirement.

### Identity, security, and configuration

Prefer ASP.NET Core Identity and established Google authentication components over custom credential/token cryptography. Decide and justify browser session transport and a concrete independent-client authentication path. Cookies with antiforgery protection are a starting point for the same-origin SPA; do not assume that choosing cookies alone proves FR-036. If using built-in Identity bearer tokens, document their actual capabilities and limitations rather than calling them JWT/OAuth tokens.

Specify local verification gating, password/reset/verification policy and expiry, session invalidation, sign-out failure, Google callbacks, and account ownership checks for every workspace/operation identifier. Do not treat UX's example password checklist as the security policy. Resolve or explicitly assign account linking/deletion questions without inventing an account-management UI. Persist required Data Protection keys across deployments and include their protection/recovery lifecycle.

Define configuration precedence, startup validation, owner-managed routing, secret injection, local development secrets, production secrets, and which settings require restart. Never expose provider keys through `VITE_*`, browser bundles, logs, or repository files. Include appropriate TLS, origin/CSRF, safe text rendering, sensitive response caching, and authorization boundaries, with traceable scope. Keep unaccepted numerical abuse limits and additional SLOs visibly proposed.

### SQLite durability and lifecycle

Specify an explicit production data-directory configuration and absolute resolved database path, for example `/var/lib/linguadesk/linguadesk.db` on the durable host volume. Treat that as a proposed deployment path, not an existing resource. Store neither the live database nor keys in `bin`, publish/release directories, the image writable layer, temporary directories, or an accidentally relative working-directory location. Define a separate stable development path outside source-controlled application files. Tests use unique disposable paths.

Require path/permission validation and production behavior that prevents silently creating a fresh empty database when a volume is missing. Distinguish intentional first provisioning from ordinary startup. Explain the volume's host-local filesystem requirements, ownership, backup destination, and survival across deploy/restart. Do not assume a platform's ephemeral filesystem is durable or that SQLite WAL is suitable for shared network filesystems.

Use EF migrations rather than `EnsureCreated` for the real schema. Choose one controlled migration execution mechanism, with deployment admission stopped, backup, schema update, startup/readiness, and failure recovery. Consider SQLite table rebuilds and interrupted migration locks. Describe rollback compatibility and recovery of metadata/keys; do not promise that arbitrary down-migrations safely undo data changes.

Choose SQLite journaling, foreign-key enforcement, lock/busy handling, short transactions, and application-managed concurrency tokens where needed. Use database constraints and atomic conditional writes to enforce invariants. Never hold a database transaction open while calling an external provider. Do not use EF's non-relational InMemory provider as evidence for SQLite behavior. Store counts and money using explicitly bounded integer representations where appropriate; avoid floating-point accounting and unsupported SQLite query assumptions.

Define WAL-safe backup and a meaningful restore check. Persistent storage surviving a deployment does not by itself establish recoverability or high availability. Explain the concrete conditions that would justify a future database/topology change without building that migration preemptively.

### Accounting, idempotency, deadlines, and crash recovery

This is a high-risk design area. Give a concrete operation state machine, transaction boundaries, persisted metadata, constraints, and recovery rules. Distinguish:

- Successful-character usage from provisional capacity reservations.
- A logical operation from individual provider attempts.
- User/global daily accounting from monthly provider expenditure.
- Client display freshness from server operation outcome.
- Known failure from an outcome that the client cannot yet confirm.

Explain atomic admission against both character allowances, successful completion/charge exactly once, release on known failure, and reconciliation after process failure. Preserve the charge for an actually successful stale operation even when its result cannot be displayed. Establish the precise success/commit boundary; do not report success before accounting is committed.

Define idempotency scope, duplicate requests, same key with different payload, concurrent duplicates, expiry, terminal-status lookup, and the boundaries of deduplication after expiry. Do not persist source/result payloads to make response replay easier. If a fingerprint is necessary, assess retention and exposure of short predictable text and use only the minimum justified metadata.

Make Q-003 recommendations for operations spanning midnight and ordered usage snapshots; avoid destructive daily-counter resets racing with in-flight work. Define which accounting period owns a reservation/charge and how delayed observations are ordered across reset periods. Leave canonical wire fields and counting rules with #5 and make their required semantics explicit.

Cover crash points before dispatch, after possible provider acceptance, after response but before durable finalization, and after commit but before client receipt. Cancellation/disconnect does not prove that provider work stopped. State what can and cannot be guaranteed without provider idempotency/status support; do not promise exactly-once external execution. Define bounded, conservative handling of uncertain costs and interrupted reservations without duplicate dispatch, false no-charge claims, indefinite hidden holds, or allowance overruns. Surface any infeasible guarantee as a design blocker.

The monetary ceiling must address every potentially paid call, including detection if applicable, alternative context, failed attempts, retries, and fallbacks. Explain admission using conservatively bounded exposure, output/context/attempt limits, durable reservations, settlement, billing-period boundaries, and startup recovery. Check before further paid attempts; a post-hoc spend counter alone cannot prevent overrun. State uncertainty in provider pricing/usage reports and any dependence on a genuine provider-side cap. Do not invent a monetary amount or claim provider eligibility has been verified. Missing required cap/eligibility configuration must prevent paid processing while still allowing deterministic local development.

### Text lifecycle, workspace state, and privacy

Provide a data classification/lifecycle table covering accounts, authentication keys/tokens, usage ledgers, operation metadata, cost reservations, source/result text, sentence correspondence, alternatives, diagnostics, backups, and test artifacts. For each, name storage, owner, retention/deletion trigger, and whether it can contain user text. Set technical retention values only within delegated authority; record product/privacy commitments that still need a decision.

Honor UX #2 §3.4: a verified tab owns one memory-only workspace; SPA route changes preserve it; reload/unload/tab close/new-workspace/sign-out/session invalidation end it. Offline intervals and backgrounding alone do not end it. Keep both feature states alive across mode navigation, but never serialize their content/settings into browser storage, URLs, history payloads, service workers, logs, or analytics.

Choose where temporary server text and sentence state live, how independent clients supply needed context, how requests are authorized, and what status survives a restart without storing text. Explain teardown and bfcache defense, cross-tab/session invalidation, loss of connectivity, and crashed/closed clients. Unload callbacks are best effort; do not claim immediate remote deletion or physical memory erasure from an unreliable browser signal. Bound server-side text lifetime without silently changing the UX workspace boundary.

Ensure logs, traces, exceptions, HTTP diagnostics, telemetry exporters, crash dumps, and test/browser artifacts do not become hidden text history. Use synthetic fixtures for test artifacts. Distinguish LinguaDesk retention from the serving provider's verified no-training and disclosed retention terms.

### Text eligibility, sentence correspondence, and frontend state

Define the boundary between deterministic eligibility/counting and uncertain language/model judgments. The omitted translation suffix must never leave the browser, including for detection or validation. Character policy belongs to #5; propose Unicode/newline/whitespace semantics and shared conformance vectors so .NET and TypeScript cannot diverge through their default string-length behavior. Define offset units and conversion separately from the charging unit.

Choose a practical implementation approach for the editable result, non-mutating source boundary decoration, word changes, clean copy, caret/selection, undo/redo, IME, and accessible sentence actions. Compare a maintained editor/decoration library with native text controls plus decoration only if useful. Avoid casually building an editor engine or choosing a component unable to satisfy the UX contract. Verify a proposed library's actual capabilities; record a bounded technical spike as a dependency if necessary.

Specify the owner and algorithmic approach for sentence segmentation/correspondence across all four languages, stable identities, result revisions, replacement groups, deleted-source comparison entries, and affected-boundary invalidation. Address repeated identical sentences, insertion/deletion/merge/split, manual undo, and alternatives containing multiple sentences. Preserve unaffected metadata; “invalidate everything” does not satisfy P-002/FR-023. Explain how manual-only edits remain local while subsequent alternatives can validate the selected text/context at the API boundary.

Use explicit typed states/events or reducers for debounce, eligibility, pending operations, stale successes/failures, manual edits, alternatives cache, usage ordering, unknown outcomes, and teardown. Define revision/generation guards; equal text after A→B→A must not revive A. Keep effects at narrow boundaries; AbortController alone is not stale-response protection. Prevent mount, focus, reconnect, query-library defaults, or React development lifecycle behavior from accidentally repeating paid operations.

Frontend slices should own their UI, state, API integration, and feature-specific helpers. Keep the app shell, generated contract types/client, genuinely shared controls, and workspace coordination small. Avoid a global state framework, query library, or shared utilities layer unless it solves a concrete problem. Specify non-persistence and retry settings for any chosen library.

## 4. Engineering rules that reduce agent rework

Select a small, compatible toolchain and verify version-sensitive claims using official documentation. Keep .NET/EF major versions aligned; pin exact implementation SDK/tool/package versions and browser tooling in their proper executable files when implementation begins. Do not guess future package versions or use floating `latest` settings in CI.

Define:

- `backend/Directory.Packages.props` for NuGet Central Package Management; `Directory.Build.props` for shared build/analyzer settings; a pinned SDK and local .NET tools; package locks and locked CI restore. “Centralized package store” means centralized dependency/version management, not a new private package registry or committed package binaries.
- One frontend npm manifest and committed `package-lock.json`, pinned compatible Node/npm versions, and `npm ci` in CI.
- C# nullable analysis, deterministic builds, SDK-pinned recommended .NET analyzers (for example `10-recommended` if supported by the chosen SDK), ASP.NET/EF/MSTest analyzers, and a small `.editorconfig`. Promote compiler/nullability/correctness warnings to build failures with scoped documented exceptions. Avoid maximal analyzer/style packs or mandatory documentation for every member.
- TypeScript `strict`, deliberate indexed-access checks, ESLint flat configuration with core recommended rules, typescript-eslint `recommendedTypeChecked`, React Hooks rules, targeted exhaustiveness/async-safety checks, and appropriate accessibility rules. Use one formatter, keep formatting rules from competing with lint, and scope generated/vendor/build files out. Do not adopt every strict/style rule by default.
- A single canonical OpenAPI contract at `docs/05-openapi.yaml` when authored, with a pinned generation path for frontend types/client and one drift/conformance mechanism. Architecture describes ownership and integration expectations; it must not become a second endpoint/schema catalog. Do not generate an implementation-derived schema and call its agreement with itself independent contract verification.
- Injectable time and external effects: .NET `TimeProvider`, controlled frontend clocks, scripted provider/email/auth boundaries, explicit culture/timezone, and stable ordering. IDs may be controlled in tests while retaining appropriate production randomness/security. Real LLM outputs remain nondeterministic; deterministic builds and fixtures do not prove linguistic quality.
- Discoverable commands for restore, format check, lint, typecheck, build, targeted tests, full applicable verification, migrations, development startup, and fixture mode. Specify intended commands/working directories and exit behavior; label them planned until they exist and run. No interactive/watch mode in CI and no production test-auth switch that can accidentally be enabled.

Keep the implementation workflow derived from #0: selected spec → coherent plan → small tasks → implementation and applicable verification. Prefer one feature change with its necessary test and contract update over speculative cross-repository refactors. Add a shared abstraction after demonstrated reuse or an actual correctness boundary. Keep dependency upgrades and broad formatting separate from behavioral changes.

## 5. Verification: preserve confidence with less duplication

Architecture owns testability and engineering gates; document #6 owns the full coverage/evaluation plan and evidence. Give a compact matrix: **risk/invariant → authoritative verification layer → test boundary/fixture → change trigger → evidence limitation**. Reference representative FR/NFR/RG and UX-US/UX-AC IDs without recreating the complete coverage catalog.

Use the least expensive layer that actually observes a failure. Many scenario IDs may map to one parameterized check; one scenario does not require separate unit, API, component, E2E, and screenshot tests. Every selected acceptance scenario still needs an appropriate verification method under #0.

Recommended allocation:

| Layer | Meaningful scope | Avoid |
| --- | --- | --- |
| MSTest unit tests | Branch-heavy pure policy: accounting-period decisions, routing precedence, bounded budgets, counting/correspondence algorithms that live in .NET | Entity getters, DTO construction, trivial delegation, testing EF or Identity internals |
| MSTest API integration | Real host, middleware/validation/authorization, real disposable file-backed SQLite with migrations, accounting constraints/races, idempotency, failure and recovery | Mocked DbSet/repository tests presented as database evidence; one shared mutable test DB |
| Contract checks | Actual HTTP responses/status/headers/auth/error behavior against the independently owned OpenAPI contract; client generation/type compilation and relevant fixture conformance | A second deployment/test framework or duplicating all business assertions |
| Vitest unit/component | Reducers, debounce/IME guards, stale/manual-edit protection, sentence/cache invalidation, usage ordering, nontrivial control interactions | Whole-tree snapshots, asserting hook internals, shallow render tests for every component |
| Playwright | Core user journeys and browser-only behavior: real wiring, navigation, editing/caret/clipboard, focus, responsive surfaces, teardown/bfcache, representative race integration | Repeating every pure state combination in every browser or using real third-party UI/provider services in ordinary CI |
| Release/change-triggered checks | Real auth/email/provider integrations, multilingual quality, production-like performance, restore/deployment, supported-browser and manual accessibility evidence | Treating fixture output, Playwright WebKit, or a mocked timer as proof of provider quality, real Safari/device behavior, or actual latency |

Use MSTest unit and integration projects under `backend/tests`; contract tests can be a folder/category in the integration project sharing `WebApplicationFactory` and fixtures. Add a third project only for a demonstrated isolation/tooling need. Share test setup without sharing mutable state. Use real separate connections and controlled synchronization for concurrency; use restart/fault boundaries for recovery evidence. Reuse production migrations. Test an upgrade from the previous supported schema when a migration changes rather than repeatedly testing every historical schema on unrelated edits.

Distinguish two Playwright modes: focused UI contract tests may intercept application HTTP; a small integrated journey set must run the real frontend, backend, auth/accounting wiring, and SQLite with only external provider/email/Google boundaries replaced. UI interception does not prove end-to-end backend integration. Share contract-valid synthetic fixtures, with no live credentials, paid LLM requests, external Google UI, or email inbox dependency in ordinary CI. Keep a real independent API-consumer check for FR-036/RG-005.

Preserve strong regression checks for double-charge/overrun, midnight boundaries, interrupted outcomes and cost exposure, authorization isolation, lost/stale/manual-edit text, targeted sentence invalidation, private-data persistence, and destructive migrations. Use fake time, deferred responses, barriers, and auto-waiting assertions instead of sleeps. Retry policy must expose flakiness, not turn a repeated failure into accepted behavior.

Apply change-based gates: formatting/type/build and the relevant deterministic suites for ordinary code changes; broader integration/contracts when shared boundaries change; cross-browser/visual checks when browser-sensitive UI changes; actual language evaluation when provider/model/prompt/output checks change; performance and operational evidence when their relevant implementation/configuration changes and at release. Run a broader deterministic baseline before merge/release as appropriate. A passing suite need not be rerun without a relevant change, new failure, or unresolved concern. Reuse evidence only when its code/config/environment remain applicable.

Do not impose a line-coverage percentage, test count, unit test per class, all-layer testing per story, mandatory test-first ceremony, mutation testing, snapshot-everything policy, or exhaustive Cartesian browser/style/language matrix. Do not write tests for reversible low-impact edits already adequately checked by types, lint, or existing acceptance coverage. Add regression tests for behavioral defects when they can detect recurrence. Never rewrite accepted behavior merely to make a brittle assertion pass; fix the assertion only when the owning contract supports the correction.

**Existing UX obligations need explicit treatment.** Review #2 §§10.5, 12.1, 12.10, and 14. They already prescribe extensive lanes, parameterization, and screenshot baselines. Propose a concrete reduction to representative visual compositions and browser-sensitive journeys, with a compact mapping of displaced assertions to equivalent semantic/component/integration checks. Keep all observable behaviors and stable scenario IDs, and preserve supported-browser and human release commitments. Fewer tests must not mean silently uncovered scenarios.

The user has explicitly authorized these targeted verification amendments. Apply the minimal changes to the owning sections of #2, record provenance/version changes, and align #3 in the same generation task. Include the displaced-check mapping and retain requirements/scenario traceability; this authorization does not permit changes to observable UX behavior, supported-browser commitments, or required human release checks. Resolve any broader change separately instead of asking again about the already authorized reduction. Do not weaken PRD release gates to achieve “full auto.” Required human language and assistive-technology checks remain explicit release dependencies.

## 6. Required output and completion check

Write a readable, concrete document with:

1. Metadata, scope, source revisions, authority, assumptions, and decision status.
2. Architecture drivers and a short rationale for the selected stack and rejected complexity.
3. System/deployment diagram, dependency rules, and one repository tree.
4. Backend/frontend component responsibilities and a representative slice flow.
5. Identity/security, API/LLM integration boundaries, configuration, and secrets.
6. Data classification, SQLite/migration/backup lifecycle, and workspace/privacy design.
7. Operation/accounting/cost state transitions, atomic boundaries, races, and crash recovery.
8. Frontend state/correspondence strategy and deterministic engineering rules.
9. Proportionate verification matrix, change-based checks, and agent execution conventions.
10. Consequential decision summaries, unresolved questions with owners/blocking gates, cross-document handoffs, and a truthful readiness statement.

Combine sections where it improves readability. Prefer tables and small Mermaid diagrams for boundaries, lifecycles, and competing events. Use precise bounded prose. Do not pad the document with generic Clean Architecture/DDD tutorials, duplicate UX copy or policy constants, exhaustive code listings, speculative roadmap items, or implementation task lists.

Create or update `docs/09-architecture-decisions.md` only for consequential technical decisions actually made here, as required by #0. Keep rationale/history there and current design in #3; unresolved choices are proposed ADRs, not accepted decisions. Do not manufacture approval. Apart from #3, necessary ADRs, and explicitly authorized targeted verification amendments, do not create/change other planning documents or application code. Planned handoffs to #4/#5/#6 are references to future work, not claims those files exist.

Before delivery, check source/requirement IDs, local links, decision statuses, internal consistency, concrete ownership, and Markdown/diffs. Specifically challenge the design against: concurrent last-allowance requests; stale success after manual edit; repeated alternatives; identical repeated sentences; a request crossing midnight; disconnect/restart after provider dispatch; response loss after charge commit; missing production volume; deploy with pending migration; and refresh/bfcache restoration of private text. Use this as a design review, not a claim that runtime tests passed.

State what is resolved, what is proposed, which external facts remain unverified, and exactly which implementation/release gate is blocked. The authoring task can finish with documented external dependencies; dependent implementation cannot be called ready prematurely. In the final response, link the created document and any necessary companion changes, summarize major choices/deviations, and report only validation actually performed.

## 7. Official reference starting points

These sources informed this prompt on 2026-09-07. Recheck relevant details against the selected versions during architecture authoring. They inform implementation choices; they do not override LinguaDesk's requirements.

- [NuGet Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) — centralized package versions and configuration scope.
- [.NET SDK/MSBuild properties](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props) — analyzer levels and build settings.
- [MSTest configuration](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-configure) and [ASP.NET Core integration testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0) — runner setup and reusable application test hosts.
- [EF Core SQLite limitations](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations) and [applying migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying) — schema operations, locking, and deployment choices.
- [SQLite WAL](https://www.sqlite.org/wal.html) and [online backup](https://www.sqlite.org/backup.html) — filesystem and backup constraints.
- [ASP.NET Core Identity for SPAs](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0) — cookie/token options and their limits.
- [ASP.NET Core static files](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0), [MapFallbackToFile](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.builder.staticfilesendpointroutebuilderextensions.mapfallbacktofile?view=aspnetcore-10.0), and [Vite production builds](https://vite.dev/guide/build.html) — serving the published SPA from the API host.
- [Vitest guide](https://vitest.dev/guide/) — Vite-integrated testing.
- [typescript-eslint configurations](https://typescript-eslint.io/users/configs/) — recommended type-aware rules without indiscriminately enabling every strict/style rule.
- [Playwright best practices](https://playwright.dev/docs/best-practices) — user-visible assertions, isolation, and control of third-party dependencies.
