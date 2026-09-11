# M017 — Selected specification
Selected items: BI-017. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: strict local parsing of the `rewriting.v1` result envelope
(AI §4.2 translation/rewriting row: `result` + non-whitespace complete text,
`refused` + null); a `RewritingPipeline` that reuses the M015
`EligibilityPipeline` (local gates, one classification dispatch, terminal
mapping with resolved-language reuse), validates exactly one requested mode
against the 9-entry `ProductCatalog.RewritingModes` catalog (default
`correctionOnly`; styles simple/casual/business/academic; tones
enthusiastic/friendly/confident/diplomatic) and then issues exactly one
transformation dispatch through the same prompt/pipeline boundary with the
validated source, resolved source language and the single mode; correction
applied in every mode with minimal edits under correction-only (already
correct text may remain unchanged); output kept in the resolved source
language (no translation into another language; Chinese output Simplified);
deterministic rejection of empty/malformed/truncated/tool-call/non-text
output and provider refusals with no salvage or repair; a bounded, reviewed
live development slice through the M019 profile/adapter/budget path covering
every language/mode cell. Requirements FR-012–014/016; AI §3.2/4/5.2/7/8/9.1;
LLM-AC-005/006/007/008/013; V-007/V-008.

Dependencies: M015 (eligibility pipeline, envelope-parser pattern, runner
pattern — Done, reused not re-proven), M004 (prompt composition,
scripted-call boundary — Done), M005 (counting policy, fixtures, operation
limits — Done), M016 (translation pipeline pattern — Done, reused not
re-proven), M019 (candidate registry, adapter, credential resolver,
evaluation budget — Done; its `DeepSeek-V4.1-Flash` access check and staged
credential are reused, not re-proven). `ProductCatalog.RewritingModes`,
`IsRewritingMode` and the 2000-character rewriting limit already exist in
code and are consumed, not redefined.

Exclusions: chain fallback/deadline traversal (M018 — a provider refusal or
invalid rewriting output is recorded failure here, never silently retried or
passed to a fallback), corpus runs and evaluation reports (M020), API/UI
integration (FR-017/018/022/023 result-lifecycle behaviors belong to
M028–M030; FR-016's explicit-submission UI wiring belongs to the workspace
slice, not this pipeline slice), script-identity checker selection and
quality/cost/latency qualification (V-013/V-014), production serving startup,
DF-001/DF-002/DF-003/DF-004/DF-006. FR-015 is retired and creates no
obligation. No per-route provider assignment; no new candidate
qualification. No translation-direction validation is added; translation
inputs are rejected by the shared selector, not rewritten.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | Exactly one requested mode from the 9-entry catalog is accepted (default `correctionOnly` when the caller passes the default); unknown, null/empty or multiple modes are rejected with zero transformation dispatches; correction-only permits already-correct text to remain unchanged while every other mode still corrects | FR-013/014; AI §3.2; LLM-AC-006; ProductCatalog modes |
| AC-002 | Eligible input in each of the 4 languages yields one validated complete plain-text result through the `rewriting.v1` prompt/pipeline: the prompt carries the original complete source as serialized data with validated task parameters (resolved source language, exactly one mode), no history/prior-result/repair transcript enters context; the result stays in the resolved source language (never translated), Chinese output is Simplified, facts/uncertainty are preserved, and only validated plain text (never the envelope) is returned | FR-012–014; AI §3.2/4.1; LLM-AC-005/006 |
| AC-003 | Provider refusal, empty/malformed/truncated/tool-call/non-text output and provider failure are recorded failures that never become success; the whole output is discarded with no salvage, repair or same-candidate retry; ineligible input (M015 gates) still ends with zero transformation dispatches | AI §4.3/7; LLM-AC-007/008; FR-004/007 |
| AC-004 | An explicit budgeted runner workflow executes an allowlisted synthetic slice with at least one case in every language/mode cell (4 languages × 9 modes, both Chinese input scripts included) through the selected profile; each live case records outcome/category, attempts, usage/exposure and deterministic findings with failures retained; rejected/failed inputs are never silently rewritten; a human reviews the sanitized report | AI §5.2/9.1; verification §3.3/7.2; LLM-AC-013 (development portion); V-008 |
| AC-005 | Logs, traces, errors and reports carry only allowed metadata (`credentialRef`/`credentialPresent`, case IDs, categories, usage/exposure); no provider key, workspace text or hidden reasoning is persisted outside explicitly designated synthetic evaluation artifacts; every live dispatch is budget-admitted with conservative reservation and unresolved exposure retained | AI §5.2/5.4/8; verification §7.2 |

## Constraints and decisions

Nonfunctional constraints: finite per-stage output/byte bounds on every
dispatch (AI §5.4, via the M019 adapter); offline-first development with
provider networking disabled except the explicit budgeted live slice; paid
rewriting reserves conservative exposure but character charging itself
belongs to later API milestones — this slice never charges. The 36-cell
live slice is larger than M016's 12-direction slice; dispatch and spend
caps must cover it explicitly (`--max-dispatches` ≥ cells plus scripted
edge cases).

Clarification status: Q-001/Q-007 remain open; M017 closes only the
rewriting prompt/pipeline/envelope/mode portion plus bounded development
evidence, not token-bound proof, qualification, corpus approval or
monetary-cap decisions. DF-001/DF-002/DF-003/DF-004/DF-006 stay deferred.

Human gates: owner credential already staged (M019); executor runs the
single bounded live slice; human reviews the sanitized report at handoff,
including mode-behavior spot review (correction minimality, one
non-correction style and one tone per language) and Simplified-output spot
review of the zh cells. A missing credential blocks AC-004's live portion
only; AC-001–003 and AC-004's offline portions plus AC-005's offline
portions still complete. Live rewriting quality in this slice is
development evidence only, not qualification — meaning/style/tone fidelity
stays with M020/M035+.
