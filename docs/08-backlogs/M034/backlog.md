# M034 — Unblock local registration without email
Status: implemented
Milestone: [M034 — Unblock local registration without email](../../07-roadmap.md#45-useful-end-to-end-product-increments)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-034 | A user can register without SMTP access: every newly created account is durably verified without delivery, receives no registration credential, and is directed to explicit sign-in; real account email is preserved as DF-008 | Must | M006, M011, M013 (Done); no external service or human access blocker | implemented | [spec](spec.md) |

Acceptance summary: a valid new registration creates exactly one verified account, sends no email and returns only the generic `signInRequired` acknowledgment; valid duplicate registrations return the same response without changing the existing account, including an existing unverified account; the web success state remains on registration and offers `Go to sign in`; sign-in and verified-account authorization then work normally. The production confirmation/reset senders remain unavailable, their existing endpoints remain dormant, and real verification/recovery delivery is deferred under DF-008. Detailed ACs are in spec.md.

Human steps: none required to complete M034. After automated checks, a human may optionally launch the app and perform the short registration → sign-in → authenticated-page walkthrough in plan.md; no mailbox, SMTP credentials or database edit is needed.
