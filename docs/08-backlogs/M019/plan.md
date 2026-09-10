# M019 — Implementation plan

Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Add the access slice to the host-independent AI surface: a validated
multi-profile candidate registry, one OpenAI-compatible Chat Completions
adapter with the DeepSeek dialect, an environment-backed credential resolver,
and two explicit runner workflows — offline transport conformance and a
single budgeted live access check. All paid dispatch passes an explicit
finite evaluation budget with conservative reservations (AI §5.4) and
dispatch counting; nothing here changes eligibility semantics, chain policy,
API/UI integration or serving startup. The live check proves only momentary
reachability for `DeepSeek-V4.1-Flash`; language quality, cost and latency
qualification stay with M020 and successors.

Selected canonical excerpts arrive through the context packet. Do not copy
them all here. The essential local invariants: profiles are non-secret and
credential references are provider-scoped; the adapter performs no retries
or hedging and honors bounded reads/cancellation; a requested live run with
a missing credential is blocked, never faked.

## Changes and order

1. `backend/src/LinguaDesk.Infrastructure.Ai/` — candidate profile model,
   registry load/validation (unique IDs, required AI §5.1 fields, HTTPS
   endpoint, explicit settings with thinking disabled, finite bounds,
   billing profile) and the Chat Completions adapter (request building,
   dialect fields, response/usage/error mapping, bounded reads,
   cancellation, one-dispatch counting). Depends on nothing; done when new
   MSTest offline cases prove AC-001–003 against fake-handler fixtures.
2. Same library — credential-resolver boundary: `CredentialRef` mapping
   supplied only by the evaluation composition, key held in the narrow
   transport credential type, diagnostics limited to
   `credentialRef`/`credentialPresent`. Depends on step 1; done when tests
   prove AC-004 (no credential in any serialized profile/report/fixture and
   offline runs read no credential variables).
3. `backend/tools/LinguaDesk.Ai.Evaluation/` — profile selection plus two
   noninteractive workflows: offline `conformance` (synthetic fixtures
   through the real adapter) and live `verify-access` (at most one
   fixed-synthetic budget-admitted request through the actual
   endpoint/model/auth/parser; blocked without credential). Retire nothing:
   `inspect`/`probe` keep working. Depends on steps 1–2; done when AC-005
   (live success recorded without secrets) and AC-006 (second credential
   reference selectable) hold.
4. `scripts/ai.sh` + operating guide — extend the AI command surface for the
   new offline checks and document the explicit live invocation with its
   finite budget flags; keep offline the default. Depends on step 3; done
   when `bash scripts/ai.sh check` covers the new tests and the README
   procedure names the exact live command, budget flags and report path.
5. Regression and secret sweep — full backend check, contract drift check,
   and a sweep proving no key material in sources, fixtures, reports or
   shell output. Depends on step 4; done when the commands below pass and
   the completion record names revision, environment and UTC time.

No migration, rollout or rollback applies: no database, API surface or
published-host change. No new package dependency beyond the already-selected
AI abstractions pin unless execution justifies it through the lock graph.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 registry | T001 | V-008 offline: `bash scripts/ai.sh check` (new MSTest registry cases) | `artifacts/test-results/ai.trx` |
| AC-002 conformance | T002 | V-008 offline: runner `conformance` over sanitized fixtures + `bash scripts/ai.sh check` | `artifacts/test-results/ai.trx`; conformance report |
| AC-003 faults | T002 | V-008 offline: fault/cancel/bound/dispatch MSTest cases, same command | `artifacts/test-results/ai.trx` |
| AC-004 secrets | T003 | V-008/V-015: offline tests with provider networking disabled + secret sweep (`grep` for key patterns across sources/fixtures/reports; env-var absence assertion) | `artifacts/test-results/ai.trx`; sweep result in tasks.md |
| AC-005 live access | T004 | V-008 live (planned entry point, implemented by T004): explicit `verify-access --profile DeepSeek-V4.1-Flash` with finite `--max-dispatches 1` and spend budget; missing credential must exit blocked/non-success | Sanitized live-access report (reference-only, no secret); tasks.md completion record |
| AC-006 second profile | T004 | V-008: offline selection test + live-path selection without DF-004 rules | `artifacts/test-results/ai.trx`; report |
| Regression | T005 | `bash scripts/backend.sh check`; `bash scripts/contract.sh check`; `python3 automation/context.py check M019` | Existing result paths; tasks.md completion record |

The live `verify-access` invocation above is a planned entry point, not a
runnable command today (per verification §3.3: document actual commands in
#10 once they exist). T003–T004 implement it; the completion record owns the
exact executed command.

## Context boundaries and risks

Omitted domains and why: UX/API contracts (no user-visible or wire change —
capabilities endpoint untouched, no OpenAPI regeneration); Core counting
(M005 reused as convention, unchanged); accounting/settlement (AI §5.4
reservation invariant only, durable settlement stays with M021+); browser,
email, storage and migration (no such surface touched); release gates
(M019 supplies access evidence toward G1, not qualification).

Dependencies and assumptions: M004/M005 done (evidence in their completion
records); .NET SDK `10.0.302` with locked restore; owner-staged DeepSeek
credential in the evaluation environment variable; DeepSeek research
snapshot current as of 2026-09-11 (re-check only if the live check shows
wire drift). Risk: shared billing scope means the live dispatch shares
throttling/balance risk — mitigated by the single-dispatch finite budget
and explicit command. If the credential is absent, AC-005 records blocked
and the remaining ACs still complete.

Human action: owner staged the credential before planning (done); executor
runs the one budgeted live check; human reviews the sanitized report at
handoff (blocks M015 consumption, not this package's offline ACs).
