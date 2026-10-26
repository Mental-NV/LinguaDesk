# M046 — Tasks and evidence

Inputs: [spec](spec.md), [plan](plan.md), [source manifest](context.json).

## Resume

Closed on 2026-10-26 at the user’s direction. **M046: Done with accepted accuracy exceptions.** [Final case results](case-results.md) records every selected case: **92 passed / eight failed / zero skipped**. All approved plans ran; accuracy improvements are deferred, with no next task or paid run scheduled. The original AC-009 all-usable gate remains failed; related classification/alternatives exceptions remain visible. This is milestone closure with accepted findings, not release qualification.

Current product revisions: eligibility v5, Translation/Write v4, alternatives v15, full-text composition v6 and `alternatives-envelope-v2`. The v2 checker fixes the observed punctuation-only variation gap without changing decoded text/spans/charging. Four cross-script rejection cases and a native lexical-variation preservation case pass. Offline replay covers all 76 M046 set attempts plus 36 retained final M045 regression sets, zero new provider calls: four formerly accepted punctuation-only sets are rejected, all 36 prior M045 sets and all 33 currently usable M046 sets remain accepted. Raw live reports remain immutable under their original revisions; checker replay and semantic reviews are separate artifacts.

Final local checks: **659 backend (341 AI), 238 frontend, 110 published Chromium**, independent bearer/API consumer, contract drift, socket Smoke and full/reduced offline matrices (`artifacts/m046/guard-local-gate-01.log`; affected AI log `wording-guard-ai-check-01.log`). Source manifest has 96 reviewed selections. Current review/index/provenance artifacts: `final-development-review.json`, `focused-semantic-review.v1.json`, `wording-guard-replay-regression-01.json`, `live-report-index.json`, and `reduced-implementation-manifest.json`, all under `artifacts/m046/`.

Paid totals: **406 requests**, known USD 0.1488051 plus retained unresolved USD 0.006534 = **USD 0.1553391 conservative** for M046. Historical plus M046 exposure is USD 0.4270199. Initial scope used 191/192 requests, follow-up 184/184, focused 31/32, all within their separately approved caps. All planned cases ran; unused margins are not an instruction to retry until a case passes. No further paid run is scheduled. Any next paid investigation needs its own finite allocation and impact review. Frozen scope remains 100 cases/33 languages/21 scripts, excluding Latin/Cyrillic/Chinese and Han-overlapping Japanese. Manifest SHA-256 `3af8e839293f913358818da7cec9e14868f9a3f5126aef49dc4f0c702d435879`. No deployment is authorized.

## Ordered tasks

- [x] T001 — Freeze canonical language/script metadata, multilingual scalar/sentence fixtures and reviewed versioned manifests; retain the broad matrix offline and validate the user-selected 100-case reduced live manifest. AC-001/002/005/009 prerequisite; no dependency; done when manifests cover the selected scope and reviewed lock is current without live dispatch.
- [x] T002 — Extend Core catalog, generate distinct pairs, update actual C# capability/operation metadata and exhaustive mappings; generate/review schema and clients before adoption. AC-001/008; depends T001; done when catalog parity, old-code compatibility and contract drift checks pass.
- [x] T003 — Revise shared prompts, Core-backed eligibility, profile snapshots and maximum-input/cost proofs; extend deterministic Smoke fixtures. AC-002/006; depends T001/T002; done when offline/transport tests cover every code and bounds without new dispatch stages.
- [x] T004 — Implement language-aware v2 sentence boundaries and option validation across C#/TypeScript, capability revision and safe stale-client handling. AC-005; depends T001/T002; done when shared spans/Greek semantics/Unicode boundaries and revision mismatch checks pass.
- [x] T005 — Consume catalog/generated types, sort native controls, update unsupported copy and implement scoped RTL/direction/font behavior. AC-003/004; depends T002/T004; done when unit/component checks preserve pause, edits, copy and long-label behavior.
- [x] T006 — Add real-boundary published and bearer/API regression paths, review representative script screenshots and rerun normal visual comparisons. AC-001–008; depends T003–T005; done when visible outcomes, native clipboard/editing, privacy/accounting and old-language regressions pass.
- [x] T007 — Add allowlisted offline `--suite m046` and offline/live `--suite m046-reduced`, validate prerequisites, execute/review required development matrix within aggregate bounds and retain targeted failure investigations. AC-009; depends T001/T003/T004 plus live prerequisite; execution/review complete with eight accepted accuracy exceptions under the spec closure amendment; original all-usable criterion remains failed, with costs/limitations recorded.
- [x] T008 — Verify isolated rollback/status compatibility and aggregate regressions; update operational instructions and close delivery/coverage/backlog with actual evidence and the user-accepted accuracy exceptions. All ACs; depends T002–T007; closed under the spec closure amendment after local checks and live/visual/diff review, without relabeling failed quality results.

