# M036 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Run frozen B1 through `DeepSeek-V4.1-Flash` primary-only and produce a
versioned `live_qualification` report with grades and a recorded human
disposition. The runner today cannot do this: `evaluate-*` commands
execute built-in development slices only, and no grading/judge
machinery exists — so this milestone adds a batch-input command plus a
pinned-judge grading step behind the existing budget/admission
boundary, then runs, reviews and reports exactly one batch.
Indispensable invariants: frozen-hash gate before dispatch; production
pipeline per case (eligibility then transformation, ≤2 dispatches, §6
timeouts); `live_qualification` labels with no fixture rows; pinned
blinded grading with calibration; seeded human sample + all flags;
§7.2 metadata-only report; failures retained; no qualification claim
beyond the batch. Excluded: prompt/checker/adapter/registry edits,
serving/API/UI/storage, performance workloads, further corpus batches.

Selected canonical excerpts arrive through the context packet. Do not
copy them all here.

## Changes and order

1. Batch-input runner (new `evaluate-batch --corpus <path> --profile
   DeepSeek-V4.1-Flash --max-dispatches <n> --max-spend-usd <m>
   [--deadline-ms <n>] [--output <path>]`, surfaced in `scripts/ai.sh`
   and the runner README): loads `Corpus/batch-b1.json` (schema
   `corpus-b1.v1`), asserts the frozen content hash equals the
   spec-pinned value before any dispatch (mismatch = blocked, exit 3),
   executes each case through the production pipeline with
   per-attempt admission at peak cache-miss rates, labels rows
   `live_qualification`, records attempts/stage durations/usage/
   reserved/actual/unresolved exposure and deterministic findings
   (eligibility match, assertion checks, output validity). Offline mode
   runs the same loader scripted (zero dispatches) for CI. Depends on
   nothing; done when scripted B1 passes offline with zero dispatches
   and a hash-mismatch fixture blocks. (AC-001/AC-002)
2. Pinned-judge grading step (runner-only dependencies, never serving
   deps; `Microsoft.Extensions.AI.Evaluation` Quality evaluators are
   the suitable option per §7.2): frozen judge configuration + grading
   prompt, blinded candidate labels, calibration record against
   human-labeled development examples, per-dimension 0–3 scores with
   critical-error flags, grader calls inside the declared paid-run
   bounds (no unbounded retry). Depends on step 1; done when every
   successful output from a scripted run carries a reproducible grade
   record with judge identity and calibration reference. (AC-003)
3. Budgeted live B1 run: `verify-access` (1 dispatch) then
   `evaluate-batch --live` under the owner-admitted budget. Sizing:
   24 cases × ≤2 dispatches = 48 + 1 access probe + judge calls;
   proposed admission `--max-dispatches 120 --max-spend-usd 8.00`
   with §6 defaults (5s eligibility, 10s transformation, 2s
   finalization reserve inside the 30s overall deadline) — owner
   confirms or amends the exact numbers before launch. Record serving
   quiescence (shared DeepSeek credential) or concurrent shared-scope
   use. Missing/blank credential blocks with zero dispatches (exit 3),
   never scripted substitution. Depends on steps 1–2 and the budget +
   weakened-ground-truth acceptances; done when all 24 rows are
   `live_qualification` within budget. (AC-002)
4. Human output review + disposition: seeded 12-of-24 sample (seed
   recorded before results are visible; stratified across scripts,
   lengths, fidelity risks, both Chinese scripts) plus every flag,
   unusable output, eligibility mismatch and grader/deterministic
   disagreement; qualified reviewers per §5.3 competence rules, second
   reviewer adjudicates and confirms suspected criticals; identity/
   competence/rationale/disposition stored; unresolved critical flag =
   batch fails. Suspect reference notes are re-validated (weakened
   ground truth), not assumed correct. Depends on step 3; done when
   the disposition record is complete. (AC-004/AC-006)
5. Versioned §7.2 report, regressions and manifest check: report file
   under `artifacts/` with manifest/case/aggregation/spend sections
   per AC-005; `bash scripts/ai.sh check`, `bash scripts/backend.sh
   check`, `python3 automation/context.py check M036`, secret/
   production-text sweep over new code and reports. Depends on step 4;
   done when all pass and the sweep is clean. (AC-005)

No migration, rollout or rollback: no storage, config, serving or
wire change. New runner-only grading packages (if adopted) stay out
of the serving library.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 | T001 | V-013: hash-gate unit + offline scripted B1 pass; `bash scripts/ai.sh check` (networking disabled, credential env stripped) | `artifacts/test-results/ai.trx` + hash assertion |
| AC-002 | T002 | V-008: `verify-access` then `evaluate-batch --live` within admitted budget; audit `live_qualification` labels, dispatches ≤ cap, spend ≤ cap, usage known | versioned report file |
| AC-003 | T003 | V-013: pinned judge config/prompt review + calibration record; reproducibility rerun of grade records | judge pin + calibration record |
| AC-004 | T004 | V-013: seeded-sample + all-flags review records with reviewer identity/competence; adjudication of disputes/criticals | disposition record |
| AC-005 | T005 | V-013/V-015: §7.2 field audit of the report; secret/production-text sweep over code + reports | report file + sweep result |
| AC-006 | T004/T005 | V-013: disposition record with retained failures and explicit G1/successor gap | tasks.md completion record |
| Regression | T005 | `bash scripts/backend.sh check`; `python3 automation/context.py check M036` | task completion record |

## Context boundaries and risks

Omitted domains and why: UX/browser (no UI surface); API
handlers/auth/accounting (no serving integration); storage/migrations;
email; performance workloads (M037/V-014); route-specific DF-004
chains; thinking-mode/context-limit/price qualification. Open
on-demand: M037 package when workloads start; DeepSeek research
snapshot only on adapter wire drift (not expected).

Dependencies, assumptions, blockers, human action: M019/M020
machinery reused untouched (regression, not re-proof); B1 consumed
knowingly weakened (M035 waiver) — assumption: owner accepts this
before T002, else the live run blocks. Reviewer availability is the
critical path (M035 had none): if bilingual/fluent reviewers plus
adjudicator are unavailable, T004 blocks and no partial review is
claimed. Live credential is present in this environment (presence
only, value never recorded); budget admission and serving-quiescence
statement are still required per run. Grader choice assumes the
Microsoft evaluation packages fit the runner; if not, an equivalent
pinned blinded judge with calibration is substituted with recorded
reason — silent unblinded grading is never acceptable.
