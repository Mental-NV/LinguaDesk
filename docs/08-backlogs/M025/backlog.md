# M025 — Report ordered usage across period changes
Status: selected; package drafted for review and lock
Milestone: [M025 — Report ordered usage across period changes](../../07-roadmap.md#44-shared-allowance-cost-and-recovery-behavior) ([current delivery](../../delivery/current.md))

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-025 | Clients receive authoritative current-day availability/usage with a durable ordering revision; midnight and month changes preserve correct charge/exposure attribution and snapshot ordering | Must | M022 Done, M023 Done, M024 Done (admission-day settlement, cost-month attribution, terminal metadata + current-day snapshot precedent); no blockers | selected | [spec](spec.md) |

Acceptance summary: a verified-account usage read and every operation response carry an authoritative current-day snapshot with a categorical availability signal; cross-midnight settlement keeps the charge on the admission day while the fresh snapshot describes the new day; cross-month monetary carryover stays consistent with snapshot availability; snapshots order by UTC day then durable revision across restarts and JSON round-trips; reads never charge or dispatch; detailed ACs are in spec.md.
Human steps: None required.