## Planning record

2026-10-26 (Europe/Moscow), baseline `554493f63625b1ea10736f61206ae2aab2f7d60f`: read planning workflow/template, selected product/UX/API/AI/verification owners and completed dependency status; inspected actual catalog, capability enum mappings, prompt/parser/bounds, Unicode sentence/comparison helpers, capability-driven selectors, runner/test commands and M045 live-authorization provenance. Consulted primary public language/direction/segmentation/provider references; findings and limitations are in the linked investigation. No credentials were read, paid calls made or application files changed.

Semantic review: all nine ACs map to tasks/checks; selected code/script rules are explicit; three-letter IDs and stale segmentation clients are addressed; both UI workflows have real-boundary browser proof; accounting and generated contracts preserve old meanings. Translation uses 264 development cases rather than all 15,252 pairs, and evidence claims distinguish unmeasured pairs. Required live evidence and release qualification are separate. Readiness covers local implementation; live completion is conditional on actual prerequisites. Lock/check establish freshness only, not implemented behavior.

Planning validation passed on 2026-10-26 (Europe/Moscow): `python3 automation/context.py lock M046` and `check M046` validated 64 reviewed source selections; `python3 automation/context.py audit` checked links/anchors/package layout across 237 Markdown documents; `git diff --check` passed. A local consistency check verified 124 unique language IDs, four existing/120 additions, 15,252 generated pair count, 24 representative directions, 264 Translation development cases and 1,116 cells each for Write/alternatives, and confirmed every manifest path exists. No application test suite was run for this documentation-only change.

## Completion record

The following dated entries are historical snapshots, including then-pending approvals and blockers. Resume and the final user-directed closure below own current status.

Local implementation verified on 2026-10-26 (Europe/Moscow), execution environment Darwin 27.0.0 arm64, .NET SDK 10.0.302, Node 24.20.0, npm 11.11.0, Playwright 1.63.0/pinned Chromium. Base revision `a5684268e70880a5a2cbae5facc2a8095cc23902`; implementation remains in the working tree. Required live model quality is **Pending**, not waived. No new database migration, user-text persistence, language-specific routing, English pivot, production deployment or release/performance qualification was introduced.

### Reviewed implementation and design impact

Core and frozen `languages.v1.json` agree on all 124 canonical IDs/names/direction/script/variety rules. API enums preserve the original four ordinals/serialized IDs and append additions; capabilities expose 15,252 distinct pairs and finite direction metadata. All three original-success response forms now use the same finite language enum. Generated OpenAPI and TypeScript were produced from C# and reviewed before final frontend adoption: additive capability metadata and language values, unchanged request shapes/limits/modes/defaults, original wire IDs and text-free recovery/status. The independent JavaScript bearer consumer proves new LTR/RTL/three-letter choices, charging/replay/conflict and invalid-alias rejection.

New immutable resources are eligibility/translation/rewriting v2 and alternatives v10; prior resources/corpus files remain untouched. Eligibility uses the complete compact Core catalog; transformations use only validated selected descriptors. The trusted composed instruction plus catalog digest is hashed without user text. The alternatives scaffold stays within 4 KiB for every language and maximum escaped 8,000-scalar context/2,000-scalar selection fits its existing 131,072-input-token envelope. No new stage/repair or provider family was added.

