# M029 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: none — all tasks done. Blockers: none.
Last check: `bash scripts/verify-milestone.sh M029` passed 2026-09-11.
Changed scope: none (translate-shared E2E baseline assertion converted from
absolute to same-page delta; see T005).

## Ordered tasks
- [x] T001 — Reuse review of M027 operation entry points, M028 workspace/transport/fixture pattern, M013 guarded-route/session behavior, `inputPolicy.ts`, generated client transport; AC-001–008 prerequisites; depends on none; done when touched files/behaviors are listed and regressions identified.
- [x] T002 — Rewriting reducer + pure helpers (`rewritingWorkspace.ts`: dispatch guard, revisions, outdated marking, edit protection, usage ordering) with Vitest units; AC-002; depends on T001; done when `npm run test` passes the new unit cases.
- [x] T003 — `RewritePage` + `/rewrite` route swap + cookie-auth transport via regenerated generated types + Smoke deterministic rewriting provider (fail-closed, fixture + sentinels); AC-001/008; depends on T001, T002; done when component renders against the real host shape and `bash scripts/contract.sh check` shows zero drift.
- [x] T004 — Component checks (mode dropdown defaults/single-choice, forms, messages, request counts, displayed usage, live-region mutations); AC-001/002/003/004/005; depends on T003; done when Testing Library cases pass via `npm run test`.
- [x] T005 — Published E2E cases (login-form case + fixture cases for happy path with mode choice, oversize, invalid/eligibility, failure, edit/copy) with browser-visible assertions; AC-001–006; depends on T004; done when `npm run test:e2e` passes against the owned seeded host with password-scan clean.
- [x] T006 — Privacy/drift closeout (AC-007 sentinel inspection; typecheck/lint clean) + `python3 automation/context.py check M029`; AC-007/008; depends on T005; done when checks pass and evidence is recorded below.
- [x] T007 — Full runner gate `bash scripts/verify-milestone.sh M029`; all ACs; depends on T006; done when the gate passes and the completion record is filled.

## Completion record

Revision: working tree at `2f2e77b` (M029 planned) plus the M029 change set
(frontend workspace, Smoke rewriting provider, tests); no commit by the
runner (runner owns commits). Environment: .NET SDK 10.0.302, Node v24.20.0,
npm 11.11.0, Chromium (Playwright 1.63.0), Linux/macOS loopback HTTPS host,
isolated migrated SQLite + owned data-protection keys per smoke run. UTC
2026-09-11. Evidence: `artifacts/test-results/frontend-unit.xml` (215 tests,
0 failures), `artifacts/test-results/frontend-e2e.xml` (44 passed),
backend/API/AI suites via `verify-milestone.sh` (all green, including 7 new
`SmokeDeterministicRewritingTests`; API suite 217 → 224 tests).

T001 note. Touched entry points: `frontend/src/shell/App.tsx` (route swap;
M013 `SignedInPlaceholder` removed — no placeholder route remains),
`frontend/src/api/inputPolicy.ts` (reused unchanged; `validateRewriting`
already covered), `frontend/src/api/accounts.ts` (antiforgery/cookie pattern
reused for operation submits), `frontend/src/api/generated/linguadesk-api.d.ts`
(reused unchanged — `RewritingCapability`/`RewritingSuccessResponse` already
generated), new `frontend/src/rewrite/` + `frontend/src/api/rewriting.ts`,
`backend/src/LinguaDesk.Api/Infrastructure/Smoke/` (new rewriting provider;
seeder unchanged, same 7,500 baseline), `Program.cs` (one Smoke-gated
registration line). Regressions identified and closed: `login.test.tsx`
remembered-route case now lands on the real rewriting workspace (auth
contracts unchanged); `translate.spec.ts` happy path converted from the
absolute 7,546 baseline to a same-page +46 delta because both suites share
the one seeded account and `rewrite.spec.ts` sorts first (M028 scope and
evidence unchanged; standalone/first-run absolute still 7,500 + 46). Findings
that shaped the slice: the rewriting prompt JSON carries `sourceHint` on
eligibility vs `sourceLanguage`+`mode` on transformation (camelCase), so the
Smoke client distinguishes stages exactly like the M028 translation client;
unconfigured rewriting stays 202 pending outside Smoke, so the adapter had to
be built Smoke-only reusing the M028 monetary env
(`MonetaryAdmission__MonthlyCapMinorUnits=1000000`/`Currency=USD`).

