# M030 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: none — all tasks done. Blockers: none.
Last check: `bash scripts/verify-milestone.sh M030` passed 2026-09-11.
Changed scope: none (additive `submitAborted` teardown action documented in
T001; see completion record).

## Ordered tasks
- [x] T000 — Confirm M029 Done via its tasks completion record and delivery status, and current workspace code state; AC-none (prerequisite); depends on none; done when resume pointer advances with no divergence noted.
- [x] T001 — Lift per-feature workspace state above the router with per-page text/settings/result/scroll preservation, destination `h1` focus, and no submit on navigation; AC-001; depends on T000; done when cross-page E2E case + store units pass.
- [x] T002 — Fence hidden-page settlement to its owning store entry by captured revision; visible page focus/text untouched; owning page reconciles authoritative usage on return; AC-002; depends on T001; done when navigate-during-pending E2E cases + flight/store units pass.
- [x] T003 — Regression-harden in-page stale guards on both pages (source/settings/result-edit defeat, MSG-020/021 stale-charge notice, MSG-022 manual-edit protection, no auto-submit, per-page duplicate suppression); AC-003, AC-004; depends on T001; done when V-002/V-003/V-010 focused checks pass.
- [x] T004 — Publish dedicated-user E2E cases with ≥1 real `/login` form path, real-auth fixture reuse, semantic visible-outcome assertions, Smoke-only seeding and password-absence scan; AC-005; depends on T001, T002; done when published suite passes and scan is clean.
- [x] T005 — Prove privacy: no source/result text, credentials, global counts, monetary values or provider internals in problems/logs/traces/TEXT columns; AC-006; depends on T002, T003; done when V-015 sentinel inspection passes.
- [x] T006 — Regenerate OpenAPI/clients per M026/M027 procedure and assert zero drift; rerun M028/M029 published suites as regressions; AC-007; depends on T004; done when generation diff is empty and regressions pass.

## Completion record

Revision: working tree at `94270a2` (M030 planned) plus the M030 change set
(frontend lifted store, pages, tests; no backend change); no commit by the
runner (runner owns commits). Environment: .NET SDK 10.0.302, Node v24.20.0,
npm 11.11.0, Chromium (Playwright 1.63.0), Linux/macOS loopback HTTPS host,
isolated migrated SQLite + owned data-protection keys per smoke run. UTC
2026-09-11. Evidence: `artifacts/test-results/frontend-unit.xml` (221 tests,
0 failures), `artifacts/test-results/frontend-e2e.xml` (48 passed),
backend/API/AI suites via `verify-milestone.sh` (all green: backend 505/505,
AI 271, `ai.sh probe` clean, contract zero drift, `git diff --check` clean).

T000 note. M029 tasks show all seven tasks done with its gate passed
2026-09-11; delivery status names M029 Done; tree was clean at `94270a2`
(M030 planned) over `cc5295f` (M029 implemented). No divergence.

T001. New `frontend/src/shell/workspaceStores.ts` (per-feature store
instances: reducer state, transport flights with per-feature
AbortControllers, copy timers, scroll offsets, flight abort) plus
`frontend/src/shell/workspaces.tsx` (`WorkspaceStoreProvider`). `App`
renders `Shell` inside the provider; pages consume their entry via
`useTranslateFeature`/`useRewriteFeature` with a standalone fallback that
preserves the exact M028/M029 per-page behavior for focused unit tests.
Navigation performs no transformation beyond the pre-existing
capabilities/usage loads (mount load is now guarded so returning never
aborts a hidden flight); destination `h1` focus is unchanged (`Page`
focuses on location change); scroll offset is saved on unmount and
restored on mount. Additive change: both reducers gain `submitAborted`,
which releases the busy phase without touching text when sign-out
invalidates flights (M013 teardown parity — the lifted store otherwise
survives unmount, so the implicit unmount-abort had to become explicit);
stale revisions are ignored exactly like `submitFailed`. Navigation never
dispatches it. `context.py lock M030` refreshed after review (three
manifest code sources changed as the plan orders); check + audit clean.

T002. Each flight closes over its feature key and dispatches only into its
own store entry by captured revision; cross-page dispatches are impossible
by construction. Verified by two published navigate-during-pending E2E
cases (one per feature) plus store units that settle a hidden operation
while the sibling entry stays untouched.

T003. Reducer guard semantics are byte-identical for every pre-existing
action (diff-verified); new focused coverage: 4 `workspaceStores` cases
(cross-page preservation with zero operations, hidden settlement
isolation, abort fencing/scroll/entry isolation, late-settlement
suppression) + 1 `submitAborted` regression case per reducer suite. Full
`frontend.sh check` green (typecheck, lint, 221 unit tests, build);
existing V-002/V-003/V-010 suites pass unchanged (215/215 pre-existing).

T004. `tests/e2e/competing-events.spec.ts` (4 cases) reusing the
documented real-auth fixture from `translate-auth.ts`: real `/login` form
case extended into cross-page preservation (both directions, per-page
text/settings kept, zero `/api/operations`, `h1` focus asserted);
translate and rewrite navigate-during-pending cases with same-page usage
deltas (+46 T-OK-A / +46 W-OK) and owning-page reconciliation; later
explicit translation replaces a preexisting result edit (+46 then +52).
All 48 published cases pass (44 pre-existing + 4 new); the smoke
password scan is clean. One test-expectation fix during the run: usage
deltas now wait for the loaded snapshot before parsing (matches the M029
pattern).

T005. AC-006 sentinel inspection: change set is frontend-only (no backend,
migration or TEXT-column change); no `console`/storage/clipboard writes
beyond the existing copy path; usage display still shows only the user's
own consumed/allowance/remaining/reset; problem mapping unchanged; smoke
password scan clean over seed log, host log and E2E report.

T006. `contract.sh check` zero drift (no wire-shape change, as selected);
M028/M029 published suites rerun as regressions inside the same smoke run
(all pass; M028/M029 scope and evidence unchanged).

Limitations. Deterministic fixture text is transport/presentation evidence
only (V-013 out of scope). Hidden-settlement E2E relies on the real
antiforgery + POST roundtrip outlasting immediate navigation; the fencing
itself is proven deterministically at store/reducer level. Seed usage
assumes seed and run share a UTC day. Sign-out preserves workspace text
for the next session (M032 owns teardown clearing); sign-out aborts only
release the busy phase. Visual baselines and browser/device/AT claims
stay with M038/M039/G3.