The maximum-Unicode audit found that the old full-text average estimator cannot prove all legal escaped inputs fit its 8,192-token base profile. The owning AI design and plan were amended/reviewed before use: a per-input full-text view bounds complete decoded prompt UTF-8 bytes plus 512 framing tokens, capped at 65,536; automatic source uses the longest descriptor including its code. Registry/output/response limits and per-operation/monthly ceilings remain unchanged. Chain and monthly admission consume that same view. Maximum 5,000-scalar Translation and 2,000-scalar Write fixtures cover astral, control, CJK, quote and backslash escaping; two worst-case primary reservations remain within the existing USD 0.05 default ceiling. Boundary denial tests now use the actual reviewed per-input bound.

Sentence v2 shares 35 frozen fixtures across C#/TypeScript: all v1 regressions plus new punctuation, Greek-only semicolons, RTL/mixed content, Indic combining/joiner text, CRLF, no-space fragments, non-BMP letter initials and conservative mark boundaries. API span validation and generated-option parsing use the resolved language. Unknown/v1/future capability revisions block alternatives requests and display a refresh notice while leaving editing/copy actions enabled; no in-flight span is resegmented.

Both native selectors use one capability mapping, stable English-name order and constrained script labels. Generated content direction/lang is bound only to current validated results; editable content uses native direction behavior and logical alignment, while navigation/controls stay LTR. [Visual review](visual-review.md) records native-size inspection of all four new captures and the glyph sheet, pinned font identities and zero missing sample cmap entries. Existing M045 baselines were unchanged. No font fetch occurs on product user-text paths.

### Commands and durable evidence

- `python3 automation/context.py check M046`, `packet M046`, reviewed `lock M046`, and `audit`: source freshness and package/link review; final lock has 79 selected inputs, audit covers 239 Markdown documents.
- `bash scripts/backend.sh check`: **651/651 passed**, including API/storage 272, Core 12, independent AI 333 and benchmark harness 34. Reports are in `artifacts/test-results/backend-*.trx`.
- `bash scripts/ai.sh check`: **333/333 passed**, deterministic inspect/probe/conformance plus legacy alternatives regression and all M046 offline cells. Frozen manifest SHA-256 `550777c025e5577b45fdc742dc04edeb4b35b063f44bfb12ee785a1214574142`; full matrix reports are in `artifacts/m046/offline-check.wQh10b/`: Translation 264, Write 1,116, alternatives 1,116, eligibility 12; zero failures and zero provider dispatches. These are synthetic shape checks only.
- `bash scripts/contract.sh generate` and `check`: reviewed actual C# → OpenAPI → TypeScript; final drift check passed. Success vocabularies, directions, capability metadata, exact request limits, defaults, errors, status/replay and existing wire IDs were checked. No generated file was hand-edited.
- `bash scripts/frontend.sh check`: **238/238 passed**, typecheck/lint/build and 28 shared scalar-policy cases included.
- `LINGUADESK_UPDATE_VISUALS=1 bash scripts/frontend.sh smoke`, followed by native-size AI inspection and ordinary `bash scripts/frontend.sh smoke`: **110/110 published Chromium journeys passed**, normal comparison of all baselines passed, independent bearer consumer passed. Real login/auth, migrated SQLite, API and controlled external adapters were used. New paths cover Arabic↔English swap, Hebrew alternatives/deletions/reuse/edit/copy, Hindi combining, Japanese/Korean, `fil`, close-language override, RTL failure preservation and 320 px reflow.
- Pinned font byte/hash and cmap inspection with FontTools 4.60.1: `artifacts/m046/font-coverage.json`, zero missing selected sample code points. License/source/hash records are under `frontend/tests/e2e/fonts/`.
- `bash scripts/verify-milestone.sh M046`: **all local regression gates passed**, including backend socket smoke, final contract drift, offline AI matrices, frontend unit/build, published browser comparisons and bearer consumer. Complete log: `artifacts/m046/local-gate-01.log`. `git diff --check` passed.

