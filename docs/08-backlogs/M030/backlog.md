# M030 — Keep editing across competing events
Status: selected
Milestone: [M030 — Keep editing across competing events](../../07-roadmap.md#45-useful-end-to-end-product-increments)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-030 | A verified user keeps editing across competing events on both workspaces: cross-page navigation preserves each page's text/settings/result, stale callbacks never touch the visible page, and discarded successful work still updates displayed usage | Must (first M030–M032 hardening slice; guards both M028/M029 journeys) | M029 Done (both workspaces, guarded routes, seed/fixture harness); no blocker | selected | [spec](spec.md) |

Acceptance summary: dedicated-user published E2E cases plus focused checks prove cross-page source/settings/result edits defeat stale callbacks while the UI preserves current text and successful discarded work still updates displayed usage. Detailed ACs are in spec.md.
Human steps: None required. Deterministic fake provider only; no live credentials, paid calls, manual AT/device review, or seeded production data. Full browser/device/AT verification is deferred to M038/M039 and G3.
