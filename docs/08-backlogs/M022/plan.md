# M022 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Extend the M021 reservation slice with deterministic settlement. One new
internal `OperationSettlementService` owns the only terminal transitions
(`pending → succeeded`, `pending → failed`); the admission service keeps
owning admission and the endpoints keep owning HTTP mapping. The status read
grows from pending-only to the succeeded/failed metadata rows of the §5.2
identity-state table and the §6.1 outcome table. Explicit exclusions: no
public settle/complete endpoint, no provider dispatch, no monetary
accounting, no interrupted/unknown states, no cross-period snapshot
ordering. Canonical settlement, finality, admission-day and revision rules
arrive through the context packet; they are not recopied here.

## Changes and order

1. Persistence: extend `OperationSubmission` with terminal outcome fields
   (`succeeded`/`failed` states, settled timestamp) via a reviewed additive
   EF migration on the M021 chain; reuse `CharacterLedgerEntry` and
   `LedgerRevision`. Done when model-drift/initialization checks pass.
2. Settlement service: conditional `UPDATE ... WHERE State = 'pending'`
   claiming the transition, then ledger move (success: reserved→consumed on
   the stored admission day; failure: reserved decrement) plus revision
   advance in the same short transaction; same-outcome duplicates return the
   stored outcome, cross-outcome attempts return fenced with no writes;
   fingerprint/payload recheck precedes settlement for the 409 rule. Done
   when policy unit tests pass with no HTTP/DB dispatch.
3. Endpoints + status extension: `GET /api/operations/{operationId}`
   returns succeeded metadata (original charge count/day, output
   unavailable) or failed metadata (zero character charge) plus a fresh
   current-day snapshot; unknown stays account-scoped 404, expired stays
   410; concurrency (parallel settlements), midnight-advanced, and
   two-process restart cases over file-backed SQLite; zero-dispatch
   counters on all read paths. Done when real-HTTP + file-backed cases pass.
4. Contract/auth/privacy: extend C# status DTOs/metadata, regenerate and
   review the OpenAPI delta plus TypeScript declarations with the drift
   gate; verified/unverified/anonymous matrix on the extended read;
   sentinel sweep over tables, bodies, logs and reports. Done when
   `contract.sh check` and sweeps pass.
5. Regressions and lock: `backend.sh check`, `contract.sh check`,
   `context.py check M022` green with revision/environment/UTC time in the
   completion record.

No migration/rollout beyond the additive migration; no config or access
changes; single-instance SQLite assumptions unchanged.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 success-once + idempotent | T002, T003 | `bash scripts/backend.sh check` (new `OperationSettlementTests`: settle-success, duplicate-success); V-005 | tasks.md completion record |
| AC-002 failure release + idempotent | T002, T003 | `bash scripts/backend.sh check` (settle-failure, duplicate-failure, ledger assertions); V-005 | tasks.md completion record |
| AC-003 late-settlement fencing + 409 | T002, T003 | `bash scripts/backend.sh check` (cross-outcome fencing, post-settlement conflict matrix); V-005 | tasks.md completion record |
| AC-004 concurrent settlements | T003 | `bash scripts/backend.sh check` (parallel success settlements, single consumed entry); V-005 | tasks.md completion record |
| AC-005 midnight settlement day | T003 | `bash scripts/backend.sh check` (clock advanced past midnight, admission-day charge); V-005 | tasks.md completion record |
| AC-006 restart + zero dispatch | T003 | `bash scripts/backend.sh check` (two-process restart proof, dispatch counters); V-005 | tasks.md completion record |
| AC-007 terminal status reads | T003 | `bash scripts/backend.sh check` (succeeded/failed/404/410 read matrix over HTTP); API-AC-006/007 portion; V-005 | tasks.md completion record |
| AC-008 contract + privacy | T004 | `bash scripts/contract.sh check`, sentinel sweep; V-009/V-015 | tasks.md completion record |
| M021 regression (admission intact) | T005 | `bash scripts/backend.sh check` full suite green; `python3 automation/context.py check M022` | tasks.md completion record |

## Context boundaries and risks

Irrelevant domains and why: UX/frontend lanes (no UI surface changes;
status JSON only) — reopen for M028+; LLM behavior/corpus (zero provider
dispatch in this slice) — reopen for M026/M027; monetary/monthly accounting
(M023) and interrupted/unknown recovery mapping (M024) — excluded by scope,
their §7.2/§7.3 portions are reference-only; email/devices/manual evidence
— none required. On-demand sources: M021 package (admission semantics),
arch §7.2 failure windows (fencing rationale), API §8 (new status codes if
any diverge from 200/404/410).

Dependencies, assumptions, blockers: M021 Done assumed (verified in
delivery/current.md and code). Risk: concurrent-settlement races under
SQLite single-writer — mitigated by the conditional-transition + unique
state claim in one transaction, proven by the parallel test. No human
actions; no blockers.
