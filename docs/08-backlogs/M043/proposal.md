# M043 proposal — live serving in the portal + opt-in live e2e (2026-09-15)

## Problem
`./scripts/backend.sh run` starts a portal with no LLM behind it: only
`UnavailableTranslationClientProvider` / `UnavailableRewritingClientProvider`
are registered ("no serving configuration exists yet"). Every Translate /
Rewrite POST ends as `ProviderUnavailable` → 202 pending → the frontend's
`pending` branch shows "We couldn't process this text…" with a Try again
button that can never succeed. Reference investigation: translate en→ru
"Hello." deterministically reproduces it.

## Owner decisions (locked)
1. Serving candidate: `DeepSeek-V4.1-Flash` (same as evaluation).
2. Secrets: environment variables only, purpose-scoped, never committed.
3. Conservative spend: **$0.05 per translation/rewrite operation**, written
   in config (`appsettings.json` + environment overrides for .NET backend).
4. Fail fast: `run` refuses to start unconfigured (non-zero exit naming the
   missing variables) instead of serving a dead portal.

## Part A — serving fix (backend)
- New `Infrastructure/Serving/` providers `ServingTranslationClientProvider`
  and `ServingRewritingClientProvider` implementing the existing
  `ITranslationClientProvider` / `IRewritingClientProvider` interfaces over
  `ChatCompletionsAdapter` + `CandidateRegistry` (primary-only serving chain,
  no fallback per roadmap no-fallback rule).
- Config section `Serving: { Translation: { CandidateId, CredentialRef,
  MaxSpendUsdPerOperation: 0.05 }, Rewriting: { ... } }` in
  `appsettings.json`; the secret reuses the existing owner-exported
  `LINGUADESK_AIEVALUATION__CREDENTIALS__DEEPSEEK__APIKEY` (blank/absent =
  unconfigured). No new key to provision: the evaluation and serving paths
  share the DeepSeek key, with spend separated by their own guards
  (per-run `EvaluationBudget` vs serving per-operation ceiling). The
  `..._MUSE__APIKEY` variable is not consumed by this milestone
  (serving candidate is DeepSeek only).
- Registration in `OperationsRegistrationExtensions` only when the serving
  section validates; otherwise the `Unavailable*` providers stay as the
  fail-closed fallback.
- `scripts/backend.sh run` validates serving config at startup and exits
  non-zero with the missing-variable list when unconfigured.
- Per-dispatch admission stays with `MonetaryAdmissionService`; the $0.05
  figure is enforced as the per-operation ceiling (exact option shape fixed
  in spec phase; coordinator currently passes `budget: null` and must honor
  the ceiling, not bypass it).
- UX: keep 202-pending semantics for genuinely pending operations, but an
  unconfigured service must never present a retry loop (startup fail-fast
  is the primary guard).

## Part B — e2e "live" tests (real LLM calls, opt-in only)
- `scripts/backend.sh e2e-live` (API level): boots the API with serving
  config on real keys and a temp database, creates a verified account,
  POSTs "Hello." en→ru, asserts 201 + non-empty Cyrillic text + recorded
  charge. Caps: single-digit dispatches, ≤ $1.00. No retries; fail loudly.
- Playwright `tests/e2e-live/translate.live.spec.ts` (browser level): same
  stack, real login form, drives `/translate` exactly as a user does,
  asserts a visible Result.
- Gating: run only with `LINGUADESK_E2E_LIVE=1` plus the existing DeepSeek key present.
  Explicitly requested without keys → fail. Otherwise skip. Excluded from
  `check`, `smoke`, and CI by construction.
- Negative live case: an invalid key must surface an honest error, never
  the pending-masquerade from the problem statement above.
- Keys via env only; reports from live runs carry credentialRef + presence
  (same sweep rule as M036 T005).

## Out of scope
Candidate qualification (G1), full-corpus coverage, rate limiting beyond
the per-operation cap, CI integration of live suites, mail/verification
(DF-008).
