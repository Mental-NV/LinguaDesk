# LinguaDesk — Product Requirements Document

**Version:** 1.1 · **Status:** Simplified MVP product baseline; unattended AI development and automated release acceptance; P-005/P-006 remain proposed · **Updated:** October 25, 2026 (Europe/Moscow)

This document consolidates the discovery decisions. It specifies product outcomes for later UX, architecture, API, and feature specifications. It contains no implementation plan or development task breakdown.

**Reading conventions:** Confirmed requirements and their observable consequences are labeled **C**. Stable `P-*` items and their accepted, amended, or proposed dispositions are collected in Section 10. **MVP / Must** means required for the current release; **Deferred** means approved later-phase intent, excluded from MVP implementation and release gates; **Retired** means removed and not automatically scheduled for later; **Should** indicates a preference. **Proposed Must** becomes binding only if accepted during review. Deferred questions have an owner phase and remain unresolved. Release criteria describe work to be verified later; they are not claims that the product has been implemented or tested.

## 1. Product summary and problem

LinguaDesk is a DeepL-inspired, API-first product for LLM-powered text translation and rewriting. It helps people prepare messages, emails, and short professional passages while preserving meaning and giving them controlled ways to improve wording.

The product provides a focused workflow instead of requiring users to repeatedly explain translation and editing instructions to a general chat assistant. It keeps the original text available and returns complete, editable, natively copyable translations or rewrites after a pause in eligible input. Sentence alternatives and change-review tools are deferred under Section 3.1.

### Users and principal use cases

| Audience | Principal use cases | Product value |
| --- | --- | --- |
| Project owner and approximately 10–100 early users | Translate messages; correct grammar and spelling; choose a writing style or tone; manually edit the complete result | Convenient, predictable everyday communication |
| Prospective hiring clients | Try the web product and assess its API integration experience | Evidence of experienced full-stack engineering through coherent UX, independent API capabilities, reliable failure handling, and controlled LLM usage |

The initial client is a React SPA with separate Translation and Rewriting pages. MVP release assurance covers the pinned Playwright Chromium engine at desktop and narrow responsive viewports. Other engines, branded/current/previous browser versions and physical mobile devices are best-effort compatibility targets with optional nonblocking checks, not a required release matrix (D-22). Native mobile and browser-extension clients can integrate independently with the API but do not ship in this MVP.

## 2. Goals and success criteria

| Goal | Confirmed measure |
| --- | --- |
| Useful language output | At least **90% usable outputs** in each evaluated translation direction and each rewriting language, assessed separately by operation |
| Preserve essential information | **No critical meaning or factual errors in the submitted text covered by the release evaluation set**, including invented information or changed numerical values |
| Responsive processing | At least 95% of benchmark requests complete within **10 seconds** for translation/full rewriting; benchmark conditions are in Section 7 |
| Recover predictably | Fallback, stale-response, validation, and total-failure acceptance criteria pass without losing source text or charging failed operations |
| Enable independent clients | An API consumer can authenticate with a local account, translate, rewrite, and obtain usage information without depending on the SPA |
| Keep usage bounded | Enforce the per-request limits, shared character allowances, UTC reset, and successful-operation accounting |
| Deliver usable web workflows | A verified user can translate text and rewrite text in the published SPA, receive a complete editable/copyable result, and retain their work through validation or processing failure |
| Demonstrate an accessible web product | Core journeys work in Firefox and Chrome on desktop and mobile |

“Usable” means acceptable without editing or with only minor edits. The minimum quality standard applies to primary and fallback model configurations. Adoption, weekly activity, revenue, and hiring conversion targets have not been agreed; the audience estimate is not an adoption commitment.

## 3. Scope and boundaries

### Confirmed MVP

