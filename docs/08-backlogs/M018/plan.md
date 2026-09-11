# M018 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Add bounded failure/fallback traversal to the host-independent AI
surface without touching either family pipeline's single-candidate
semantics: a validated per-family `FamilyChain` snapshot (primary plus
zero or one distinct fallback, referencing `CandidateRegistry`
profiles), a chain orchestrator implementing AI §6 traversal and §7
disposition over the M016/M017 pipelines (or their shared stage
boundary) with an injectable clock for the original 30-second deadline
and per-attempt admission through the M019 `EvaluationBudget`, a
deterministic scripted fault matrix proving every advance/stop row for
both families, and a small primary-only live bounds slice recording the
same attempt/usage/exposure/deadline metadata around natural provider
calls. Nothing here routes per language/mode, retries, repairs, hedges,
charges characters, or touches API/UI/serving.

Selected canonical excerpts arrive through the context packet. Do not
copy them all here. The essential local invariants: at most three paid
dispatches per operation (eligibility accepted once and reused; a
candidate failing eligibility never transforms); fallback gets the
original complete source and original settings, never a failed result;
`effective call timeout = min(stage timeout, remaining overall time −
finalization reserve)` with no dispatch when the full stage allowance
no longer fits; every dispatch reserved at peak cache-miss rates before
launch with unresolved exposure retained on timeout/unknown usage;
candidates sharing a known-blocked `CredentialRef` scope are skipped;
live runs are primary-only and need an explicit finite
dispatch+spend+deadline budget plus a present credential, otherwise
they report blocked, never faked.

## Changes and order

1. `backend/src/LinguaDesk.Infrastructure.Ai/` — `FamilyChain` snapshot
   plus validation: per-family primary plus optional fallback candidate
   IDs resolved through `CandidateRegistry.Select`; reject unknown IDs,
   identical primary/fallback profiles (a hidden retry) and any
   route/mode-keyed selection; expose the evaluation default (primary
   `DeepSeek-V4.1-Flash`, fallback
   `DeepSeek-V4.1-Flash-SecondaryRef`). Depends on nothing; done when
   new MSTest cases prove AC-001 (acceptance plus every rejection
   class with zero dispatches).
2. Same library — chain orchestrator over both family pipelines:
   capture the family chain and overall deadline once; eligibility
   first with accepted-result reuse; fresh admission before each
   transformation dispatch; §7 advance-once versus terminal mapping
   (including `CredentialRef`-scope skip and budget-denied stop);
   cancellation at stage deadline with late-output fencing; terminal
   outcome preserves the §6 dispatch table and zero character charge
   on definitive failure, with fallback success shaped exactly like
   primary success. Depends on step 1; done when scripted-`IChatClient`
   plus fake-time/fake-admission tests prove AC-002 (both
   three-dispatch paths per family, reuse, original-source fallback,
   exactly-once visits, no retry/repair/hedge), AC-003 (every
   advance/stop row including credential-scope skip), AC-004
   (deadline capture, no-fit refusal, cancellation fencing, fake-time
   expiry at each stage) and AC-005 (reserve-before-launch, denial
   stop, unresolved-exposure retention, invisibility of fallback
   success).
3. `backend/tools/LinguaDesk.Ai.Evaluation/` — `evaluate-chain-bounds`
   workflow: allowlisted synthetic slice (a few translation directions
   including both Chinese input scripts, plus one rewriting cell per
   language) executed through the primary-only chain in `--offline`
   scripted mode and `--live` natural mode with
   `--profile/--fallback-profile/--max-dispatches/--max-spend-usd/--deadline-ms`
   flags reusing `EvaluationBudget`; the live path refuses a
   configured fallback (roadmap no-fallback rule) and records
   per-case outcome/category/attempts/usage/exposure/deadline
   timings with retained failures in a metadata-only sanitized
   report under `artifacts/`. Depends on steps 1–2; done when
   offline runs pass with no secrets (AC-006 offline portions)
   and the live shape is proven blocked-without-credential.
