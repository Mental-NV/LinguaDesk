# LinguaDesk — Product Requirements Document

**Version:** 0.3 · **Status:** Simplified MVP product baseline; deferred scope explicit; P-005/P-006 remain proposed · **Updated:** September 8, 2026 (UTC)

This document consolidates the discovery decisions. It specifies product outcomes for later UX, architecture, API, and feature specifications. It contains no implementation plan or development task breakdown.

**Reading conventions:** Confirmed requirements and their observable consequences are labeled **C**. Stable `P-*` items and their accepted, amended, or proposed dispositions are collected in Section 10. **MVP / Must** means required for the current release; **Deferred** means approved later-phase intent, excluded from MVP implementation and release gates; **Retired** means removed and not automatically scheduled for later; **Should** indicates a preference. **Proposed Must** becomes binding only if accepted during review. Deferred questions have an owner phase and remain unresolved. Release criteria describe work to be verified later; they are not claims that the product has been implemented or tested.

## 1. Product summary and problem

LinguaDesk is a DeepL-inspired, API-first product for LLM-powered text translation and rewriting. It helps people prepare messages, emails, and short professional passages while preserving meaning and giving them controlled ways to improve wording.

The product provides a focused workflow instead of requiring users to repeatedly explain translation and editing instructions to a general chat assistant. It keeps the original text available and returns complete, editable, copyable translations or rewrites on explicit request. Sentence alternatives and change-review tools are deferred under Section 3.1.

### Users and principal use cases

| Audience | Principal use cases | Product value |
| --- | --- | --- |
| Project owner and approximately 10–100 early users | Translate messages; correct grammar and spelling; choose a writing style or tone; manually edit the complete result | Convenient, predictable everyday communication |
| Prospective hiring clients | Try the web product and assess its API integration experience | Evidence of experienced full-stack engineering through coherent UX, independent API capabilities, reliable failure handling, and controlled LLM usage |

The initial client is a React SPA with separate Translation and Rewriting pages. Desktop and mobile browsers, keyboard operation, and screen-reader access are MVP requirements. Native mobile and browser-extension clients can integrate independently with the API but do not ship in this MVP.

## 2. Goals and success criteria

| Goal | Confirmed measure |
| --- | --- |
| Useful language output | At least **90% usable outputs** in each evaluated translation direction and each rewriting language, assessed separately by operation |
| Preserve essential information | **No critical meaning or factual errors in the submitted text covered by the release evaluation set**, including invented information or changed numerical values |
| Responsive processing | At least 95% of benchmark requests complete within **10 seconds** for translation/full rewriting; benchmark conditions are in Section 7 |
| Recover predictably | Fallback, stale-response, validation, and total-failure acceptance criteria pass without losing source text or charging failed operations |
| Enable independent clients | An API consumer can authenticate with a local account, translate, rewrite, and obtain usage information without depending on the SPA |
| Keep usage bounded | Enforce the per-request limits, shared character allowances, UTC reset, and successful-operation accounting |
| Demonstrate an accessible web product | Core journeys work on desktop/mobile browsers and with keyboard and screen-reader access |

“Usable” means acceptable without editing or with only minor edits. The minimum quality standard applies to primary and fallback model configurations. Adoption, weekly activity, revenue, and hiring conversion targets have not been agreed; the audience estimate is not an adoption commitment.

## 3. Scope and boundaries

### Confirmed MVP

- Translation among English, Russian, Romanian, and Chinese in all 12 directed pairs; automatic source detection and manual override remain.
- Rewriting in all four languages, with mandatory grammar/spelling correction and one native dropdown: **Correction only** (default) or one style/tone.
- Explicit **Translate** and **Rewrite** buttons. Typing, pasting, selecting settings, reconnecting, and allowance resets never submit paid processing automatically.
- Complete results in plain editable text fields; copy, source preservation, outdated-response protection, and explicit recovery.
- Translation blocks input over 5,000 characters; rewriting blocks input over 2,000. Neither silently truncates or processes a prefix.
- Open local email/password registration, email verification, password reset, and local sign-in. Independent API authentication remains in scope.
- Free access with successful-character accounting, per-user/global daily allowances, and a separate monetary ceiling.
- One configured model chain for all Translation routes and one for all Rewriting languages, each with one primary and at most one fallback. Existing model eligibility and quality requirements remain.
- Responsive React SPA with separate feature pages, native selectors, inline tools, and an independent API for both operations and usage.

### 3.1 Deferred scope register (product authority)

The user approved the simplification on **2026-09-08**, against the documents at Git revision `54343c3`. This register is the single source for later-phase scope; it is not a second executable specification or a roadmap. **Deferred** items are intended for later implementation, with milestone/date unassigned. They are not MVP blockers and need no placeholder endpoints, DTOs, feature flags, UI, or tests now. Before selecting one, refine its design against the then-current product; retain its original IDs and connect it to a document #8 backlog/package. Historical detailed UX at `54343c3` is reference material, not automatically reinstated acceptance criteria.

