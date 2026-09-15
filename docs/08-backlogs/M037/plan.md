# M037 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Build the smallest real-API benchmark harness that §6.1 can trust, then
prove it with a 24-request unpaid rehearsal. The evaluation runner today
drives pipelines directly — it has no HTTP timing, no concurrency
discipline, no denominator math and no §6.1 report — so this milestone adds
a runner-only benchmark tool that submits through the real HTTP boundary
against an owned loopback host and records everything §6.1 requires.
Indispensable invariants: submission-to-complete-body timing with
serving-committed success; full-denominator counting (failures never
timely); nearest-rank percentiles over all observations; fresh identities
for repeats; reuse disabled; unknown cache cohorts stay unknown; every
§6.1 composition rule either rehearsed or explicitly deferred to the
successor. Excluded: the 360-request live workload, §6.2 deadline/fault
evidence, any NFR-002/G2 claim, and all serving/pipeline/UI/storage edits.

Selected canonical excerpts arrive through the context packet. Do not
copy them all here.

## Changes and order

1. Benchmark tool (new `backend/tools/LinguaDesk.Api.Benchmark/`, runner-only
   references, never serving dependencies; surfaced as `benchmark` in
   `scripts/backend.sh` alongside `check`/`smoke`, documented in the tool
   README): workload manifest loader (declared composition: family cells,
   length bands, distinct combinations, repeat-with-fresh-identity rule,
   interleave seed), HTTP driver (bounded concurrency with N-in-flight
   discipline and drain disclosure, per-request submission→complete-body
   clock, stage-duration capture from authoritative usage/settlement
   records, CredentialRef presence only), composition auditor (fails the
   run on missing band/cell, repeated identity, replayed completion, or
   dropped request), and §6.1 report writer (all AC-006 fields, full
   observation preservation, retained failures). Depends on nothing; done
   when unit tests prove denominator/percentile/cohort math on synthetic
   observations and the composition auditor rejects each violation class
   with a fixture. (AC-002/AC-003/AC-004/AC-005)
2. Owned-host rehearsal wiring: launch the published API on an OS-assigned
   loopback port with isolated migrated file-backed SQLite (WAL), seed a
   local test account, exercise real login/auth per the §3.1 smoke pattern,
   run the 24-request rehearsal manifest (12 Translation + 12 Rewriting
   across all three bands with both Chinese scripts, recorded seed), then
   dispose the host on failure; test-only adapters unreachable outside this
   wiring. Depends on step 1; done when the rehearsal passes end to end
   with credential env stripped and zero provider dispatches. (AC-001)
3. Rehearsal report, regressions and manifest check: versioned report file
   under `artifacts/benchmark/` with deployment/endpoint/provider identity,
   configuration, per-request and aggregate §6.1 sections; `bash
   scripts/backend.sh check`, `bash scripts/ai.sh check`,
   `python3 automation/context.py check M037`, secret/synthetic-text sweep
   over new code and the report. Depends on steps 1–2; done when all pass
   and the sweep is clean. (AC-006)

No migration, rollout or rollback: no storage, config, serving or wire
change. The benchmark tool never enters the serving host or its test
surface beyond the HTTP boundary.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 | T002 | V-014: owned-loopback rehearsal, 24/24 measured through real HTTP auth + settlement; readiness separation and drain audited in the report | rehearsal report + host log |
| AC-002 | T001 | V-014: timing unit tests (submission→complete-body, stage capture) plus rehearsal duration audit; no TestServer timing presented | test results + report |
| AC-003 | T001 | V-014: denominator/percentile unit tests incl. failure-as-not-timely and successful-only rejection; composition-audit fixtures per violation class | test results |
| AC-004 | T001 | V-014: cohort unit tests (first-seen/repeated, hit/miss/unknown) + reuse-disabled assertion in the rehearsal | test results + report |
| AC-005 | T001 | V-014: review of the declared §6.1 rule table against the rehearsal manifest mapping | plan + manifest file |
| AC-006 | T003 | V-014: §6.1 field audit of the versioned report; secret/synthetic-text sweep over code + report | report file + sweep result |
| Regression | T003 | `bash scripts/backend.sh check`; `bash scripts/ai.sh check`; `python3 automation/context.py check M037` | task completion record |

## Context boundaries and risks

Omitted domains and why: UX/browser (no UI surface — rehearsal is
API-only); LLM quality grading and human review (M036 track, untouched);
§6.2 maximum-length/fallback/fault deadlines (explicit successor scope —
the harness records stage durations the successor will assert against);
email; storage migrations (isolated scratch databases only, §3.1 pattern);
live provider, cost and serving qualification (Q-001/Q-007 — zero paid
dispatches by construction). Open on-demand: the successor live-workload
package when budgeted; M026/M027 specs if the HTTP boundary misbehaves;
M020 report spec if §7.2 field questions arise.

Risks: loopback timing noise is irrelevant (rehearsal asserts recording,
not targets); fake-provider cache behavior yields mostly `unknown`/miss
cohorts — the cohort paths are proven structurally, live hit/miss
observation waits for the successor; concurrency-4 proves the discipline
logic, not 10-way host behavior.
