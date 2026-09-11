# M034 — Selected specification
Selected items: BI-034. Status: ready; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

M034 selects the live-email portion of FR-002, the remaining delivery detail
of Q-006 for the account slice, and the V-004/V-016 email evidence. M007 is
complete and supplies the confirmation/resend contracts (strict anonymous
boundary, base64url wire codes, durable 60-second delivery cooldown,
`UnavailableAccountConfirmationSender` production default); M010 is complete
and supplies the forgot/reset contracts (60-minute reset lifetime, dedicated
reset marker, stamp rotation, `UnavailableAccountPasswordResetSender`
production default); M012/M014 own the web consumption of delivered links.
Coverage records live email as the unselected remainder of FR-002; this
package selects exactly that remainder.

The milestone adds one real email adapter behind the two existing sender
interfaces (`IAccountConfirmationSender`, `IAccountPasswordResetSender`),
chosen and configured at execution, plus plain-text confirmation and reset
message templates that assemble links against one configured public origin.
It changes no endpoint, wire shape, token lifetime, cooldown, error category
or UI route. Registration, resend and forgot-password keep their generic
acknowledgments and delivery-failure semantics; the adapter is invoked only
after the existing cooldown-marker gate, and any send failure retains the
current safe result (generic 202, account state unchanged).

Explicit exclusions: new API operations or schema changes (generated
contracts must show zero drift); token/cooldown policy changes (M007/M010
fixed); web form/route work (M011–M014 own those); account deletion, backup
or retention claims (Q-004 stays open); abuse controls (P-005) and
compatibility guarantees (P-006); bulk, marketing or notification mail;
production serving or qualification claims (G1/G2/G4); any second provider,
failover chain or priority engine (one adapter only).

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | Through the real host with the configured adapter, a resend request for a known unverified account delivers one confirmation message to the live test mailbox; submitting its delivered user ID/code to `confirmLocalAccountEmail` returns 200, durably confirms the account, and issues no credential or language work. | FR-002; API §3.3; V-004/V-016 |
| AC-002 | Through the same host, a forgot-password request delivers one reset message; submitting its delivered user ID/code with an M006-policy password returns 200, rotates the security stamp, invalidates pre-reset sessions on next use, and leaves verification status unchanged. | FR-002; API §3.3; V-004/V-016 |
| AC-003 | Without email configuration the production default is unchanged: both senders remain unavailable, registration/resend/forgot keep their generic accepted acknowledgments with no delivery, and the deterministic capturing/failing test adapters remain impossible to select through production configuration. | Architecture §5.2; M007/M010 dependency |
| AC-004 | No credential, user ID, confirmation/reset code or token, email address, query string, cookie, request body or raw provider error enters responses, logs, traces, evidence records, test reports or committed fixtures; messages carry link material only in the recipient body; the smoke evidence records only categories, redacted provider metadata and endpoint outcomes. | Architecture §5.2; V-015 |
| AC-005 | Delivered links use the single configured public origin and remain consumable by the unchanged M012 verification and M014 recovery routes (query material posted once as JSON, never mutated by GET); regenerated OpenAPI/TypeScript show zero drift and generation performs no send. | API §3.3; M012/M014 dependency; V-009 |
| AC-006 | Existing V-004/V-009/V-015 account suites, aggregate backend/contract/frontend checks and the published smoke pass unchanged; M007/M010/M012/M014 behavior retains its prior results. | V-004/V-009/V-015/V-016; #6 §7.1 |

## Constraints and decisions

Provider credentials travel only through explicitly named environment
variables on the backend host, never committed files, evidence or logs; the
adapter is selected only when that configuration is present. Link tokens stay
out of URLs beyond the recipient's own message, out of server logs (existing
redaction stays), and out of every evidence artifact. The smoke uses a
dedicated test mailbox and test accounts only, with a bounded send count
fixed in plan.md; it never targets real users.

Q-006 is resolved for the account slice by this adapter selection, the
message/origin shape and the live-smoke procedure. Q-004 does not block this
slice: no new stored record, deletion, backup or retention claim is
introduced. P-005 and P-006 remain proposed.

Human gates: operator supplies provider credentials, test mailbox and bounded
send approval before T004, and confirms live receipt before completion. If
that access is missing, execution stops and records the blocker without
claiming any gate passed; the roadmap exit criterion makes missing access a
completion blocker.
