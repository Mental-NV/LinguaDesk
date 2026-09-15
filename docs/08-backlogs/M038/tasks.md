# M038 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: arrange desktop manual-review access (Windows NVDA/Firefox, macOS Safari/VoiceOver, branded Chrome/Edge) before T004.
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T001 — Add V-010 desktop browser contracts (native edit/caret/selection/undo, real-clipboard copy, keyboard-only journeys, composition guard, desktop geometry/zoom/spacing); AC-003; depends on none; done when focused contracts pass in the published run.
- [ ] T002 — Add three-baseline desktop screenshot harness with pinned capture plus automated semantic/contrast scans; AC-004/AC-005 (automated part); depends on T001; done when baselines pass at starting thresholds with zero serious/critical scan findings.
- [ ] T003 — Add/extend dedicated-user published desktop E2E Translation/Rewriting journeys at 1440×900 with ≥1 real `/login` form case and real-auth fixture reuse; AC-001/AC-002/AC-006; depends on T001; done when published suite passes with a clean password scan.
- [ ] T004 — Prepare and execute desktop manual procedures (keyboard-only, zoom/spacing/forced-colors/reduced-motion, NVDA/Firefox and VoiceOver/Safari journeys, branded Chrome/Edge smoke) with reviewer sign-off and explicit gap list; AC-005 (manual part); depends on T003; done when sign-off and gaps are recorded.
- [ ] T005 — Prove new-artifact privacy, `contract.sh check` zero drift, M028–M032 regressions and readiness review (baseline design-review approval, coverage of every AC); AC-007 plus gate; depends on T004; done when all gates pass and the completion record is written.

## Completion record
Pending execution.
