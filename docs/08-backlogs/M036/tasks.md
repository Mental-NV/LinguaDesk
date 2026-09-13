# M036 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: none (planning complete; owner budget + weakened-ground-truth acceptances required before T002 live run).
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T001 — Batch-input runner (`evaluate-batch`, hash gate, scripted offline B1, `ai.sh` surface); AC-001/AC-002; depends on none; done when scripted B1 passes offline with zero dispatches and a hash-mismatch fixture blocks.
- [ ] T002 — Budgeted live B1 run (`verify-access` + `evaluate-batch --live`, admitted dispatches/spend/deadline, quiescence recorded); AC-002; depends on T001 + owner budget/weakened-truth acceptances; done when all 24 rows are `live_qualification` within budget.
- [ ] T003 — Pinned-judge grading (blinded labels, calibration record, bounded retries); AC-003; depends on T001 (offline proof) and T002 (live grades); done when every successful output carries a reproducible grade record.
- [ ] T004 — Human output review (seeded 12-of-24 + all flags/mismatches, adjudication, disposition record); AC-004/AC-006; depends on T002/T003; done when disposition is recorded with identity/competence/rationale.
- [ ] T005 — Versioned §7.2 report, regressions (`ai.sh check`, backend check, `context.py check M036`), secret/production-text sweep; AC-005/AC-006; depends on T004; done when all pass and the sweep is clean.

## Completion record
Pending execution. No evidence yet.
