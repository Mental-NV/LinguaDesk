Repair the planning context for one LinguaDesk milestone.

The milestone's owning package is `docs/08-backlogs/{{MILESTONE}}/`; use it as
the primary location for milestone-specific context, scope, tasks and evidence.

Run `python3 automation/context.py check {{MILESTONE}}` and
`python3 automation/context.py audit`. Inspect every reported failure and make
the smallest planning-artifact or context-manifest changes needed for both
commands to pass. Preserve valid work already present in the working tree.
Review changed authoritative inputs before updating affected milestone content
or refreshing its context lock; never blindly relock stale context. Do not
change production code or tests, and do not create a Git commit (the runner
owns commits).

Finish with a concise repair/blocker summary and exactly one raw final status
line, without a bullet, fence or backticks:
MILESTONE_CONTEXT_REPAIR_STATUS: COMPLETE
or, if the validation cannot be repaired safely:
MILESTONE_CONTEXT_REPAIR_STATUS: BLOCKED

Selected milestone: {{MILESTONE}}.
