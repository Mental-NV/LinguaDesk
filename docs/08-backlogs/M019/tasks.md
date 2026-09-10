# M019 — Tasks and evidence

Inputs: [spec](spec.md), [plan](plan.md).

## Resume

Next: T001 (awaits execution run; planning lock holds the inputs).
Blockers: none for T001–T003; T004 live gate needs the owner-staged
credential present in the execution environment.
Last check: `python3 automation/context.py check M019` — pending (run after
lock; record result here). Changed scope: none.

## Ordered tasks

- [ ] T001 — Add candidate profile model + registry validation in `backend/src/LinguaDesk.Infrastructure.Ai/`; AC-001; depends on none; done when MSTest offline cases prove multi-profile load, duplicate/invalid rejection and route-neutral selection.
- [ ] T002 — Add the OpenAI-compatible Chat Completions adapter (DeepSeek dialect, bounded reads, cancellation, dispatch counting, error/usage mapping) with fake-handler conformance fixtures; AC-002, AC-003; depends on T001; done when offline conformance and fault/cancel/bound/one-dispatch cases pass with sanitized fixtures.
- [ ] T003 — Add the credential-resolver boundary (evaluation-only secret mapping, `credentialRef`/`credentialPresent` diagnostics) and prove offline runs read no credential variables with networking disabled; AC-004; depends on T002; done when secret-sweep and env-absence tests pass.
- [ ] T004 — Add runner profile selection, offline `conformance` and live `verify-access` (one fixed-synthetic budget-admitted dispatch; blocked without credential); AC-005, AC-006; depends on T003; done when the live check succeeds for `DeepSeek-V4.1-Flash` with a secret-free report and a second credential reference is selectable without serving-rule changes.
- [ ] T005 — Extend `scripts/ai.sh` + operating guide, run full regressions and the secret sweep, fill the completion record; all ACs; depends on T004 (offline portions may run if the live gate is blocked); done when `backend.sh check`, `contract.sh check`, `ai.sh check`, `context.py check M019` and the sweep pass with revision/environment/UTC time recorded.

## Completion record

Pending execution. Record per AC/task: revision, configuration, actual
command/procedure, environment, UTC timestamp, result/evidence link,
failures/fixes and limitations. Link the sanitized live-access report;
paste no logs or key material. A missing credential records AC-005 as
blocked (not passed) with the offline ACs standing.
