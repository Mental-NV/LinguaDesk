# M021 — Selected specification
Selected items: BI-021. Status: ready; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: one shared admission slice behind a new authenticated
operation submission path that validates, fingerprint-matches and
atomically reserves exactly one logical operation per verified
account identity. The request carries a client-generated UUIDv7
`operationId` plus family (`translation`/`rewriting`), exact decoded
`source` and effective settings (`sourceSelection`, `target` for
translation, `mode` for rewriting with M005 defaults applied before
matching). The server authenticates (cookie or Bearer), requires the
`VerifiedAccount` policy, validates structure/Unicode/length locally
with the M005 `unicode-scalar-v1` policy and catalog limits, resolves
existing `(account, operationId)` state first, then claims the
identity and reserves the full scalar count against both the user and
global UTC-day ledgers in one short database transaction. A minimal
metadata-only status read (`GET /api/operations/{operationId}`,
account-scoped) returns the pending reservation and a fresh
current-day user snapshot; it never dispatches work. Identity wire
names and the status URL shape are fixed here in generated C#/OpenAPI
as the first operation slice. This slice performs zero provider
dispatches: no eligibility, transformation, settlement or retry.

Dependencies: M003 Done (explicit file-backed store, production
migrations, WAL, single-writer serialization, no startup mutation);
M005 Done (scalar counting, 5000/2000 limits, 30s deadline metadata,
UUIDv7 86400s validity / 300s skew constants, capabilities + generated
contract baseline); M007 Done (durable verified accounts,
current-state authorization). Architecture §7.1; API §5/§7.1/§7.3
(minimal snapshot)/§8; V-005 (admission/concurrency portion);
API-AC-004/005 (admission/matching portion).

Exclusions: character settlement, charge commit and reservation
release (M022); per-attempt monetary admission, month attribution and
caps (M023); interrupted/unknown recovery mapping, output delivery
and full status semantics (M024 — the M021 status read reports only
`pending`/`not found`); cross-midnight/month attribution and ordered
snapshots across periods (M025); real eligibility/transformation
dispatch and fallback (M026/M027); UI, email, corpus/eval, visual or
performance workloads. Terminal operation states beyond `pending`
do not exist in this slice.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | Concurrent identical identity+payload claims from one account admit exactly one reservation: all callers observe the same pending operation, the ledger holds one reserved count, and zero provider dispatches occur | FR-026/027; API §5.2; API-AC-004; V-005 |
| AC-002 | The same identity with any changed effective payload (family, one scalar incl. CRLF/LF, source/target selection, mode) returns 409 identity conflict; no reservation, ledger or dispatch changes | FR-026/035; API §5.2; API-AC-004/005; V-005 |
| AC-003 | Equivalent JSON spellings (property order, `\uXXXX` escapes) match the same operation; expired identities (24h + 5min skew per M005 constants, server time authoritative) return 410 with no admission even after record cleanup; malformed/future-skewed identities return 400 with server time | API §5.1; API-AC-005; Q-003 |
| AC-004 | Concurrent distinct operations from one account and across accounts share the ledgers: committed + reserved + new count is tested against user 20,000 and global 2,000,000 in the same transaction; over-capacity returns 429 distinguishing user/global without exposing global counts; usage never exceeds either allowance | FR-027; API §7.1/§8; V-005 |
| AC-005 | Pre-admission failures (unauthenticated 401, unverified 403, malformed/unknown-field 400/415, empty/oversize/invalid-selection 422) leave no operation record, no reservation and no dispatch; the response states no operation was admitted | FR-024; API §4/§8; V-005 |
| AC-006 | Reservations, ledger values and the snapshot revision survive a host restart on the same file; a duplicate after restart still observes pending (no second reservation); status and usage reads make zero model calls | Arch §7.1/§6.1; V-005 |
| AC-007 | Actual C# DTOs/metadata generate the reviewed OpenAPI delta (fixed identity wire + status path, 202/400/401/403/404/409/410/415/422/429 shapes, no-store) with regenerated TypeScript declarations and zero drift; operation records hold fingerprints/metadata only (no source text); logs/traces/reports carry allowlisted numerics only | Arch §5.2/§8.3; API §8; NFR-004; V-009/V-015 |

## Constraints and decisions

Nonfunctional constraints: single-instance SQLite — DB unique
constraint on `(account, operationId)` plus one short write
transaction per admission (relying on single-writer serialization,
not an in-process lock alone); reservations are UTC-day-stamped with
the server transaction clock; amounts are integer scalars;
metadata-only persistence (HMAC fingerprint, counts, day, deadline,
revision) with Fail-closed unknown-price posture inherited (no paid
dispatch exists here); NFR-003 concurrency/no-double-charge portion;
NFR-004 privacy portion (distinctive sentinels must not appear in
DB/logs/traces/reports/backups).

Clarification status: Q-003 shared identity/matching/expiry/day
rules are implemented for this slice; wire fields, fingerprint-key
handling and concurrency evidence close here. Q-004 does not block:
operation metadata follows the M006/M007 account-metadata precedent
(no source/result retention, no deletion/retention claim).
Monetary-cap (Q-001), end-to-end recovery (Q-003 remainder) and
launch lifecycle decisions stay open with their owning milestones.

Human gates: none required at beginning or end. No live provider,
credential, email, device or manual evidence is used; executor runs
the deterministic policy + file-backed SQLite/HTTP checks and the
contract/privacy sweeps autonomously.