Early failures were retained in sanitized `artifacts/m046/m046-*.log`: sandbox socket/compiler-server restrictions resolved by authorized local execution outside the sandbox; two old unsupported-German assertions became noncanonical-alias negatives; new test fixture currency and input-view cost assertions were corrected; the RTL test's attempt to select a disabled same-language target was reordered. No guard was weakened to make a test pass. Build/restore wrappers now use one MSBuild node and disable build servers to avoid sandbox worker stalls. Production credentials were never printed. Source diff review confirmed no persistence migration, text-bearing status/replay, source logging or identity-fingerprint changes.

### Acceptance disposition

| AC | Current evidence/disposition |
| --- | --- |
| AC-001 | Local pass: frozen catalog/Core/enum/capability/generated-client parity, 15,252 unique non-self pairs, canonical/alias/null/duplicate validation and old IDs. |
| AC-002 | Deterministic/controlled-provider pass; actual live ambiguity/script/language classification remains pending with AC-009. |
| AC-003 | Local pass: sorted native selectors, one pause, no duplicate operation, current-revision swap/reuse and long-label reflow. |
| AC-004 | Local pass for selected representative scripts: scoped direction, exact editing/selection/copy, pinned glyph coverage and reviewed captures. Optional physical IME/additional-engine evidence is untested. |
| AC-005 | Local pass: 35 shared scalar-span fixtures, Greek semantics, resolved-language option parser and safe revision mismatch. |
| AC-006 | Local pass: Core-derived trusted metadata, strict finite envelopes, maximum legal escaped inputs/scaffold/context/cost bounds, unchanged chain/deadline/output/spend policies. Model completeness/usability remains a live gate. |
| AC-007 | Controlled-provider published pass with real seeded account/login/auth/API/SQLite; model semantics are not inferred from these journeys. |
| AC-008 | Local pass: real bearer, scalar charging once, zero-charge invalid requests, replay/conflict/stale/recovery/privacy and generated/independent consumer regressions. No new migration. |
| AC-009 | **Blocked before paid dispatch by automatic approval review.** The user-selected reduced 100-case gate is frozen and passes offline; required live outputs/classifications and semantic reviews do not exist yet. See [live review](live-review.md). |

T007 and final T008 closeout stay unchecked. All unsampled Translation pairs remain unmeasured, and all required live development, release qualification and performance results remain explicitly unclaimed.


### Reduced-scope verification amendment — 2026-10-26

The subsequent user instruction selected exactly 100 live cases, excluding Latin/Cyrillic/Chinese. The owning verification §5.5, product language policy, AC-009, plan, frozen corpus and CLI were amended before any candidate generation. The sample includes 33 supported languages/21 supported writing systems plus an explicitly unsupported Thaana classification case; Japanese is omitted to avoid Han overlap. The earlier full matrix is retained unchanged for offline checks. Both source and target languages of all 16 live Translation directions satisfy the exclusions; payload inspection also rejects Latin/Cyrillic/Han letter ranges.

New manifest `m046-reduced-development-v1`, SHA-256 `3af8e839293f913358818da7cec9e14868f9a3f5126aef49dc4f0c702d435879`, is pre-reviewed with synthetic meaning/classification constraints: 16 Translation, 40 Write, 36 alternatives sets, 8 eligibility. All 33 selected languages occur in both Write operations, every mode occurs in both, and correction/minimal edits, already-correct text, numbers/negation/conditions/uncertainty, instruction-as-content, repeated/fragment/context selections, Greek question punctuation and scalar offsets are represented. No new runtime language restriction or release-qualification waiver is introduced.

Eligibility v3 generalizes the old ambiguity instruction to all short shared greetings/names/cognates and says script alone is insufficient; the full-text view revision is v3. Bounds, output limits, operation/monthly ceilings, stages and provider chains remain unchanged. Historical prompt versions remain in source. Broad and reduced offline reports are labeled synthetic shape-only.

`bash scripts/verify-milestone.sh M046` passed after these changes: **653/653 backend**, including **335 AI**, **238/238 frontend**, **110/110 published Chromium**, independent bearer consumer, contract drift and 81-source context/link/diff checks. Complete current log: `artifacts/m046/reduced-local-gate-01.log`; reports under `artifacts/test-results/` and the offline directory named by that log. No visual baseline changed.

