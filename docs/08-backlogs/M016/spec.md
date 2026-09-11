# M016 — Selected specification
Selected items: BI-016. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: strict local parsing of the `translation.v1` result envelope
(AI §4.2 translation row: `result` + non-whitespace complete text,
`refused` + null); a `TranslationPipeline` that reuses the M015
`EligibilityPipeline` (local gates, one classification dispatch, terminal
mapping with resolved-language reuse) and then issues exactly one
transformation dispatch through the same prompt/pipeline boundary with the
validated source, resolved source language and explicit target; deterministic
rejection of empty/malformed/truncated/tool-call/non-text output and provider
refusals with no salvage or repair; a bounded, reviewed live development
slice through the M019 profile/adapter/budget path covering every
Translation direction. Requirements FR-006–011; AI §3.2/4/5.2/7/8/9.1;
LLM-AC-005/006/007/008/013; V-007/V-008.

Dependencies: M015 (eligibility pipeline, envelope-parser pattern, runner
pattern — Done, reused not re-proven), M004 (prompt composition,
scripted-call boundary — Done), M005 (counting policy, fixtures, operation
limits — Done), M019 (candidate registry, adapter, credential resolver,
evaluation budget — Done; its `DeepSeek-V4.1-Flash` access check and staged
credential are reused, not re-proven).

Exclusions: rewriting modes (M017), chain fallback/deadline traversal (M018 —
a provider refusal or invalid translation output is recorded failure here,
never silently retried or passed to a fallback), corpus runs and evaluation
reports (M020), API/UI integration, script-identity checker selection and
quality/cost/latency qualification (V-013/V-014), production serving startup,
DF-001/DF-004/DF-006. No per-route provider assignment; no new candidate
qualification. No rewriting-mode validation is added; rewriting inputs are
rejected by the shared selector, not translated.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | Strict parser accepts exactly the §4.2 translation forms (`result` + non-whitespace complete text; `refused` + null) and rejects unknown/duplicate fields, unknown enums, multiple JSON values, missing fields, wrong types, excessive nesting, trailing prose and code fences; legitimate refusal wording inside source text is never a parser refusal; raw response bytes are bounded before deserialization | AI §4.2; LLM-AC-007 |
| AC-002 | Eligible input in each of the 12 Translation directions yields one validated complete plain-text result through the `translation.v1` prompt/pipeline: the prompt carries the original complete source as serialized data with validated task parameters (resolved source, explicit different target), no history/prior-result/repair transcript enters context, and only validated plain text (never the envelope) is returned | FR-008–011; AI §3.2/4.1; LLM-AC-005; ProductCatalog directions |
| AC-003 | Provider refusal, empty/malformed/truncated/tool-call/non-text output and provider failure are recorded failures that never become success; the whole output is discarded with no salvage, repair or same-candidate retry; ineligible input (M015 gates) still ends with zero transformation dispatches | AI §4.3/7; LLM-AC-007/008; FR-004/007 |
| AC-004 | An explicit budgeted runner workflow executes an allowlisted synthetic slice with at least one case in every Translation direction (both Chinese input scripts included, Simplified output recorded) through the selected profile; each live case records outcome/category, attempts, usage/exposure and deterministic findings with failures retained; rejected/failed inputs are never silently transformed; a human reviews the sanitized report | AI §5.2/9.1; verification §3.3/7.2; LLM-AC-013 (development portion); V-008 |
| AC-005 | Logs, traces, errors and reports carry only allowed metadata (`credentialRef`/`credentialPresent`, case IDs, categories, usage/exposure); no provider key, workspace text or hidden reasoning is persisted outside explicitly designated synthetic evaluation artifacts; every live dispatch is budget-admitted with conservative reservation and unresolved exposure retained | AI §5.2/5.4/8; verification §7.2 |

## Constraints and decisions

Nonfunctional constraints: finite per-stage output/byte bounds on every
dispatch (AI §5.4, via the M019 adapter); offline-first development with
provider networking disabled except the explicit budgeted live slice; paid
translation reserves conservative exposure but character charging itself
belongs to later API milestones — this slice never charges.

Clarification status: Q-001/Q-007 remain open; M016 closes only the
translation prompt/pipeline/envelope portion plus bounded development
evidence, not token-bound proof, qualification, corpus approval or
monetary-cap decisions. DF-001/DF-004/DF-006 stay deferred.

Human gates: owner credential already staged (M019); executor runs the
single bounded live slice; human reviews the sanitized report at handoff,
including Simplified-output and fidelity spot review of the zh-target cases.
A missing credential blocks AC-004's live portion only; AC-001–003 and
AC-004's offline portions plus AC-005's offline portions still complete.
Live translation quality in this slice is development evidence only, not
qualification — meaning/style/tone fidelity stays with M020/M035+.
