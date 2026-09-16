# M038 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Milestone complete 2026-09-16 (T005 signed off below). Follow-ups:
container-pinned (Noto Sans/SC) Linux re-capture; Firefox/Chrome mobile
matrix (no M039 yet); one Firefox desktop submission run unrecorded;
G3/RG-006 stay pending on the mobile matrix.
Last check: `bash scripts/verify-milestone.sh M038` passed 2026-09-16
(context check + audit, backend check + smoke, contract check, AI
check + probe, frontend check + smoke with 74/74 Chromium cases, `git
diff --check`). Changed scope since the partial record: narrowed
browser/AT requirements (RG-006, NFR-005, G3, UX §4–5/10, UX-AC-079
retired), `MonetaryAdmission` config-first default with Development
fail-fast guard plus tests, and NFR-007 nav icons (globe/pencil per
§4.1 token) with unit assertions; no backend, migration, DTO, OpenAPI
or seed change.

## Ordered tasks
- [x] T001 — Add V-010 desktop browser contracts (native edit/caret/selection/undo, real-clipboard copy, keyboard-only journeys, composition guard, desktop geometry/zoom/spacing); AC-003; depends on none; done when focused contracts pass in the published run.
- [x] T002 — Add three-baseline desktop screenshot harness with pinned capture plus automated semantic/contrast scans; AC-004/AC-005 (automated part); depends on T001; done when baselines pass at starting thresholds with zero serious/critical scan findings.
- [x] T003 — Add/extend dedicated-user published desktop E2E Translation/Rewriting journeys at 1440×900 with ≥1 real `/login` form case and real-auth fixture reuse; AC-001/AC-002/AC-006; depends on T001; done when published suite passes with a clean password scan.
- [x] T004 — Prepare and execute desktop manual procedures (keyboard-only, zoom/spacing/forced-colors/reduced-motion, NVDA/Firefox and VoiceOver/Safari journeys, branded Chrome/Edge smoke) with reviewer sign-off and explicit gap list; AC-005 (manual part); depends on T003; executed subset recorded 2026-09-16 (M-001/M-002 pass, M-003/M-005 partial, M-004 not executed per owner scope decision) with explicit gaps in [manual-desktop-procedures](manual-desktop-procedures.md).
- [x] T005 — Prove new-artifact privacy, `contract.sh check` zero drift, M028–M032 regressions and readiness review (baseline design-review approval, coverage of every AC); AC-007 plus gate; depends on T004; gates passed 2026-09-16, design-review approved and readiness accepted by Owner 2026-09-16, final record below.

## Completion record (partial — automated slice, 2026-09-15T20:08Z UTC)

Revision: `69bd09c` (M038 planned) plus uncommitted harness set below;
host/mode `scripts/frontend.sh smoke` published Smoke host (owned
loopback HTTPS, fresh isolated migrated SQLite, seeded
`m013-verified@example.test`, deterministic provider adapters);
environment macOS arm64, Node v24.20.0, npm 11.11.0, .NET 10.0.302,
bundled Playwright Chromium (1440×900, scale factor 1).

New/changed files (no commit — runner owns commits):
- `frontend/tests/e2e/desktop-interaction.spec.ts` (T001, AC-003): 7
  cases — native caret/selection/undo, keyboard-only Translate/Rewrite
  completion with visible focus and no trap, composition guard (button
  disabled while composing, exactly one submit after explicit
  post-composition activation), exact-value real-clipboard copy on both
  pages, geometry at 1440×900 incl. effective-200% viewport and
  text-spacing overrides. Guard finding fixed in-test: activation is
  refused via disabled readiness state, not via ignored click.
- `frontend/tests/e2e/desktop-visual.spec.ts` + 3 committed
  `*-chromium-darwin.png` baselines (T002, AC-004): Translation ready,
  Translation oversize-error, registration long-validation at §4.4
  starting thresholds (0.1 / 0.001). Run-owned usage line pinned to a
  fixed representative value during capture (a mask hid text but not the
  layout shift from charge-dependent wrapping — found via 0.01-ratio
  failure, fixed, re-proven). Baselines render with host fallback fonts
  (Noto Sans/SC absent on this Mac): `*-darwin` provenance; Linux
  container re-capture with pinned Noto is a recorded follow-up, and
  human design-review approval is still pending (AC-004 human gate).
