# M046 — Expanded Translator and Write languages

Status: In progress. Local implementation and aggregate regression verification passed; the required live-development matrix is blocked by automatic approval review pending explicit provider destination/payload approval. No paid M046 request was dispatched.
Milestone: [M046 roadmap](../../07-roadmap.md#45-useful-end-to-end-product-increments).

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-001 | A verified user translates and improves writing in the 124-language catalog through the existing workspace, including RTL text and sentence alternatives | P1 | M045, M019, M020, M043 and transitive auth/accounting foundations Done; live gate needs usable profile/credential, finite budget and shared-scope record | In progress | [spec](spec.md), [plan](plan.md), [tasks](tasks.md) |

Acceptance summary: all catalog entries participate in both features; existing operation semantics and old language codes remain; common Translation template plus lightweight language smokes replaces exhaustive live pair evaluation. Native editing/copy and Write review/alternatives work across selected scripts. Detailed ACs live only in spec.

External prerequisites: none for planning/local implementation. Before T007, the authorized runner verifies existing live authorization, credentials, current finite remaining dispatch/spend capacity and shared-provider/quiescent scope; no secret values enter evidence. Missing inputs block live completion, not independent local tasks. Reuse valid standing authorization without repeated approval requests. Release qualification and deployment are separate work.

Optional human checks: physical-device/native-IME/AT/additional-browser observations are nonblocking under D-22.
