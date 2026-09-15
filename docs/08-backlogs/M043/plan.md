# M043 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Bind a validated `Serving` options section (per-family candidate, credential reference, $0.05 ceiling) from non-secret config plus the existing DeepSeek env key; register primary-only serving client providers over the existing adapter/registry when valid and keep the `Unavailable*` providers otherwise; make `run` refuse to start unconfigured; enforce the per-operation ceiling in both coordinators; then prove the path with deterministic tests plus two env-gated live suites that never enter default gates.

Selected canonical excerpts arrive through the context packet. Do not copy them all here.

## Changes and order

1. Serving options and validation: add a `Serving` section (`Translation`/`Rewriting` each with `CandidateId`, `CredentialRef`, `MaxSpendUsdPerOperation = 0.05`) bound in `AddLinguaDeskOperations`, with an `IValidateOptions` validator (known candidate IDs, non-empty credential refs, positive finite ceiling). Blank/absent key material means unconfigured, never a default credential.
2. Serving providers: add `Infrastructure/Serving/ServingTranslationClientProvider` and `ServingRewritingClientProvider` implementing the existing provider interfaces over `ChatCompletionsAdapter` + `CandidateRegistry` (primary-only `FamilyChain`, no fallback). Register them only when the serving section validates; otherwise retain the `Unavailable*` registrations untouched.
3. Coordinator ceiling: replace `budget: null` in `TranslationOperationCoordinator` and `RewritingOperationCoordinator` with a per-operation ceiling derived from the serving options, so the $0.05 figure is enforced on the serving path while `MonetaryAdmissionService` keeps per-dispatch admission, monthly-cap accounting and reconciliation unchanged.
4. Fail-fast startup: `scripts/backend.sh run` validates the serving configuration before listening and exits non-zero naming the missing variables when unconfigured (partial family configuration also fails). Health/readiness probes still assert process/storage readiness only and make no paid provider calls.
5. Live evidence: add `scripts/backend.sh e2e-live` (temp database, verified account, real-key Translate POST with 201/Cyrillic/charge assertions, dispatch and spend caps, no retries, honest invalid-key error) and `tests/e2e-live/translate.live.spec.ts` (real login form → `/translate` → visible Result), both gated on `LINGUADESK_E2E_LIVE=1` plus key presence (skip otherwise, fail when explicitly requested without keys) and excluded from `check`/`smoke`/CI. Reports carry `credentialRef` + presence only.
6. Contracts and close: no wire-schema change is expected (serving reuses existing operations contracts); run `contract.sh check` to prove zero drift, run the full default gates, refresh the context lock after review and close the package. No migration; rollback removes the serving registration/options and restores unconditional `Unavailable*` behavior together.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001/AC-002 | T002 | Serving-provider + coordinator tests with scripted `IChatClient`: 201 result, single full-length charge, recorded provider charge, source-language rewrite | tasks.md completion record |
| AC-003 | T003 | Per-operation ceiling tests: over-cap dispatch denied pre-call → 503/MSG-017, zero charge, no fallback; at-cap admitted; monthly-cap path unchanged | tasks.md completion record |
| AC-004 | T001 | Serving-options validation tests + `run` startup guard: unconfigured/partial config exits non-zero naming missing variables; configured starts | tasks.md completion record |
| AC-005 | T004 | `LINGUADESK_E2E_LIVE=1 bash scripts/backend.sh e2e-live` (owner-authorized; ≤ $1.00, single-digit dispatches); invalid-key honest-error case | tasks.md completion record |
| AC-006 | T004 | Live Playwright case `tests/e2e-live/translate.live.spec.ts` under the same opt-in gate: real login → visible Result | tasks.md completion record |
| AC-007 | T004/T005 | Default gates need no key and never invoke live suites: `bash scripts/backend.sh check`, `bash scripts/backend.sh smoke`, `bash scripts/contract.sh check`, `bash scripts/frontend.sh check`, `bash scripts/frontend.sh smoke`; secret-sentinel scan per V-015 | tasks.md completion record |
| Readiness | T005 | `python3 automation/context.py check M043` fresh at close; `bash scripts/verify-milestone.sh M043`; `git diff --check` | tasks.md completion record |

## Context boundaries and risks

Omitted domains and why: account/registration/sign-in flows are reused unchanged (M034/M013 Done; live suites use the dedicated verified E2E account through the real login boundary); character-allowance accounting is unchanged except for consuming existing admission/settlement; prompt/parser/eligibility behavior is reused (M015–M018 Done); quality evaluation stays with M036 and performance rehearsal with M037; visual/responsive/accessibility matrices stay with M038/M039; release gates are untouched (milestone completion ≠ release). Open on demand: M026/M027 coordinator internals when diagnosing settlement regressions; M019 access evidence if the shared-key assumption is questioned.

Dependencies: M026, M027, M028, M029, M019 are Done per delivery/current.md with specs referenced in the manifest. Q-001 remains open but its serving slice (candidate, tariff bounds, shared credential) is owner-locked and present: the DeepSeek key is already exported (M019 evidence) and present in this environment. Primary risks are bypassing the ceiling on one family (both coordinators change together with a shared ceiling helper), serving a half-configured portal (validator + `run` guard fail on partial config), live suites leaking into default gates (gating tested by running default gates without the flag), and key material entering evidence (credentialRef + presence only, sentinel scan). Human action owner/timing/gate: owner (or authorized runner) executes the two opt-in live suites at the end; this blocks only AC-005/AC-006 evidence.
