# LinguaDesk — Verification Plan

**Document:** #6 · **Version:** 1.12 · **Status:** M001–M006 selected-scope evidence passed; release evidence pending
**Updated:** 2026-09-09

## 1. Authority, ownership and scope

This is the canonical verification plan and requirement-to-evidence matrix required by [planning workflow #0](00-SDD-Planning-Workflow.md). It specifies Q-005's corpus, rubric, human coverage and workloads. These are engineering verification decisions under #0, not new product thresholds or evidence of a passing release.

The authoring baseline is Git `0ac27aef9641e3bf8a23649268a513a0198b7332`: #0 v1.3, #1 v0.5, #2 v1.4, #3 v1.6, #4 v1.2, #5 v1.0 and #9 v1.6. Companion revisions clarify ownership and replace duplicated methods with links here.

| Owner | What stays authoritative there | Verification detail owned here |
| --- | --- | --- |
| [PRD #1](01-PRD.md), Sections 5–8 | Product scope, priorities, limits, quality/performance thresholds and RG-001–008 | Test allocation, denominators, workloads, review procedure and evidence |
| [UX #2](02-ux-specification.md), Sections 3–12 | Workspace behavior, messages, accessibility/browser support and stable UX-AC contracts | Fixtures, automated lanes, manual execution, visual baselines and coverage matrix formerly in Sections 10–14 |
| [Architecture #3](03-architecture.md), Sections 2–8 | Stack, dependencies, testability, migration and production integrity constraints | Layer strategy and deterministic harness formerly in Section 9 |
| [LLM #4](04-llm-specification.md), Sections 2–10 | Shared Infrastructure.Ai boundary, runtime contracts, candidate eligibility and stable LLM-AC contracts | Standalone workflows, evaluation tooling, reports and qualification procedure formerly in Section 9 |
| [API #5](05-api-design.md) | Counting, authentication, idempotency, recovery, accounting and stable API-AC contracts | Assertion allocation and contract-generation verification |
| [ADRs #9](09-architecture-decisions.md) | Decision rationale | Current execution procedures are here; rationale is not a second test plan |

Only active MVP acceptance is tested for release. Preserve deferred/retired IDs without placeholder tests. A compound requirement creates work only for its active branch. NFR-007 remains Should; P-005/NFR-008 and P-006 remain proposed. Test already accepted authentication, isolation, credentials, privacy and instruction-as-content contracts without inventing abuse limits or compatibility obligations. Provider retention/no-training certification is not a gate under D-18; application text privacy and monetary admission remain required.

The [M001 / package 001](../specs/001-backend-foundation/spec.md) backend host, [M002 / package 002](../specs/002-published-web-shell/spec.md) published signed-out shell, [M003 / package 003](../specs/003-durable-storage-foundation/spec.md) durable-storage foundation, [M004 / package 004](../specs/004-independent-ai-development/spec.md) offline AI boundary, [M005 / package 005](../specs/005-shared-input-capability-contract/spec.md) public capability/shared-input contract and [M006 / package 006](../specs/006-register-local-api-account/spec.md) local registration slice are implemented. M006 passed its real Identity registration, non-enumeration, unverified current-state policy, migration/key/readiness, secret-sentinel, generated-contract and retained regression portions. There is still no sign-in, confirmation completion/live delivery, editor, HTTP language submission, eligibility parser/decision, provider evidence or passing release evidence. Names outside completed packages identify planned checks, not necessarily existing commands or files. Document review cannot discharge a release gate.

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

### 3.1 Database, HTTP and published-host harness

Use `WebApplicationFactory`/in-process `TestServer` by default for HTTP boundary tests. Direct persistence tests can construct `DbContext` without an HTTP host. TestServer removes listening-port conflicts; determinism and parallel safety still require isolated database paths, users/configuration, fake time, and controlled external responses.

Use the same EF SQLite provider and migrations as production. File-backed databases in unique temporary directories with WAL are required for concurrency, locking, crash/restart, and migration tests. Each concurrent request has its own context/connection; use barriers/deferred fakes rather than sleeps. SQLite in-memory may be used for focused relational cases without file/locking claims, with an explicitly owned connection lifetime; EF's InMemory provider and mocked `DbSet` are not substitutes. Tests may share helpers, not mutable cross-test state.

Test fresh migration and upgrade from the last released schema with representative account/ledger data; before the first release, test fresh schema plus any data-transforming migration with its prior schema. Assert retained data and constraints and check pending model changes. Simulate failures at [architecture Section 7.2](03-architecture.md#72-failure-windows-and-limits-of-idempotency)'s commit boundaries; a small process-restart test verifies persistence survives beyond the host instance.

Use fake provider/email adapters and sanitized transport fixtures for adapter parsing/error tests. Most authorization tests may use a controlled principal, but focused tests must use the real configured cookie/bearer handlers, antiforgery, verification gate, and token flows. A test-auth handler alone cannot prove authentication security.

Browsers cannot connect to in-memory TestServer. Integrated smoke therefore launches an owned Kestrel process on an OS-assigned/discovered port, with isolated migrated storage and test-only external adapters. Seed a verified local test account and exercise the real cookie login; use loopback HTTPS with a test certificate trusted only by the harness when exercising Secure cookies. Check readiness, dispose the process on failure, and never replace the browser's `/api` calls in this suite. Test adapters must be inaccessible in production configuration. Fixture-only browser contracts remain distinct.

The in-process HTTP harness follows [ASP.NET Core integration testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0). Database fidelity follows [EF Core testing guidance](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy): alternate providers cannot prove production-provider behavior. The concrete isolation and workload rules above are LinguaDesk decisions.

### 3.2 High-risk accounting and privacy cases

V-005/V-006 exercise simultaneous identical identities, conflicting payloads, multiple users sharing the global ledger, separate request contexts, insufficient remaining capacity, rollback and duplicate/late settlement. Use #5's canonical matching and UUID validity rules, including equivalent JSON escapes, changed newlines/modes, account scoping, expired identity after cleanup, original-day character attribution and per-attempt monetary periods. Assert both returned outcomes and durable ledger/exposure state.

Inject failures before dispatch, after uncertain dispatch, before terminal commit and after commit before response delivery according to #3 Section 7.2. Restart with the same file to prove fencing and reconciliation; a missing status is never evidence of zero charge. Status reads and usage refreshes must make zero model calls. Exercise deadline-versus-success races and client disconnect independently. A successful stale result still settles once while frontend revision checks prevent text replacement.

V-015 uses distinctive synthetic source/result/secret sentinels and inspects database tables, logs, traces, error exports, browser URL/history/storage/cache, and release backup artifacts. Trigger success, rejection, timeout, adapter failure and interrupted recovery. Confirm permitted operational metadata remains usable without text. Provider-managed cache observations do not establish application persistence or reduce character charges. Account deletion/backup retention details still depend on Q-004; their absence blocks the affected readiness check rather than being assumed safe.

### 3.3 Independent AI workflows

`LinguaDesk.Infrastructure.Ai` and its tests must build/run without Api, EF/Identity, frontend, browser, production database, credentials or provider network. `LinguaDesk.Ai.Evaluation` uses the same composition, prompts, validators and configuration checks as serving. Offline CI disables outbound provider access; inspect prompts with synthetic input and test the adapter through its actual transport serialization. This boundary is required by #3/#4, not a second application architecture.

| Workflow | Inputs and effects | Completion evidence |
| --- | --- | --- |
| Build/fast AI tests | Ai/Core, scripted `IChatClient`, fake admission/time; no secrets/network | Deterministic MSTest results; failing assertions are nonzero exit |
| Inspect prompt / validate configuration | Synthetic fixture plus bundle/profile | Exact serialized prompt/settings inspectable locally; no provider call; invalid config is nonzero exit |
| Adapter conformance | Synthetic HTTP fixtures through real adapter and fake `HttpMessageHandler` | Request body/options, response mapping, bounded reads, cancellation and one-dispatch assertions |
| Live candidate evaluation | Explicit live mode, allowlisted corpus, provider credentials and finite run budget | Machine-readable report identifying actual live calls, usage/exposure, errors and quality results |
| Chain evaluation | Configured pair, same orchestrator; controlled primary failures plus live qualification separately | Fallback ordering/dispatch count and behavior with reused eligibility; primary success cannot hide an unqualified fallback |
| API/UI integration | Sections 3–4 and 6 | Auth, durable accounting, wire errors, result application and end-to-end latency; separate from standalone AI evidence |

The implementation must supply noninteractive entry points for these workflows and document actual commands in #10 once they exist. Do not present hypothetical `dotnet` commands as runnable today. Offline tests are the ordinary development/PR default. Missing credentials must not turn a requested live run into a fake passing report; report it as blocked/non-success.

Live runs require a declared maximum spend, dispatch count and concurrency. Include generation, failed attempts and any paid graders in admission; reserve before each call, including parallel cases. Use separate evaluation credentials/billing scope where available. If sharing a serving billing scope, integrate with its global monetary admission; a standalone in-memory budget cannot protect concurrent production spend. Persist metadata-only unresolved evaluation exposure across restarts or require reconciliation before another live run. Development text may be inspected in active console memory; it must not become a hidden production-text collection feature.

## 4. Frontend, browser and manual verification

### 4.1 Shared UI fixtures

Scenarios are layer-independent contracts. Use pure MSTest/Vitest units for policy/revisions and Vitest + Testing Library for visible components, explicit request counts and forms. Fake only external/transport boundaries appropriate to that layer. Integrated browser smoke uses real SPA/API/local auth/migrated SQLite and fake provider/email adapters; it never intercepts the application's `/api` calls. Deterministic tests never call live services or use arbitrary sleeps.

Keep fixed time `2026-09-07T17:40:00Z`, UTC display assertions, controlled deferred responses and fake timers only for deadlines/cooldowns/notices. There is **no debounce timer**. Count logical transformations separately from auth, usage, eligibility and status reads. Default auth is a verified local `writer@example.test` with user usage 7,500 and available global/budget capacity; seed prior results explicitly and exclude seed operations from observed counts.

Use T-OK-A: `Hello, the meeting starts at 14:30. Please go.` (46 ASCII chars), Romanian fixture `Bună, întâlnirea începe la 14:30. Te rog să mergi.`; T-OK-B: `The report is ready.` (20), Romanian `Raportul este gata.`. W-OK source: `The report is really ready. We sends it today.` (46); result: `The report is ready. We send it today.` (38). Result fixtures carry no sentence IDs. T-LONG is 5,312 `a` characters and must be rejected with excess 312, not accepted/truncated. Boundary fixtures are L−1/L/L+1 for both limits; #5 supplies exact Unicode expectations for emoji, combining marks, CRLF, tabs/spaces and Chinese. Use shared counting fixtures rather than JS string length assumptions.

Supported direction fixtures cover all 12 pairs using short equivalent sentences in the four languages. Rewrite fixtures include Traditional input with Simplified output and all nine dropdown choices. Language correctness is evaluation evidence in #6; fixtures establish transport and presentation only. For M006 and later local-password slices, use #5's selected 15–128 Unicode-scalar/no-composition policy; `Maple!River2026` is a valid 15-scalar synthetic fixture. Earlier references to a possible 12-character fixture minimum were nonbinding and are superseded by the selected API policy. Use invalid/expired token fixtures and controlled known/unknown-email outcomes.

Query by semantic role/name. Native selectors are asserted through value/options/disabled state and browser keyboard smoke, not custom menu internals. Do not preserve old sentence/boundary test IDs. Browser-only checks own actual clipboard, caret/selection, layout/history/storage and restoration; DOM emulators cannot prove them.

### 4.2 Browser lanes

Support current and immediately previous stable major releases at each release candidate for Chrome, Edge, Firefox, desktop Safari, iOS Safari and Android Chrome. Record actual tested versions in the release evidence. Bundled Playwright Chromium/WebKit represent engines, not branded/browser-version or physical-device proof. Previous-major support requires bounded actual-version smoke or an explicit evidence gap before release support is claimed.

| Lane | Automated scope | Release/manual scope |
| --- | --- | --- |
| Chromium desktop 1440×900 | Core keyboard/focus/edit/copy/history/privacy subset, curated visuals per Section 4.4, small integrated smoke | Branded Chrome/Edge and keyboard/zoom smoke |
| Chromium narrow 390×844 | Curated visuals and ordinary reflow/native-control geometry | Android Chrome/TalkBack at 360×800 |
| Chromium 320×640 / 1024×768 | Small overflow/breakpoint checks, no extra screenshot matrix | Zoom and text-spacing checks |
| Firefox desktop 1024×768 | Short native-editor/selector/keyboard/composition-event smoke when affected or at release | NVDA and actual supported Firefox versions |
| WebKit desktop 1440×900 | Short focus/navigation/editor smoke when affected or at release | Actual macOS Safari/VoiceOver |
| iOS Safari 390×844 | No separate full automated narrow-engine suite | VoiceOver, editing/copy, native selects and software keyboard |

### 4.3 Manual accessibility and device evidence

Before RG-006, check local registration/login/recovery, Translation, Rewriting mode selection, explicit submission, validation, copy and session expiry with NVDA/Firefox on Windows and VoiceOver/Safari on macOS. Include actual Simplified/Traditional Chinese IME on macOS. Check source/result editing, native selectors, copy and keyboard visibility with iOS Safari/VoiceOver and Android Chrome/TalkBack. Check keyboard-only Windows/macOS at 100%, 200% and effective 400%, forced colors, text spacing, and speech-control labels.

Automated semantics, focus, contrast/reflow and live-region checks do not prove understandable screen-reader narration or native mobile keyboard behavior. Deferred sentence, comparison and custom-sheet journeys are excluded from current evidence, not reported as passing.

### 4.4 Curated visual regression

Use **seven** initial Chromium baselines: Translation ready and oversize-error layouts (each 1440×900 and 390×844), Rewriting with inline mode and a plain edited result (390×844), local registration with long validation errors (1440×900), and workspace-reset dialog (390×844). Add a baseline only for an uncovered layout risk. No sentence, comparison, Google, prefix, custom-dropdown or tools-sheet baseline ships now. Remaining combinations use DOM assertions or targeted browser geometry, not screenshots of every scenario.

Pin Noto Sans/Noto Sans SC, Chromium/container, scale factor 1 and deterministic synthetic content/time. Wait for fonts; hide caret; disable motion and control scrollbars. Native select popups are excluded from pixel capture. Start with per-pixel threshold 0.1 and max differing ratio 0.001; record measured reasons for adjustments and never automatically accept changed baselines. DOM emulators cannot prove layout. Keep structural/semantic checks independent of screenshots.

Browser contracts use semantic locators and isolated contexts; prefer explicit readiness and controlled responses over timing sleeps. See [Playwright best practices](https://playwright.dev/docs/best-practices). Native clipboard, composition, selection and restoration require their actual platform evidence; synthetic DOM events or bundled engines cannot stand in for physical-device or assistive-technology review. Unsupported access to a required device/version is an explicit evidence gap, blocking RG-006 until resolved.

## 5. LLM quality and candidate qualification (Q-005)

### 5.1 Versioned corpus and reference construction

Create a frozen release corpus of **600 eligible quality cases**, separate from development/calibration cases. These counts are the initial verification design; changing them requires a reviewed #6 revision and a recorded reason, never removal of failing cases to obtain a pass.

| Set | Composition | Scoring unit |
| --- | --- | --- |
| Translation quality | 20 distinct cases for each of 12 directed pairs: 240 | Each direction separately |
| Rewriting quality | 10 distinct cases for each of four languages × nine modes: 360 | Each language separately (90 cases), with all 36 language/mode cells reported |
| Eligibility and boundaries | 40 cases per family: 80 additional cases | Expected classification/dispatch outcome; separate from quality denominator |
| Development/calibration | At least 40 additional cases, 10 per language, disjoint from release cases | Prompt/checker development and grader calibration; never release-score evidence |

For each family, the 40 eligibility cases contain eight cases in each group: valid main-language text with foreign names/short phrases; ambiguous automatic input with labeled manual-hint variants; substantially unsupported/mixed input; contradictory manual hint or same-language Translation (Rewriting uses contradictory hints); locally invalid empty/whitespace/oversize/malformed-Unicode input. Include each supported language where applicable. Some classifications remain semantically ambiguous: bilingual reviewers resolve expected outcomes before freezing. Do not invent a foreign-word percentage or trust model confidence as ground truth. Purely invalid cases prove zero paid dispatch offline; live classifier cases run only where #4 permits a call.

Release quality cases cover short/long text, paragraphs/lists, spelling/grammar errors, already-correct text, negation/uncertainty, names, numbers/dates, URLs, natural style/tone and instructions embedded as source content. In each Translation direction and each Rewriting language/mode cell include at least two fidelity-risk cases (numbers, negation or factual constraints), one paragraph/list case, one instruction-as-content case and one upper-length-band case; tags may overlap. Correction-only cells include both already-correct text and text needing minimal correction. In every Chinese-source direction use ten Simplified and ten Traditional cases; each Chinese Rewriting mode has five of each. All Chinese outputs are checked for the Simplified policy. Valid transliteration is evaluated for identity, not spelling equality.

A case records stable ID, provenance/license or author approval, exact source, operation/settings, canonical scalar count, expected eligibility, atomic meaning/fact assertions, and acceptable-output notes. Bilingual authors prepare Translation reference notes; fluent reviewers prepare Rewriting mode/correction expectations. A single reference is an example, not an exact-match oracle. Independently review references before freezing; the case author cannot be its only release reviewer. No production workspace text is imported.

### 5.2 Rubric and denominator

Score each successful output on four dimensions: (1) meaning/factual fidelity and structural completeness; (2) grammar/naturalness; (3) requested translation style/tone fidelity or rewriting mode/minimal correction; (4) correct language/script and clean complete plain output. Use the following anchored scale for each dimension:

| Score | Meaning |
| --- | --- |
| 3 | Meets the dimension without a material defect |
| 2 | Usable as delivered or with minor surface edits; no substantive repair, meaning change or missing requested intent |
| 1 | Requires substantive editing or misses the requested intent; unusable |
| 0 | Fails the dimension or contains a critical error |

A usable case has a complete accepted outcome, every dimension at least 2, and no critical error. Critical examples follow PRD Section 8: invented facts, material omission, meaning reversal, changed numerical value or wrong output language. For example, changing “must not” to “must,” 14:30 to 15:30, or inventing a meeting location is critical. Natural date/number formatting or name transliteration that preserves meaning is not automatically critical. Script and mode violations fail their applicable dimension even where they are not factual errors. Human review resolves ambiguous findings.

The denominator is **every eligible case assigned to that candidate/run**, including unexpected eligibility rejection, refusal, invalid output, provider error, timeout and missing output; those cases are unusable. Do not score only returned text, average away a critical error, combine strong and weak Translation directions, or let a fallback rescue a candidate's individual score. Apply NFR-001's threshold to each Translation direction and separately each Rewriting language. Mode breakdowns expose weaknesses without inventing a different per-mode percentage gate. Negative-input cases are evaluated separately and cannot inflate quality results; mismatches require triage against their frozen expected contract before qualification.

### 5.3 AI grading and human review

AI-assisted grading covers every successful quality output using the frozen source, reference constraints and rubric. Pin the judge configuration and grading prompt, hide candidate labels, calibrate against human-labeled development examples, and record judge identity and limitations. The judge may flag errors but cannot silently redefine ground truth. A missing/invalid grade is unresolved; obtain human grading or keep qualification blocked. Grader calls share the declared paid-run bounds; there is no unbounded grade-retry loop.

Human review has a fixed minimum per candidate: **five cases in each Translation direction** (60 for the family) and **two cases in each Rewriting language/mode cell** (72 for the family). Choose samples using a recorded seed before seeing results, stratified across scripts, length and fidelity risks. Include both input scripts in Chinese-source samples. Also review every critical-error flag, unusable output, eligibility false rejection, and disagreement between deterministic findings and the grader. These additional reviews do not replace the preselected sample. Review the labeled eligibility outcomes separately, including all mismatches.

Translation reviewers must understand source and target; Rewriting reviewers must be fluent in the language and requested intent. Human reviews in all four languages are mandatory. A second qualified reviewer adjudicates disputed findings and every suspected critical error. Store final scores, reviewer identity/competence, rationale for overrides and unresolved findings. Do not treat unavailable reviewers as a waiver or AI grades as human evidence. A release set with a confirmed critical error fails; an unresolved critical flag cannot pass. Sampled review is evidence with known limits, not proof that all production outputs are correct.

### 5.4 Qualification and changes

Qualify each primary and optional fallback directly for the entire family it serves using the same corpus, rubric and product thresholds. A candidate serving both families needs both reports. Candidate-only evaluation disables fallback rescue while preserving the production eligibility/transformation pipeline. Then exercise the configured chain normally and with controlled primary failures, including both three-dispatch paths, terminal user validation, eligibility reuse and deadline exhaustion. Fault-injected results are labeled separately from natural live results.

Qualification links adapter capability/settings, finite context/output/cost bounds, runtime checker behavior, quality/human review and applicable API performance to exact revisions. Startup admission remains #4's responsibility. No qualified fallback means configure none. Record failed runs; after prompt/model/checker changes, create a new candidate revision and rerun the affected family, retaining earlier failures and stable cases. Do not tune on the release set and then claim it is unseen; record exposure and add fresh held-out cases in the next reviewed corpus revision.

Reassess evidence after provider/SDK/model alias, prompt, sampling/settings or checker changes. Returned model/fingerprint observations help identify change but do not prove unchanged behavior. A pricing-only update requires cost/bounds verification; language regrading is needed when behavior may also change. Record the impact decision and its evidence. No new monitoring cadence or uptime SLA is created here.

## 6. Performance and workload composition (Q-005)

### 6.1 API benchmark

Measure NFR-002 at the real published API over HTTP/TLS with real authentication, migrated SQLite, durable admission/settlement and the live candidate. Record deployment hardware/region, configuration, network location and provider identity. Start each duration on API submission and stop when the complete response body arrives; serving must already have committed success. Standalone runner/SDK timing, TestServer timing, mocked API responses and browser rendering time are separate diagnostics.

For each candidate-family configuration, and for each deployed chain with a distinct configuration, run **360 measured requests at ten concurrent operations** with input/context within the PRD benchmark bound. Translation has 30 requests per direction; Rewriting has ten per language/mode cell. Use equal length bands of 1–100, 101–500 and 501–1,000 canonical characters across each family (120 requests per band). Balance automatic/manual source selection and Chinese scripts across the run.

Use 180 distinct source/settings combinations and repeat each once with a fresh submission identity: 15 distinct per Translation direction or five per Rewriting language/mode cell. Allocate 60 distinct cases to each length band, rotating the extra case across Rewriting cells. Interleave requests with a recorded seed; never use replay of an already completed operation as a fast generation sample. Observe provider cache usage; first-seen inputs are not proof of cold provider cache. Report first-seen/repeated and observed hit/miss/unknown cohorts, including insufficient evidence for a cache condition. Disable application/runner response reuse; provider-managed caching remains allowed.

No paid warm-up cases are silently discarded. Establish host readiness before measurements; record any predeclared initialization requests/cost separately. Keep ten operations in flight while work remains and disclose the final drain. Arrange verified test accounts, sufficient genuine daily/global capacity and a finite paid-run budget before starting; do not disable allowance enforcement. If setup exhausts quota/budget, retain outcomes and label the run incomplete/invalid for qualification rather than dropping those requests.

Report complete-success counts within NFR-002's time target, failures/timeouts, p50/p95 using nearest rank, per-route/language/mode distributions, admission/eligibility/transformation/settlement durations, and cost/cache cohorts. For target assessment unsuccessful requests are not timely completions (use infinite effective latency or an equivalent full-denominator success-within-target calculation). Do not claim a pass from successful-only percentiles. Preserve all observations and apply the PRD's exact percentile target separately to Translation and full Rewriting.

### 6.2 Maximum length, fallback and failure deadlines

Outside that percentile workload, run L−1 and L for every Translation direction (24 cases) and every Rewriting language/mode cell (72 cases), with L from FR-007. Record full-source counts, complete output or classified failure, total duration and attempts. Exercise output expansion and context bounds; partial/truncated output is never success. L+1 is a deterministic local/API rejection case with zero provider calls, not a paid performance sample.

Use controlled faults to expire time during admission, eligibility, primary transformation, fallback and final settlement. Prove the original overall deadline is never reset, no post-deadline dispatch/commit wins, and late responses are fenced. Fake time covers boundary permutations; a small real-socket run verifies actual cancellation/deadline behavior without treating simulated provider speed as live latency. Include configured fallback success, exhaustion, throttling and unknown expenditure. Controlled timeout cases may fail as designed but cannot be counted as timely successes in the natural benchmark. RG-003 requires both the measured percentile workload and maximum-length/fallback deadline evidence.

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

### 8.1 Product and release coverage

This is the sole cross-document product coverage matrix; the source documents retain their behavioral acceptance IDs. Active product rows remain **Unselected / Pending** except for the explicitly linked, passing M005 and M006 portions below; neither narrow allocation satisfies a complete product requirement or release gate. The separately identified M001–M006 rows link completed packages. Once product work is selected, link its actual #8 backlog/package and state partial coverage honestly. Deferred, retired and proposed rows are intentionally not current test gaps or passing gates. Design references plus Section 8.2 connect product rows to local scenario checks without copying their full acceptance prose.

| Requirement / gate | Shared design / acceptance owner | Checks | Backlog/package → evidence/status |
| --- | --- | --- | --- |
| FR-001 | UX §9; API §3 | V-004, V-003, V-010 | [M006 / BI-006](08-backlogs/M006-register-local-api-account.md) → [AC-001–003](../specs/006-register-local-api-account/spec.md#4-selected-acceptance) API registration portion → Passed; sign-in and web flow remain unselected |
| FR-002 | UX §9; API §3 | V-004, V-003, V-016 | [M006 / BI-006](08-backlogs/M006-register-local-api-account.md) → [AC-004/005](../specs/006-register-local-api-account/spec.md#4-selected-acceptance) durable unverified/delivery-intent portion → Passed; confirmation/reset and real email remain unselected |
| FR-003 | UX §3; API §2 | V-003, V-009, V-012 | Unselected → Pending |
| FR-004 | UX §6–8; AI §3; API §2/4 | V-001, V-002, V-003, V-007, V-009, V-013 | [M005 AC-002/005](../specs/005-shared-input-capability-contract/spec.md#4-selected-acceptance) source-choice discovery/local selector portion → Passed; semantic eligibility/UI remain unselected |
| FR-005 | AI §3; UX §7–8; API §2 | V-003, V-007, V-009, V-013 | [M005 AC-002](../specs/005-shared-input-capability-contract/spec.md#4-selected-acceptance) supported-language discovery portion → Passed; eligibility/quality/UI remain unselected |
| FR-006 | AI §3; UX §7–8; API §2 | V-003, V-007, V-009, V-013 | [M005 AC-002](../specs/005-shared-input-capability-contract/spec.md#4-selected-acceptance) Chinese script-policy discovery portion → Passed; transformation/quality/UI remain unselected |
| FR-007 | API §2/4; UX §7–8 | V-001, V-002, V-003, V-009 | [M005 AC-003–005](../specs/005-shared-input-capability-contract/spec.md#4-selected-acceptance) counts/limits/pure-validation portion → Passed; HTTP submission/UI remain unselected |
| FR-008 | AI §3–4; UX §7–8 | V-007, V-013, V-010 | Unselected → Pending |
| FR-009 | AI §3; UX §7; API §2 | V-013, V-003, V-009 | [M005 AC-002](../specs/005-shared-input-capability-contract/spec.md#4-selected-acceptance) direction-discovery portion → Passed; transformations/evaluation/UI remain unselected |
| FR-010 | AI §3–4; UX §7 | V-013, V-007, V-003 | Unselected → Pending |
| FR-011 | UX §6–7; AI §4; API §6 | V-002, V-003, V-007, V-010, V-012 | Unselected → Pending |
| FR-012 | AI §3; UX §8; API §2 | V-003, V-009, V-013 | [M005 AC-002](../specs/005-shared-input-capability-contract/spec.md#4-selected-acceptance) rewrite-language-discovery portion → Passed; transformations/evaluation/UI remain unselected |
| FR-013 | AI §3–4; UX §8 | V-013, V-007, V-003 | Unselected → Pending |
| FR-014 | UX §8; AI §3; API §2/4 | V-002, V-003, V-007, V-009, V-013 | [M005 AC-002/005](../specs/005-shared-input-capability-contract/spec.md#4-selected-acceptance) mode catalog/default/local-selector portion → Passed; rewrite behavior/UI remain unselected |
| FR-015 | PRD FR-015; UX-AC-041/042 | — | Not applicable — Retired |
| FR-016 | UX §6/8; API §5 | V-002, V-003, V-010, V-012 | Unselected → Pending |
| FR-017 | UX §6/8 | V-002, V-003, V-010 | Unselected → Pending |
| FR-018 | UX §6/8; API §6 | V-002, V-003, V-005, V-010 | Unselected → Pending |
| FR-019 | PRD DF-002; UX deferred comparison | — | Not applicable — Deferred |
| FR-020 | PRD DF-001 | — | Not applicable — Deferred |
| FR-021 | PRD DF-001 | — | Not applicable — Deferred |
| FR-022 | UX §6/8 | V-002, V-003, V-010 | Unselected → Pending |
| FR-023 | UX §6/8 | V-002, V-003, V-010 | Unselected → Pending |
| FR-024 | API §4/7; architecture §7 | V-001, V-005, V-003 | Unselected → Pending |
| FR-025 | PRD DF-001 | — | Not applicable — Deferred |
| FR-026 | API §5–7; architecture §7; UX §6 | V-005, V-002, V-003 | Unselected → Pending |
| FR-027 | API §7; architecture §7 | V-005, V-003 | Unselected → Pending |
| FR-028 | API §7; UX §6/9 | V-005, V-002, V-003, V-009 | Unselected → Pending |
| FR-029 | AI §5; architecture §5 | V-007, V-008, V-013 | Unselected → Pending |
| FR-030 | AI §5–6 | V-007, V-013 | Unselected → Pending |
| FR-031 | PRD DF-004 | — | Not applicable — Deferred |
| FR-032 | AI §6 | V-007, V-008, V-014 | Unselected → Pending |
| FR-033 | AI §4/6–7; API §8 | V-007, V-008, V-005, V-009 | Unselected → Pending |
| FR-034 | AI §6–7; UX §6; API §6 | V-007, V-013, V-014, V-005, V-003 | Unselected → Pending |
| FR-035 | API §2/5/9 | V-009, V-005 | Unselected → Pending |
| FR-036 | API §2–3/7 | V-004, V-009, V-005 | [BI-005](08-backlogs/M005-shared-input-capability-contract.md) → [AC-001–006](../specs/005-shared-input-capability-contract/spec.md#4-selected-acceptance) public choices/limits and generated-type portion → Passed; auth/language/usage independence remains unselected |
| FR-037 | API §8; AI §7; UX §6/9 | V-009, V-003, V-004, V-005, V-007 | Unselected → Pending |
| FR-038 | UX §3/8; architecture §6/8 | V-002, V-003, V-010, V-015 | Unselected → Pending |
| NFR-001 | PRD §8; AI §3–5 | V-013 | Unselected → Pending |
| NFR-002 | PRD §7; AI §6; API §6 | V-014, V-007, V-005 | Unselected → Pending |
| NFR-003 | UX §6; architecture §7; API §5–7 | V-002, V-005, V-007, V-010, V-016 | Unselected → Pending |
| NFR-004 | UX §3; architecture §6; AI §8; API §9 | V-015, V-010, V-005 | Unselected → Pending |
| NFR-005 | UX §4–5/10 | V-003, V-010, V-011 | Unselected → Pending |
| NFR-006 | architecture §7; AI §5–6; API §7 | V-006, V-008, V-013, V-014 | Unselected → Pending |
| NFR-007 | UX §4–5; Should priority retained | V-011 | Unselected → Pending |
| NFR-008 | PRD P-005; no added acceptance | — | Not applicable — Proposed |
| RG-001 | PRD §8; active functional rows above | V-001, V-002, V-003, V-004, V-005, V-007, V-009, V-010, V-012 | Unselected → Pending |
| RG-002 | PRD §8; Section 5 | V-013 | Unselected → Pending |
| RG-003 | PRD §8; Section 6 | V-014 | Unselected → Pending |
| RG-004 | architecture §7; API §5–7; UX §6 | V-005, V-006, V-002, V-003 | Unselected → Pending |
| RG-005 | API §2–3/8; UX §9 | V-004, V-009, V-012, V-016 | Unselected → Pending |
| RG-006 | UX §10; Section 4 | V-010, V-011 | Unselected → Pending |
| RG-007 | architecture §6–7; AI §5/8–9 | V-015, V-006, V-008, V-013, V-014 | Unselected → Pending |
| RG-008 | PRD §11; Section 9 | V-016 | Unselected → Pending |
| M001 / BI-001 — enabling scope | Architecture §2–3/5; backend build/process and absent-route boundary; prerequisite for FR-035/036, no product acceptance claimed | V-009 HTTP-host portion; V-012 process-lifecycle portion | [BI-001](08-backlogs/M001-backend-foundation.md) → [package AC-001–006](../specs/001-backend-foundation/spec.md#3-selected-acceptance) → [tasks/evidence](../specs/001-backend-foundation/tasks.md#3-completion-record); Done / Passed — 9 HTTP tests plus successful and controlled-failure process smoke |
| M002 / BI-002 — enabling scope | Architecture §3.1; UX §3–5 staged shell; partial enablement for FR-003/NFR-005/007, no full product acceptance claimed | V-003 shell components; V-009 API/static boundaries; V-010 navigation/reflow; V-011 visual inspection; V-012 published-shell portion | [BI-002](08-backlogs/M002-published-web-shell.md) → [package AC-001–008](../specs/002-published-web-shell/spec.md#3-selected-acceptance) → [tasks/evidence](../specs/002-published-web-shell/tasks.md#3-completion-record); Done / Passed — 8 component, 20 HTTP and 6 isolated published-host Chromium cases plus stale-asset and screenshot evidence |
| M003 / BI-003 — enabling scope | Architecture §4.1/5.2/6.1; explicit initialization and clean-restart persistence; no account/ledger or full product acceptance claimed | V-016 fresh migrations/model drift; V-005 file-backed transaction/restart foundation only; V-009/V-012 host/published regressions | [BI-003](08-backlogs/M003-durable-storage-foundation.md) → [package AC-001–006](../specs/003-durable-storage-foundation/spec.md#3-selected-acceptance) → [tasks/evidence](../specs/003-durable-storage-foundation/tasks.md#3-completion-record); Done / Passed — 22 storage cases within 42 backend tests, explicit repeat migration and same-file two-process restart, plus backend/published-shell regressions |
| M004 / BI-004 — enabling scope | Architecture §3.2/4.1/8.2; AI §2/4.1/9; host-independent shared boundary and synthetic prompt inspection only | V-007 independent build/prompt/scripted-client portions; V-009/V-012 host/published regressions | [BI-004](08-backlogs/M004-independent-ai-development.md) → [package AC-001–007](../specs/004-independent-ai-development/spec.md#3-selected-acceptance) → [tasks/evidence](../specs/004-independent-ai-development/tasks.md#3-completion-record); Done / Passed — 10 focused AI and 42 retained API/storage cases, deterministic prompt hash/inspection, controlled failures, backend smoke and 8 component/6 published Chromium regressions; M015–M020 and all product/release assertions remain unselected |
| M005 / BI-005 — product contract slice | PRD FR-004–007/009/012/014/036; API §2/4/9; public discovery, pure local validation and generated contract/types only | V-001 shared scalar/limit/selector fixtures; V-009 public capability HTTP and schema/type drift portions; V-012 route/publish regressions | [BI-005](08-backlogs/M005-shared-input-capability-contract.md) → [package AC-001–008](../specs/005-shared-input-capability-contract/spec.md#4-selected-acceptance) → [tasks/evidence](../specs/005-shared-input-capability-contract/tasks.md#3-completion-record); Done / Passed — 47 API/storage, 10 Core, 10 AI, 36 frontend unit and 6 published Chromium cases plus deterministic generation/drift and failure guards; text submission, auth, usage, AI behavior, accounting, UI adoption and release checks remain unselected |
| M006 / BI-006 — local registration slice | PRD FR-001/002; architecture §5–6; API §3/9; anonymous local registration, durable unverified state and indispensable account-serving readiness only | V-004 Identity registration/non-enumeration/current-state guard; V-009 independent registration wire/schema/type drift; V-015 secret sentinels; V-016 Identity migration/key/startup/readiness; retained V-003/V-012 shell/publish regressions | [BI-006](08-backlogs/M006-register-local-api-account.md) → [package AC-001–008](../specs/006-register-local-api-account/spec.md#4-selected-acceptance) → [tasks/evidence](../specs/006-register-local-api-account/tasks.md#3-completion-record); Done / Passed — 67 API/storage/Identity/readiness, 10 Core, 10 AI, 36 frontend unit and 6 published Chromium cases plus deterministic generation/drift/startup failure guards; sign-in, confirmation/resend/status/reset, web form, live email, language work, deletion and RG-005 remain outside this slice |

### 8.2 Local acceptance allocation

Read the complete contracts at [UX Section 12](02-ux-specification.md#12-acceptance-contracts-and-preserved-scenario-ids), [LLM Section 10](04-llm-specification.md#10-local-acceptance-scenarios-and-handoffs) and [API Section 10](05-api-design.md#10-acceptance-scenarios-and-readiness). The following indexes allocate every existing scenario. Active checks remain pending except for M005's completed API-AC-001 fixture/local-boundary portion and API-AC-014 first capability-slice portion. Multiple V-IDs split different assertions; they do not require repeating a journey at every layer. For example, UX-AC-106 uses V-002 for ordering, V-003 for display and V-005 for durable snapshot order.

| UX scenario IDs | Current status | Check allocation |
| --- | --- | --- |
| UX-AC-001 | MVP | V-003, V-010, V-012 |
| UX-AC-002 | MVP | V-003, V-010, V-012 |
| UX-AC-003 | MVP | V-002, V-003, V-010, V-015 |
| UX-AC-004 | Amended | V-002, V-003, V-005, V-010 |
| UX-AC-005 | Amended | V-002, V-003, V-005 |
| UX-AC-006 | MVP | V-003, V-004 |
| UX-AC-007 | MVP | V-003, V-004, V-010 |
| UX-AC-008 | MVP | V-003, V-004 |
| UX-AC-009 | MVP | V-003, V-004 |
| UX-AC-010 | MVP | V-003, V-004 |
| UX-AC-011 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-012 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-013 | MVP | V-003, V-004, V-010, V-015 |
| UX-AC-014 | MVP | V-003, V-004, V-010 |
| UX-AC-015 | MVP | V-002, V-003, V-004, V-010, V-015 |
| UX-AC-016 | Amended | V-002, V-003, V-004, V-010, V-015 |
| UX-AC-017 | MVP | V-003, V-004, V-010 |
| UX-AC-018 | MVP | V-003, V-004, V-010 |
| UX-AC-019 | MVP | V-003, V-004, V-010 |
| UX-AC-020 | MVP | V-003, V-004 |
| UX-AC-021 | Amended | V-001, V-002, V-003 |
| UX-AC-022 | Amended | V-002, V-003 |
| UX-AC-023 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-024 | Amended | V-002, V-003 |
| UX-AC-025 | Amended | V-002, V-003, V-010, V-011 |
| UX-AC-026 | Amended | V-002, V-003, V-010 |
| UX-AC-027 | Amended | V-003, V-007, V-013 |
| UX-AC-028 | Amended | V-001, V-002, V-003 |
| UX-AC-029 | Amended | V-003, V-007, V-013 |
| UX-AC-030 | Amended | V-001, V-002, V-003 |
| UX-AC-031 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-032 | Amended | V-002, V-003, V-005 |
| UX-AC-033 | MVP | V-002, V-003, V-010 |
| UX-AC-034 | Amended | V-002, V-003 |
| UX-AC-035 | Amended | V-002, V-003, V-005 |
| UX-AC-036 | Amended | V-002, V-003, V-005 |
| UX-AC-037 | Amended | V-002, V-003, V-010 |
| UX-AC-038 | Amended | V-003, V-010 |
| UX-AC-039 | Amended | V-002, V-003 |
| UX-AC-040 | Amended | V-002, V-003 |
| UX-AC-041 | Retired | Not applicable; owning UX row records disposition |
| UX-AC-042 | Retired | Not applicable; owning UX row records disposition |
| UX-AC-043 | Amended | V-002, V-003, V-010, V-011 |
| UX-AC-044 | Amended | V-002, V-003, V-010 |
| UX-AC-045 | Amended | V-001, V-002, V-003 |
| UX-AC-046 | Amended | V-002, V-003, V-005 |
| UX-AC-047 | MVP | V-002, V-003, V-005 |
| UX-AC-048 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-049 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-050 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-051 | Amended | V-002, V-003, V-010 |
| UX-AC-052 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-053 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-054 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-055 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-056 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-057 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-058 | Amended | V-002, V-003, V-010 |
| UX-AC-059 | Amended | V-002, V-003, V-005, V-010 |
| UX-AC-060 | Amended | V-002, V-003, V-010 |
| UX-AC-061 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-062 | Retired | Not applicable; owning UX row records disposition |
| UX-AC-063 | Retired | Not applicable; owning UX row records disposition |
| UX-AC-064 | Retired | Not applicable; owning UX row records disposition |
| UX-AC-065 | MVP | V-002, V-003, V-005 |
| UX-AC-066 | Amended | V-002, V-003, V-005 |
| UX-AC-067 | Amended | V-002, V-003, V-005 |
| UX-AC-068 | Amended | V-002, V-003, V-005 |
| UX-AC-069 | Amended | V-002, V-003, V-005 |
| UX-AC-070 | Amended | V-002, V-003, V-006 |
| UX-AC-071 | Amended | V-002, V-003 |
| UX-AC-072 | MVP | V-002, V-003, V-005 |
| UX-AC-073 | MVP | V-002, V-003, V-005 |
| UX-AC-074 | Amended | V-002, V-003 |
| UX-AC-075 | MVP | V-002, V-003, V-005 |
| UX-AC-076 | Amended | V-002, V-003 |
| UX-AC-077 | Amended | V-010 |
| UX-AC-078 | Amended | V-003, V-010, V-011 |
| UX-AC-079 | Amended | V-003, V-010, V-011 |
| UX-AC-080 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-081 | Amended | V-003, V-010, V-011 |
| UX-AC-082 | Amended | V-002, V-003 |
| UX-AC-083 | Amended | V-003, V-010, V-011 |
| UX-AC-084 | Amended | V-002, V-003, V-010 |
| UX-AC-085 | Amended | V-002, V-003, V-005, V-010, V-015 |
| UX-AC-086 | Amended | V-002, V-003, V-010, V-015 |
| UX-AC-087 | MVP | V-002, V-003, V-010, V-015 |
| UX-AC-088 | Amended | V-003, V-007, V-013 |
| UX-AC-089 | Amended | V-003, V-007, V-013 |
| UX-AC-090 | Amended | V-002, V-003, V-013 |
| UX-AC-091 | Amended | V-002, V-003, V-013 |
| UX-AC-092 | Amended | V-003, V-010 |
| UX-AC-093 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-094 | Amended | V-002, V-003, V-010 |
| UX-AC-095 | Amended | V-003, V-010, V-011 |
| UX-AC-096 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-097 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-098 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-099 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-100 | Amended | V-002, V-003, V-010 |
| UX-AC-101 | MVP | V-003, V-004, V-010 |
| UX-AC-102 | MVP | V-003, V-004 |
| UX-AC-103 | MVP | V-003, V-004, V-010, V-015 |
| UX-AC-104 | MVP | V-003, V-004, V-010 |
| UX-AC-105 | Amended | V-002, V-003, V-005 |
| UX-AC-106 | MVP | V-002, V-003, V-005 |
| UX-AC-107 | Amended | V-002, V-003 |
| UX-AC-108 | MVP | V-002, V-003, V-005 |
| UX-AC-109 | Amended | V-001, V-002, V-003 |
| UX-AC-110 | Amended | V-002, V-003, V-013 |
| UX-AC-111 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-112 | MVP | V-003, V-010, V-012 |

| LLM scenario | Check allocation |
| --- | --- |
| LLM-AC-001 | V-007 |
| LLM-AC-002 | V-007 |
| LLM-AC-003 | V-001, V-007, V-013 |
| LLM-AC-004 | V-007, V-013 |
| LLM-AC-005 | V-007, V-008, V-013, V-015 |
| LLM-AC-006 | V-007, V-013 |
| LLM-AC-007 | V-007, V-008, V-013 |
| LLM-AC-008 | V-007, V-008 |
| LLM-AC-009 | V-007, V-005, V-014 |
| LLM-AC-010 | V-006, V-007 |
| LLM-AC-011 | V-008 |
| LLM-AC-012 | V-015, V-005 |
| LLM-AC-013 | V-007, V-006, V-013 |
| LLM-AC-014 | V-013, V-014 |
| LLM-AC-015 | V-007, V-005, V-002, V-003, V-012 |

| API scenario | Check allocation |
| --- | --- |
| API-AC-001 | V-001; M005 AC-004/005 passed for the fixture/local-boundary portion, HTTP submission boundary later |
| API-AC-002 | V-004 |
| API-AC-003 | V-004 |
| API-AC-004 | V-005 |
| API-AC-005 | V-001, V-005 |
| API-AC-006 | V-005, V-009, V-015 |
| API-AC-007 | V-005, V-016 |
| API-AC-008 | V-005, V-014 |
| API-AC-009 | V-005, V-002, V-003 |
| API-AC-010 | V-006 |
| API-AC-011 | V-005, V-002, V-003 |
| API-AC-012 | V-004, V-015 |
| API-AC-013 | V-009 |
| API-AC-014 | V-009; M005 AC-006/007 passed for the first capability slice |

The UX index contains 86 active scenarios (29 MVP, 57 Amended), 21 Deferred and five Retired, retaining all 112 IDs. The LLM index retains 15 and the API index 14 active scenarios. Deferred alternative branches within active rows do not create current checks. Native browser/AT evidence uses Section 4's representative journeys, not 86 full end-to-end scripts.

## 9. Readiness and remaining dependencies

Q-005 is **specified at design level** by Sections 5–6. The corpus, implementation, reviewers and measurements are still pending; specifying counts is not qualifying a candidate. M001–M006's scoped evidence is complete, but it does not qualify language behavior or any release gate. M005 passes only the V-001/V-009/V-012 portions listed above; M006 passes only the V-004/V-009/V-015/V-016 and retained regression portions allocated above. Later work continues through #0's roadmap/backlog/delivery-package process.

| Dependency | Required before claiming readiness |
| --- | --- |
| Selected implementation and #10 commands | Executable suites, actual packages/test names, deterministic fixtures, report locations and CI gates |
| Q-001/Q-007 | Chosen serving/billing arrangement, credentials, verified adapter/settings/context bounds, qualified primary/optional fallback and an actual monetary ceiling before paid serving |
| Q-003/Q-006 | M005 resolves public capability/count/artifact mechanics; M006 resolves registration DTO/status/error fields, email/password/duplicate/delivery-intent behavior and unverified current-state guard for its package. Confirmation/sign-in/token/antiforgery/usage/language/status details remain due before their handlers/clients; generated review precedes dependent client adoption |
| Q-004 | M006 explicitly creates durable account/key records without deletion/retention claims. Remaining deletion, backup/aggregate/unresolved-exposure retention and reconciliation rules block their related features/launch, not selected registration; prove later lifecycle/restart/restore against them |
| Quality and performance evidence | Frozen approved corpus, qualified reviewers/judge configuration, explicit run budgets and actual candidate/chain/API reports |
| Browser/AT and operations | Access to supported current/previous actual versions/devices/AT; real email and published-host/restore evidence |
| Q-008/Q-010 / P-005/P-006 | Product-owner disposition of proposed safeguards/compatibility before making them binding; RG-008 reviews current launch dependencies only |

**M005 implementation validation:** The [completion record](../specs/005-shared-input-capability-contract/tasks.md#3-completion-record) links the exact deterministic/runtime evidence for the selected public capability, fixture/validation and contract-generation portions. The record distinguishes the initial published-artifact failure and corrected rerun, passing package audits, deliberate drift/CLI/zero-test failures and retained regressions. No HTTP text-submission, live-provider, quality, performance, accessibility or release gate is reported passed. Earlier technical references retain their recorded dates; package 005 records the generation-tool sources checked on 2026-09-09.

**M006 implementation validation:** [Package 006](../specs/006-register-local-api-account/spec.md) AC-001–008 passed real Identity/file-backed HTTP, normalized-email concurrency, current-state authorization, migration/key/readiness, secret-sentinel and generated-contract checks. Its [completion record](../specs/006-register-local-api-account/tasks.md#3-completion-record) records exact commands, counts, hashes, audits and deliberate failure guards. Deterministic capture/failure email adapters establish only application delivery intent; they do not establish confirmation completion, a live mailbox/provider, full FR-001/002, RG-005 or release readiness.
