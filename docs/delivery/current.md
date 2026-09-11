# Current delivery status

Mutable #7 status owner. Read for selection/dependency checks and closeout; do not include in the stable execution prefix.

**Most recently completed milestone: [M032 — End and reset the workspace safely](../08-backlogs/M032/backlog.md).** State: **Done**. [Package 032](../08-backlogs/M032/spec.md) AC-001–009 passed with dedicated-user published E2E cases plus focused checks: inline `Start new workspace` on both pages opens the exact §3.4 native dialog with Cancel focus (Escape/Cancel preserves state and returns trigger focus), while empty workspaces reset immediately with no dialog; confirm clears both pages to FR-038 defaults, navigates to `/translate`, focuses Source text and submits nothing; real reload, full-document navigation, restoration and a second tab all open empty with no text in storage, URL/history payloads or caches; sign-out and session-expiry teardown clear lifted text before the login render with MSG-037, and Back exposes no old text; late responses restore no text while independently settled usage still reconciles; at least one case signs in through the real login form and the rest reuse the documented real-auth fixture; the footer states the workspace lifetime with only the user's own usage displayed; and the M026/M027 wire shapes are reused with zero OpenAPI/TypeScript drift. Its [completion record](../08-backlogs/M032/tasks.md#completion-record) owns the exact evidence. Only deterministic fake providers dispatch; live serving/quality/performance do not exist yet; this slice is not serving.

## Selected milestone

| ID | State | Dependency/readiness | Package |
| --- | --- | --- | --- |
| — | None | Select and lock the next milestone before implementation | — |

**Next recommended delivery train:** M032 → M033. M028/M029 delivered both web workspaces under the dedicated-user contract, M030 hardened them for competing events, M031 made recovery from unavailable or unknown outcomes visible and M032 made ending the workspace safe; deliver M033 next for the independent API-use demonstration, unless a recorded blocker requires another eligible choice. At least one published E2E case must sign in through the visible login form; other cases may use only #6's real-auth fixture.

M019 completed the independent-track enabler for the explicit live provider path required by M015–M018. The bounded access check reached `DeepSeek-V4.1-Flash` with one dispatch through the configured shared credential and returned `deepseek-flash` with known usage and no unresolved reservation; no credential value entered diagnostics or evidence. The same actual key may serve every LinguaDesk operation routed to DeepSeek. Quality, performance, context-limit and production-serving qualification remain unverified; D-19's route-specific providers and longer fallback chains remain later DF-004 work.

## Completed milestones

