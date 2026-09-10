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
