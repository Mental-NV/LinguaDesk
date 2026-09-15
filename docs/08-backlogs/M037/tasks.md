# M037 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: none.
Last check: not run. Changed scope: none (new package).

## Ordered tasks
- [ ] T001 — Benchmark tool (manifest loader, HTTP driver with N-in-flight discipline, composition auditor, §6.1 math + report writer, `backend.sh benchmark` surface); AC-002/AC-003/AC-004/AC-005; depends on none; done when denominator/percentile/cohort unit tests and per-violation-class auditor fixtures pass.
- [ ] T002 — Owned-host rehearsal (loopback Kestrel + migrated scratch SQLite + real login, 24-request manifest with recorded seed, credential env stripped, zero provider dispatches); AC-001; depends on T001; done when 24/24 requests are measured through the real HTTP boundary with drain disclosed.
- [ ] T003 — Versioned rehearsal report under `artifacts/benchmark/`, regressions (`backend.sh check`, `ai.sh check`, `context.py check M037`), secret/synthetic-text sweep; AC-006; depends on T002; done when all pass and the sweep is clean.

## Completion record
Not started. No human gates: rehearsal uses the deterministic provider,
isolated storage and local test accounts; the successor live workload's
owner-admitted budget, verified test accounts and capacity are explicitly
out of scope. Rehearsal success proves harness recording fidelity only —
it claims no NFR-002 compliance and discharges no part of G2.
