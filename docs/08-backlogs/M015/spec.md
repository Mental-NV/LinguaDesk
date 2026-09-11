# M015 — Selected specification
Selected items: BI-015. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: strict local parsing of the `eligibility.v1` response envelope (AI §4.2);
local zero-dispatch gates (empty/whitespace-only, oversize per operation family,
explicit equal source/target for translation) using `unicode-scalar-v1`;
terminal mapping of every classification (eligible, uncertain, unsupported,
mixed, source_mismatch, refused, invalid/malformed, provider failure) with no
transformation of rejected input and no fallback overturn of a validation
outcome; a bounded, reviewed live development slice through the M019
profile/adapter/budget path covering the supported languages and
model-decided negative classes. Requirements FR-004–007; AI §3–5;
LLM-AC-003/004/005; V-001/V-007/V-008.

Dependencies: M004 (prompt composition, scripted-call boundary), M005
(counting policy, fixtures, operation limits), M019 (candidate registry,
adapter, credential resolver, evaluation budget). All three are Done; M019's
`DeepSeek-V4.1-Flash` access check and staged credential are reused, not
re-proven.

Exclusions: transformation semantics (M016/M017), chain fallback/deadline
traversal (M018 — a provider failure in eligibility is recorded as failure
here, never silently passed), corpus runs and evaluation reports (M020),
API/UI integration, DF-001/DF-004/DF-006, quality/cost/latency qualification
(V-013/V-014) and production serving startup. No per-route provider
assignment; no new candidate qualification.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | Strict parser accepts exactly the §4.2 eligibility forms (`eligible` + supported language; `uncertain`/`unsupported`/`mixed`/`refused` + null; `source_mismatch` + detected language differing from the hint) and rejects unknown/duplicate fields, unknown enums, multiple JSON values, missing fields, wrong types, excessive nesting, trailing prose and code fences; raw response bytes are bounded before deserialization | AI §4.2; LLM-AC-007 |
| AC-002 | Empty/whitespace-only input, oversize input per operation limit (translation 5000 / rewriting 2000 scalars, `unicode-scalar-v1`), and explicitly equal source/target for translation end locally with zero provider dispatches and no charge path | FR-004/007; AI §3.1; LLM-AC-003; M005 policy |
| AC-003 | `uncertain`, `unsupported`, `mixed`, `source_mismatch` and `refused` classifications end without transformation, fallback or character success; an `eligible` classification contradicting the validated manual choice is invalid output, not a reinterpretation; empty/malformed classification and provider failure are recorded failures that never pass input as eligible | FR-004/005; AI §3.1/4.2; LLM-AC-004 |
| AC-004 | An accepted `eligible` classification returns the resolved source language for pipeline reuse; this slice performs no transformation and issues no translation/rewriting dispatch | AI §3.2; LLM-AC-005/008 (eligibility portion) |
| AC-005 | An explicit budgeted runner workflow executes an allowlisted synthetic slice covering en/ru/ro/zh, both Chinese scripts, short/ambiguous texts, mixed/unsupported content and source-mismatch cases through the selected profile; rejected inputs are not transformed; failures are retained with usage/exposure metadata and no secrets; a human reviews the sanitized report | AI §5.2/9.1; verification §3.3/7.2; LLM-AC-013 (development portion); V-008 |
| AC-006 | Logs, traces, errors and reports carry only allowed metadata (`credentialRef`/`credentialPresent`, case IDs, categories, usage/exposure); no provider key, workspace text or hidden reasoning is persisted outside explicitly designated synthetic evaluation artifacts | AI §8; verification §7.2 |

## Constraints and decisions

Nonfunctional constraints: finite per-stage output/byte bounds on every
dispatch (AI §5.4, via the M019 adapter); offline-first development with
provider networking disabled except the explicit budgeted live slice; paid
classification reserves conservative exposure but never charges character
allowance by itself.

Clarification status: Q-001/Q-005/Q-007 remain open; M015 closes only the
eligibility/parser/gate portion plus bounded development evidence, not
qualification, corpus approval or monetary-cap decisions. DF-001/DF-004/DF-006
stay deferred.

Human gates: owner credential already staged (M019); executor runs the single
bounded live slice; human reviews the sanitized report at handoff. A missing
credential blocks AC-005 only; AC-001–004 and AC-006's offline portions still
complete.
