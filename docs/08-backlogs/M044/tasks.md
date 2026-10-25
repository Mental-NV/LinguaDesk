# M044 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: none; M044 is complete. Blockers: none.
Last check: published Chromium smoke passed 73/73 with baseline updates disabled; frontend 221/221 and backend 614/614 passed.

## Ordered tasks
- [x] T001 — Reconcile owners, author/review package and lock inputs; all AC prerequisites; no dependency; done when context check passes.
- [x] T002 — Implement revision-safe shared scheduler/availability/recovery; AC-003–006; depends T001; fake timer/deferred checks pass.
- [x] T003 — Implement frames/native panels/responsive tokens and preserve teardown; AC-001/002/006; depends T001.
- [x] T004 — Migrate regressions and add pause/geometry/native-browser coverage, review seven captures; AC-001–007; depends T002/T003.
- [x] T005 — Run required checks, review diff and close affected delivery/coverage; AC-001–007; depends T004.

## Completion record
Planning: reviewed UX/layout/processing/acceptance, PRD D-21/D-22, architecture frontend, API identity/recovery/usage, frontend harness, current source and installed toolchain. Dependencies are present; no external prerequisite or paid run is required. Runtime evidence pending.


### Completion — 2026-10-25 09:50 UTC

Baseline revision: deb0da78243fc482d936f149512fb24ea75a8556 plus this uncommitted M044 implementation. Node 24.20.0, npm 11.11.0, .NET SDK 10.0.302; macOS 27.0.1 / pinned Chromium 153.0.8010.12 (Playwright 1.63.0, revision 1243). No production deployment or paid provider call occurred.

- T002 / AC-003–006: `bash scripts/frontend.sh check` passed typecheck, lint, 221 Vitest cases (28 shared Unicode/input cases), and production build. Shared pause tests cover exact 999/1000 ms timing, all 12 Translation pairs / 36 language-mode payloads, queue replacement, edits/composition before and after dispatch, StrictMode, offline/read-only recovery, unresolved identity, client deadline and unclassified 5xx unknown outcomes. Lifecycle tests verify generation fencing and shared ordered usage. Replaced obsolete status/copy/reset/button suites with shared current-contract suites; retained auth/API/policy regressions.
- T003/T004 / AC-001/002/006/007: `bash scripts/frontend.sh smoke` passed 73 Chromium cases through the published SPA, real account/API/migrated SQLite and deterministic Smoke providers. Both feature panels meet exact widths at 1440/768/390/320; desktop heights/top edges align. Native clipboard, composition-event guards, focus/history, full navigation/reload/new-tab clearing, observed expiry, essential errors and no removed controls are asserted in the browser.
- T004 / AC-004/005: `bash scripts/backend.sh check` passed 614 cases: API/storage 257, core policy 10, AI offline 313, benchmark harness 34. Accounting/settlement semantics remain at the API/database boundary; no usage-summary DOM is restored.
- T004/T005 / AC-007: seven curated captures intentionally updated, inspected and accepted with [visual rationale and image hashes](visual-review.md); final normal smoke rerun passed with baseline updates disabled. Font bytes and manifest hashes were independently checked.
- T001/T005: reviewed owner/source consistency and full changed/new-source diff; `python3 automation/context.py check M044` and `git diff --check` pass. Initial browser failures were obsolete broad heading locators, an assumption about multi-event native undo grouping, and requiring the registration summary to remain in view while first-error focus scrolls the page; tests now assert the actual native/reflow contracts.

Runtime reports: `artifacts/test-results/frontend-unit.xml`, `frontend-e2e.xml`, `backend-api.trx`, `backend-core.trx`, `backend-ai.trx`, `backend-benchmark.trx`. Sanitized command logs: `artifacts/m044-frontend-check.log`, `m044-smoke-final.log`, `m044-backend-check.log`. The checked-in suites, font manifest and seven PNGs reproduce the evidence. Initial restricted-sandbox .NET runs were stopped and rerun with authorized cache/network/owned-loopback access; no automatic approval rejection occurred.

Limits: scripted provider output proves integration/presentation, not live linguistic quality or performance. Synthetic composition proves guards, not a native IME/device pass. Navigation restoration tests clear data on actual browser navigation; they do not assert that every Back used bfcache. Complete cache/retention/backup and live corpus gates remain with their owning milestones. M039 supplies the next dedicated narrow core-journey group; no product-wide RG is declared passed here.
