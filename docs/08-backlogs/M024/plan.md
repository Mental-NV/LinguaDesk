# M024 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Extend the M022/M023 operation slice with terminal `interrupted`
recovery mapping. A new internal `OperationRecoveryService` owns the
pending→interrupted finalization; the settlement service keeps owning
success/failure transitions and the endpoints keep owning HTTP
mapping. Recovery finalizes only abandoned work — restart-orphaned
records and deadline-passed pending records found by a bounded
startup reconciliation scan — in one short transaction that releases
the character reservation (zero charge), advances the revision and
leaves M023 monetary exposure unresolved. The §5.2 identity-state
table, §6.1 finality rows and §7.2 failure windows arrive through the
context packet and are not recopied here. A disconnect never
finalizes: unexpired pending work stays committable. No dispatch
occurs in this slice, so nothing can spend real money or regenerate
lost output.

## Changes and order

1. State + persistence: add `OperationStates.Interrupted` alongside
   the existing pending/succeeded/failed values; the state column
   already stores short strings, so no migration is expected — verify
   with the model-drift check and add a reviewed additive migration
   only if the model snapshot requires one. Done when drift checks
   pass with interrupted round-tripping through the store.
2. Recovery service: new `OperationRecoveryService` with
   `InterruptAsync` (fingerprint-checked, conditional `UPDATE ...
   WHERE State = 'pending'` claiming the transition, then
   reservation release on the stored admission day plus revision
   advance in the same short transaction; same-state repeats return
   the stored outcome; non-pending records fence; fingerprint
   mismatch conflicts; missing records stay unknown) and a bounded
   startup `ReconcileOrphansAsync` scan (deadline-passed pending →
   interrupt; unexpired pending untouched) wired into host startup
   behind the existing storage/test seams, following the
   `OperationSettlementService` transaction and race-reread
   precedent. Monetary exposure is never touched here — M023
   reconciliation keeps owning it. Done when policy unit tests pass
   with no HTTP/DB dispatch beyond the service's own store.
3. Endpoints + contract: extend `OperationStatus` with `Interrupted`,
   map interrupted records to 200 status responses with zero
   character charge and fresh usage, return recorded interrupted
   metadata (no redispatch) for same-payload resubmission of an
   interrupted identity, keep 409 for changed payloads, 404
   unknown-operation for missing records and 410 for expired
   identities; regenerate and review the OpenAPI delta plus
   TypeScript declarations with the drift gate; verified/unverified/
   anonymous matrix on touched reads; sentinel sweep over tables,
   bodies, logs and reports. Done when real-HTTP/file-backed cases,
   `contract.sh check` and sweeps pass.
4. Recovery/restart/disconnect tests: new `OperationRecoveryTests`
   proving AC-001 (pre-commit crash → restart → interrupted, zero
   charge, released reservation, advanced revision, metadata-only
   read), AC-002 (deadline-passed interrupts, unexpired pending
   untouched after abort), AC-005 (unknown 404 asserts nothing,
   creates nothing; expired 410) and AC-007 (same-file restart
   preserves interrupted state/ledgers/revision; post-restart
   duplicates fenced; zero-dispatch counters on all read paths).
   Done when the new tests pass over file-backed SQLite.
5. Fencing/composition tests: late success after interrupted fenced,
   interrupt after success fenced, changed-payload 409, duplicate
   interrupt idempotent (AC-003/AC-004), aborted-wait success
   committing within the deadline with succeeded/output-unavailable
   status (AC-004), and M023 composition — interrupted retains
   unresolved exposure until evidence settles/releases it in its
   original cost month with zero character charge (AC-006). Done
   when the new tests pass.
6. Regressions and lock: `backend.sh check`, `contract.sh check`,
   `context.py check M024` green with revision/environment/UTC time
   in the completion record.

No migration/rollout beyond step 1's conditional additive migration;
single-instance SQLite assumptions unchanged. Midnight/month
boundaries use the server transaction clock; reservations are never
moved implicitly with the wall clock. The startup scan only
finalizes deadline-passed records, so a slow-but-live operation is
never fenced by a restart.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 crash → interrupted, zero charge | T004 | `bash scripts/backend.sh check` (new `OperationRecoveryTests`: pre-commit crash, restart reconcile, reservation release, metadata read); V-005/V-016 | tasks.md completion record |
| AC-002 deadline scan, abort untouched | T004 | `bash scripts/backend.sh check` (deadline-passed interrupts, unexpired pending committable after abort); V-005 | tasks.md completion record |
| AC-003 late-success fence + 409 + idempotent | T005 | `bash scripts/backend.sh check` (fenced cross-outcome attempts, changed-payload conflict, duplicate interrupt); V-005 | tasks.md completion record |
| AC-004 success-wins fence + abort-then-success | T005 | `bash scripts/backend.sh check` (interrupt-after-success fenced, aborted wait commits, output-unavailable status); API-AC-006/008 portions; V-005 | tasks.md completion record |
| AC-005 unknown 404 / expired 410 | T004 | `bash scripts/backend.sh check` (unknown asserts nothing and writes nothing, expiry mapping, zero dispatch); V-005/V-009 | tasks.md completion record |
| AC-006 exposure retained, month attribution | T005 | `bash scripts/backend.sh check` (interrupted exposure unresolved until evidence, original-month settle-once, zero character charge); V-006 | tasks.md completion record |
| AC-007 restart + zero dispatch | T004 | `bash scripts/backend.sh check` (two-process restart proof, post-restart fencing, dispatch counters); V-005/V-016 | tasks.md completion record |
| AC-008 contract + privacy | T003 | `bash scripts/contract.sh check`, sentinel sweep; V-009/V-015 | tasks.md completion record |
| M022/M023 regression intact | T006 | `bash scripts/backend.sh check` full suite green; `python3 automation/context.py check M024` | tasks.md completion record |

## Context boundaries and risks

Irrelevant domains and why: UX/frontend lanes (no UI surface changes;
error/status JSON only) — reopen for M028+; LLM dispatch/fallback
traversal (zero provider dispatch in this slice; recovery never
regenerates output) — reopen for M026/M027; character settlement and
monetary admission internals (M022/M023 own them; their services are
reused unchanged and their §6.1/§7.1/§7.2/§7.3 portions are
reference-only); ordered cross-period usage snapshots (M025 owns
§7.3/§8.1 beyond the current-day snapshot reused here);
corpus/eval and performance workloads (M035+/M037) — no
qualification claim here; email/devices/manual evidence — none
required. On-demand sources: M022/M023 packages (settlement/exposure
semantics if interplay arises), API §7.2 (if cost-month interplay
arises).

Dependencies, assumptions, blockers: M022/M023 Done assumed (verified
in delivery/current.md and code: `OperationSettlementService`,
`MonetaryAdmissionService`, character/monetary ledgers, revision
counter, status routes). Assumption: no paid serving exists yet, so
interrupted exposure retention is test/local scaffolding under the
Q-001 open cap question. Risk: startup scan fencing live work —
mitigated by finalizing only deadline-passed records, proven by the
unexpired-pending test. Risk: SQLite single-writer races on the
claiming UPDATE — mitigated by the conditional-write plus
race-reread precedent from settlement, proven by concurrent
interrupt/success tests. No human actions; no blockers.
