# M025 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Complete the §7.3 usage-reporting contract over the unchanged
M022/M023/M024 accounting core. A new `GET /api/usage` read and the
existing submit/duplicate/status responses share one consistent
snapshot reader that returns the current-day ledger values, the
durable revision and a categorical availability signal in a single
logical read. Admission-day settlement, cost-month attribution,
conditional transitions and fencing stay exactly as M022–M024
built them; this slice only makes their cross-period results
authoritatively readable and ordered. The §7.1/§7.2/§7.3 period
rules and §8 categories arrive through the context packet and are
not recopied here. No dispatch occurs in this slice.

## Changes and order

1. Contract: extend `UsageSnapshot` in `OperationsContract.cs` with
   a categorical availability signal (wire values fixed once here,
   e.g. available/user-exhausted/service-exhausted/
   monetary-suspended/unavailable; no global counts or monetary
   amounts anywhere in the shape), keeping `Day`, `ResetAtUtc`,
   consumed/reserved/allowance/available and `Revision`. Add the
   `GET /api/usage` response shape reusing `UsageSnapshot`.
   Regenerate and review the OpenAPI delta plus TypeScript
   declarations with the drift gate. Done when `contract.sh check`
   passes with zero drift and no-store preserved on the new read.
2. Snapshot reader: extend `LedgerSnapshot.ReadAsync` (or a
   co-located reader reusing its query pattern) to compute
   availability from the same consistent read — user ledger,
   global ledger, monetary ceiling state (known + unresolved vs
   configured cap), and storage health — returning unavailable
   rather than values when the store cannot be read. Wire the
   reader into `OperationAdmissionService` submit/duplicate/status
   paths and the new usage endpoint. Reads never write ledgers,
   advance the revision, reserve exposure or dispatch work. Done
   when policy unit tests prove the availability matrix with no
   HTTP/DB dispatch beyond the reader's own store.
3. Usage endpoint: new `GET /api/usage` in `OperationsEndpoints.cs`
   behind the verified-account policy (unverified 403 via policy,
   anonymous 401, `CacheControl: no-store`, query string rejected
   like the status read), returning the authoritative snapshot and
   mapping storage failure to 503 availability without asserting
   any usage value. Done when real-HTTP/file-backed cases pass
   with zero-dispatch counters on the read path.
4. Cross-period tests: new `OperationUsageTests` proving AC-001
   (snapshot fields + availability matrix, privacy of global
   counts/amounts), AC-002 (admit before / settle after midnight:
   charge on admission day, new day untouched, fresh new-day
   snapshot, stale-charge disclosure), AC-005 (post-midnight
   duplicate/status reuses original day with fresh snapshot and no
   ledger change) and AC-006 (interrupted/failed across midnight:
   zero charge, original day, fresh snapshot; unknown 404 asserts
   nothing; expired 410). Fake `TimeProvider` moves the server
   clock; no wall-clock dependence. Done when the new tests pass
   over file-backed SQLite.
5. Ordering/month/restart tests: AC-003 (prior-month unresolved
   exposure keeps new-month admission denied with suspended
   availability; settle-once attribution in original month;
   character snapshot rolls correctly), AC-004 (day-first then
   revision-second ordering incl. newer-day-smaller-consumed
   acceptance, monotonic revision across operations/settlements/
   recovery/restarts, exact JSON round-trip ordering, no
   cross-account comparison) and AC-007 (two-process restart
   preserving both days' ledgers, revision and availability with
   consistent post-restart reads). Done when the new tests pass.
6. Regressions and lock: `backend.sh check`, `contract.sh check`,
   sentinel sweep over tables/bodies/logs/reports, `context.py
   check M025` green with revision/environment/UTC time in the
   completion record.

No migration/rollout beyond additive shape changes; single-instance
SQLite assumptions unchanged. Midnight/month boundaries use the
server transaction clock; reservations and charges are never moved
implicitly with the wall clock. The usage read creates no
operation, reservation or identity record.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 snapshot + availability matrix | T004 | `bash scripts/backend.sh check` (new `OperationUsageTests`: fields, four availability states, privacy of counts/amounts); V-005/V-006 | tasks.md completion record |
| AC-002 cross-midnight settle + fresh snapshot | T004 | `bash scripts/backend.sh check` (fake-clock midnight settlement, admission-day charge, new-day snapshot); API-AC-009 portion; V-005 | tasks.md completion record |
| AC-003 cross-month carryover + availability | T005 | `bash scripts/backend.sh check` (unresolved-exposure denial, original-month settle-once, day rollover); API-AC-010 portion; V-006 | tasks.md completion record |
| AC-004 day/revision ordering + JSON exactness | T005 | `bash scripts/backend.sh check` (ordering rules, monotonic revision, round-trip); API-AC-011 portion; V-005 | tasks.md completion record |
| AC-005 duplicate/status reuse original period | T004 | `bash scripts/backend.sh check` (post-midnight duplicate, 409 on changed payload, no ledger change); API-AC-004 portion; V-005 | tasks.md completion record |
| AC-006 interrupted/failed across midnight | T004 | `bash scripts/backend.sh check` (zero charge, original day, fresh snapshot, 404/410 mapping); API-AC-006/007 portions; V-005 | tasks.md completion record |
| AC-007 restart + zero dispatch | T005 | `bash scripts/backend.sh check` (two-process restart proof, consistent reads, dispatch counters); V-005/V-016 | tasks.md completion record |
| AC-008 contract + privacy | T001 | `bash scripts/contract.sh check`, sentinel sweep; V-009/V-015 | tasks.md completion record |
| M022/M023/M024 regression intact | T006 | `bash scripts/backend.sh check` full suite green; `python3 automation/context.py check M025` | tasks.md completion record |

## Context boundaries and risks

Irrelevant domains and why: UX/frontend lanes (no UI surface
changes; JSON contract only, display follows in M028+) — reopen
for M028+; LLM dispatch/fallback traversal (zero provider dispatch
in this slice; reads never regenerate output) — reopen for
M026/M027; settlement/admission/recovery internals (M022/M023/M024
own them; their services are reused unchanged and their
§6.1/§7.1/§7.2/§5.2 portions are reference-only);
corpus/eval and performance workloads (M035+/M037) — no
qualification claim here; email/devices/manual evidence — none
required. On-demand sources: M022/M023/M024 packages (if
settlement/exposure interplay arises), API §8 (if a new
availability distinction arises).

Dependencies, assumptions, blockers: M022/M023/M024 Done assumed
(verified in delivery/current.md and code:
`OperationAdmissionService`, `OperationSettlementService`,
`MonetaryAdmissionService`, `OperationRecoveryService`,
character/monetary ledgers, revision counter, status routes).
Assumption: no paid serving exists yet, so monetary-suspension
availability is test/local scaffolding under the Q-001 open cap
question; tests use small explicit configured caps. Risk: revision
read racing a concurrent write — mitigated by the single-writer
short-transaction precedent, proven by concurrent admission/
settlement tests asserting one monotonic advance each. Risk:
availability signal drifting from denial categories — mitigated by
computing both from the same consistent read, proven by paired
snapshot/denial assertions. No human actions; no blockers.
