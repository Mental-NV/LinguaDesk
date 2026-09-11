# M030 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Keep both M028/M029 workspaces trustworthy under competing events.
Each feature's workspace state (source/settings/result/scroll plus its
in-flight revision) lives above the router so mode-link and Back/Forward
navigation preserves text without submitting; settlement callbacks are
fenced by owning page plus captured revision, so a hidden page's result
never touches the visible page while its successful charge still
reconciles authoritative usage. In-page revision guards already in the
reducers extend unchanged to the lifted store. No backend, wire-shape,
or counting-rule change.

## Changes and order

1. Lift per-feature workspace state above the router (`frontend/src`
   shell): one stored workspace per feature (`translate`, `rewrite`)
   holding the current reducer state; pages dispatch into their own
   store entry and read only it. Preserve scroll offset per page and
   focus the destination `h1` after navigation. Navigation dispatches
   no transformation and starts no fetch beyond the existing
   capabilities/usage loads.
2. Move submit flights out of the unmount-abort path: a pending
   operation continues for its hidden page and settles into its own
   store entry by captured revision; cross-page dispatches are
   impossible by construction (each flight closes over its feature key).
   Keep duplicate-activation suppression per page; the other feature
   submits independently.
3. Verify in-page guards hold after the lift on both pages: source/
   settings edits and manual result edits defeat stale application;
   stale success updates usage with UX-MSG-020 → UX-MSG-021;
   manual-edit protection shows UX-MSG-022; later explicit submission
   replaces preexisting edits. No reducer semantic change expected;
   add focused Vitest coverage only for the lifted store plus
   regression units for both reducers.
4. Extend the published E2E suite for the dedicated verified user:
   cross-page preserve/navigate-during-pending cases on both features
   with semantic locators, visible-outcome assertions, per-page usage
   deltas (T-OK-A +46 translate / W-OK +46 rewrite over the fresh-seed
   baseline within each case's own page), at least one real `/login`
   form case, real-auth fixture reuse elsewhere, Smoke-only seeding and
   password-absence scan. No `/api` interception, no test login route.
5. Reconfirm zero contract drift: regenerate OpenAPI/clients per the
   M026/M027 procedure and assert no diff; rerun the M028/M029
   published suites plus V-002/V-003/V-005/V-010 focused checks as
   regressions.

No migration, rollout, or rollback beyond the standard SPA deploy; no
new configuration or access. Operational effect: none (deterministic
fake provider; no live serving).

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 cross-page preservation | T001 | V-012 published E2E `competing-events` case: navigate both directions, assert per-page text kept, zero new `/api/operations` posts, `h1` focus; plus Vitest store unit | `frontend/tests/e2e/` new spec; tasks.md completion record |
| AC-002 hidden settlement isolation | T002 | V-002/V-012: submit A, navigate away, settle; assert visible page untouched, owning page reconciled on return with authoritative usage | Same E2E spec; Vitest flight/store units |
| AC-003 in-page stale defeat both pages | T003 | V-002 reducer/store units + V-003 component checks: stale success fenced with MSG-020/021, manual edit protected with MSG-022, later submit replaces preexisting edits | `frontend/tests/unit/` additions |
| AC-004 no auto-submit, per-page duplicate guard | T003 | V-002/V-003: composition/mode/elapsed-time submit nothing; duplicate suppressed per page; sibling page independent | Unit + component tests |
| AC-005 E2E auth contract | T004 | V-012: ≥1 real `/login` form case; fixture reuse; Smoke-only seed; password-absence scan over artifacts | Published suite run; scan result |
| AC-006 privacy | T005 | V-015 sentinel inspection: problems/logs/traces/TEXT columns free of text/secrets/globals | Privacy check run |
| AC-007 zero wire drift | T006 | V-009: regenerate OpenAPI/clients per M026/M027 procedure; assert zero diff | Generation diff log |
| Prerequisite M029 Done | T000 | Confirm `docs/08-backlogs/M029/tasks.md` completion record and current code state before editing | tasks.md resume pointer |

## Context boundaries and risks

- UX §3.2 + §6, FR-017/018/022/023/026, V-002/V-003/V-005/V-010/V-012,
  arch §8, #6 §4.1/§4.5, M028/M029 dependency specs and the workspace
  source files are in the manifest. Omitted domains and reasons are in
  spec.md Constraints; open on demand only if execution hits a missing
  requirement or cross-boundary conflict.
- Risk: lifted store changes render/subscription behavior for both
  pages; mitigate by keeping reducer semantics byte-identical and
  running the full M028/M029 published suites as regressions.
- Risk: hidden-page settlement racing a later explicit submission on
  the same page; mitigated by captured-revision fencing (newer
  `requestRevision` defeats older callbacks) plus per-page duplicate
  suppression.
- Dependencies: M029 Done (verified in current delivery status and its
  tasks completion record). No blocker. Human steps: none required.
