# M028 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: none (all tasks done; runner owns commits). Blockers: none.
Last check: `bash scripts/verify-milestone.sh M028` passed 2026-09-11 (context,
audit, backend check+smoke, contract, AI check+probe, frontend check+smoke,
diff check).
Changed scope: one reviewed addition beyond the plan's "no backend handler
change": a Smoke-environment-only deterministic translation provider plus the
documented 7,500 usage seed and Smoke monetary config. No handler, wire-shape
or contract change (`contract.sh check` zero drift); production keeps the
unavailable provider.

## Ordered tasks
- [x] T001 — Reuse review of M026 operation entry points, M013 guarded-route/session behavior, `inputPolicy.ts`, generated client transport; AC-001–008 prerequisites; depends on none; done when touched files/behaviors are listed and regressions identified.
- [x] T002 — Translation reducer + pure helpers (dispatch guard, revisions, outdated marking, edit protection, usage ordering) with Vitest units; AC-002; depends on T001; done when `npm run test` passes the new unit cases.
- [x] T003 — `TranslatePage` + `/translate` route swap + cookie-auth transport via regenerated generated types; AC-001/008; depends on T001, T002; done when component renders against the real host shape and `bash scripts/contract.sh check` shows zero drift.
- [x] T004 — Component checks (forms, selectors, defaults, messages, request counts, displayed usage, live-region mutations); AC-001/003/004/005; depends on T003; done when Testing Library cases pass via `npm run test`.
- [x] T005 — Published E2E cases (login-form case + fixture cases for happy path, oversize, invalid, failure, edit/copy) with browser-visible assertions; AC-001–006; depends on T004; done when `npm run test:e2e` passes against the owned seeded host with password-scan clean.
- [x] T006 — Privacy/drift closeout (AC-007 sentinel inspection; typecheck/lint clean) + `python3 automation/context.py check M028`; AC-007/008; depends on T005; done when checks pass and evidence is recorded below.
- [x] T007 — Full runner gate `bash scripts/verify-milestone.sh M028`; all ACs; depends on T006; done when the gate passes and the completion record is filled.

## Completion record

Revision: working tree at `0083072` (M028 planned) plus the M028 change set
(frontend workspace, Smoke provider/seeder, smoke-script monetary env, tests);
no commit by the runner (runner owns commits). Environment: .NET SDK 10.0.302,
Node v24.20.0, npm 11.11.0, Chromium (Playwright 1.63.0), Linux/macOS loopback
HTTPS host, isolated migrated SQLite + owned data-protection keys per smoke
run. UTC 2026-09-11. Evidence: `artifacts/test-results/frontend-unit.xml`
(176 tests, 0 failures), `artifacts/test-results/frontend-e2e.xml` (38 passed),
backend/API/AI suites via `verify-milestone.sh` (all green, including 7 new
`SmokeDeterministicTranslationTests`).

T001 note. Touched entry points: `frontend/src/shell/App.tsx` (route swap;
`SignedInPlaceholder` kept for `/rewrite`), `frontend/src/api/inputPolicy.ts`
(reused unchanged), `frontend/src/api/accounts.ts` (antiforgery/cookie pattern
reused for operation submits), `frontend/src/api/generated/linguadesk-api.d.ts`
(reused unchanged), new `frontend/src/translate/` + `frontend/src/api/translation.ts`,
`backend/src/LinguaDesk.Api/Infrastructure/Smoke/` (new provider, extended
seeder), `Program.cs` (one Smoke-gated registration line),
`scripts/frontend.sh` (Smoke-serve monetary env). Regressions identified and
closed: `login.test.tsx` request-count/placeholder assertions updated for the
workspace landing (auth contracts unchanged; transformation submits still
forbidden); E2E `signin.spec.ts` unaffected (sign-out affordance kept,
Back shows no workspace). Findings that shaped the slice: the published host
registers `UnavailableTranslationClientProvider` (translation stays 202
pending) and binds no monetary config (per-dispatch admission denies), so the
#6 §4.5 deterministic-provider adapter had to be built Smoke-only with
`MonetaryAdmission__MonthlyCapMinorUnits=1000000`/`Currency=USD`; the Smoke
seeder created accounts with zero usage, so the §4.1 7,500 baseline is now
seeded as three settled 2,500-scalar submissions plus user/global ledger rows
(no source/result text stored). `/api/operations` requires no antiforgery
validation, but the client still sends the header as specified.

