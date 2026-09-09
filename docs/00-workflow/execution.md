# Executing a milestone

Apply [#0](../00-SDD-Planning-Workflow.md). The selected spec/plan and their source manifest define the normal reading set; the numbered documentation map is not a bulk-read checklist.

## Start or resume

Run `python3 automation/context.py check <scope-id>`, then `python3 automation/context.py packet <scope-id>`. The packet puts stable governance first, selected canonical excerpts next, the spec/plan after them, and mutable backlog/tasks last. It identifies on-demand sources without loading their content. Inspect only relevant code, generated contracts and command implementations as the tasks require.

The check reads/hashes sources locally without sending their full text into model context. It verifies selected source text and spec/plan freshness, not code correctness or semantic completeness. Inspect Git status/diff and current dependency state too. A changed source or plan requires review of the affected section/diff and plan/verification impact before locking again. Do not blindly re-lock to make a failure disappear. Added/unlisted requirements, missing references and conflicts require expanding the manifest at the owner; no small-context rule permits guessing or ignoring a contract. A missing legacy manifest requires planning before implementation resumes.

Completed packages predating this convention retain historical inputs/evidence. Their relocation does not recertify readiness. Do not generate replacement locks or rewrite their acceptance merely to satisfy the new workflow.

## Execute and verify

Follow task dependencies and the selected AC → task → check mapping. Read referenced material once, at the smallest useful section. Reuse already loaded content within a turn; after compaction/resume, load the stable packet and current task pointer rather than prior conversation logs. Refresh only changed sources or tasks during a continuous run.

Fix inconsistencies at their owning specification, then adjust dependent plans/tasks. Run every applicable package check, including regressions and generated-contract review; use the lowest layer that proves each assertion. A runner's fixed regression suite is an additional gate, not a replacement for selected checks. Preserve failures and limitations honestly; fixture success cannot discharge required live/manual evidence.

Confirm blocking dependencies, decisions, credentials and human actions before dependent work. Use existing authorization. Continue independent authorized tasks when possible; if no progress remains, record the blocker. No fixed duration limit applies. Do not begin another milestone.

## Closeout and handoff

Review the complete diff for scope, correctness, privacy/security and accidental artifacts. Record actual commands/procedures, revision/configuration, UTC time, environment and evidence in tasks; mark each selected AC passed only with its evidence. Update backlog item state, current delivery status and affected #6 coverage rows. Keep source requirements, design and operating instructions accurate; do not append duplicate milestone narratives to each shared document.

Long logs belong in sanitized artifacts. The tasks resume note identifies the next task, last applicable check, pending blockers and changed files; it is not a running transcript. Update spec/plan only for changed behavior/design, not checkbox progress. Human/live evidence still pending at a required milestone gate prevents COMPLETE. Release remains a separate gate and decision.
