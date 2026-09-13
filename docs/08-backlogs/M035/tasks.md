# M035 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: done under owner waiver (see Deviation record). Batch B1 frozen at sha256 `aebdc2dc482487d726a55953f9de1a46d66aa09b6693e353696d50e16cb83012`; M036 may consume knowing the evidence is weakened.
Last check: `bash scripts/ai.sh check` 281/281 pass; `bash scripts/backend.sh check` 517/517 pass; `python3 automation/context.py check M035` flags only the expected mutable `delivery/current.md` status-row edit (reviewed: dependency facts M020 Done and train M035→M036→M037 unchanged, no locked spec/plan/manifest file touched, no re-lock). Changed scope: none (uncommitted: `backend/tools/LinguaDesk.Ai.Evaluation/Corpus/batch-b1.json`, `backend/tests/LinguaDesk.Infrastructure.Ai.Tests/CorpusBatchB1Tests.cs`, `docs/08-backlogs/M035/tasks.md`, `docs/delivery/current.md`; runner owns commits).

## Deviation record (2026-09-13, owner decision)

Owner waived the AC-003 two-human-reviewer gate: no bilingual/independent human reviewers are
available, so AI review is substituted for all remaining slots. 4 cases keep their human bilingual
author approval (Mental-NV, en/ru); the other 20 author slots and the 24 independent slots were never
human-reviewed. All AI review slots are labeled `AI-review` with session identity — they do not claim
human competence. Consequences: (a) non-compliance with Verification llm-evaluation.md §5.1/§5.3 and
PRD Section 8 human-review clauses; (b) circularity risk — AI approves the ground truth that M036 will
use to grade AI outputs; (c) no native-speaker validation for ro/zh cases. M036 consumes this batch
knowing the qualification evidence is weakened. This waiver was an explicit owner decision, not a
silent edit; AC-003 text below is kept intact.

## Ordered tasks
- [x] T001 — Author `backend/tools/LinguaDesk.Ai.Evaluation/Corpus/batch-b1.json` (24 cases per AC-001/AC-002, schema revision marker); AC-001/AC-002; depends on none; done when the pinned-schema validation passes locally.
- [x] T002 — Add offline `CorpusBatchB1Tests.cs` (schema, required fields, tag/script matrix, ID uniqueness, dev-slice disjointness, count recomputation); AC-002/AC-004; depends on T001; done when the suite passes with networking disabled and no credential read.
- [x] T003 — Bilingual authoring + independent per-case review, freeze record with content hash, schema revision, selection, approvals and per-cell successor-gap matrix; AC-003/AC-006; depends on T002 (revalidates after every case-text iteration); done when all 24 approvals recorded and the hash pinned. DONE UNDER WAIVER: 4 human author approvals (Mental-NV, en/ru: b1-tr-en-ru, b1-tr-ru-en, b1-rw-en-correctionOnly, b1-rw-en-simple); 20 author + 4 independent slots AI-reviewed (labeled `AI-review`, Muse Spark session hill-meridian); 20 independent slots `waived-T003-deviation`. One review-driven fix (b1-rw-ru-academic grammar, count 1507→1498) revalidated before freezing. Freeze hash pinned below.
- [x] T004 — Regressions (`ai.sh check`, backend check or present equivalent), `context.py check M035`, secret/production-text sweep; AC-005 + regressions; depends on T003; checks run green on the draft revision but must rerun after any review-driven case-text change and after the freeze. DONE: `ai.sh check` 281/281 pass; `backend.sh check` 517/517 pass (first attempt hit a transient `dotnet restore` segfault, clean on retry); `context.py check M035` flags only the expected mutable `delivery/current.md` status-row edit; secret sweep clean (0 sentinel matches in corpus/tasks/human-review).

## Completion record
Revision: 17d7b16 plus uncommitted T001/T002 files (no commit per runner ownership).
Environment: .NET SDK 10.0.302; `ai.sh check` runs offline (credential env stripped, proxies forced to 127.0.0.1:1).
UTC: 2026-09-13T15:48:55Z.
Draft batch (NOT frozen, NOT approved): `backend/tools/LinguaDesk.Ai.Evaluation/Corpus/batch-b1.json`,
schema revision `corpus-b1.v1`, 24 cases (12 Translation one per directed pair; 12 Rewriting three per language
with correction-only plus two catalog modes), sha256 `aebdc2dc482487d726a55953f9de1a46d66aa09b6693e353696d50e16cb83012`
(FROZEN 2026-09-13 under owner waiver; interim revisions `ff888a7b…` (draft) and `41918cad…`
(4 human approvals) superseded). AI review finding fixed before freeze: `b1-rw-ru-academic` source
carried 6 unacknowledged Russian agreement errors (`составила`→`составило` ×3 incl. obs 3/5/6,
`составила`→`составил` obs 4, `21/22/23/24 дворов`→`двора`, `1/2/3/4 процентов`→`процента`);
academic-mode sources must be clean or grading is contaminated by silent grammar correction.
Canonical scalar count recomputed 1507→1498; all other 23 cases approved as-is (incl. `b1-rw-ro-business`
`scade pe 30 martie` phrasing and `b1-tr-ro-zh` "32 minutes archived" = `procese-verbale`, both judged
meaning-preserving).
Generator: `/tmp/gen_b1.py` (throwaway; recomputes canonical scalar counts, asserts length bands).

