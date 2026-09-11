# M033 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: none (all tasks done). Blockers: none.
Last check: `bash scripts/backend.sh check` passed 505/505; `bash scripts/backend.sh smoke` passed; `bash scripts/contract.sh check` passed; `python3 automation/context.py check M033` fresh at close.
Changed scope: none (no handler, policy or schema change; sample + docs only).

## Ordered tasks
- [x] T001 — Confirm M026/M027 dependency evidence and current wire surface (operations/status/usage routes, generated YAML/types); AC-005 baseline; depends on none; done when routes and committed artifacts are identified with no behavior change.
- [x] T002 — Scaffold `samples/api-consumer/` (plain-fetch Node script using generated types only, README, token-from-environment with redaction); AC-001/AC-002/AC-006; depends on T001; done when the sample completes one translation and one rewrite with snapshots against an isolated host, no React executed.
- [x] T003 — Extend the sample run with recovery and classified-failure demonstrations (replay metadata, duplicate/conflict, usage read, 4xx input + 401 with categories); AC-003/AC-004/AC-006; depends on T002; done when the run output shows each case distinctly from success.
- [x] T004 — Regenerate and review contracts (`generate` + `check`), record per-shape semantic agreement of touched envelopes/codes/categories with observed runtime behavior; AC-005; depends on T003; done when `check` is clean and the review record is linked.
- [x] T005 — Run regression (`bash scripts/backend.sh`), refresh `python3 automation/context.py check M033`, write the completion record; all ACs; depends on T004; done when suites pass and the manifest is fresh.

## Completion record

Revision: dd24aad (M033 planned) plus uncommitted `samples/api-consumer/` and
M033 doc updates (runner owns commits; no commit made here).
Environment: .NET SDK 10.0.302, Node v24.20.0, macOS loopback host.
UTC: 2026-09-11 (sample run and checks executed this date).

- T001: M026/M027 confirmed Done via delivery/current.md; wire surface
  confirmed in code: `POST /api/operations`, `GET /api/operations/{id}`,
  `GET /api/usage`, `POST /api/accounts/bearer-sign-in`; committed
  `docs/05-openapi.yaml` and `frontend/src/api/generated/linguadesk-api.d.ts`
  present. No behavior change.
- T002/T003: `bash samples/api-consumer/run.sh` builds the API, migrates an
  isolated temporary database, seeds verified/unverified local accounts via
  the one-shot `Smoke` seeder, serves on an OS-assigned loopback port with
  deterministic Smoke translation/rewriting providers
  (`MonetaryAdmission__MonthlyCapMinorUnits=1000000`, matching the
  published-suite host), then runs `consumer.mjs`. Clean run demonstrated:
  sign-in (verified, expiresIn 900); translation 201 with exact `ro` fixture
  text, 46-scalar charge on 2026-09-11; status re-read succeeded with
  outputAvailable=false; rewrite 201 mode simple, 47-scalar charge; status
  re-read succeeded with outputAvailable=false; settled usage delta +93 over
  the 7500 seeded baseline; duplicate replay 200 with no new charge;
  changed-payload 409 identityConflict; empty-source 422 inputEligibility
  with no charge; anonymous usage 401. AC-001/AC-002/AC-003/AC-004 passed.
  AC-006 passed by inspection: token travels by environment variable, is
  used only in the Authorization header and never printed; only synthetic
  fixture text appears in output. No React executed (only `node:crypto`).
- T004: `bash scripts/contract.sh check` passed with zero drift. Semantic
  review: committed YAML operations `submitLanguageOperation`,
  `getLanguageOperationStatus`, `getCurrentUsage` expose the observed
  200/201/401/409/422 codes; `TranslationSuccessResponse.translatedText`,
  `RewritingSuccessResponse.rewrittenText`,
  `OperationStatusResponse.outputAvailable` and all `UsageSnapshot` fields
  match observed runtime payloads; observed categories `identityConflict`
  and `inputEligibility` sit inside the documented Problem Details contract.
  TypeScript declarations contain all touched operations/schemas. AC-005
  passed. No finding: no silent contract edit was needed or made.
- T005: `bash scripts/backend.sh check` passed 505 total / 505 passed /
  0 failed (API/storage 224, core 10, AI 271); `bash scripts/backend.sh
  smoke` passed; `git diff --check` clean;
  `python3 automation/context.py check M033` reports selected inputs and
  spec/plan unchanged. Failures/fixes during execution: initial run.sh
  combined seeding with serving (the seed step exits 0 by design, Program.cs
  lines 48–53) and omitted the monetary-cap override (503
  monetarySuspension); fixed by splitting seed-then-serve per the
  frontend.sh pattern and setting the same cap override. Limitations: sample
  proves the independent-consumer path against deterministic fake
  providers; accounting/eligibility/deadline policy evidence stays with the
  M026/M027 regression suites, not the sample.

AC verdicts: AC-001 passed, AC-002 passed, AC-003 passed, AC-004 passed,
AC-005 passed, AC-006 passed.
