# M028 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Replace the M013 `SignedInPlaceholder` on the guarded `/translate` route
with the first real translation workspace, wired to the reused M026
translation operation through cookie auth + antiforgery and the generated
client. Invariants: explicit activation only (no auto-submit, no
post-composition queue); one unsettled operation per page with
request/workspace-revision guards so stale responses and manual edits are
protected; authoritative inline usage from server snapshots; complete
source retained on every rejection/failure with zero successful-operation
charge. No backend handler change is planned; the contract-review gate
reopens if implementation finds the cookie path needs one.

Selected canonical excerpts arrive through the context packet. Do not copy
them all here.

## Changes and order

1. Reuse review: M026 operation surface (submit/status DTOs, verified-account
   policy, antiforgery expectations), M013 guarded-route/session behavior,
   `frontend/src/api/inputPolicy.ts` counting, generated client transport.
   List touched entry points before changing them.
2. State core: translation feature reducer + pure helpers (explicit-dispatch
   guard, revision capture, outdated marking, manual-edit protection, usage
   ordering) with Vitest units. Reuse arch §8.1 patterns; no global store or
   editor framework.
3. Page + wiring: `TranslatePage` (selectors, editors, result view, usage
   display, normative messages), route swap in `frontend/src/shell/App.tsx`,
   cookie-auth submit/status transport via generated types; regenerate types
   before adoption (`scripts/contract.sh`), zero drift.
4. Component checks: forms, selectors, defaults, messages, request counts,
   displayed usage, live-region mutations (Testing Library).
5. Published E2E: login-form case + fixture cases covering AC-001/003/004/005
   against the owned HTTPS loopback host with migrated seed database and the
   dedicated verified account; browser-visible assertions only, supplemental
   counts permitted.
6. Closeout: privacy inspection (AC-007), drift/typecheck/lint, context
   re-check, evidence record. No migration, rollout, or rollback: additive
   frontend slice behind the existing verified guard; rewrite placeholder
   untouched.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| Reuse review | T001 | Read-only review of listed entry points; regressions identified | tasks.md T001 note |
| AC-002 (guards/revisions) | T002 | `npm run test` (frontend Vitest units: reducer/revision/edit-protection) | artifacts/test-results/frontend-unit.xml |
| AC-001/003/004/005 (visible) | T003+T004 | `npm run test` (Testing Library components: forms/messages/counts/usage) + `npm run typecheck` | same XML + typecheck log |
| AC-008 (wire/drift) | T003 | `bash scripts/contract.sh check` — zero drift | contract check output |
| AC-001–006 (published journey) | T005 | `npm run test:e2e` with owned host (`LINGUADESK_PUBLISHED_URL` HTTPS loopback), seeded Smoke database; one login-form case + fixture cases; password-scan clean | artifacts/playwright + junit XML |
| AC-007 (privacy) | T006 | Sentinel inspection (no text/secrets/counts in problems/logs/traces/TEXT columns) | tasks.md completion record |
| Full gate | T007 | `bash scripts/verify-milestone.sh M028` | tasks.md completion record |

## Context boundaries and risks

Omitted domains and why: rewriting/assistance (separate M027/M029 scope, no
shared code path); backend accounting/recovery/chain (M026 Done, reused not
re-proven — regression suites stay green as guard only); live
quality/performance/serving (deterministic slice; Q-001/Q-005 pending by
design); email/lifecycle/safeguards (no delivery, lifecycle, or new
enforcement in slice); curated visual baselines (unchanged; full
browser/device/AT claim sits with M038/M039/G3); product-wide coverage
(stays in #6; only the selected-scenario matrix above ships here).

Risks: cookie+antiforgery submit through the published host is the newest
integration point — proven early in T003 against the real host, not mocked;
stale-response/edit races are the highest-risk UI behavior and carry
dedicated unit + E2E assertions; fixture-count coupling (usage 7,546) fails
closed on any seed/count change. Assumptions: M026 wire shape needs no
cookie-specific change; seed baseline (usage 7,500) and fixtures
(T-OK-A/T-LONG) unchanged. Human actions: none; blocked gate: none.
