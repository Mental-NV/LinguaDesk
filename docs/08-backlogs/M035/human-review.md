# M035 — Human review instruction (T003 blocker, batch B1 freeze gate)

Status: **WAIVED by owner decision 2026-09-13** (see Deviation record in [tasks](tasks.md)).
AI review substituted for all remaining slots; slots are labeled `AI-review` and never claim human
competence. M036 consumes the batch knowing the evidence is weakened (no native-speaker validation
for ro/zh, circularity risk, §5.1/§5.3 + PRD §8 non-compliance).

- Draft artifact (do not treat as frozen): `backend/tools/LinguaDesk.Ai.Evaluation/Corpus/batch-b1.json`
- Schema revision: `corpus-b1.v1`; FROZEN sha256 `aebdc2dc482487d726a55953f9de1a46d66aa09b6693e353696d50e16cb83012` (2026-09-13, under owner waiver; re-pin after any edit — any post-freeze change creates a NEW batch revision)
- Machine validation so far: `bash scripts/ai.sh check` 281/281 pass (incl. 10 new `CorpusBatchB1Tests`); see [tasks](tasks.md) completion record. Machine checks do NOT substitute for this review.
- Governing contract: [spec](spec.md) AC-001–AC-006, Verification `llm-evaluation.md` §5.1/§5.3, PRD Section 8 (90% gate, critical-error classes).

## 1. Who may review

Per AC-003 / §5.1 / §5.3 (author is never sole reviewer):

- Translation case: reference notes prepared by a bilingual author (source AND target); approved by one independent qualified reviewer who also understands source and target.
- Rewriting case: expectations prepared by a reviewer fluent in the language and requested intent; approved by one independent qualified reviewer.
- The case author cannot be its only release reviewer. A second qualified reviewer adjudicates any dispute and confirms every suspected critical-error note.
- Record per case: reviewer identity + competence (e.g. languages, role), rationale for any override, and the freeze approval. Current placeholders (`review.author` / `review.independentReviewer` = `pending-T003`) must all be replaced — 4 of 24 author slots done (Mental-NV, en/ru bilingual, 2026-09-13), 0 of 24 independent slots done.

## 2. What to check per case (all 24: 12 Translation, 12 Rewriting)

Open the JSON and for each case verify:

1. Composition (AC-001): Translation covers all 12 directed pairs exactly once; Chinese-source cases carry a script label with ≥1 Hans and ≥1 Hant. Rewriting has 3 per language (en, ru, ro, zh), each with correction-only plus two catalog modes; correction-only cells cover both already-correct and needs-minimal-correction text.
2. Required fields (AC-002): stable unique `b1-`-prefixed ID; provenance/license or author approval; exact source text; operation/settings; canonical scalar count (recomputed, not trusted); `eligible` expectation consistent with `ScalarInputPolicy`; atomic meaning/fact assertions; acceptable-output notes; ≥1 coverage tag, with every tag class (fidelity-risk, paragraph/list, instruction-as-content, upper-length-band) present in each family.
3. Reference quality (§5.1/§5.3, PRD §8): assertions are atomic and checkable; acceptable-output notes anticipate valid variation (a reference is an example, not an exact-match oracle); every suspected critical error (invented fact, material omission, meaning reversal incl. negation flip such as "must not"→"must", changed number/date such as 14:30→15:30, wrong output language) is flagged and confirmed by the second reviewer. Natural date/number formatting or transliteration preserving meaning is not automatically critical; script/mode violations still fail their dimension.
4. Boundaries (AC-004/AC-005): ID and source text byte-disjoint from the M015–M018 development/calibration slices (no reuse); no production workspace text, secrets, keys, or fingerprints in cases, notes, or records (the known benign `secretariatul` substring in the Romanian source is acceptable — confirm nothing else matches); Chinese cases carry the Simplified-output policy note.

Reject (do not approve) any case failing the above. Fix the case text/notes, then require revalidation before freezing (step 4).

## 3. How to approve and freeze

1. Edit `review.author` / `review.independentReviewer` in `batch-b1.json` from `pending-T003` to the real identities/competence, with override rationale where applicable.
2. Demand a re-run after every case-text iteration: `bash scripts/ai.sh check` with networking disabled and credential env stripped (hash-reproduction rerun), plus the secret/production-text sweep. Any validation failure blocks the freeze.
3. Pin the freeze in [tasks](tasks.md) completion record: final content hash, schema revision, case selection, all 24 approvals, and the per-cell successor-gap matrix (draft matrix is already there — confirm or update it: Translation 1/20 per direction; Rewriting per language/mode as recorded; #6 counts 600 quality / 80 eligibility unchanged, no failing-case removal).
4. Hand the frozen hash to M036. Rule: any post-freeze case change creates a NEW reviewed batch revision — never a silent edit.

## 4. Out of scope for this review

AI grading, grader calibration, human review of model outputs, and qualification dispositions belong to M036/G1 — do not grade model outputs here. Prompt/checker/adapter changes, live provider dispatches, credentials, and spend are excluded (offline-only work).

## 5. Done criteria

- [x] 4 of 24 cases carry human bilingual preparation (Mental-NV, en/ru); remaining 20 carry labeled AI review (waiver, see [tasks](tasks.md) Deviation record).
- [x] No disputes; one AI review finding (`b1-rw-ru-academic` grammar) fixed with rationale in [tasks](tasks.md); 20 independent slots explicitly waived, not adjudicated.
- [x] Post-review validation + sweep green (`ai.sh check` 281/281, backend 517/517, sweep clean); freeze hash `aebdc2dc…` pinned in `tasks.md`; T003/T004 checked on the frozen revision under waiver.