The first reduced live command was rejected before process start by automatic approval review, which requires explicit permission naming `https://api.deepseek.com` and the synthetic cases plus repository-derived prompt/catalog instructions. The asynchronous question asks for exactly that, with the existing credential, at most 192 initial requests/USD 0.50. No paid call, spend, new unresolved exposure, candidate response or live semantic pass exists. T007/T008 and milestone Done remain pending that approval and actual reviewed results. No alternative egress was attempted.


### Current reduced execution and correction — 2026-10-26

The explicit user approval resolved the initial DeepSeek destination/payload blocker. All 100 initial frozen cases were executed and semantically reviewed, including both alternatives: Translation 9/16, Write 26/40, alternatives 24/36, eligibility 8/8 usable. Overall 67/100 is a retained failed initial development result, not a milestone pass. Total 191 requests/USD 0.0703665, zero unresolved; historical plus current conservative exposure USD 0.3420473.

Native-Unicode JSON composition and distinct-alternative prompt corrections are implemented/tested. The final aggregate local gate has 654 backend tests (336 AI), 238 frontend and 110 published browser tests; final v12 prompt-only AI/offline rerun has 336 tests. New native-data regression covers Ethiopic, Georgian, Khmer, Devanagari, Arabic and Greek with exact source round trips and untrusted-data boundaries. Full/reduced synthetic matrices, maximum legal input/scaffold/cost bounds, contract drift, independent bearer and source/link/diff checks pass. Tests never substitute for the pending corrected live rows. Historical visual captures and existing baselines are unchanged.

Current AC-001/003/004/005/007/008 retain their proven local/published dispositions. AC-002 and AC-006 retain deterministic boundary proofs plus the eight correct live eligibility classes, with affected live transformations still pending. AC-009 is **not passed**: initial failures remain unresolved until corrected outputs are reviewed. T007/T008 remain unchecked. The concrete 184-request/USD 0.40 follow-up was rejected before process start by automatic approval review; the reason is an additional paid request scope beyond the explicit initial 192-request approval. The exact follow-up approval question is pending. [Live review](live-review.md) retains all 100 initial row dispositions, hashes, revisions, finite follow-up allocation and limitations.


### Final local readiness — focused approval pending

The user-approved 184-request follow-up is complete. Latest unique dispositions: Translation 15/16, Write 33/40, alternatives 32/36, eligibility 8/8 under the prior v4 (v5 safety checks pending). Twelve failed unique cases still require corrected outputs; no critical case is silently dropped. The exact 20-ID/32-request/USD 0.10 focused plan is frozen in `artifacts/m046/focused-plan.json`; it contains those 12 plus the same eight eligibility references and has dispatched zero requests.

Final shared prompt revisions are eligibility v5, Translation v4, Write v4, alternatives v15, full-text composition v6. Final aggregate gate passed with 654 backend, 238 frontend, 110 published Chromium, independent bearer and contract drift (`final-local-gate-01.log`); metadata-only evaluation report refinement was verified by a fresh 336-test AI/offline run (`final-report-ai-check-01.log`). Source check has 93 reviewed selections, Markdown audit covers 239 files, diff check passes. No visual baseline, contract, migration, source/reference, stage count, deadline or monetary ceiling changed during the focused prompt refinements. T007/T008 and Done remain pending the explicit 32-request approval and actual usable reviewed results.


### Current disposition after every approved plan — 2026-10-26

All 100 unique cases and bounded investigations are executed and reviewed, including both options in every alternatives set. The focused approval was received and its six planned batches ran, 31/32 requests, USD 0.0120567 known and zero new unresolved. It fixes the Pashto Write classification, Thai uncertainty, Telugu alternatives wording and Hindi→Bengali time specificity, and all eight eligibility references match v5. Eight other findings persist under the actual selected profile despite prompt refinements: Assamese false mismatch; Thai/Urdu owner changes; Arabic singularization; Hindi unchanged agreement; Amharic/Hebrew/Pashto unusable alternative sets. The full frozen corpus, original responses, per-run denominators, failed prompt revisions and timeout reservation remain retained. A same-agent reference-review correction (initial Hindi plurality) is explicit and the earlier review is preserved.

