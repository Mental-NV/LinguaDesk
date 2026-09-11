# M031 — Recover visibly from unavailable or unknown outcomes
Status: selected
Milestone: [M031](../../07-roadmap.md#45-useful-end-to-end-product-increments)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-031 | A verified user distinguishes definitive failure, unavailable usage and unknown outcome in the `/translate` and `/rewrite` workspaces; explicit Check status / Try again / Refresh usage actions read or resubmit only as specified and never cause unintended language work | Next (after M030) | M030, M024, M025 Done — no blocker | Selected | [spec](spec.md) |

Acceptance summary: dedicated-user published E2E cases plus focused checks prove the three recovery kinds are visually distinct on both pages, Check status resolves the original identity read-only, Try again always uses a new operation key, and Refresh usage sends a usage read only; detailed ACs are in spec.md.
Human steps: None required.
