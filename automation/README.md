# Milestone automation

`automation/run-milestones.sh` plans, implements, verifies and commits milestones sequentially with noninteractive Codex runs. Start only from a clean repository with the automation changes committed and Codex CLI authenticated. Python 3.9+, Git and the documented application toolchains are required.

```sh
./automation/run-milestones.sh 15 20
```

Bounds are inclusive numeric IDs (defaults 1–999). The runner follows numeric order within those bounds; it does not compute the roadmap's recommended dependency order. Consult [current delivery status](../docs/delivery/current.md) and choose eligible bounds. It stops at an undefined milestone, readiness blocker, missing final status, failed context/link check, failed regression or Git error. Existing implementation commits are skipped, including legacy M001/M002 wording.

Each unfinished milestone gets a planning run and `<ID> planned` commit, an implementation run, regression checks and `<ID> implemented` commit. An existing planning commit is reusable only when its selected context lock is current. Missing/stale manifests trigger planning again; READY without a valid manifest cannot proceed to implementation or a planning commit. Source hashes check freshness, not semantic readiness. Planning/implementation prompts use the [bounded context workflow](context-guide.md), with all package artifacts under `docs/08-backlogs/<ID>/`.

The implementation run owns every selected package check and evidence gate. The runner additionally executes backend check/smoke, contract drift, AI check/probe and frontend check/smoke. It does not establish live-provider, email, manual accessibility or release evidence through these deterministic checks. If a run stops with changes, inspect and commit, restore or stash them deliberately before resuming; the clean-tree guard prevents absorbing unrelated work.

Documentation/tooling checks that do not invoke Codex, application services or commits:

```sh
python3 automation/context.py audit
python3 -m unittest discover -s automation/tests
bash -n automation/run-milestones.sh
```
