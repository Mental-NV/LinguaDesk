# M018 — Bound failures and fallback
Status: done; all selected ACs passed with deterministic offline proof and a bounded live primary-only slice (see [tasks](tasks.md#completion-record))
Milestone: [M018 — Bound failures and fallback](../../07-roadmap.md#43-independently-testable-language-behavior) ([current delivery](../../delivery/current.md))

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-018 | Deterministic fault cases prove both family pipelines obey fallback, dispatch/admission bounds and the original deadline; a small live no-fallback path records the same bounds/metadata around natural provider calls without treating mocks as live evidence | Must | M016 Done (translation pipeline reused, not re-proven); M017 Done (rewriting pipeline reused, not re-proven); M015 Done (eligibility reuse); M019 Done (registry, adapter, credential resolver, evaluation budget); M004/M005 Done (inner loop, counting policy) | implemented | [spec](spec.md) |

Acceptance summary: validated per-family chains (one primary plus zero or one distinct fallback); deterministic traversal of both three-dispatch paths with eligibility reuse in both families; Section 7 advance/stop mapping including correlated-credential skip; the original 30-second deadline enforced end to end with fake-time proof; every paid dispatch budget-admitted with conservative exposure; a small primary-only live slice recording bounds/metadata; detailed ACs are in spec.md.
Human steps: executor runs the bounded live slice under an explicit finite budget (credential already staged via M019; no new access needed); human reviews the sanitized report at handoff (fallback-invisibility spot check does not apply — the live path is primary-only by design — review covers bounds/metadata completeness and retained-failure handling). No other human input required.