| ID | State | Acceptance and evidence |
| --- | --- | --- |
| M001 | Done | [Backlog](../08-backlogs/M001/backlog.md), [spec](../08-backlogs/M001/spec.md), [tasks/evidence](../08-backlogs/M001/tasks.md#3-completion-record) |
| M002 | Done | [Backlog](../08-backlogs/M002/backlog.md), [spec](../08-backlogs/M002/spec.md), [tasks/evidence](../08-backlogs/M002/tasks.md#3-completion-record) |
| M003 | Done | [Backlog](../08-backlogs/M003/backlog.md), [spec](../08-backlogs/M003/spec.md), [tasks/evidence](../08-backlogs/M003/tasks.md#3-completion-record) |
| M004 | Done | [Backlog](../08-backlogs/M004/backlog.md), [spec](../08-backlogs/M004/spec.md), [tasks/evidence](../08-backlogs/M004/tasks.md#3-completion-record) |
| M005 | Done | [Backlog](../08-backlogs/M005/backlog.md), [spec](../08-backlogs/M005/spec.md), [tasks/evidence](../08-backlogs/M005/tasks.md#3-completion-record) |
| M006 | Done | [Backlog](../08-backlogs/M006/backlog.md), [spec](../08-backlogs/M006/spec.md), [tasks/evidence](../08-backlogs/M006/tasks.md#3-completion-record) |
| M007 | Done | [Backlog](../08-backlogs/M007/backlog.md), [spec](../08-backlogs/M007/spec.md), [tasks/evidence](../08-backlogs/M007/tasks.md#completion-record) |
| M008 | Done | [Backlog](../08-backlogs/M008/backlog.md), [spec](../08-backlogs/M008/spec.md), [tasks/evidence](../08-backlogs/M008/tasks.md#completion-record) |
| M009 | Done | [Backlog](../08-backlogs/M009/backlog.md), [spec](../08-backlogs/M009/spec.md), [tasks/evidence](../08-backlogs/M009/tasks.md#completion-record) |
| M010 | Done | [Backlog](../08-backlogs/M010/backlog.md), [spec](../08-backlogs/M010/spec.md), [tasks/evidence](../08-backlogs/M010/tasks.md#completion-record) |
| M011 | Done | [Backlog](../08-backlogs/M011/backlog.md), [spec](../08-backlogs/M011/spec.md), [tasks/evidence](../08-backlogs/M011/tasks.md#completion-record) |
| M012 | Done | [Backlog](../08-backlogs/M012/backlog.md), [spec](../08-backlogs/M012/spec.md), [tasks/evidence](../08-backlogs/M012/tasks.md#completion-record) |
| M013 | Done | [Backlog](../08-backlogs/M013/backlog.md), [spec](../08-backlogs/M013/spec.md), [tasks/evidence](../08-backlogs/M013/tasks.md#completion-record) |
| M014 | Done | [Backlog](../08-backlogs/M014/backlog.md), [spec](../08-backlogs/M014/spec.md), [tasks/evidence](../08-backlogs/M014/tasks.md#completion-record) |
| M019 | Done | [Backlog](../08-backlogs/M019/backlog.md), [spec](../08-backlogs/M019/spec.md), [tasks/evidence](../08-backlogs/M019/tasks.md#completion-record) |
| M015 | Done | [Backlog](../08-backlogs/M015/backlog.md), [spec](../08-backlogs/M015/spec.md), [tasks/evidence](../08-backlogs/M015/tasks.md#completion-record) |
| M016 | Done | [Backlog](../08-backlogs/M016/backlog.md), [spec](../08-backlogs/M016/spec.md), [tasks/evidence](../08-backlogs/M016/tasks.md#completion-record) |
| M017 | Done | [Backlog](../08-backlogs/M017/backlog.md), [spec](../08-backlogs/M017/spec.md), [tasks/evidence](../08-backlogs/M017/tasks.md#completion-record) |
| M018 | Done | [Backlog](../08-backlogs/M018/backlog.md), [spec](../08-backlogs/M018/spec.md), [tasks/evidence](../08-backlogs/M018/tasks.md#completion-record) |
| M020 | Done | [Backlog](../08-backlogs/M020/backlog.md), [spec](../08-backlogs/M020/spec.md), [tasks/evidence](../08-backlogs/M020/tasks.md#completion-record) |
| M021 | Done | [Backlog](../08-backlogs/M021/backlog.md), [spec](../08-backlogs/M021/spec.md), [tasks/evidence](../08-backlogs/M021/tasks.md#completion-record) |
| M022 | Done | [Backlog](../08-backlogs/M022/backlog.md), [spec](../08-backlogs/M022/spec.md), [tasks/evidence](../08-backlogs/M022/tasks.md#completion-record) |
| M023 | Done | [Backlog](../08-backlogs/M023/backlog.md), [spec](../08-backlogs/M023/spec.md), [tasks/evidence](../08-backlogs/M023/tasks.md#completion-record) |
| M024 | Done | [Backlog](../08-backlogs/M024/backlog.md), [spec](../08-backlogs/M024/spec.md), [tasks/evidence](../08-backlogs/M024/tasks.md#completion-record) |
| M025 | Done | [Backlog](../08-backlogs/M025/backlog.md), [spec](../08-backlogs/M025/spec.md), [tasks/evidence](../08-backlogs/M025/tasks.md#completion-record) |
| M026 | Done | [Backlog](../08-backlogs/M026/backlog.md), [spec](../08-backlogs/M026/spec.md), [tasks/evidence](../08-backlogs/M026/tasks.md#completion-record) |
| M028 | Done | [Backlog](../08-backlogs/M028/backlog.md), [spec](../08-backlogs/M028/spec.md), [tasks/evidence](../08-backlogs/M028/tasks.md#completion-record) |
| M027 | Done | [Backlog](../08-backlogs/M027/backlog.md), [spec](../08-backlogs/M027/spec.md), [tasks/evidence](../08-backlogs/M027/tasks.md#completion-record) |
| M029 | Done | [Backlog](../08-backlogs/M029/backlog.md), [spec](../08-backlogs/M029/spec.md), [tasks/evidence](../08-backlogs/M029/tasks.md#completion-record) |
| M030 | Done | [Backlog](../08-backlogs/M030/backlog.md), [spec](../08-backlogs/M030/spec.md), [tasks/evidence](../08-backlogs/M030/tasks.md#completion-record) |
| M031 | Done | [Backlog](../08-backlogs/M031/backlog.md), [spec](../08-backlogs/M031/spec.md), [tasks/evidence](../08-backlogs/M031/tasks.md#completion-record) |
| M032 | Done | [Backlog](../08-backlogs/M032/backlog.md), [spec](../08-backlogs/M032/spec.md), [tasks/evidence](../08-backlogs/M032/tasks.md#completion-record) |

All roadmap milestones other than completed M001–M031 are Candidate with no selected package and evidence pending. Full product and release evidence remains pending in #6 coverage.