| ID | Deferred capability / related IDs | Current MVP substitute | Reactivation boundary |
| --- | --- | --- | --- |
| DF-001 | Sentence alternatives, selection/reuse, sentence-only charging, sentence/version API association — FR-020/021/025; deferred parts of FR-022/035 | Complete rewrite plus ordinary result editing; no sentence metadata or alternatives API | Define alternative context/IDs/accounting/evaluation and local result lifecycle. Any manual edit clears all assistance metadata; targeted preservation remains retired |
| DF-002 | Change review: highlighting/comparison — FR-019; related FR-038 preference | Plain editable result; no diff/comparison view or Show changes control | Start from a separate read-only comparison design; re-evaluate highlighting needs. Does not require sentence alternatives or restore targeted correspondence |
| DF-003 | Automatic processing after a pause — timing parts of FR-011/016, former P-001/P-003 | Explicit buttons; validity recovery merely reenables submission | Define opt-in/default behavior, debounce/IME timing, races, cost disclosures and tests before activation; prior 1-second design is historical input |
| DF-004 | Per-language/directed-route matching, priorities, overlapping rules, arbitrary fallback chains — FR-031; deferred parts of FR-030/032 | Two operation-family chains, one primary plus at most one fallback each | Evidence that route-specific quality/cost needs justify the rule system; validate configurations and eligible models |
| DF-005 | Bespoke selectors, anchored tools, responsive rails/sheets — UX custom interaction design | Native selects and persistent inline controls; ordinary responsive layout and accessibility remain | A selected UX package demonstrates user benefit; do not replace native behavior solely for visual parity |
| DF-006 | Translation prefix processing, truncation disclosure and processed-boundary navigation — deferred part of FR-007/024 | Reject oversized input with count and shortening guidance; preserve all source text | Reintroduce partial-result semantics, Unicode prefix boundary, charging and disclosures together |
| DF-007 | Google sign-in and related external-account linking — deferred part of FR-001, Q-004 | Local accounts with verification/recovery; cookie and independent API access remain | Add provider/callback/linking design and real integration evidence; no Google configuration needed for MVP |

**Removed, not deferred:** targeted sentence identity/cache preservation through manual edits (former FR-023/Q-002) is cut. If DF-001/002 are implemented later, any manual edit invalidates all assistance metadata; the text remains editable/copyable. The separate Correction-only toggle, None set duplication, and remembered-style restoration (former FR-015/Q-002) are retired in favor of the single dropdown. These are not implied future tasks.

### 3.2 User decision mapping

| Review item | Accepted disposition | Governing outcome |
| --- | --- | --- |
| #1 | Cut targeted invalidation | FR-023; Q-002 resolved for MVP |
| #2 | Defer alternatives | DF-001 |
| #3 | Defer all change-review UI | DF-002; no simplified diff ships in MVP either |
| #4 | Explicit buttons now; automation later | FR-011/016; DF-003 |
| #5 | Simple family chains now; advanced routing later | FR-030/032; DF-004 |
| #6 | Native/inline controls now; bespoke controls later | UX specification; DF-005 |
| #7 | One dropdown | FR-014; FR-015 retired |
| #8 | Reject oversized input now; prefix processing later | FR-007/024; DF-006 |
| #9 | Local only; Google later | FR-001/002; DF-007 |
| #10 | Keep independent API | FR-035–037, RG-005 |

### Confirmed exclusions

File translation, voice translation, paid plans/billing, saved text history, Markdown-specific preservation, substantially mixed-language passages, unsupported languages, Traditional Chinese output, combined translation-and-rewriting operations, simultaneous style-and-tone selection, routing administration UI, and traffic weighting are outside MVP scope.

Native mobile and browser-extension client delivery are outside the initial release. Subsequent architecture, detailed UI design, OpenAPI contracts, implementation plans, and development tasks are separate deliverables. These exclusions remain uncommitted; the explicitly deferred capabilities in Section 3.1 have later-phase intent but no delivery date.

Persistent saved preferences and UI localization are outside MVP scope. The MVP interface is English. Settings may last only within the active workspace/session; a new workspace uses the confirmed defaults. Administration beyond the explicitly excluded routing UI is not an implied requirement.

## 4. Main user journeys

