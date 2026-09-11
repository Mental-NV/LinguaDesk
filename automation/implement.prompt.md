Execute one LinguaDesk milestone under the authoritative SDD workflow.

The milestone's owning package is `docs/08-backlogs/{{MILESTONE}}/`; use it as
the primary location for milestone-specific context, scope, tasks and evidence.

Run `python3 automation/context.py packet {{MILESTONE}}`. It validates reviewed inputs and supplies #0, execution rules, selected canonical excerpts, spec/plan and mutable backlog/tasks in that order. Follow this bounded reading set; do not reread all authoritative documents or follow every link. Inspect relevant code and current dependencies/Git changes. If the packet fails, review the affected inputs and plan before continuing; never blindly refresh a lock. Missing required context must be resolved at its owner and added to the selected manifest.

Complete all ordered tasks and selected acceptance, run every applicable package/regression check, review the full diff, and update tasks/evidence, backlog state, current delivery status, affected coverage rows and operating instructions. Preserve human/live evidence gates and scope exclusions. No other milestone or Git commit (the runner owns commits). The runner's fixed checks supplement package-specific verification.

Finish with a concise outcome/blocker summary and exactly one raw final status line, without a bullet, fence or backticks:
MILESTONE_AUTOMATION_STATUS: COMPLETE
or, when work/evidence/blockers remain:
MILESTONE_AUTOMATION_STATUS: BLOCKED

Selected milestone: {{MILESTONE}}.
