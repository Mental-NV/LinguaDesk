# M045 — Implementation plan

Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json). Mutable progress and evidence: [tasks](tasks.md).

## Execution brief

Implement the whole D-23 user journey, retaining the existing full-text scheduler/auth/ledger and adding explicit alternatives as a separate operation within the Rewriting family. Keep shell preferences separate from effective input revisions. Store submitted-source snapshots, result revisions, sentence spans, selected options and comparison only in memory; never infer a language by inspecting generated text. Use ordinary textareas for source/manual result editing and a selectable presentation for Write assistance.

Work is ordered so generated contracts and integrity tests precede client consumption. M045 is broader than a CSS change because a user must obtain real alternatives through the published application. Tasks below are internal steps of one selected outcome; none alone establishes completion. Planning does not execute the implementation or authorize paid calls/deployment.

## Changes and order

### T001 — Review fixtures and executable boundaries

Before provider-serving integration, implement shared synthetic fixtures for the owner-defined scalar spans, sentence boundaries, Unicode/CRLF/duplicate-sentence cases and context/output limits. Freeze the separate development and qualification corpus manifests specified by #6, with review provenance; qualification execution remains outside M045. Add `docs/08-backlogs/M045/evidence-design.md` during implementation only if needed to link fixture hashes, review roles and exact gate commands. Do not substitute the model's own output for reference meaning assertions. Record live prerequisite status without reading/logging secrets. Open the locked selected owner excerpts, inspect the current diff and refresh the lock only after impact review when implementation changes an included code/generated source.

### T002 — Actual typed contracts, capability discovery and generation

In `backend/src/LinguaDesk.Api/Features/Operations/`, add the alternatives request/success metadata and a separate endpoint at the owner-selected route; reuse auth/antiforgery/problem/status infrastructure. Extend capability metadata under `Features/Capabilities/` with supported alternative kind, exact input bounds, segmentation revision, two-option count and 15-second deadline. Add the validated resolved-source-language value to full-text success responses and carry it through both coordinators, Smoke providers and generated clients; this is necessary for current detection labels, automatic-source swap and Write language association.

Keep existing `/api/operations` full-text payloads and nine mode IDs unchanged. Extend operation/status metadata with the new kind; existing rows default to fullText. Add contracts to real C# source, run `bash scripts/contract.sh generate`, review operations/defaults/error/association/nullable semantics, and then generate/adopt TypeScript. Do not hand-edit `docs/05-openapi.yaml`, create a contract-only host or publish scaffold-only handlers. Implement full behavior before treating the endpoint as usable. Contract review is recorded before T007 client adoption.

### T003 — Selected-sentence validation, admission and migration

Add a small pure `SentenceBoundaryPolicy` and alternatives validation policy under `LinguaDesk.Core`, mirrored by a frontend helper through shared fixtures. Validate exact result context, scalar offsets, supported language/mode and the selected complete sentence; derive selected text/count server-side. Add an operation-kind discriminator to `OperationSubmission`/EF mapping/migration with existing rows backfilled as fullText. Keep the account + operation ID uniqueness across all kinds and the two routing families.

Extend `OperationAdmissionService`, `OperationFingerprint`, settlement/status/recovery mapping and tests. Full-text fingerprint serialization stays byte-for-byte unchanged for the 24-hour recovery window. Alternatives use a separately versioned, length-delimited canonical encoding of kind, context/span, association and effective settings; reject ambiguity including embedded NUL content. Reserve selected-sentence count and the alternatives deadline using validated server values, never a caller-supplied count. Preserve success-only settlement, same-ID conflict, rollover and metadata-only replay. No sentence/context/option strings enter EF columns, logs, exceptions or fingerprints stored without a server key.

### T004 — Alternatives prompt, parser and serving path

Under `LinguaDesk.Infrastructure.Ai`, add `AlternativesPrompt`, embedded versioned prompt, strict envelope parser, validated input/outcome and `AlternativesPipeline`; extend chain traversal for the explicit operation kind while retaining Rewriting candidate order. Reuse the narrow `IChatClient` adapter boundary. Validate both options, complete-response byte limits and finish reason before success. No streaming, tool invocation, repair transcript or post-hoc model diff.

