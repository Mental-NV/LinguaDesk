# LinguaDesk.Ai.Evaluation runner

Offline-first evaluation harness for the LinguaDesk AI pipelines. It exercises
eligibility, translation, rewriting, chain-bounds, and combined-report flows
against scripted fixtures (offline) or a real provider (live), and emits
canonical JSON observations plus saved report files.

- Project: `backend/tools/LinguaDesk.Ai.Evaluation/LinguaDesk.Ai.Evaluation.csproj`
- Library under test: `backend/src/LinguaDesk.Infrastructure.Ai`
- Requires .NET SDK `10.0.302` (`bash scripts/ai.sh setup` restores with `--locked-mode`).

## How to run

Canonical entry point is `scripts/ai.sh` (it strips credential env vars and
dead-ends proxies for offline runs). Direct `dotnet` works too after building.

```bash
bash scripts/ai.sh setup
bash scripts/ai.sh check                      # offline build + full AI test suite

# No-arg probes (always offline, zero dispatches)
bash scripts/ai.sh inspect
bash scripts/ai.sh probe
bash scripts/ai.sh conformance
bash scripts/ai.sh verify-access --profile DeepSeek-V4.1-Flash \
  --max-dispatches 1 --max-spend-usd 0.05

# Family evaluations (default offline; add --live for provider dispatches)
bash scripts/ai.sh evaluate-eligibility --profile DeepSeek-V4.1-Flash \
  --max-dispatches 50 --max-spend-usd 1.00
bash scripts/ai.sh evaluate-translation --profile DeepSeek-V4.1-Flash \
  --max-dispatches 50 --max-spend-usd 1.00
bash scripts/ai.sh evaluate-rewriting --profile DeepSeek-V4.1-Flash \
  --max-dispatches 50 --max-spend-usd 1.00
bash scripts/ai.sh evaluate-chain-bounds --profile DeepSeek-V4.1-Flash \
  --max-dispatches 50 --max-spend-usd 1.00
bash scripts/ai.sh evaluate-report --profile DeepSeek-V4.1-Flash \
  --max-dispatches 50 --max-spend-usd 1.00
```

Direct equivalent (from repo root, after `dotnet build` in Release):

```bash
DLL=backend/tools/LinguaDesk.Ai.Evaluation/bin/Release/net10.0/LinguaDesk.Ai.Evaluation.dll
dotnet $DLL inspect
dotnet $DLL evaluate-translation --offline --profile DeepSeek-V4.1-Flash \
  --max-dispatches 50 --max-spend-usd 1.00 --output /tmp/tr.json
```

`evaluate-chain-bounds` and `evaluate-report` also accept `--deadline-ms <n>`
(positive finite overall deadline). All `evaluate-*` commands accept at most
one of `--live` / `--offline`; omitting both means offline. `--output <path>`
saves the report copy to that path instead of the default `artifacts/` path.

Exit codes: `0` success (report `status` `pass`/`success`), `1` check or live
failure (`fail`/`error`), `2` usage error (bad/missing flags, unknown
candidate), `3` blocked — credential missing/blank, zero dispatches made.

## How to configure (including changing the LLM model)

Selection is by candidate ID only (`--profile <candidate-id>`, exact match).
There are no family-owned credentials or route-table defaults.

Current registry (`backend/src/LinguaDesk.Infrastructure.Ai/CandidateRegistry.cs`):

| CandidateId | Endpoint | Model | CredentialRef |
| --- | --- | --- | --- |
| `DeepSeek-V4.1-Flash` | `https://api.deepseek.com` | `deepseek-flash` | `deepseek` |
| `DeepSeek-V4.1-Flash-SecondaryRef` | `https://api.deepseek.com` | `deepseek-flash` | `deepseek-secondary` |

`FamilyChain.EvaluationPrimaryId` / `EvaluationFallbackId`
(`backend/src/LinguaDesk.Infrastructure.Ai/FamilyChain.cs`) point at these two
IDs; keep them in sync if you rename a candidate.