| Journey | Expected outcome and important failure paths |
| --- | --- |
| Get access | Register/sign in with local email/password; verify before LLM use; recover a forgotten password by email. No Google control or callback exists in MVP. |
| Translate a message | Enter supported text within the limit, keep detection or select a source, select a target, and activate Translate. Validation failures preserve text and require correction plus a new activation; no automatic retry. Edit/copy the complete result. |
| Correct or restyle writing | Enter text within the rewriting limit, choose Correction only or one style/tone, and activate Rewrite. Source and previous result remain available during processing. |
| Continue editing during processing | Source/settings or manual result edits invalidate the pending text response. A successful outdated operation still updates usage once. A later explicit submission can replace previously edited results; no typing-driven work runs. |
| Recover from failure | The configured fallback, if any, runs invisibly for eligible failures within the same operation deadline. Definitive failure preserves work and charges zero; explicit Try again starts a new operation only when the prior outcome is known. Unknown outcomes use status recovery, never blind redispatch. |
| Reach a limit | Oversized input, exhausted user/global allowance, or monetary suspension preserves work. Correcting input or restored availability reenables the action; only explicit activation submits. |

## 5. Functional requirements

Unmarked requirements in this section are **MVP / C / Must**. Explicit **Deferred** and **Retired** rows are excluded from MVP acceptance, including when other requirements refer to this catalog. IDs remain stable through subsequent revisions.

### 5.1 Access, languages, and shared input behavior

| ID | Requirement | Observable acceptance criteria |
| --- | --- | --- |
| FR-001 | Local registration and sign-in | **MVP / Must.** A new user registers and signs in with local email/password credentials. Google sign-in is **deferred (DF-007)**. HTTP Basic Authentication is not a confirmed protocol requirement. |
| FR-002 | Local-account verification and recovery | An unverified local account cannot invoke LLM operations. After verification it can, subject to limits. A user who forgets a password can complete a self-service reset. |
| FR-003 | Separate launch features | The React SPA provides separate Translation and Rewriting pages. Translation and rewriting are separate API capabilities; there is no combined transformation operation. |
| FR-004 | Source-language selection | **MVP / Must.** Both features default to automatic detection and permit manual override. Detection/eligibility runs as part of explicit submission; uncertain source, substantially unsupported input, or same-language translation produces validation with no transformation or character charge. Translation requires an explicitly selected different target. Correcting input/settings reenables submission; the user activates the button again. A server may receive a validation request; this is not a successful paid transformation. |
| FR-005 | Supported input languages | Accept one main language from English, Russian, Romanian, or Chinese. Foreign names, technical terms, and short foreign phrases are allowed. Substantially mixed passages or substantial unsupported-language content produce validation feedback and no allowance deduction. |
| FR-006 | Chinese scripts | Accept Simplified and Traditional Chinese input. Chinese output is Simplified, for both translation and rewriting. The script conversion is an explicit exception to preserving a Chinese input’s script. |
| FR-007 | Input eligibility and length limits | **MVP / Must.** Empty/whitespace-only input starts no operation and incurs no charge. Translation accepts at most **5,000 characters** and full rewriting at most **2,000**; oversized input remains editable, is blocked without processing/charge, and shows the excess count. The API enforces the same limits. Correcting input never submits automatically. Prefix translation is **deferred (DF-006)**. Exact Unicode counting remains Q-003. |
| FR-008 | Plain-text fidelity | Within the text submitted under FR-007, preserve paragraph/list structure, factual meaning, numerical values, names’ identities, and URLs. Transliteration may preserve a name’s identity without preserving its spelling. Markdown-specific syntax preservation is not promised. |

### 5.2 Translation

| Source | Supported targets |
| --- | --- |
| English | Russian, Romanian, Chinese |
| Russian | English, Romanian, Chinese |
| Romanian | English, Russian, Chinese |
| Chinese | English, Russian, Romanian |

| ID | Requirement | Observable acceptance criteria |
| --- | --- | --- |
| FR-009 | All directed pairs | Each of the 12 directions above is available and covered by language-quality evaluation. |
| FR-010 | Faithful translation | Output uses the selected target language and preserves the original meaning, style, and tone as closely as possible, subject to natural translation and FR-008. No rewriting style/tone controls are combined with translation. |
| FR-011 | Explicit translation and complete output | **MVP / Must.** Activate **Translate** to submit the current input/settings once. Typing, pasting, language changes, or elapsed time never submits. Do not submit during IME composition. Retain the previous result while updating; only a current response applies and failure preserves it. Return a complete editable/copyable result. Automatic pause-based submission is **deferred (DF-003)**. |

### 5.3 Rewriting and deferred assistance

The single **Writing mode** dropdown contains exactly one selection from:

| Choice category | Values |
| --- | --- |
| Preserve existing style/tone | **Correction only** — the default |
| Writing style | simple, casual, business, academic |
| Tone | enthusiastic, friendly, confident, diplomatic |