Use the selected finite operation-specific bounds without modifying existing full-text profile settings. Extend `ContextBounds` application, `AttemptCostProfile`, `ServingOperationBudget` and the serving/Smoke provider registrations so every eligibility/generation/fallback call is admitted conservatively. Existing provider/credential and $0.05 per-operation policy remain; missing bounds/qualification never become unlimited defaults or a fake success. Respect the 15-second operation deadline, three-dispatch maximum and typed refusal/failure categories. Add `AlternativesOperationCoordinator` and tests for original-response delivery, selected-sentence settlement, lost response, cost denial and stale completion. Finish the endpoint behavior from T002, including independent cookie/bearer cases.

### T005 — Shell, panels and toolbar

Refactor `frontend/src/shell/App.tsx`, `WorkspaceView.tsx`, `NavIcons.tsx` and `shell.css` into shared navigation, language bar, editor card and editing-tools components as useful. Keep account-form layout unchanged. Authenticated account menu calls the existing sign-out path; obtain only actual available account data and use a generic Account label if no display name exists.

Implement UX §4 exact widths/tokens, all four desktop panel combinations, compact overlay navigation, compact/mobile tools in flow, mobile header disclosures, focus return and hidden-control tab-order removal. Nav expansion is shared tab state; toolbar expansion is per page. Both collapse by default and clear on teardown. Add accessible grouped Styles/Tones listbox and two switch rows, menu viewport positioning/scroll and selected check. Disabled Translator tools never reach request payloads. Write switch/dropdown states resolve to one API mode; equivalent correctionOnly states do not advance input revision. Visible labels become Translator/Write without renaming routes or API family values.

### T006 — Snapshot/state model and copy/swap

Extend `workspaceModel.ts`, `workspaceStores.ts`, `workspaces.tsx`, feature models and API mappings with resolved language tied to the matching input revision, submitted-source snapshot, displayed-result version, operation kind and presentation preferences. Add one atomic swap/reuse reducer action; a sequence of independent source/target actions must not transiently dispatch invalid work. Compare actual new values and schedule only one eligible changed revision.

Use the current-result predicate plus unsettled/unknown-outcome guard for swap/reuse. Preserve Clear-source behavior and result copies during errors/offline states. Implement clipboard buttons with exact current plain-text input and success/failure messages after the actual API outcome. Copy, menus, disclosures and Show changes do not cancel/restart an existing processing timer. Expand the single teardown path in `WorkspaceReset.tsx`/`workspaces.tsx` to clear all snapshots/caches/preferences before callbacks/rendering.

### T007 — Write presentation, native editing and local comparison

Add focused helpers/components under `frontend/src/rewrite/` for sentence spans, bounded comparison, display/edit modes and alternative rows. Build local comparison from the submitted source snapshot and current result, rendering text nodes rather than HTML interpolation. Use grapheme-safe spans and the owner's bounded comparison fallback; deletion annotations are labeled and excluded from copied/edited result text. Handle paragraph/newline preservation and whole-sentence deletion. Edit result enters a native textarea; Done editing retains the current value. Manual edit/compositionstart invalidates all assistance and pending associations, even when undo later reproduces the original string. Entering/leaving edit mode alone preserves metadata; while edit mode is active keep the caret surface stable.

### T008 — Explicit alternatives and competing work

Add `frontend/src/api/alternatives.ts` from generated types and extend `workspaceStores.ts` with an explicit alternatives operation lane sharing the page's existing unsettled guard, usage channel and status recovery. Source/effective-mode changes may queue only the latest full rewrite; alternatives never autoqueue/regenerate. Map the 15-second deadline and original identity into Check status. Keep text application and ordered accounting separate.

