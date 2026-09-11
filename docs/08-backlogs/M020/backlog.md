# M020 — Produce trustworthy evaluation reports
Status: selected
Milestone: [M020 — Produce trustworthy evaluation reports](../../07-roadmap.md#43-independently-testable-language-behavior) ([current delivery](../../delivery/current.md))

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-020 | An evaluator can run the bounded fixture and live development batches and obtain one versioned report that distinguishes access, behavior, injected failures, missing credentials and unresolved cost without capturing secrets | Must | M018 Done (per-family pipelines, fault/fallback/deadline proof, live-slice pattern); M019 Done (registry, adapter, credential resolver, evaluation budget, access check); M015–M017 Done (development slices reused as batch input, not re-proven) | selected | [spec](spec.md) |

Acceptance summary: one runner command emits a versioned combined report over the bounded offline fixture batch plus the bounded live development batch (primary-only chains, explicit budget), with per-observation disposition labels, retained failures, blocked-on-missing-credential sections, conservative exposure accounting and no secrets; detailed ACs are in spec.md.
Human steps: executor runs the bounded live batch under an explicit finite budget (credential already staged via M019; no new access needed); human reviews the sanitized combined report at handoff (disposition labeling, retained-failure handling, exposure completeness). No other human input required.
