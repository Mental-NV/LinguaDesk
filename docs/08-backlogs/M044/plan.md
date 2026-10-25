# M044 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief
Separate workspace framing from account cards. Reuse native controls and existing wire operations. Lift a shared pause scheduler above routing, retain original operation identities, and gate text independently from accounting using generation/input/result revisions. Never infer cancellation or zero charge from an aborted wait.

## Changes and order
1. T001: Reconcile acceptance rows, add roadmap/package and review/lock exact sources.
2. T002: Shared workspace state/scheduler and transport in frontend/src/shell plus feature reducer/API mappings. Track per-feature timers/queues, composition and visibility synchronously; preflight checks recapture current fields and result revision at dispatch. Share ordered availability snapshots within the tab, clear account context at teardown, classify deadline/network/pending recovery.
3. T003: App.tsx workspace frame, shared native editor/settings presentation, shell.css tokens/responsive tracks. Remove reset dialog/custom copy/status UI and preserve lifetime teardown.
4. T004: Migrate affected unit/component/browser/live-client tests from button/usage assumptions. Add fake-timer/deferred-response tests for races/recovery and published geometry/native-copy/lifecycle checks. Retain unaffected auth/policy coverage. Pin deterministic captures and inspect all seven baselines; no API interception in published journeys.
5. T005: Run checks, review diff and visuals, record evidence, update delivery/coverage. Production rollout is out of scope; rollback is a source revert with no data migration.

## Verification map
| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| Source coherence | T001 | python3 automation/context.py lock M044; python3 automation/context.py check M044 | context.json |
| AC-003–006 | T002/T004 | bash scripts/frontend.sh check (V-001/002/003; fake timers/deferred transport) | artifacts/test-results/frontend-unit.xml |
| AC-001/002/006/007 | T003/T004 | bash scripts/frontend.sh smoke (V-010/011/012; real published auth/API/SQLite) | artifacts/test-results/frontend-e2e.xml |
| AC-004/005 accounting | T004 | existing API boundary tests via bash scripts/backend.sh check, plus published read-only usage assertions | artifacts/test-results |
| AC-007 visuals | T004/T005 | capture seven curated screenshots with pinned Chromium/fonts/scale; inspect before/after and verify independent geometry/semantics | frontend/tests/e2e/*-snapshots; tasks.md |
| Closeout | T005 | git diff --check; python3 automation/context.py check M044 | tasks.md |

## Context boundaries and risks
Existing standalone API accepts the required operations/metadata; extend client mappings only, never handwritten generated schemas. Open wire metadata if a mismatch emerges. No backend behavior, provider selection, mail delivery, paid measurements or operational migration changes are needed. Historical foundations exist in current source; their obsolete UI evidence is not a current pass. Timer races, React stale closures/duplicate effects, hidden-page work, shared usage ordering, unresolved delivery and bfcache are the primary risks and require focused tests. Installed local toolchains are available; browser sandbox/network issues must be recorded honestly, with authorized escalation only where necessary.
