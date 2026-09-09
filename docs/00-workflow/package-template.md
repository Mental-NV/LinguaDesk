# Milestone package template

Under [#0](../00-SDD-Planning-Workflow.md), use `docs/08-backlogs/<scope-id>/` with a stable ID such as M015. No slug, separate package sequence or `specs/` tree. All four required artifacts share the same folder. Existing M001–M006 packages retain historical acceptance/evidence; a completed package is opened only for a relevant dependency, regression or audit.

## backlog.md

```markdown
# <scope-id> — <outcome>
Status: <item state>
Milestone: <roadmap row/heading link>

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-NNN | <bounded outcome or necessary enablement> | <priority> | <IDs/evidence> | <state> | [spec](spec.md) |

Acceptance summary: <brief outcome>; detailed ACs are in spec.md.
Human steps: <owner, timing, blocked gate; or None required>.
```

## spec.md

```markdown
# <scope-id> — Selected specification
Selected items: <BI IDs>. Status: <draft/ready; completion evidence in tasks.md>.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope
<Requirement IDs; included behavior; explicit exclusions; dependencies.>

## Acceptance
| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | <testable scenario> | <link/ID> |

## Constraints and decisions
<Applicable nonfunctional constraints; clarification status; human gates.>
```

## plan.md

```markdown
# <scope-id> — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief
<Outcome, component boundaries, indispensable invariants, explicit exclusions.>
<Selected canonical excerpts arrive through the context packet. Do not copy them all here.>

## Changes and order
<Concrete files/components, sequencing, shared/generated-contract gates, data/configuration.>
<Migration/rollout/rollback and operational effects when applicable.>

## Verification map
| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 | T001 | <applicable V-ID + runnable check or planned procedure> | <path/record> |

## Context boundaries and risks
<Why each omitted domain is irrelevant; when to open on-demand sources.>
<Dependencies, assumptions, blockers, human action owner/timing/gate.>
```

## tasks.md

```markdown
# <scope-id> — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: <task ID>. Blockers: <none or concrete prerequisite>.
Last check: <command/result or not run>. Changed scope: <none or owner link>.

## Ordered tasks
- [ ] T001 — <action + file/component>; AC-001; depends on <IDs/none>; done when <check>.

## Completion record
<AC/task, revision/configuration, actual command/procedure, environment, UTC timestamp,
result/evidence link, failures/fixes, and limitations. No pasted command logs.>
```

The companion [context.json schema and commands](../../automation/context-guide.md) let one execution packet combine small local specifications with exact canonical excerpts. Prefer this hybrid to a copied all-in-one specification (drift) or an unbounded bibliography (repeated reads). A reviewed lock protects spec/plan and selected inputs; task checkboxes/evidence remain mutable.
