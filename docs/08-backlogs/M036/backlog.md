# M036 — Evaluate one candidate batch
Status: selected
Milestone: [M036 — Evaluate one candidate batch](../../07-roadmap.md#46-bounded-evidence-and-release-preparation)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-036 | One budgeted live run of frozen B1 (24 cases) through `DeepSeek-V4.1-Flash` with reproducible observations, pinned-judge grades and explicit human review disposition, labeled `live_qualification` without fixture/live confusion | Must | M019/M020/M035 Done (B1 frozen `aebdc2dc…` under owner waiver — ground truth weakened, consumed knowingly); owner-admitted live budget; bilingual/fluent output reviewers | selected | [spec](spec.md) |

Acceptance summary: frozen B1 revision consumed byte-identical; all 24 cases run live primary-only through the production pipeline under an explicit dispatch/spend/deadline budget with `live_qualification` rows; deterministic findings plus pinned-judge AI grades; seeded human sample plus every flag reviewed with recorded disposition; versioned §7.2 report with no secrets or production text; failures retained and G1/successor gap explicit; detailed ACs are in spec.md.
Human steps: owner admits the live budget (dispatches/spend/deadline, timing: before the live run, blocked gate: T002); bilingual/fluent reviewers plus adjudicator review outputs and record disposition (timing: after grading, blocked gate: T004/report); owner confirms the weakened-ground-truth limitation is acceptable for this run (timing: before T002, blocked gate: live run).
