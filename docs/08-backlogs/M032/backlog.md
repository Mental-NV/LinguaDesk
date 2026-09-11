# M032 — End and reset the workspace safely
Status: selected
Milestone: [M032](../../07-roadmap.md#45-useful-end-to-end-product-increments)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-032 | A verified user ends the workspace safely: explicit reset, sign-out/session-expiry teardown and real navigation/restoration all clear private text and restore visible defaults, while independently settled usage remains correct | Next (after M031) | M031 Done — no blocker | selected | [spec](spec.md) |

Acceptance summary: dedicated-user published E2E cases plus focused checks prove Start-new-workspace reset (dialog and empty bypass), immediate sign-out/expiry teardown, and real reload/navigation/bfcache restoration clear source/result/settings with no text in storage/history/cache, restore Correction-only/detection/empty-target defaults, submit nothing, fence late responses, and keep only independently settled usage; detailed ACs are in spec.md.
Human steps: None required.
