# M034 — Selected specification
Selected items: BI-034. Status: ready; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

M034 implements D-20's temporary MVP substitute for the deferred real-account-email work in FR-002 and Q-006. A newly created local account is immediately and durably stored with `EmailConfirmed = true`. Registration sends no confirmation message, writes no delivery-cooldown marker, issues no cookie or bearer credential, and returns a generic `202 {"status":"signInRequired"}` acknowledgment. The user then signs in through the existing M013 journey.

Duplicate privacy remains unchanged. A syntactically valid registration for an existing normalized email returns the same accepted body and metadata, creates no account, and does not modify the existing row. In particular, duplicate registration must never turn a pre-existing unverified account into a verified account.

The M007 confirmation/resend and M010 forgot/reset API contracts, token providers, cooldown behavior and security-stamp behavior remain in the codebase for legacy/test compatibility, but their production sender implementations remain unavailable. M012/M014 routes may remain implemented but are not linked from the active registration journey. DF-008 owns selecting a mail transport, restoring unverified-on-registration and real confirmation/reset delivery, and proving both journeys through a live mailbox.

Explicit exclusions: SMTP or another email adapter; mail credentials/configuration/templates; live mailbox evidence; automatic verification or password reset of existing accounts; database backfill or migration; removal/redesign of retained M007/M010/M012/M014 behavior; registration-issued authentication; Google sign-in; account deletion/retention policy; abuse controls or compatibility guarantees.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | A valid registration for a new normalized email creates exactly one durable Identity account with `EmailConfirmed = true`, a password hash rather than plaintext, and no verification delivery/cooldown marker. It returns `202` with only `status = signInRequired`, `Cache-Control: no-store`, and no cookie, bearer/refresh credential, redirect, address, account ID or confirmation material. | FR-001/002; API §3.4; D-20; V-004/V-015 |
| AC-002 | Sequential, case-equivalent and concurrent valid duplicates receive an acknowledgment indistinguishable from a new registration and do not create or mutate an account. A duplicate request for a deliberately seeded unverified account leaves it unverified. Validation and malformed-request behavior remain bounded and non-enumerating. | API §3.4; Architecture §5.2; V-004/V-015 |
| AC-003 | The registration UI replaces the submitted form with `Account registration accepted. Sign in to continue.`, reveals neither the email nor password, stays on `/register`, focuses the status, and offers `Go to sign in`. It sets no pending-verification guard; protected navigation follows the ordinary sign-in path. | UX §9; UX-MSG-034; V-003/V-012 |
| AC-004 | The new account can sign in through the existing browser and bearer paths and satisfies `VerifiedAccount`; a deliberately seeded legacy unverified account is still denied by that policy. Registration itself never authenticates the caller or invokes language work. | FR-001/002; API §3.2/3.4; V-004/V-009 |
| AC-005 | Both production account-email sender interfaces remain bound to their unavailable implementations. Registration does not invoke a sender. Retained resend/forgot operations preserve their existing generic acknowledgments and dormant failure behavior; no SMTP selection, secret, external dependency or live send is introduced. | DF-008; Architecture §5.2; V-004/V-015 |
| AC-006 | The regenerated OpenAPI and TypeScript artifacts intentionally change registration acceptance from `verificationRequired` to `signInRequired` and advance the pre-release revision to M034, with no unrelated wire drift. Focused and aggregate backend, contract and frontend checks pass. | Q-006; V-003/V-009/V-012/V-015 |

## Constraints and decisions

Automatic verification applies only at successful creation time. It is not proof that the caller controls the supplied address, so product and operational documentation must state that limitation and point to DF-008. Existing rows are not rewritten. No new persistence shape is needed because the standard Identity confirmation flag already exists.

Privacy takes precedence over convenience on duplicates: the response cannot disclose whether an email exists, and a duplicate cannot be used as a verification side effect. The success UI therefore also avoids echoing the submitted address.

Human gates: none. A human can optionally run the published app and verify registration → explicit sign-in → one authenticated page, but automation owns completion and no external email account is required.
