# Plan — config-first refactoring (JSON defaults, env overrides, secrets stay env-only)

## Goal
Move non-secret application policy from scattered env-var exports into committed JSON config files, keeping API keys env-only and preserving fail-fast behavior. Fix the README's env-only serving story so `run` starts with only the DeepSeek key exported.

## Success Criteria
- `backend/src/LinguaDesk.Api/appsettings.json` (plus `appsettings.Development.json`) holds the Serving/Operations policy; no effective value changes.
- `bash scripts/backend.sh run` starts with only `LINGUADESK_AIEVALUATION__CREDENTIALS__DEEPSEEK__APIKEY` exported; without it, startup refuses and names exactly that variable.
- Supplying the full old env set produces byte-identical effective behavior (equivalence, proven by existing suites).
- No secret appears in any committed file (sweep clean).

## Context And Current Facts
- No `appsettings*.json`/`appconfig*.json` exist; the API is env-only plus code defaults. `WebApplication.CreateBuilder` (`Program.cs`) already merges JSON → env (`__` maps to `:`), and all options bind via `GetSection(...).Get<T>()` — no binding-code changes needed.
- Sections consumed: `Serving` (2 families × CandidateId/CredentialRef/MaxSpendUsdPerOperation=0.05), `Operations` (allowances default 20000/2000000 in code), `MonetaryAdmission` (cap, currency), `Security`, `Storage`.
- Over-use sites: README documents 6 env exports where 5 are non-secret policy (`README.md:50-62`); `scripts/backend.sh` hardcodes `Serving__*` in the run guard (~lines 39-54) and `e2e-live` (~lines 657-662) — a bash check cannot see merged JSON and would false-fail after the move.
- Correctly env-only today (unchanged): the DeepSeek key (`ServingCredential` already resolves the existing eval-scoped variable — no new key was ever needed), smoke/live test passwords, dev-cert paths, `LINGUADESK_OPENAPI_GENERATION`, machine-local `Storage__DatabasePath` / `Security__DataProtectionKeysPath` computation.
- `ServingStartupGuard` already validates effective merged config (app side); `ServingCredential.TryResolve` and `CollectMissingVariables` read raw env instead of `IConfiguration`.
- Existing tests: `ServingOptionsTests`, `ServingCeilingTests`, `ServingProviderTests` (extend, no new harness).

## Constraints And Non-goals
- API keys never enter JSON (per owner instruction); machine-local paths stay script-computed env; frontend `.env`, `ai.sh`/benchmark credential env, `SmokeAccountSeeder` flags untouched.
- No effective default value changes — verbatim moves only.
- No commits (owner owns Git); no production-code behavior change beyond config sourcing.

## Key Decisions
1. **File names: `appsettings.json` + `appsettings.Development.json`, not `appconfig.json`.** `CreateBuilder` loads these automatically; `appconfig.*` would need custom wiring for zero benefit.
2. **JSON holds portable non-secret policy: full `Serving` section (verbatim values) + `Operations` allowances (verbatim code defaults).** Rule for the rest, applied at implementation: non-secret portable policy → JSON; secret or machine-local → env. `MonetaryAdmission`/`Storage`/`Security` keep current behavior unless they meet the JSON rule with unchanged effective values.
3. **Bash serving pre-check is removed; fail-fast moves fully to the app guard.** The script keeps one check — presence of the DeepSeek key variable (the only thing that must be env) — because bash cannot evaluate merged config. Effective-config validation stays in `ServingStartupGuard`, which already covers it.
4. **`ServingCredential` / `CollectMissingVariables` read via `IConfiguration` instead of raw env.** Values stay env-sourced; this only unifies the lookup so JSON-supplied and env-supplied values satisfy the same guard (plus User-Secrets/testability).
5. **`appsettings.Development.json` ships local-only non-secret deltas; `{}` if none are found.** Committed (no secrets by construction); secrets override via env in every environment.

## Recommended Approach
Add the two JSON files with verbatim policy, repoint the two env-direct lookups at `IConfiguration`, strip the duplicated `Serving__*` exports and the JSON-blind bash pre-check from `scripts/backend.sh` (keeping path computation and the single key-presence check), and rewrite the README run section around one key export plus the JSON-first/override rule. Extend the existing serving options tests with a JSON-bind case.

## Work Plan
1. Add `backend/src/LinguaDesk.Api/appsettings.json` (`Serving` both families verbatim; `Operations` allowances verbatim) and `appsettings.Development.json` (`{}` unless local deltas are found).
2. Verify packaging: JSON must reach both `dotnet run` (project dir) and the built-dll path used by `smoke`; add explicit `Content` copy entries to the csproj if the SDK default does not carry them.
3. Rework `ServingConfiguration.CollectMissingVariables` and `ServingCredential.TryResolve` onto `IConfiguration`; keep fail-closed semantics; extend `ServingOptionsTests` with a JSON-bind/validate case.
4. Simplify `scripts/backend.sh`: remove `Serving__*` inline blocks (run guard, `e2e-live`); keep Storage/Security path computation; keep a presence check for the DeepSeek key variable only.
5. Rewrite README serving/run sections: config-first model, `__`-override rule, single-key example, secrets list.
6. Apply the Section-5 rule to `MonetaryAdmission`/`Storage`/`Security`; move only what meets it with unchanged effective values.

## Validation Plan
- `bash scripts/backend.sh check` (full backend suite incl. serving tests) and `bash scripts/backend.sh smoke` green.
- Manual: `run` without the key → startup refusal naming exactly the DeepSeek variable; with the key → starts; `e2e-live` still skips without its flag.
- Equivalence: existing suites (which pin current effective values) pass unchanged.
- `git diff --check`, secret sweep over the new JSON + edited scripts, `python3 automation/context.py audit` clean.

## Risks / Rollback
- SDK copy semantics for the dll-run path (mitigated by work unit 2's explicit check).
- A JSON typo fails startup loudly (acceptable — fail-fast, caught by smoke).
- Accidental secret commit (mitigated by sweep + owner review of the diff).
- Rollback: delete the two JSON files and restore the env exports; binding code is untouched so removal is clean.

## Open Questions
None. File naming, Development-file handling, and the Monetary/Storage/Security boundary are settled above as stated assumptions; values move verbatim so no product decision is required.
