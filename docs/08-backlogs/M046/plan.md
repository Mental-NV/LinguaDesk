# M046 — Implementation plan

Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json). Feasibility and source references: [investigation](../../research/language-expansion-2026-10-26.md).

## Execution brief

Extend the existing shared pipeline to the [124-language catalog](../../product/languages.md). Keep one Translation chain and one Write chain, direct transformation, all nine modes and the existing authentication, pause, scalar accounting, privacy and recovery behavior. The existing capability-driven native selectors are reusable. Shared RTL presentation and sentence punctuation are the substantive UI/policy additions.

Translation evaluation is deliberately **not Cartesian**: 24 deeply representative directions plus two lightweight English-anchor smokes per added language. All-pairs completeness is a deterministic catalog assertion. English is only an evaluation anchor. Write and alternatives keep language/mode coverage. The implementation request selects T001–T008. Deployment remains excluded; paid work requires actual continuing authorization and execution approval.

## Changes and order

### T001 — Freeze catalog and evidence inputs

Review `docs/product/languages.md` against current code and lock exact code/name/direction/script policies, including three-letter IDs, `fil` rather than a duplicate Tagalog choice, Arabic standard variety, Serbian script preservation, and script-limited Mongolian/Punjabi/Kurdish choices. Add versioned shared language/sentence fixtures under `contracts/fixtures/`, preserving current fixtures for regression. Inventory code/tests/scripts/evaluator/sample for hardcoded four-language arrays, 12-direction counts and unsupported examples that become supported (for example Spanish must no longer be a negative merely because it is Spanish).

Create versioned development manifests and reviewed expectations before paid work. Freeze the 24 R directions specified by verification §5.5, 240 added-language anchor cases, 1,116 Write cells and 1,116 alternatives cells; do not create 15,252 live cases. Existing Chinese-script, fidelity, instruction-as-content and mode cases stay represented; add close-language/unsupported-script negatives and Serbian dual-script cases. Classification stages can satisfy positive eligibility evidence. Record reference/reviewer identities and ambiguity dispositions. A manifest validation check must prove catalog/cell coverage without dispatching. Do not edit historical M015–M045 evidence/corpus files in place.

### T002 — Core and real generated contract

Extend `backend/src/LinguaDesk.Core/ProductCatalog.cs` descriptors with direction and selected script/variety policy; build `TranslationDirections` deterministically from distinct IDs. Keep old code semantics and existing mode/limit constants. Extend `Features/Capabilities/CapabilitiesContract.cs`, `CapabilitiesDocument.cs` and operation/success metadata in `Features/Operations/` to cover the same values and content-direction metadata. Use an exhaustive table/test to prevent drift among enum serialized values, mappings and Core. Do not make a separate language database or accept arbitrary BCP-47 strings.

Run `bash scripts/contract.sh generate`, inspect the actual C# → OpenAPI → TypeScript diff, then adopt generated types in `frontend/src/api/translation.ts`, `rewriting.ts` and assistance models. Remove the handwritten four-language union. Preserve supported-directions array shape; test 15,252 entries, serialized payload size and client parsing without quadratic runtime rendering loops. Do not hand-edit YAML/types or widen API request/body limits merely because the capability response is larger. Existing modes, defaults and full-text fingerprints remain stable. Contract scaffolding alone does not satisfy AC-001/008.

### T003 — Prompts, eligibility and finite bounds

Replace `EligibilityEnvelopeParser.IsSupportedLanguage` with Core-backed membership. Create new immutable resources/revisions for eligibility, translation, rewriting and alternatives; update `PromptResource`, prompt factories, embedded-resource selection and profile/bundle validation together. Preserve old resources for reproducibility. Eligibility renders the deterministic allowed-code/name block; transformations render **only validated selected language descriptors and required script rules**, avoiding a full catalog in the 4 KiB alternatives scaffold. Hash the composed trusted instruction/catalog snapshot as part of the bundle, not user text. Strict JSON/envelope rules remain.

Exercise the production pipeline with every code, wrong/unknown values, ambiguous closely related languages, Chinese/Serbian script policy, instruction-like content and terminal rejection. Avoid adding a script-only language detector or another paid validation call. Recheck worst-case UTF-8/JSON escaping, static scaffold, request context and configured token/output bounds for all stages. If the selected composition exceeds a finite bound, first reduce redundant metadata; any necessary bound change needs owner/profile/admission proof and a reviewed lock before use. Never relax the per-operation spend cap, source limit or output completeness to pass a language case. Adapt Smoke providers to explicit synthetic language fixtures, not a heuristic that pretends to know arbitrary text. The maximum-Unicode audit requires a full-text per-input profile view capped at 65,536 input tokens (complete decoded prompt UTF-8 bytes plus 512 framing tokens), with unchanged registry/output/response limits and operation spend caps; update the owning AI design and prove chain/monthly admission against that view before use.