| ID | Requirement | Observable acceptance criteria |
| --- | --- | --- |
| FR-012 | Rewriting language | Support all four languages. Rewrite in the source language, subject to the Simplified Chinese output policy; rewriting does not translate into another language. |
| FR-013 | Mandatory correction | Grammar and spelling correction apply in every rewriting mode and cannot be disabled. Grammar-only passes make minimal corrections, avoiding discretionary restyling. |
| FR-014 | Single exclusive rewriting choice | **MVP / Must.** One dropdown defaults to **Correction only**, preserving style/tone with minimal grammar/spelling corrections. Other choices are the four styles and four tones listed above; exactly one may be selected and correction always applies. Selecting a choice does not submit. No None set option, separate toggle, or hidden prior-style memory. |
| FR-015 | Separate correction-only override — retired | **Retired, not deferred.** The old toggle/disable/restore behavior is replaced by FR-014. This ID is retained for traceability and creates no implementation or test obligation. |
| FR-016 | Explicit rewrite submission | **MVP / Must.** Activate **Rewrite** to submit current source and the selected dropdown value once. Typing, pasting, mode changes, or elapsed time never submits. Do not submit during IME composition. Pause-based submission is **deferred (DF-003)**. |
| FR-017 | Protect the source | **MVP / Must.** Source remains an ordinary editable field throughout processing. Rewriting never replaces its contents, changes its formatting, or moves its typing cursor. |
| FR-018 | Apply only current results | Retain the previous result during processing and label it **Updating…**. Apply a response only when it matches the latest source/settings and has not been invalidated by a manual result edit; ignore outdated responses. Failures preserve source and previous result. |
| FR-019 | Review changes — deferred | **Deferred (DF-002).** No highlighting, Show changes preference, or comparison view ships in MVP. Plain editable/copyable output is governed by FR-023; future review design must not restore the cut targeted-invalidation requirement. |
| FR-020 | Generate sentence alternatives — deferred | **Deferred (DF-001).** Later intent: on-demand contextual sentence alternatives. The former 2–4/default-3 option contract is design input for that selected phase, not an MVP API/UI/evaluation requirement. |
| FR-021 | Select and reuse alternatives — deferred | **Deferred (DF-001).** Later intent: local sentence replacement and reusable choices without a full rewrite or additional selection charge. No cache or sentence-selection behavior is required now. |
| FR-022 | Outdated and replaced results | **MVP / Must.** Source/settings changes mark the previous result outdated but do not submit. A later explicitly submitted successful full rewrite replaces it, including edits that existed when that submission began. Edits made after submission remain protected by FR-018/023. Alternative-menu/cache behavior is **deferred (DF-001)**. |
| FR-023 | Editable and copyable results | **MVP / Must.** Users directly edit and copy plain results. A manual result edit changes no source, starts no request, and incurs no charge. Earlier pending responses cannot overwrite it; a later explicit submission may replace it. Copy returns the current edited text. Targeted preservation of unaffected sentence identities/cache/comparison metadata is **cut**. No assistance metadata exists in MVP; future DF-001/002 assistance must be invalidated in full by any manual edit. |

### 5.4 Allowances and accounting

| Limit | Confirmed value |
| --- | ---: |
| User daily allowance, shared across translation and rewriting | **20,000 characters** |
| Global daily allowance, shared across all users | **2,000,000 characters** |
| Daily reset for both | **00:00 UTC** |

| ID | Requirement | Observable acceptance criteria |
| --- | --- | --- |
| FR-024 | Character-based usage | **MVP / Must.** A successful full translation/rewrite charges the complete submitted text length once. A new successful submission charges the full length even after a small edit. Oversized input is rejected and charged zero; prefix charging is **deferred (DF-006)**. |
| FR-025 | Alternatives accounting — deferred | **Deferred (DF-001).** Later intent: one selected-sentence-length charge per successful generated option set, no character charge for context/options or local selection. No alternatives ledger branch is required now. |
| FR-026 | Success-only, single-operation charging | Failed operations and retry attempts add no charge. One logical operation is charged at most once when it succeeds, including success after retries/fallback. Successful responses discarded as outdated still count. Retry identity and interruption edge cases are deferred to the API specification. |
| FR-027 | Enforce shared daily allowances | Server-side accounting enforces the user and global limits across clients and resets both at midnight UTC. Concurrent operations must not create duplicate deductions or successful usage beyond either allowance. |
| FR-028 | Report usage to clients | The server calculates usage and returns it to clients. Clients can reflect consumption and allowance-related failures consistently; exact response fields belong to the API contract. |

The limits are configuration-controlled product values rather than fixed client assumptions. A monetary cap is also required; its amount remains deferred. Character allowances are not a representation of provider token charges, especially for retries and failures. Alternative-context exposure is part of DF-001 when selected.

### 5.5 Routing and fallback

