# M037 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: none (all tasks done; package ready for delivery-status update).
Blockers: none.
Last check: 2026-09-15T19:40Z — `bash scripts/backend.sh check` 605/605
(API/storage 248, Core 10, independent AI 313, benchmark harness 34),
`bash scripts/backend.sh smoke` green, `bash scripts/backend.sh benchmark`
24/24 with zero paid dispatches, `bash scripts/ai.sh check` 313/313,
`bash scripts/ai.sh probe` green, `bash scripts/contract.sh check` green,
`python3 automation/context.py check M037` green after reviewed re-lock,
`python3 automation/context.py audit` green, `git diff --check` clean,
secret/synthetic-text sweep clean (`/tmp/m037-sweep.py`).
Changed scope: `scripts/backend.sh` (planned benchmark surface + harness
test gate) and the mandated `delivery/current.md` closeout update each
re-locked into [context](context.json) after diff review confirmed the
planned inputs semantically intact; the transient first `smoke` attempt
crashed in `dotnet restore` (segfault 11) and passed clean on rerun with no
code change.

## Ordered tasks
- [x] T001 — Benchmark tool (manifest loader, HTTP driver with N-in-flight discipline, composition auditor, §6.1 math + report writer, `backend.sh benchmark` surface); AC-002/AC-003/AC-004/AC-005; depends on none; done when denominator/percentile/cohort unit tests and per-violation-class auditor fixtures pass.
- [x] T002 — Owned-host rehearsal (loopback Kestrel + migrated scratch SQLite + real login, 24-request manifest with recorded seed, credential env stripped, zero provider dispatches); AC-001; depends on T001; done when 24/24 requests are measured through the real HTTP boundary with drain disclosed.
- [x] T003 — Versioned rehearsal report under `artifacts/benchmark/`, regressions (`backend.sh check`, `ai.sh check`, `context.py check M037`), secret/synthetic-text sweep; AC-006; depends on T002; done when all pass and the sweep is clean.

## Completion record
Executed 2026-09-15T19:36–19:40Z at revision `ec1a3e5` (working tree below:
new `backend/tools/LinguaDesk.Api.Benchmark/`, new
`backend/tests/LinguaDesk.Api.Benchmark.Tests/`, `backend/LinguaDesk.slnx`,
`scripts/backend.sh`, re-locked `docs/08-backlogs/M037/context.json`;
report at `artifacts/benchmark/m037-rehearsal-20260915T193643Z.json`),
Release configuration, .NET SDK 10.0.302. No human gates: rehearsal uses the
deterministic provider, isolated storage and a synthetic local account; the
successor live workload's owner-admitted budget, verified test accounts and
capacity are explicitly out of scope. Rehearsal success proves harness
recording fidelity only — it claims no NFR-002 compliance and discharges no
part of G2.

- T001: `LinguaDesk.Api.Benchmark` (Exe, net10.0; references only
  `LinguaDesk.Core` for catalog values and canonical scalar counting; never
  the serving host) with `SourceSynthesis` (deterministic exact-length
  synthetic sources, rune-based scalar counting, SHA-256),
  `ManifestLoader` (declared composition, repeat-with-fresh-identity
  expansion, recorded-seed interleave), `BenchmarkHttpDriver` (real
  register/Bearer-sign-in/submit, fresh UUIDv7 per submission,
  submission-to-complete-body clock, N-in-flight discipline with drain
  accounting, no response cache), `CompositionAuditor` (fails on missing
  band/cell, repeated identity, replayed completion, dropped request),
  `BenchmarkMath` (full-denominator success-within-target, nearest-rank
  percentiles with failures at infinite latency, no successful-only path)
  and `ReportWriter` (`benchmark-report.v1`, refuses unmapped rules).
  `Manifest/m037-rehearsal-24.json` declares 2 translation directions
  (en→zh, zh→ru) × 3 bands × 2 plus 2 rewriting cells (en/correctionOnly,
  en/business) × 3 bands × 2 with auto/manual selection and both Chinese
  input scripts; `Composition/full-360-rules.json` declares the 12 §6.1
  rules the manifest maps one by one. Surfaced as `benchmark` in
  `scripts/backend.sh` alongside `check`/`smoke`; tool README documents
  runs, inputs, outputs and review steps. Evidence: 34/34 new MSTest tests
  (`BenchmarkMathTests` 8 incl. failure-as-not-timely and successful-only
  contrast, `CompositionAuditorTests` 7 incl. all four violation classes,
  `ManifestLoaderTests` 6, `SourceSynthesisTests` 4, `HttpDriverTests` 5
  incl. concurrency-cap and clock-floor proofs, `ReportWriterTests` 4 incl.
  no-text-leakage and unmapped-rule rejection).
- T002: `bash scripts/backend.sh benchmark` builds the solution, migrates an
  isolated scratch SQLite database (WAL), starts the published API on an
  OS-assigned loopback port in the Smoke environment with the deterministic
  fake providers and a fixed test monetary cap, polls `/health/live` +
  `/health/ready` (2 predeclared probes, kept separate), then runs the tool
  with credential environment stripped and proxies dead-ended (loopback
  bypassed). Result 2026-09-15T19:36Z: 24/24 measured through the real HTTP
  auth + settlement boundary (12 translation + 12 rewriting, all 201 with
  committed charges totaling 9,096 characters), 24 unique submission
  identities, interleave seed 37037, max 4 in flight, final drain 434 ms,
  zero paid dispatches. AC-001 met.
- T003: report `artifacts/benchmark/m037-rehearsal-20260915T193643Z.json`
  (`benchmark-report.v1`, `status: complete`, empty `auditViolations`).
  AC-002: every request carries its submission-to-complete-body duration;
  stage durations stay explicitly unknown with provenance (the synchronous
  API exposes no per-stage boundary; inventing partitions would be
  fabrication). AC-003: full-denominator success-within-target 12/12 per
  family, nearest-rank p50/p95 per family (translation 5.6/485.4 ms,
  rewriting 5.4/653.5 ms — rehearsal clock values, not targets),
  per-route distributions, auditor green. AC-004: first-seen 12 / repeated
  12, provider-cache cohort 24 unknown with reason, reuse-disabled asserted
  (fresh UUIDv7 + no driver cache + uniqueness audit), per-request usage
  snapshots and committed charges, monetary exposure unknown-by-design with
  zero paid dispatches. AC-005: all 12 §6.1 rules mapped to a rehearsal
  analogue with successor-only aspects named. AC-006: every §6.1 field
  present, all observations preserved, CredentialRef + presence only,
  synthetic fixture text only, sweep clean. Regression evidence: backend
  check 605/605, `ai.sh check` 313/313, `ai.sh probe` exit 0, smoke green
  (one transient `dotnet restore` segfault first, clean rerun, no code
  change), contract check green, context check green after reviewed re-lock
  of the planned `backend.sh` change, context audit green, `git diff
  --check` clean. Coverage: RG-003/V-014 intentionally unchanged — the
  rehearsal is harness evidence, not percentile measurement, so the release
  gate stays pending by design.
