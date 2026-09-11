# M025 — Selected specification
Selected items: BI-025. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: authoritative usage/availability reporting over the
M022/M023/M024 operation slice. A new verified-account usage read
returns the authoritative current-day snapshot; every operation
submit/duplicate/status response keeps carrying a fresh current-day
snapshot extended with a categorical availability signal computed
from the same consistent read (user allowance, global allowance,
monetary suspension, storage-unavailable). Snapshots order by UTC
day first and durable revision second; the revision advances on
every admission/settlement/recovery transaction and survives
restart. Cross-midnight settlement keeps the charge on the stored
admission day while the fresh snapshot describes the new day, with
the operation's own charged day disclosed separately and never
added locally to the server counter. Cross-month monetary carryover
(M023) stays consistent with snapshot availability. Reads never
charge allowances, reserve exposure, or dispatch work.

Dependencies: M022 Done (admission-day settlement, conditional
transitions, revision counter, restart durability); M023 Done
(per-attempt cost-month attribution, carryover, settle-once);
M024 Done (interrupted metadata + fresh current-day snapshot,
zero-dispatch reads); architecture §7.1 (atomic consistent reads,
immutable period stamps)/§7.3 (ceiling equation, suspension
semantics); API §7.1 (admission-day rules)/§7.2 (cost-month
rules)/§7.3 (snapshot/ordering rules)/§8 (allowance, suspension,
unavailable categories); V-005/V-006 (allowance/cost portions);
API-AC-009/011 (selected portions); API-AC-004/007/010 (composition
reference).

Exclusions: real provider dispatch, eligibility/transformation and
fallback traversal (M026/M027); new charge/exposure arithmetic
beyond the M022/M023 rules reused unchanged; launch cap amount,
serving/billing attribution verification and invoice mapping
(Q-001 remainder, pre-launch); deletion/backup/retention claims
(Q-004); UI display of usage/availability beyond the JSON contract
(M028+); email, corpus/eval, visual or performance workloads.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | The usage read and every operation response carry an authoritative current-day snapshot: UTC day, next UTC reset, consumed, reserved, user allowance, nonnegative available, durable revision, and a categorical availability signal; exhausted user allowance reports user-exhausted, exhausted global reports service-exhausted, a reached monetary ceiling reports monetary-suspended, and storage failure reports unavailable — all without global counts, monetary amounts, source text or secrets | FR-027/028; API §7.3/§8; API-AC-011; V-005/V-006 |
| AC-002 | Cross-midnight composition: an operation admitted before UTC midnight settles successfully after midnight with the full charge committed to the stored admission day, the new day's ledger untouched, the status read returning the original admission day plus a fresh new-day snapshot, and the earlier-period charge disclosed without incrementing today's consumed count | FR-024/027/028; API §7.1/§7.3; API-AC-009; V-005 |
| AC-003 | Cross-month composition: unresolved prior-month exposure keeps new-month paid admission denied (monetary-suspended availability) until authoritative settlement; once settled, attribution stays in its original cost month exactly once while the character snapshot rolls to the current day correctly; no cost is counted as both known spend and unresolved exposure | NFR-006; API §7.2/§7.3; API-AC-010 portion; V-006 |
| AC-004 | Snapshot ordering: within an account an older-day snapshot is ignored, a lower same-day revision is ignored, and a newer day is accepted even when its consumed count is smaller; the revision is monotonic across admissions, settlements, recovery and restarts; exact ordering survives the JSON round-trip; snapshots are never compared across accounts or after the account context is invalidated | API §7.3; API-AC-011; V-005 |
| AC-005 | Duplicate/status reads reuse the original period: a same-payload resubmission or status read after midnight returns the stored admission day with a fresh current-day snapshot, creates no new reservation or charge, and advances no ledger; a changed payload still conflicts | FR-026/027; API §5.2/§7.1; API-AC-004 portion; V-005 |
| AC-006 | Interrupted/failed composition: status reads of interrupted and failed operations across a midnight boundary report zero character charge with the original admission day and a fresh current-day snapshot; unknown identities stay 404 without asserting any charge and expired identities stay 410 | FR-026; API §6.1/§7.1; API-AC-006/007 portions; V-005 |
| AC-007 | Ledgers for both the old and new day, the snapshot revision and the availability signal survive a host restart on the same file; post-restart usage/status reads stay consistent, make zero model calls, and change no ledger or revision by themselves | Arch §7.1/§7.2; V-005/V-016 portion |
| AC-008 | Actual C# DTOs/metadata generate the reviewed OpenAPI delta (usage read, availability signal, no-store preserved) with regenerated TypeScript declarations and zero drift; operation/ledger records hold fingerprints/metadata only (no source text); logs/traces/reports carry allowlisted numerics only, with no global counts, monetary minor-unit values or secrets in problems, logs or TEXT columns | Arch §8.3; API §8/§9; NFR-004; V-009/V-015 |

## Constraints and decisions

Nonfunctional constraints: single-instance SQLite — revision,
ledger values and availability inputs read consistently
(single-writer serialization, not an in-process lock alone);
amounts are integer scalars; admission-day and cost-month stamps
are immutable and never moved implicitly with the wall clock; the
server transaction-time clock decides the day; metadata-only
persistence (fingerprints, counts, days, months, outcomes,
revision) with no source/result text; NFR-003 no-double-charge
portion; NFR-004 privacy portion (distinctive sentinels must not
appear in DB/logs/traces/reports/backups).

Clarification status: Q-003 shared day/month/ordering rules are
implemented for this slice; exact wire field names are fixed once
in C#/OpenAPI here and adopted by later clients. Q-001 cap
amount, serving/billing verification stay open pre-launch and
block paid serving, not this slice — tests use small explicit
configured caps. Q-004 does not block: usage metadata follows the
M006/M007/M021 account-metadata precedent (no source/result
retention, no deletion/retention claim).

Human gates: none required at beginning or end. No live provider,
credential, email, device or manual evidence is used; the executor
runs the deterministic admission/settlement + file-backed
SQLite/HTTP checks and the contract/privacy sweeps autonomously.
