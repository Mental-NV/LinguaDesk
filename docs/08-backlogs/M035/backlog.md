# M035 — Prepare one reviewed evaluation batch
Status: selected
Milestone: [M035 — Prepare one reviewed evaluation batch](../../07-roadmap.md#46-bounded-evidence-and-release-preparation)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-035 | First approved release-corpus batch (B1, 24 cases) with stable IDs, reference constraints and coverage tags, frozen and review-approved ready for the M036 evaluation run | Must | M020 Done (report workflow is the consumer contract, reused not re-proven); bilingual + independent reviewers (human gate at freeze) | selected | [spec](spec.md) |

Acceptance summary: batch B1 (12 Translation cases, one per directed pair; 12 Rewriting cases, three per language) is frozen as versioned JSON with §5.1 case fields, reference constraints and coverage tags, validated offline, disjoint from development cases, and approved by bilingual authors plus an independent reviewer per case; detailed ACs are in spec.md.
Human steps: bilingual authors prepare reference notes during execution; one independent reviewer per case approves at freeze (owner: evaluation reviewers, timing: end of execution, blocked gate: batch freeze/approval — M036 cannot consume an unapproved batch). No live/provider input required: offline data work only, no credential or spend.