### T004 — Shared sentence policy and Write behavior

Implement `sentence-boundaries-v2` in `Core/SentenceBoundaryPolicy.cs` and `frontend/src/rewrite/sentences.ts` using the frozen terminal/closer tables from UX §8.5. Pass the resolved language into segmentation and `AlternativesEnvelopeParser` so Greek `;` is not interpreted as a terminal in every language. Keep scalar spans, exact gaps/newlines, complete-fragment fallback and existing bounds. Share fixtures for every added punctuation family, decimal/abbreviation/initial exceptions, adjacent quotes, repeated occurrences, no-space content, bidi controls and complex graphemes. Add specific regression fixtures for all v1 behavior intended to remain.

Expose v2 in capabilities and check it before alternatives. A stale client disables alternatives with a refresh notice while preserving current work; it never silently resegments an already pending request. Deploy server and generated SPA together. Existing text-free operation status/replay must remain readable; no segmentation revision is retroactively added to old fingerprints. Update helper calls and `AlternativesInputPolicy` consistently. Keep `comparison.ts`'s grapheme-safe bounded fallback; no dictionary-based word segmentation or rich-text caret reconstruction is needed.

### T005 — Catalog selectors, RTL and fonts

Update capability mapping and `TranslatePage.tsx`/`RewritePage.tsx` for stable English-name ordering and complete labels, while retaining native selects/typeahead and current defaults. Replace the four-language error with UX-MSG-008's catalog-neutral copy. Keep disabled known-source targets, current detected labels, swap/reuse and exact one-second revision behavior. Loading new capabilities alone must not submit paid work.

In `shell/WorkspaceView.tsx`, Write result/alternative presentation and stylesheet rules, apply `dir="auto"` to editable content and explicit resolved direction to generated text, appropriate paragraph isolation, `lang` when current, and logical text alignment. Keep UI/navigation/control order LTR. Isolate mixed-direction numbers/URLs/labels and deletion annotations without changing copied strings. Verify fallback font glyph coverage; add only needed licensed, pinned test font subsets with provenance under `frontend/tests/e2e/fonts/`. Runtime may use suitable local fallbacks; tests must not accidentally depend on developer-machine fonts. Do not fetch remote fonts on user-text paths.

### T006 — Integrated behavior and visual evidence

Add focused unit/HTTP parity tests plus published Playwright journeys using the existing seeded verified account, real auth/API and migrated SQLite with deterministic external providers. At least one case signs in through the real login form. Cover native selection/copy/manual edits, Arabic↔English swap, Hebrew/RTL Write alternatives/deletions, Hindi combining text, Japanese/Korean/no-space content, a three-letter language and ambiguous-source manual override. Every visible feature needs browser assertions; route/cell completeness belongs to cheaper layers, not one browser test per pair.

Include the four captures allocated in verification frontend §4.5 (RTL Translator and RTL Write, each at 1440×900 and 390×844), plus 320 px reflow and long-selector labels. Inspect newly added/changed baselines at native size and record script/font identities and review findings in `visual-review.md`; a screenshot hash is not visual review. Existing M045 snapshots remain regressions except where intentional language labels/order affect them. Add real bearer/sample cases for a new LTR, RTL and three-letter code, invalid aliases, same-language rejection, alternative scalar offsets, replay/conflict and usage. Check prior-code fingerprints and recovery readers without introducing a database migration. Preserve text-free persistence/log/status guarantees.

### T007 — Explicit bounded live development

Extend the existing `EvaluateEligibility`, `EvaluateTranslation`, `EvaluateRewriting` and alternatives runner paths plus `scripts/ai.sh` to accept a committed allowlisted **`--suite m046`** selector. This flag is a planned implementation deliverable, not an existing command. Keep default offline behavior and historical suites reproducible. Suite selection must not allow arbitrary user text/config/credentials. Preflight validates frozen cases and cost/dispatch requirements; report missing coverage before network activity.

Inspect continuing authorization in M045's evidence and current session/configuration. Record allowed profile/model/credential reference, finite aggregate remaining spend/dispatch limits (candidate and review calls), ledger/quiescent scope, and per-batch caps. Preserve an existing valid authorization without asking again. Do not infer current unused funds from a historical report or read/display key values. Use at most one compatible access check when current evidence cannot be reused. Required checks may run unattended within those bounds; missing prerequisites block this task only.

Run/review the 264 Translation cases, then all Write and alternatives cells; record reusable eligibility observations and a bounded negative slice. Retain failed cases and classify reference versus prompt/model defects. Use verification §5.5's at-most-five targeted cases per affected Translation direction per investigation batch. No all-pairs live expansion or blanket full rerun is triggered by a language-specific issue. Shared prompt/model changes require impact review and rerunning affected representative/smoke strata. No candidate failure is hidden by fallback success. Every required development output must be usable and free of unresolved critical findings; compare both alternative options. Record exact catalog/bundle/profile/corpus/checker hashes and sanitized outcomes, costs and unresolved exposure. These are development results, not release qualification or latency evidence.