Per AC:
- AC-001 — Machine portion verified by `CorpusBatchB1Tests` (exact count, all 12 directed pairs, both Chinese
  scripts across zh-source cases: Hans on zh-hans-en/zh-hans-ro, Hant on zh-hant-ru; 3 rewriting modes per language
  incl. correction-only; IDs unique, `b1-`-prefixed, disjoint from M015–M018 development slices). Approval portion pending T003.
- AC-002 — Verified: every case carries provenance/CC0, exact source, operation/settings, recomputed canonical scalar
  count, `eligible` expectation consistent with `ScalarInputPolicy`, atomic assertions, acceptable-output notes and
  ≥1 coverage tag; every tag class (fidelity-risk, paragraph/list, instruction-as-content, upper-length-band)
  present in each family. Full per-direction/per-cell matrix explicitly pending successors (see gap matrix).
- AC-003 — DONE UNDER OWNER WAIVER (Deviation record above). 4 of 24 human author approvals (Mental-NV,
  en/ru bilingual, 2026-09-13); remaining 20 author + 4 independent slots AI-reviewed and labeled `AI-review`;
  20 independent slots `waived-T003-deviation`. No human freeze approval exists; the waiver is the freeze
  authority. M036 must treat human-review clauses of §5.1/§5.3 and PRD §8 as not satisfied.
- AC-004 — Verified offline with zero provider dispatches and no credential read: `bash scripts/ai.sh check`
  281/281 pass (`artifacts/test-results/ai.trx`), including 10 new `CorpusBatchB1Tests`. Failures fixed during
  execution (test-only defects: disposed `JsonDocument`, MSTest analyzer assertion forms, two swapped bound/value
  argument orders); corpus needed one fix (invalid `24:30` time in the long ro-zh case, now a valid two-day program).
- AC-005 — Sweep clean: `batch-b1.json` contains no secret sentinels (single `secret` substring is Romanian
  `secretariatul`); all 24 sources are original synthetic text authored for this batch, no production workspace text
  imported; records carry hashes/selections only. Test-file `secret` matches are its own sentinel list and method names.
- AC-006 — Draft coverage matrix below; #6 corpus counts (600 quality / 80 eligibility) unchanged, no failing-case
  removal. M036 MUST NOT consume this revision until T003 records all 24 approvals and pins the freeze hash; any
  post-review case-text change creates a new reviewed revision, never a silent edit.

Draft coverage matrix (B1 → remaining gap to §5.1 full corpus):
- Translation per direction: 1/20 each (gap 19 per direction).
- Chinese-source scripts: zh→en Hans 1 (gap 9 Hans + 10 Hant); zh→ru Hant 1 (gap 10 Hans + 9 Hant); zh→ro Hans 1 (gap 9 + 10).
- Rewriting per language/mode cell: en correctionOnly/simple/business 1 each (other 6 modes 0); ru
  correctionOnly/simple/academic 1 each (other 6 modes 0); ro correctionOnly/casual/business 1 each (other 6 modes 0);
  zh correctionOnly(Hans)/simple(Hant)/friendly(Hans) 1 each (other 6 modes 0). Gap per covered cell: 9; per uncovered cell: 10.
- Correction-only subtypes: already-correct text covered by b1-rw-ru-correctionOnly; needs-minimal-correction covered by
  b1-rw-en-correctionOnly, b1-rw-ro-correctionOnly and b1-rw-zh-hans-correctionOnly.
- Eligibility/boundary 80 cases and development/calibration disjointness: B1 adds 0 eligibility cases (pending successors);
  B1 IDs and source texts verified byte-disjoint from the M015–M018 development slices.
- Limitations: upper-length-band represented by one 3945-scalar translation case (limit 5000) and one 1507-scalar
  rewriting case (limit 2000); full L−1/L maximum-length evidence belongs to later workloads, not this batch.