| ID | Requirement | Observable acceptance criteria |
| --- | --- | --- |
| FR-029 | Owner configuration | The owner configures routing without an administration UI. Model configurations support relevant provider/model settings, including thinking/reasoning controls where supported. |
| FR-030 | Operation-family routing | **MVP / Must.** One configured Translation chain serves every supported directed pair; one configured Rewriting chain serves every supported rewriting language/style/tone. No per-route matching or priorities. Route-specific selection and alternatives sharing are **deferred (DF-004/DF-001)**. |
| FR-031 | Rule precedence — deferred | **Deferred (DF-004).** Nonnegative integer priorities, overlap validation and rule matching are not MVP requirements. Revisit the former 0-highest-priority design when selecting advanced routing. |
| FR-032 | Bounded fallback order | **MVP / Must.** Each family configures one primary and zero or one fallback, tried in that order only for FR-033 failures and within the overall deadline. Exhaustion ends the operation. No matching-rule/default-chain merging or arbitrary-length chains; those routing capabilities are **deferred (DF-004)**. |
| FR-033 | Eligible fallback triggers | Try the next candidate for provider failures, including timeouts/throttling/service errors, and clearly invalid outputs such as empty or unusable responses. Invalid user input, user authentication failures, and exhausted allowances end the request without model fallback. |
| FR-034 | Quality and failure outcomes | Every candidate must pass the same applicable minimum quality criteria. Successful fallback is invisible to users. Exhaustion or deadline expiry produces a clear failure, preserves work, and consumes no character allowance. |

**Initial default (unchanged discovery choice):** DeepSeek V4 Flash in non-thinking mode. The prior discovery review recorded non-thinking support and the need to disable default thinking explicitly; this scope-only revision does not reverify current provider behavior. #4 must verify the actual serving configuration before relying on it. This is not evidence of LinguaDesk’s quality, latency, or privacy eligibility. [DeepSeek thinking-mode documentation](https://api-docs.deepseek.com/guides/thinking_mode/)

The serving provider/contract, eligible fallback configurations, and their evaluations remain pre-launch dependencies. Selection of a model does not establish compliance with the agreed no-training/retention requirements.

### 5.6 MVP interface state

| ID | Requirement | Observable acceptance criteria |
| --- | --- | --- |
| FR-038 | English UI and non-persistent preferences | **MVP / Must.** English UI; settings remain only within the active workspace. New workspaces default to automatic source detection and the single rewriting dropdown set to **Correction only**; Translation requires an explicit target. No Show changes setting or separate correction toggle exists. Workspace boundaries remain Q-004/UX-owned. |

## 6. API-facing product requirements

| ID | Priority / status | Requirement and observable acceptance criteria |
| --- | --- | --- |
| FR-035 | Must / C | Expose translation and full-text rewriting as distinct operations. Operation/request identity remains required for correlation and safe retries. Sentence alternatives and sentence/version association are **deferred (DF-001)**; do not add placeholder schemas for them. |
| FR-036 | Must / C | Expose languages, the single rewriting choice, limits, and usage independently of the SPA. All clients use authenticated access and the same server-side allowances. A separate local-account API consumer can authenticate, translate, rewrite, and read usage without executing React. Token lifecycle and independent-client tests remain MVP scope. |
| FR-037 | Must / C | Return meaningful failures for authentication/verification, invalid or unsupported input, input length, allowance/budget exhaustion, and processing failure/deadline expiry. The API contract defines categories and recovery guidance; it must distinguish these from successful output. |

Translation returns a complete result. Rewriting applies complete result replacements as specified in FR-018/022; intermediate generated text is not an agreed interactive result. Exact transport, endpoint schemas, token formats, cancellation mechanisms, and retry identifiers are deferred.

API documentation and integration examples should make the above behavior independently understandable (**P-006**). Compatibility/versioning guarantees beyond these product capabilities are not yet agreed.

## 7. Nonfunctional requirements

| ID | Priority / status | Expected outcome and acceptance measure |
| --- | --- | --- |
| NFR-001 | Must / C | **Quality:** at least 90% usable outputs in each evaluated route/language and operation, with no critical meaning/factual errors in the release set. Apply the same standard to every eligible primary/fallback configuration. |
| NFR-002 | Must / C | **Performance:** meet the percentile targets and overall deadlines below. These are acceptance targets requiring measurement, not assumed provider performance. |
| NFR-003 | Must / C | **Reliability:** controlled failure/concurrency tests show no source loss, stale response replacing a newer source/settings result or manual result edit, duplicate allowance charging, or allowance overruns. User-visible failure is bounded by the overall deadline. No additional numerical uptime SLA has been agreed. |
| NFR-004 | Must / C | **Privacy:** no saved text history or source/result content in LinguaDesk’s persistent logs. Source and result text may exist only for active-workspace/session processing and are not persisted across its end; only necessary usage/operational metadata is retained afterward. A provider must not use submitted content for training; limited, disclosed retention is permitted and must be verified before selection. The exact active-workspace/session boundary and teardown mechanics remain under Q-004. |
| NFR-005 | Must / C | **Accessibility and responsiveness:** complete the core journeys in desktop/mobile browsers, with keyboard and screen-reader access. This includes native mode/language controls, explicit submission, text editing, copy, validation, and processing/failure status. Deferred tools add no MVP accessibility journey. Exact browser coverage and accessibility test criteria are deferred to UX specification. |
| NFR-006 | Must / C | **Cost:** enforce character allowances and establish the separate monthly monetary cap before launch. Suspend further paid processing at that ceiling, preserving work and returning a budget-related availability error. Assess provider exposure using the actual serving arrangements, including unsuccessful attempts. Alternative context is evaluated only when DF-001 is selected. The monetary amount remains unset. |
| NFR-007 | Should / C | **Visual direction:** minimalist, professional, office-like presentation; monochrome icons are preferred. Detailed layout, typography, and component design belong to UX specification. |
| NFR-008 | Proposed Must / P-005 | **Security and abuse resistance:** treat submitted text as content rather than executable instructions; protect account/workspace isolation and provider credentials; constrain abusive attempt volume independently of successful-character accounting. Specific controls and rates are deferred to security/architecture specification. |

