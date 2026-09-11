# M016 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Add the translation slice to the host-independent AI surface: a strict
`translation.v1` result-envelope parser, a `translation.v1` prompt resource
plus operation pipeline (M015 eligibility reuse, one transformation dispatch
through the M019 adapter path, terminal validated-text-or-recorded-failure
mapping), and an explicit budgeted runner workflow executing a reviewed
synthetic live slice over all 12 Translation directions. Nothing here
rewrites, retries, falls back, or touches API/UI/serving. A refusal or
invalid output is recorded failure, never salvageable success; ineligible
input never reaches transformation.

Selected canonical excerpts arrive through the context packet. Do not copy
them all here. The essential local invariants: parser implements exactly the
§4.2 translation row; prompt serialization never interpolates source into
instruction text; counting reuses Core `unicode-scalar-v1` via the M015
pipeline with no normalization; live runs need an explicit finite
dispatch+spend budget and a present credential, otherwise they report
blocked, never faked.

## Changes and order

1. `backend/src/LinguaDesk.Infrastructure.Ai/` — `TranslationEnvelopeParser`
   plus outcome model: strict §4.2 translation acceptance/rejection classes,
   raw-byte bound before deserialization, refusal-wording-in-source rule
   (parser refusal only from envelope status, never keyword matching).
   Depends on nothing; done when new MSTest offline cases prove AC-001
   (every valid form, every rejection class).
2. Same library — `translation.v1` prompt resource (`Prompts/`) plus
   `TranslationPrompt` composer (trusted system instruction, JSON-serialized
   user data with complete source/resolved source/explicit target), and
   `TranslationPipeline` (M015 `EligibilityPipeline` first: local gates and
   terminal mapping unchanged; on `Eligible`, one transformation call
   through `CompleteResponseBoundary`, then strict envelope validation and
   §4.3/§7 deterministic rejection with recorded failure, no retry).
   Depends on step 1; done when scripted-`IChatClient` tests prove AC-002
   (12-direction validated results, prompt-data separation, plain-text-only
   return) and AC-003 (refusal/invalid/truncated/tool-call/failure handling,
   zero transformation dispatches for ineligible input).
3. `backend/tools/LinguaDesk.Ai.Evaluation/` — `evaluate-translation`
   workflow: allowlisted synthetic case set (≥1 case per Translation
   direction, both Chinese input scripts, short/ambiguous texts, one
   refusal-scripted and one malformed-scripted edge case),
   `--profile/--max-dispatches/--max-spend-usd` budget flags reusing
   `EvaluationBudget`, metadata-only sanitized report under `artifacts/`
   with per-case outcome/category/attempts/usage/exposure and retained
   failures. Depends on steps 1–2; done when offline runs (scripted) and
   the live slice shape pass without secrets (AC-004 offline, AC-005
   offline portions).
4. `scripts/ai.sh` + operating guide — extend the AI command surface
   (`check` covers new tests, live `evaluate-translation` passthrough with
   credential stripping for offline modes mirroring
   `evaluate-eligibility`) and document the explicit live invocation with
   finite budget flags; keep offline the default. Depends on step 3; done
   when `bash scripts/ai.sh check` covers the new tests and the README
   documents the reviewed procedure (AC-004/AC-005).
5. Regressions + live slice + human review — run `backend.sh check`,
   `contract.sh check`, secret sweep and `context.py check M016`; execute
   the bounded live slice against `DeepSeek-V4.1-Flash`; present the
   sanitized report for human review (including zh-target output spot
   review). Depends on step 4 (offline portions may run if the live gate is
   blocked); done when all ACs hold with revision/environment/UTC time
   recorded.

No migration, rollout or rollback: no storage, config-file or serving
change. No new package dependencies expected; the AI library keeps its
single abstractions reference plus the existing Core reference.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 | T001 | V-007: `bash scripts/ai.sh check` (parser MSTest cases) | `artifacts/test-results/ai.trx` |
| AC-002 | T002 | V-007: same check (12-direction scripted pipeline cases, prompt-separation cases) | same TRX + fixture report |
| AC-003 | T002 | V-007: same check (refusal/invalid/truncation/tool-call/failure scripted cases, zero-dispatch ineligible cases) | same TRX |
| AC-004 offline | T003 | V-007: same check + runner offline synthetic run | TRX + offline report |
| AC-004 live | T004 | V-008: `bash scripts/ai.sh evaluate-translation --live --profile DeepSeek-V4.1-Flash --max-dispatches <n> --max-spend-usd <amount>`; human reviews sanitized report | `artifacts/` sanitized report |
| AC-005 | T003/T004 | Secret sweep over sources/fixtures/scripts/reports; `credentialRef`-only diagnostics assertion; budget reservation/unresolved-exposure assertions | sweep result + report |
| Regression | T005 | `bash scripts/backend.sh check`, `bash scripts/contract.sh check`, `python3 automation/context.py check M016` | task completion record |

## Context boundaries and risks

Omitted domains and why: UX/browser (no UI surface), API handlers/auth
(no serving integration), durable accounting/settlement (evaluation budget
only; character charge lives with later API milestones), storage/migrations,
email, rewriting modes (M017 owns mode validation), fallback/deadline
traversal (M018 owns retry/fallback/deadline proof), performance workloads
(V-014 pending for M037), corpus qualification and grading (V-013 pending
for M020/M035/M036), script-identity checker selection (qualification
prerequisite; zh-target outputs are human spot-reviewed here, not
checker-gated). Open on-demand: M017/M018 packages when rewriting/fallback
work starts; the DeepSeek research snapshot only on adapter wire drift.

Dependencies and risks: M015 eligibility/parser/runner patterns are Done and
reused; M019 adapter/budget/credential path is Done and reused; live-slice
cost is bounded by explicit flags plus conservative reservation; model
translation quality in the slice is development evidence only, not
qualification — the reviewed report records limitations. If the credential
is absent, AC-004's live portion stays blocked and the remaining ACs still
complete. Direction coverage depends on `ProductCatalog.TranslationDirections`
(12 directions verified in code); any catalog change requires spec/plan
impact analysis before execution claims AC-002.
