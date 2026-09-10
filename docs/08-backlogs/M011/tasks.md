# M011 — Tasks and evidence

Inputs: [spec](spec.md), [plan](plan.md).

## Resume

Next: none; M011 is complete. Blockers: none.
Last check: focused suites, aggregate backend/contract/frontend gates, published-browser smoke, dependency audits, diff checks and context audits passed (see record).
Changed scope: `frontend/src/api/accounts.ts`, `frontend/src/auth/RegisterPage.tsx`, `frontend/src/auth/VerifyEmailEntryPage.tsx`, `/register` + `/verify-email` routing and form styles in `frontend/src/shell/`, `frontend/tests/unit/register.test.tsx`, `/register` contract in `frontend/tests/unit/App.test.tsx`, `frontend/tests/e2e/registration.spec.ts`.

## Ordered tasks

- [x] T001 — Reconfirm the locked baseline, selection state, M002/M006 completion, tool pins and restore access; verify `registerLocalAccount` in the generated client and run the canonical drift check with no API change. AC-007; depends on none; done when preflight passes and the adopted shape is current.
- [x] T002 — Implement the `/register` form, verification-entry state and routing changes under `frontend/src/`; wire exactly `{email, password}` through the generated type with flight disabling, duplicate guards, stale-completion discard, password clearing and in-memory-only email. AC-001–006; depends on T001; done when component behavior matches the spec contract.
- [x] T003 — Add focused Testing Library suites covering every AC-001–006 assertion without live services, sleeps or secret-bearing reports; update the `/register` placeholder assertions in `App.test.tsx`. AC-001–006; depends on T002; done when the focused suite passes.
- [x] T004 — Extend the published Chromium smoke with the registration trip, reload-clears and sentinel checks while preserving M002 boundary trips. AC-001/002/005/006; depends on T002/T003; done when the browser trip passes against the owned loopback host.
- [x] T005 — Run contract drift, aggregate backend/frontend gates, published smoke and dependency audits; inspect output, processes, logs and diff for echoed passwords, stored credentials or side effects while preserving M001–M010 behavior. AC-007; depends on T001–T004; done when every exact plan command passes with sanitized evidence or the failing gate remains recorded.
- [x] T006 — Reconcile the diff and actual evidence against BI-011, FR-001, UX-AC-006/007/101/102 and V-003/V-010/V-015; update only affected owner/status/coverage/operating rows and this backlog/tasks record, leaving M012–M014, M034, full product requirements, Q-004, P-005/P-006 and release gates pending. AC-001–007; depends on T005; done when every selected AC has reproducible evidence, local link/context audits pass and no unsupported completion claim remains.

## Completion record

Implementation and closing verification completed 2026-09-10T10:40:13Z on
Darwin 25.6.0 arm64, .NET SDK 10.0.302, Node v24.20.0 and npm 11.11.0.
Planning baseline: `7499a7d6d6d3428cf200ba139fa3e0b06e698269`; implementation
worked uncommitted on top of `aa04356` (M011 planned); no commit was created
(the runner owns commits). `python3 automation/context.py check M011` reports
the 30 selected inputs unchanged.

| Task / AC | Command or procedure | Result / evidence |
| --- | --- | --- |
| T001 / AC-007 | `python3 automation/context.py check M011`; Git/status and tool-pin inspection; `bash scripts/contract.sh check`; `registerLocalAccount` presence in `frontend/src/api/generated/linguadesk-api.d.ts` | Lock selected inputs unchanged (30 sources); `dotnet --version` 10.0.302, `node --version` v24.20.0, `npm --version` 11.11.0. Generated `registerLocalAccount` operation with exact `{email, password}` body and 202/400/415/503 no-store shapes adopted unchanged; no generation or handler work. |
| T002–T003 / AC-001–006 | New `frontend/src/api/accounts.ts` (typed `fetch` wrapper sending exactly `{email, password}`, 202/field/retry mapping, no logging); new `frontend/src/auth/RegisterPage.tsx` (UX §9 form, 15–128 scalar checklist via shared counting, Show password with caret preservation, linked errors with first-invalid focus, flight disabling, duplicate guard, stale-completion discard, password clearing, in-memory email) and `VerifyEmailEntryPage.tsx` (MSG-034 entry, no resend/continuation); `/register` + `/verify-email` routes in `frontend/src/shell/App.tsx`; form styles in `shell.css` | Focused suites 27/27 passed (`frontend/tests/unit/register.test.tsx` 18 tests, `frontend/tests/unit/App.test.tsx` 9 tests): single-request `{email, password}` submission with disabled-fields/duplicate-Enter guard and MSG-034 entry; no-request validation matrix (required/shape, 14/129-scalar and lone-surrogate policy cases, mismatch) with `Check the highlighted fields.`, linked errors, `aria-invalid` and first-invalid focus; 15/128 checklist truthfulness with the `Maple!River2026` 15-scalar shape; 400-rejection password clearing with retained email, first-error focus and one-request retry; network-failure generic retry with no false success; resolve-after-unmount discard with no password restore; Enter-once keyboard path; caret/selection-preserving reveal; storage/history/URL sentinel absence. Report: `artifacts/test-results/frontend-unit.xml`. |
| T004 / AC-001/002/005/006 | `bash scripts/frontend.sh smoke` (owned loopback host, isolated database/keys, synthetic `example.test` address) with new `frontend/tests/e2e/registration.spec.ts` | Published Chromium smoke 11/11 passed (6 retained M002 shell trips, 5 registration trips): valid registration posts exactly once to the real `/api/accounts/register` and reaches `/verify-email` with MSG-034 and the in-memory email; invalid local input sends no register request; reload clears password state and drops the memory-only verification entry back to `/register`; 390px/320px registration reflow with reduced motion and unclipped errors. Report: `artifacts/test-results/frontend-e2e.xml`. |
| T005 / AC-007 | `bash scripts/contract.sh check`; `bash scripts/backend.sh check`; `bash scripts/frontend.sh check`; `bash scripts/frontend.sh smoke`; `npm --prefix frontend audit --audit-level=moderate`; `dotnet list backend/LinguaDesk.slnx package --vulnerable --include-transitive`; `git diff --check`; `python3 automation/context.py audit`; sentinel scans of reports/sources | Contract drift check passed unchanged (no API surface added). Backend 134/134 passed (114 API/storage, 10 Core, 10 AI). Frontend gate passed: typecheck, lint, 55 component/policy tests (28 shared input-policy), production build. Published smoke 11/11. npm audit 0 vulnerabilities; .NET audit no vulnerable packages. `git diff --check` clean; context link/layout audit passed (96 Markdown documents). Fixture password absent from test reports and shipped sources (synthetic fixtures live only in test sources per plan); no storage/credential side effects; M001–M010 behavior retained. |
| T006 / AC-001–007 | Full diff/schema/log/process/privacy review; owner updates (backlog, current delivery, coverage §8.1/§8.2); `python3 automation/context.py audit` | Diff scoped to the frontend registration slice plus M011 records; no API, migration, package, auth-scheme or persistence change. BI-011 done with every selected AC reproducibly evidenced; M012–M014, M034, full FR-001, Q-004, P-005/P-006 and release gates remain pending. |

AC-001 through AC-007: **passed** with the evidence above for the bounded M011
web-registration slice; BI-011 is implemented and done. No human/live evidence
gate is selected by M011. This does not complete full FR-001, UX-AC-006/007/101/102
beyond the registration-form portions, verification/resend/continuation (M012),
sign-in/out and safe-return routing (M013), recovery forms (M014), live email
(M034) or any release gate.
