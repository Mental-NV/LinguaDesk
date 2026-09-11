# Milestone automation

`automation/run-milestones.sh` plans, implements, verifies and commits roadmap
milestones in an explicitly supplied order. Run it from a clean repository with
Git, Python 3.9+, the project toolchains and the selected authenticated agent
CLI available.

## Usage

```sh
./automation/run-milestones.sh "19, 15-18, 20, 21, 22-26"          # Codex
./automation/run-milestones.sh --claude "19, 15-18, 20"           # Claude
./automation/run-milestones.sh --muse "19, 15-18, 20"             # Muse
```

Pass one quoted, comma-separated expression. Each item is either a milestone
number or an inclusive ascending range. Ranges expand in place, so
`"19, 15-18, 20"` processes `M019`, then `M015` through `M018`, then `M020`.

After planning, the runner validates the milestone context lock and audits
local documentation links and package layout. If either validation fails, the
selected AI runner gets up to three context-repair attempts. Both validations
are rerun after every repair; an unrepaired package is left uncommitted and
implementation does not start.

Implementation and the runner use the same regression entry point:

```sh
bash scripts/verify-milestone.sh M026
```

It checks the selected context lock, documentation audit, backend tests and
smoke, contract drift, independent AI checks/probe, frontend checks and
published browser smoke, and whitespace errors. Package-specific acceptance
and human/live evidence remain additional requirements. The runner executes
the gate after pulling upstream changes and before the implementation commit.

When this gate fails, the selected agent receives the saved failed command and
output and gets up to three repair attempts. Every repair is followed by the
entire gate again; a COMPLETE message alone cannot authorize the commit. A
BLOCKED result, agent failure or exhausted repair budget stops the run with
the working tree preserved. Repairs must fix causes against the selected
requirements; they must not skip checks or weaken acceptance. Planning and
implementation prompts also require review of transitional check assumptions
when a milestone introduces a new integration boundary.

Agent transcripts (including separate Muse attempts) and runner verification
logs are retained under ignored `artifacts/milestone-runs/run.*/`. Failure
output identifies the stage and log directory. These are local diagnostic
artifacts; review/redact them before sharing.

## Recovering a stopped run

The runner still requires a clean tree at startup and does not adopt arbitrary
uncommitted work. Do not stash a completed milestone simply to restart it.
Inspect its diff, task evidence and failure log, repair the cause, then run
the shared verification command above and any remaining package checks. Update
the task evidence with the recovery results. After reviewing the complete diff,
commit the recovered implementation with the recognized subject
`M026 implemented` (substitute the selected milestone). The next normal run
will skip that milestone and continue the requested order. If upstream changes
are integrated during recovery, rerun verification on the integrated tree
before committing.

This recovery is explicit: the runner does not yet have a checkpoint-based
resume mode. Use one runner per checkout and avoid concurrent manual changes,
because the runner stages the whole working tree when committing.
