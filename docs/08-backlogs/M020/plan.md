# M020 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Add a combined-report workflow to the host-independent evaluation
runner without changing any family pipeline, chain traversal, adapter
or budget semantics: a new `evaluate-report` command that runs the
existing offline slice runners (eligibility, translation, rewriting,
chain-bounds) in scripted mode plus the same slices live through the
primary-only production pipeline under the M019 `EvaluationBudget`,
then emits one versioned JSON report following verification §7.2 with
per-observation disposition labels and conservative exposure totals.
Nothing here grades quality, qualifies candidates, builds corpus,
touches serving/API/UI/storage, or changes dispatch/budget behavior.

Selected canonical excerpts arrive through the context packet. Do not
copy them all here. The essential local invariants: offline runs read
no credential and make zero provider calls; live runs need an explicit
profile plus finite dispatch+spend+deadline budget and a present
credential, otherwise live sections report `blocked` with exit 3 and
zero dispatches, never faked; injected faults are labeled
`fault_injected` and excluded from live-behavior counts; every paid
attempt is reserved at peak cache-miss rates before launch with
unresolved exposure retained on missing/inconsistent usage; reports
carry `credentialRef`/`credentialPresent` only — never secret values,
headers, env dumps, fingerprints or raw bodies.

## Changes and order

1. `backend/tools/LinguaDesk.Ai.Evaluation/` — combined-report model:
   versioned `evaluation_report` record (format version `1`,
   run manifest, per-section observations reusing the four existing
   observation shapes plus a disposition label, per-family/route/
   language aggregates with rewriting mode breakdowns, exposure
   totals, deterministic findings, blocked-section markers).
   Depends on nothing; done when MSTest cases prove the schema
   (required §7.2 fields present, fault/live sections disjoint,
   aggregates match section rows).
2. Same tool — offline aggregation: `evaluate-report --offline`
   executes the four existing offline slice paths in-process (scripted
   clients, no credential read, networking disabled) and emits the
   combined report to `artifacts/evaluation/`; nonzero exit on any
   natural-fixture mismatch with failures retained. Depends on step
   1; done when the offline run passes with zero dispatches and the
   report validates (AC-001).
3. Same tool — live aggregation: `evaluate-report --live --profile
   <id> --max-dispatches <n> --max-spend-usd <amount>
   [--deadline-ms <n>] [--output <path>]` runs the bounded live
   development slices primary-only through the production pipeline
   under one shared `EvaluationBudget`, prepends the `live_access`
   probe observation, skips offline-only fault cases, marks
   missing-credential sections `blocked` (exit 3, zero dispatches),
   and records per-attempt usage/reserved/actual/unresolved exposure
   with run totals. Depends on steps 1–2; done when the live shape
   is proven blocked-without-credential and budgeted-with-credential
   in a dry review (AC-002/AC-003/AC-004 live portions).
4. `scripts/ai.sh` + operating guide — extend the AI command surface
   (`evaluate-report` offline/live passthrough with credential
   stripping for offline mode mirroring the existing evaluate
   commands) and document the explicit live invocation with finite
   budget flags; keep offline the default. Depends on step 3; done
   when `bash scripts/ai.sh check` covers the new tests and the
   README documents the reviewed procedure.
5. Regressions + live batch + human review — run `backend.sh check`,
   `contract.sh check`, secret sweep over sources/runner/scripts/
   reports and `context.py check M020`; execute the bounded live
   development batch against `DeepSeek-V4.1-Flash`; present the
   sanitized combined report for human review (disposition labeling,
   retained-failure handling, exposure completeness) and record
   disposition. Depends on step 4 (offline portions may run if the
   live gate is blocked); done when all ACs hold with revision/
   environment/UTC time recorded.

No migration, rollout or rollback: no storage, config-file or serving
change. No new package dependencies expected; the runner keeps its
existing references (evaluation-only scope stays out of the serving
library).

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 | T002 | V-007: `bash scripts/ai.sh check` (report-schema + offline-aggregation MSTest cases) plus `bash scripts/ai.sh evaluate-report --offline --profile DeepSeek-V4.1-Flash --max-dispatches <n> --max-spend-usd <amount>` with networking disabled | `artifacts/test-results/ai.trx` + `artifacts/evaluation/` offline report |
| AC-002 | T003 | V-008: `bash scripts/ai.sh evaluate-report --live --profile DeepSeek-V4.1-Flash --max-dispatches <n> --max-spend-usd <amount>`; human reviews sanitized report | `artifacts/evaluation/` live report |
| AC-003 | T003 | V-008: same live command with the credential variable unset → `blocked` sections, zero dispatches, exit 3 | blocked report + exit code |
| AC-004 | T003 | V-006: exposure-aggregation MSTest cases + secret sweep over sources/runner/scripts/reports; `credentialRef`-only diagnostics assertion | TRX + sweep result + report |
| AC-005 | T001/T005 | V-013 (report-shape portion): schema-field checklist against §7.2 + recorded human review disposition | checklist + completion record |
| Regression | T005 | `bash scripts/backend.sh check`, `bash scripts/contract.sh check`, `python3 automation/context.py check M020` | task completion record |

## Context boundaries and risks

Omitted domains and why: UX/browser (no UI surface), API
handlers/auth (no serving integration), durable character
settlement/period attribution (evaluation budget only; charging lives
with M021–M025), storage/migrations, email, corpus construction,
AI grading and qualification review (M035/M036/V-013 full),
performance workloads and percentile measurement (M037/V-014;
deadlines already proven in M018), production serving startup and
monetary-cap decisions (Q-001), route-specific chains and shared
alternatives (DF-004/DF-001). Open on-demand: M035 package when
corpus work starts; the DeepSeek research snapshot only on adapter
wire drift.

Dependencies and risks: M015–M018 slice runners and M019
registry/adapter/budget/credential path are Done and consumed
unchanged — the report workflow aggregates their outputs, never
edits their semantics; live-batch cost is bounded by explicit flags
plus conservative reservation and stays small because the batch is
the existing primary-only development slices; fallback liveness,
quality and latency qualification stay explicitly pending — record
those limitations in the report and completion record. If the
credential is absent, the live sections stay blocked and the offline
sections still complete. Any `ProductCatalog` or prompt-revision
change requires spec/plan impact analysis before execution claims
the ACs.