### T008 — Regressions, operational notes and closeout

Run the aggregate milestone gate, independent consumer and final contract drift check; record AC mapping, commands, environment/revision, live/fixture distinctions, visual review and limitations. Review complete diff for hidden text storage, hardcoded four-language assumptions and unbounded configuration. Update README/runtime instructions, delivery/coverage and backlog/task state only after every selected AC passes. Full release quality/performance remains governed by §5.5 and successor gates. No historical package lock is refreshed merely because owning design changed.

## Verification map

Existing commands are runnable today; `--suite m046`, new fixtures/tests and manifests are deliverables. Evidence targets are proposed until produced.

| AC / prerequisite | Task | Command / executable procedure | Evidence target |
| --- | --- | --- | --- |
| Source freshness and scope | T001 | `python3 automation/context.py check M046`; `python3 automation/context.py audit` | context.json; tasks planning record |
| AC-001/008 | T002/T006 | `bash scripts/backend.sh check`; `bash scripts/contract.sh generate`, review, then `bash scripts/contract.sh check`; sample exercised by published smoke | backend reports; reviewed schema/client diff |
| AC-002/006 | T003 | `bash scripts/ai.sh check`; `bash scripts/ai.sh probe`; `bash scripts/ai.sh conformance`; backend check for HTTP zero-dispatch/cost/fingerprint guards | offline and transport reports |
| AC-005 | T004 | Shared fixtures consumed by `bash scripts/backend.sh check` and `bash scripts/frontend.sh check`; unknown-revision component/browser case | identical scalar spans and option validation reports |
| AC-003/004/007 | T005/T006 | `bash scripts/frontend.sh check`; `bash scripts/frontend.sh smoke`; intentional capture with `LINGUADESK_UPDATE_VISUALS=1 bash scripts/frontend.sh smoke`, inspect captures, then normal smoke | unit/E2E reports; visual-review.md |
| AC-008 | T006/T008 | Backend check plus published bearer consumer; sentinel persistence/log/status assertions and old fingerprint/restart tests | API/privacy/recovery reports |
| AC-009 | T001/T007 | Planned `bash scripts/ai.sh evaluate-translation --suite m046 --live --profile "$M046_PROFILE" --max-dispatches "$M046_MAX_DISPATCHES" --max-spend-usd "$M046_MAX_SPEND_USD"`; same flags with `evaluate-rewriting`, `evaluate-alternatives`, and bounded `evaluate-eligibility` when needed | artifacts/m046/; live-review.md with aggregate authorization/exposure record |
| All local regressions | T008 | `bash scripts/verify-milestone.sh M046`; `git diff --check`; separately verify required live review exists and passes | tasks completion record and coverage |

Values in the live command come from the actual authorization record. Reusing the same ceiling independently across commands must not multiply authorized spend: preflight deducts settled and unresolved exposure across the whole run. Default local verification performs no paid calls.

## Migration, rollout and rollback

No EF migration, account change, new secret or provider family is expected. Confirm language values are not constrained to four IDs in any durable path; widen only an actually discovered constraint through reviewed design/migration if necessary. New language descriptors are compiled data. Operation IDs/fingerprints for old languages remain byte-compatible.

A future authorized rollout deploys matching server/SPA/schema expectations and catalog/prompt/segmentation revisions together after applicable evidence gates. Existing clients with closed enums must regenerate to discover new values; preserve all old request meanings. Reverting to a four-language binary can reject new-language requests and must not corrupt pending/status identities: quiesce admissions, settle/fence in-flight work, and keep a compatible recovery reader until the 24-hour identity window expires. Never delete operation or ledger rows to enable downgrade. Exercise rollback against isolated data, not production. Planning/qualification does not authorize deployment.

## Context boundaries and risks

Code expansion is mostly shared data/policy work; validating 120 new languages is not zero effort. Translation explicitly avoids quadratic evaluation. Write/alternatives remain larger evidence sets; keep them bounded and reuse observations only where they prove the same assertion. Failure to meet a selected language's requirements is recorded at the owner before any scope change. Do not quietly advertise untested additions or invent model-specific guarantees.

Provider access, reference quality, font coverage, short-text ambiguity, punctuation tailoring, serialized prompt size and mixed-version clients are principal risks. No new authentication/email/account lifecycle, retention system, advanced routing or provider adapter is selected; open those owners only for a concrete discovered dependency. Read API identity/ledger/privacy rules on demand for regressions. Human/native-device/AT checks remain optional. This plan requests no sub-agent work and creates no new paid-run authorization.
