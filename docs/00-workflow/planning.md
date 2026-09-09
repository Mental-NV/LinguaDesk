# Planning a milestone

This is the planning procedure under [#0](../00-SDD-Planning-Workflow.md). It is not part of the default execution reading set.

## Select and gather bounded context

1. Inspect Git status and current code. Read the chosen row in [#7](../07-roadmap.md), [current delivery](../delivery/current.md), and its `docs/08-backlogs/<scope-id>/backlog.md` if present. Confirm named/transitive dependencies from evidence and current code; do not read every predecessor's plan.
2. Read relevant PRD requirement rows, proposal dispositions and scope exclusions. Check applicable Q-IDs in [open questions](../product/open-questions.md). Never promote a proposal or revive deferred behavior through planning.
3. Use `python3 automation/context.py outline <path>` to discover owner headings, then `read <path> --heading '<exact heading>'` or `--ids FR-001 FR-002` to read only relevant sections/rows. An ID row may depend on a parent rule or shared definition; include those explicitly. Read applicable architecture, UX/API/AI contracts, V-groups and harness rules. Include cross-cutting identity, data lifecycle, privacy, accounting/cost, error/recovery and nonfunctional constraints where relevant. Record why omitted domains are irrelevant.
4. Inspect actual files and commands implementing these boundaries. Git history is for a specific provenance/dependency question, not a default full-history read. Resolve technical questions within delegated authority, at their design owner. Record material rationale in a relevant ADR.

At the beginning, inspect available access/tooling and batch indispensable human inputs/actions. Record owner, timing and blocked gate, or “None required.” Existing authorization and routine setup suffice. Prepare nonblocking human verification for the end with concrete procedures; required live/manual evidence cannot be replaced by fakes. If an unexpected dependency appears, continue independent work and report the blocker without claiming its gate passed.

## Author the four-artifact package

Use [the package template](package-template.md); all files live under the stable scope ID. IDs express identity, not order. One item has one owning backlog and one active delivery package. Split independently acceptable increments into new stable milestones before selecting them; do not overwrite completed packages or add unrelated scope to their task lists. Feature IDs such as F001 are allowed only when a feature truly needs an owning backlog; use no slugs or separate sequence namespace.

- `backlog.md`: item ID, outcome/value or enabling justification, priority, dependencies, state, brief acceptance summary and links. State is candidate → ready for selection → selected → in progress → done; deferred/dropped requires a reason. Once selected, detailed scenarios live only in spec.
- `spec.md`: selected item/requirement IDs, exclusions, observable AC IDs, failure/boundary cases, applicable nonfunctional constraints and clarification dispositions. Describe intended behavior; avoid duplicating the complete upstream catalog or generated wire schema.
- `plan.md`: component/file changes, ordering, contract/data design, migration and rollout/rollback if relevant, configuration/access, risks, human steps, and a small execution brief. Map every AC to a check/task and a verification command or concrete manual procedure. Reuse owner requirements through selected excerpts; locally restate only essential invariants needed to make a step unambiguous.
- `tasks.md`: task ID, selected AC or indispensable prerequisite, concrete action/target, dependencies and completion check. Keep checkboxes, blockers, current resume pointer and concise evidence here. Link longer sanitized reports rather than pasting logs. No second full copy of the plan or spec.

For API slices, specify selected operations, inputs/outputs and acceptance before handlers. Early in implementation add actual C# DTOs/metadata, generate/review OpenAPI, then generate client types before adoption and implement handlers against that reviewed shape. Never handwrite provisional YAML, maintain a second complete Markdown schema or ship incomplete contract scaffolding as working APIs. Generation is not behavioral verification; check schema/client drift and runtime semantics separately.

## Review and lock execution inputs

Create `context.json` using the documented schema in [the context guide](../../automation/context-guide.md). It identifies source paths, exact headings or stable ID rows, reasons and whether each excerpt is included at execution or available on demand. Include every source relied on by spec/plan, including dependency evidence, applicable question dispositions and generated/code contracts when essential. Avoid whole-file sources when a section/row suffices.

Check requirements clarity, scope, dependencies, human gates, shared-contract consistency and AC → task → verification coverage. Do not label checks passed merely because documents were generated. Remove examples/placeholders and unresolved blocking assumptions. Include a coverage matrix only for selected scenarios; product-wide coverage stays in #6.

After reviewing the exact current inputs, run `python3 automation/context.py lock <scope-id>` and `check <scope-id>`. Locking records a baseline commit, source-selection hashes and spec/plan hashes; it **does not prove semantic completeness or approve a plan**. Review the manifest for missing sources. Changing an unrelated heading in a source file need not invalidate a selected section. A new requirement outside the manifest still requires human/agent impact analysis; hashes cannot discover omissions.

Report READY only after this review and the readiness gate pass. Planning creates no production code/tests. Do not pre-plan the entire roadmap. Optional research/data-model/quickstart files need a distinct current purpose and references from the plan; never force execution to read them merely because they exist.
