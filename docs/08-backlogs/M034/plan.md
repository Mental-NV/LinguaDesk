# M034 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Change the account-creation point to set the existing Identity confirmation flag and remove registration's initial-delivery call. Rename the accepted registration status to `signInRequired`, update the active web success state to offer explicit sign-in without a verification guard, retain unavailable production senders and all dormant verification/recovery contracts, then regenerate and review the declared contract. No database migration or external service is involved.

Selected canonical excerpts arrive through the context packet. Do not copy them all here.

## Changes and order

1. Backend registration: set `EmailConfirmed = true` only on a successfully created account; do not invoke the confirmation-delivery coordinator; return `signInRequired` while retaining the generic duplicate branch and no-credential response.
2. Privacy/regression: prove no sender/cooldown work occurs for new accounts, duplicate registration cannot alter a row (especially a seeded unverified row), and `VerifiedAccount` still denies deliberately unverified accounts.
3. Frontend registration: replace the form in place with UX-MSG-034, clear secrets, avoid echoing the address or installing a pending-verification guard, focus the result and offer `/login`.
4. Deferred boundary: keep the unavailable confirmation/reset sender registrations and retained M007/M010/M012/M014 operations unchanged; remove the unfinished SMTP adapter/configuration/tests from this package and record restoration under DF-008.
5. Contract and evidence: advance the pre-release document revision to M034, regenerate OpenAPI/TypeScript, inspect the semantic diff, run focused and aggregate checks, refresh the context lock and close the package.
6. No migration or rollout step. Rollback restores unverified creation, registration delivery, the prior status enum and verification-routing UI together; a partial rollback is invalid.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001/AC-002 | T002 | Focused `AccountRegistrationTests`: durable verified creation, no sender/cooldown, response headers/body, sequential/case/concurrent duplicate invariants and seeded-unverified duplicate | tasks.md completion record |
| AC-003 | T003 | Focused registration/App unit tests and published registration Playwright case: in-place focused status, secrets/address absent, `/login` action and no verification guard | tasks.md completion record |
| AC-004 | T002/T005 | Registration policy/sign-in tests plus aggregate backend/frontend checks; deliberately unverified seed remains denied | tasks.md completion record |
| AC-005 | T001/T002 | DI inspection/tests and retained verification/recovery regression with email unconfigured; no SMTP files/configuration remain | tasks.md completion record |
| AC-006 | T004/T005 | `bash scripts/contract.sh generate`, semantic diff, `bash scripts/contract.sh check`, aggregate checks | tasks.md completion record |
| Readiness | T006 | `python3 automation/context.py check M034` fresh at close | tasks.md completion record |

## Context boundaries and risks

Omitted domains and why: language-operation execution and accounting are unchanged except for consuming the existing `VerifiedAccount` policy; LLM evaluation, performance and provider selection are unrelated; deployment/backup lifecycle gains no migration or configuration; browser/AT matrices beyond the changed registration state remain owned by later aggregate gates. Open on demand: M007/M010 specs when diagnosing retained endpoint regressions, and M011/M013 specs for registration/sign-in behavior already reused rather than redesigned.

Dependencies: M006 account persistence/policy, M011 registration UI and M013 sign-in are Done according to delivery/current.md. There is no external or human blocker. Primary risks are accidentally verifying an existing account through the duplicate path, leaving a pending-verification UI redirect, or deleting dormant security behavior that DF-008 will reactivate; dedicated tests and a narrow diff cover each risk.

Optional human walkthrough after automation: launch the app with the ordinary local scripts, register a fresh address, select `Go to sign in`, sign in with that address/password, and open Translation or Rewriting. Expected: no email is sent or requested, no database edit is needed, registration itself does not sign in, and the explicit sign-in grants normal verified access.
