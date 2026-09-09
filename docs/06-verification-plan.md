# LinguaDesk — Verification Plan

**Document:** #6 · **Version:** 1.12 · **Status:** Current design; implementation/evidence status is maintained in delivery/current.md and verification/coverage.md
**Updated:** 2026-09-09

## 1. Authority, ownership and scope

This is the canonical verification plan and requirement-to-evidence matrix required by [planning workflow #0](00-SDD-Planning-Workflow.md). It specifies Q-005's corpus, rubric, human coverage and workloads. These are engineering verification decisions under #0, not new product thresholds or evidence of a passing release.

| Owner | What stays authoritative there | Verification detail owned here |
| --- | --- | --- |
| [PRD #1](01-PRD.md), Sections 5–8 | Product scope, priorities, limits, quality/performance thresholds and RG-001–008 | Test allocation, denominators, workloads, review procedure and evidence |
| [UX #2](02-ux-specification.md), Sections 3–12 | Workspace behavior, messages, accessibility/browser support and stable UX-AC contracts | Fixtures, automated lanes, manual execution, visual baselines and coverage matrix formerly in Sections 10–14 |
| [Architecture #3](03-architecture.md), Sections 2–8 | Stack, dependencies, testability, migration and production integrity constraints | Layer strategy and deterministic harness formerly in Section 9 |
| [LLM #4](04-llm-specification.md), Sections 2–10 | Shared Infrastructure.Ai boundary, runtime contracts, candidate eligibility and stable LLM-AC contracts | Standalone workflows, evaluation tooling, reports and qualification procedure formerly in Section 9 |
| [API #5](05-api-design.md) | Counting, authentication, idempotency, recovery, accounting and stable API-AC contracts | Assertion allocation and contract-generation verification |
| [ADRs #9](09-architecture-decisions.md) | Decision rationale | Current execution procedures are here; rationale is not a second test plan |

Only active MVP acceptance is tested for release. Preserve deferred/retired IDs without placeholder tests. A compound requirement creates work only for its active branch. NFR-007 remains Should; P-005/NFR-008 and P-006 remain proposed. Test already accepted authentication, isolation, credentials, privacy and instruction-as-content contracts without inventing abuse limits or compatibility obligations. Provider retention/no-training certification is not a gate under D-18; application text privacy and monetary admission remain required.

Current implementation and completed evidence are indexed in [delivery status](delivery/current.md) and [coverage](verification/coverage.md). Planned check names are not necessarily existing commands. Document review cannot discharge a release gate.

## 2. Verification layers and check catalog

### 2.1 Layer ownership

The broadest set of cases belongs to pure units, followed by focused DOM component tests. Integration suites are smaller and exercise real boundaries. Browser journeys are the smallest suite. This is a risk-based allocation, not a numeric coverage quota or an instruction to weaken data integrity checks.

| Layer | Owns | Does not establish |
| --- | --- | --- |
| MSTest pure units | Count/limit boundaries, reservation and settlement decisions, cost arithmetic, two-family chain validation/order, failure classification, deadline policy with fake time | SQL atomicity, migrations, HTTP/auth wiring |
| Vitest pure units | Reducers, explicit-dispatch/duplicate guards, no-auto-resume policy, stale-response and usage ordering, workspace/result-edit revisions | Browser editing/layout |
| Vitest + Testing Library DOM components | Visible messages, forms, control state, effect request counts, usage rendering, semantic names/live-region mutations | Real clipboard, layout, bfcache, native IME, complete accessibility |
| EF/SQLite integration | Queries, constraints, transaction rollback, concurrent admission/settlement, crash recovery metadata, migrations | Provider language quality or browser behavior |
| MSTest HTTP integration (`WebApplicationFactory`) | Routing, validation, auth/antiforgery, error/usage serialization, independent API operations, unknown API paths | Actual socket/TLS/static publish behavior |
| Playwright browser contracts | Small keyboard/focus/clipboard/editing/history/privacy/reflow set and curated visual baselines in Section 4 | Server accounting when API responses are intercepted |
| Playwright integrated smoke | Published SPA → real API/auth → migrated SQLite → deterministic provider adapter; one explicit Translation and one full-Rewrite journey with local sign-in | Live email/provider service reliability |
| Separate release evidence (#6) | Live local-account/email smoke, provider quality/performance/cost eligibility, manual browser/assistive-technology checks | Cannot be replaced by fixture success |

### 2.2 Stable check groups

V-IDs name shared verification groups, not backlog tasks. Selected packages refine them into concrete test names and evidence paths; keep these IDs when implementation is split. Coverage is by assertion: a fake UI response cannot prove charging, and a database test cannot prove caret behavior.

| Check | Assertions and principal evidence |
| --- | --- |
| V-001 | Shared scalar/whitespace/count/limit fixtures in C# and TypeScript; pure validation plus HTTP no-provider rejection boundary |
| V-002 | Frontend reducers/revisions, explicit activation and duplicate guards, composition events, no auto-resume, stale success/failure and manual-edit protection; Vitest units |
| V-003 | Forms, selectors, defaults, messages, request counts, displayed usage and live-region mutations; Vitest/Testing Library components |
| V-004 | Real local registration, cookie/bearer handlers, verification/reset/refresh/stamp invalidation, antiforgery, safe redirects and non-enumerating responses; HTTP/account integration |
| V-005 | Atomic idempotency, daily allowances, charges, ordered snapshots, rollover, failure windows and restart reconciliation; policy units plus file-backed SQLite/HTTP |
| V-006 | Cost arithmetic/admission, every paid attempt, cache usage, unknown exposure, cross-month carryover and durable concurrent ceiling; pure and file-backed SQLite checks |
| V-007 | Independent AI build, config/prompt/parser/validator/chain/deadline behavior and runner budget/report semantics; offline MSTest with scripted IChatClient and fake time/admission |
| V-008 | Actual adapter serialization, thinking/JSON/output settings, error/usage mapping, cancellation, bounded transport and dispatch counts; sanitized HTTP fixtures plus explicit live capability evidence |
| V-009 | Independent authenticated API consumer, wire errors/recovery and generated schema/client drift; HTTP integration and reproducible contract generation |
| V-010 | Native editing/caret/selection/clipboard, focus/keyboard/IME events, history/storage/bfcache and reflow; focused browser contracts |
| V-011 | Seven curated visual baselines, semantic/accessibility scans, actual browser/device/AT checks and manual design review |
| V-012 | Published SPA/API/local cookie auth/migrated database with fake external adapters; small integrated smoke, routing/static delivery/TLS and lifecycle checks |
| V-013 | Each candidate's fixed quality/eligibility corpus, AI grading and human review; live qualification and configured-chain evidence |
| V-014 | Real API performance at defined concurrency, maximum-length requests, fallback/deadline evidence; measured load and controlled fault runs |
| V-015 | Synthetic text/secret sentinels absent from application database/logs/traces/errors/storage/history/cache and retained backups; privacy/lifecycle inspection and tests |
| V-016 | Fresh/upgrade migrations, model drift, published restart/backup restore, live account/email smoke, specification readiness and assembled release evidence |

## 3. Deterministic backend and independent AI verification

See [Backend and independent AI verification](verification/backend.md#3-deterministic-backend-and-independent-ai-verification).

### 3.1 Database, HTTP and published-host harness

See [Backend and independent AI verification](verification/backend.md#31-database-http-and-published-host-harness).

### 3.2 High-risk accounting and privacy cases

See [Backend and independent AI verification](verification/backend.md#32-high-risk-accounting-and-privacy-cases).

### 3.3 Independent AI workflows

See [Backend and independent AI verification](verification/backend.md#33-independent-ai-workflows).

## 4. Frontend, browser and manual verification

See [Frontend and browser verification](verification/frontend.md#4-frontend-browser-and-manual-verification).

### 4.1 Shared UI fixtures

See [Frontend and browser verification](verification/frontend.md#41-shared-ui-fixtures).

### 4.2 Browser lanes

See [Frontend and browser verification](verification/frontend.md#42-browser-lanes).

### 4.3 Manual accessibility and device evidence

See [Frontend and browser verification](verification/frontend.md#43-manual-accessibility-and-device-evidence).

### 4.4 Curated visual regression

See [Frontend and browser verification](verification/frontend.md#44-curated-visual-regression).

## 5. LLM quality and candidate qualification (Q-005)

See [LLM quality and performance verification](verification/llm-evaluation.md#5-llm-quality-and-candidate-qualification-q-005).

### 5.1 Versioned corpus and reference construction

See [LLM quality and performance verification](verification/llm-evaluation.md#51-versioned-corpus-and-reference-construction).

### 5.2 Rubric and denominator

See [LLM quality and performance verification](verification/llm-evaluation.md#52-rubric-and-denominator).

### 5.3 AI grading and human review

See [LLM quality and performance verification](verification/llm-evaluation.md#53-ai-grading-and-human-review).

### 5.4 Qualification and changes

See [LLM quality and performance verification](verification/llm-evaluation.md#54-qualification-and-changes).

## 6. Performance and workload composition (Q-005)

See [LLM quality and performance verification](verification/llm-evaluation.md#6-performance-and-workload-composition-q-005).

### 6.1 API benchmark

See [LLM quality and performance verification](verification/llm-evaluation.md#61-api-benchmark).

### 6.2 Maximum length, fallback and failure deadlines

See [LLM quality and performance verification](verification/llm-evaluation.md#62-maximum-length-fallback-and-failure-deadlines).

## 7. Execution gates and evidence

### 7.1 When to run

| Stage | Required verification |
| --- | --- |
| Inner loop | Affected pure/backend/component tests, type checks and analyzers; focused persistence/HTTP checks for changed boundaries. Independent AI work uses V-007/V-008 offline without API/UI startup. |
| Pull request | All fast tests, backend build/integration, frontend production build, schema/client drift for existing generated contracts, and the small integrated Chromium smoke. Changed UI runs affected browser contracts/visuals; shared styles, harness or hosting changes run the whole relevant set. Uncertain selection means run the relevant suite in full. |
| Candidate qualification | Explicit budgeted live adapter/corpus/human/performance evidence for affected candidates; deterministic chain/validator checks first. These runs are outside ordinary PR feedback. |
| Release candidate | All applicable PR checks plus actual published-host/migration/restore/restart evidence, live account/email flow, complete supported browser/device/AT matrix, candidate qualification, workload/cap/privacy verification and RG-001–008 evidence review. |

During scaffolding, absent commands/harnesses are pending, not passing or quietly skipped. Each selected slice implements the relevant checks before being called complete; it need not build every future suite. When the first API slice is selected, generate OpenAPI 3.1 JSON from actual C# contracts/endpoint metadata, deterministically convert to `docs/05-openapi.yaml`, review it, and generate the typed client. V-009 repeats generation to detect drift and proves generation has no live database migration, email or provider effects. A clean diff proves reproducibility, not handler semantics; test those separately. No handwritten all-MVP YAML or contract-only duplicate host is introduced by this plan.

### 7.2 Evaluation report and data boundary

Use MSTest for deterministic behavior. `Microsoft.Extensions.AI.Evaluation` and its Quality evaluators are suitable optional runner dependencies for AI-assisted grading and custom `IEvaluator` implementations. Microsoft's reporting components can store responses, so do not adopt default caching/storage behavior without explicitly configuring the evaluation-only data boundary. These packages never become serving dependencies. [Microsoft evaluation libraries](https://learn.microsoft.com/en-us/dotnet/ai/evaluation/libraries)

Sections 5–6 specify Q-005. Every evaluation report must supply:

- Stable case IDs, corpus revision/hash and provenance; operation, source/target or mode, source fixture, expected eligibility and fidelity assertions/reference notes. Include all directions/languages/modes, both Chinese scripts, ambiguity, short texts, mixed/unsupported content, corrections, names, numbers, URLs and paragraph/list structure.
- A run manifest with code/SDK/package revision, prompt/validator/settings hashes, configured and observed model identity, provider endpoint profile, billing snapshot, case selection, concurrency, timestamps and live/fixture/cache disposition.
- Per-case outcome, rejection/failure category, attempts by stage/candidate, full pipeline and stage durations, token usage/cache evidence, known spend/unresolved exposure, deterministic findings, grader/rubric revision and human review disposition. Never omit failed calls from latency/quality reporting or count failures as timely successful completions.
- Per-route/language and operation aggregation with mode breakdowns and critical-error tracking. Grade both isolated candidates and configured-chain behavior; the same thresholds apply to fallback. AI grades assist human review and cannot satisfy the required human coverage alone.
- Candidate-to-baseline comparisons on the same case IDs; no exact-output equality requirement for live rewriting/translation and no claim that low temperature makes runs reproducible byte for byte.

Persist corpus text/reference answers and generated outputs only for intentionally authored synthetic or approved public evaluation fixtures, in explicitly designated evaluation artifacts. Never import real workspace submissions/results or production traces. Reports from ad hoc/private input contain metadata only and do not persist model text or grader explanations that quote it. Do not store hidden reasoning. Application response caching remains absent; disable runner response reuse for fresh qualification/performance evidence and mark any fixture replay clearly.

Standalone latency exposes model/pipeline performance but does not prove NFR-002's API-submission-to-complete-response benchmark. Section 6 measures the real API, admissions/settlement and concurrency using the agreed workload. Report provider cache-hit/miss conditions, fallback, maximum input and timeout cases. A cached response replay or scripted client cannot establish live quality, cost or latency.

### 7.3 Evidence records and failure handling

Each deterministic/manual/release record includes check and acceptance IDs, selected package/task once it exists, commit/configuration, command or manual procedure, environment/tool/browser/device versions, UTC timestamp, outcome, artifact links and unresolved limitations. Distinguish **Pending** (not executed), **Blocked** (named dependency), **Failed**, **Passed** and **Not applicable** (explicit inactive scope). A passing document check or fixture cannot be relabeled as live evidence. Keep sanitized logs, screenshots, measurement data and reviewer decisions sufficient to reproduce the conclusion; secrets and production text are excluded.

On failure, retain the original result, classify defect versus harness/environment problem, and rerun the affected checks after a recorded fix. Do not accept changed visual baselines automatically or discard unfavorable LLM generations. Report flaky checks as unresolved until diagnosed. Before release, assemble one evidence index for the candidate commit/configuration and each RG row below; earlier evidence needs an explicit impact assessment after changes. Evidence retention/access and actual storage paths are selected with the implementing package/#10 under #3's data policy, not invented as deployed infrastructure here.

## 8. Canonical coverage and acceptance allocation

See [Coverage and acceptance allocation](verification/coverage.md#8-canonical-coverage-and-acceptance-allocation).

### 8.1 Product and release coverage

See [Coverage and acceptance allocation](verification/coverage.md#81-product-and-release-coverage).

### 8.2 Local acceptance allocation

See [Coverage and acceptance allocation](verification/coverage.md#82-local-acceptance-allocation).

## 9. Readiness and remaining dependencies

See [Verification readiness and dependencies](verification/readiness.md#9-readiness-and-remaining-dependencies).
