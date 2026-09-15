# M037 — Make API performance measurable
Status: implemented
Milestone: [M037 — Make API performance measurable](../../07-roadmap.md#46-bounded-evidence-and-release-preparation)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-037 | A bounded workload rehearsal proves the real-API benchmark harness records the required timings, denominators, counts and cost/cache distinctions | Must | M026, M027, M020 (all Done, evidence in delivery/current.md); no blockers | implemented | [spec](spec.md) |

Acceptance summary: a small unpaid rehearsal (24 requests at 4 concurrent
operations through an owned loopback Kestrel host with real auth, migrated
SQLite and the deterministic provider) exercises every §6.1 recording path —
submission-to-complete-body timings with stage durations, full-denominator
success-within-target counts with nearest-rank p50/p95, first-seen/repeated
and hit/miss/unknown cost/cache cohorts — and emits a versioned report with
all §6.1 fields; the full 360-request live workload and §6.2
maximum-length/fallback/deadline evidence remain successor work and G2 stays
open; detailed ACs are in spec.md.
Human steps: None required. Fake provider, isolated file-backed test
databases and existing local-account auth suffice; no live credentials,
paid budget, capacity reservation, manual review or device access is
involved. No indispensable human input at start; no pending live evidence
at end. The successor live workload will need an owner-admitted finite
paid-run budget with verified test accounts and capacity (blocked gate
then, not now).
