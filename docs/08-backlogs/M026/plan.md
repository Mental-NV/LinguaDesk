# M026 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Wire the existing translation AI path (M016 pipeline + M018 chain
traversal) behind the existing operations admission/settlement pipeline
for the `translation` family only, served to verified Bearer [REDACTED] with a
deterministic fake provider. One logical operation keeps one UUIDv7
identity, one character reservation settling exactly once on the
admission day, per-attempt monetary admission in its own cost month,
and a fresh M025 usage snapshot on every response. The current
`POST /api/operations` description stating "No provider dispatch
occurs" is updated for translation; rewriting still dispatches
nothing until M027. Local validation precedes reservation; paid
eligibility runs only after admission. No new product policy, no live
serving, no UI.

## Changes and order

1. Contract first: extend `backend/src/LinguaDesk.Api/Features/Operations/OperationsContract.cs`
   with the translation submit fields (family-scoped source, target,
   optional source hint), success envelope (complete text, charged
   count, charge day) and wire-fixed Problem Details category names per
   API §8; keep DF-001 placeholders out. Add actual C# DTOs/metadata,
   then generate and review OpenAPI before handler adoption.
2. Fake translation provider: add a deterministic test-only
   `IChatClient` (scripted eligibility classification + translation
   result per direction, plus injectable transient/invalid/refusal and
   latency faults) registered only in the test harness composition,
   inaccessible in production configuration.
3. Coordinator: add a translation execution coordinator in
   `Features/Operations/` driving `OperationAdmissionService` →
   `ChainOrchestrator`/`TranslationPipeline` (fake client) → result
   validation → `OperationSettlementService`, honoring the stored
   absolute deadline with injected time, fencing late output, and
   settling success/failed/interrupted through existing conditional
   transitions. Reuse `MonetaryAdmissionService` per attempt with
   cost-month attribution; denial stops traversal.
4. Endpoints: extend `OperationsEndpoints.cs` submit/status reads for
   the translation execution path; preserve identity-state table
   semantics (pending → metadata, succeeded → metadata/output
   unavailable, failed/interrupted → recorded failure, conflict,
   expired), dispatch-free reads, `no-store` on text-bearing
   responses, and authentication-before-metadata ordering.
5. Recovery/observability: keep `OperationRecoveryService` fencing;
   metadata-only logs/traces; snapshot/availability via the M025
   reader unchanged.
6. Order: contract + OpenAPI review → fake + coordinator → endpoints →
   settlement/recovery integration → drift/privacy checks → full
   backend + contract suites.

Migration/rollout/rollback: no schema change beyond what the slice
needs; if a migration is required it follows the tested
fresh-plus-upgrade rule. No rollout beyond the local host; rollback is
the prior commit. No production serving configuration is introduced.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001/004/006/007 | T004 | V-005: file-backed SQLite/HTTP translation submit/duplicate/status/restart suite; `scripts/backend.sh` aggregate | tasks.md completion record |
| AC-002/003 | T004 | V-001 (translation boundary portion) + V-007: validation/eligibility matrix with zero-dispatch assertions; `scripts/backend.sh` | tasks.md completion record |
| AC-005 | T004 | V-007/V-008 (translation portions): scripted fault/deadline/budget traversal bounds; `scripts/backend.sh` | tasks.md completion record |
| AC-008 | T003 | V-009: `scripts/contract.sh` generation parity + drift review; V-015 privacy inspection (no text/secrets in problems/logs/TEXT columns) | tasks.md completion record |
| M009/M022–M025 reuse | T001 | Regression: existing auth/accounting/usage suites stay green; `scripts/backend.sh` | tasks.md completion record |
| Readiness | T005 | `python3 automation/context.py check M026` fresh at close | tasks.md completion record |

## Context boundaries and risks

Omitted domains and why: UX workspace/browser (no UI until M028;
API-only slice per roadmap §4.5); rewriting modes (M027 owns the
second family); LLM evaluation corpora and candidate qualification
(Q-005/G1 — deterministic fake only); performance workloads (G2/M037
rehearsal, not this slice); email delivery (M034); release
device/AT/browser matrix (G3/M038/M039); backup/restore lifecycle
(M040/M041, Q-004). Open on demand: `docs/08-backlogs/M027`
boundary if the shared coordinator shape affects it; ADR index only
if the slice changes a recorded decision (none intended).

Risks: coordinator bypassing admission order (mitigated by reusing
`OperationAdmissionService`/`MonetaryAdmissionService` entry points);
late fake output resurrecting terminal state (conditional
transitions + fencing); translation description change leaking into
rewriting behavior (family-scoped branch + regression suites);
contract drift (generation + drift gate before adoption).

Human steps: none required; no owner/timing/blocked gate. Existing
local-account authorization and routine isolated-test setup suffice.
