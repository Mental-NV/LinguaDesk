# M043 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: none (all tasks done; package ready for delivery-status update).
Blockers: none.
Last check: `bash scripts/verify-milestone.sh M043` equivalent gates all green
2026-09-15T19:06Z (backend check 571/571, backend smoke, contract check,
AI check 313/313, AI probe, frontend check 261, frontend smoke 61/61,
`context.py check M043`, `context.py audit`, `git diff --check`).
Changed scope: none (working tree holds only the M043 implementation).

## Ordered tasks
- [x] T001 — Serving options, validation and fail-fast `run`; AC-004; depends on none; done when options-validator tests pass and unconfigured/partial `run` exits non-zero naming missing variables.
- [x] T002 — Serving providers + coordinator ceiling for Translate and Rewrite; AC-001/AC-002/AC-003; depends on T001; done when scripted-adapter tests prove 201 results, single full-length charges with recorded provider charges, and ceiling enforcement on both families.
- [x] T003 — Deterministic failure-boundary tests (over-cap denial → 503/MSG-017 zero-charge no-fallback; at-cap admission; `Unavailable*` fallback retained when unconfigured; pending/202 semantics unchanged for genuinely pending work); AC-003/AC-004; depends on T002; done when focused tests pass.
- [x] T004 — Live suites (`e2e-live` API script + Playwright live browser case) with opt-in gating, dispatch/spend caps and credentialRef-only reporting; AC-005/AC-006/AC-007; depends on T003; done when both suites pass under `LINGUADESK_E2E_LIVE=1` with the key present, skip without the flag, and default gates show no live invocation.
- [x] T005 — Contract drift (`contract.sh check`), full default gates, context lock refresh and close; all ACs; depends on T004; done when `verify-milestone.sh M043`, fresh `context.py check M043` and `git diff --check` pass and the completion record below is filled.

## Completion record
Executed 2026-09-15T19:06:44Z at revision `9020096` (working tree below),
Release configuration, .NET SDK 10.0.302, Node v24.20.0 / npm 11.11.0.

- T001: `ServingOptions` (`Serving:Translation/Rewriting` with
  `CandidateId`, `CredentialRef`, `MaxSpendUsdPerOperation` default `0.05`,
  fail-closed empty defaults) plus `ServingOptionsValidator` (known candidate
  IDs, credential-ref match, positive finite ceiling; partial config fails),
  `ServingCredential` (env-only `LINGUADESK_AIEVALUATION__CREDENTIALS__<REF>__APIKEY`,
  blank/absent = unconfigured) and `ServingConfiguration` missing-variable
  reporting. `scripts/backend.sh run` validates before restore/listen and
  exits 1 naming missing variables (verified: empty, partial and blank-key
  cases); `ServingStartupGuard` backstops direct `dotnet run`
  (Development only; production-like hosting keeps the M006 fail-closed
  pending contract). Evidence: `ServingOptionsTests` (12 tests).
- T002: `ServingTranslationClientProvider` / `ServingRewritingClientProvider`
  over `ChatCompletionsAdapter` + `CandidateRegistry` as primary-only chains
  (no fallback; `Fallback` null), registered only when the section validates
  with `Unavailable*` retained otherwise; `ServingChatClient` bridges the
  adapter to `IChatClient` without double budget reservation and reports the
  last attempt for chain settlement. Both coordinators build a per-operation
  `EvaluationBudget` (4 stages, family ceiling default `0.05`) instead of
  `budget: null` and map `ChainDecision.BudgetDenied` to
  monetary suspension (503 budget category). No `IValidateOptions<Serving>`
  DI registration: validating on read would turn the pending path into a 500
  (found by 31 existing-test failures, fixed; pending/202 preserved).
  Evidence: `ServingProviderTests` (5 tests: primary-only chains, response
  text, `IChainAttemptReporter`, blank-key `Blocked`, 401 `ProviderFailure`).
- T003: `ServingCeilingTests` (5 tests): over-cap denied pre-call
  (503 `monetarySuspension`, zero provider calls, zero charge, no fallback on
  both families); exactly-at-cap (`0.014746` = 2 × stage bound `0.007373`)
  admitted with 201; just-below-cap (`0.014745`) denied on the second stage
  with no fallback. `Unavailable*` retention proven by the untouched pending
  tests (`TranslationWithoutConfiguredProviderRemainsPending`,
  admission pending cases). Evidence: focused 22/22 serving tests green,
  then full `backend.sh check` 571/571.
- T004: `scripts/backend.sh e2e-live` (skip without `LINGUADESK_E2E_LIVE=1`,
  loud fail when explicitly requested without keys; self-publishes the API to
  a temp dir on a temp database over HTTPS loopback) plus
  `frontend/tests/e2e-live/translate.live.spec.ts` under
  `frontend/playwright.live.config.ts` (separate testDir, never in default
  gates). Owner-authorized run 2026-09-15: invalid-key phase → honest 503
  `processingFailure` (never 202); happy phase → 201 `Здравствуйте.`,
  full-length charge 6/6; browser phase → real login form → `/translate` →
  visible Cyrillic Result (1 passed). Report carries only
  `credentialRef=deepseek credentialPresent=true`, 2 live submission requests
  (no retries), per-operation cap $0.05, run cap $1.00.
- T005: `contract.sh check` zero drift (no wire change); `backend.sh check`
  571/571 (API 248 incl. 22 new serving tests, core 10, AI 313);
  `backend.sh smoke`, `ai.sh check` (313), `ai.sh probe`, `frontend.sh check`
  (261 incl. lint/typecheck/build), `frontend.sh smoke` (61/61) all green;
  `context.py lock M043` refreshed after diff review, `check M043` and
  `audit` green, `git diff --check` clean. No live invocation inside any
  default gate (verified by the green keyless runs above); sentinel posture
  unchanged (credentialRef + presence only in live reporting; no key material
  in code, config, logs or evidence).
