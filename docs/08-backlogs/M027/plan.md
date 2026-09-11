# M027 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Wire the existing rewriting AI path (M017 pipeline + M018 chain
traversal) behind the existing operations admission/settlement
pipeline for the `rewrite` family only, served to verified Bearer [REDACTED] with a
deterministic fake provider. One logical operation keeps one UUIDv7
identity, one character reservation settling exactly once on the
admission day, per-attempt monetary admission in its own cost month,
and a fresh M025 usage snapshot on every response. The request
carries exactly one mode from the nine-value catalog (default
correctionOnly); eligibility classifies the source language and the
transformation returns same-language corrected/styled text, which may
validly equal the source for already-correct correction-only input.
Local validation precedes reservation; paid eligibility runs only
after admission. The M026 translation branch is untouched. No new
product policy, no live serving, no UI.

## Changes and order

1. Contract first: extend `backend/src/LinguaDesk.Api/Features/Operations/OperationsContract.cs`
   with the rewrite submit fields (family-scoped source, mode with
   correctionOnly default, optional source hint), success envelope
   (complete text, charged count, charge day) and wire-fixed Problem
   Details category names per API §8; keep DF-001/DF-002 placeholders
   out. Add actual C# DTOs/metadata, then generate and review OpenAPI
   before handler adoption.
2. Fake rewriting provider: add a deterministic test-only
   `IChatClient` (scripted eligibility classification + per-mode
   rewriting result, including unchanged-text correction, plus
   injectable transient/invalid/refusal and latency faults)
   registered only in the test harness composition, inaccessible in
   production configuration.
3. Coordinator: add a rewriting execution coordinator in
   `Features/Operations/` (alongside `TranslationOperationCoordinator`)
   driving `OperationAdmissionService` →
   `ChainOrchestrator`/`RewritingPipeline` (fake client) → result
   validation → `OperationSettlementService`, honoring the stored
   absolute deadline with injected time, fencing late output, and
   settling success/failed/interrupted through existing conditional
   transitions. Reuse `MonetaryAdmissionService` per attempt with
   cost-month attribution; denial stops traversal.
4. Endpoints: extend `OperationsEndpoints.cs` submit/status reads for
   the rewrite execution path; preserve identity-state table
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
| AC-001/004/006/007 | T004 | V-005: file-backed SQLite/HTTP rewrite submit/duplicate/status/restart suite; `scripts/backend.sh` aggregate | tasks.md completion record |
| AC-002/003 | T004 | V-001 (rewriting boundary portion) + V-007: mode/validation/eligibility matrix with zero-dispatch assertions; `scripts/backend.sh` | tasks.md completion record |
| AC-005 | T004 | V-007/V-008 (rewriting portions): scripted fault/deadline/budget traversal bounds; `scripts/backend.sh` | tasks.md completion record |
| AC-008 | T003 | V-009: `scripts/contract.sh` generation parity + drift review; V-015 privacy inspection (no text/secrets in problems/logs/TEXT columns) | tasks.md completion record |
| M026/M017/M022–M025 reuse | T001 | Regression: existing translation/auth/accounting/usage suites stay green; `scripts/backend.sh` | tasks.md completion record |
| Readiness | T005 | `python3 automation/context.py check M027` fresh at close | tasks.md completion record |

## Context boundaries and risks

Omitted domains and why: UX workspace/browser (no UI until M029;
API-only slice per roadmap §4.5); translation target selection (M026
owns that family; this slice branches on family); LLM evaluation
corpora and candidate qualification (Q-005/G1 — deterministic fake
only); performance workloads (G2/M037 rehearsal, not this slice);
email delivery (M034); release device/AT/browser matrix (G3/M038/
M039); backup/restore lifecycle (M040/M041, Q-004). Open on demand:
M029 boundary if the shared coordinator shape affects the journey;
ADR index only if the slice changes a recorded decision (none
intended).

Risks: coordinator bypassing admission order (mitigated by reusing
`OperationAdmissionService`/`MonetaryAdmissionService` entry points);
late fake output resurrecting terminal state (conditional
transitions + fencing); rewrite branch leaking into translation
behavior (family-scoped branch + M026 regression suites); mode
default resolved after fingerprinting causing replay mismatch
(default applied before matching per API §4.2); contract drift
(generation + drift gate before adoption).

Human steps: none required; no owner/timing/blocked gate. Existing
local-account authorization and routine isolated-test setup suffice.
