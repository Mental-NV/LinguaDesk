# M027 — Selected specification
Selected items: BI-027. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: one explicit rewriting operation through the real
host/accounting pipeline for an independent verified Bearer [REDACTED] No SPA execution.
The request carries family `rewrite`, exactly one supported mode
(`correction`, `simple`, `casual`, `business`, `academic`,
`enthusiastic`, `friendly`, `confident`, `diplomatic`; omitted mode
resolves to `correction` before payload matching), an optional manual
source hint (default automatic detection), the complete source text
and a client-generated UUIDv7 operation identity. The host
authenticates (Bearer, verified-account gate), validates shape/mode/
count, fingerprint-matches the identity, atomically admits character
capacity and per-attempt monetary exposure, runs eligibility then
transformation through the M017 rewriting pipeline and M018 chain
traversal with a deterministic fake provider, validates the complete
same-language result, settles exactly once on the stored admission
day, and returns complete text or a classified Problem Details
failure with a fresh current-day usage snapshot. Requirements
FR-012/013/014/024/035–037; FR-004/005 (eligibility terminal
mapping) composing portions; NFR-002 (deadline portion)/NFR-004/
NFR-006 (exposure portion).

Dependencies (Done, reused not re-proven): M026 (operations
submit/status wire, admission/settlement/recovery/snapshot/monetary
services, contract-generation baseline — extended, not duplicated);
M017 (rewriting pipeline, `rewriting.v1` bundle, envelope parser,
mode-intent mapping); M018 (explicit traversal, ≤3 dispatches,
deadline enforcement, cost admission); M022 (conditional settle-once,
admission-day settlement); M023 (per-attempt cost-month attribution,
carryover, ceiling equation); M024 (interrupted fencing,
dispatch-free status reads); M025 (authoritative snapshot +
availability signal on every response).

Exclusions: translation family and target-language selection (M026
owns that path; this slice adds a family-scoped branch only); web
workspace and browser/E2E journeys (M029); live provider dispatch,
candidate qualification and serving/billing attribution (Q-001
remainder); actual monetary cap amount (Q-001); sentence
alternatives, sentence/version association, change highlighting or
comparison views (DF-001/DF-002, per FR-035); prefix charging
(DF-006); automatic pause-based submission (DF-003); public
operation-cancellation endpoint (none for MVP); operation-history
listing or result retrieval beyond metadata-only status replay;
account deletion/backup retention claims (Q-004); compatibility or
versioning guarantees (P-006 remains proposed); FR-015 (retired,
traceability only, no obligation).

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | A verified Bearer [REDACTED] submits a new valid rewrite identity with an explicit mode and receives either synchronous success (complete validated same-language rewritten text, charged scalar count, admission-day period, fresh usage snapshot) or 202 pending with a status reference; a later status read reports the terminal outcome metadata. | FR-012/013/014/035–037; API-AC-013 (rewriting portion) |
| AC-002 | Malformed JSON, unknown/duplicate fields, invalid Unicode, unknown mode, combined modes, empty or whitespace-only source, or oversized source is rejected (400/422 input categories) with zero provider dispatch, zero character charge and no admitted operation. Omitted mode defaults to correction; explicit correction and omitted mode fingerprint identically. | FR-004/007/014/024/037; API-AC-001 (rewriting portion); LLM-AC-002/003 (rewriting portions) |
| AC-003 | Uncertain, unsupported, substantially mixed or source-mismatched input ends as an eligibility rejection (422) with no transformation dispatch and no character charge; paid detection exposure stays private accounting metadata only. | FR-004/005; AI §3.1; LLM-AC-004 (rewriting portion) |
| AC-004 | Success commits exactly one full-source `unicode-scalar-v1` charge on the stored admission day — including a valid correction-only pass that leaves correct text unchanged; a duplicate same-identity/same-payload read returns the original outcome metadata (output unavailable on replay) with fresh usage and no new charge or dispatch; a same-identity/different-payload resubmission conflicts (409). A mode change is a different payload. | FR-013/024/026; API §5.2/6.1/7.1; API-AC-004/005/006 (rewriting portions) |
| AC-005 | Classified fake-provider transient/invalid-output/refusal exhausts the bounded chain to a terminal 503 processing failure with zero character charge; overall deadline expiry is a terminal 504 with zero charge; denied per-attempt monetary admission stops further dispatch (503 monetary suspension). Fallback, when configured, receives the original complete source and mode. | FR-032–034; AI §6/7; LLM-AC-008/009/010 (rewriting portions) |
| AC-006 | A pre-commit crash or restart is fenced as interrupted with zero character charge; an aborted client wait may still settle success within the deadline; status and usage reads never dispatch provider work; an unknown/missing record never asserts success or zero charge. | API §6.1/6.2; arch §7.2; API-AC-007/008 (rewriting portions) |
| AC-007 | Exhausted user or global allowance is denied (429) distinguishing user/global with reset guidance; allowance, snapshot and availability behavior follows the M025 rules unchanged, including cross-midnight charge attribution and revision ordering. | FR-024/027/028; API §7; API-AC-009/011 (rewriting portions) |
| AC-008 | The reviewed generated OpenAPI delta for the rewrite operation ships with zero client drift; no source/result text, global counts, monetary values, credentials or provider internals appear in problems, logs, traces or TEXT columns; text-bearing responses use `Cache-Control: no-store`. | API §8/9; NFR-004/006; API-AC-014 (rewrite delta); V-009/V-015 (rewriting portions) |

## Constraints and decisions

Deadline: PRD overall deadline measured from server receipt, enforced
with injected/fake time; engineering stage defaults 5 s eligibility /
10 s transformation / 2 s finalization reserve (tuneable settings, not
new SLAs). At most three paid dispatches across the chain; accepted
eligibility is reused; no same-candidate retry, hedging or JSON repair.
Rewriting returns the resolved source language (Simplified Chinese
output policy applies); there is no equal-selector rejection for this
family. Q-003 shared wire/counting/recovery semantics are extended by
this slice (mode wire name fixed once in C#/OpenAPI alongside the
M026 fields). Q-006 remaining account details do not block this
slice; M009 auth behavior is reused. Q-001 cap/billing verification
and Q-004 retention/deletion stay open and make no new claim here.
Q-005 corpus qualification is out of scope; the fake provider is
deterministic evidence, not model acceptance. P-005/P-006 add no
obligation. Human gates: none required.
