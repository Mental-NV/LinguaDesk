# M024 — Selected specification
Selected items: BI-024. Status: ready; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: terminal `interrupted` recovery mapping over the M022/M023
operation state machine. A new internal recovery service (test seam,
following the settlement/admission precedent) finalizes abandoned
pending operations — restart-orphaned in-flight records and
deadline-passed pending records found by a bounded startup
reconciliation scan — as `interrupted` inside a single short database
transaction that releases the character reservation with zero consumed
change and advances the durable snapshot revision. Retained monetary
exposure stays unresolved until M023 reconciliation settles or
releases it against authoritative evidence. The account-scoped status
read is extended to report `interrupted` (zero character charge plus a
fresh current-day usage snapshot); reads never dispatch work. A
disconnect/aborted fetch never finalizes anything by itself: bounded
processing may still commit success within the original deadline.

Recovery here is driven by the internal service and the startup scan,
not by a new public endpoint: the MVP has no public
operation-cancellation endpoint, and no provider dispatch occurs in
this slice. Late results cannot revive a terminal outcome in either
direction.

Dependencies: M022 Done (pending/succeeded/failed states, conditional
transitions, fencing, admission-day settlement, revision counter,
restart durability); M023 Done (per-attempt exposure reservation,
missing-evidence retention, cost-month attribution); architecture
§7.1 (conditional finalization, fencing)/§7.2 (failure windows,
exposure retention); API §5.1 (identity validity)/§5.2
(identity-state table, same-identity retransmission)/§6.1
(outcome/finality)/§6.2 (deadlines, no public cancellation)/§8
(recovery categories); V-005 (failure-window portion); V-016
(restart/durability portion); API-AC-006/007/008 (selected portions).

Exclusions: real provider dispatch, eligibility/transformation and
fallback traversal (M026/M027); ordered usage snapshots across period
changes and usage reporting beyond the current-day snapshot (M025);
output delivery or result-text recovery of any kind (no result cache,
no regeneration call — a new generation needs an explicit new
operation); launch cap amount and serving/billing verification (Q-001
remainder, pre-launch); deletion/backup/retention claims (Q-004);
UI, email, corpus/eval, visual or performance workloads.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | A pending operation orphaned by a pre-commit crash is reconciled as interrupted: after restart with the same file, the record is terminal `interrupted`, user and global reserved counts are released on the stored admission day with consumed unchanged, the revision advances, and a same-payload status read returns interrupted metadata with fresh usage without dispatching work | FR-026; API §6.1/§7.1; Arch §7.2; API-AC-007; V-005/V-016 |
| AC-002 | Deadline-passed pending records finalize as interrupted on the recovery scan while unexpired pending records are left untouched: a mere disconnect/abort with time remaining leaves the operation pending and committable, never auto-interrupted | API §6.2; Arch §7.1/§7.2; V-005 |
| AC-003 | Late settlement after interrupt is fenced: success after terminal interrupted, and a changed effective payload against an interrupted identity (409 identity conflict), change nothing — no ledger, revision or state change; a repeated interrupt of the same identity is idempotent with no further ledger change | FR-026; API §5.2/§6.1; Arch §7.1; V-005 |
| AC-004 | Interrupt after terminal success is fenced with the charge intact; an aborted wait may still commit success within the original deadline, and status then reports succeeded with output unavailable and the original charge; late output never revives terminal interrupted or failed records | FR-026; API §6.1/§6.2; API-AC-006/008; V-005 |
| AC-005 | Unknown identities stay 404 unknown-operation without asserting zero charge, creating no record and dispatching nothing; expired identities stay 410; malformed/future-skew identities stay 400 with server time | API §5.1/§5.2/§6.1/§8; API-AC-007; V-005/V-009 |
| AC-006 | Interruption retains unresolved monetary exposure: an interrupted operation keeps its M023 attempt reservation until authoritative billing evidence settles or releases it, while character ledgers show zero charge; exposure settlement still attributes its original cost month exactly once | NFR-006; Arch §7.2/§7.3; API §7.2; V-006 |
| AC-007 | Interrupted state, ledger values and the snapshot revision survive a host restart on the same file; a post-restart duplicate interrupt or late success is still idempotent/fenced, and status and usage reads make zero model calls | Arch §7.1/§7.2; V-005/V-016 |
| AC-008 | Actual C# DTOs/metadata generate the reviewed OpenAPI delta (interrupted status shape, no-store preserved) with regenerated TypeScript declarations and zero drift; operation records hold fingerprints/metadata only (no source text); logs/traces/reports carry allowlisted numerics only, with no monetary minor-unit value in problems, logs or TEXT columns | Arch §8.3; API §8; NFR-004; V-009/V-015 |

## Constraints and decisions

Nonfunctional constraints: single-instance SQLite — conditional
pending→interrupted transition plus ledger release plus revision
advance in one short write transaction per recovery (single-writer
serialization, not an in-process lock alone); amounts are integer
scalars; admission-day stamp is immutable; deadline is immutable and
never reset by duplicate delivery, fallback, midnight or status
reads; metadata-only persistence (fingerprint, counts, day, deadline,
outcome, revision) with no source/result text; NFR-003
no-double-charge portion; NFR-004 privacy portion (distinctive
sentinels must not appear in DB/logs/traces/reports/backups).

Clarification status: Q-003 shared interruption/cancellation/finality
rules are implemented for this slice (recovery mapping, deadline
handling, no public cancellation endpoint). Q-004 does not block:
interrupted metadata follows the M006/M007/M021 account-metadata
precedent (no source/result retention, no deletion/retention claim).
Q-001 serving/billing bounds stay open pre-launch and block paid
serving, not this slice.

Human gates: none required at beginning or end. No live provider,
credential, email, device or manual evidence is used; the executor
runs the deterministic recovery + file-backed SQLite/HTTP checks and
the contract/privacy sweeps autonomously.