### Performance targets

| Operation | At least 95% complete within | Overall deadline including fallbacks |
| --- | ---: | ---: |
| Translation | **10 seconds** | **30 seconds** |
| Full-text rewriting | **10 seconds** | **30 seconds** |
| Sentence alternatives — deferred DF-001, not an MVP gate | **5 seconds** (historical target to revisit) | **15 seconds** (historical target to revisit) |

Percentile benchmark: input/context up to **1,000 characters**, with **10 concurrent requests**, measured from API submission to complete response; editing time before explicit activation is outside the measurement. Overall deadlines also apply at the maximum supported input length. Ten concurrent requests is a benchmark condition, not the expected continuous workload or an approved concurrency limit.

## 8. Evaluation and release acceptance

### Language-quality evaluation

Use a fixed multilingual test set, AI-assisted grading, and human checks in every supported language. Cover all 12 translation directions, four rewriting languages, every style/tone choice, and the single Correction only mode. Deferred alternatives and comparison UI are outside MVP evaluation. Include Simplified/Traditional input, grammar/spelling cases, names, numerical values, URLs, and paragraph/list structure.

Score usefulness and preservation of meaning, grammar/spelling, requested style/tone, and supported language/script. When DF-001 is selected, its evaluation must additionally cover distinctness, context fit and option count. Critical failures include invented facts, material omissions, meaning reversal, changed numerical values, or the wrong output language. Oversized translation is tested as rejection with no provider transformation or charge; there is no partial-result quality gate in MVP.

Apply the same criteria to every model/settings configuration on the routes it may serve. The Translation family candidate must pass all 12 directions and the Rewriting candidate all four languages and modes; assess a configured fallback to the same standard. Record per-route/language results rather than relying only on an aggregate score. Dataset size, grading rubric, human sample coverage, and reference-answer construction remain evaluation-specification decisions; the **90% threshold and human checks in every language are confirmed**.

### Release gates

| Gate | Required evidence |
| --- | --- |
| RG-001 — Core behavior | All current MVP Must criteria pass: explicit submission, validation/oversize blocking, single-dropdown defaults, source protection, stale/manual-edit protection, editable/copyable complete output. Deferred/retired criteria are not required. |
| RG-002 — Quality | Fixed-set results meet NFR-001, human checks cover every language, and no critical meaning/factual error remains in the release evaluation set. |
| RG-003 — Performance | Benchmark results meet NFR-002; maximum-length cases and fallback scenarios respect overall deadlines. Failed/time-out requests do not count as timely completions. |
| RG-004 — Accounting and recovery | Verify user/global sharing, UTC reset, full-submission charges, zero charge for rejected oversize input, duplicate/retry protection, configured fallback/failures, stale successes, interruption/status recovery and concurrent requests. |
| RG-005 — Access and integration | Verify local registration/sign-in, email verification, password recovery, meaningful errors, and an independent authenticated API consumer completing Translation and Rewriting with usage information. Google and alternatives are deferred. |
| RG-006 — Web access | Verify core journeys on the agreed desktop/mobile browser matrix and through keyboard/screen-reader interaction. |
| RG-007 — Privacy and cost | Verify LinguaDesk’s text-retention/logging behavior, provider training/retention eligibility, eligible model configurations, and a configured monetary ceiling. |
| RG-008 — Specification readiness | Resolve current-MVP pre-launch questions in Section 11. Deferred-feature dependencies are excluded from this release gate. Review proposed safeguards before making them binding. |

There is no agreed minimum pilot-user count or adoption gate. Actual launch evidence, model evaluation, and performance testing will be produced during subsequent work.

## 9. Dependencies and material risks