Capture generation, result/context revision, selected scalar span, mode/language and association at dispatch. Two option rows appear below the main result only for a still-current selection. Reuse a valid set without a request; controlled replacement increments local result version, retains the selected set under its new association and clears other context-dependent sets. Manual edits/source changes/full results clear all assistance. A late valid set for a different selected sentence may be cached but cannot reopen it. Drag selection is distinguished from sentence activation; keyboard activation works without intercepting native editor Enter. Cover over-bound expanded results with a truthful alternatives-unavailable message and retained edit/copy.

### T009 — Integrated interaction, visual and privacy evidence

Extend `frontend/tests/unit/` reducer/paused-workspace/lifecycle coverage and existing published cases in `frontend/tests/e2e/`; add focused alternatives and panel cases, not a second harness. Update `workspace-fixture.ts` and deterministic Smoke providers to supply known text/options through the real API. Preserve the DB-seeded verified account and at least one visible sign-in path. Assert displayed output/focus/clipboard plus real usage/status for paid actions; never intercept application `/api` calls.

Replace affected D-21 snapshots with the exact 12-baseline D-23 set from verification §4.4; keep the unaffected registration capture unless evidence justifies a change. Inspect every intended change and record rationale/hashes in `visual-review.md`. Capture with pinned fonts/Chromium/scale and deterministic content, then rerun with baseline updates off. Include 320/768/1024 px and zoom/text spacing, both expanded desktop panels, account/menu focus, annotation overflow, native selection/copy, composition guards, offline/error/unknown-result states and real teardown. No claim of physical-device, native IME or full WCAG verification.

### T010 — Required bounded live development

Extend the existing independent evaluator and `scripts/ai.sh` with `evaluate-alternatives` offline/live modes, using the same production prompt/parser/chain and explicit profile/budget flags. Default checks remain offline. Run the 36-case versioned development batch only after the prerequisite record below is satisfied; include every mode/language and Chinese-script/fidelity strata. Confirm access with the existing one-dispatch command when no current compatible access evidence exists. Record candidate/prompt/checker/context revision, exact costs/dispatch limits, case outcomes, both-option review and limitations. A failed or incomplete batch is not a pass; keep the failed run and resolve findings within newly recorded remaining authorization. Do not silently run the full qualification corpus, spend against invalid credentials or loosen output validation to make a batch green.

### T011 — Final regressions, migration/rollback and closeout

Update the independent API consumer sample and README with the actual working alternatives and capability/status contract, preserving old request examples. Verify migration from a copy of the prior schema with unexpired operations and real restart/recovery, plus fresh initialization/model-drift checks. Execute the standard milestone gate and AC-013 evidence. Review generated schema, full diff, privacy sentinels and visual changes. Record actual commands/revisions/results in tasks, mark BI-001 Done only when all ACs pass, and update delivery/coverage with narrower evidence claims; no production/release pass is inferred.

## Verification map

Existing commands below are executable today; tests for the new feature and the explicitly marked new evaluator command are implementation deliverables. A command name alone is not acceptance. Reports use `artifacts/test-results/` and sanitized `artifacts/m045/`; durable review summaries live beside tasks.

