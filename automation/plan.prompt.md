Plan one LinguaDesk milestone under the authoritative SDD workflow.

Read `docs/00-SDD-Planning-Workflow.md` and `docs/00-workflow/planning.md`. Follow their bounded reading procedure: inspect the selected roadmap row, current dependencies, relevant code and only applicable source sections/IDs. The document map is not a bulk-read checklist. Historical/completed packages are on-demand dependency evidence.

Create or refine `docs/08-backlogs/{{MILESTONE}}/backlog.md`, `spec.md`, `plan.md`, `tasks.md` and `context.json`. Review selected acceptance, source completeness, dependencies, human gates and verification mapping. Lock and check the context manifest only after review. Work only on the selected milestone and indispensable prerequisites; no production code/tests or Git commit (the runner owns commits).

Finish with a concise artifact/blocker summary and exactly one raw final status line, without a bullet, fence or backticks:
MILESTONE_AUTOMATION_STATUS: READY
or, if any readiness blocker remains:
MILESTONE_AUTOMATION_STATUS: BLOCKED

Selected milestone: {{MILESTONE}}.
