# M018 — Selected specification
Selected items: BI-018. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: per-family chain configuration (`Translation { Primary,
Fallback? }`, `Rewriting { Primary, Fallback? }`) referencing named
registry candidates; a chain orchestrator driving the M016 translation
and M017 rewriting pipelines through AI §6 explicit traversal (at most
three paid dispatches across both candidates, accepted eligibility
reused, fallback receives the original complete source and original
settings); AI §7 eligible-trigger versus terminal mapping; the original
overall deadline (PRD 30 seconds; engineering stage defaults 5 s
eligibility / 10 s transformation / 2 s finalization reserve) enforced
with an injectable clock and fake-time proof; per-attempt cost admission
through the M019 evaluation budget with conservative exposure; a small
primary-only live bounds slice in both families recording
attempt/usage/exposure/deadline metadata. Requirements
FR-029/030/032–034, NFR-002 (deadline portion), NFR-006 (exposure
portion); AI §5.1/5.2/5.4/6/7/8/9.1; LLM-AC-002/008/009/010/011/014;
V-007/V-008.

Dependencies: M015 (eligibility pipeline and terminal mapping — Done,
reused not re-proven), M016 (translation pipeline, envelope parser,
runner case pattern — Done), M017 (rewriting pipeline, mode validation —
Done), M004 (prompt composition, scripted-call boundary — Done), M005
(counting policy, fixtures, operation limits — Done), M019 (candidate
registry with `DeepSeek-V4.1-Flash` plus secondary reference, adapter,
credential resolver, evaluation budget — Done; its access check and
staged credential are reused, not re-proven). The evaluation chain
snapshot uses primary `DeepSeek-V4.1-Flash` with fallback
`DeepSeek-V4.1-Flash-SecondaryRef`; both profiles already exist in
`CandidateRegistry.Default`.

Exclusions: route-specific selection and multi-candidate chains (DF-004
— no match predicates, priorities, weights or serving arrays; language/
route/mode never changes the family chain); corpus runs, grading and
evaluation reports (M020); API/UI integration and durable character
settlement (character charging itself belongs to later API milestones —
this slice never charges); performance workloads and percentile
measurement (V-014 pending); candidate quality/cost/latency
qualification and token-bound proof (Q-001); production serving startup
and monetary-cap decisions; DF-001/DF-002/DF-003/DF-006. Live fallback
is out of scope by roadmap design: injected failures are proven
deterministically offline, and the live path is primary-only so mocks
are never treated as live evidence.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | Each family configures one primary and zero or one fallback by candidate ID; unknown IDs, identical primary/fallback profiles and any route/mode-dependent selection fail validation with zero dispatches; the evaluation snapshot resolves primary `DeepSeek-V4.1-Flash` with fallback `DeepSeek-V4.1-Flash-SecondaryRef` | FR-029/030/032; AI §5.1; LLM-AC-002 |
| AC-002 | Deterministic fault cases traverse both §6 three-dispatch paths in each family (primary-eligibility-fails → fallback-eligibility → fallback-transformation; primary-eligibility-succeeds → primary-transformation-fails → fallback-transformation); accepted eligibility is reused, never re-fetched; the fallback receives the original complete source and original settings; each candidate-stage pair is visited at most once with no same-candidate retry, JSON repair, hedging or return to a previous candidate; primary success never dispatches the fallback | FR-032/033; AI §6.1; LLM-AC-008 |
| AC-003 | Provider transient failures (network/timeout/throttling/service errors), configuration/access failures with an independently usable fallback, invalid outputs (empty/malformed/truncated/non-text/invalid envelope), refusals and unknown protocol finishes advance once to the fallback; invalid local input, auth/verification failures, admission denials, budget denial, internal defects and deadline/cancellation end the operation with no fallback; a candidate sharing a known-blocked `CredentialRef` scope is skipped without dispatch | FR-033; AI §7; LLM-AC-008 |
| AC-004 | The original overall deadline (30 s) is captured once and never reset by admission, eligibility, fallback or validation; no dispatch starts when the complete stage allowance no longer fits inside the remaining budget minus the finalization reserve; transport is cancelled at its deadline, late output is fenced and can never resurrect a terminal result; fake time expiring during admission, eligibility, transformation or fallback yields deadline failure with no post-deadline dispatch or success | NFR-002; AI §6.2; LLM-AC-009 |
| AC-005 | Every paid dispatch is admission-reserved before launch at peak cache-miss rates; denied budget stops traversal with no further dispatch; timeout, unknown/missing usage and cancelled calls retain the full conservative reservation as unresolved exposure; every definitive overall failure carries zero character charge; successful fallback returns complete plain text indistinguishable from primary success in output/usage shape | FR-034; NFR-006; AI §5.4; LLM-AC-010 |
| AC-006 | An explicit budgeted runner workflow executes a small allowlisted synthetic slice in both families through the primary-only chain live; each live case records outcome/category, attempts, per-attempt usage/exposure, deadline timings and deterministic findings with failures retained; scripted fault cases are marked offline-only and never counted as live evidence; a missing credential blocks the live run without fallback to scripted output; a human reviews the sanitized metadata-only report | AI §5.2/9.1; verification §3.3/7.2; LLM-AC-011/013/014 (development portion); V-008 |

## Constraints and decisions

Nonfunctional constraints: finite per-stage output/byte bounds on every
dispatch (AI §5.4, via the M019 adapter); offline-first development with
provider networking disabled except the explicit budgeted live slice;
the live slice is primary-only so its dispatch cap stays small (both
families' natural two-call paths plus edge cases); paid chain work
reserves conservative exposure but character charging itself belongs to
later API milestones — this slice never charges. Deadline engineering
defaults (5 s / 10 s / 2 s reserve) are tuneable settings requiring
renewed performance evidence, not product SLAs; the percentile targets
stay with V-014.

Clarification status: Q-001/Q-007 remain open; M018 closes only the
chain/fallback/deadline/admission portion plus bounded development
evidence, not token-bound proof, qualification, corpus approval or
monetary-cap decisions. DF-001/DF-004 stay deferred: no per-route
assignment, no shared-alternative fallback, no serving arrays.

Human gates: owner credential already staged (M019); executor runs the
single bounded primary-only live slice; human reviews the sanitized
report at handoff, including bounds/metadata completeness and
retained-failure handling. A missing credential blocks AC-006's live
portion only; AC-001–005 and AC-006's offline portions still complete.
Live behavior in this slice is development evidence only, not
qualification — fallback liveness, quality and latency qualification
stay with M020/V-013/V-014.
