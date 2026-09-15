# LinguaDesk.Api.Benchmark runner

Real-API benchmark harness for the section 6.1 percentile workload (M037).
It submits through the real HTTP boundary against an owned loopback host and
records everything section 6.1 requires: submission-to-complete-body timings,
full-denominator success-within-target counts, nearest-rank percentiles,
per-route distributions, first-seen/repeated plus observed cache cohorts,
and a versioned report carrying every section 6.1 field.

- Project: `backend/tools/LinguaDesk.Api.Benchmark/LinguaDesk.Api.Benchmark.csproj`
- Library under test: none serving-side. The tool references only
  `LinguaDesk.Core` (catalog values, canonical scalar counting); it never
  references the serving host and never enters it except over HTTP.
- Requires .NET SDK `10.0.302`.

## How to run

Canonical entry point is `scripts/backend.sh benchmark`: it builds the API
and this tool, migrates an isolated scratch database, starts the published
API on an OS-assigned loopback port in the Smoke environment with the
deterministic fake providers, polls readiness, strips credential environment
and dead-ends proxies, runs the 24-request rehearsal manifest at 4 concurrent
operations, audits composition, writes the versioned report to
`artifacts/benchmark/`, and disposes the host on failure.

```bash
bash scripts/backend.sh benchmark
```

Direct equivalent (after the script has a host listening on `$BASE`):

```bash
DLL=backend/tools/LinguaDesk.Api.Benchmark/bin/Release/net10.0/LinguaDesk.Api.Benchmark.dll
dotnet $DLL rehearse --base-url "$BASE" \
  --email benchmark-001@example.test --password 'm037-benchmark-pass-01' \
  --manifest backend/tools/LinguaDesk.Api.Benchmark/Manifest/m037-rehearsal-24.json \
  --rules backend/tools/LinguaDesk.Api.Benchmark/Composition/full-360-rules.json \
  --output artifacts/benchmark/m037-rehearsal-<stamp>.json \
  --code-revision "$(git rev-parse HEAD)" --sdk "$(dotnet --version)" \
  --readiness-probes 2
```

`rehearse` refuses to run when any provider credential environment is
present (exit `3`, blocked, zero dispatches). `--concurrency` and
`--target-ms` default to the manifest values and must match them when given:
rescaling or retargeting needs a reviewed manifest, never a flag.

Exit codes: `0` complete (24/24 measured, composition audit passed),
`1` measurement or audit failure (the report is still written with
`status: failed` and retained violations), `2` usage error, `3` blocked —
credential environment present, zero dispatches made.

## Inputs

- `Manifest/m037-rehearsal-24.json` (schema `benchmark-manifest.v1`): the
  declared 24-request composition — 2 translation directions x 3 length
  bands x 2 per cell plus 2 rewriting language/mode cells x 3 bands x 2 per
  cell, exact canonical lengths, automatic/manual source selection, both
  Chinese input scripts, the recorded interleave seed, and the per-rule
  mapping to the full workload or an explicit successor-only deferral.
- `Composition/full-360-rules.json` (schema `benchmark-rules.v1`): the
  normative section 6.1 rules the harness enforces for the successor live
  workload. The report fails to build when a rule has neither a rehearsal
  analogue nor a deferral.

Sources are synthesized deterministically from the manifest base texts to
exact canonical (Unicode-scalar) lengths. Every repeat reuses its combo
source text with a fresh UUIDv7 submission identity minted at run time.

## Outputs

- `stdout`: the canonical camelCase rehearsal report
  (`kind: benchmark_rehearsal_report`, schema `benchmark-report.v1`).
- Report file: the same document at `--output` (the script uses
  `artifacts/benchmark/m037-rehearsal-<utc-stamp>.json`); the path is echoed
  on stderr.

The report carries deployment/endpoint/provider identity (credentialRef +
presence only, zero paid dispatches with basis), configuration, readiness
probes kept separate from measured requests, per-request outcomes with
submission-to-complete-body durations and cohorts, family aggregations with
full-denominator success-within-target plus nearest-rank p50/p95,
first-seen/repeated and hit/miss/unknown cache cohorts, spend/exposure
totals, the reuse-disabled assertion, the section 6.1 rule mapping, and
limitations. Per-request stage durations stay explicitly unknown with
provenance: the synchronous operation API exposes no per-stage timing
boundary, so inventing partitions would be fabrication. Per-request records
carry hashes, lengths, counts and snapshots only — never source text, result
text, tokens or secrets.

## How to review

1. Confirm the boundary: `deployment.kind` is the owned loopback host,
   `credentialPresent` is false with zero paid dispatches, and every
   observation has an independent submission clock (no TestServer, runner,
   or mocked timing).
2. Check the audit: `status` is `complete` with empty `auditViolations`;
   `observations` holds 24 entries over 12 combos x original-plus-repeat
   with 24 unique submission identities.
3. Audit the math: `successWithinTargetRate` divides by the full measured
   denominator; failures never count as timely; p50/p95 are nearest rank.
4. Check the cohorts: first-seen/repeated split 12/12; cache stays
   `unknown` unless the run observed a real cache signal.
5. Keep boundaries: this rehearsal proves harness recording fidelity only.
   It claims no NFR-002 compliance, discharges no part of G2, and never
   substitutes for the 360-request live workload or section 6.2 evidence.
