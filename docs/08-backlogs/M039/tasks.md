# M039 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: none; M039 is complete. Blockers: none. Last check: 221 frontend, 616 backend and 80 published Chromium cases passed; all seven visual comparisons passed without updates.

## Ordered tasks
- [x] T001 — Author/review package and lock source inputs; all AC prerequisites; depends M044; done when context check passes.
- [x] T002 — Add shared scanner and bounded narrow browser group; AC-001–004; depends T001.
- [x] T003 — Run unit/published regressions, review unchanged visuals and close evidence/coverage; AC-001–005; depends T002.

## Completion record
M044 evidence is reusable for unchanged scheduler, backend, privacy handlers and curated visual design. The selected increment adds narrow UI evidence; runtime results pending.


### Completion — 2026-10-25 10:02 UTC

Revision: deb0da78243fc482d936f149512fb24ea75a8556 plus the M044/M039 working changes. Same pinned environment as M044: Node 24.20.0/npm 11.11.0, .NET 10.0.302, macOS 27.0.1, Playwright 1.63.0 and Chromium 153.0.8010.12 / revision 1243, scale 1. The owned HTTPS Smoke host used an isolated migrated SQLite database and real local cookie authentication; provider adapters were deterministic. No production deployment or paid provider run occurred.

- T002 / AC-001/002: `narrow-workspace.spec.ts` completes the real Email/Password/Sign in flow to both requested feature routes at 390×844, verifies zero posts at 999 ms and one at 1,000 ms, visible editable results, retained source focus, exact native clipboard text/newlines, no extra charge for manual edits/copy, essential failures, explicit new-key retry and API-observed session expiry clearing before login.
- T002 / AC-002: one real admitted Translation is interrupted by actual browser offline mode; a bounded client deadline exposes unknown outcome, retaining its original operation key and previous result. Check status reads that exact identity once and adds no operation. A pending server answer keeps Check status and forbids Try again; the test does not infer failure or zero charge. Controlled terminal classifications are separately covered by the shared component/API tests. No `/api` request or response is intercepted.
- T002 / AC-003/004: registration validation/reveal/accepted state and explicit continuation are exercised at 390×844. A distinct synthetic visitor is registered through the public form; transformations use the predefined verified seed account. Shared browser scans check names/labels/document language, visible text, control boundaries and empty-result placeholder contrast. Workspace controls/reflow/text-spacing checks pass at 1024×768, 720×450 and 320×640, and active account forms at 320×640. Viewport reflow is not a claim of physical-device or actual native-browser zoom verification.
- Discovered/fixed defects: initial signed-out `/rewrite` bootstrap used to discard the safe return destination; it now preserves that path, while an auth epoch rejects old bootstrap completions after newer account actions. Enabled select text previously passed briefly through a low-contrast color during its disabled-to-enabled transition; control text now changes immediately and only border color transitions. The shared scanner caught the actual contrast dip. The updated initial-entry regression asserts Rewriting after login; actual expiry still clears work and resets its continuation.
- Smoke-only external-adapter support: a cancellable 750 ms M039-DELAY fixture in each existing fake provider exposes admitted work to the real interruption test. These providers remain registered only under Smoke. Two unit cases cancel the delay without waiting and assert cancellation. Production provider/wire/schema/storage behavior is unchanged.
- T003 / AC-005: `bash scripts/frontend.sh check` passed typecheck, lint, 221 unit/component cases (including 28 Unicode/input cases), and production build. The published smoke wrapper repeats this gate.
- T003 / AC-005: `bash scripts/backend.sh check` passed 616/616: API/storage 259, core 10, offline AI 313, benchmark harness 34. Registration-outside-Smoke tests preserve the fixture boundary.
- T003 / AC-005: final `bash scripts/frontend.sh smoke` passed 80/80 in 51.7 seconds, with baseline updates disabled. All seven M044 screenshots match; [the reviewed hashes/rationale](../M044/visual-review.md) remain applicable. No tolerance or baseline regeneration was used to fix the two product defects.
- Source/closeout checks: AI reviewed the final changed/new source and selected acceptance-to-check map. `python3 automation/context.py check M039`, `git diff --check`, and `python3 automation/context.py audit` pass. The audit checked local links/anchors and package layout in 223 Markdown files before final closeout; its final rerun is recorded below if the total changes.

Evidence: `artifacts/m039-frontend-check.log`, `m039-backend-check.log`, `m039-smoke-final.log`, `m039-context-audit.log`; JUnit/TRX reports in `artifacts/test-results/`; source suites and the seven checked-in PNGs/font manifest reproduce the checks. The first unknown-outcome browser assertion incorrectly required an already terminal server response; it was corrected to honor a still-pending authoritative result and keep the paid-retry block. That intermediate failure remains in `artifacts/m039-smoke.log`.

Limits: no full WCAG certification, human/AT/native-IME/device/additional-engine or Linux-container claim. Synthetic composition establishes guards only. Fake provider text establishes integration, not live language quality/performance. Actual navigation clearing is checked; no assertion claims that every navigation used real bfcache. Full corpus, live load, backup/restore and broader lifecycle/release assembly remain outside these two selected increments. M044's completed package/lock is historical; adding the M039 roadmap package link changes its old selected future-roadmap row without changing its accepted conclusions, so its lock was not retroactively rewritten.
