# M038 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Add a desktop evidence lane without touching product behavior or the
backend wire: V-010 focused Chromium-desktop browser contracts for
native interaction, keyboard, composition and geometry; a curated
three-baseline desktop screenshot harness with pinned capture and
reviewed baselines plus automated accessibility scans (V-011 automated
slice); dedicated-user published Translation/Rewriting journey cases
at 1440×900 reusing the real-auth fixture (V-012 desktop lane); and a
bounded desktop manual-evidence pass with named reviewers and explicit
gaps. AC-001/AC-002 carry the journey proof; AC-003 the native
contracts; AC-004/AC-005 the visual/scan/manual slice with human
sign-off; AC-006/AC-007 the E2E-auth, privacy and drift gates.
M028–M032 suites regression-guard functional behavior.

## Changes and order

1. V-010 desktop contracts (`frontend/tests/e2e/`, new
   `desktop-interaction.spec.ts` or sibling-focused files): native
   edit/caret/selection/undo checks, real-clipboard exact-value copy
   on both pages, keyboard-only journey completion with focus
   visibility and no-trap assertion, composition-guard cases (no
   submit during composition; one submit on explicit post-composition
   activation), desktop geometry (no horizontal overflow, no clipped
   controls) at 1440×900 plus 200% zoom and text-spacing overrides.
   Deterministic provider; semantic locators; explicit readiness, no
   timing sleeps.
2. V-011 automated slice: add screenshot-baseline harness to the
   published Playwright project (pinned Noto Sans/SC, container,
   scale factor 1, deterministic synthetic content/time, fonts
   awaited, caret hidden, motion off, controlled scrollbars; native
   select popups excluded) for the three desktop baselines with §4.4
   starting thresholds (per-pixel 0.1, max differing ratio 0.001);
   add an automated semantic/contrast scan step (no
   serious/critical). Any threshold/baseline change needs a measured
   reason plus human design-review approval — never auto-accept.
3. V-012 desktop lane: add/extend dedicated-user published E2E
   Translation and Rewriting journey cases at 1440×900 covering
   AC-001/AC-002 including oversize/invalid and controlled-failure
   preservation, reusing the documented real-auth Playwright fixture
   with ≥1 real `/login` form case; Smoke-only seeding, run-owned
   cleanup and password-absence scan unchanged.
4. Manual pass: prepare concrete desktop procedures (keyboard-only
   scripts, zoom/spacing/forced-colors/reduced-motion checklists,
   NVDA/Firefox and VoiceOver/Safari journey scripts, branded
   Chrome/Edge smoke with version recording) and execute with named
   reviewers; record actual versions, findings and every uncovered
   combination as an explicit gap. No backend, migration, DTO,
   OpenAPI or seed change. Regenerate client types and prove zero
   drift before adoption.

Migration/rollout/rollback: none — test-harness-only slice with no
schema, config or published-contract change. Roll back by reverting
the harness change set and deleting added baselines.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 desktop Translate journey | T003 | V-012 published E2E at 1440×900 in the publish smoke path; `scripts/frontend.sh` E2E gate | tasks.md completion record |
| AC-002 desktop Rewrite journey | T003 | V-012 published E2E at 1440×900 incl. Correction-only default and mode select | tasks.md completion record |
| AC-003 native/keyboard/composition/geometry | T001 | V-010 focused browser contracts in the published run; real clipboard/composition, no synthetic-event substitution | tasks.md completion record |
| AC-004 desktop baselines + review | T002 | Screenshot-baseline suite at starting thresholds + named human design-review sign-off; change log for any adjustment | tasks.md completion record |
| AC-005 scans + desktop manual subset | T002+T004 | Automated scan step (zero serious/critical) + executed manual procedures with reviewer sign-off and explicit gap list | tasks.md completion record |
| AC-006 E2E auth contract | T003 | Real `/login` case + fixture reuse + Smoke-only seeding + password-absence scan | tasks.md completion record |
| AC-007 privacy + zero drift | T005 | V-015 sentinel inspection of new artifacts + `scripts/contract.sh check` zero drift | tasks.md completion record |
| Regression M028–M032 | T005 | Rerun M028/M029/M030/M031/M032 published suites in the same smoke run; `scripts/frontend.sh check`, `scripts/backend.sh check` | tasks.md completion record |

## Context boundaries and risks

- Omitted domains and why: narrow/mobile lanes and devices
  (M039 owns them; opening here would exceed one bounded group);
  backend accounting/recovery/live provider/quality/perf (fake
  provider only per roadmap §4.5 note; regression-covered);
  deletion/backup retention (Q-004 remainder, launch-gated);
  safeguards/compat proposals (unadopted); email delivery (DF-008).
  Open on demand only if execution meets a handler need (reopens the
  contract-review gate) or a changed input.
- Dependencies: M014 Done and M032 Done (proven by
  delivery/current.md; specs referenced for exercised states, not
  re-proven). M028/M029 own journey correctness — this group adds
  desktop evidence, not new journey behavior.
- Assumptions: Chromium desktop lane stands in for engine-level
  automation only; branded browsers and AT need the arranged human
  access. Risks: reviewer/device access unavailable (→ explicit gap,
  RG-006 stays blocked); baseline flakiness (→ pinned capture plus
  measured threshold reasons, never auto-accept).
- Human actions: owner arranges Windows NVDA/Firefox, macOS
  Safari/VoiceOver and branded Chrome/Edge access at the start;
  reviewers execute the desktop manual subset and baseline
  design-review at the end; both sign-offs block their gates.