To change the model (or add a candidate), edit `CandidateRegistry.Default`
with a new `CandidateProfile`: candidate ID, adapter ID
(`openai-chat-completions-v1`), HTTPS endpoint, wire `model` string,
provider-scoped `credentialRef` (key material never lives in the profile),
`EffectiveSettings`, prompt/validator revisions, finite `ContextBounds`, and a
`BillingProfile` with positive peak per-million-token rates. `Validate()`
rejects anything outside the envelope: temperature must be exactly `0`, no
`top_p`, no tools, thinking exactly `disabled`, bounded JSON response mode on,
and all bounds/billing finite and positive (see `CandidateProfile.cs`). The
`conformance` command asserts the wire body carries exactly these settings.

Credentials resolve from the environment as
`LINGUADESK_AIEVALUATION__CREDENTIALS__<REF>__APIKEY`, with `-` uppercased to
`_`, e.g. `...__DEEPSEEK__APIKEY` and `...__DEEPSEEK_SECONDARY__APIKEY`.
Blank/missing means blocked (exit `3`), never a silent offline substitution.
Budgets are per-run and mandatory: `--max-dispatches` and `--max-spend-usd`
must both be positive and finite, even for offline runs.

## Inputs

- CLI flags: `--profile` (required for `verify-access` and all `evaluate-*`),
  `--max-dispatches` + `--max-spend-usd` (required, always), `--live` /
  `--offline` (at most one; default offline), `--output` (optional report
  path), `--deadline-ms` (chain-bounds/report only).
- Built-in development case slices compiled into each `Evaluate*.cs`
  (eligibility/translation/rewriting/bounds fixtures, including fault cases).
  These are dev/calibration inputs, disjoint by design from the release corpus.
- `Corpus/batch-b1.json` (schema `corpus-b1.v1`) is the M035 release-corpus
  draft under human review — related context, not a CLI input to these
  commands. Do not feed dev-slice IDs or source text into release evidence.
- Live runs additionally read the credential env var above; offline runs must
  see none (the `ai.sh` wrapper enforces this).

## Outputs

- `stdout`: one canonical camelCase JSON document per invocation. `kind`
  identifies it: `eligibility_evaluation_report`,
  `translation_evaluation_report`, `rewriting_evaluation_report`,
  `chain_bounds_report`, `evaluation_report` (combined), plus
  `verify_access_report`, `conformance_report`, `raw_scripted_observation`
  (probe), and the `inspect` prompt snapshot (`promptId`, `resourceSha256`,
  `messages`).
- Report file: a copy of the stdout document, defaulting to
  `artifacts/<section>/evaluate-<command>-<utc-stamp>-offline|live.json`
  under the repo root (e.g. `artifacts/translation/`), or `--output <path>`.
  The file path is echoed on stderr (`... evaluation report: <path>`).
- Envelope fields to expect: `candidateId`, `credentialRef`,
  `credentialPresent`, `live` (must be `false` unless `--live` with a real
  credential), per-case rows (`caseId`, route/cell, `decision`,
  `matchesReference`, `dispatches`, token usage, `reservedUsd`/`actualUsd`),
  aggregate `status` (`pass`/`fail` or `success`/`error`/`blocked`), and
  `mismatches`/`failed` counters. Row `disposition` is exactly one of
  `offline_fixture`, `fault_injected`, or `live_development`.

## How to review

1. Confirm the mode: `live: false` and `offline_fixture`/`fault_injected`
   dispositions for offline claims; `live: true` with `live_development` rows
   only for live runs. Any mixture or a live-labeled run with zero dispatches
   is a finding, not evidence.
2. Check the verdict: `status` pass/success with `mismatches == 0` and
   `failed == 0`; `verify-access` success with usage reported. `blocked`
   (exit 3) means re-run with the credential present — never treat it as a
   pass.
3. Audit spend: `dispatchesUsed <= maxDispatches`,
   `reservedUsd <= maxSpendUsd`, `usageKnown` true on live rows, and
   `returnedModel`/`returnedFingerprint` recorded where the provider returns
   them.
4. Check determinism: run `inspect` twice — `promptId` and `resourceSha256`
   must be identical. `conformance` must pass (exit 0) after any adapter,
   settings, or registry change.
5. Keep boundaries: offline output never substitutes for live qualification;
   fault-injected rows are labeled separately and never counted as successes;
   release-corpus review (M035 `Corpus/batch-b1.json` approvals) is a
   separate human gate — these runner reports do not approve it.
