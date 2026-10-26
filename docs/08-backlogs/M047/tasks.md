# M047 — Tasks and evidence

Inputs: [spec](spec.md), [plan](plan.md).

## Resume

Next: T005/T007 establish bootstrap/provisioning contracts, then T001 and the remaining implementation tasks when implementation is requested. Scope of the current request is planning only.
Blockers: none for repository implementation. T006 requires authenticated Azure/GitHub bootstrap access, regional F1 quota and runtime provider keys. Hosted filesystem semantics must pass the selected storage checks.
Last check: `python3 automation/context.py check M047`, `python3 automation/context.py audit` (244 Markdown documents), and `git diff --check` passed on 2026-10-26. Changed scope: Bicep provisions resources; configuration is versioned/generated with zero GitHub variables, one secret per environment and capped Application Insights. SQLite remains a file in each Linux App Service.

## Tasks (stable IDs; dependencies define execution order)

- [ ] T001 — Implement the App Service SQLite DELETE profile and exclusive pre-serve preparation path; AC-001/006/007; no dependencies; done when fresh/upgrade/failure/locking/key-persistence tests pass without rewriting applied migrations.
- [ ] T002 — Add the offline CI entrypoint and package preparation; AC-004/005; no dependency on Azure setup; done when existing offline backend/AI/frontend/Chromium checks pass and live commands/credential injection are excluded.
- [ ] T003 — Add event routing, OIDC, environment scope, stale-run protection and serialization; AC-002/003/006/008; depends T002; done when actionlint and routing/failure fixtures pass.
- [ ] T004 — Add combined ZIP, startup/migration wrapper, bounded smoke and rollback procedure; AC-004/007/008/009; depends T001–T003; done when isolated deploy/restart/failure/restore rehearsals preserve account/ledger/key state.
- [ ] T005 — Implement bootstrap, generated non-secret identity manifest, GitHub environment setup and .NET/Bicep configuration contract; AC-001/006/010; no Azure dependency for offline fixtures; done when bootstrap is idempotent, zero GitHub variables are required and only the provider secret must be supplied. Finish the operator guide after T003/T004/T007.
- [ ] T006 — Verify provisioned Azure Staging through PR updates and Production through master push; AC-007/008/009/010/011/012; depends T001–T005/T007/T008 and bootstrap access; done when real provision/reconcile/deploy/restart/persistence evidence, provider-free smoke, exact cap/retention readback and isolated telemetry markers pass. No live LLM evaluation.
- [ ] T007 — Implement environment Bicep modules and automated PowerShell cap reconciliation; AC-001/010/011; depends T005 configuration contract; done when template/parameter checks, mocked failure/idempotency cases, 50 MB → 0.05 GB conversion and both-cap readback pass without manual settings or paid SKUs.
- [ ] T008 — Add config-bound Azure Monitor telemetry with privacy filters and sampling; AC-005/011/012; depends T005 configuration contract; done when offline exporter/privacy tests pass and CI cannot export externally. Hosted evidence belongs to T006.

## Planning record

2026-10-26: reviewed event routing, trust boundaries, F1 topology, actual publish/check commands, configuration names, SQLite migration/journal conflict, storage isolation, offline-only verification and AC-to-task coverage. Revised after user direction: replace manual resource/configuration setup with Bicep/bootstrap, reduce GitHub inputs to one secret per environment, and add two isolated monitoring stacks with automated daily caps. Reviewed allowance arithmetic, delayed cap enforcement, SDK privacy and no external export in offline checks. The Linux SQLite clarification is incorporated in architecture §6.1 and this package. No application code, workflows, Azure resources or GitHub settings were changed by planning. No live tests or deployment ran.

## Completion record

Implementation not started. Record revision, environment, command/procedure, UTC timestamp, result and sanitized evidence for each task. Hosted or quality checks not run remain unverified.
