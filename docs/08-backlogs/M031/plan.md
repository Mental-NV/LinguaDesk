# M031 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Make recovery visible and safe on both workspaces. Each page keeps its
M030 revision-guard semantics and gains two read-only actions alongside
the existing explicit retry: `Check status` (unknown-outcome only,
`GET /api/operations/{originalId}`, never dispatches) and `Refresh
usage` (usage-unavailable only, `GET /api/usage`, never submits).
Definitive failure keeps `Try again` with a new operation key; the
unknown state forbids retry until Check-status returns a terminal
no-record/failed/interrupted answer. New paid work always requires an
explicit activation and never masquerades as replay of the old key.

Selected canonical excerpts arrive through the context packet. Do not
copy them all here. Essential invariants: status/usage reads never
dispatch, charge, or create claims; unknown outcome asserts neither
success nor zero charge; stale failures never replace newer state.

## Changes and order

1. Add status-read clients (`frontend/src/api/translation.ts`,
   `frontend/src/api/rewriting.ts`): `fetchOperationStatus(id)`
   against the existing generated `getLanguageOperationStatus`
   response (`OperationStatusResponse`: pending/succeeded/failed/
   interrupted, characterCount, admissionDay, usage snapshot;
   `OutputAvailable` always false). Map 404 → `unknown-record`,
   410 → `window-expired`, 401/403 → auth errors, other failures →
   `status-unavailable` (unknown state preserved). No new backend
   shape; reuse regenerated client types.
2. Extend both workspace reducers (`translationWorkspace.ts`,
   `rewritingWorkspace.ts`) with an unknown-outcome error kind
   (retains `pendingOperationId`, `canCheckStatus: true`,
   `canRetry: false`), a `statusChecked` resolution set
   (succeeded-unavailable disclosure with confirmed charge;
   failed/interrupted zero-charge with retry enabled;
   pending → unknown preserved; unknown-record/window-expired →
   nothing-known guidance with new-submission permitted), and a
   usage-unavailable notice with refresh state. Edits clear recovery
   alerts exactly like existing failures; stale revisions ignore
   late status answers; `submitAborted`/teardown semantics unchanged.
3. Update both pages (`TranslatePage.tsx`, `RewritePage.tsx`):
   render the §6.2 unknown-outcome copy with `Check status`, the
   `Usage update unavailable` notice with `Refresh usage`, and the
   lost-output disclosure; wire buttons to the lifted store flights
   (`frontend/src/shell/workspaceStores.ts`) with per-page revision
   fencing; keep `role="alert"`/`role="status"` and announcement
   rules (§6.4: one start/completion announcement, errors once).
4. Extend the published E2E suite for the dedicated verified user:
   per-page definitive-failure retry with new-key assertion (sentinel
   `M028-PROCESSING-FAILURE` / rewrite equivalent), per-page
   offline-submit unknown-outcome → `Check status` → no-record
   guidance (via `context.setOffline`, no `/api` interception), and
   usage-refresh read-only case with operation-post counts; at least
   one real `/login` form case, real-auth fixture reuse elsewhere,
   Smoke-only seeding and password-absence scan.
5. Add focused Vitest coverage: reducer transitions for
   unknown-outcome/check-resolution/usage-unavailable on both
   workspaces, status-client mapping against the real generated
   shapes (all four terminal states + 404/410/unavailable), and
   component checks for the three distinct recovery renderings;
   V-015 sentinel inspection for the new UI paths.
6. Reconfirm zero contract drift: regenerate OpenAPI/clients per the
   M026/M027 procedure and assert no diff; rerun the
   M028/M029/M030 published suites plus V-002/V-003/V-005/V-010
   focused checks as regressions.

No migration, rollout, or rollback beyond the standard SPA deploy; no
new configuration or access. Operational effect: none (deterministic
fake provider; no live serving).

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 definitive failure + new-key retry | T004 | V-012 published E2E per page: sentinel failure, zero-charge usage line, Try again posts new operationId; plus V-002 reducer units | `frontend/tests/e2e/` new spec; tasks.md completion record |
| AC-002 deadline/allowance/budget states | T004/T005 | V-012 E2E (deadline copy where reachable) + V-002/V-003 units for MSG-014/015/016/017 rendering with no retry affordance | Same E2E spec; unit/component runs |
| AC-003 unknown outcome on interruption | T004 | V-012 E2E per page: `setOffline(true)` submit → unknown copy, work preserved, no charge claim | Same E2E spec |
| AC-004 Check-status resolution | T001/T004 | V-009/V-012: E2E no-record path via real GET; units cover succeeded/failed/interrupted/pending mapping with generated shapes | Client/reducer unit runs; E2E spec |
| AC-005 usage-unavailable + read-only refresh | T004 | V-012 E2E: refresh sends zero operation posts and replaces counter; V-002/V-003 units for notice state | Same E2E spec; unit runs |
| AC-006 no unintended work, stale failure fenced | T003/T004 | V-002 units + E2E request-count assertions; stale status answers ignored by revision | Unit runs; E2E spec |
| AC-007 E2E auth contract | T004 | V-012: ≥1 real `/login` form case; fixture reuse; Smoke-only seed; password-absence scan over artifacts | Published suite run; scan result |
| AC-008 privacy | T005 | V-015 sentinel inspection: problems/logs/traces/TEXT columns free of text/secrets/globals | Privacy check run |
| AC-009 zero wire drift | T006 | V-009: regenerate OpenAPI/clients per M026/M027 procedure; assert zero diff | Generation diff log |
| Prerequisite M030/M024/M025 Done | T000 | Confirm tasks completion records and current code state before editing | tasks.md resume pointer |

## Context boundaries and risks

- Roadmap M031, FR-026/028/034/037, UX §6.1/§6.2/§6.3, selected
  UX-AC recovery scenarios, API §6.1/§6.2/§8, #6 §4.5/§2.2, fixtures
  §4.1, arch §8, M030/M024/M025 dependency specs and the workspace/API
  source files are in the manifest. Omitted domains and reasons are in
  spec.md Constraints; open on demand only if execution hits a missing
  requirement or cross-boundary conflict.
- Risk: Check-status `succeeded` path cannot occur deterministically
  end to end (Smoke provider settles synchronously); mitigated by
  mapping the real generated shape in units and proving the live
  no-record path in E2E.
- Risk: offline simulation flakiness; mitigated by restoring online
  state in `finally` and asserting on visible copy plus request
  counts rather than timing.
- Dependencies: M030, M024, M025 Done (verified in current delivery
  status and their tasks completion records). No blocker. Human steps:
  none required.
