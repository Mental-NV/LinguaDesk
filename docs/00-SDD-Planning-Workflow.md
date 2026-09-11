# LinguaDesk — SDD Planning Workflow

Document #0 is the authoritative process entry point. Read it first. Links are a routing map, **not an instruction to read every linked document**. Load only the stage procedure and sources needed for the selected scope.

## 1. Authority and ownership

#0 owns process, readiness and change control. #1 owns product outcomes, requirements, thresholds, proposal dispositions and release gates. #2–6 own shared design and verification within product constraints. #7–8 own delivery selection, plans and progress. #9 owns decision rationale; current design stays with its subject. #10 owns procedures for verified implementation.

Resolve conflicts at the owning source, then update affected dependents. A newer plan or implementation cannot silently override product intent. Preserve stable requirement, question, scenario, milestone and task IDs; never reuse retired IDs. Proposed, deferred, retired, ready, implemented and verified are distinct statuses. Existing approvals and delegated decisions remain valid; routine work does not require renewed permission.

## 2. Document map

| # | Owner / entry point | Supporting documents; open selectively |
| --- | --- | --- |
| 0 | This workflow | [Planning](00-workflow/planning.md), [execution](00-workflow/execution.md), [package template](00-workflow/package-template.md), [context tooling and research](../automation/context-guide.md) |
| 1 | [PRD](01-PRD.md) | [Deferred scope](product/deferred-scope.md), [open questions](product/open-questions.md); active FR/NFR/RG and P dispositions remain in the PRD |
| 2 | [UX](02-ux-specification.md) | [Processing and messages](ux/processing.md), [acceptance scenarios](ux/acceptance.md) |
| 3 | [Architecture](03-architecture.md) | Component, identity, data, accounting and engineering constraints; read selected sections |
| 4 | [LLM behavior](04-llm-specification.md) | [Dated provider research](research/deepseek-2026-09-08.md); verify before provider selection/paid use |
| 5 | [API design](05-api-design.md) | [Accounts](api/accounts.md), [operations/counting/accounting/recovery](api/operations.md), [generated OpenAPI](05-openapi.yaml); editable wire structure is C# DTOs/metadata |
| 6 | [Verification rules and check catalog](06-verification-plan.md) | [Backend/AI harness](verification/backend.md), [frontend/browser](verification/frontend.md), [LLM evaluation/performance](verification/llm-evaluation.md), [coverage](verification/coverage.md), [readiness](verification/readiness.md) |
| 7 | [Roadmap outcomes and dependencies](07-roadmap.md) | [Current delivery status and completed package links](delivery/current.md) |
| 8 | [Milestone package convention](00-workflow/package-template.md) | `docs/08-backlogs/<scope-id>/backlog.md`, `spec.md`, `plan.md`, `tasks.md`; all existing packages indexed in [current delivery](delivery/current.md) |
| 9 | [ADR index](09-architecture-decisions.md) | Individual `docs/decisions/ADR-NNN.md`; open only relevant decisions |
| 10 | [Operating guide](../README.md) | [Automation](../automation/README.md), executable `scripts/` commands |
| — | [Archive index](archive/README.md) | Provenance and superseded research; excluded from default planning/execution |

Supporting files retain their parent's authority and section numbers. Parent section headings route old references to the new owner. Use links with headings or stable IDs rather than line numbers. A reference is navigation, not a copy of a requirement.

## 3. Stage routing and completion

**Plan:** read [planning](00-workflow/planning.md), the selected roadmap row, current dependency status and that milestone's backlog. Discover relevant owner headings/IDs before reading full files. Produce one consistent package only for the next authorized increment.

**Execute:** read [execution](00-workflow/execution.md), then the validated selected package and its bounded source excerpts. Do not reread the whole PRD, roadmap, other packages, historical research or complete coverage matrix. Expand context when a missing requirement, changed input or cross-boundary conflict requires it.

**Close:** tasks own task completion and reproducible evidence; backlog owns item state; [current delivery](delivery/current.md) owns milestone status; [coverage](verification/coverage.md) owns product/scenario mapping. Update only affected rows. Stable design files change for changed contracts, not new test counts or chronological completion notes.

Ready for implementation means coherent spec/plan/tasks, satisfied dependencies, resolved blocking questions, reviewed source manifest, and verification coverage for every selected scenario. Done requires all selected acceptance and applicable checks to pass with revision, environment, command/procedure and evidence. Pending human/live evidence blocks its applicable gate. Milestone completion does not establish a product/release gate. No fixed duration limit applies.

For a milestone whose outcome is a user journey, **Done requires the named user to complete that journey in the published UI through the real application auth/API/persistence boundaries**, with controlled external adapters where live services are not part of the selected evidence. An API endpoint, standalone LLM runner, route shell, component fixture, or document review cannot by itself discharge a user-visible outcome. The roadmap must state the actor and observable UI result, and #6 must allocate both focused UI checks and at least one integrated browser path for the journey.

Every UI-facing milestone includes published end-to-end cases that use #6's dedicated verified synthetic account and assert the visible outcome in the browser. At least one case signs in through the real login form. Other cases may reuse a documented test-only authenticated browser state created through the real auth boundary; they may not disable authentication, forge a session, introduce a test login route or replace UI assertions with network assertions. The isolated test database is migrated and seeded before the host starts serving cases, and the production environment cannot invoke that seed path.

For scope/design changes, update the owner first, assess affected inputs, revise the selected plan/tasks, refresh its source lock after review and run affected checks. Unrelated changes do not invalidate every package. Completed packages are historical evidence: keep their scope/results, add a new stable milestone for follow-up work, and mark affected old conclusions superseded explicitly.
