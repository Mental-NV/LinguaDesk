# Milestone automation

`automation/run-milestones.sh` plans, implements, verifies and commits roadmap
milestones in numeric order. Run it from a clean repository with Git, Python
3.9+, the project toolchains and the selected authenticated agent CLI available.

## Usage

```sh
./automation/run-milestones.sh 15 20           # Codex (default)
./automation/run-milestones.sh --claude 15 20  # Claude
./automation/run-milestones.sh --muse 15 20    # Muse
```

The start and end milestone numbers are inclusive. Both are optional; they
default to `1` and `999`. Use the same number twice to run one milestone.
