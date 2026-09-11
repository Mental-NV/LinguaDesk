# M022 — Settle a result exactly once
Status: done; all selected ACs passed with deterministic settlement + file-backed SQLite/HTTP evidence and zero provider dispatches (see [tasks](tasks.md#completion-record))
Milestone: [M022 — Settle a result exactly once](../../07-roadmap.md#44-shared-allowance-cost-and-recovery-behavior) ([current delivery](../../delivery/current.md))

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-022 | A reserved logical operation settles deterministically: success commits exactly one full-source character charge to its admission UTC day, definitive failure releases the reservation with zero charge, and duplicate or late settlement cannot charge twice | Must | M021 Done (pending reservations, UTC-day ledgers, revision, identity/matching rules) | implemented | [spec](spec.md) |

Acceptance summary: pending reservations settle to `succeeded` (one full scalar charge committed to the stored admission day) or `failed` (reservation released, zero charge) through conditional state transitions; duplicate same-outcome settlement is idempotent, late cross-outcome settlement is fenced with no ledger change, concurrent settlements commit once, settled state and ledgers survive restart, and status reads report terminal metadata without dispatching work or storing source text; detailed ACs are in spec.md.
Human steps: None required.
