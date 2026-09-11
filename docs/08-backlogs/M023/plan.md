# M023 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Extend the M021/M022 accounting slice with a monetary ceiling enforced
before any paid dispatch. A new internal `MonetaryAdmissionService`
owns per-attempt cost admission and reconciliation; the character
admission and settlement services keep owning their ledgers and the
endpoints keep owning HTTP mapping. Each attempt reserves its
`BillingProfile`-derived upper bound in integer minor units against one
configured monthly cap; the ceiling equation, per-attempt month stamp
and conservative carryover arrive through the context packet and are
not recopied here. No product cap amount is decided: the slice adds
explicit `MonetaryAdmission` configuration (cap minor units, currency)
with startup validation, and tests use small configured caps. No
dispatch occurs in this slice, so nothing can spend real money.

## Changes and order

1. Persistence: new `MonetaryCostLedger` (one row per UTC cost month:
   known spend + unresolved exposure in minor units) and
   `MonetaryAttemptReservation` (attempt identity, operation reference,
   attempt number, bound minor units, cost month, tariff reference,
   reserved/settled/released state) via a reviewed additive EF
   migration on the M022 chain; extend `LinguaDeskDbContext` with the
   same key-length conventions as the character entities. Done when
   model-drift/initialization checks pass.
2. Options: new `MonetaryAdmissionOptions` (`SectionName =
   "MonetaryAdmission"`: positive `MonthlyCapMinorUnits`, non-empty
   `Currency`), bound from non-secret configuration with an
   `IValidateOptions` startup validator following the
   `OperationAdmissionOptionsValidator` precedent; unconfigured or
   invalid values fail closed (paid admission denied; startup
   validation fails). Wire local/test configuration with small
   explicit caps. Done when options-validation unit tests pass.
3. Admission service: `known + unresolved(all carried months) + new
   bound <= cap` checked and reserved in one short transaction;
   per-attempt cost month from the server transaction clock; upper
   bound computed from the attempt's `BillingProfile` + finite
   `ContextBounds` with ceiling rounding; unknown/missing/unbounded
   profile input returns ineligible with no writes; duplicate attempt
   identity observes the stored reservation. Done when policy unit
   tests pass with no HTTP/DB dispatch beyond the service's own store.
4. Reconciliation: conditional `UPDATE ... WHERE State = 'reserved'`
   claiming the transition, then exposure→known-spend move (settled
   actual capped at the reservation) or exposure decrement (released)
   in the same short transaction; same-evidence duplicates return the
   stored outcome; missing/inconsistent evidence retains the full
   reservation; settled attribution never leaves its cost month. Done
   when policy unit tests pass.
5. Endpoints + contract/privacy: map denial/ineligible outcomes to the
   existing 503 monetary-suspension and 422/503 problem shapes with no
   monetary values in bodies; extend C# DTOs/metadata only if a shape
   must change, regenerate and review the OpenAPI delta plus
   TypeScript declarations with the drift gate; verified/unverified/
   anonymous matrix on touched reads; sentinel sweep over tables,
   bodies, logs and reports. Concurrency (parallel admissions at the
   ceiling), month-advanced, and two-process restart cases over
   file-backed SQLite; zero-dispatch counters on all read paths. Done
   when real-HTTP/file-backed cases, `contract.sh check` and sweeps
   pass.
6. Regressions and lock: `backend.sh check`, `contract.sh check`,
   `context.py check M023` green with revision/environment/UTC time in
   the completion record.

No migration/rollout beyond the additive migration; single-instance
SQLite assumptions unchanged. Midnight/month boundaries use the server
transaction clock; reservations are never moved implicitly with the
wall clock.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 ceiling admit/deny + 503 | T003, T005 | `bash scripts/backend.sh check` (new `MonetaryAdmissionTests`: fit-admits, over-cap 503, no-write-on-deny, reads-available); V-006 | tasks.md completion record |
| AC-002 unknown-price ineligible | T003 | `bash scripts/backend.sh check` (missing-profile, unbounded-scope denials, zero exposure); V-006 | tasks.md completion record |
| AC-003 concurrent ceiling race | T003 | `bash scripts/backend.sh check` (parallel admissions at ceiling, cap-total invariant); V-006 | tasks.md completion record |
| AC-004 settle-once + release | T004 | `bash scripts/backend.sh check` (duplicate-settlement idempotent, no double count, release-to-zero floor); V-006 | tasks.md completion record |
| AC-005 missing-evidence retention | T004 | `bash scripts/backend.sh check` (timeout/cancel/reject without evidence keeps exposure); V-006 | tasks.md completion record |
| AC-006 cross-month carryover | T003, T004 | `bash scripts/backend.sh check` (clock advanced past month end: carryover blocks, new bucket opens, original-month attribution); API-AC-010 portion; V-006 | tasks.md completion record |
| AC-007 restart + zero dispatch | T003 | `bash scripts/backend.sh check` (two-process restart proof, duplicate observes, dispatch counters); V-006 | tasks.md completion record |
| AC-008 contract + privacy | T005 | `bash scripts/contract.sh check`, sentinel sweep; V-009/V-015 | tasks.md completion record |
| M021/M022 regression intact | T006 | `bash scripts/backend.sh check` full suite green; `python3 automation/context.py check M023` | tasks.md completion record |

## Context boundaries and risks

Irrelevant domains and why: UX/frontend lanes (no UI surface changes;
error JSON only) — reopen for M028+; LLM dispatch/fallback traversal
(zero provider dispatch in this slice; `BillingProfile` consumed only
as bound input) — reopen for M026/M027; character settlement and
interrupted/unknown recovery mapping (M022/M024 own them; their §6.1/
§7.1/§7.2 portions are reference-only); corpus/eval and performance
workloads (M035+/M037) — no qualification claim here; email/devices/
manual evidence — none required. On-demand sources: M021/M022 packages
(admission/settlement semantics), arch §7.2 failure windows (exposure
retention rationale), API §5.2/§6.1 (if settlement interplay arises).

Dependencies, assumptions, blockers: M021/M022 Done assumed (verified
in delivery/current.md and code: `OperationAdmissionService`,
`OperationSettlementService`, character ledgers). Assumption: no paid
serving exists yet, so the configured cap is test/local scaffolding,
not the Q-001 launch amount. Risk: concurrent-admission races under
SQLite single-writer — mitigated by the single-transaction
check-and-reserve with conditional writes, proven by the parallel
test. Risk: decimal→minor-unit rounding leaking fractions — mitigated
by integer-only ledgers with ceiling rounding at the bound computation
boundary. No human actions; no blockers.
