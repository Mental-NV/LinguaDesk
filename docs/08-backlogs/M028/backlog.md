# M028 — Translate text in the web workspace
Status: selected
Milestone: [M028 — Translate text in the web workspace](../../07-roadmap.md#45-useful-end-to-end-product-increments)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-028 | A verified user translates text in the published SPA at `/translate` through the real cookie-auth → API → accounting → deterministic-provider path, with browser-visible evidence for one complete result, authoritative usage, and preservation/no-charge behavior for invalid, oversized and controlled-failure cases | Must (first end-user language outcome; gates M029) | M013 Done (guarded `/translate` route, sign-in form, verified session); M026 Done (translation API, accounting/recovery wire, generated contracts); no blocker | Selected | [spec](spec.md) |

Acceptance summary: predefined verified E2E user opens `/translate`, selects a valid source/target, enters text, waits without causing a request, activates **Translate** once, receives one complete editable/copyable result and sees authoritative usage; edits/copies the result with no new operation; oversize/invalid input and a controlled definitive failure preserve source and prior result with no successful-operation charge; at least one published case authenticates through the real login form. Detailed ACs are in spec.md.
Human steps: None required. Deterministic fake provider only; no live credentials, paid calls, manual AT/device review, or seeded production data. Human verification (full browser/device/AT) is deferred to M038/M039 and G3.
