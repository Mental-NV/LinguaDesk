# M035 — Selected specification
Selected items: BI-035. Status: ready; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: first release-corpus batch B1 — 24 frozen quality cases
(12 Translation, one per directed pair; 12 Rewriting, correction-only
plus two catalog modes per language) stored as versioned JSON with a
pinned schema revision, each case carrying the §5.1 field set, coverage
tags and review-approval metadata. Batch-level freeze record (content
hash, schema revision, case selection, reviewer approvals) that M036
consumes as its input contract. Offline machine validation of schema,
tag presence, script coverage, ID stability and disjointness from
development/calibration cases.

Dependencies: M020 Done (combined-report workflow and
`offline_fixture`/`live_qualification` disposition boundary — reused,
not re-proven); AI §5/9.2 (evaluation profiles/bounds ownership and
data boundary); verification §5.0/5.1 and V-013 (corpus-construction
portion only); PRD Section 8 (90% threshold, per-direction/per-language
denominators, critical-error classes constraining reference notes);
Q-005 (design specified; this batch is first executable evidence, the
question stays open until full corpus + qualification complete).

Exclusions: remaining full-corpus density (600 quality cases, 80
eligibility cases, 10/10 Hans/Hant per Chinese-source direction,
all 36 rewriting cells at full density — pending successor batches,
counts in #6 unchanged); AI grading, grader calibration and human
review of model outputs (M036); qualification dispositions (M036/G1);
API performance workloads (M037); serving/API/UI/storage integration;
any live provider dispatch, credential use or spend; prompt/checker
changes.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | B1 holds exactly 24 stable-ID quality cases: 12 Translation, one per directed pair, with both Chinese scripts present across the three Chinese-source cases (at least one Hans, one Hant, script labeled per case); 12 Rewriting, three per language (correction-only plus two catalog modes). IDs are stable, unique and never reused from development cases | Verification §5.1; V-013 |
| AC-002 | Every case records: provenance/license or author approval, exact source text, operation/settings, canonical scalar count, expected eligibility, atomic meaning/fact assertions, acceptable-output notes, and at least one coverage tag (fidelity-risk, paragraph/list, instruction-as-content, upper-length-band, tags may overlap). Every tag class appears at least once per family; the full per-direction/per-cell tag matrix is explicitly recorded as pending successors | Verification §5.1; V-013 |
| AC-003 | Translation reference notes are prepared by a bilingual author (source and target); Rewriting expectations by a reviewer fluent in the language and requested intent. The case author is never the only release reviewer: one independent qualified reviewer approves each case, adjudicates disputes and confirms every suspected critical-error note. Reviewer identity/competence, rationale for overrides and the freeze approval are recorded; an unapproved case is not frozen | Verification §5.1/5.3; PRD Section 8 |
| AC-004 | The frozen batch validates fully offline with zero provider dispatches and no credential read: schema/pinned-revision conformance, required-field presence, tag/script coverage matrix, Simplified-output policy note on Chinese cases, ID uniqueness/stability, and byte-level disjointness (no shared ID or source text) from the M015–M018 development/calibration slices. Any validation failure blocks the freeze | Verification §5.0/5.1; V-007 (corpus-validation portion) |
| AC-005 | No production workspace text is imported into any case or note. Reports, tests and review records carry content hashes/selections only where text would otherwise be duplicated, and contain no secrets, keys or fingerprints | AI §9.2; verification §7.2; V-015 |
| AC-006 | A batch coverage matrix maps B1 per Translation direction and per Rewriting language/mode cell, states the remaining gap to the §5.1 full corpus per cell, and confirms #6 counts unchanged (no corpus-size reduction, no failing-case removal). M036 consumes the frozen revision; any post-freeze case change creates a new reviewed batch revision, never a silent edit | Verification §5.1/5.4; V-013 |

## Constraints and decisions

Nonfunctional constraints: offline-only data work — no provider
calls, credentials, spend or deadlines; conservative handling of
source text (licensed/author-approved provenance only, no production
text); frozen means content-hash pinned — validation reruns must
reproduce the identical hash.

Clarification status: Q-005 remains open (full 600-case corpus,
grading rubric execution, per-candidate human coverage and measured
evidence complete only with M036 + successors); Q-001 (caps/serving)
and Q-007 (orchestration) are untouched by this batch. No proposal
is promoted and no deferred behavior revived.

Human gates: bilingual authors + one independent reviewer per case
(owner: evaluation reviewers, timing: end of execution, blocked gate:
freeze/approval). The batch is not ready for M036 until every case
carries its recorded approval.
