# M035 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: none (M020 Done; reviewers to be engaged during T003).
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T001 — Author `backend/tools/LinguaDesk.Ai.Evaluation/Corpus/batch-b1.json` (24 cases per AC-001/AC-002, schema revision marker); AC-001/AC-002; depends on none; done when the pinned-schema validation passes locally.
- [ ] T002 — Add offline `CorpusBatchB1Tests.cs` (schema, required fields, tag/script matrix, ID uniqueness, dev-slice disjointness, count recomputation); AC-002/AC-004; depends on T001; done when the suite passes with networking disabled and no credential read.
- [ ] T003 — Bilingual authoring + independent per-case review, freeze record with content hash, schema revision, selection, approvals and per-cell successor-gap matrix; AC-003/AC-006; depends on T002 (revalidates after every case-text iteration); done when all 24 approvals recorded and the hash pinned.
- [ ] T004 — Regressions (`ai.sh check`, backend check or present equivalent), `context.py check M035`, secret/production-text sweep; AC-005 + regressions; depends on T003; done when all pass and the sweep is clean.

## Completion record
Pending execution. Record per AC: revision/configuration, actual
command/procedure, environment, UTC timestamp, result/evidence link,
failures/fixes and limitations. No pasted command logs. The freeze
record (content hash, schema revision, case selection, reviewer
identity/competence per case, override rationale, successor-gap
matrix) lands here before the READY claim for M036 consumption.