4. `scripts/ai.sh` + operating guide — extend the AI command surface
   (`check` covers new tests, live `evaluate-chain-bounds`
   passthrough with credential stripping for offline mode mirroring
   `evaluate-translation`) and document the explicit live invocation
   with finite budget flags sized for the small primary-only slice;
   keep offline the default. Depends on step 3; done when
   `bash scripts/ai.sh check` covers the new tests and the README
   documents the reviewed procedure (AC-006).
5. Regressions + live slice + human review — run `backend.sh check`,
   `contract.sh check`, secret sweep and `context.py check M018`;
   execute the bounded primary-only live slice against
   `DeepSeek-V4.1-Flash`; present the sanitized report for human
   review (bounds/metadata completeness, retained-failure handling).
   Depends on step 4 (offline portions may run if the live gate is
   blocked); done when all ACs hold with revision/environment/UTC
   time recorded.

No migration, rollout or rollback: no storage, config-file or serving
change. No new package dependencies expected; the AI library keeps its
single abstractions reference plus the existing Core reference.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 | T001 | V-007: `bash scripts/ai.sh check` (chain-validation MSTest cases) | `artifacts/test-results/ai.trx` |
| AC-002 | T002 | V-007: same check (both three-dispatch paths per family, reuse, exactly-once, no-retry scripted cases) | same TRX |
| AC-003 | T002 | V-007: same check (§7 advance/stop matrix incl. credential-scope skip, budget-denied stop) | same TRX |
| AC-004 | T002 | V-007: same check (fake-time deadline cases at each stage, no-fit refusal, fencing cases) | same TRX |
| AC-005 | T002 | V-007: same check (admission/exposure MSTest cases, fallback-invisibility shape cases) | same TRX |
| AC-006 offline | T003 | V-007: same check + runner offline bounds run | TRX + offline report |
| AC-006 live | T004 | V-008: `bash scripts/ai.sh evaluate-chain-bounds --live --profile DeepSeek-V4.1-Flash --max-dispatches <n> --max-spend-usd <amount> --deadline-ms <n>` (no fallback flag); human reviews sanitized report | `artifacts/` sanitized report |
| Privacy/budget | T003/T004 | Secret sweep over sources/runner/scripts/reports; `credentialRef`-only diagnostics assertion; reservation/unresolved-exposure assertions | sweep result + report |
| Regression | T005 | `bash scripts/backend.sh check`, `bash scripts/contract.sh check`, `python3 automation/context.py check M018` | task completion record |

## Context boundaries and risks

Omitted domains and why: UX/browser (no UI surface), API
handlers/auth (no serving integration), durable character
settlement/period attribution (evaluation budget only; charging lives
with M021–M025), storage/migrations, email, corpus construction and
grading (V-013 pending for M020/M035+), performance workloads and
percentile measurement (V-014 pending; the 30-second deadline is
enforced here, measured at load later), script-identity checker
selection, production serving startup and monetary-cap decisions
(Q-001), route-specific chains and shared alternatives (DF-004/
DF-001). Open on-demand: M020 package when report work starts; the
DeepSeek research snapshot only on adapter wire drift.

Dependencies and risks: M015/M016/M017 single-candidate pipelines are
Done and consumed unchanged — the orchestrator adds traversal around
them, never edits their terminal mapping; M019 registry/adapter/
budget/credential path is Done and reused (both chain profiles already
exist); live-slice cost is bounded by explicit flags plus conservative
reservation and stays small because the live path is primary-only;
fallback liveness is deliberately unproven live in this slice
(deterministic proof only) — record that limitation explicitly. If the
credential is absent, AC-006's live portion stays blocked and the
remaining ACs still complete. Any `ProductCatalog` or prompt-revision
change requires spec/plan impact analysis before execution claims the
ACs.
