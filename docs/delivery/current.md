# Current delivery status

Mutable #7 status owner. Read for selection/dependency checks and closeout; do not include in the stable execution prefix.

**Most recently completed milestone: [M026 — Translate through the authenticated API](../08-backlogs/M026/backlog.md).** State: **Done**. [Package 026](../08-backlogs/M026/spec.md) AC-001–008 passed with deterministic fake-provider translation through the real host/accounting pipeline + file-backed SQLite/HTTP evidence: a verified Bearer [REDACTED] translation submit executes synchronously behind the stored absolute deadline and returns `201` complete validated text with one admission-day charge and a fresh current-day snapshot, or a classified `422`/`429`/`503`/`504` failure with zero charge; local validation and eligibility rejections dispatch nothing; duplicates replay succeeded/output-unavailable metadata with no new charge or dispatch and changed payloads conflict; transient/invalid/refusal faults exhaust the bounded chain (fallback receives the original source) and deadline expiry fences late success, each with zero charge and released monetary exposure; per-dispatch monetary admission denies over-cap or unconfigured dispatch with `monetarySuspension`; exhausted allowances deny with `429` reset guidance; settled translation state survives restart with dispatch-free reads; and the reviewed `TranslationSuccessResponse` (`201`/`504`) delta ships with zero OpenAPI/TypeScript drift and no source/result text, global counts, monetary values, credentials or provider internals in problems, logs, traces or TEXT columns. Its [completion record](../08-backlogs/M026/tasks.md#completion-record) owns the exact evidence. Only the deterministic fake provider dispatches; rewriting dispatch (M027), browser journeys (M028/M029) and live serving do not exist yet; this slice is not serving.

## Selected milestone

| ID | State | Dependency/readiness | Package |
| --- | --- | --- | --- |
| — | None | Select and lock the next milestone before implementation | — |

**Next recommended delivery train:** M026 → M028 → M027 → M029. Select M026 next as the bounded API enabler, then deliver M028 before beginning Rewriting unless a recorded blocker requires another eligible choice. M028 is the first milestone after M025 that establishes an end-user language outcome: the predefined verified E2E user can translate text in the published UI with the outcome asserted in-browser. M029 then establishes rewriting under the same dedicated-user contract. At least one published E2E case must sign in through the visible login form; other cases may use only #6's real-auth fixture. M026/M027 alone remain independent-client outcomes and do not satisfy either web journey.

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

All roadmap milestones other than completed M001–M026 are Candidate with no selected package and evidence pending. The M026 → M028 → M027 → M029 recommendation does not select or create a package; the next planning pass must lock M026 under #0. Full product and release evidence remains pending in #6 coverage.
