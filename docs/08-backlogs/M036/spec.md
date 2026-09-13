# M036 — Selected specification
Selected items: BI-036. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: one budgeted live evaluation of frozen release-corpus batch B1
(24 cases, schema `corpus-b1.v1`, sha256
`aebdc2dc482487d726a55953f9de1a46d66aa09b6693e353696d50e16cb83012`)
through candidate `DeepSeek-V4.1-Flash` (`deepseek-flash`,
`https://api.deepseek.com`, CredentialRef `deepseek`), primary-only
(candidate-only evaluation disables fallback rescue per §5.4; no
qualified fallback exists so none is configured). The run executes the
production eligibility/transformation pipeline per case under an
explicit dispatch/spend/deadline budget, records deterministic
findings, adds pinned-judge AI grades, completes seeded human review
with an explicit disposition, and emits one versioned §7.2 report with
`live_qualification` rows only.

Dependencies: M019 Done (registry, adapter, credential resolver,
budget semantics — reused, not re-proven); M020 Done (combined-report
workflow and `offline_fixture`/`live_development` disposition
boundary — reused, extended with `live_qualification`, not re-proven);
M035 Done under owner waiver (B1 input contract; 20/24 author slots
and 20/24 independent slots AI-reviewed, so ground truth is weakened
and §5.1/§5.3 + PRD §8 human-review clauses are not satisfied by the
input — this run records that limitation, it does not repair it);
AI §5/6 (profiles, bounds, traversal, deadlines); verification
§5.0/5.2/5.3/5.4 and §7.2 (grading, review, report); PRD Section 8
(critical-error classes) and NFR-001 (90% gate, not claimable at
n=1/direction — reported per direction, not asserted); Q-001/Q-007
(budget/deadline inputs admitted per run, product cap still unset);
Q-005 (this run is first executable grading/review evidence; the
question stays open until full corpus + qualification complete).

Exclusions: remaining full-corpus density (600 quality / 80
eligibility cases — pending successors, #6 counts unchanged);
qualification/G1 dispositions beyond this batch; API performance
workloads (M037/V-014); serving/API/UI/storage integration; prompt,
checker, adapter or registry changes (any change forces a new
candidate revision and rerun per §5.4); route-specific DF-004 chains;
thinking-mode, context-limit or price qualification.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | The run consumes exactly the frozen B1 revision: content hash verified equal to `aebdc2dc…` before dispatch; any mismatch blocks the run; any post-freeze case change would require a new reviewed batch revision, never a silent edit | M035 freeze record; verification §5.1/5.4 |
| AC-002 | All 24 cases execute live through the production pipeline under the admitted budget (dispatches, spend, §6 stage timeouts + finalization reserve); every observation row is labeled `live_qualification` with attempts, stage durations, token usage, reserved/actual/unresolved exposure and deterministic findings; zero fixture/scripted rows; per-run budget never exceeded; serving quiescence (or shared-scope concurrency) recorded | AI §5.2/5.4/6; verification §5.0/7.2; V-008 |
| AC-003 | Every successful quality output carries an AI grade from the pinned judge configuration and grading prompt with blinded candidate labels and recorded judge identity/limitations; calibration against human-labeled development examples is recorded; a missing/invalid grade stays unresolved and blocks the disposition — it is never silently defaulted or retried unboundedly | Verification §5.2/5.3; V-013 |
| AC-004 | Human review covers a seeded sample recorded before seeing results (12 of 24, stratified across scripts, lengths and fidelity risks, both Chinese scripts included) plus every critical-error flag, unusable output, eligibility mismatch and grader/deterministic disagreement; Translation reviewers understand source and target, Rewriting reviewers are fluent; a second qualified reviewer adjudicates disputes and confirms every suspected critical error; identity/competence, override rationale and unresolved findings are stored; an unresolved critical flag fails the batch | Verification §5.3; PRD Section 8 |
| AC-005 | One versioned report per §7.2 (case IDs, corpus hash, code/SDK/prompt/validator/settings revisions, candidate/profile/adapter/model/endpoint identity, CredentialRef + presence only, billing snapshot, selection/concurrency/timestamps, per-case outcomes, per-route/language aggregation with mode breakdowns, critical-error tracking, spend/exposure totals); no key, header, env dump, fingerprint, raw provider body, hidden reasoning, cached replay or production text; runner response reuse disabled | Verification §7.2; AI §9.2; V-013 |
| AC-006 | An explicit review disposition is recorded (bounded-batch evidence accepted / accepted-with-findings / failed with retained failures); failures and reruns are retained, never discarded; no per-direction 90% qualification is claimed at n=1; the gap to full corpus, full human coverage and G1 is stated with successor work identified | Verification §5.4/7.3; V-013 |

## Constraints and decisions

Nonfunctional constraints: explicit bounded live spend (per-run
admission at peak cache-miss rates, conservative fixed-point exposure,
unresolved exposure retained); live runs excluded from the ordinary
offline test target and never mixed with fixture rows; metadata-only
reports over synthetic fixtures; no production text anywhere.

Clarification status: Q-005 remains open (full corpus, per-candidate
human coverage, qualification complete only with successors); Q-001
(product monetary cap) and Q-007 (orchestration beyond §6 defaults)
are touched only as per-run admitted inputs. No proposal is promoted
and no deferred behavior revived.

Human gates: owner admits the live budget before T002 (blocked gate:
live run); bilingual/fluent reviewers + adjudicator complete T004
(blocked gate: disposition/report); owner accepts the
weakened-ground-truth limitation before T002 (M035 waiver carries
forward: AI-approved references grade AI outputs — reviewers must
re-validate suspect reference notes rather than assume the model is
wrong). Unavailable reviewers block T004; only an explicit owner
decision (as in M035) can waive a gate, never silence.
