# M039 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief
Reuse the M044 current workspace and seven reviewed screenshots. Add one focused published narrow group with visible account/core outcomes through real auth/API/migrated SQLite and existing deterministic providers. Extract the existing semantic/contrast scanner for shared desktop/narrow use; include native editor text/control boundaries and empty-result placeholder checks.

## Changes and order
1. T001: Review M044 completion, current owners and selected narrow assertions; lock package.
2. T002: Extract tests/e2e/accessibility.ts, add narrow-workspace.spec.ts using real form login, pause clock, result/native-copy, failure/retry/expiry and usage reads. Add narrow registration and effective-zoom/text-spacing geometry. Add a cancellable 750 ms M039-DELAY fixture only to the existing Smoke providers for real network-interruption/status recovery, with cancellation tests. Fix the two discovered product defects: preserve the initial safe protected destination on signed-out bootstrap (while fencing old auth reads), and avoid an enabled-text contrast dip on control-color transitions.
3. T003: Run frontend check and full published smoke, enforcing existing seven baselines. Inspect any discrepancy rather than regenerating. Record exact environment, outcome/limitations and update delivery/coverage.

## Verification map
| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| Source readiness | T001 | python3 automation/context.py lock M039; python3 automation/context.py check M039 | context.json |
| AC-001–004 | T002/T003 | bash scripts/frontend.sh smoke; focused narrow-workspace.spec.ts through the owned published host | artifacts/test-results/frontend-e2e.xml |
| AC-003/004 semantics | T002/T003 | shared executable label/name/contrast/control-boundary/placeholder scanner and browser geometry | same published report |
| Smoke adapter cancellation | T002/T003 | bash scripts/backend.sh check (including the two new cancellation cases) | artifacts/test-results/backend-api.trx |
| AC-005 | T003 | bash scripts/frontend.sh check; bash scripts/frontend.sh smoke with no update flag; git diff --check | frontend-unit.xml; frontend-e2e.xml |

## Context boundaries and risks
Dependencies are evidenced by M044's current code/completion and existing seed/account/provider harness. The baseline backend 614-case result remains scoped evidence; rerun the backend gate because the Smoke-only adapters gain the delay fixture. No paid/production-provider, backup/retention, real-email or API design changes occur. Focus/scroll behavior, native copy, font stabilization, clock-controlled browser timing and page context isolation are risks; real browser checks provide evidence at the sufficient layer. Runtime permissions may require authorized owned-loopback/cache access. Additional platforms and human observations remain optional and unclaimed. No rollout/migration applies; rollback is a source revert.
