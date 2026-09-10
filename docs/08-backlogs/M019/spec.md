# M019 — Selected specification

Selected items: BI-019. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: multi-profile candidate registry (required fields and validation
per AI §5.1); provider-neutral credential references with an external
resolver (AI §5.2); the first OpenAI-compatible Chat Completions adapter with
the DeepSeek dialect subset (endpoint/model/auth selection from the profile,
temperature `0`, no `top_p` override, no tools, explicit
`thinking: {"type":"disabled"}`, bounded JSON output); sanitized transport
conformance through real serialization; error/usage mapping, cancellation,
bounded reads and dispatch counting; an explicit one-dispatch live access
check admitted by a finite evaluation budget for `DeepSeek-V4.1-Flash`;
evaluator selection across separately configured provider profiles.

Dependencies: M004 (AI inner loop, scripted client, prompt inspection),
M005 (shared counting/contract conventions reused for bounds, not changed).
Question dispositions: Q-001 and Q-007 remain open for full
quality/performance/cost qualification; M019 supplies only the access-evidence
increment they require first.

Exclusions: eligibility/transformation semantics (M015–M017), chain
fallback/deadline behavior (M018), corpus runs and evaluation reports
(M020), API/UI integration, DF-004 route tables and multi-candidate serving
chains, quality/cost/latency qualification, production serving startup, and
any per-route provider assignment. No second adapter is required unless a
second provider's fixtures prove the base contract insufficient.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | Registry holds at least two non-secret profiles (including `DeepSeek-V4.1-Flash` with adapter ID, endpoint `https://api.deepseek.com`, model `deepseek-flash`, `CredentialRef` `deepseek`, effective settings, revisions, bounds and billing profile); duplicate candidate IDs and structurally invalid profiles are rejected; profiles are selectable per route without family-owned credentials | AI §5.1; roadmap M019 |
| AC-002 | Adapter conformance passes through real transport serialization against a fake `HttpMessageHandler`: request carries the profile's endpoint/model/auth, required settings (temperature `0`, no `top_p`, no tools, thinking disabled, bounded JSON output) and bounded input; responses map to the internal envelope; fixtures are sanitized (no key material) | AI §5.1–5.2; V-008 |
| AC-003 | Fault fixtures prove error/usage mapping, request cancellation, bounded response reads and exactly-one-dispatch counting; unsupported or silently ignored settings are rejected, not dropped | AI §5.1; verification §3.3; V-008 |
| AC-004 | Credential resolver maps `CredentialRef` to secret material only in the evaluation composition; the key appears in no config, profile, source, script, snapshot or report; diagnostics expose at most `credentialRef`/`credentialPresent`; offline tests read no credential variables and run with provider networking disabled | AI §5.2; AI §8; V-008 |
| AC-005 | Explicit live access check makes at most one low-output fixed-synthetic budget-admitted Chat Completions request through the profile's actual endpoint/model/auth/settings/parser and succeeds for `DeepSeek-V4.1-Flash`; missing/blank credential yields blocked/non-success with no scripted fallback; the report names the live call, reference and usage/exposure without secrets | AI §5.2; V-008 |
| AC-006 | A second profile referencing a different credential source is selectable by the evaluator and reaches its own access check without DF-004 serving rules and without assuming one credential per family | AI §5.1–5.2; D-19/DF-004; V-008 |

Failure/boundary cases: duplicate/invalid profiles rejected at load (AC-001);
unsupported settings rejected (AC-003); cancelled calls bounded with no retry
or hedging (AC-003); blank credential blocks live dispatch (AC-005);
breached bound or missing/inconsistent usage invalidates the profile for
further paid dispatch and retains the conservative reservation (AI §5.4,
carried as an admission invariant, not a new accounting implementation).

## Constraints and decisions

Nonfunctional constraints: finite per-stage `MaxOutputTokens`,
`MaxResponseBytes`, serialized-full-prompt input bound and context check are
required before live dispatch — no unlimited defaults (AI §5.4); one live
dispatch under an explicit finite budget; offline-first development with
provider networking disabled; no secret or production text in reports or
logs (AI §8; verification §7.2–7.3).

Clarification status: Q-001 (provider/quality/cost/cap arrangement) and
Q-007 (error classification, attempt/deadline policy, diagnostics) stay open;
M019 closes only the adapter/access/credential-reference portion. DF-004
stays deferred: no route keys, precedence, candidate/dispatch-cap redesign
and no change to the three-dispatch bound.

Human gates: owner credential already staged (see backlog §3); executor runs
the single budgeted live check; human reviews the sanitized report at
handoff. A missing credential blocks AC-005 only; AC-001–004 and AC-006's
offline selection still complete.
