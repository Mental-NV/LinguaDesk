# M032 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

End the workspace safely on both feature pages without touching the
backend wire. Add a clearing reducer action plus store-level reset-all
so explicit reset, sign-out/expiry teardown and `pagehide`/`pageshow`
restoration all funnel through one clearing path that aborts flights,
bumps/invalidates revisions against late responses, keeps only
independently settled usage, restores FR-038 defaults and focuses
correctly. Inline Start-new-workspace/Sign-out controls, the exact
§3.4 dialog, footer copy and storage/autocomplete exclusions complete
the visible contract. AC-001–AC-006 carry the new behavior; AC-007–
AC-009 carry the E2E, privacy and drift gates; M028–M031 suites
regression-guard happy paths, competing events and recovery.

## Changes and order

1. Reducers (`frontend/src/translate/translationWorkspace.ts`,
   `frontend/src/rewrite/rewritingWorkspace.ts`): add a
   `workspaceCleared` action clearing source/result/settings edits,
   errors/notices, pending operation identity and composing state to
   `createInitialWorkspace` defaults while preserving the settled usage
   snapshot; revision handling must fence late `submitSucceeded`/
   `submitFailed`/`statusResolved` dispatches after clearing. Pure
   Vitest units prove clearing, fencing and usage preservation.
2. Store (`frontend/src/shell/workspaceStores.ts`): add `resetAll`
   dispatching the clearing action to both features, aborting paid and
   read-only flights, and clearing saved scroll; wire `resetAll` into
   sign-out, session-expiry and reset-confirm paths so lifted text
   cannot survive teardown. No reducer-semantics change beyond the new
   action.
3. Reset dialog (both pages or shared shell component): inline `Start
   new workspace` button per page; non-empty workspaces open the
   native `<dialog>` with the exact §3.4 heading/body/buttons, `Cancel`
   autofocus, Escape/Cancel preservation with trigger-focus return;
   empty workspaces reset immediately; confirm clears both pages,
   navigates to `/translate`, focuses Source text and submits nothing.
   Component tests prove dialog copy/focus/bypass/request counts.
4. Teardown/restoration: `pagehide` clearing plus defensive `pageshow`
   reset on workspace routes; expiry path clears before login renders
   with MSG-037; failure copy/redirect-suppression per §3.4; footer
   lifetime text on workspace pages; editor autocomplete off and no
   text in storage/URL/history/IndexedDB/service-worker/HTTP-cache
   paths. Focused browser contracts prove real restoration; synthetic
   dispatch proves handlers only.
5. No backend, migration, DTO, OpenAPI or seed change. Regenerate
   client types and prove zero drift before adoption.

Migration/rollout/rollback: none — frontend-only slice with no schema,
config or published-contract change. Roll back by reverting the
frontend change set.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 dialog/focus/bypass | T003 | V-003 component tests in `scripts/frontend.sh check`; E2E dialog case in published smoke | tasks.md completion record |
| AC-002 confirm with pending ops | T001+T003+T005 | V-002 reducer/store units + V-010/V-012 published E2E (late-response fencing, zero operation posts on confirm) | tasks.md completion record |
| AC-003 empty immediate reset | T003+T005 | V-003 component + V-012 E2E empty-reset case | tasks.md completion record |
| AC-004 real restoration | T004+T005 | V-010 focused browser contracts (real reload/navigation/bfcache) + V-012 E2E; synthetic dispatch is handler-only evidence | tasks.md completion record |
| AC-005 sign-out teardown | T002+T005 | V-012 E2E sign-out case (immediate clear, failure copy, Back clean) + V-002 fencing units | tasks.md completion record |
| AC-006 session expiry | T002+T005 | V-012 E2E/unit expiry case (MSG-037, pre-render clear, late-callback rejection) | tasks.md completion record |
| AC-007 E2E auth contract | T005 | Published smoke via `scripts/publish.sh` path with real `/login` case + fixture reuse + password-absence scan | tasks.md completion record |
| AC-008 privacy | T006 | V-015 sentinel inspection (DB/logs/traces/errors/storage/history/cache/backups) in smoke + focused suites | tasks.md completion record |
| AC-009 zero drift | T006 | `scripts/contract.sh check` zero OpenAPI/TypeScript drift | tasks.md completion record |
| Regression M028–M031 | T006 | Rerun M028/M029/M030/M031 published suites in the same smoke run; `scripts/frontend.sh check`, `scripts/backend.sh check` | tasks.md completion record |

## Context boundaries and risks

- Omitted domains and why: backend accounting/recovery (reused wire,
  regression-covered, no handler selected); live provider/quality/perf
  (deterministic fake only per roadmap §4.5 note); email delivery
  (M034); curated visuals and device/AT breadth (M038/M039/G3);
  deletion/backup retention rules (Q-004 remainder, launch-gated, not
  observable-teardown); safeguards/compat proposals (Q-008/P-005/
  P-006, unadopted). Open on demand only if execution meets a handler
  need (reopens the contract-review gate) or a changed input.
- Dependencies: M031 Done (proven by delivery/current.md and the M031
  package reference); transitive M028/M029/M030 workspaces,
  M024 status-read semantics, M025 usage snapshots — all Done, reused
  by reference, not reread in full.
- Assumptions: dedicated verified E2E account and Smoke-only seeding
  from #6 §4.1 unchanged; real-device/cache gaps, if any, are labeled
  explicitly rather than claimed via synthetic proof.
- Human steps: None required. No live credentials, paid dispatch,
  manual AT/device evidence or production access is needed; pre-arrange
  nonblocking human review of the dialog/restoration behavior at the
  end with the published smoke procedure.
- Top risk: lifted-store text surviving sign-out/expiry because the
  current handler aborts flights without clearing — closed by routing
  every teardown through the single reviewed reset-all path with
  revision fencing.
