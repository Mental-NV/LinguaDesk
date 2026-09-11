# M023 — Selected specification
Selected items: BI-023. Status: ready; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: per-attempt monetary cost admission for paid attempts. A new
internal attempt-admission service reserves a finite conservative upper
bound for each paid attempt before any dispatch, stamped with its own
cost-admission UTC month, and enforces `known spend + unresolved
exposure + new attempt upper bound <= configured cap` atomically in one
short database transaction. A reconciliation path settles reserved
exposure to known spend exactly once against authoritative billing
evidence, or releases it against authoritative no-charge evidence. The
upper bound for an attempt is computed from that attempt's verified
billing profile (peak cache-miss input rate, peak output rate over the
configured finite maximum output, plus other billable attempt charges)
in integer minor units rounded conservatively upward. Monetary amounts
stay private: no minor-unit value appears in API responses, logs,
traces or reports.

Admission here is driven by the internal service (test seam), not by a
new public endpoint: real provider dispatch arrives in M026/M027, and
no attempt is dispatched in this slice. Character admission (M021) and
character settlement (M022) are reused unchanged; a monetary denial
creates no operation mutation.

Dependencies: M021 Done (atomic admission transaction, identity/matching
rules, UTC-day ledgers); M022 Done (conditional terminal transitions,
fencing, restart durability precedent); architecture §7.1 (atomic
reservation rules) and §7.3 (ceiling equation); API §7.2 (per-attempt
month attribution, carryover); API §8 (monetary-suspension category);
LLM §5.4 (attempt upper-bound formula, fail-closed posture,
fixed-point arithmetic); V-006; API-AC-010 (cross-month monetary
scenario).

Exclusions: real provider dispatch, eligibility/transformation and
fallback traversal (M026/M027); interrupted/unknown recovery mapping
and output delivery (M024); ordered usage snapshots across periods and
usage reporting beyond the existing current-day snapshot (M025);
launch cap amount, serving/billing attribution verification and actual
provider invoice mapping (Q-001 remainder, pre-launch); alternative
context accounting (DF-001, deferred); UI, email, corpus/eval, visual
or performance workloads. Terminal `interrupted` and `unknown` states
do not exist in this slice; crash-window recovery mapping stays with
M024.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | Admitting a paid attempt whose upper bound fits the remaining ceiling reserves the bound against the configured cap in the attempt's cost-admission UTC month; an attempt that would exceed the cap is denied with the monetary-suspension category (503, distinct from outage, no invented resume time), writes no reservation or exposure, and leaves status/usage reads available | NFR-006; Arch §7.3; API §7.2/§8; V-006 |
| AC-002 | An attempt with unknown price, missing billing profile or unbounded billable scope is ineligible for paid dispatch: admission is denied with no reservation, no exposure and no dispatch, even when the ceiling has headroom | NFR-006; LLM §5.4; Arch §7.3; V-006 |
| AC-003 | Concurrent attempts racing a nearly exhausted ceiling commit within the cap: parallel admissions against one month hold `known + unresolved + admitted <= cap`, and each denied caller observes the suspension category with no partial reservation | Arch §7.1/§7.3; V-006; backend §3.2 |
| AC-004 | Reconciliation against authoritative billing evidence settles reserved exposure to known spend exactly once: the same settlement evidence applied twice changes nothing further, settled cost is never counted as both known spend and unresolved exposure, and release on authoritative no-charge evidence decrements exposure with no known-spend change and never below zero | Arch §7.3; LLM §5.4; API-AC-010; V-006 |
| AC-005 | Missing or inconsistent usage evidence retains the full conservative reservation; a timeout, cancelled call or rejected output with no authoritative evidence keeps its exposure reserved | LLM §5.4; Arch §7.2; V-006 |
| AC-006 | Cross-month carryover: unresolved prior-month exposure counts in new-month admission until authoritatively settled; month rollover opens a new known-spend bucket; settled attribution stays in its original cost month and is never double counted | API §7.2; Arch §7.3; API-AC-010; V-006 |
| AC-007 | Monetary ledgers and attempt reservations survive a host restart on the same file; a post-restart duplicate admission observes the existing reservation with no second exposure, and status/usage reads make zero model calls | Arch §7.1/§7.2; V-006 |
| AC-008 | Actual C# DTOs/metadata generate the reviewed OpenAPI delta (if any shape changes) with regenerated TypeScript declarations and zero drift; no monetary minor-unit value appears in any response, log, trace, report or backup artifact; operation records hold fingerprints/metadata only | Arch §5.2/§8.3; API §8; NFR-004; V-009/V-015 |

## Constraints and decisions

Nonfunctional constraints: single-instance SQLite — ceiling check plus
exposure reservation in one short write transaction per admission
(single-writer serialization, not an in-process lock alone); money in
integer minor units with conservative upward rounding, never
floating-point accumulation; cost-month stamp immutable once reserved;
metadata-only persistence (attempt identity, bound, month, tariff
reference, outcome) with no source/result text; NFR-006 suspension
portion (preserve work, return budget-related availability error);
NFR-004 privacy portion (distinctive sentinels must not appear in
DB/logs/traces/reports/backups).

Clarification status: Q-001 cost-month attribution and conservative
carryover are implemented for this slice; the actual cap amount,
serving/billing bounds and attribution verification stay open for
pre-launch and block paid serving, not this slice — tests use small
explicit configured caps. Q-008 provider-cost tracking scope stays with
its proposal disposition; this slice implements only the NFR-006
ceiling guarantee. Q-004 does not block: monetary metadata follows the
M006/M007/M021 account-metadata precedent (no source/result retention,
no deletion/retention claim). DF-001 alternative context adds no MVP
accounting branch.

Human gates: none required at beginning or end. No live provider,
credential, email, device or manual evidence is used; the executor runs
the deterministic cost-admission + file-backed SQLite checks and the
contract/privacy sweeps autonomously.
