# M026 — Translate through the authenticated API
Status: selected
Milestone: [M026 — Translate through the authenticated API](../../07-roadmap.md#45-useful-end-to-end-product-increments)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-026 | An independent verified client explicitly translates through the real host/accounting pipeline with a fake provider and receives complete output or a classified failure | Must | M009, M016, M018, M022, M023, M024, M025 (all Done, evidence in delivery/current.md); no blockers | selected | [spec](spec.md) |

Acceptance summary: verified Bearer [REDACTED] translation submit → complete validated output or classified failure; local validation with zero dispatch; eligibility rejection without transformation or character charge; exactly-once admission-day charge with duplicate/conflict semantics; bounded fallback, deadline and per-attempt monetary admission; fenced recovery with dispatch-free status reads; reviewed generated-contract delta with no text/secret leakage. Detailed ACs are in spec.md.
Human steps: None required. Fake provider, isolated file-backed test databases and existing local-account auth suffice; no live credentials, email delivery, manual review or paid serving is involved. No indispensable human input at start; no pending live evidence at end.