- Translation among English, Russian, Romanian, and Chinese in all 12 directed pairs; automatic source detection and manual override remain.
- Rewriting in all four languages, with mandatory grammar/spelling correction and one native dropdown: **Correction only** (default) or one style/tone.
- Translation and rewriting start automatically after a one-second pause in eligible source/settings changes. IME composition, result editing, reconnecting, and allowance resets do not independently start paid processing.
- Complete results in plain editable text fields; native text selection/copy, source preservation, outdated-response protection, and safe recovery.
- Translation blocks input over 5,000 characters; rewriting blocks input over 2,000. Neither silently truncates or processes a prefix.
- Open local email/password registration and local sign-in. New accounts are automatically treated as verified for the MVP; real email-address verification and email-delivered password recovery are deferred under DF-008. Independent API authentication remains in scope.
- Free access with successful-character accounting, per-user/global daily allowances, and a separate monetary ceiling.
- One configured model chain for all Translation routes and one for all Rewriting languages, each with one primary and at most one fallback. Existing model eligibility and quality requirements remain.
- Responsive React SPA with separate feature pages, native selectors, inline tools, and an independent API for both operations and usage.

### 3.1 Deferred scope register (product authority)

See [Deferred scope register](product/deferred-scope.md#31-deferred-scope-register-product-authority).

### 3.2 User decision mapping

See [Approved scope review provenance](archive/scope-review.md#32-user-decision-mapping).

### Confirmed exclusions

File translation, voice translation, paid plans/billing, saved text history, Markdown-specific preservation, substantially mixed-language passages, unsupported languages, Traditional Chinese output, combined translation-and-rewriting operations, simultaneous style-and-tone selection, routing administration UI, and traffic weighting are outside MVP scope.

Native mobile and browser-extension client delivery are outside the initial release. Subsequent architecture, detailed UI design, OpenAPI contracts, implementation plans, and development tasks are separate deliverables. These exclusions remain uncommitted; the explicitly deferred capabilities in Section 3.1 have later-phase intent but no delivery date.

Persistent saved preferences and UI localization are outside MVP scope. The MVP interface is English. Settings may last only within the active workspace/session; a new workspace uses the confirmed defaults. Administration beyond the explicitly excluded routing UI is not an implied requirement.

## 4. Main user journeys

| Journey | Expected outcome and important failure paths |
| --- | --- |
| Get access | Register with local email/password, receive no credential, then sign in. Newly created accounts are automatically verified for this temporary MVP substitute. Real email-address verification and email-delivered password recovery are deferred under DF-008; no Google control or callback exists. |
| Translate a message | Enter supported text within the limit, keep detection or select a source, and select a target. One second after the last eligible source/settings change, Translation starts. Validation failures preserve text; a corrected source/settings revision starts a new pause. Edit and natively copy the complete result. |
| Correct or restyle writing | Enter text within the rewriting limit and choose Correction only or one style/tone. One second after the last eligible source/settings change, Rewriting starts. Source and previous result remain available during processing. |
| Continue editing during processing | Source/settings or manual result edits invalidate the pending text response. A successful outdated operation still updates accounting once. A later automatically triggered operation may replace result edits that predate it; edits made after its dispatch remain protected. |
| Recover from failure | The configured fallback, if any, runs invisibly for eligible failures within the same operation deadline. Definitive failure preserves work and charges zero; explicit Try again starts a new operation only when the prior outcome is known. Unknown outcomes use status recovery, never blind redispatch. |
| Reach a limit | Oversized input, exhausted user/global allowance, or monetary suspension preserves work. A new eligible source/settings edit after availability returns starts a one-second pause; availability changes alone never submit paid work. |

## 5. Functional requirements

Unmarked requirements in this section are **MVP / C / Must**. Explicit **Deferred** and **Retired** rows are excluded from MVP acceptance, including when other requirements refer to this catalog. IDs remain stable through subsequent revisions.

### 5.1 Access, languages, and shared input behavior

| ID | Requirement | Observable acceptance criteria |
| --- | --- | --- |
| FR-001 | Local registration and sign-in | **MVP / Must.** A new user registers and signs in with local email/password credentials. Google sign-in is **deferred (DF-007)**. HTTP Basic Authentication is not a confirmed protocol requirement. |
| FR-002 | Local-account verification and recovery | **MVP / Must temporary substitute.** A newly created local account is durably marked verified without email delivery and receives no credential from registration; the user signs in explicitly. Existing unverified accounts remain unable to invoke LLM operations. Real email-address ownership verification and email-delivered self-service password recovery are **deferred (DF-008)**. |
| FR-003 | Separate launch features | The React SPA provides separate Translation and Rewriting pages. Translation and rewriting are separate API capabilities; there is no combined transformation operation. |
| FR-004 | Source-language selection | **MVP / Must.** Both features default to automatic detection and permit manual override. Detection/eligibility runs for a current paused candidate; uncertain source, substantially unsupported input, or same-language translation produces validation with no transformation or character charge. Translation requires an explicitly selected different target. Correcting input/settings starts a new one-second pause. A server may receive a validation request; this is not a successful paid transformation. |
| FR-005 | Supported input languages | Accept one main language from English, Russian, Romanian, or Chinese. Foreign names, technical terms, and short foreign phrases are allowed. Substantially mixed passages or substantial unsupported-language content produce validation feedback and no allowance deduction. |
| FR-006 | Chinese scripts | Accept Simplified and Traditional Chinese input. Chinese output is Simplified, for both translation and rewriting. The script conversion is an explicit exception to preserving a Chinese input’s script. |
| FR-007 | Input eligibility and length limits | **MVP / Must.** Empty/whitespace-only input starts no operation and incurs no charge. Translation accepts at most **5,000 characters** and full rewriting at most **2,000**; oversized input remains editable and is blocked without processing/charge. The UI has no persistent character counter/limit line. The API enforces the same limits. Shortening the input to an eligible revision starts a new one-second pause. Prefix translation is **deferred (DF-006)**. Q-003's technical counting rule is specified in [API design Section 4](api/operations.md#4-complete-input-and-canonical-counting). |
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
| FR-011 | Paused translation and complete output | **MVP / Must.** After one second without a source or language edit, submit the current eligible input/settings once. Each edit restarts the timer; never submit during IME composition. Result editing or leaving the feature cancels an undispatched timer/queued candidate. A settled unchanged revision does not resubmit. Retain the previous result while updating; only a current response applies and failure preserves it. Return a complete editable result that supports native selection/copy. |

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
| FR-014 | Single exclusive rewriting choice | **MVP / Must.** One dropdown defaults to **Correction only**, preserving style/tone with minimal grammar/spelling corrections. Other choices are the four styles and four tones listed above; exactly one may be selected and correction always applies. Changing the choice restarts the processing pause when source input is eligible. No None set option, separate toggle, or hidden prior-style memory. |
| FR-015 | Separate correction-only override — retired | **Retired, not deferred.** The old toggle/disable/restore behavior is replaced by FR-014. This ID is retained for traceability and creates no implementation or test obligation. |
| FR-016 | Paused rewrite submission | **MVP / Must.** After one second without a source, language, or mode edit, submit the current eligible source and single mode once. Each edit restarts the timer; never submit during IME composition. Result editing or leaving the feature cancels an undispatched timer/queued candidate. A settled unchanged revision does not resubmit. |
| FR-017 | Protect the source | **MVP / Must.** Source remains an ordinary editable field throughout processing. Rewriting never replaces its contents, changes its formatting, or moves its typing cursor. |
| FR-018 | Apply only current results | Retain the previous result during processing and label it **Updating…**. Apply a response only when it matches the latest source/settings and has not been invalidated by a manual result edit; ignore outdated responses. Failures preserve source and previous result. |
| FR-019 | Review changes — deferred | **Deferred (DF-002).** No highlighting, Show changes preference, or comparison view ships in MVP. Plain editable/copyable output is governed by FR-023; future review design must not restore the cut targeted-invalidation requirement. |
| FR-020 | Generate sentence alternatives — deferred | **Deferred (DF-001).** Later intent: on-demand contextual sentence alternatives. The former 2–4/default-3 option contract is design input for that selected phase, not an MVP API/UI/evaluation requirement. |
| FR-021 | Select and reuse alternatives — deferred | **Deferred (DF-001).** Later intent: local sentence replacement and reusable choices without a full rewrite or additional selection charge. No cache or sentence-selection behavior is required now. |
| FR-022 | Outdated and replaced results | **MVP / Must.** Source/settings changes mark the previous result outdated and restart the processing pause. A later current successful full rewrite replaces it, including edits that existed when that operation began. Edits made after dispatch remain protected by FR-018/023. Alternative-menu/cache behavior is **deferred (DF-001)**. |
| FR-023 | Editable and natively copyable results | **MVP / Must.** Users directly edit and natively select/copy plain results; the UI has no Copy result control or copy-status line. A manual result edit changes no source, starts no request, and incurs no charge. Earlier pending responses cannot overwrite it; a later operation triggered by an eligible source/settings revision may replace it. Native copy uses the current edited text. Targeted preservation of unaffected sentence identities/cache/comparison metadata is **cut**. No assistance metadata exists in MVP; future DF-001/002 assistance must be invalidated in full by any manual edit. |

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
| FR-026 | Success-only, single-operation charging | Failed operations and retry attempts add no charge. One logical operation is charged at most once when it succeeds, including success after retries/fallback. Successful responses discarded as outdated still count. Retry identity and interruption behavior are specified in [API design Sections 5–6](api/operations.md#5-submission-identity-duplicates-and-replay); wire details and verification follow in selected implementation slices. |
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

**Initial default (updated provider alias):** DeepSeek-V4.1-Flash via `deepseek-flash` in non-thinking mode. #4 records the current public provider evidence, explicit thinking control and owner-supplied evaluation credential binding. It must still verify the actual serving configuration before relying on it. This is not evidence of LinguaDesk’s quality, latency, or cost eligibility. [DeepSeek thinking-mode documentation](https://api-docs.deepseek.com/guides/thinking_mode/)

The serving arrangement, supported model settings, quality/performance evaluation and monetary bounds remain pre-launch dependencies. Under D-18, provider retention and no-training guarantees are not selection or launch requirements. Low-cost models and provider-managed caching are permitted when they satisfy the remaining functional, quality, performance and cost requirements; this does not assert any provider data-use guarantee.

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
| NFR-004 | Must / C | **Privacy:** no saved text history or source/result content in LinguaDesk’s persistent logs. Source and result text may exist only for active-workspace/session processing and are not persisted across its end; only necessary usage/operational metadata is retained afterward. These are LinguaDesk-managed storage/logging rules. Provider retention duration and no-training guarantees are not eligibility requirements (D-18). The exact active-workspace/session boundary and teardown mechanics remain under Q-004; provider-managed caching is permitted and does not change these application rules or add a LinguaDesk completed-response cache. |
| NFR-005 | Must / C | **Accessibility and responsiveness:** automated core journeys pass in pinned Playwright Chromium at desktop and narrow responsive viewports. This includes native mode/language controls, paused automatic processing, text editing, native copy, validation, essential failure feedback, and executable semantic/contrast/reflow checks. Human verification, a browser-version/engine matrix and physical-device/assistive-technology review are not required. UX/#6 own exact automated checks; untested platform behavior is not claimed as verified. Deferred tools add no MVP accessibility journey. |
| NFR-006 | Must / C | **Cost:** enforce character allowances and establish the separate monthly monetary cap before launch. Suspend further paid processing at that ceiling, preserving work and returning a budget-related availability error. Assess provider exposure using the actual serving arrangements, including unsuccessful attempts. Alternative context is evaluated only when DF-001 is selected. The monetary amount remains unset. |
| NFR-007 | Should / C | **Visual direction:** minimalist, professional, office-like presentation; monochrome icons are preferred. Detailed layout, typography, and component design belong to UX specification. |
| NFR-008 | Proposed Must / P-005 | **Security and abuse resistance:** treat submitted text as content rather than executable instructions; protect account/workspace isolation and provider credentials; constrain abusive attempt volume independently of successful-character accounting. Specific controls and rates are deferred to security/architecture specification. |

### Performance targets

| Operation | At least 95% complete within | Overall deadline including fallbacks |
| --- | ---: | ---: |
| Translation | **10 seconds** | **30 seconds** |
| Full-text rewriting | **10 seconds** | **30 seconds** |
| Sentence alternatives — deferred DF-001, not an MVP gate | **5 seconds** (historical target to revisit) | **15 seconds** (historical target to revisit) |

Percentile benchmark: input/context up to **1,000 characters**, with **10 concurrent requests**, measured from API submission to complete response; the one-second client pause is outside this API measurement. Overall deadlines also apply at the maximum supported input length. Ten concurrent requests is a benchmark condition, not the expected continuous workload or an approved concurrency limit.

## 8. Evaluation and release acceptance

### Language-quality evaluation

Use a fixed multilingual test set, pinned AI grading, automated reference review and AI adjudication in every supported language. AI may author references and calibration labels with separate review/judging roles and recorded identities, revisions and limitations; human checks are optional and nonblocking. Cover all 12 translation directions, four rewriting languages, every style/tone choice, and the single Correction only mode. Deferred alternatives and comparison UI are outside MVP evaluation. Include Simplified/Traditional input, grammar/spelling cases, names, numerical values, URLs, and paragraph/list structure.

Score usefulness and preservation of meaning, grammar/spelling, requested style/tone, and supported language/script. When DF-001 is selected, its evaluation must additionally cover distinctness, context fit and option count. Critical failures include invented facts, material omissions, meaning reversal, changed numerical values, or the wrong output language. Oversized translation is tested as rejection with no provider transformation or charge; there is no partial-result quality gate in MVP.

Apply the same criteria to every model/settings configuration on the routes it may serve. The Translation family candidate must pass all 12 directions and the Rewriting candidate all four languages and modes; assess a configured fallback to the same standard. Record per-route/language results rather than relying only on an aggregate score. Dataset size, grading rubric, automated review coverage, reference-answer construction and workload methodology are specified in [verification plan #6](06-verification-plan.md). The **90% threshold and zero confirmed critical errors remain binding**; D-22 removes mandatory human checks. Missing grades and unresolved critical findings block qualification.

### Release gates

| Gate | Required evidence |
| --- | --- |
| RG-001 — Core behavior | A verified user can complete both current whole-text operations in the published web UI: select the required language/mode, pause one second after eligible input/settings changes, receive a complete editable/natively copyable result, and retain source/prior result through validation or failure. All other current MVP Must criteria also pass, including oversize blocking, single-dropdown defaults and stale/manual-edit protection. API-only, standalone-LLM or route-shell evidence does not satisfy these user-facing outcomes. Deferred/retired criteria are not required. |
| RG-002 — Quality | Fixed-set automated evaluation and AI review cover every language and meet NFR-001; no confirmed critical meaning/factual error or unresolved critical finding remains in the release evaluation set. No human verification/sign-off is required. |
| RG-003 — Performance | Benchmark results meet NFR-002; maximum-length cases and fallback scenarios respect overall deadlines. Failed/time-out requests do not count as timely completions. |
| RG-004 — Accounting and recovery | Verify user/global sharing, UTC reset, full-submission charges, zero charge for rejected oversize input, duplicate/retry protection, configured fallback/failures, stale successes, interruption/status recovery and concurrent requests. |
| RG-005 — Access and integration | Verify automatic-verification registration followed by explicit local sign-in, meaningful errors, and an independent authenticated API consumer completing Translation and Rewriting with usage information. Real email verification/recovery and Google alternatives are deferred under DF-008/DF-007. |
| RG-006 — Web access | Automated core journeys and applicable semantic/contrast/reflow, native editing/copy and workspace-lifecycle checks pass in pinned Playwright Chromium at desktop and narrow viewports. No cross-browser/version/device matrix or manual pass is required. Screen-reader, physical-device/native-IME and additional-browser observations are optional and nonblocking; untested combinations are not reported as verified. |
| RG-007 — Privacy and cost | Verify LinguaDesk’s own text-retention/logging behavior, model quality/performance and supported settings, and a configured monetary ceiling. Provider retention/no-training certification is not a release gate. |
| RG-008 — Specification readiness | Resolve current-MVP pre-launch questions in Section 11. Deferred-feature dependencies are excluded from this release gate. Review proposed safeguards before making them binding. |

There is no agreed minimum pilot-user count or adoption gate. Actual launch evidence, model evaluation, and performance testing will be produced during subsequent work.

Under D-22, AI evaluates RG-001–008 from recorded automated evidence without a human release approval. Required live quality/performance evidence, privacy/accounting checks and actual finite monetary limits remain binding. Live runs may execute under standing authorization for declared profiles, environments and dispatch/spend bounds. Eligibility does not itself authorize deployment; an authorized target and rollout/rollback limits are required for unattended publication.

## 9. Dependencies and material risks

| Dependency or risk | Product implication / required response |
| --- | --- |
| Default model’s serving arrangement is unverified | Confirm access, model settings, quality/performance and pricing before launch. Provider retention/no-training guarantees are not eligibility conditions; the model name alone does not establish the remaining criteria. |
| Correlated AI evaluation errors | Separate candidate, reference-review, judge and adjudication roles; pin configurations, calibrate on disjoint development examples, preserve findings and disclose shared-model limitations. Automated review satisfies D-22; human review is optional. |
| Configured fallback models and credentials | Each candidate needs applicable quality/latency evaluation. Sharing a provider can leave correlated outages; assess this during model/provider selection. |
| Explicit resubmission and charged stale successes | A later submission charges the full text on success. Manual edits protect the displayed result but cannot reverse an already successful charge; communicate usage and unknown outcomes clearly. |
| Whole-text length limits | A user must shorten oversized input manually. Clear validation must not discard text or secretly submit a prefix. |
| Failed attempts | Character limits alone do not bound provider expenditure. Establish the monetary ceiling and evaluate the proposed abuse controls. |
| Deferred scope accidentally returning | Later-phase designs must use Section 3.1, not reinstate obsolete UX contracts from Git. No targeted sentence correspondence or dual mode controls should leak into MVP or later work by default. |
| Shared global allowance | One user affects availability for others. Architecture reserves capacity for integrity; no additional product fairness policy has been agreed. |
| Deferred email and provider availability | Real local-account email is excluded until DF-008 is reactivated. Verify current automatic-verification registration/sign-in and provider integration/failure behavior. Google is not a current prerequisite. |

## 10. Proposal dispositions and remaining proposals

Accepted and amended dispositions below are incorporated into the confirmed requirements. Proposed items remain recommendations and do not become binding through inclusion here.

| ID | Disposition | Decision and remaining downstream work |
| --- | --- | --- |
| P-001 | **Amended 2026-09-20** | One-second paused processing replaces explicit Translate/Rewrite buttons. DF-003 is activated; previous-result preservation and stale-response protection remain binding. |
| P-002 | **Amended 2026-09-20** | Manual result edits remain local, uncharged and protected from older responses. A later operation triggered by source/settings changes may replace them. Targeted sentence metadata preservation is cut; future assistance invalidates all metadata after any manual edit. |
| P-003 | **Amended 2026-09-20** | Empty, unsupported, uncertain, same-language and oversized input is blocked without successful-transformation charge. Correcting source/settings starts a new one-second pause; availability recovery alone never submits. Both features reject oversize input; prefix translation is DF-006. |
| P-004 | **Accepted; defaults amended 2026-09-08** | English UI and memory-only workspace settings/text remain. New rewriting mode is Correction only in one dropdown; separate toggle/None set/Show changes defaults are removed from MVP. Q-004/Q-009 retain workspace ownership. |
| P-005 | **Proposed** | Adopt the security/abuse safeguards in NFR-008, with separate short-term limits on attempts where necessary. These would be additional controls, not a reinstatement of the discarded daily call allowance. Review remains in the PRD, followed by security and architecture specification. |
| P-006 | **Proposed** | Provide API usage documentation/examples and preserve documented behavior within a published API version. Versioning mechanics and the exact compatibility policy remain deferred to PRD review and the API specification. |

## 11. Compact decision log and deferred questions

### Historical decision log and current amendment

D-01–D-16 are preserved in the [discovery archive](archive/product-discovery.md). Their alternatives/comparison, prefix translation, Google, targeted invalidation, and advanced routing clauses remain superseded or deferred by D-17 / Sections 3 and 5. D-21 supersedes D-17's explicit-button choice without reinstating unrelated historical UX. D-22 supersedes mandatory human-verification and browser-matrix clauses, while retaining quality/performance thresholds and integrity/privacy/cost requirements. D-18 additionally supersedes D-10’s provider no-training/retention clauses. D-19 confirms the later advanced-routing and shared-provider-credential direction without reactivating DF-004. D-20 defers real account email and selects automatic verification as the temporary MVP substitute. Unaffected product choices remain, including the default model preference subject to the remaining quality/performance/cost criteria.

| ID | Confirmed decision / superseded alternative |
| --- | --- |
| D-17 | **2026-09-08 user approval:** adopt all ten dispositions in Section 3.2. MVP uses explicit buttons, plain editable output, single dropdown, native/inline controls, whole-input limits, local accounts, two simple chains, and the independent API. DF-001–DF-007 are later-phase scope; targeted invalidation and the duplicate toggle are retired. |
| D-18 | **2026-09-08 user approval:** remove LLM-provider retention/no-training eligibility requirements from Q-001, NFR-004 and RG-007. Favor low-cost serving and allow provider caching subject to existing quality/performance/cost criteria. The user clarified that caching is **provider-managed**; no LinguaDesk completed-response cache is selected and per-tab text teardown is unchanged. #4 verifies provider cache capabilities/pricing; successful-character accounting remains unchanged. |
| D-19 | **2026-09-11 user direction:** a provider credential may be shared by evaluation and every LinguaDesk operation using that provider. Later routing is expected to support route-specific providers and more than one fallback; for example, a Chinese Translation route may prefer DeepSeek while an English route evaluates Muse Spark 1.3. This confirms later-phase intent under DF-004 without adding advanced routing to the current MVP. Reactivation must keep routing server-controlled and bound candidates, dispatches, deadlines and aggregate provider-account exposure conservatively. |
| D-20 | **2026-09-13 user direction:** defer real account-email delivery because SMTP configuration is not currently practical. Until DF-008 is selected, every newly created local account is marked verified without sending email, registration issues no credential and directs the user to explicit sign-in, and existing unverified-account enforcement remains. Email-address ownership is therefore not proven by the temporary MVP. |
| D-21 | **2026-09-20 user direction:** use one-second pause processing for Translation and Rewriting with no Translate/Rewrite button. Remove the New workspace control, usage summary, persistent character count/limit, dedicated source/result status lines and Copy result control from the active UI. Keep server-side counting/accounting, native result selection/copy, and essential blocking/failure feedback. This activates DF-003 and supersedes only the conflicting UI choices in D-17. |
| D-22 | **2026-10-25 user approval:** adopt unattended AI development and automated release acceptance. Remove mandatory human verification/sign-off and the browser/version/physical-device matrix; require pinned Chromium desktop/narrow automated evidence. AI may author/review evaluation references, grade/adjudicate outputs, review visual baselines and advance through authorized roadmap increments. Keep quality/performance thresholds, corpus/workload counts, integrity/privacy/authentication checks, finite budgets and honest evidence states. Permit standing bounded live-run authorization and evidence reuse after recorded impact review; human/additional-platform checks are optional and nonblocking. Completed packages retain historical evidence; deployment still needs an authorized target and rollout/rollback limits. |

### Open questions and their scope

See [Open product and design questions](product/open-questions.md#open-questions-and-their-scope).

## 12. References and review status

The v0.5 amendment links Q-003/Q-006 to their downstream technical design and the user-approved API authoring workflow. It changes no product scope, limits, quality/performance targets or proposal dispositions.

The primary source is the confirmed discovery conversation. External verification was limited to decisions that depended on current model information:

- [DeepSeek model documentation](https://api-docs.deepseek.com/quick_start/pricing/) — available model/mode information; no price or monthly budget is committed by this PRD.
- [DeepSeek thinking-mode documentation](https://api-docs.deepseek.com/guides/thinking_mode/) — non-thinking support and the need to disable the default thinking mode explicitly.
- [DeepSeek Open Platform terms](https://cdn.deepseek.com/policies/en-US/deepseek-open-platform-terms-of-service.html) — historical discovery reference; provider retention/no-training eligibility checks were removed by D-18. No provider privacy guarantee is asserted here.
- [User-supplied rewriting widget](https://chatgpt.com/s/w_6a9c7faf64148191abb437fe326d861c) — not accessible during discovery. The user’s written six-rule behavior description is the confirmed source, not an inferred widget implementation.

**Review outcome:** The product owner approved the ten scope dispositions in Section 3.2 on September 8, 2026, superseding affected discovery decisions and P-001–P-004 clauses. D-18 subsequently removes provider retention/no-training eligibility gates and records the clarified choice of provider-managed caching. D-19 records shared provider credentials and route-specific multi-candidate routing as later intent while leaving DF-004 deferred. D-20 defers real account email as DF-008 and selects automatic verification for newly created local accounts as the temporary MVP substitute. Current behavior is specified in Sections 3–8; DF-001–DF-008 preserve later intent without blocking MVP. D-22 adopts unattended AI development and automated release eligibility, with required Chromium desktop/narrow coverage and optional human/additional-platform observations. P-005/P-006 remain proposed. This baseline is ready for scoped planning, not evidence of implementation or release readiness.