| Dependency or risk | Product implication / required response |
| --- | --- |
| Default model’s serving arrangement is unverified | Confirm no-training terms, retention, access, model settings, and pricing before launch. The model name alone does not settle provider eligibility. |
| Qualified human review in all four languages | Arrange reviewers and an evaluation protocol; AI grading alone does not satisfy the chosen release approach. |
| Configured fallback models and credentials | Each candidate needs applicable quality/latency evaluation. Sharing a provider can leave correlated outages; assess this during model/provider selection. |
| Explicit resubmission and charged stale successes | A later submission charges the full text on success. Manual edits protect the displayed result but cannot reverse an already successful charge; communicate usage and unknown outcomes clearly. |
| Whole-text length limits | A user must shorten oversized input manually. Clear validation must not discard text or secretly submit a prefix. |
| Failed attempts | Character limits alone do not bound provider expenditure. Establish the monetary ceiling and evaluate the proposed abuse controls. |
| Deferred scope accidentally returning | Later-phase designs must use Section 3.1, not reinstate obsolete UX contracts from Git. No targeted sentence correspondence or dual mode controls should leak into MVP or later work by default. |
| Shared global allowance | One user affects availability for others. Architecture reserves capacity for integrity; no additional product fairness policy has been agreed. |
| Email delivery and provider availability | Complete real local-account email and provider integration/failure checks. Google is not a current prerequisite. |

## 10. Proposal dispositions and remaining proposals

Accepted and amended dispositions below are incorporated into the confirmed requirements. Proposed items remain recommendations and do not become binding through inclusion here.

| ID | Disposition | Decision and remaining downstream work |
| --- | --- | --- |
| P-001 | **Amended 2026-09-08** | Explicit Translate/Rewrite replaces automatic pause processing for MVP; that timing capability is deferred as DF-003. Previous-result preservation and stale-response protection remain binding. |
| P-002 | **Amended 2026-09-08** | Manual result edits remain local, uncharged and protected from older responses. Later explicit submission may replace them. Targeted sentence metadata preservation is cut; future assistance invalidates all metadata after any manual edit. |
| P-003 | **Amended 2026-09-08** | Empty, unsupported, uncertain, same-language and oversized input is blocked without successful-transformation charge. Correcting it enables explicit submission; never automatic resumption. Both features reject oversize input; prefix translation is DF-006. |
| P-004 | **Accepted; defaults amended 2026-09-08** | English UI and memory-only workspace settings/text remain. New rewriting mode is Correction only in one dropdown; separate toggle/None set/Show changes defaults are removed from MVP. Q-004/Q-009 retain workspace ownership. |
| P-005 | **Proposed** | Adopt the security/abuse safeguards in NFR-008, with separate short-term limits on attempts where necessary. These would be additional controls, not a reinstatement of the discarded daily call allowance. Review remains in the PRD, followed by security and architecture specification. |
| P-006 | **Proposed** | Provide API usage documentation/examples and preserve documented behavior within a published API version. Versioning mechanics and the exact compatibility policy remain deferred to PRD review and the API specification. |

## 11. Compact decision log and deferred questions

### Historical decision log and current amendment

D-01–D-16 record the discovery baseline. Their automatic processing, alternatives/comparison, prefix translation, Google, targeted invalidation, and advanced routing clauses are **superseded or deferred by D-17 / Sections 3 and 5**; they are not current MVP instructions. Unaffected product choices remain, including the default model preference subject to eligibility.

| ID | Confirmed decision / superseded alternative |
| --- | --- |
| D-01 | Everyday/professional communication; owner, 10–100 early users, and prospective hiring clients; React SPA and independent API integration. |
| D-02 | All 12 translation directions and four rewriting languages; automatic source detection with override; explicit translation target. |
| D-03 | Both Chinese scripts accepted, Simplified output; one main supported language; plain text and factual/structural preservation. |
| D-04 | Mandatory correction; mutually exclusive style/tone dropdown; default **None set** and **Correction only = false**. Independent simultaneous style/tone controls were superseded. |
| D-05 | Separate translation and rewriting APIs/pages; automatic processing; editable/copyable results; confirmed detailed rewriting interaction and sentence-alternatives operation. |
| D-06 | **20-call daily allowance discarded.** Use 20,000 characters/user/day and 2,000,000 globally, resetting at midnight UTC. |
| D-07 | Charge successful full submissions; alternatives charge selected sentence only. Retries/failures add no charge; successful outdated responses count. |
| D-08 | Configuration-only routing; shared writing/alternatives chain per language; nonnegative integer rule priorities, 0 highest; defaults plus ordered fallbacks. Decimal priorities superseded; no traffic weights. |
| D-09 | Fallback for provider failures/invalid output; same minimum quality for all candidates; recovery invisible; total failure preserves work without charge. |
| D-10 | Open registration with Google/local accounts; local email verification and password reset; no saved text history/logging; no provider training use and limited disclosed retention. |
| D-11 | Default DeepSeek V4 Flash, non-thinking; percentile targets **10/10/5 seconds**, deadlines **30/30/15 seconds** for translation/rewriting/alternatives; monetary cap value deferred. |
| D-12 | **90%** usable-output threshold, no critical factual/meaning errors in release evaluation; fixed set plus AI-assisted grading and human checks in every language; responsive desktop/mobile web with keyboard/screen-reader access. |
| D-13 | P-001 accepted: translation matches rewriting’s configurable one-second pause, waits for completed IME composition before starting that pause, retains the prior result while updating, rejects stale responses, and preserves the prior result on total failure. |
| D-14 | P-002 amended: manual result edits are local and uncharged, invalidate earlier pending responses, and are replaced by the next source/mode-triggered full rewrite; comparison and alternative data are invalidated only for affected sentences. |
| D-15 | P-003 amended: empty input is not processed; uncertain and same-language cases require language correction; all resume automatically when valid. Oversized translation processes and charges a clearly disclosed 5,000-character prefix, while oversized rewriting remains blocked. |
| D-16 | P-004 accepted: the MVP UI is English; preferences and source/result text do not persist across sessions; session-only control state is permitted; new workspaces use the confirmed defaults. |
| D-17 | **2026-09-08 user approval:** adopt all ten dispositions in Section 3.2. MVP uses explicit buttons, plain editable output, single dropdown, native/inline controls, whole-input limits, local accounts, two simple chains, and the independent API. DF-001–DF-007 are later-phase scope; targeted invalidation and the duplicate toggle are retired. |

