# LinguaDesk — SDD Planning Workflow

**Version:** 1.2 · **Updated:** September 8, 2026

**Role:** Document #0 — authoritative entry point and governance for specification-driven development (SDD).

## 1. Start here: authority and ownership

Read this document before creating or changing any LinguaDesk specification, backlog, implementation plan, or task list. It defines the rules under which the PRD and all other specifications are authored, reviewed, maintained, and used to guide implementation. It is the project's process constitution; it is not derived from the PRD.

Authority is assigned by subject:

- **Document #0 owns process:** document responsibilities, specification quality, sequencing, traceability, readiness, and change control. Amend this document first when changing those rules.
- **Document #1 owns product intent:** outcomes, scope, requirements, product constraints, and release acceptance. Every downstream specification must trace to it. A new or changed product commitment must be recorded there before dependent work adopts it.
- **Documents #2–6 own shared design and verification:** each defines its subject within the accepted product constraints. Document #9 records the rationale for consequential technical decisions; the current design remains in the owning specification.
- **Documents #7–8 own delivery:** milestones, backlog items, selected implementation scope, plans, and tasks. Scheduling or implementing an item does not authorize a product or shared-design change.
- **Document #10 owns operating instructions:** procedures must describe verified implementation behavior.

Resolve contradictions in the document that owns the decision, then update its dependents. A more detailed or newer document does not automatically override its source. Implementation is evidence of current behavior, not authority to silently rewrite requirements.

The current [PRD](01-PRD.md) is a product baseline with active MVP requirements, explicitly deferred/retired scope, unaccepted proposals and scoped open questions. Preserve those distinctions. Finished authoring does not mean every proposal is accepted. Reuse its stable `FR-*`, `NFR-*`, `P-*`, `Q-*`, and `RG-*` IDs; do not duplicate its requirement catalog or policy values here. Existing approvals and delegated decisions remain valid.

## 2. Document map

Document numbers identify responsibilities, not a mandatory waterfall sequence. Paths for documents that do not yet exist are prescribed destinations for future authoring. Document #8 is a repeatable artifact family, not one growing file.

| # | Document and canonical location | Responsibility and required content |
| --- | --- | --- |
| 0 | **SDD planning workflow** — `docs/00-SDD-Planning-Workflow.md` | Entry point, process constitution, document ownership, generation rules, delivery lifecycle, and quality gates. Governs all other documents. |
| 1 | **Product Requirements Document** — `docs/01-PRD.md` | Product outcomes, scope, stable requirements, acceptance criteria, proposals, deferred questions, and release gates. Own product changes and proposal dispositions, including Q-010. |
| 2 | **UX/UI specification** — `docs/02-ux-specification.md` | Layouts, wireframes, components, interaction states, messages, responsive behavior, browser coverage, and accessibility checks. Own Q-009; coordinate Q-002 with selected feature specifications and the workspace interactions in Q-004. |
| 3 | **Architecture and engineering principles** — `docs/03-architecture.md` | Technology choices, component boundaries, deployment, identity, data lifecycle, concurrency/accounting, configuration, secrets, observability, privacy, security, and cost enforcement. Own technical design for Q-001, Q-004, and Q-008; coordinate Q-003 and Q-006 with the API contract. |
| 4 | **LLM behavior and routing specification** — `docs/04-llm-specification.md` | Prompt versions, provider adapters/settings, configuration validation, routing, output checks, error classification, context bounds, and attempt budgets. Own Q-007 and provider/model eligibility evidence for Q-001 with architecture and evaluation. Reference product policies and thresholds. |
| 5 | **API contract** — `docs/05-openapi.yaml` | Operations, schemas, authentication, identifiers and lifecycle, errors, counting rules, usage fields, retry/cancellation semantics, and compatibility. Own Q-003 and Q-006 with architecture and selected feature design. Keep client-facing examples in OpenAPI descriptions. Use the generation and review rules below. |
| 6 | **Verification and LLM evaluation plan** — `docs/06-verification-plan.md` | Test strategy, requirement coverage, evaluation corpus/rubric, human review, workloads, and reproducible evidence. Own Q-005. Map requirements and release gates to checks and results, referencing product thresholds. |
| 7 | **Roadmap and milestone plan** — `docs/07-roadmap.md` | Stable milestone IDs, outcomes, dependency order, exit criteria, backlog links, blockers, and the current milestone summary. Detail near-term delivery; keep later milestones at outcome level. |
| 8 | **Milestone/feature backlogs and selected delivery packages** — `docs/08-backlogs/<scope-id>-<slug>.md`; `specs/<sequence>-<slug>/` | Backlogs define candidate outcomes and acceptance criteria. A separate delivery package specifies a selected subset and contains its implementation plan and tasks only when that subset is about to be built. See Section 4. |
| 9 | **Architecture decision records** — `docs/09-architecture-decisions.md` | Stable ADR IDs, context, alternatives, decision, consequences, status, and links. Preserve superseded decisions; maintain current design in its owning specification. |
| 10 | **README, operating guide, and portfolio walkthrough** — `README.md` | Working setup, configuration, deployment/rollback, diagnostics, recovery, and a guided demo. Link architecture, API, evaluation evidence, and actual limitations. Include procedures arising from Q-004/Q-008. |

