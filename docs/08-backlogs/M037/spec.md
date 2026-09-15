# M037 — Selected specification
Selected items: BI-037. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: one benchmark-harness rehearsal at 1/15 scale of the §6.1
workload — 24 measured requests at 4 concurrent operations through the real
published HTTP API boundary (owned Kestrel process on a loopback address,
migrated SQLite in an isolated directory, real local-account
authentication, durable admission/settlement, deterministic fake provider,
zero paid dispatches). Composition mirrors §6.1 structurally: Translation
12 requests (2 directions × 3 length bands × 2 per cell), Rewriting 12
requests (a declared subset of language/mode cells × 3 length bands, each
distinct source/settings combination repeated once with a fresh submission
identity), recorded interleave seed, automatic/manual source selection and
both Chinese scripts represented. The harness records submission-to-complete-
body timings with admission/eligibility/transformation/settlement stage
durations, full-denominator success-within-target counts, nearest-rank
p50/p95, per-route/language/mode distributions, and first-seen/repeated
plus observed hit/miss/unknown cost/cache cohorts, and emits one versioned
report carrying every §6.1 field with all observations preserved.

Dependencies: M026/M027 Done (real host/accounting pipeline with fake
provider — the HTTP boundary, auth and settlement semantics rehearsed here
are reused, not re-proven); M020 Done (versioned combined-report and
disposition-label conventions — reused for the rehearsal report, not
re-proven); verification §3.1 (owned Kestrel loopback host pattern —
reused). Q-005 stays open: workloads are specified, measurements pending;
this rehearsal is first executable §6.1 harness evidence, not a measurement.

Exclusions: the full 360-request live workload at 10 concurrent operations
(successor milestone under an owner-admitted budget); §6.2
maximum-length (L−1/L per direction/cell), fallback, throttling and
controlled-fault deadline evidence (successor); any NFR-002 pass/fail claim
or G2 discharge (a scripted rehearsal cannot discharge G2); live
provider/candidate qualification, cost qualification and serving
configuration (Q-001/Q-007 untouched — no live budget or cap is admitted);
prompt, checker, adapter, registry, handler, storage or UI changes;
sentence-alternatives timing (deferred DF-001, not an MVP gate).

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | The rehearsal drives exactly the declared 24-request composition through the real HTTP API boundary: owned loopback Kestrel host, migrated SQLite, real local-account auth, durable admission/settlement, deterministic provider; recorded interleave seed; every repeat uses a fresh submission identity; 4 operations in flight while work remains with the final drain disclosed; host-readiness requests (if any) predeclared and reported separately, never counted as measured requests | Verification §6.1; §3.1 host pattern; M026/M027 pipeline |
| AC-002 | Every measured request carries a submission-to-complete-body duration (clock starts at API submission, stops when the complete response body arrives, serving already committed success) plus admission/eligibility/transformation/settlement stage durations; a TestServer, standalone-runner or mocked-response timing is never presented as an API measurement | Verification §6.1; NFR-002/PRD performance targets |
| AC-003 | The report computes complete-success-within-target over the full denominator (failures/timeouts are not timely completions — infinite effective latency or equivalent full-denominator calculation), nearest-rank p50/p95, and per-route/language/mode distributions, applied separately to Translation and full Rewriting; no successful-only percentile is claimed; a composition-audit check fails the run on any missing band/cell, repeated identity, replayed completion reused as a generation sample, or dropped request | Verification §6.1; RG-003 |
| AC-004 | The report distinguishes first-seen/repeated cohorts and observed hit/miss/unknown provider-cache cohorts (insufficient evidence stays `unknown`, never assumed), asserts application/runner response reuse was disabled, and records per-request usage and reserved/actual/unresolved exposure; provider-managed caching remains allowed and is observed, not disabled | Verification §6.1 |
| AC-005 | The harness declares the full §6.1 composition rules it will enforce for the successor live workload (360 requests, 10 concurrent, 30 per Translation direction, 10 per Rewriting language/mode cell, 120 per length band, 180 distinct combinations × fresh-identity repeat, recorded seed, no silent warm-up discard, quota/budget exhaustion labeled incomplete/invalid with outcomes retained) and the rehearsal manifest maps each rule to its scaled rehearsal analogue or an explicit successor-only deferral | Verification §6.1; readiness §9 |
| AC-006 | One versioned rehearsal report holds every §6.1 field (deployment/endpoint/provider identity, configuration, selection/concurrency/timestamps, per-request outcomes with durations and cohorts, aggregations, spend/exposure totals); all observations preserved, failures retained, no text/secret leakage (CredentialRef + presence only, synthetic fixture text only); offline regression gates pass with zero paid dispatches | Verification §6.1/§7.2 shaped; V-014 |

## Constraints and decisions

Nonfunctional constraints: NFR-002 targets (Translation 95% within 10s,
30s overall; full Rewriting 95% within 10s, 30s overall; inputs ≤1,000
chars in the percentile workload) frame the harness math — the rehearsal
proves recording fidelity against a fast fake provider, never provider
speed or NFR-002 compliance; 4-concurrency is a rehearsal scale-down, not
an approved limit; FR-007 maximum lengths (5,000/2,000) bind §6.2
successor cases only (L+1 stays a deterministic rejection, never a paid
sample).

Clarification status: Q-005 remains open (full live workloads,
maximum-length/fallback/deadline evidence and G2 still pending); Q-001
(monetary cap, serving/billing arrangement) and Q-007 (attempt/deadline
policy beyond defaults) are untouched — no per-run inputs are admitted
because no paid dispatch occurs. No proposal is promoted and no deferred
behavior is revived.

Human gates: none. Rehearsal setup (isolated storage, test accounts,
loopback host, fake provider) is routine delegated execution; no owner
budget admission, reviewer time or manual evidence is needed. Unavailable
reviewers or credentials block nothing — the rehearsal must pass with the
credential environment stripped. The successor live workload's
owner-admitted budget, verified test accounts and capacity stay a future
blocked gate.
