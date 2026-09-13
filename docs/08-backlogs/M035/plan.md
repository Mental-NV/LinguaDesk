# M035 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Author, review and freeze release-corpus batch B1 (24 cases) as
versioned JSON under the evaluation tool, with a pinned schema
revision, offline MSTest validation and a recorded bilingual +
independent review approval per case. No runner behavior changes, no
prompt/pipeline/adapter edits, no live dispatches, no grading. The
frozen revision hash is the handoff M036 consumes; #6 corpus counts
stay unchanged.

Selected canonical excerpts arrive through the context packet. Do not
copy them all here. The essential local invariants: §5.1 field set on
every case (ID, provenance/approval, exact source, operation/settings,
canonical scalar count, expected eligibility, atomic assertions,
acceptable-output notes, coverage tags); author is never sole reviewer;
B1 disjoint from M015–M018 development slices; frozen means
content-hash pinned; no production workspace text anywhere.

## Changes and order

1. `backend/tools/LinguaDesk.Ai.Evaluation/Corpus/batch-b1.json`
   (new) — 24 frozen cases per AC-001/AC-002 with schema revision
   marker; Chinese-source cases script-labeled (≥1 Hans, ≥1 Hant);
   every tag class present per family. Depends on nothing; done when
   the file validates against the pinned schema (AC-004 first pass).
2. `backend/tests/LinguaDesk.Infrastructure.Ai.Tests/CorpusBatchB1Tests.cs`
   (new) — offline MSTest validation: schema/revision conformance,
   required fields, tag/script coverage matrix, ID uniqueness,
   disjointness from development slices, canonical-count
   recomputation. Depends on step 1; done when the suite passes with
   networking disabled and no credential read (AC-004).
3. Human review + freeze — bilingual authors prepare reference notes,
   independent reviewer approves each case, freeze record (content
   hash, schema revision, selection, approvals) written to
   `tasks.md` completion record; batch coverage matrix with
   per-cell successor gaps published in the freeze record. Depends on
   steps 1–2 (review may iterate on case text, each iteration
   revalidates); done when all 24 approvals recorded and the hash
   pinned (AC-003/AC-006).
4. Regressions + manifest check — `bash scripts/ai.sh check`,
   `bash scripts/backend.sh check`, `python3 automation/context.py
   check M035`, secret sweep over the new corpus/tests/records.
   Depends on step 3; done when all pass and the sweep is clean
   (AC-005).

No migration, rollout or rollback: no storage, config, serving or
wire change. No new package dependencies; corpus is data, tests use
existing MSTest references.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 | T001 | V-013: reviewer + offline count/direction/script audit against §5.1 composition table | freeze record case list |
| AC-002 | T001/T002 | V-013: `CorpusBatchB1Tests` required-field + tag-matrix cases | `artifacts/test-results/ai.trx` |
| AC-003 | T003 | V-013: recorded per-case approvals with reviewer identity/competence; author≠sole-reviewer assertion in freeze record | tasks.md completion record |
| AC-004 | T002 | V-007: `bash scripts/ai.sh check` with networking disabled; credential env stripped; hash-reproduction rerun | TRX + pinned hash |
| AC-005 | T004 | V-015: secret/production-text sweep over corpus, tests, records | sweep result in completion record |
| AC-006 | T003 | V-013: batch coverage matrix vs §5.1 full counts; `python3 automation/context.py check M035` | freeze record + check output |
| Regression | T004 | `bash scripts/backend.sh check` | task completion record |

## Context boundaries and risks

Omitted domains and why: UX/browser (no UI surface), API
handlers/auth/accounting (no serving integration), storage/migrations,
email, AI grading and output review (M036), qualification/G1 (M036 +
successors), performance workloads (M037/V-014), provider adapter and
credential/budget machinery (M019/M020, reused untouched), prompt and
checker revisions (frozen inputs; any change forces spec/plan impact
analysis). Open on-demand: M036 package when consumption starts; the
DeepSeek research snapshot only on adapter wire drift (not expected).

Dependencies and risks: M020 report workflow is the consumer contract
— B1 must match the case-identity conventions M036 will feed it, but
M020 semantics are not re-proven; reviewer availability is the critical
path — if bilingual/independent reviewers are unavailable, the freeze
(T003) blocks and no partial approval is claimed; case-text iteration
during review must re-run validation before freezing, otherwise the
pinned hash is meaningless. Post-freeze edits create a new reviewed
revision, never a silent fix.
