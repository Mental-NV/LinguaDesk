# LinguaDesk — Verification Plan

**Document:** #6 · **Version:** 1.18 · **Status:** Current design; unattended AI verification and Chromium release evidence; implementation/evidence status is maintained in delivery/current.md and verification/coverage.md
**Updated:** 2026-10-26 (Europe/Moscow)

## 1. Authority, ownership and scope

This is the canonical verification plan and requirement-to-evidence matrix required by [planning workflow #0](00-SDD-Planning-Workflow.md). It specifies Q-005's corpus, rubric, automated review coverage and workloads. These are engineering verification decisions under #0, not new product thresholds or evidence of a passing release. PRD D-22 permits AI review, grading/adjudication, visual-baseline disposition and release eligibility without human verification/sign-off. Required browser evidence uses pinned Chromium desktop/narrow viewports; additional engines/versions/devices/AT and human checks are optional and nonblocking. D-22 leaves corpus/workload counts and quality/performance thresholds unchanged. D-24 subsequently adopts the explicit [expanded-language template/smoke matrix](verification/llm-evaluation.md#55-expanded-language-matrix-d-24), avoiding exhaustive Translation pair evaluation.

| Owner | What stays authoritative there | Verification detail owned here |
| --- | --- | --- |
| [PRD #1](01-PRD.md), Sections 5–8 | Product scope, priorities, limits, quality/performance thresholds and RG-001–008 | Test allocation, denominators, workloads, review procedure and evidence |
| [UX #2](02-ux-specification.md), Sections 3–12 | Workspace behavior, messages, accessibility/browser support and stable UX-AC contracts | Fixtures, automated lanes, optional manual observations, visual baselines and coverage matrix formerly in Sections 10–14 |
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
| Vitest pure units | Reducers, one-second pause/duplicate guards, no availability-only auto-resume, stale-response and usage ordering, workspace/result-edit revisions | Browser editing/layout |
| Vitest + Testing Library DOM components | Essential alerts, forms, selectors, effect request counts, removed-control absence, semantic names/live-region mutations | Real clipboard, layout, bfcache, native IME, complete accessibility |
| EF/SQLite integration | Queries, constraints, transaction rollback, concurrent admission/settlement, crash recovery metadata, migrations | Provider language quality or browser behavior |
| MSTest HTTP integration (`WebApplicationFactory`) | Routing, validation, auth/antiforgery, error/usage serialization, independent API operations, unknown API paths | Actual socket/TLS/static publish behavior |
| Playwright browser contracts | Small keyboard/focus/clipboard/editing/history/privacy/reflow set and curated visual baselines in Section 4 | Server accounting when API responses are intercepted |
| Playwright integrated smoke | Published SPA → real API/auth → migrated SQLite → deterministic provider adapter; paused Translator and Write journeys plus explicit alternatives generation/reuse with local sign-in | Live email/provider service reliability |
| Separate release evidence (#6) | Automated current local-account smoke, live provider quality/performance/cost eligibility and published migration/restore/privacy checks; real email deferred under DF-008 | Required live evidence cannot be replaced by fixture success; human/device/AT review is optional |

**M047 GitHub CI policy — user direction, 2026-10-26:** GitHub Actions runs deterministic/offline checks only, including the published Chromium harness with controlled providers. Never invoke live LLM evaluation, live access probes or paid performance tests in GitHub CI or postdeploy smoke. Provider runtime secrets belong only to environment-scoped deployment configuration. Required live release evidence remains a separate authorized activity and is not satisfied by offline CI. See [M047](08-backlogs/M047/plan.md).

### 2.2 Stable check groups

V-IDs name shared verification groups, not backlog tasks. Selected packages refine them into concrete test names and evidence paths; keep these IDs when implementation is split. Coverage is by assertion: a fake UI response cannot prove charging, and a database test cannot prove caret behavior.

| Check | Assertions and principal evidence |
| --- | --- |
| V-001 | Shared scalar/whitespace/count/limit fixtures in C# and TypeScript; pure validation plus HTTP no-provider rejection boundary |
| V-002 | Frontend reducers/revisions, one-second timer restart/cancellation on result edit or route leave, duplicate guards, composition events, no availability-only auto-resume, stale success/failure and manual-edit protection; Vitest units |
| V-003 | Forms, selectors, defaults, essential errors, request counts, absence of removed controls/status lines and assistive live-region mutations; Vitest/Testing Library components |
| V-004 | Real local registration, cookie/bearer handlers, verification/reset/refresh/stamp invalidation, antiforgery, safe redirects and non-enumerating responses; HTTP/account integration |
| V-005 | Atomic idempotency, daily allowances, charges, ordered snapshots, rollover, failure windows and restart reconciliation; policy units plus file-backed SQLite/HTTP |
| V-006 | Cost arithmetic/admission, every paid attempt, cache usage, unknown exposure, cross-month carryover and durable concurrent ceiling; pure and file-backed SQLite checks |
| V-007 | Independent AI build, config/prompt/parser/validator/chain/deadline behavior and runner budget/report semantics; offline MSTest with scripted IChatClient and fake time/admission |
| V-008 | Actual adapter serialization, thinking/JSON/output settings, error/usage mapping, cancellation, bounded transport and dispatch counts; sanitized HTTP fixtures plus explicit live capability evidence |
| V-009 | Independent authenticated API consumer, wire errors/recovery and generated schema/client drift; HTTP integration and reproducible contract generation |
| V-010 | Native editing/caret/selection/clipboard, focus/keyboard/IME events, history/storage/bfcache and reflow; focused browser contracts |
| V-011 | Seven curated Chromium visual baselines, semantic/accessibility scans and AI design review; additional-browser/device/AT and human observations optional |
| V-012 | Published SPA/API/local cookie auth/migrated database with fake external adapters and the dedicated verified E2E account; at least one real login-form journey, reusable real-auth browser state for other cases, and visible-outcome coverage for every selected UI-facing feature; routing/static delivery/TLS and lifecycle checks |
| V-013 | Each candidate's fixed quality/eligibility corpus, pinned AI grading, separate reference/review/adjudication roles; live qualification and configured-chain evidence |
| V-014 | Real API performance at defined concurrency, maximum-length requests, fallback/deadline evidence; measured load and controlled fault runs |
| V-015 | Synthetic text/secret sentinels absent from application database/logs/traces/errors/storage/history/cache and retained backups; privacy/lifecycle inspection and tests |
| V-016 | Fresh/upgrade migrations, model drift, published restart/backup restore, automated current-account smoke, specification readiness and assembled release evidence; real email excluded until DF-008 reactivation |

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

### 4.3 Manual browser-matrix evidence

See [Frontend and browser verification](verification/frontend.md#43-manual-browser-matrix-evidence).

### 4.4 Curated visual regression

See [Frontend and browser verification](verification/frontend.md#44-curated-visual-regression).

### 4.5 Required user-visible transformation evidence

Translation and Rewriting are separate user outcomes. Each requires focused state/component checks **and** a published integrated browser journey. The integrated journey runs the built SPA through the real local cookie-auth, API, migrated SQLite and durable accounting/recovery boundaries. It may use the deterministic provider adapter so functional UI evidence is repeatable, but it must not intercept the application's `/api` calls or replace the workspace with a test-only screen. Live model quality remains V-013 and is not inferred from deterministic text.

**Dedicated E2E identity and database setup:** the canonical published-suite account is the synthetic verified local user `m013-verified@example.test` with password `Maple!River2026`. These fixed values are test data, not secrets, and must never identify a production account. Before the published host serves browser cases, the harness applies migrations to a fresh isolated SQLite database and creates this account through ASP.NET Identity with `EmailConfirmed = true`. Seeding is available only in the `Smoke` environment, runs as a bounded setup step rather than a public endpoint, and must fail closed in every other environment. The temporary database and data-protection keys are owned by the run and removed afterward. The account starts with the documented usage/availability baseline; feature-created operations are the only changes counted by the case. The password value must not appear in host logs, test reports, traces or screenshots; the harness scans produced text artifacts and fails if it is exposed.

**Authentication paths:** at least one published E2E case starts signed out at `/login`, fills the predefined email/password, activates **Sign in**, and asserts successful protected-route navigation plus visible authenticated content. Other cases may avoid repeating the form by using a documented Playwright authentication fixture that obtains antiforgery state and signs in the same predefined user through the real published account API, then supplies the resulting same-origin cookie storage state to the browser context. The bypass may skip only form interaction: it may not inject or forge cookies, write session records directly, disable auth/antiforgery/authorization middleware, add a test-only login endpoint, intercept account/operation APIs or use an in-memory replacement host.

**UI assertion rule:** every selected UI-facing feature has one or more published E2E cases operating as the predefined user. Each case exercises the source/settings controls through semantic browser locators and asserts the visible outcome—for example route/heading, essential alert, source/result value, focus or native clipboard result—in addition to timer request-count and database/accounting assertions. A 2xx response, persisted row, trace entry or component test without the corresponding browser-visible outcome does not satisfy the feature.

| User-visible outcome | Milestone and required evidence | Minimum observed behavior before Done | Insufficient on its own |
| --- | --- | --- | --- |
| Translate text in the UI | M028/M044/M039 historical evidence plus an unselected D-23 follow-up; V-001/V-002/V-003 focused checks, V-010 native browser checks and V-012 dedicated-user published E2E cases | The predefined verified user opens `/translate`, chooses a valid source/target, enters text, observes no request before one second and one current request after the pause, receives one complete result, edits it and natively copies its exact value. Oversize/invalid input and a controlled definitive failure preserve the source and prior result without a successful-operation charge; only controls still excluded by D-23 and persistent status lines are absent. | M016 AI output, M026 HTTP success, generated client types, a mocked component response, a route screenshot or E2E network assertions without UI assertions |
| Rewrite text in the UI | M029/M044/M039 historical evidence plus an unselected D-23 follow-up; V-001/V-002/V-003 focused checks, V-010 native browser checks and V-012 dedicated-user published E2E cases | The predefined verified user opens `/rewrite`, sees **Correction Only** on by default, turns it off to select a supported Styles/Tones choice when desired, enters text, observes one current request after the one-second pause, receives one complete same-language result, and edits/natively copies it while the source remains unchanged. Oversize/invalid input and a controlled definitive failure preserve work without a successful-operation charge; only controls still excluded by D-23 and persistent status lines are absent. | M017 AI output, M027 HTTP success, generated client types, a mocked component response, a route screenshot or E2E network assertions without UI assertions |
| Review and refine Write output | Unselected D-23 packages; V-002/V-003/V-005/V-007/V-009/V-010/V-011/V-012 and independent V-013/V-014 live qualification | The verified user expands tools, toggles Show changes, activates a current sentence, receives two alternatives through the real API/ledger, selects/reuses one without a new charge, copies clean text and edits with whole-metadata invalidation. Pending/stale/failure states preserve work. | Full-rewrite success, UI fixtures without an alternatives endpoint, or a live provider response without the integrated user journey |
| Keep both journeys trustworthy | M030–M032/M044/M039 historical evidence plus an unselected D-23 follow-up; V-002/V-003/V-005/V-010/V-015 plus dedicated-user V-012 regressions | Navigation, competing callbacks, timer races, manual edits, unknown outcomes and session teardown preserve current workspace/accounting rules across both features, with each selected UI behavior asserted in the published browser. | One happy-path browser trip or backend-only stale/accounting tests |

Each current milestone record identifies the route, controls, timer/dispatch count, API operation/status, displayed result, accounting transition, edit/native-copy observation and failure-path result. M028/M029 retain historical explicit-UI evidence and M044/M039 retain D-21 evidence; D-23 needs newly selected implementation/evidence for panels, toolbar, review, alternatives and copy/swap. V-013/V-014, Section 4.2–4.4 and RG-002/003/006 still govern live quality, performance and the complete browser/device/accessibility claim.

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
| Candidate qualification | Explicit budgeted live adapter/corpus/AI-review/performance evidence for affected candidates; deterministic chain/validator checks first. AI schedules runs within standing profile/environment/dispatch/spend authorization. These runs are outside ordinary PR feedback. |
| Release candidate | All applicable PR checks plus automated published-host/migration/restore/restart and current local-account evidence, required Chromium desktop/narrow checks, candidate qualification, workload/cap/privacy verification and AI review of RG-001–008. No human sign-off or browser/version/device/AT matrix is required; real email remains deferred under DF-008. |

During scaffolding, absent commands/harnesses are pending, not passing or quietly skipped. Each selected slice implements the relevant checks before being called complete; it need not build every future suite. When the first API slice is selected, generate OpenAPI 3.1 JSON from actual C# contracts/endpoint metadata, deterministically convert to `docs/05-openapi.yaml`, review it, and generate the typed client. V-009 repeats generation to detect drift and proves generation has no live database migration, email or provider effects. A clean diff proves reproducibility, not handler semantics; test those separately. No handwritten all-MVP YAML or contract-only duplicate host is introduced by this plan.

AI records impact-based evidence reuse for unchanged assertions/configurations; unrelated UI/documentation changes do not require new model qualification. One run may satisfy overlapping development/qualification/release checks when its exact coverage and measurements suffice. Do not repeat paid runs solely for a stage label. Changed behavior requires affected checks; missing required evidence still blocks eligibility. Existing executable commands may need follow-up work to implement this policy; this amendment does not claim new runners or passing tests.

### 7.2 Evaluation report and data boundary

Use MSTest for deterministic behavior. `Microsoft.Extensions.AI.Evaluation` and its Quality evaluators are suitable optional runner dependencies for AI-assisted grading and custom `IEvaluator` implementations. Microsoft's reporting components can store responses, so do not adopt default caching/storage behavior without explicitly configuring the evaluation-only data boundary. These packages never become serving dependencies. [Microsoft evaluation libraries](https://learn.microsoft.com/en-us/dotnet/ai/evaluation/libraries)

Sections 5–6 specify Q-005. Every evaluation report must supply:

- Stable case IDs, corpus revision/hash and provenance; operation, source/target or mode, source fixture, expected eligibility and fidelity assertions/reference notes. Include all directions/languages/modes, both Chinese scripts, ambiguity, short texts, mixed/unsupported content, corrections, names, numbers, URLs and paragraph/list structure.
- A run manifest with code/SDK/package revision, prompt/validator/settings hashes, configured and observed model identity, provider endpoint profile, billing snapshot, case selection, concurrency, timestamps and live/fixture/cache disposition.
- Per-case outcome, rejection/failure category, attempts by stage/candidate, full pipeline and stage durations, token usage/cache evidence, known spend/unresolved exposure, deterministic findings, grader/rubric revision and automated review/adjudication disposition, including role/model/run identities and limitations. Never omit failed calls from latency/quality reporting or count failures as timely successful completions.
- Per-route/language and operation aggregation with mode breakdowns and critical-error tracking. Grade both isolated candidates and configured-chain behavior; the same thresholds apply to fallback. Pinned AI grading and separate review/adjudication satisfy required coverage under D-22; human observations, if supplied, retain their actual provenance.
- Candidate-to-baseline comparisons on the same case IDs; no exact-output equality requirement for live rewriting/translation and no claim that low temperature makes runs reproducible byte for byte.

Persist corpus text/reference answers and generated outputs only for intentionally authored synthetic or approved public evaluation fixtures, in explicitly designated evaluation artifacts. Never import real workspace submissions/results or production traces. Reports from ad hoc/private input contain metadata only and do not persist model text or grader explanations that quote it. Do not store hidden reasoning. Application response caching remains absent; disable runner response reuse for fresh qualification/performance evidence and mark any fixture replay clearly.

Standalone latency exposes model/pipeline performance but does not prove NFR-002's API-submission-to-complete-response benchmark. Section 6 measures the real API, admissions/settlement and concurrency using the agreed workload. Report provider cache-hit/miss conditions, fallback, maximum input and timeout cases. A cached response replay or scripted client cannot establish live quality, cost or latency.

### 7.3 Evidence records and failure handling

Each automated/release record and optional manual observation includes check and acceptance IDs, selected package/task once it exists, commit/configuration, command/procedure, environment/tool/browser/device versions as applicable, UTC timestamp, outcome, artifact links and unresolved limitations. Distinguish **Pending** (not executed), **Blocked** (named dependency), **Failed**, **Passed** and **Not applicable** (explicit inactive scope). Mark optional unexecuted observations **Pending — optional/nonblocking**, not Passed or Not applicable merely because no device/reviewer is available. A passing document check or fixture cannot be relabeled as live evidence. Keep sanitized logs, screenshots, measurement data and identifiable AI/human review decisions sufficient to reproduce the conclusion; secrets and production text are excluded.

On failure, retain the original result, classify defect versus harness/environment problem, and rerun the affected checks after a recorded fix. AI may accept intentional visual-baseline changes only after image review, a recorded design rationale and independent structural/semantic checks under Section 4.4; unexplained changes remain failing. Do not discard unfavorable LLM generations. Report flaky required checks as unresolved until diagnosed. Before release, assemble one evidence index for the candidate commit/configuration and each RG row below; earlier evidence needs an explicit AI impact assessment after changes. AI determines eligibility without human sign-off; deployment requires an authorized target and rollout/rollback limits. Evidence retention/access and actual storage paths are selected with the implementing package/#10 under #3's data policy, not invented as deployed infrastructure here.

## 8. Canonical coverage and acceptance allocation

See [Coverage and acceptance allocation](verification/coverage.md#8-canonical-coverage-and-acceptance-allocation).

### 8.1 Product and release coverage

See [Coverage and acceptance allocation](verification/coverage.md#81-product-and-release-coverage).

### 8.2 Local acceptance allocation

See [Coverage and acceptance allocation](verification/coverage.md#82-local-acceptance-allocation).

## 9. Readiness and remaining dependencies

See [Verification readiness and dependencies](verification/readiness.md#9-readiness-and-remaining-dependencies).
