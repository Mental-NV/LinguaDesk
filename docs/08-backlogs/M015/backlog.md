# M015 — Validate language eligibility
Status: selected
Milestone: [M015 — Validate language eligibility](../../07-roadmap.md#43-independently-testable-language-behavior)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-015 | Shared pipeline validates language eligibility deterministically and on a bounded live slice, rejecting ineligible input without transformation | P1 | M004 done, M005 done, M019 done; owner credential staged (M019 evidence) | selected | [spec](spec.md) |

Acceptance summary: deterministic eligibility/zero-dispatch cases pass and a bounded, reviewed live development slice covers the supported languages and model-decided negative classes without transforming rejected input; detailed ACs are in spec.md.
Human steps: executor runs the bounded live slice under an explicit finite budget; human reviews the sanitized report at handoff (blocks AC-005 only). No other human input required.
