# M043 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: none (dependencies Done; shared DeepSeek key present; live opt-in run deferred to the end).
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T001 — Serving options, validation and fail-fast `run`; AC-004; depends on none; done when options-validator tests pass and unconfigured/partial `run` exits non-zero naming missing variables.
- [ ] T002 — Serving providers + coordinator ceiling for Translate and Rewrite; AC-001/AC-002/AC-003; depends on T001; done when scripted-adapter tests prove 201 results, single full-length charges with recorded provider charges, and ceiling enforcement on both families.
- [ ] T003 — Deterministic failure-boundary tests (over-cap denial → 503/MSG-017 zero-charge no-fallback; at-cap admission; `Unavailable*` fallback retained when unconfigured; pending/202 semantics unchanged for genuinely pending work); AC-003/AC-004; depends on T002; done when focused tests pass.
- [ ] T004 — Live suites (`e2e-live` API script + Playwright live browser case) with opt-in gating, dispatch/spend caps and credentialRef-only reporting; AC-005/AC-006/AC-007; depends on T003; done when both suites pass under `LINGUADESK_E2E_LIVE=1` with the key present, skip without the flag, and default gates show no live invocation.
- [ ] T005 — Contract drift (`contract.sh check`), full default gates, context lock refresh and close; all ACs; depends on T004; done when `verify-milestone.sh M043`, fresh `context.py check M043` and `git diff --check` pass and the completion record below is filled.

## Completion record
Pending execution. No evidence yet.
