# M019 — Configurable candidate access backlog

**Document:** #8 · **Version:** 1.0 · **Updated:** 2026-09-10
**State:** Selected; package AC-001–006 pending execution
**Roadmap:** [M019 — Connect and verify configurable candidate access](../../07-roadmap.md#43-independently-testable-language-behavior)

## 1. Outcome and authoritative inputs

An evaluator can configure multiple provider-neutral candidate profiles, reach a
real provider through the first OpenAI-compatible Chat Completions adapter, and
prove live access for `DeepSeek-V4.1-Flash` (`deepseek-flash` at
`https://api.deepseek.com`) with one budgeted dispatch that never exposes the
shared credential. The registry holds profiles beyond the two serving chains,
each profile references a provider credential instead of owning one, and the
evaluator can select separately configured providers/routes without
implementing deferred serving rules (DF-004) or assuming one credential per
family. Basis: AI §5; Q-001/Q-007 (access evidence only, not qualification);
V-008; ADR-012; D-19 direction with DF-004 remaining deferred.

M004 (host-independent AI inner loop) and M005 (shared input/capability
contract) are Done and serve as dependency evidence only; their packages are
opened on demand for regression questions, not reread here. At selection the
solution contains the `LinguaDesk.Infrastructure.Ai` library (eligibility
prompt snapshot, scripted boundary) and the `LinguaDesk.Ai.Evaluation` runner
with `inspect`/`probe` only — no candidate registry, no transport adapter, no
credential resolver, no live mode. M019 adds exactly that access slice.

## 2. Item

| Item ID | Outcome/title | Priority | Target milestone | Dependencies/blockers | State | Delivery package |
| --- | --- | --- | --- | --- | --- | --- |
| BI-019 | Connect and verify configurable candidate access | Next / AI-path enablement | M019 | M004, M005 done; owner-reported DeepSeek credential present in the evaluation environment variable | selected | [M019](spec.md) |

### BI-019 — Connect and verify configurable candidate access

**Value and scope:** Add a multi-profile non-secret candidate registry with
provider-neutral `CredentialRef` values, the first OpenAI-compatible Chat
Completions adapter (DeepSeek dialect: explicit non-thinking control, bounded
JSON output), sanitized transport conformance through the real serialization
path, an external credential resolver, and an explicit one-dispatch live
access check admitted by a finite evaluation budget. Detailed acceptance is
owned by [package AC-001–006](spec.md#acceptance).

**Exclusions:** Language eligibility/transformation behavior (M015–M017),
family-chain fallback/deadline policy (M018), corpus/budget/report evaluation
execution (M020), API composition and HTTP/OpenAPI/client work, DF-004 route
tables and longer fallback chains, candidate quality/cost/latency
qualification, production serving, and any per-route provider assignment.
A successful access check proves only momentary reachability; it is not
quality, context-limit, price, caching or serving evidence.

**Boundaries:** Credentials stay in the external environment source and the
narrow transport credential type; never in `appsettings*.json`, profile JSON,
`.runsettings`, source, scripts, snapshots or reports. Diagnostics may report
`credentialRef` and `credentialPresent`, never the value. Offline tests read
no credential variables and run with provider networking disabled. A missing
or blank credential makes a requested live run blocked/non-success, never a
scripted pass. The adapter implements no SDK/HTTP retries or hedging and
falls back to no unlimited defaults.

## 3. Human steps and prerequisites

No new human input is required to begin: M004/M005 are done, the .NET 10 SDK
pin and locked restore are routine autonomous setup, and offline
conformance/test work needs no credential. The owner already reports the
shared DeepSeek credential present in
`LINGUADESK_AIEVALUATION__CREDENTIALS__DEEPSEEK__APIKEY` for the evaluation
environment. The execution's single live dispatch consumes a small amount of
that shared billing scope under owner authorization D-19; the executor
records the actual admitted budget and observed usage without printing the
key. End-of-milestone human verification: review the sanitized live-access
report (reference-only credential identity, live-call disposition, no
secret material) before M015 consumes this path. If the credential is absent
at execution, stop the live gate, record it blocked, and report it without
claiming the gate passed.

Acceptance summary: multi-profile registry with credential references,
DeepSeek-dialect adapter conformance, secret-safe credential resolution, one
budgeted live access success for `DeepSeek-V4.1-Flash`, and separately
selectable provider profiles without deferred routing; detailed ACs are in
spec.md.
Human steps: owner-supplied credential already present; executor runs the
bounded live check and a human reviews the sanitized report at handoff.
