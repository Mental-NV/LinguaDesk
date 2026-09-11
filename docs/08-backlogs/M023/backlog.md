# M023 — Bound paid-attempt exposure
Status: selected
Milestone: [M023 — Bound paid-attempt exposure](../../07-roadmap.md#44-shared-allowance-cost-and-recovery-behavior) ([current delivery](../../delivery/current.md))

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-023 | Every paid attempt reserves finite monetary exposure against a configured monthly ceiling before any dispatch; denied capacity, unknown spend and reconciliation cannot bypass the ceiling | Must | M021 Done (atomic admission, UTC-day ledgers, identity rules); M022 Done (deterministic settlement, conditional transitions, restart fencing) | selected | [spec](spec.md) |

Acceptance summary: per-attempt cost admission reserves a finite conservative upper bound stamped with its own cost-admission UTC month; admission enforces known spend + unresolved exposure + new bound against the configured cap atomically, denying with the monetary-suspension category when exceeded; unknown price or unbounded attempts are ineligible with no reservation; concurrent ceiling races commit within the cap; reconciliation settles exposure to known spend exactly once on authoritative evidence and retains the full reservation when evidence is missing; unresolved prior-month exposure carries into new-month admission; ledgers survive restart; monetary values stay private; detailed ACs are in spec.md.
Human steps: None required.
