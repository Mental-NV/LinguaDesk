# Documentation context refactor — verification record

Baseline: Git `c236952ad4149f586f2bf5e8f5774365be99da8c` (M006 implemented). Refactor checked 2026-09-09. [Research, design and usage](../../automation/context-guide.md) explain the bounded reading workflow and link official sources.

## Scope and measured size

| Reading surface | Before (words) | After (words) | Reduction |
| --- | ---: | ---: | ---: |
| Document #0 | 3,958 | 632 | 84.0% |
| Numbered entry documents #0–7 and #9 | 53,925 | 27,640 | 48.7% |

Counts use Python `len(text.split())` on the same nine paths at the baseline and after refactoring. These are entry-surface sizes, **not measured model token savings**. Supporting detail remains accessible in subject modules, archives and completed packages; the total stored Markdown volume is approximately unchanged after adding workflow/tooling guidance. The improvement is that routine execution no longer loads that whole corpus. Its default governance is #0 plus the 470-word execution procedure, followed by the selected excerpts and four package artifacts. `context.py measure <ID>` measures that actual combined packet after a real package is reviewed and locked.

No paid Codex milestone, API cache experiment or application release was run. Cache reuse depends on the rendered conversation and runtime settings; document reorganization alone cannot prove it.

## Integrity and tooling checks

- All 24 M001–M006 artifacts preserve historical text and evidence after normalizing migrated links and package-name labels. All four artifacts now share their stable-ID folder; the old separate tree is removed.
- Compared baseline and current catalogs after normalizing link destinations: all 77 PRD requirement/gate/proposal/question/deferred rows, 176 UX story/message/scenario rows, 15 LLM scenarios and 14 API scenarios are unchanged. No requirement or scenario disposition was silently dropped.
- `python3 automation/context.py audit`: local file/anchor links and stable-ID package layout pass.
- `python3 -m unittest discover -s automation/tests`: 16 tests pass. Coverage includes exact selection, child/sibling boundaries, missing/ambiguous sources, stale source/map/plan/governance detection, mutable task progress, on-demand exclusion, deterministic packet order, links, and isolated runner readiness/resume gates with a fake coding CLI.
- `bash -n automation/run-milestones.sh` and `git diff --check`: pass.

Historical packages have no retroactive execution locks; their evidence has not been recertified. M015 remains the recommended unselected candidate. The next planning run must review its complete source map and acceptance-to-verification coverage before reporting READY.
