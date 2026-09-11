# M022 — Selected specification
Selected items: BI-022. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: deterministic settlement of M021 pending reservations. An internal
settlement service moves one `(account, operationId)` record from `pending`
to `succeeded` or to terminal `failed` inside a single short database
transaction that also updates both UTC-day ledgers and advances the durable
snapshot revision. Success commits exactly the reserved full scalar count as
consumed on the stored admission day; failure decrements the reserved count
with no consumed change. All terminal transitions are conditional on the
current state so duplicates are idempotent and late cross-outcome attempts
are fenced. The account-scoped status read is extended to report `succeeded`
(charge metadata, output unavailable) and `failed` (zero character charge)
with a fresh current-day usage snapshot; reads never dispatch work.

Settlement here is driven by the internal service (test seam), not by a new
public completion endpoint: real provider dispatch arrives in M026/M027, and
no public cancellation endpoint exists. No provider calls occur in this slice.

Dependencies: M021 Done (pending reservations, fingerprint matching,
UTC-day ledgers, revision counter, identity validity/expiry rules, minimal
status read). Architecture §7.1/§7.2; API §5.2 (identity-state table)/
§6.1 (outcome/finality)/§7.1 (admission-day settlement)/§7.3 (snapshot
revision); V-005 (settlement/duplication portion); API-AC-004/006/007
(selected portions).

Exclusions: per-attempt monetary admission, month attribution and caps
(M023); interrupted/unknown recovery mapping, output delivery and full
status/replay semantics beyond succeeded/failed metadata (M024); ordered
snapshots across period changes and usage reporting beyond the current-day
snapshot (M025); real eligibility/transformation dispatch and fallback
(M026/M027); UI, email, corpus/eval, visual or performance workloads.
Terminal `interrupted` and `unknown` states do not exist in this slice.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | Settling a pending reservation as success commits exactly one full-source charge: user and global ledgers move the reserved count to consumed on the stored admission day, the revision advances, and the record is `succeeded`; settling the same operation as success again returns the same outcome with no ledger or revision change | FR-024/026; API §6.1/§7.1; API-AC-004; V-005 |
| AC-002 | Settling a pending reservation as definitive failure releases the reservation: user and global reserved counts decrement, consumed is unchanged, the revision advances, and the record is terminal `failed`; settling it as failed again is idempotent with no ledger change | FR-026; API §6.1; API-AC-007; V-005 |
| AC-003 | Late cross-outcome settlement is fenced: success after terminal failure, and failure after terminal success, are rejected with no ledger, revision or state change; a changed effective payload against a settled identity still returns 409 identity conflict | FR-026; API §5.2; Arch §7.1; V-005 |
| AC-004 | Concurrent duplicate success settlements against one pending reservation commit exactly one charge: parallel callers observe the same succeeded outcome and the ledgers hold a single consumed entry | FR-026/027; Arch §7.1; V-005 |
| AC-005 | Success settling after UTC midnight charges the original admission day: with the server clock advanced past midnight, consumed increments on the stored day, the new day is untouched, and the fresh snapshot still describes the current day | API §7.1; FR-024/027; V-005 |
| AC-006 | Settled state, ledger values and the snapshot revision survive a host restart on the same file; a post-restart duplicate or late settlement is still idempotent/fenced, and status and usage reads make zero model calls | Arch §7.1/§7.2; API-AC-007; V-005 |
| AC-007 | Status reads report terminal metadata only: `succeeded` returns the original charge count/day and fresh usage with output unavailable (never redispatching), `failed` returns zero character charge and fresh usage, unknown identities stay 404 without asserting zero charge, and expired identities stay 410 | API §6.1/§5.2; API-AC-006/007; V-005 |
| AC-008 | Actual C# DTOs/metadata generate the reviewed OpenAPI delta (extended status shapes, no-store preserved) with regenerated TypeScript declarations and zero drift; operation records hold fingerprints/metadata only (no source text); logs/traces/reports carry allowlisted numerics only | Arch §5.2/§8.3; API §8; NFR-004; V-009/V-015 |

## Constraints and decisions

Nonfunctional constraints: single-instance SQLite — conditional state
transition plus ledger update plus revision advance in one short write
transaction per settlement (single-writer serialization, not an in-process
lock alone); amounts are integer scalars; admission-day stamp is immutable;
metadata-only persistence (fingerprint, counts, day, deadline, outcome,
revision) with no source/result text; NFR-003 no-double-charge portion;
NFR-004 privacy portion (distinctive sentinels must not appear in
DB/logs/traces/reports/backups).

Clarification status: Q-003 shared settlement/finality/day rules are
implemented for this slice; interrupted/unknown mapping stays with M024.
Q-004 does not block: settlement metadata follows the M006/M007/M021
account-metadata precedent (no source/result retention, no
deletion/retention claim). Monetary-cap (Q-001) and end-to-end recovery
(Q-003 remainder) stay open with their owning milestones.

Human gates: none required at beginning or end. No live provider,
credential, email, device or manual evidence is used; the executor runs the
deterministic settlement + file-backed SQLite/HTTP checks and the
contract/privacy sweeps autonomously.
