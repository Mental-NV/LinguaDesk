# M017 — Rewrite with one requested mode
Status: done; all selected ACs passed with offline and bounded live evidence (see [tasks](tasks.md#completion-record))
Milestone: [M017 — Rewrite with one requested mode](../../07-roadmap.md#43-independently-testable-language-behavior) ([current delivery](../../delivery/current.md))

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-017 | Scripted edge cases and at least one reviewed live development case in every language/mode cell exercise correction and the exclusive 9-mode catalog through the same prompt/pipeline, with failures retained as evidence | Must | M015 Done (eligibility pipeline, strict-envelope parser pattern, budgeted runner pattern); M019 Done (adapter, credential resolver, evaluation budget); M016 Done (translation pipeline pattern reused, not re-proven) | implemented | [spec](spec.md) |

Acceptance summary: strict `rewriting.v1` result-envelope parsing; a `RewritingPipeline` that reuses M015 eligibility, validates exactly one catalog mode, issues one transformation dispatch and returns validated same-language plain text (Simplified Chinese output, correction always applied); deterministic rejection of refusal/invalid output with no salvage or retry; a bounded reviewed live development slice covering all 36 language/mode cells with failures retained; detailed ACs are in spec.md.
Human steps: executor ran the bounded live slice under an explicit finite budget; human reviews the sanitized report at handoff (mode-behavior and Simplified-output spot review). No other human input required.
