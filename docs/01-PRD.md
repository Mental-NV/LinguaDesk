# LinguaDesk — Product Requirements Document

**Version:** 0.1 · **Status:** Review draft, not yet agreed · **Date:** September 5, 2026 (UTC)

This document consolidates the discovery decisions. It specifies product outcomes for later UX, architecture, API, and feature specifications. It contains no implementation plan or development task breakdown.

**Reading conventions:** Confirmed requirements and their observable consequences are labeled **C**. Unaccepted recommendations are labeled **P** and collected in Section 10. **Must** means required for the MVP; **Should** indicates a preference. **Proposed Must** becomes binding only if accepted during review. Deferred questions have an owner phase and remain unresolved. Release criteria describe work to be verified later; they are not claims that the product has been implemented or tested.

## 1. Product summary and problem

LinguaDesk is a DeepL-inspired, API-first product for LLM-powered text translation and rewriting. It helps people prepare messages, emails, and short professional passages while preserving meaning and giving them controlled ways to improve wording.

The product provides a focused workflow instead of requiring users to repeatedly explain translation and editing instructions to a general chat assistant. It keeps the original text available, makes rewriting changes reviewable, and offers sentence alternatives without requiring a complete rewrite each time.

### Users and principal use cases

| Audience | Principal use cases | Product value |
| --- | --- | --- |
| Project owner and approximately 10–100 early users | Translate messages; correct grammar and spelling; choose a writing style or tone; refine individual sentences | Convenient, predictable everyday communication |
| Prospective hiring clients | Try the web product and assess its API integration experience | Evidence of experienced full-stack engineering through coherent UX, independent API capabilities, reliable failure handling, and controlled LLM usage |

The initial client is a React SPA with separate Translation and Rewriting pages. Desktop and mobile browsers, keyboard operation, and screen-reader access are MVP requirements. Native mobile and browser-extension clients can integrate independently with the API but do not ship in this MVP.

## 2. Goals and success criteria

| Goal | Confirmed measure |
| --- | --- |
| Useful language output | At least **90% usable outputs** in each evaluated translation direction and each rewriting/alternatives language, assessed separately by operation |
| Preserve essential information | **No critical meaning or factual errors in the release evaluation set**, including invented information or changed numerical values |
| Responsive processing | At least 95% of benchmark requests complete within **10 seconds** for translation/full rewriting and **5 seconds** for alternatives; benchmark conditions are in Section 7 |
| Recover predictably | Fallback, stale-response, validation, and total-failure acceptance criteria pass without losing source text or charging failed operations |
| Enable independent clients | An API consumer can authenticate, translate, rewrite, request sentence alternatives, and obtain usage information without depending on the SPA |
| Keep usage bounded | Enforce the per-request limits, shared character allowances, UTC reset, and successful-operation accounting |
| Demonstrate an accessible web product | Core journeys work on desktop/mobile browsers and with keyboard and screen-reader access |

“Usable” means acceptable without editing or with only minor edits. The minimum quality standard applies to primary and fallback model configurations. Adoption, weekly activity, revenue, and hiring conversion targets have not been agreed; the audience estimate is not an adoption commitment.

## 3. Scope and boundaries

### Confirmed MVP

- Translation among English, Russian, Romanian, and Chinese in all 12 directed pairs.
- Rewriting in all four languages, with mandatory grammar/spelling correction and one optional style or tone.
- Automatic processing after a pause; complete translation results and complete replacements of rewritten results.
- Editable, copyable results; rewriting change highlighting and sentence alternatives.
- Open registration, Google sign-in, and ordinary login/password accounts with email verification and password reset.
- Free access with server-calculated character accounting, per-user/global allowances, and a monetary ceiling whose value remains deferred.
- Configuration-controlled model routing, prioritized rules, ordered fallbacks, and model-specific settings.
- Responsive React SPA, separate feature pages, and API access to the product capabilities.

### Confirmed exclusions

