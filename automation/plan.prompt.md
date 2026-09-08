Prepare milestone {{MILESTONE}} for implementation in the LinguaDesk repository.

Read the authoritative SDD documents, beginning with `docs/00-SDD-Planning-Workflow.md`, and the roadmap entry for {{MILESTONE}} in `docs/07-roadmap.md`. Inspect the current repository and Git history before changing planning artifacts.

Complete the planning lifecycle required by the SDD workflow for {{MILESTONE}}:

- Work only on {{MILESTONE}} and its indispensable prerequisites.
- Confirm the milestone's dependencies are complete. Stop and report the blocker if they are not.
- Select and refine the milestone backlog item, create its delivery package, and produce or update `spec.md`, `plan.md`, and `tasks.md` as required by document #0.
- Resolve planning questions at the correct source of authority. Do not silently turn proposals or assumptions into accepted requirements.
- Record anticipated human steps, verification coverage, source revisions, risks, and remaining blockers.
- Review the resulting backlog, specification, plan, and tasks for consistency and readiness for implementation.
- Do not implement production code or tests during this planning run.
- Do not create a Git commit; the automation script owns commits.

Finish with a concise summary of changed planning artifacts and any blocker that prevents implementation. End your final response with exactly one of these raw status lines, without a bullet, Markdown fence, or backticks:

`MILESTONE_AUTOMATION_STATUS: READY` when the milestone is ready for implementation.

`MILESTONE_AUTOMATION_STATUS: BLOCKED` when it is not ready. Never report `READY` while a blocking issue remains.