The exposed punctuation-only acceptance gap is now fixed by deterministic checker v2. Its five new regression cases bring AI tests to 341 and backend to 659. All 112 recorded-set offline replays, including the prior four-language regression, preserve valid sets and reject only the four additional punctuation-only failures; replay adds no live case/call. The aggregate product check passes with 238 frontend and 110 published Chromium cases, independent bearer consumer, exact generated-contract drift and current context/link/diff checks. The checker does not claim to detect every semantic/model error.

| AC | Current evidence/disposition |
| --- | --- |
| AC-001 | Local pass: canonical 124-code catalog/API/generated/UI parity, 15,252 distinct pairs, preserved old meanings and finite metadata. |
| AC-002 | Local boundaries and eight v5 live safety classes pass; Assamese false mismatch remains an unresolved live finding. |
| AC-003 | Published native controls/order/long-label/pause/swap/reuse checks pass. |
| AC-004 | Reviewed local RTL/Indic/Ethiopic/CJK/Southeast Asian captures and glyph evidence pass; optional physical IME/additional-engine checks remain unclaimed. |
| AC-005 | Local shared segmentation/scalar spans/revision safety and v2 distinctness guard pass; three model alternatives sets remain unusable. |
| AC-006 | Local catalog/trusted-data, maximum-Unicode/scaffold/context/cost, stage/deadline and strict-envelope proofs pass; complete semantic usability is not inferred from those checks. |
| AC-007 | Published real login/auth/API/migrated SQLite/controlled-provider journeys pass. |
| AC-008 | Bearer/accounting/scalar/replay/conflict/stale/recovery/privacy and independent consumer checks pass; no migration. |
| AC-009 | **Failed: 92/100 usable, eight unresolved findings.** All required cases ran; no pending approval/credential/provider-access prerequisite. |

M046 remains In progress. T007 requires a scoped shared-prompt/global-candidate strategy to resolve the eight recorded findings and verify affected strata under a new finite paid allocation; T008 final Done depends on that actual evidence. No acceptance weakening, language deletion, language-specific routing, extra production repair stage, deployment or release/performance qualification was introduced.

### User-directed closure — 2026-10-26

The user accepted stopping accuracy work and explicitly instructed closure. M046 and BI-001 are Done with accepted accuracy exceptions. T007 is checked for completed execution/review; its original all-usable AC-009 target remains failed. T008 is checked for final evidence/status closeout under the explicit spec amendment. No failure was deleted, regraded or marked skipped. Related AC-002/005 findings and the eight AC-009 failures are deferred to future selected accuracy work.

[Final case results](case-results.md) lists all 100 IDs and selected outputs/classifications, eight detailed failures, and skipped/unmeasured scopes. The selected scope has 92 passed, eight failed and zero skipped. The preceding In-progress/blocked snapshots are retained as history, superseded only for closure status. Existing raw reports/reviews, hashes and prompt resources remain unchanged. Delivery, backlog, verification, product and README summaries reflect this disposition. No new paid request, deployment, successor milestone or application change was made during closeout.

Documentation-only closeout validation is recorded below after the source lock, link audit, result reconciliation and diff checks. Prior runtime evidence remains 659 backend / 238 frontend / 110 published Chromium, with 112 offline retained-set replays.

Closeout verification at 2026-10-26 10:41 UTC, Darwin 27.0.0 arm64: reconciled all 100 document rows against the frozen corpus/final review and verified each selected source-report SHA-256; 92 passed/eight failed/zero skipped. `python3 automation/context.py lock M046` and `check M046` pass with 97 reviewed sources; `audit` passes across 240 Markdown documents; `git diff --check` passes. The final documentation-only edits require no runtime rerun; existing product verification above is retained. Closure provenance is `artifacts/m046/user-directed-closure.json`; historical JSON snapshots are unchanged.