File translation, voice translation, paid plans/billing, saved text history, Markdown-specific preservation, substantially mixed-language passages, unsupported languages, Traditional Chinese output, combined translation-and-rewriting operations, simultaneous style-and-tone selection, routing administration UI, and traffic weighting are outside MVP scope.

Native mobile and browser-extension client delivery are outside the initial release. Subsequent architecture, detailed UI design, OpenAPI contracts, implementation plans, and development tasks are separate deliverables. No excluded feature has been committed to a later release.

Persistent saved preferences and UI localization have not been accepted as features; proposed boundaries appear in Section 10. Administration beyond the explicitly excluded routing UI is not an implied requirement.

## 4. Main user journeys

| Journey | Expected outcome and important failure paths |
| --- | --- |
| Get access | A user registers or signs in with Google or login/password. Local-account email verification is required before LLM use; forgotten passwords have a self-service reset flow. Authentication or verification failures prevent processing. |
| Translate a message | Enter text, use automatic source detection or a manual source language, and select a target language. After a pause, obtain a complete translation; edit or copy the result. Unsupported/substantially mixed input and input over 5,000 characters receive validation feedback without allowance deduction. |
| Correct or restyle writing | Enter up to 2,000 characters. The initial settings correct grammar/spelling. Optionally choose one style or tone. Processing begins after the configured pause. Source editing remains available; the previous result stays visible during updates. |
| Refine one sentence | Open a result sentence’s alternatives, review the default three options, and select one. Only that sentence changes; other choices remain intact. Reopening the menu reuses its options, and selecting an option causes no full rewrite or extra usage charge. |
| Continue writing during processing | Source edits or mode changes make the displayed result outdated and close the alternatives menu. Only a response matching the latest source/settings may replace it. The next complete rewrite resets prior alternative choices. Successful outdated responses still count toward allowance. |
| Recover from failure | Provider or invalid-output failures try the next eligible candidate within the overall deadline. Successful recovery is invisible. Total failure preserves source and previous result, shows an error, and deducts no character allowance. |
| Reach an allowance | Processing is constrained by both the user and global allowance. Clients receive usage information. A limit-related failure preserves the work and consumes no further allowance; exact display wording is deferred to UX design. |

## 5. Functional requirements

All requirements in this section are **C / Must**, except the explicitly identified visual preference. IDs remain stable through subsequent revisions.

### 5.1 Access, languages, and shared input behavior

| ID | Requirement | Observable acceptance criteria |
| --- | --- | --- |
| FR-001 | Open account registration and sign-in | A new user can register and sign in with Google or ordinary login/password credentials. HTTP Basic Authentication is not a confirmed protocol requirement. |
| FR-002 | Local-account verification and recovery | An unverified local account cannot invoke LLM operations. After verification it can, subject to limits. A user who forgets a password can complete a self-service reset. |
| FR-003 | Separate launch features | The React SPA provides separate Translation and Rewriting pages. Translation and rewriting are separate API capabilities; there is no combined transformation operation. |
| FR-004 | Source-language selection | Both features default to automatic source-language detection and permit manual override. Translation requires an explicitly selected target language. |
| FR-005 | Supported input languages | Accept one main language from English, Russian, Romanian, or Chinese. Foreign names, technical terms, and short foreign phrases are allowed. Substantially mixed passages or substantial unsupported-language content produce validation feedback and no allowance deduction. |
| FR-006 | Chinese scripts | Accept Simplified and Traditional Chinese input. Chinese output is Simplified, for both translation and rewriting. The script conversion is an explicit exception to preserving a Chinese input’s script. |
| FR-007 | Input-length limits | Translation accepts at most **5,000 characters** per operation; full rewriting accepts at most **2,000**. Over-limit input is not processed successfully or charged. Oversized rewriting text remains editable with a validation message. Exact Unicode counting rules are deferred under Q-003. |
| FR-008 | Plain-text fidelity | Preserve paragraph/list structure, factual meaning, numerical values, names’ identities, and URLs. Transliteration may preserve a name’s identity without preserving its spelling. Markdown-specific syntax preservation is not promised. |

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
| FR-011 | Automatic translation and complete output | Processing starts after a pause rather than requiring a Translate button. Display a complete result when ready. Users can edit and copy it. The translation pause duration remains a proposed default in P-001. |

