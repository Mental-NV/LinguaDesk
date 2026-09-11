# M033 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Add one small standalone consumer sample that proves the public API
is independently usable: plain HTTP calls with a local-account
Bearer token, no SPA and no React, covering translate → rewrite →
usage → recovery → classified errors in a single deterministic run
against an isolated local host (fake provider, seeded account).
Separately, regenerate the OpenAPI/TypeScript artifacts and record
a semantic review tying each touched wire shape to observed runtime
behavior. No handler, policy or schema change is intended; any
behavioral mismatch found by the review becomes a reported finding,
not a silent contract edit.

## Changes and order

1. Sample scaffold: new `samples/api-consumer/` with a small Node
   script (plain `fetch`, no React) importing only the committed
   generated types at `frontend/src/api/generated/linguadesk-api.d.ts`
   for request/response shapes, plus a README with prerequisites,
   run command and expected output. Token handling reads the Bearer
   token from an environment variable; the sample never prints it.
2. Flow: sign-in → `POST /api/operations` (translation identity,
   UUIDv7) → poll `GET /api/operations/{operationId}` to terminal →
   `POST /api/operations` (rewrite identity, one mode) → poll →
   `GET /api/usage` → duplicate/conflict/error demonstrations
   (AC-003/AC-004). Each step prints the outcome category, result
   or failure classification, charge day/count and snapshot.
3. Contract review: run `bash scripts/contract.sh generate` then
   `check`; diff the delta. For each touched shape, assert the
   observed status code, envelope field and error category against
   the generated schema and record the agreement in tasks.md.
4. Order: scaffold → flow against isolated host → failure/recovery
   demonstrations → contract regenerate/check + semantic review →
   regression suites.
5. No migration, rollout or production configuration. Rollback is
   the prior commit. The sample targets a local host only and must
   not be pointed at any shared or production deployment.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001/AC-002 | T002 | V-009: deterministic sample run against isolated host with fake provider; `bash scripts/backend.sh` aggregate stays green | tasks.md completion record |
| AC-003 | T003 | V-009: sample recovery/error demonstration output (replay metadata, 409 conflict, usage snapshot) | tasks.md completion record |
| AC-004 | T003 | V-009: classified-failure demonstration (4xx input + 401) with Problem Details categories | tasks.md completion record |
| AC-005 | T004 | V-009: `bash scripts/contract.sh check` clean + recorded semantic review of touched shapes | tasks.md completion record |
| AC-006 | T002/T003 | V-015 (sample portion): token redaction + synthetic-text-only inspection of sample output | tasks.md completion record |
| M026/M027 reuse | T001 | Regression: existing translation/rewriting/auth/accounting suites stay green; `bash scripts/backend.sh` | tasks.md completion record |
| Readiness | T005 | `python3 automation/context.py check M033` fresh at close | tasks.md completion record |

## Context boundaries and risks

Omitted domains and why: SPA/browser journeys (no UI in this
slice; M028–M032 own that evidence); translation/rewriting
policy internals (M016–M018/M022–M025 reused, not re-proven);
LLM evaluation corpora and qualification (Q-005/G1 — fake only);
performance workloads (G2/M037); email delivery (M034);
device/AT/browser matrix (G3/M038/M039); backup/restore
lifecycle (M040/M041, Q-004). Open on demand: M026/M027 specs
for operation semantics the sample touches; ADR index only if
the review changes a recorded decision (none intended).

Dependencies, assumptions, blockers: M026/M027 Done per
delivery/current.md — satisfied. Local host, fake provider and
seeded local account are existing test capabilities; no new
access needed. Human steps: none required; no owner/timing/
blocked gate.

Risks: sample drifting into a second client implementation
(mitigated by importing generated types, not redefining shapes);
sample asserting only its own fixtures as proof of charging
(regression suites, not the sample, own accounting evidence);
review stopping at generation parity (AC-005 requires observed
runtime agreement per shape); token leakage in output (redaction
rule + inspection).
