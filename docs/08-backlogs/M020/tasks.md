# M020 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: none (M018/M019 Done; credential already staged; no new access needed).
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T001 — Combined-report model in `backend/tools/LinguaDesk.Ai.Evaluation/` (versioned schema, manifest, disposition labels, aggregates, blocked markers); AC-005 (schema portion); depends on none; done when MSTest schema/aggregate cases pass under `bash scripts/ai.sh check`.
- [ ] T002 — Offline aggregation (`evaluate-report --offline` over the four slice paths, networking disabled, no credential read); AC-001; depends on T001; done when the offline combined run passes with zero dispatches and a validated report under `artifacts/evaluation/`.
- [ ] T003 — Live aggregation (`--live` primary-only budgeted run, access probe, blocked-without-credential path, exposure totals, secret-safety); AC-002/AC-003/AC-004; depends on T002; done when the live shape is proven (blocked without credential; budgeted run with retained failures and exposure totals) and the secret sweep passes.
- [ ] T004 — `scripts/ai.sh evaluate-report` surface (offline/live passthrough, offline credential stripping) plus operating-guide procedure; plan step 4; depends on T003; done when `bash scripts/ai.sh check` covers the new tests and the README documents the reviewed live procedure.
- [ ] T005 — Regressions (`backend.sh check`, `contract.sh check`, `context.py check M020`), bounded live development batch against `DeepSeek-V4.1-Flash`, sanitized-report human review with recorded disposition; all ACs; depends on T004 (offline portions may run earlier if the live gate is blocked); done when every AC holds with revision/environment/UTC time recorded below.

## Completion record
Pending — no runs yet. Record per AC/task: revision/configuration, actual command/procedure, environment, UTC timestamp, result/evidence link, failures/fixes and limitations. No pasted command logs.