### 5.3 Rewriting and sentence alternatives

The **Styles** dropdown contains exactly one selection from:

| Choice category | Values |
| --- | --- |
| Preserve existing style/tone | **None set** — the default |
| Writing style | simple, casual, business, academic |
| Tone | enthusiastic, friendly, confident, diplomatic |

| ID | Requirement | Observable acceptance criteria |
| --- | --- | --- |
| FR-012 | Rewriting language | Support all four languages. Rewrite in the source language, subject to the Simplified Chinese output policy; rewriting does not translate into another language. |
| FR-013 | Mandatory correction | Grammar and spelling correction apply in every rewriting mode and cannot be disabled. Grammar-only passes make minimal corrections, avoiding discretionary restyling. |
| FR-014 | Defaults and exclusive transformation choice | A new workspace starts with **Correction only = false** and **Styles = None set**, producing grammar/spelling correction while preserving style/tone. A user can choose one style or one tone, never both. |
| FR-015 | Correction-only override | Enabling **Correction only** disables the Styles dropdown and applies correction alone, regardless of a previously selected style/tone. The toggle is not a switch that disables grammar correction when off. |
| FR-016 | Automatic rewrite timing | Start a request **1 second after the last edit**, with the delay configurable. Typing, pasting, and mode changes restart the timer. Chinese input composition must finish before submission. |
| FR-017 | Protect the source | Source text remains an ordinary editable field throughout processing. Rewriting and alternative selection never replace its contents, modify its formatting, or move its typing cursor. |
| FR-018 | Apply only current results | Retain the previous result during processing and label it **Updating…**. Apply a response only when it matches the latest source/settings; ignore outdated responses. Failures preserve source and previous result. |
| FR-019 | Review changes in the result | **Show changes** is enabled by default. Highlight revised words in the result only. Sentence comparison details can show original wording, including deletions. Copy produces clean text, with no highlighting markup. |
| FR-020 | Generate alternatives on demand | Activating any result sentence offers **2–4 distinct alternatives**, defaulting to **3**, generated using context and the selected style/tone. In correction-only behavior, activating a sentence explicitly requests alternative phrasing while the automatic full pass remains minimal. |
| FR-021 | Select and reuse alternatives | Selecting an option changes only that result sentence, preserves other sentence choices, and starts no full rewrite. Reopening the menu reuses generated options. Reopening or choosing an option adds no allowance charge. |
| FR-022 | Invalidate results after source/settings changes | Editing source or changing mode closes the alternatives menu and marks the result outdated. The next complete rewrite replaces the previous result and resets its alternative choices. |
| FR-023 | Editable and copyable results | Users can directly edit and copy the generated result. Copy includes selected alternatives and current result text. Detailed interactions between manual result edits, comparisons, alternatives, and pending responses remain explicit proposals/questions under P-002 and Q-002. |

### 5.4 Allowances and accounting

| Limit | Confirmed value |
| --- | ---: |
| User daily allowance, shared across translation and rewriting | **20,000 characters** |
| Global daily allowance, shared across all users | **2,000,000 characters** |
| Daily reset for both | **00:00 UTC** |

