## Milestone automation

`automation/run-milestones.sh` plans, implements, verifies, and commits roadmap milestones sequentially with non-interactive Codex runs. Start it only from a clean repository after committing the automation files and authenticating the Codex CLI:

```sh
./automation/run-milestones.sh 3 10
```

The two arguments are inclusive numeric milestone bounds; omitted bounds default to `1` and `999`. For each defined, unfinished milestone, the runner creates `<ID> planned`, executes that plan, runs the backend solution tests in Release mode, and creates `<ID> implemented`. It stops immediately when Codex reports a blocker, omits the required readiness/completion status, or when testing or Git fails. It skips implementation commits already in history, recognizes the legacy M001/M002 commit wording, and reuses an existing planning commit when resuming.

The planning and implementation instructions live in `automation/plan.prompt.md` and `automation/implement.prompt.md`. The implementation run is responsible for every package-specific check (including frontend, smoke, integration, or evaluation checks); the runner repeats the backend solution tests as its final independent gate. If a run stops with working-tree changes, inspect and either commit, restore, or stash them before resuming—the clean-tree guard will not absorb unrelated work into a milestone commit.