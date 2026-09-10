# LLM quality and performance verification

Authoritative continuation of [06-verification-plan.md](../06-verification-plan.md); section numbers refer to that document; read only when relevant to the selected scope.

## 5. LLM quality and candidate qualification (Q-005)

### 5.0 Evidence layers and live-development gate

Use the cheapest layer that can prove the claim, but do not use a cheaper layer to claim behavior it cannot observe:

| Layer | Required use | What it proves / does not prove |
| --- | --- | --- |
| Offline unit and pipeline fixtures | Default local/CI suite and every behavior change | Deterministic parsing, validation, prompts, traversal, admission, timeout and error permutations with zero provider calls; cannot prove provider access or model behavior |
| Sanitized transport contract tests | Every adapter/dialect/settings change | Exact URI/headers/body, explicit output/thinking controls, response/error/usage mapping and one-dispatch behavior through a fake HTTP transport; never includes a real key and cannot prove the provider honors the request |
| Live credential/access check | Before live behavior work for each candidate/profile/credential reference | At most one fixed-synthetic, low-output, budget-admitted request proves current authentication plus endpoint/model/required-settings access; does not qualify quality, cost, limits or reliability |
| Live development behavior batch | Required evidence to complete M015–M018 and for affected prompt/checker changes | The real model executes the production prompt/pipeline on versioned non-production development cases; small coverage exposes integration and behavior risk but is not release qualification |
| Frozen-corpus qualification and live API workload | G1/G2 through M035–M037 and successors | Full Section 5 quality/human evidence and Section 6 serving performance; cannot be replaced by earlier smoke results |

M019 first supplies the profile-selected adapter and credential/access workflow. M015's live development slice covers each supported language plus each model-decided negative class relevant to both families; deterministic local-invalid cases continue to prove zero calls. M016 runs at least one reviewed development case in each of the 12 Translation directions. M017 runs at least one reviewed development case in every language/mode cell. M018 keeps exhaustive failure/fallback/deadline permutations scripted, while a small live no-fallback path confirms the same bounds/metadata around natural provider calls; do not provoke provider abuse, deliberately spend against an invalid credential, or claim that unobserved live failures were tested. M020 emits the combined report. These are minimum smoke strata, not corpus-size reductions or product pass thresholds.

Live checks are explicit, non-default and excluded from the ordinary offline test target because they are paid, variable and depend on external service state. Nevertheless their recorded success is a milestone completion requirement, not optional evidence. A missing credential, denied budget or unavailable provider blocks only the live gate and is reported as such; it must not skip cases, substitute a mock, or turn the offline suite red when no live run was requested. Routine pull requests run the offline and transport layers; rerun the affected live layer when a prompt, model/profile, adapter, provider dialect, output checker or effective setting changes and before accepting its milestone/evidence.

The runner resolves `CredentialRef` as specified by [#4 Section 5.2](../04-llm-specification.md#52-evaluation-profiles-and-credentials). Each live command requires an explicit candidate profile, maximum dispatches and maximum spend; the credential check hard-limits dispatches to one. A profile may target DeepSeek, OpenAI or another provider implementing the required OpenAI-compatible Chat Completions subset. Test compatibility per profile: shared request shape is not evidence that provider-specific controls, finish reasons, usage or errors are portable.

The DeepSeek credential is intentionally shared with all LinguaDesk operations routed to that provider. Therefore live evaluation uses the same global monetary admission as concurrent serving when it exists, or records that serving was quiescent for the run. Apply public peak, cache-miss and configured maximum-output assumptions before dispatch; retain full unresolved exposure when usage is missing. A runner-local maximum remains a useful per-run stop but is not evidence that the shared provider account or unrelated use stayed below the product ceiling.

Reports label every observation `offline_fixture`, `transport_fixture`, `live_access`, `live_development`, `live_qualification` or `fault_injected`. They record candidate/profile/adapter/model/endpoint identity, credential reference, shared-scope/quiescence status, bundle/settings/checker revisions, time, dispatch count, usage, admitted/settled/unresolved exposure and sanitized outcome. They never contain the API key, authorization header, environment dump, key fingerprint, raw provider body or unrestricted prompt/output. Development case IDs may link to separately controlled source/review material; production text is never captured.

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

If DF-004 is later selected, qualify each candidate on every directed route or rewriting cell it may serve rather than assuming provider-wide quality. Route-specific evidence may narrow an assignment, but no strong route may average away a weak assigned route. Each longer configured chain also needs bounded traversal, correlated-credential failure, deadline and conservative aggregate-cost evidence. Muse Spark 1.3 is currently only an example candidate; publisher agent/coding claims are not LinguaDesk language-quality evidence.

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