| ID | Requirement | Observable acceptance criteria |
| --- | --- | --- |
| FR-024 | Character-based usage | The earlier 20-call allowance is superseded. Full translation/rewriting charges the submitted text’s length for a successful operation. A new successful submission charges the full submitted text, even after a small edit. |
| FR-025 | Alternatives accounting | A successful alternatives generation charges the selected sentence’s length once for the entire option set. Context and generated options add no character charge. Both daily allowances include this usage. |
| FR-026 | Success-only, single-operation charging | Failed operations and retry attempts add no charge. One logical operation is charged at most once when it succeeds, including success after retries/fallback. Successful responses discarded as outdated still count. Retry identity and interruption edge cases are deferred to the API specification. |
| FR-027 | Enforce shared daily allowances | Server-side accounting enforces the user and global limits across clients and resets both at midnight UTC. Concurrent operations must not create duplicate deductions or successful usage beyond either allowance. |
| FR-028 | Report usage to clients | The server calculates usage and returns it to clients. Clients can reflect consumption and allowance-related failures consistently; exact response fields belong to the API contract. |

The limits are configuration-controlled product values rather than fixed client assumptions. A monetary cap is also required; its amount remains deferred. Character allowances are not a representation of provider token charges, especially for alternative context, retries, and failures.

### 5.5 Routing and fallback

| ID | Requirement | Observable acceptance criteria |
| --- | --- | --- |
| FR-029 | Owner configuration | The owner configures routing without an administration UI. Model configurations support relevant provider/model settings, including thinking/reasoning controls where supported. |
| FR-030 | Routing families | Translation matches directed source/target routes. Full rewriting and sentence alternatives share a configured model chain per rewriting language. They cannot independently choose different chains under the confirmed MVP policy. |
| FR-031 | Rule precedence | Priorities are nonnegative integers: **0 wins over 1, then 2, …**. Select the lowest-numbered matching rule. Equal-priority overlapping rules are rejected as ambiguous. Decimal priorities and traffic weighting are excluded. |
| FR-032 | Defaults and fallback order | An explicitly configured default chain covers unmatched routes in each operation family. The winning rule supplies its own ordered candidate list; rule priority and model fallback order are distinct. Exhausting a matching rule’s chain ends the operation rather than implicitly merging other rules’ candidates. |
| FR-033 | Eligible fallback triggers | Try the next candidate for provider failures, including timeouts/throttling/service errors, and clearly invalid outputs such as empty or unusable responses. Invalid user input, user authentication failures, and exhausted allowances end the request without model fallback. |
| FR-034 | Quality and failure outcomes | Every candidate must pass the same applicable minimum quality criteria. Successful fallback is invisible to users. Exhaustion or deadline expiry produces a clear failure, preserves work, and consumes no character allowance. |

