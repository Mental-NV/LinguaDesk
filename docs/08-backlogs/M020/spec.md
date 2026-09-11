# M020 — Selected specification
Selected items: BI-020. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: one explicit runner workflow in `LinguaDesk.Ai.Evaluation`
(plus `scripts/ai.sh` surface) that executes the bounded fixture batch
offline and the bounded live development batch live through the
production prompt/pipeline, and emits a single versioned combined
report. The fixture batch reuses the M015–M018 allowlisted synthetic
development cases (eligibility, every Translation direction, every
Rewriting language/mode cell, chain-bounds slice) in scripted mode with
provider networking disabled. The live batch reuses the same slices in
primary-only natural mode under an explicit profile plus finite
dispatch/spend/deadline budget, including the `live_access`
verification observation. Every observation carries its disposition
(`offline_fixture`, `transport_fixture`, `live_access`,
`live_development`, `fault_injected`), case identity, attempts,
durations, usage/exposure metadata and deterministic findings; injected
faults are labeled separately and never counted as live behavior
evidence. Missing/blank credential blocks the live sections with zero
dispatches and no scripted fallback. Unresolved spend is retained
conservatively and aggregated per run.

Dependencies: M018 Done (chain traversal, fault matrix, deadline and
admission proof, live-slice report pattern — reused, not re-proven);
M019 Done (candidate registry with `DeepSeek-V4.1-Flash`, adapter,
credential resolver, evaluation budget, access check — reused, not
re-proven); M015/M016/M017 Done (eligibility/translation/rewriting
pipelines and development slices — consumed unchanged); M004/M005 Done
(inner loop, counting policy). AI §5/9; verification §3.3/5.0/7.2;
V-006/V-007/V-008/V-013 (development-report portion only).

Exclusions: frozen release corpus construction and approval (M035);
AI grading, human qualification review and qualification dispositions
(M036/V-013 full); API performance workloads and percentile
measurement (M037/V-014); serving/API/UI integration, durable
character settlement and period attribution (M021–M025); production
serving startup and monetary-cap decisions (Q-001); route-specific
chains and shared alternatives (DF-004/DF-001); thinking-mode,
context-limit and price qualification.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | One explicit command runs the bounded fixture batch fully offline (provider networking disabled, no credential read): all four workflow slices execute scripted, injected-fault cases are labeled `fault_injected`, natural fixture cases `offline_fixture`, and the combined report is emitted with zero provider dispatches | AI §9.1; verification §3.3/5.0; V-007 |
| AC-002 | The same command with `--live`, an explicit candidate profile and finite `--max-dispatches/--max-spend-usd/--deadline-ms` budget runs the bounded live development slice through primary-only chains reusing the production pipeline; live cases are labeled `live_development`, the access probe `live_access`, offline-only fault cases are skipped (never relabeled live), and failures are retained per case with attempts/durations/usage/exposure | AI §5.2/9.1; verification §3.3/5.0/7.2; V-008 |
| AC-003 | A missing or blank credential for the selected profile blocks every live section: the report marks them `blocked` with zero dispatches, nonzero/non-success exit, and no substitution of scripted output; the offline fixture sections still complete | AI §5.2; verification §3.3/5.0; V-008 |
| AC-004 | Every paid attempt is admission-reserved before launch at peak cache-miss rates; each observation records reserved, authoritative actual (where present) and unresolved exposure; missing or inconsistent usage retains the full reservation as unresolved; per-run and per-family exposure totals are reported; the report and all diagnostics contain only `credentialRef`/`credentialPresent` — no key, header, env dump, fingerprint, raw provider body, unrestricted prompt/output or production text | AI §5.4; verification §7.2; V-006 |
| AC-005 | The combined report is versioned (report format version, code/SDK revision, prompt/validator/settings/bundle hashes, candidate/profile/adapter/model/endpoint identity, corpus/case revisions, billing snapshot, case selection, concurrency, timestamps, live/fixture/fault disposition) and aggregates per family/route/language with rewriting mode breakdowns; a human reviews the sanitized report and records disposition; review is development acceptance, not qualification | Verification §5.0/7.2; V-013 (report-shape portion) |

## Constraints and decisions

Nonfunctional constraints: offline-first development with provider
networking disabled except the explicit budgeted live batch; the live
batch stays small (existing development slices only) and primary-only
so its dispatch cap stays small; conservative fixed-point exposure
arithmetic rounded up; metadata-only reports (source hashes/selections,
never full ad hoc text or production text); runner-only scope — no
serving, storage, API/UI or grading dependencies enter the library.

Clarification status: Q-001 (monetary cap, serving qualification),
Q-005 (corpus/grading/human qualification design — report shape only
here) and Q-007 (orchestration policy — M018 portion closed) remain
open; M020 closes only the combined development-report workflow plus
bounded development evidence, not qualification, corpus approval or
cap decisions.

Human gates: owner credential already staged (M019); executor runs the
single bounded live batch under the explicit finite budget; human
reviews the sanitized combined report at handoff, covering disposition
labeling, retained-failure handling and exposure completeness. A
missing credential blocks AC-002/AC-003's live portions only;
AC-001 and the offline sections still complete.