T002. `translationWorkspace.ts`: explicit-dispatch guard, request-revision
capture, outdated marking, manual-edit protection (a later explicit submission
clears protection and may replace edits), stale-success fencing with
UX-MSG-020 → UX-MSG-021 charge notice, stale failures ignored, server
problem → normative-message mapping, usage formatting. One fix from its own
suite: a later explicit submission now clears `resultEdited` (UX-AC-037).
26 Vitest cases pass.

T003. `TranslatePage` (selectors with `Chinese (Simplified output)`, source
editor, explicit Translate, editable/copyable result, authoritative usage,
normative messages, offline banner, capability-load retry) mounted on the
guarded `/translate` route inside the shell `Page` (heading focus kept);
cookie-auth submit/status through generated types with UUIDv7 identities;
`contract.sh check` zero drift.

T004. 11 Testing Library cases: defaults/forms/messages/request counts/usage/
live regions, zero-transformation typing/wait, duplicate suppression via a
gated first response, oversize/invalid guards, edit/copy with exact clipboard
value, full problem-message matrix (503/504/429-user/429-global/
monetary/422-mixed/422-uncertain/401/403), retry with a new operation key,
IME-composition guard, capability-load failure. `login.test.tsx` (30 cases)
updated minimally for the workspace landing; full `frontend.sh check`
(typecheck, lint, 176 unit tests, build) green.

T005. `tests/e2e/translate.spec.ts` (6 cases) + documented real-auth fixture
`translate-auth.ts` (antiforgery + real sign-in API → same-origin cookie
state; the login-form case covers the visible form). Happy path: manual
en→ro, 1 s wait with zero `/api/operations`, one activation, exact Romanian
fixture result, usage `7,546 of 20,000 characters used`, edit with no new
operation, clipboard equals the edited value. Oversize (T-LONG, excess 312,
retained, disabled, zero ops), same-language (target-first then source change
retains `en` with MSG-007, zero ops), eligibility sentinel (MSG-008, source +
prior result preserved, usage unchanged), processing-failure sentinel
(MSG-013 + Try again, zero usage change, work preserved; retry resubmits
current fields with a new key; correcting input clears the failure and
reenables Translate with a +20 charge). 38/38 Playwright cases pass against
the owned host; the smoke password scan is clean. Two failures fixed during
the run, both in test expectations, both kept as stronger coverage: the fixed
fixture text is identical for every valid input (retry proven by key + charge,
not text), and correcting input clears Try again per FR-011 (retry proven
before the correction, Translate after).

T006. AC-007 sentinel inspection: coordinator logs carry counts/days/
dispatches only; Smoke provider/seeder log nothing; translate UI has no
console/storage writes; `OperationSubmission` persists counts/fingerprints
only; problem details use fixed copy; the usage line shows only the user's
own consumed/allowance/remaining/reset. `contract.sh check` zero drift;
`context.py check M028` unchanged (12 sources). Typecheck + lint clean.

T007. `bash scripts/verify-milestone.sh M028` passed 2026-09-11 (context,
audit, backend check+smoke, contract, AI check+probe, frontend check+smoke
with 38 E2E, `git diff --check`).

Limitations. Deterministic fixture text is transport/presentation evidence
only (V-013 out of scope); per-target fixed strings, not real translation.
Sentinel-gated Smoke behaviors (`M028-ELIGIBILITY-REJECT`,
`M028-PROCESSING-FAILURE`) exist only behind the Smoke registration and never
in production. Seed usage assumes seed and run share a UTC day (midnight
crossover would move the baseline). Allowance-exhausted/monetary-suspended
UI mapping is proven at component level; E2E proves the 503-processing path.
Pending (202) has no dedicated E2E (deterministic provider always executes);
the client refreshes usage and shows Try again. Visual baselines and
browser/device/AT claims stay with M038/M039/G3.
