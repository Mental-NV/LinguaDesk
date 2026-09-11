# M032 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: none (M031 Done; see delivery/current.md).
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T001 — Add `workspaceCleared` reducer action in `translationWorkspace.ts` and `rewritingWorkspace.ts` (defaults restoration, usage-snapshot preservation, late-response fencing); AC-002/AC-005/AC-006; depends on none; done when V-002 pure units pass.
- [ ] T002 — Add store `resetAll` in `workspaceStores.ts` and wire sign-out/expiry/reset-confirm teardown through it (abort paid + read-only flights, clear saved scroll); AC-005/AC-006; depends on T001; done when store/fencing units pass.
- [ ] T003 — Build the §3.4 native reset dialog with inline `Start new workspace` on both pages, empty-workspace bypass, confirm clearing + `/translate` navigation + Source-text focus, footer copy, autocomplete/storage exclusions; AC-001/AC-002/AC-003; depends on T002; done when V-003 component tests pass with zero operation posts on reset.
- [ ] T004 — Add `pagehide` clearing + defensive `pageshow` reset on workspace routes and storage/URL/history/cache audit; AC-004/AC-008; depends on T003; done when focused V-010 browser contracts prove real restoration clearing.
- [ ] T005 — Publish dedicated-user E2E reset/teardown/restoration cases (dialog, empty bypass, pending-confirm fencing, reload, sign-out incl. failure, expiry MSG-037) with ≥1 real `/login` form path, real-auth fixture reuse, semantic assertions, Smoke-only seeding and password-absence scan; AC-001–AC-007; depends on T004; done when published suite passes and scan is clean.
- [ ] T006 — Prove privacy on new paths and run V-002/V-003/V-010 focused checks, `contract.sh check` zero drift, and M028–M031 regression suites; AC-008/AC-009; depends on T005; done when sentinel inspection, focused suites, drift check and regressions pass.

## Completion record
Pending — recorded at execution close with AC/task mapping, revision/configuration, actual command/procedure, environment, UTC timestamp, result/evidence link, failures/fixes and limitations. No pasted command logs.
