# M033 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: none (M026/M027 Done per delivery/current.md).
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T001 — Confirm M026/M027 dependency evidence and current wire surface (operations/status/usage routes, generated YAML/types); AC-005 baseline; depends on none; done when routes and committed artifacts are identified with no behavior change.
- [ ] T002 — Scaffold `samples/api-consumer/` (plain-fetch Node script using generated types only, README, token-from-environment with redaction); AC-001/AC-002/AC-006; depends on T001; done when the sample completes one translation and one rewrite with snapshots against an isolated host, no React executed.
- [ ] T003 — Extend the sample run with recovery and classified-failure demonstrations (replay metadata, duplicate/conflict, usage read, 4xx input + 401 with categories); AC-003/AC-004/AC-006; depends on T002; done when the run output shows each case distinctly from success.
- [ ] T004 — Regenerate and review contracts (`generate` + `check`), record per-shape semantic agreement of touched envelopes/codes/categories with observed runtime behavior; AC-005; depends on T003; done when `check` is clean and the review record is linked.
- [ ] T005 — Run regression (`bash scripts/backend.sh`), refresh `python3 automation/context.py check M033`, write the completion record; all ACs; depends on T004; done when suites pass and the manifest is fresh.

## Completion record
Pending. Record per AC/task: revision, configuration, actual
command/procedure, environment, UTC timestamp, result/evidence
link, failures/fixes and limitations. No pasted command logs.