**Initial default:** DeepSeek V4 Flash in non-thinking mode. Official documentation confirms that non-thinking mode is supported and thinking is enabled by default, so this configuration must explicitly disable it. This verifies mode availability, not LinguaDesk’s quality, latency, or privacy eligibility. [DeepSeek thinking-mode documentation](https://api-docs.deepseek.com/guides/thinking_mode/)

The serving provider/contract, eligible fallback configurations, and their evaluations remain pre-launch dependencies. Selection of a model does not establish compliance with the agreed no-training/retention requirements.

## 6. API-facing product requirements

| ID | Priority / status | Requirement and observable acceptance criteria |
| --- | --- | --- |
| FR-035 | Must / C | Expose translation, full-text rewriting, and sentence alternatives as distinct operations. Full rewriting and alternatives use version and sentence identifiers so clients can associate results and options with the appropriate text revision and sentence. Identifier representation and lifecycle belong to the API specification. |
| FR-036 | Must / C | Expose the supported language choices, rewriting modes, limits, and usage behavior independently of the SPA. All clients are subject to the same authenticated access and server-side allowances. A separate client can complete all three operation types without executing React UI logic. |
| FR-037 | Must / C | Return meaningful failures for authentication/verification, invalid or unsupported input, input length, allowance/budget exhaustion, and processing failure/deadline expiry. The API contract defines categories and recovery guidance; it must distinguish these from successful output. |

Translation returns a complete result. Rewriting applies complete result replacements as specified in FR-018/022; intermediate generated text is not an agreed interactive result. Exact transport, endpoint schemas, token formats, cancellation mechanisms, and retry identifiers are deferred.

API documentation and integration examples should make the above behavior independently understandable (**P-006**). Compatibility/versioning guarantees beyond these product capabilities are not yet agreed.

## 7. Nonfunctional requirements

| ID | Priority / status | Expected outcome and acceptance measure |
| --- | --- | --- |
| NFR-001 | Must / C | **Quality:** at least 90% usable outputs in each evaluated route/language and operation, with no critical meaning/factual errors in the release set. Apply the same standard to every eligible primary/fallback configuration. |
| NFR-002 | Must / C | **Performance:** meet the percentile targets and overall deadlines below. These are acceptance targets requiring measurement, not assumed provider performance. |
| NFR-003 | Must / C | **Reliability:** controlled failure/concurrency tests show no source loss, stale response replacing a newer source/settings result, unintended replacement of other sentence choices, duplicate allowance charging, or allowance overruns. User-visible failure is bounded by the overall deadline. No additional numerical uptime SLA has been agreed. |
| NFR-004 | Must / C | **Privacy:** no saved text history or source/result content in LinguaDesk’s persistent logs. Text is available for active-workspace processing; only necessary usage/operational metadata is retained afterward. A provider must not use submitted content for training; limited, disclosed retention is permitted and must be verified before selection. |
| NFR-005 | Must / C | **Accessibility and responsiveness:** complete the core journeys in desktop/mobile browsers, with keyboard and screen-reader access. This includes mode controls, sentence alternatives, comparison details, copy, validation, and processing/failure status. Exact browser coverage and accessibility test criteria are deferred to UX specification. |
| NFR-006 | Must / C | **Cost:** enforce character allowances and establish the separate monthly monetary cap before launch. Suspend further paid processing at that ceiling, preserving work and returning a budget-related availability error. Assess provider exposure using the actual serving arrangements, including uncharged alternative context and unsuccessful attempts. The monetary amount remains unset. |
| NFR-007 | Should / C | **Visual direction:** minimalist, professional, office-like presentation; monochrome icons are preferred. Detailed layout, typography, and component design belong to UX specification. |
| NFR-008 | Proposed Must / P-005 | **Security and abuse resistance:** treat submitted text as content rather than executable instructions; protect account/workspace isolation and provider credentials; constrain abusive attempt volume independently of successful-character accounting. Specific controls and rates are deferred to security/architecture specification. |

### Performance targets

| Operation | At least 95% complete within | Overall deadline including fallbacks |
| --- | ---: | ---: |
| Translation | **10 seconds** | **30 seconds** |
| Full-text rewriting | **10 seconds** | **30 seconds** |
| Sentence alternatives | **5 seconds** | **15 seconds** |

Percentile benchmark: input/context up to **1,000 characters**, with **10 concurrent requests**, measured from API submission to complete response and excluding the typing pause. Overall deadlines also apply at the maximum supported input length. Ten concurrent requests is a benchmark condition, not the expected continuous workload or an approved concurrency limit.

## 8. Evaluation and release acceptance

### Language-quality evaluation

Use a fixed multilingual test set, AI-assisted grading, and human checks in every supported language. Cover all 12 translation directions, four rewriting languages, every style/tone choice, the two correction-only control states, and sentence alternatives. Include Simplified/Traditional input, grammar/spelling cases, names, numerical values, URLs, and paragraph/list structure.

Score usefulness and preservation of meaning, grammar/spelling, requested style/tone, and supported language/script. An alternatives evaluation must also assess distinctness, context fit, and the required option count. Critical failures include invented facts, material omissions, meaning reversal, changed numerical values, or the wrong output language.

Apply the same criteria to every model/settings configuration on the routes it may serve. A shared rewriting/alternatives candidate must pass both operations for its languages. Record per-route/language results rather than relying only on an aggregate score. Dataset size, grading rubric, human sample coverage, and reference-answer construction remain evaluation-specification decisions; the **90% threshold and human checks in every language are confirmed**.

### Release gates

| Gate | Required evidence |
| --- | --- |
| RG-001 — Core behavior | All confirmed Must functional acceptance criteria pass, including defaults, source protection, change display, and independent sentence replacement. |
| RG-002 — Quality | Fixed-set results meet NFR-001, human checks cover every language, and no critical meaning/factual error remains in the release evaluation set. |
| RG-003 — Performance | Benchmark results meet NFR-002; maximum-length cases and fallback scenarios respect overall deadlines. Failed/time-out requests do not count as timely completions. |
| RG-004 — Accounting and recovery | Verify user/global sharing, UTC reset, full-input versus sentence-only charges, retries, provider failures, stale successes, repeated menus, and concurrent requests. |
| RG-005 — Access and integration | Verify both sign-in methods, local email verification, password recovery, meaningful errors, and an independent API consumer completing all operation types with usage information. |
| RG-006 — Web access | Verify core journeys on the agreed desktop/mobile browser matrix and through keyboard/screen-reader interaction. |
| RG-007 — Privacy and cost | Verify LinguaDesk’s text-retention/logging behavior, provider training/retention eligibility, eligible model configurations, and a configured monetary ceiling. |
| RG-008 — Specification readiness | Resolve the pre-launch questions in Section 11 through their designated specifications. Review proposed safeguards before turning them into binding requirements. |

There is no agreed minimum pilot-user count or adoption gate. Actual launch evidence, model evaluation, and performance testing will be produced during subsequent work.

## 9. Dependencies and material risks

| Dependency or risk | Product implication / required response |
| --- | --- |
| Default model’s serving arrangement is unverified | Confirm no-training terms, retention, access, model settings, and pricing before launch. The model name alone does not settle provider eligibility. |
| Qualified human review in all four languages | Arrange reviewers and an evaluation protocol; AI grading alone does not satisfy the chosen release approach. |
| Configured fallback models and credentials | Each candidate needs applicable quality/latency evaluation. Sharing a provider can leave correlated outages; assess this during model/provider selection. |
| Automatic resubmission and charged stale successes | Small edits can repeatedly consume the full input length. Usage feedback and debouncing must be understandable; no incremental-text discount is agreed. |
| Unmetered context and failed attempts | Character limits alone do not bound provider expenditure. Establish the monetary ceiling and evaluate the proposed abuse controls. |
| Manual result edits alongside comparisons/alternatives | Sentence correspondence and pending responses can become ambiguous. Resolve P-002/Q-002 before detailed feature specification. |
| Shared global allowance | One user’s consumption affects availability for others. No additional reservation or fairness policy has been agreed. |
| Google authentication, email delivery, and provider availability | Complete real integration and failure checks; provider/infrastructure choices belong to subsequent specifications. |

## 10. Proposed defaults and assumptions — not yet agreed

These items are review recommendations, not confirmed scope. Their inclusion does not override any confirmed requirement.

| ID | Proposal | Decision phase |
| --- | --- | --- |
| P-001 | Use the same configurable **1-second** pause for translation, with composition-aware submission and the rewriting pattern for pending/outdated results during typing. Automatic translation, complete output, and preservation on total failure are already confirmed; the additional interaction details are proposals. | PRD review / UX specification |
| P-002 | Direct result editing makes no model request and incurs no character charge. The next full rewrite replaces manual edits as well as alternative choices. Edits invalidate sentence options/comparison metadata where correspondence no longer holds. Exact handling of an in-flight response versus a manual result edit remains Q-002. | PRD review / UX and feature specifications |
| P-003 | Empty/whitespace-only input starts no automatic request; uncertain detection asks for manual language selection; same-language translation asks for a different target without inference or charge. Do not silently truncate oversized text. | PRD review / feature specification |
| P-004 | Keep persistent saved preferences and UI localization outside MVP; start new workspaces with the confirmed defaults and use an English UI. Active-session behavior is defined in UX specification. Neither exclusion has yet been accepted. | PRD review |
| P-005 | Adopt the security/abuse safeguards in NFR-008, with separate short-term limits on attempts where necessary. These are additional proposed controls, not a reinstatement of the discarded daily call allowance. | PRD review / security and architecture specifications |
| P-006 | Provide API usage documentation/examples and preserve documented behavior within a published API version. Versioning mechanics and the exact compatibility policy remain deferred. | PRD review / API specification |

## 11. Compact decision log and deferred questions

### Decision log

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

### Deferred questions

| ID | Open decision | Resolution phase / dependency |
| --- | --- | --- |
| Q-001 | Hosting/provider arrangement, verified no-training/retention terms, eligible fallback models/settings, and monetary cap amount | Model/provider evaluation and cost specification, before launch |
| Q-002 | Manual result edits versus pending responses; sentence identity after edits/merges/splits; valid comparisons and cached alternatives; restoring dropdown selection after the correction-only toggle | UX and rewriting feature specifications, before implementation of these interactions |
| Q-003 | Exact Unicode/whitespace/newline counting; retry identity; interrupted/cancelled operations; operation crossing midnight; usage fields and concurrent allowance enforcement semantics | API/feature specification and architecture, before implementation of accounting |
| Q-004 | Active-workspace lifetime, temporary text handling, metadata retention duration, account deletion/linking, and provider disclosure details | Privacy/security and account feature specifications, before launch; no saved text history remains fixed |
| Q-005 | Evaluation corpus size, grading rubric, human review coverage, critical-error examples, and performance-test workload composition | Evaluation specification, before model acceptance |
| Q-006 | API schemas, auth token/protocol choices, versioning/compatibility, error taxonomy, cancellation, and identifier formats | API/security specifications; product capabilities remain as defined here |
| Q-007 | Provider error/refusal classification, concrete output-validity checks, sentence-alternative context bounds, attempt time budgets, and owner diagnostics | LLM feature and architecture specifications, within agreed fallback/deadline policies |
| Q-008 | Short-term abuse limits, operational alerts, provider-cost tracking/enforcement, and any additional availability SLO | Security/architecture/operations specifications, before launch; safeguard scope awaits P-005 review |
| Q-009 | Browser coverage, accessibility acceptance checklist, responsive layouts, and detailed validation/loading/limit messages | UX specification; desktop/mobile, keyboard, and screen-reader support are confirmed |
| Q-010 | Acceptance or amendment of P-001 through P-006; any optional adoption/pilot success measure | PRD review; no additional feature or adoption target is silently assumed |

## 12. References and review status

The primary source is the confirmed discovery conversation. External verification was limited to decisions that depended on current model information:

- [DeepSeek model documentation](https://api-docs.deepseek.com/quick_start/pricing/) — available model/mode information; no price or monthly budget is committed by this PRD.
- [DeepSeek thinking-mode documentation](https://api-docs.deepseek.com/guides/thinking_mode/) — non-thinking support and the need to disable the default thinking mode explicitly.
- [DeepSeek Open Platform terms](https://cdn.deepseek.com/policies/en-US/deepseek-open-platform-terms-of-service.html) — a source for the still-open provider eligibility review; this draft does not assert that the chosen arrangement meets the privacy requirements.
- [User-supplied rewriting widget](https://chatgpt.com/s/w_6a9c7faf64148191abb437fe326d861c) — not accessible during discovery. The user’s written six-rule behavior description is the confirmed source, not an inferred widget implementation.

**Review outcome pending:** confirm the consolidated requirements, accept or amend the proposals, and resolve any contradictions raised during review. A subsequent revision can be marked agreed after explicit product-owner acceptance. Deferred architecture/API/UX decisions remain with their stated phases.