**API contract authoring:** Document #5 remains the canonical, version-controlled `docs/05-openapi.yaml` artifact. Endpoint metadata, typed DTOs, and OpenAPI descriptions/transformers in C# generate it deterministically; do not hand-edit generated YAML. Review its diff as a design change against the selected specification before dependent clients rely on it. A selected package defines required API behavior before implementation; contract-only DTO/endpoint metadata scaffolding may be authored during planning to review the generated shape, without claiming that handlers are implemented. Generation does not let implementation silently redefine that behavior. CI regenerates the schema and client and fails on drift. This replaces duplicate manual schema maintenance, without changing document #5's ownership of the external contract or accepting P-006 compatibility guarantees.

Keep diagrams and decision/checklist sections with their subject. Executable schemas, fixtures, configuration, and generated evidence may live beside code and be linked from specifications. Create supporting planning files only when they have a distinct purpose; avoid duplicate contracts, requirement lists, and status reports.

## 3. Rules for authoring and generating specifications

Apply these rules to human-authored and generated artifacts alike:

1. **Establish context before drafting.** Read document #0, the relevant PRD requirements and decision statuses, and the existing owning specifications. For delivery work, also read the milestone, backlog, selected package, and relevant repository code. Name the inputs and their revision or commit so later changes can be assessed. Do not treat planned paths as existing documents or claim to have read missing inputs.
2. **Separate intent from implementation.** Product requirements, backlog items, and `spec.md` describe what users or API consumers need, why, and how success is observed. Shared design and `plan.md` describe how to achieve it. `tasks.md` describes executable work derived from that plan. Reference existing technical constraints without inventing new choices during behavioral specification.
3. **Use testable, bounded statements.** Give each item an outcome, explicit scope and exclusions, observable acceptance scenarios, and applicable failure/boundary cases. Cover relevant UX states, API behavior, data lifecycle, concurrency, accessibility, security/privacy, cost, and LLM uncertainty. Mark irrelevant concerns as not applicable with a reason. Reuse upstream thresholds; propose missing targets explicitly instead of making them binding by invention.
4. **Preserve decision status.** Distinguish accepted decisions, proposals, assumptions, and unresolved questions. Record each blocking question's owner, affected IDs, and the stage it blocks. A product-affecting assumption cannot become a requirement merely because a generator chose a default. Routine technical decisions within delegated scope may be made and recorded without renewed approval.
5. **Keep detail proportional to proximity and risk.** Specify enough to select work; design enough to implement the selected slice; create ordered tasks only immediately before implementation. Do not generate plans and task lists for the entire future roadmap.
6. **Keep one source for each fact.** Link canonical requirements, contracts, decisions, and evidence. Add local acceptance detail with an upstream reference rather than copying policies or creating competing definitions. Preserve stable IDs when moving or deferring work; never reuse retired IDs.
7. **Make quality and status explicit.** Markdown artifacts identify their ID/title, status, scope, source references, and last update. Record accepted decision provenance and unresolved issues. Use equivalent valid metadata/descriptions in OpenAPI. Draft, ready, implemented, and verified are distinct claims; attach evidence before claiming verification.
8. **Review generated output.** Remove template examples, resolve selected-scope placeholders, validate links and identifiers, and check consistency with document #0 and upstream constraints. Report missing inputs and blockers. Generation alone is neither acceptance nor proof that checks passed.

