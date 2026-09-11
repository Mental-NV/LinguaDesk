# M029 — Rewrite text in the web workspace
Status: selected
Milestone: [M029 — Rewrite text in the web workspace](../../07-roadmap.md#45-useful-end-to-end-product-increments)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-029 | A verified user rewrites text in the published SPA at `/rewrite` through the real cookie-auth → API → accounting → deterministic-provider path, with browser-visible evidence for one complete same-language result, authoritative usage, and preservation/no-charge behavior for invalid, oversized and controlled-failure cases | Must (second end-user language outcome; completes the M027 → M029 rewriting pair) | M028 Done (workspace pattern, guarded routes, seed/fixture harness); M027 Done (rewriting API, accounting/recovery wire, generated contracts); no blocker | implemented | [spec](spec.md) |

Completion evidence: [tasks/evidence](tasks.md#completion-record). All eight
selected ACs passed with the published deterministic-provider journey; the
runner's full regression gate passed 2026-09-11.

Acceptance summary: predefined verified E2E user opens `/rewrite`, sees **Correction only** by default, keeps it or chooses one supported mode, enters text, waits without causing a request, activates **Rewrite** once, receives one complete same-language editable/copyable result with source unchanged and sees authoritative usage; edits/copies the result with no new operation; oversize/invalid input and a controlled definitive failure preserve source and prior result with no successful-operation charge; at least one published case authenticates through the real login form. Detailed ACs are in spec.md.
Human steps: None required. Deterministic fake provider only; no live credentials, paid calls, manual AT/device review, or seeded production data. Human verification (full browser/device/AT) is deferred to M038/M039 and G3.
