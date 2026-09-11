# M016 — Translate complete text through the AI boundary
Status: done; all selected ACs passed with offline and bounded live evidence (see [tasks](tasks.md#completion-record))
Milestone: [M016 — Translate complete text through the AI boundary](../../07-roadmap.md#43-independently-testable-language-behavior)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-016 | Shared pipeline translates eligible complete text deterministically and on a bounded live slice covering every Translation direction, retaining failures as evidence | P1 | M015 done, M004 done, M005 done, M019 done; owner credential staged (M019 evidence) | implemented | [spec](spec.md) |

Acceptance summary: scripted edge cases and at least one reviewed live development case in every Translation direction produce validated complete outcomes through the same prompt/pipeline, with failures retained as evidence; detailed ACs are in spec.md.
Human steps: executor ran the bounded live slice under an explicit finite budget; human reviews the sanitized report at handoff. No other human input required.