For clarification, ask at most 1–3 related questions at a time about unresolved choices that materially affect the current work. Present meaningful alternatives and a recommendation when helpful. Continue independent drafting where possible. Resolve product choices in the PRD, shared design choices in their owning document, and local clarification in the selected specification; link resolutions back to the original question.

## 4. Document #8: backlogs and delivery packages

### Recommended structure

Use one backlog file per milestone by default. A substantial feature spanning milestones may instead own a feature backlog, linked from the relevant milestones. Each item has exactly one owning backlog; milestone assignments reference it without copying it. Document #7 is the index and aggregate progress summary, so no separate backlog index is required.

```text
docs/
  07-roadmap.md
  08-backlogs/
    M001-<milestone-slug>.md
    M002-<milestone-slug>.md
    F001-<feature-slug>.md          # only when a feature needs its own backlog
specs/
  001-<selected-slice-slug>/
    spec.md                       # selected behavior and acceptance scenarios
    plan.md                       # created when implementation is imminent
    tasks.md                      # derived from the current plan
    research.md                   # optional: evidence for unresolved technical choices
    data-model.md                 # optional: detailed model for this slice
    quickstart.md                 # optional: reproducible validation/demo procedure
    checklists/                   # optional: requirements/design review records
  002-<selected-slice-slug>/
    spec.md
```

These are naming patterns, not committed milestones or features. Allocate stable scope IDs (`M001`, `F001`), backlog item IDs (`BI-001`), and globally unique package sequences (`001-...`). Local scenario and task IDs are qualified by package path when referenced elsewhere. Moving a backlog item to a later milestone does not change its ID.

