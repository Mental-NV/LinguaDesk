# Current delivery status

Mutable #7 status owner. Read for selection/dependency checks and closeout; do not include in the stable execution prefix.

**Most recently completed milestone: [M046 — Expanded Translator and Write languages](../08-backlogs/M046/backlog.md).** State: Done with accepted accuracy exceptions, closed at the user’s direction on 2026-10-26. The 124-language catalog, direct Translation pairs, scoped RTL and language-aware alternatives are implemented. Local checks pass: 659 backend, 238 frontend, 110 published Chromium. The selected live sample records **92 passed, eight failed, zero skipped**; accuracy improvements are deferred, and the original all-usable quality gate remains failed. [Final case results](../08-backlogs/M046/case-results.md) and [tasks/evidence](../08-backlogs/M046/tasks.md#user-directed-closure--2026-10-26).

## Selected milestone

**[M047 — Azure App Service delivery](../08-backlogs/M047/backlog.md)** remains in implementation. User direction on 2026-10-30 retires Staging: all PRs run offline CI only, and checked pushes to `main` deploy the sole Production Linux F1 Web App in Sweden Central. Production retains its resource names, hostname, identity, persistent SQLite, Bicep/bootstrap, OIDC and capped monitoring. The [operator guide](../deployment/azure-app-service.md) describes this setup; [tasks](../08-backlogs/M047/tasks.md) record retirement and verification evidence. Hosted Production readiness, storage/recovery and marker qualification remain pending; M047 is not complete. M046 remains closed with eight accepted accuracy exceptions and no new paid run or release qualification. [Live review](../08-backlogs/M046/live-review.md) retains its evidence.

**D-22 policy:** required checks may run unattended; optional physical-device/native-IME/AT/additional-browser observations remain unclaimed. M045's final live batch passed after preserved failed prompt revisions; total conservative exposure including unresolved calls was USD 0.2716808 under the authorized quiescent scope.

M019 completed the independent-track enabler for the explicit live provider path required by M015–M018. The bounded access check reached `DeepSeek-V4.1-Flash` with one dispatch through the configured shared credential and returned `deepseek-flash` with known usage and no unresolved reservation; no credential value entered diagnostics or evidence. The same actual key may serve every LinguaDesk operation routed to DeepSeek. Quality, performance, context-limit and production-serving qualification remain unverified; D-19's route-specific providers and longer fallback chains remain later DF-004 work.

## Completed milestones

| ID | State | Acceptance and evidence |
| --- | --- | --- |
| M046 | Done with accepted accuracy exceptions | [Backlog](../08-backlogs/M046/backlog.md), [spec amendment](../08-backlogs/M046/spec.md#closure-amendment), [case results](../08-backlogs/M046/case-results.md), [tasks/evidence](../08-backlogs/M046/tasks.md#user-directed-closure--2026-10-26) |
| M045 | Done | [Backlog](../08-backlogs/M045/backlog.md), [spec](../08-backlogs/M045/spec.md), [tasks/evidence](../08-backlogs/M045/tasks.md#completion-record) |
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
| M033 | Done | [Backlog](../08-backlogs/M033/backlog.md), [spec](../08-backlogs/M033/spec.md), [tasks/evidence](../08-backlogs/M033/tasks.md#completion-record) |
| M034 | Done | [Backlog](../08-backlogs/M034/backlog.md), [spec](../08-backlogs/M034/spec.md), [tasks/evidence](../08-backlogs/M034/tasks.md#completion-record) |
| M043 | Done | [Backlog](../08-backlogs/M043/backlog.md), [spec](../08-backlogs/M043/spec.md), [tasks/evidence](../08-backlogs/M043/tasks.md#completion-record) |
| M037 | Done | [Backlog](../08-backlogs/M037/backlog.md), [spec](../08-backlogs/M037/spec.md), [tasks/evidence](../08-backlogs/M037/tasks.md#completion-record) |
| M035 | Done under historical owner waiver | [Backlog](../08-backlogs/M035/backlog.md), [spec](../08-backlogs/M035/spec.md), [tasks/evidence](../08-backlogs/M035/tasks.md) |
| M036 | Done (bounded batch only) | [Backlog](../08-backlogs/M036/backlog.md), [spec](../08-backlogs/M036/spec.md), [dated task completion entries](../08-backlogs/M036/tasks.md#ordered-tasks) |
| M039 | Done (bounded current Chromium narrow group) | [Backlog](../08-backlogs/M039/backlog.md), [spec](../08-backlogs/M039/spec.md), [tasks/evidence](../08-backlogs/M039/tasks.md#completion--2026-10-25-1002-utc) |
| M044 | Done (current D-21 workspace and desktop/responsive regression) | [Backlog](../08-backlogs/M044/backlog.md), [spec](../08-backlogs/M044/spec.md), [tasks/evidence](../08-backlogs/M044/tasks.md#completion--2026-10-25-0950-utc) |
| M038 | Done (historical UI contract; D-21-affected evidence superseded) | [Backlog](../08-backlogs/M038/backlog.md), [spec](../08-backlogs/M038/spec.md), [final evidence](../08-backlogs/M038/tasks.md#completion-record-final--t005-sign-off-2026-09-16-utc) |

M001–M039, M043 and M044 have completed scoped records (M035 under its historical waiver). M040–M042 remain Candidate with no selected package. M044 closes its selected historical D-21 UI scope; remaining full-corpus/live-measurement work requires fresh selection; full product and release evidence remains pending in #6 coverage.
