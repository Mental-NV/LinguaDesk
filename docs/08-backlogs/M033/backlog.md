# M033 — Demonstrate independent API use
Status: selected
Milestone: [M033 — Demonstrate independent API use](../../07-roadmap.md#45-useful-end-to-end-product-increments)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-033 | A small consumer example completes translation, rewriting and usage recovery through the authenticated API without the SPA, and the reviewed generated contracts agree with actual runtime semantics | Must | M026, M027 (both Done, evidence in delivery/current.md); no blockers | selected | [spec](spec.md) |

Acceptance summary: independent consumer authenticates with a local account Bearer token, completes one translation and one rewrite with a selected mode, reads usage snapshots, recovers operation outcomes through status reads, surfaces classified failures distinctly from success, and the regenerated OpenAPI plus TypeScript client show zero drift with a recorded semantic review. Detailed ACs are in spec.md.
Human steps: None required.