The split between backlog selection and delivery packages is a LinguaDesk convention. The package names and separation of specification, planning, and tasks follow [GitHub Spec Kit's artifact approach](https://github.com/github/spec-kit/tree/main/templates). Shared API definitions remain canonical in document #5; any package-level contract material links to it rather than defining a second API.

### Backlog file content

Each backlog contains:

- **Scope and purpose:** scope ID, intended outcome, milestone/feature references, boundaries, dependencies, and relevant exit criteria from document #7.
- **Item table:** `Item ID | Outcome/title | Priority | Target milestone | Dependencies/blockers | State | Delivery package`. Use `candidate → ready for selection → selected → in progress → done`; allow `deferred` or `dropped` with a reason. Record blockers separately from state.
- **One detail section per item:** user/API value or necessary technical enablement; upstream requirement/decision IDs; in-scope and out-of-scope behavior; observable acceptance scenarios; relevant edge cases and nonfunctional checks; dependencies; assumptions and open questions.

Backlog items are testable increments of value or explicitly justified prerequisites, not a list of files to edit. They do not contain an implementation approach or coding task breakdown. Future items can remain coarse candidates. Refine only those approaching selection, favoring small slices that can be demonstrated and verified after their explicit prerequisites are satisfied. This uses the emphasis on prioritized, independently testable stories and observable scenarios in [Spec Kit's specification template](https://github.com/github/spec-kit/blob/main/templates/spec-template.md).

### Selecting work and creating a package

Select one item or a cohesive subset from the backlog when there is intent to implement it next. Confirm dependencies are satisfied or explicitly included. Record the selected item IDs, their source revision, and links in a new `specs/<sequence>-<slug>/spec.md`; link the package from each selected item. A package may deliver an entire small milestone or one feature slice. Avoid combining unrelated items simply because they share a milestone.

`spec.md` contains the selected scope, prioritized stories, acceptance scenario IDs, exclusions, requirement references, dependencies, and clarification decisions. It refines backlog outcomes into the behavioral contract for this increment. Once selected, detailed scenarios live here; the backlog links to them and retains its selection summary. Update the backlog summary if the intended outcome changes. Unselected items stay in the backlog and receive no speculative implementation plan.

After specification readiness, create `plan.md` using the selected spec, current shared designs, and repository reality. Include:

- Selected item/scenario references and source revisions; a check against document #0 and applicable architecture principles.
- Technical context, affected components/files, chosen approach, integrations, and relevant data/API changes.
- Resolved technical unknowns and supporting evidence; consequential decisions linked to document #9.
- Verification approach and mapping to document #6; migration, rollout/rollback, observability, and operational impacts where applicable.
- Dependencies, risks, and any remaining blockers; final consistency check after design.

Create `tasks.md` only after the plan is coherent. Give each task an ID, selected item/scenario or enabling-dependency reference, concrete action and file/component target, dependency order, and completion check. Group necessary setup and prerequisites first, then work and verification by story. Mark parallelizable work only when dependencies and file ownership permit it. These conventions adapt [Spec Kit's task template](https://github.com/github/spec-kit/blob/main/templates/tasks-template.md); LinguaDesk's applicable verification requirements come from document #6 and the selected spec.

A backlog item should belong to only one active delivery package. If only part of it is selected, split it into independently acceptable items first, preserving links to the original. Later increments get new packages; do not overwrite completed plans or silently add scope to completed task lists. Keep historical evidence and reassess affected artifacts when requirements change.

## 5. Planning and implementation sequence

The lifecycle adapts [GitHub Spec Kit's workflow](https://github.github.com/spec-kit/quickstart.html): constitution → specify → clarify → plan → checklist → tasks → analyze → implement → verify completeness. The milestone/backlog layer selects the input to that cycle. Command names below describe corresponding stages; they do not imply Spec Kit is installed.

| Stage | LinguaDesk action | Result / gate |
| --- | --- | --- |
| Establish governance and product intent | Read document #0; create or refine the PRD under its rules. Resolve product proposals needed for near-term work. | Accepted intent for the next scope; future questions remain explicit. |
| Outline shared design and delivery | Develop documents #2–6 to the depth needed for the next outcome; outline milestones in #7 and candidate backlogs in #8. Iterate between behavior, dependencies, and feasibility. | Coherent boundaries and prioritized candidates; no need to finish every future design. |
| Specify and clarify | Select backlog items and create/refine `spec.md` (`specify`, `clarify`). Resolve selected-scope behavior before technical planning. | Ready for planning. |
| Plan and review | Create `plan.md` (`plan`), resolve technical unknowns, and refine affected shared specifications. Review requirements/design quality (`checklist`) and record decisions. | Feasible, consistent design with appropriate verification. |
| Derive tasks and analyze | Generate `tasks.md` (`tasks`); check spec, plan, tasks, shared contracts, and governing rules for conflicts, missing coverage, and unsupported work (`analyze`). Fix issues at their source. | Ready for implementation. |
| Implement and verify | Execute selected tasks in dependency order (`implement`); run applicable checks, record results, and compare behavior against the whole selected specification. Use a completeness review, or `converge` if available, to find remaining work. | Done only after evidence supports acceptance and documents match behavior. |
| Close and repeat | Update backlog item states, milestone summary, coverage/evidence, and operating instructions. Select the next increment using what was learned. | Traceable increment; release remains a separate decision. |

Shared design may start before selection to reveal dependencies and evolve during selected-slice planning. Plan required access, accounting, privacy, and cost prerequisites before user-facing LLM work that depends on them. Verify provider eligibility before a plan relies on a serving arrangement. Preserve the PRD's proposal status when determining which safeguards are binding.

If Spec Kit tooling is adopted, ensure its constitution and templates load these rules. Keep any required `.specify/memory/constitution.md` as an explicitly derived adapter identifying this document and linking applicable engineering principles in #3. Synchronize it when its sources change; do not maintain a competing process constitution. Check the installed tooling's conventions before configuring paths or invoking commands.

## 6. Readiness, completion, and traceability

| Gate | Required evidence or condition |
| --- | --- |
| **Ready for selection** | Backlog item has bounded value, upstream references, observable acceptance, priority, and explicit dependencies/questions. No unresolved product choice prevents deciding its scope. Technical unknowns may remain for planning. |
| **Ready for planning** | Selected IDs and exclusions are recorded in `spec.md`; stories/scenarios are testable; blocking product/behavior questions are resolved; assumptions and applicable nonfunctional constraints are explicit. No coding task breakdown is required yet. |
| **Ready for implementation** | Current spec, shared UX/API/design, plan, and ordered tasks agree. Blocking technical questions and required prerequisites are resolved. Every selected scenario has a verification method and task coverage; tasks have a scope or prerequisite justification. Requirements/design review and consistency analysis have no unresolved blocking findings. |
| **Done** | Selected acceptance and applicable regression/contract/evaluation checks pass; reproducible evidence identifies the code revision and relevant environment/configuration. Tasks and backlog states are accurate, and affected specs and operating instructions match the implementation. Explain any non-applicable checks. |
| **Milestone / release complete** | Milestone exit criteria or PRD release gates, respectively, have their own evidence. Completed items alone do not establish product-wide quality, performance, or launch readiness. Resolve questions due at that gate. |

Requirements-quality checklists assess specification clarity and coverage; checked boxes are not runtime test results. Record review outcomes separately from implementation evidence. Existing approvals and delegated decisions stand; these gates do not add repeated permission requests for routine work.

Maintain one product coverage table in document #6:

**PRD requirement / release-gate ID → shared design or API reference → backlog item / package scenario → verification check → evidence and status**.

Include uncovered **active** requirements as pending rather than omitting them. Retain deferred/retired IDs with that status and their owning PRD reference; do not count them as active gaps or passing checks. In each package, link scenarios to tasks and evidence without recreating the whole product matrix. Backlog files own item state, `tasks.md` owns task completion, and document #7 owns only the milestone summary and links. Mark a parent item done only when all of its acceptance criteria are verified.

## 7. Changing specifications and resuming work

**Deferring product scope:** Use an explicit PRD status/register, not a lower priority that could still be read as an MVP obligation. The PRD owns what is deferred, retired, or retained; affected shared specifications show the same dispositions. A deferred capability has later-phase intent but no scheduled milestone until selected. Do not create full future implementation specs, dormant endpoints/flags, or placeholder tests just to retain intent. Keep the old design in Git as historical evidence and preserve stable IDs. When the capability is selected, refine it against the current baseline and create its normal #8 backlog/package. Retired behavior is not reactivated implicitly with adjacent deferred features.


Before resuming a package, compare its recorded input revisions with the current PRD, shared designs, and implementation. If relevant inputs changed, reassess readiness and update the plan/tasks before continuing affected work. Unrelated changes do not require regenerating every artifact.

For a scope, behavior, or design change:

1. Identify affected requirement, question, backlog, scenario, contract, and decision IDs.
2. Record the decision in its owning document, preserving proposal/acceptance status and rationale. Change document #0 first for governance changes; the PRD first for product changes; the owning design and ADR for technical changes.
3. Update dependent specifications, selected package artifacts, and verification coverage in the same change where practical. Otherwise record explicit follow-up links and block dependent implementation until consistent.
4. Re-run the affected quality gates and checks. Preserve completed task/evidence history, marking superseded conclusions; never reuse an earlier passing result as proof of changed behavior.

When generating the next document, deliver the requested artifact with its source references, resolved decisions, remaining blockers, and the next applicable gate. Generate only the documents needed for the authorized scope, under this workflow.
