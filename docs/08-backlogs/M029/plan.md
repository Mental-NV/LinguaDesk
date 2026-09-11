# M029 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Replace the M013 `SignedInPlaceholder` on the guarded `/rewrite` route
with the real rewriting workspace, wired to the reused M027 rewriting
operation through cookie auth + antiforgery and the generated client,
mirroring the M028 translation slice. Invariants: explicit activation
only (no auto-submit, no post-composition queue); mode dropdown holds
exactly one value with Correction only default; one unsettled operation
per page with request/workspace-revision guards so stale responses and
manual edits are protected; authoritative inline usage from server
snapshots; complete source retained on every rejection/failure with
zero successful-operation charge; rewriting never touches the source
field. No backend handler change is planned; the contract-review gate
reopens if implementation finds the cookie path needs one. One reviewed
addition is expected beyond "no backend change" (same class as M028's):
a Smoke-environment-only deterministic rewriting provider plus
sentinel-gated failure/eligibility paths, reusing the M028 seeder
baseline. No wire-shape or contract change.

Selected canonical excerpts arrive through the context packet. Do not copy
them all here.

## Changes and order

1. Reuse review: M027 operation surface (submit/status DTOs, mode wire
   name, verified-account policy, antiforgery expectations), M028
   workspace pattern (`translationWorkspace.ts`, `TranslatePage`,
   `translation.ts` transport, `translate-auth.ts` fixture, E2E shape),
   M013 guarded-route/session behavior, `frontend/src/api/inputPolicy.ts`
   counting, generated client transport. List touched entry points
   before changing them.
2. State core: new `frontend/src/rewrite/rewritingWorkspace.ts` reducer
   + pure helpers (explicit-dispatch guard, revision capture, outdated
   marking, manual-edit protection, usage ordering) with Vitest units.
   Reuse arch §8.1 patterns and the M028 guard shape; no global store or
   editor framework.
3. Page + wiring: `RewritePage` (mode dropdown with Correction only
   default + eight styles/tones, source editor, explicit Rewrite,
   editable/copyable plain result, authoritative usage, normative
   messages), route swap in `frontend/src/shell/App.tsx`, cookie-auth
   submit/status transport via generated types (`frontend/src/api/`
   rewriting module mirroring `translation.ts`); regenerate types before
   adoption (`scripts/contract.sh`), zero drift.
4. Backend test-only addition: Smoke-gated deterministic rewriting
   provider (fixed W-OK fixture result, sentinel-gated eligibility
   rejection and processing failure, fail-closed outside Smoke) plus
   Smoke monetary env already added by M028; no handler, wire-shape or
   contract change (`contract.sh check` zero drift).
5. Component checks: dropdown defaults/single-choice, forms, messages,
   request counts, displayed usage, live-region mutations (Testing
   Library).
6. Published E2E: login-form case + fixture cases covering AC-001/002
   (mode choice)/003/004/005 against the owned HTTPS loopback host with
   migrated seed database and the dedicated verified account;
   browser-visible assertions only, supplemental counts permitted.
7. Closeout: privacy inspection (AC-007), drift/typecheck/lint, context
   re-check, evidence record. No migration, rollout, or rollback:
   additive frontend slice behind the existing verified guard;
   `/translate` untouched.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| Reuse review | T001 | Read-only review of listed entry points; regressions identified | tasks.md T001 note |
| AC-002 (guards/revisions) | T002 | `npm run test` (frontend Vitest units: reducer/revision/edit-protection) | artifacts/test-results/frontend-unit.xml |
| AC-001/003/004/005 (visible) | T003+T004 | `npm run test` (Testing Library components: dropdown/forms/messages/counts/usage) + `npm run typecheck` | same XML + typecheck log |
| AC-008 (wire/drift) | T003 | `bash scripts/contract.sh check` — zero drift | contract check output |
| AC-001–006 (published journey) | T005 | `npm run test:e2e` with owned host (`LINGUADESK_PUBLISHED_URL` HTTPS loopback), seeded Smoke database; one login-form case + fixture cases; password-scan clean | artifacts/playwright + junit XML |
| AC-007 (privacy) | T006 | Sentinel inspection (no text/secrets/counts in problems/logs/traces/TEXT columns) | tasks.md completion record |
| Full gate | T007 | `bash scripts/verify-milestone.sh M029` | tasks.md completion record |

## Context boundaries and risks

Omitted domains and why: translation/target selection (separate M028
scope, no shared code path beyond the guard shell); backend
accounting/recovery/chain (M027 Done, reused not re-proven —
regression suites stay green as guard only); live
quality/performance/serving (deterministic slice; Q-001/Q-005 pending by
design); email/lifecycle/safeguards (no delivery, lifecycle, or new
enforcement in slice); curated visual baselines (unchanged; full
browser/device/AT claim sits with M038/M039/G3); product-wide coverage
(stays in #6; only the selected-scenario matrix above ships here).

Risks: the Smoke deterministic rewriting provider is new test-only
backend surface — modeled exactly on the reviewed M028 provider
(fail-closed registration, fixture + sentinel behavior, no text in
logs); stale-response/edit races are the highest-risk UI behavior and
carry dedicated unit + E2E assertions; fixture-count coupling (usage
7,546) fails closed on any seed/count change. Assumptions: M027 wire
shape needs no cookie-specific change; seed baseline (usage 7,500) and
fixtures (W-OK/W-LONG) unchanged; mode catalog unchanged (nine choices).
Human actions: none; blocked gate: none.