### Open questions and their scope

| ID | Open decision | Resolution phase / dependency |
| --- | --- | --- |
| Q-001 | Hosting/provider arrangement, verified no-training/retention terms, eligible fallback models/settings, and monetary cap amount | Model/provider evaluation and cost specification, before launch |
| Q-002 | **Resolved for MVP by D-17.** Targeted sentence-correspondence and toggle restoration are cut. No sentence metadata exists in MVP. Future assistance uses whole-result metadata invalidation on manual edits. | No MVP blocker. Future sentence/alternative association design belongs to DF-001; change review belongs to DF-002. |
| Q-003 | Exact Unicode/whitespace/newline counting; retry identity; interrupted/cancelled operations; operation crossing midnight; usage fields and concurrent allowance enforcement semantics | API/feature specification and architecture, before implementation of accounting |
| Q-004 | Confirm per-tab memory teardown, metadata/backup retention, local-account deletion/revocation and provider disclosure. External-account linking is deferred with Google (DF-007). | Privacy/security and local-account specifications before launch; UX fixes observable workspace lifetime. |
| Q-005 | Evaluation corpus size, grading rubric, human review coverage, critical-error examples, and performance-test workload composition | Evaluation specification, before model acceptance |
| Q-006 | API schemas, auth token/protocol choices, versioning/compatibility, error taxonomy, cancellation, and identifier formats | API/security specifications; product capabilities remain as defined here |
| Q-007 | Provider error/refusal classification, output validity, bounded attempt/deadline policy for the two simple chains, and owner diagnostics. Alternative context bounds are deferred DF-001. | LLM/architecture specifications before provider orchestration; no advanced routing prerequisite. |
| Q-008 | Short-term abuse limits, operational alerts, provider-cost tracking/enforcement, and any additional availability SLO | Security/architecture/operations specifications, before launch; safeguard scope awaits P-005 review |
| Q-009 | Update browser/accessibility criteria, native controls, explicit-button/validation/processing/error behavior, oversize blocking, manual-edit protection and workspace teardown. | UX v1.2 resolves active presentation. Deferred surfaces/boundaries are not MVP blockers. |
| Q-010 | Acceptance or amendment of P-005 and P-006; any optional adoption/pilot success measure | PRD review; P-001 through P-004 are resolved, and no additional feature or adoption target is silently assumed |

## 12. References and review status

The primary source is the confirmed discovery conversation. External verification was limited to decisions that depended on current model information:

- [DeepSeek model documentation](https://api-docs.deepseek.com/quick_start/pricing/) — available model/mode information; no price or monthly budget is committed by this PRD.
- [DeepSeek thinking-mode documentation](https://api-docs.deepseek.com/guides/thinking_mode/) — non-thinking support and the need to disable the default thinking mode explicitly.
- [DeepSeek Open Platform terms](https://cdn.deepseek.com/policies/en-US/deepseek-open-platform-terms-of-service.html) — a source for the still-open provider eligibility review; this draft does not assert that the chosen arrangement meets the privacy requirements.
- [User-supplied rewriting widget](https://chatgpt.com/s/w_6a9c7faf64148191abb437fe326d861c) — not accessible during discovery. The user’s written six-rule behavior description is the confirmed source, not an inferred widget implementation.

**Review outcome:** The product owner approved the ten scope dispositions in Section 3.2 on September 8, 2026, superseding affected discovery decisions and P-001–P-004 clauses. Current behavior is specified in Sections 3–8; DF-001–DF-007 preserve later intent without blocking MVP. P-005/P-006 remain proposed. This baseline is ready for scoped planning, not evidence of implementation or release readiness.
