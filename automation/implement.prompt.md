Implement milestone {{MILESTONE}} in the LinguaDesk repository.

Read the authoritative SDD documents beginning with `docs/00-SDD-Planning-Workflow.md`, the roadmap and backlog entry for {{MILESTONE}}, and the selected delivery package's `spec.md`, `plan.md`, and `tasks.md`. Compare their recorded source revisions with the current repository before making changes.

Execute the selected implementation plan completely:

- Work only on {{MILESTONE}} and its documented indispensable prerequisites. Do not start another milestone.
- Confirm the package is ready for implementation and stop if a blocking dependency, decision, credential, or human action is missing.
- Follow the authoritative specifications and ordered tasks. Fix inconsistencies at their owning source rather than coding around them.
- Implement the required behavior and tests completely. Milestones have no fixed duration limit; completion requires all acceptance criteria and applicable verification.
- Run every applicable check from the selected package and verification plan, including relevant regression, integration, smoke, or frontend checks; correct failures before finishing.
- Review the complete diff for scope, correctness, security, privacy, and accidental generated files.
- Update task completion, backlog state, roadmap evidence, affected specifications, and operating instructions so they describe verified behavior accurately.
- Do not claim a release gate, live-service result, or human verification without its required evidence.
- Do not create a Git commit; the automation script owns commits.

Finish only when {{MILESTONE}} is done under document #0, or clearly report the blocker without claiming completion. End your final response with exactly one of these raw status lines, without a bullet, Markdown fence, or backticks:

`MILESTONE_AUTOMATION_STATUS: COMPLETE` only when {{MILESTONE}} is done and all required checks pass.

`MILESTONE_AUTOMATION_STATUS: BLOCKED` when it is not complete. Never report `COMPLETE` while work, evidence, or a blocking issue remains.
