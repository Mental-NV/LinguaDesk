# M043 — Serve live translation/rewriting in the portal with bounded spend
Status: implemented
Milestone: [M043 — Serve live translation/rewriting in the portal with bounded spend](../../07-roadmap.md#45-useful-end-to-end-product-increments)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-043 | A verified user completes Translate and Rewrite in the portal through the serving candidate under a $0.05-per-operation cap; `run` without serving configuration fails fast; opt-in env-gated live suites prove the portal path end-to-end against real LLM calls without entering default gates | Must | M026, M027, M028, M029, M019 Done (delivery/current.md; dependency specs as reference in context.json); Q-001 open, serving slice owner-locked by docs/research/m043-live-serving-proposal-2026-09-15.md | implemented | [spec](spec.md) |

Acceptance summary: live Translate and Rewrite succeed in the portal through primary-only DeepSeek-V4.1-Flash serving chains under the per-operation cap with honest failure semantics; unconfigured `run` refuses to start; env-gated live API + browser suites prove the real-call path and stay out of default gates; detailed ACs are in spec.md.
Human steps: owner holds the shared DeepSeek key (exported for M019; present in this environment). At execution end, a human (or owner-authorized runner) sets `LINGUADESK_E2E_LIVE=1` with the key present and runs the two live suites; this blocks only AC-005/AC-006 live evidence, nothing else. Bounded paid spend (single-digit dispatches, ≤ $1.00) is owner-authorized by the roadmap change note.