| AC / prerequisite | Task | Check / exact command or executable procedure | Evidence target |
| --- | --- | --- | --- |
| Source selection/readiness | T001 | `python3 automation/context.py check M045`; `python3 automation/context.py audit` | context.json; tasks planning record |
| AC-001/002/003 | T005/T009 | `bash scripts/frontend.sh check`; `bash scripts/frontend.sh smoke`: semantic panels/listbox/account/defaults and exact geometry | frontend-unit.xml; frontend-e2e.xml |
| AC-004/005 | T002/T006/T007/T009 | Same frontend commands; resolved-language HTTP tests via `bash scripts/backend.sh check`; actual clipboard and native edit browser assertions | test reports; visual-review.md |
| AC-006/010 | T006/T008/T009 | Frontend check fake timers/deferred outcomes plus published alternatives/cached-selection/no-dispatch and lifecycle cases | unit and E2E reports |
| AC-007/008/012 | T002/T003/T004/T011 | `bash scripts/backend.sh check`; `bash scripts/backend.sh smoke`; `bash scripts/contract.sh generate` then `bash scripts/contract.sh check`; real SQLite concurrent/status/migration cases and consumer sample execution | backend-api.trx; schema review; migration/consumer evidence |
| AC-009 | T001/T004 | `bash scripts/ai.sh check`; `bash scripts/ai.sh probe`; `bash scripts/ai.sh conformance`; backend check serving/ceiling/deadline fixtures | backend-ai.trx; offline/transport reports |
| AC-011 | T009 | `bash scripts/frontend.sh smoke`; intentional capture via `LINGUADESK_UPDATE_VISUALS=1 bash scripts/frontend.sh smoke` (supported by frontend/playwright.config.ts); inspect 12 images then normal smoke | 12 baseline files; visual-review.md; E2E report |
| AC-013 | T001/T010 | Planned `bash scripts/ai.sh evaluate-alternatives --live --profile "$M045_PROFILE" --max-dispatches "$M045_MAX_DISPATCHES" --max-spend-usd "$M045_MAX_SPEND_USD"`; values must come from the recorded authorization, not defaults | development report/review and prerequisite record |
| All ACs / regression gate | T011 | `bash scripts/verify-milestone.sh M045`; separate AC-013 report review; `git diff --check` | tasks completion record and coverage update |

The capture opt-in is verified in `frontend/playwright.config.ts`. Never treat updating a PNG or a manifest as design/semantic review.

## Configuration, migration and rollback

Reuse two provider families, existing account auth, daily allowance and configured monthly cost ledger. The only new durable datum is operation kind; sentence context/association/choice text stays transient (association is bound into the keyed fingerprint). Preserve existing full-text fingerprints and operation deadlines when backfilling. No auto-migration at application startup. The selected additive EF migration is applied through the existing storage workflow in isolated verification and later authorized rollout.

Rollout is not authorized here. For a future rollout: apply the reviewed additive migration, deploy matching server/SPA/generated types together, and verify health/readiness. Rollback must quiesce alternatives admissions and settle/fence all in-flight work first. Do not downgrade/remove kind metadata while unexpired alternative identities require correct replay/accounting; retain a compatible recovery reader or wait for the 24-hour identity window after terminal settlement. Restore only a consistent database/application backup under the operational runbook, never delete ledger entries to make an old binary start. Migration/rollback risks are verified in isolated storage before closeout.

## Context boundaries, prerequisites and risks

- Planning/local verification needs no external account, mailbox or paid model. Observed local toolchain/dependencies are recorded in spec. Existing completed packages establish foundations only; current code was inspected independently.
- Live AC-013 is required by verification §5.0 because prompts/parser behavior changes. Before T010 the owner/authorized runner records allowed profile/model/credential reference, environment, total dispatch and spend caps (including access/review calls), concurrent-serving/shared-ledger or quiescent scope, and remaining capacity. No reusable M045 run authorization was found; the prior M043 run was dated and bounded. This gate may be blocked while local tasks proceed. Do not label M045 Done or globally ready for every check until the prerequisite is met. No permission request or live run is part of this planning task.
- Full qualification corpus execution, p95 workloads, new tariff verification for serving, production cap decisions, deployment, operational retention and unrelated proposals stay with G1–G4/other packages. M045 prepares the alternatives corpus design and production-compatible path; fixtures and its shallow live batch cannot satisfy release qualification.
- Primary risks: treating alternatives as full-text charging; corrupting old fingerprints/status during migration; trusting stale detected language; duplicate dispatch from toggle/listbox/swap; UTF-16 versus scalar offsets; selection versus drag; editable text mixed with annotations; assistance resurrected by undo/late callbacks; panel geometry and focus on responsive reflow. Each is allocated to an AC above.
- Provider credentials/prices are never copied into documents, prompts or artifacts. Existing profiles are implementation inputs, not current public verification. Revalidate provider capability/tariff/qualification before paid serving; that external check is unnecessary to author this plan.
- Omitted domains: auth algorithms/email delivery, advanced routing, document services, saved history and deployment administration have no selected changes. Read their on-demand owners only if implementation reveals a concrete dependency. No proactive sub-agent work is requested by this plan.
