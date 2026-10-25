# M044 — Selected specification
Selected items: BI-001. Status: ready; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope
Implement D-21 and UX sections 3–10 on the existing protected Translation/Rewriting journeys. FR-011/014/016/022/023/028/038 and the affected UX acceptance govern. Existing auth/API/provider contracts remain; real email, assistance/diffs, custom controls and paid quality/performance qualification are excluded. Completed milestone packages remain historical.

## Acceptance
| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | Both protected routes expose the specified native selectors, visible editor h2 labels, always-present read-only empty/editable completed result, Clear source text, inline Sign out and footer; removed controls/status/count/usage are absent. | UX §4–5/7–8; UX-AC-021/038/065/078 |
| AC-002 | Exact workspace/column widths at 1440/768/390/320, equal desktop editor heights and aligned tops; correct selector placement, mobile reading order, heights/scrolling, no overflow. | UX §4.2; UX-AC-077/078 |
| AC-003 | Only the latest eligible edit dispatches after 1,000 ms; restart/cancel/IME/queue guards, one unsettled per feature and no unchanged-revision retry. | UX processing §6.1; UX-AC-022–025/039–046/096 |
| AC-004 | Source/settings/result edits and composition invalidate pending text; latest queued source waits for known terminal outcome; hidden settlement preserves visible focus, usage is ordered/shared and stale success discloses its charge/day. | UX §3/6; UX-AC-002–005/035/047/058–060/106 |
| AC-005 | Limits/same-language/allowance/offline and classified failures use essential alerts; unknown recovery reads original status, explicit retry uses a new identity, restored availability alone never submits. | UX §6–9; UX-AC-030/036/068–076/105/107–109 |
| AC-006 | Clear preserves result/focus and cancels work; native edit/copy is uncharged; sign-out/expiry/reload/full navigation/bfcache clear text/timers and reject callbacks. | UX §3/7–10; UX-AC-003/015/016/026/033/051/087 |
| AC-007 | Published real-auth/API/SQLite/provider-fixture journeys and semantic/contrast/reflow checks pass; seven curated Chromium baselines receive AI review; narrow/core geometry is checked without platform claims. | Verification frontend §4; D-22 |

## Constraints and decisions
No schema/database migration or production publication. Browser evidence uses the dedicated Smoke verified account through real local auth. API/database assertions prove accounting without restoring the usage UI. Required checks run unattended. Existing acceptance AC-006/100 is aligned at its owner with DF-008/native-copy behavior before implementation. Synthetic composition proves guards only. M039 remains the later dedicated narrow journey group; M044 includes responsive regression and all seven required initial baselines.