T002. `rewritingWorkspace.ts`: explicit-dispatch guard, request-revision
capture, outdated marking on source/language/mode edits, manual-edit
protection (a later explicit submission clears protection and may replace
edits), stale-success fencing with UX-MSG-020 → UX-MSG-021 charge notice,
stale failures ignored, server problem → normative-message mapping (422
uncertain/oversizedSource/unsupported; no same-language branch — rewriting
has no target), usage formatting; 2,000-scalar limit with the UX-MSG-011
rewrite copy. 27 Vitest cases pass.

T003. `RewritePage` (Writing language selector with Detect automatically,
Writing mode dropdown with Correction only default + eight styles/tones,
source editor, explicit Rewrite, editable/copyable plain result,
authoritative usage, normative messages, offline banner, capability-load
retry) mounted on the guarded `/rewrite` route inside the shell `Page`
(heading focus kept); cookie-auth submit/status through generated types with
UUIDv7 identities (`family: 'rewriting'`, `sourceSelection` + `mode`, no
`target`); Smoke deterministic rewriting provider (fixed W-OK fixture
result, `M029-ELIGIBILITY-REJECT`/`M029-PROCESSING-FAILURE` sentinels,
fail-closed outside Smoke); `contract.sh check` zero drift.

T004. 12 Testing Library cases: defaults (Correction only, nine options)/
forms/messages/request counts/usage/live regions, zero-transformation
typing/wait, single-final-mode selection (Business then Friendly → one
request with Friendly), duplicate suppression via a gated first response,
oversize (W-LONG 2,312, excess 312, retained, disabled, zero ops),
edit/copy with exact clipboard value, stale/pending edit protection with
later-submission replacement, full problem-message matrix (503/504/
429-user/429-global/monetary/422-mixed/422-uncertain/401/403), retry with a
new operation key, IME-composition guard, capability-load failure.
`login.test.tsx` remembered-route case updated minimally for the real
workspace landing; full `frontend.sh check` (typecheck, lint, 215 unit
tests, build) green. One lint fix during the run: `selectInlineValidation`
takes only state (rewriting has no same-language inline branch).

T005. `tests/e2e/rewrite.spec.ts` (6 cases) + documented real-auth fixture
`rewrite-auth.ts` (antiforgery + real sign-in API → same-origin cookie
state; the login-form case covers the visible form, then asserts the
`/rewrite` workspace defaults). Happy path: Correction only default, manual
en, W-OK in, 1 s wait with zero `/api/operations`, one activation, exact
W-OK fixture result, +46 usage delta, source unchanged, edit with no new
operation, clipboard equals the edited value. Mode case (Friendly final,
zero prior requests, one request with `friendly`), oversize (W-LONG, excess
312, retained, disabled, zero ops), eligibility sentinel (MSG-008, source +
prior result preserved, usage unchanged), processing-failure sentinel
(MSG-013 + Try again, zero usage change, work preserved; retry resubmits
current fields with a new key; correcting input clears the failure and
reenables Rewrite with a +43 charge). 44/44 Playwright cases pass against
the owned host (38 pre-existing + 6 rewrite); the smoke password scan is
clean. Two failures fixed during the run, both in test expectations, both
kept as stronger coverage: usage reads now wait for the loaded snapshot
before parsing, and both happy paths assert same-page charge deltas so the
shared seeded account stays order-independent.

T006. AC-007 sentinel inspection: rewriting coordinator logs carry
counts/days/dispatches only; Smoke rewriting provider/seeder log nothing;
rewrite UI has no console/storage writes and shows only the user's own
usage; `OperationSubmission` persists counts/fingerprints only (no text
columns); problem details use fixed copy; `Cache-Control: no-store` on
text-bearing responses comes from the reused M027 endpoint mapping.
`contract.sh check` zero drift; `context.py check M029` unchanged
(12 sources); `context.py audit` clean. Typecheck + lint clean.

T007. `bash scripts/verify-milestone.sh M029` passed 2026-09-11 (context,
audit, backend check+smoke, contract, AI check+probe, frontend check+smoke
with 44 E2E, `git diff --check`).

Limitations. Deterministic fixture text is transport/presentation evidence
only (V-013 out of scope); one fixed W-OK result for every valid input, not
real rewriting. Sentinel-gated Smoke behaviors (`M029-ELIGIBILITY-REJECT`,
`M029-PROCESSING-FAILURE`) exist only behind the Smoke registration and never
in production. Seed usage assumes seed and run share a UTC day (midnight
crossover would move the baseline). Allowance-exhausted/monetary-suspended
UI mapping is proven at component level; E2E proves the 503-processing path.
Pending (202) has no dedicated E2E (deterministic provider always executes);
the client refreshes usage and shows Try again. Visual baselines and
browser/device/AT claims stay with M038/M039/G3.
