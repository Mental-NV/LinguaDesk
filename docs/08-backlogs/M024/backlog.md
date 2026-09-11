# M024 — Recover an interrupted operation
Status: selected
Milestone: [M024 — Recover an interrupted operation](../../07-roadmap.md#44-shared-allowance-cost-and-recovery-behavior) ([current delivery](../../delivery/current.md))

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-024 | A client can inspect its original operation after disconnect/restart without regeneration, lost-charge claims or a late result reviving failure | Must | M022 Done (conditional settlement, fencing, restart durability); M023 Done (cost admission, exposure retention on missing evidence) | selected | [spec](spec.md) |

Acceptance summary: terminal `interrupted` recovery mapping over the M022/M023 state machine — restart-orphaned or deadline-passed pending operations finalize as interrupted with zero character charge and released reservation while retaining unresolved monetary exposure; same-payload status reads return interrupted metadata without redispatch; late success after interrupt and interrupt after success are fenced; unknown identities stay 404 without asserting zero charge; a disconnect may still settle success with output unavailable; state, ledgers and revision survive restart with zero-dispatch reads; detailed ACs are in spec.md.
Human steps: None required.
