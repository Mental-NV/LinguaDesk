# 004 — Independent AI Development: Tasks

**Version:** 1.0 · **Updated:** 2026-09-09
**State:** Ready for implementation; all tasks pending
**Inputs:** [spec.md](spec.md) v1.0; [plan.md](plan.md) v1.0

## 1. Ordered implementation tasks

- [ ] **T001 — Preflight the selected baseline and dependency graph.** Confirm Git/source revisions, M001 completion, SDK `10.0.302`, package access/audit and existing backend/frontend commands. Add the central `Microsoft.Extensions.AI.Abstractions` 10.9.0 pin, three selected project files/solution entries and their locked dependency graphs; do not add Core, API→Ai, provider, full AI middleware or evaluation-package references. **Targets:** `backend/Directory.Packages.props`, `backend/LinguaDesk.slnx`, new Ai/test/runner project and lock files. **Depends:** none. **Checks:** AC-001/004/005; locked restore succeeds, the graph matches the plan and any indispensable access/audit failure stops dependent work.
- [ ] **T002 — Implement the shared prompt snapshot and single-call boundary.** Add the embedded `eligibility.v1` instruction, typed synthetic-input composition, canonical prompt metadata/hash and narrow complete-response `IChatClient` call. Create fresh ordered messages/options per invocation, serialize source only as JSON user data, pass cancellation and return the raw response; add no parser, product classification, retry/fallback, admission, provider adapter, middleware, streaming or logging. **Targets:** `backend/src/LinguaDesk.Infrastructure.Ai/`. **Depends:** T001. **Checks:** AC-002/003/005; deterministic fixed snapshot and exactly one scripted-compatible call with the exclusions intact.
- [ ] **T003 — Add focused independent-AI verification.** Implement the capturing/scripted `IChatClient` and MSTest cases for project isolation, prompt roles/order/hash/round-trip/determinism, fresh request/options, one call, cancellation and exception propagation. Guard against zero/absent/failed tests and ensure fixtures/output contain only committed synthetic text. **Targets:** `backend/tests/LinguaDesk.Infrastructure.Ai.Tests/` and focused report helpers if needed. **Depends:** T002. **Checks:** AC-001–005; V-007 selected assertions pass deterministically and a controlled test failure returns nonzero.
- [ ] **T004 — Deliver the standalone inspect/probe workflow.** Implement strict `inspect` and `probe` runner modes plus `scripts/ai.sh setup|check|inspect|probe`. Use the same shared composition; keep fixed inputs, canonical output and raw/scripted labeling; reject extra/unknown arguments and propagate runner failures; target only AI projects and run offline with no API/UI/storage/live configuration. **Targets:** `backend/tools/LinguaDesk.Ai.Evaluation/`, `scripts/ai.sh`. **Depends:** T002/T003. **Checks:** AC-002–005; repeated inspect output is byte-identical, probe captures the same request once, unreachable-proxy/no-credential runs pass and negative commands exit nonzero.
- [ ] **T005 — Integrate aggregate checks, documentation and regressions.** Make `scripts/backend.sh check` safe for multiple test projects with collision-free reports and separate minimum guards for the retained 42 API/storage cases and a positive AI count. Document only verified AI commands/synthetic-only limitations in README. Run locked focused AI checks plus backend check/smoke and frontend check/smoke; inspect the diff/artifacts for accidental provider, credential, database, listener, report or generated-file effects. **Targets:** `scripts/backend.sh`, `README.md`, `.gitignore` only if required, existing regression surface. **Depends:** T004. **Checks:** AC-004–007; all selected and retained suites pass with report paths/counts and controlled failures remain nonzero.
- [ ] **T006 — Reconcile acceptance and close.** Review the complete diff against #0, BI-004, spec and plan. Record actual baseline/implementation revision and environment, package/resolved versions, commands/codes/counts/reports, prompt ID/hash, no-effect observations, risks/limitations and scenario outcomes below. Update #4/#6/#7/backlog/tasks/README only with supporting evidence; preserve M001–M003 history and leave M015–M020/live/product/release evidence pending. **Targets:** this record and affected planning/operating documents. **Depends:** T005. **Checks:** AC-001–007 supported before Done; otherwise leave affected state incomplete with its real blocker and remaining work.

## 2. Scenario-to-task mapping

| Scenario | Tasks | Planned evidence |
| --- | --- | --- |
| AC-001 | T001/T003/T005/T006 | Project/asset graph, locked Release build and independent AI test report |
| AC-002 | T002/T003/T004/T006 | Canonical repeated inspect output, prompt ID/hash and exact JSON round-trip assertions |
| AC-003 | T002/T003/T004/T006 | Capturing scripted-client call count/messages/options/token and raw probe observation |
| AC-004 | T001/T003/T004/T005/T006 | SDK/locked commands, positive test counts and recorded controlled nonzero failures |
| AC-005 | T001/T002/T003/T004/T005/T006 | No-credential/unreachable-proxy run and host/database/frontend/report-effect inspection |
| AC-006 | T001/T005/T006 | Aggregate API/storage/AI reports plus backend process and published frontend regressions |
| AC-007 | T005/T006 | Verified README commands and reconciled package/shared evidence with explicit limitations |

## 3. Completion record

**Implementation status:** Not started. **Scenario disposition:** AC-001–007 pending. **Human action/blocker:** None anticipated.

Record runtime evidence here during implementation. Planning/document review is not execution evidence. Do not mark tasks or BI-004/M004 done until every selected scenario and applicable regression passes; do not claim eligibility, provider, evaluation, product or release evidence from the synthetic boundary.