- `frontend/tests/e2e/desktop-a11y-scan.spec.ts` (T002, AC-005 automated
  part): dependency-free semantic/contrast scan over translate
  ready/oversize, rewrite ready, register-errors and login states plus a
  focus-visibility assertion; zero violations. Disabled controls are
  contrast-exempt per WCAG (found a 2.45:1 disabled-button reading,
  scoped out with rationale in-test).
- `docs/08-backlogs/M038/manual-desktop-procedures.md` (T004
  preparation): M-001…M-005 scripts, sign-off table (all BLOCKED —
  not executed), explicit gap list.
- T003 required no new journey code: `translate.spec.ts` /
  `rewrite.spec.ts` already prove AC-001/AC-002/AC-006 at 1440×900 with
  the documented real-auth fixture and ≥1 real `/login` case each; the
  full smoke re-proves them (74/74).

Evidence: `bash scripts/contract.sh check` → zero drift;
`bash scripts/frontend.sh smoke` → 74/74 Chromium published cases pass
with clean synthetic-credential scan (M028–M032 regressions included);
`bash scripts/verify-milestone.sh M038` → all gates pass. New-artifact
privacy: specs reference credentials via env only; PNG baselines carry
 synthetic fixtures (oversize `a`-runs, `not-an-email`/`short`/
`mismatch`) — `grep`/`strings` scans clean; committed PNGs reviewed in
-session for layout/state correctness (first pass only, not a design
sign-off).

AC status: AC-001/AC-002/AC-006 automated parts pass; AC-003 passes;
AC-004 automated capture passes, human design-review approval pending;
AC-005 automated scans pass, full manual subset pending; AC-007 passes
(privacy + zero drift). Milestone stays BLOCKED on T004/T005 human
gates; G3/RG-006 stay pending.

## Completion record (final — T005 sign-off, 2026-09-16 UTC)

Revision: `014fe56` plus the uncommitted tree at sign-off (narrowed
browser/AT requirements, `MonetaryAdmission` config-first default with
Development fail-fast guard and tests, NFR-007 nav icons with unit
assertions, re-locked manifests); environment macOS arm64, .NET SDK
10.0.302, Node v24.20.0, published Smoke host with deterministic
provider adapters.

Gates: `bash scripts/verify-milestone.sh M038` passed 2026-09-16 —
context check + audit, backend check + smoke, `contract.sh check` zero
drift, AI check + probe, frontend check (261 unit incl. new icon
assertions) + smoke with 74/74 Chromium cases (M028–M032 regressions
included, visual 3/3 within §4.4 thresholds), `git diff --check`.

Privacy: committed PNG baselines `strings`-clean (no account, password,
token or bearer material); specs and guard reference credentials via
env only; no new binary artifacts since the partial record.

Human gates: baseline design-review approved by Owner on 2026-09-16
(header with nav icons judged in the live dev server; committed
`*-darwin` baselines stand per thresholds). Readiness accepted by Owner
on 2026-09-16 with the recorded subset-plus-gaps satisfying AC-005's
manual part.

AC status at sign-off: AC-001/AC-002/AC-006 pass; AC-003 automated
contracts pass (keyboard-only portion now non-gating engine evidence
per retired UX-AC-079); AC-004 automated capture passes with design
approval; AC-005 automated scans pass with the owner-accepted manual
subset and explicit gaps (M-004 not executed, forced-colors/Edge/
previous-version/Safari steps not executed); AC-007 passes.

Still pending outside this milestone: G3/RG-006 on the Firefox/Chrome
mobile matrix (no M039 yet; one Firefox desktop submission run also
unrecorded); container-pinned (Noto Sans/SC) Linux re-capture as a
recorded follow-up.
