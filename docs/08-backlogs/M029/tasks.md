# M029 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: none.
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T001 — Reuse review of M027 operation entry points, M028 workspace/transport/fixture pattern, M013 guarded-route/session behavior, `inputPolicy.ts`, generated client transport; AC-001–008 prerequisites; depends on none; done when touched files/behaviors are listed and regressions identified.
- [ ] T002 — Rewriting reducer + pure helpers (`rewritingWorkspace.ts`: dispatch guard, revisions, outdated marking, edit protection, usage ordering) with Vitest units; AC-002; depends on T001; done when `npm run test` passes the new unit cases.
- [ ] T003 — `RewritePage` + `/rewrite` route swap + cookie-auth transport via regenerated generated types + Smoke deterministic rewriting provider (fail-closed, fixture + sentinels); AC-001/008; depends on T001, T002; done when component renders against the real host shape and `bash scripts/contract.sh check` shows zero drift.
- [ ] T004 — Component checks (mode dropdown defaults/single-choice, forms, messages, request counts, displayed usage, live-region mutations); AC-001/002/003/004/005; depends on T003; done when Testing Library cases pass via `npm run test`.
- [ ] T005 — Published E2E cases (login-form case + fixture cases for happy path with mode choice, oversize, invalid/eligibility, failure, edit/copy) with browser-visible assertions; AC-001–006; depends on T004; done when `npm run test:e2e` passes against the owned seeded host with password-scan clean.
- [ ] T006 — Privacy/drift closeout (AC-007 sentinel inspection; typecheck/lint clean) + `python3 automation/context.py check M029`; AC-007/008; depends on T005; done when checks pass and evidence is recorded below.
- [ ] T007 — Full runner gate `bash scripts/verify-milestone.sh M029`; all ACs; depends on T006; done when the gate passes and the completion record is filled.

## Completion record
Not started. Selected package locked for execution; no production code or tests written by planning.
