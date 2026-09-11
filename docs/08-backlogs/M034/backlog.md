# M034 — Deliver real local-account email
Status: selected
Milestone: [M034 — Deliver real local-account email](../../07-roadmap.md#45-useful-end-to-end-product-increments)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-034 | The chosen email adapter sends real verification and password-reset mail that conforms to the verified account journeys, with bounded live-smoke delivery evidence | Must | M007, M010 (both Done, evidence in delivery/current.md); live mailbox/provider access required — missing access blocks completion | selected | [spec](spec.md) |

Acceptance summary: a configured real adapter delivers confirmation mail (registration/resend) and reset mail (forgot-password) to a live test mailbox; the delivered user ID/code material completes the unchanged M007 confirmation and M010 reset endpoints; production default still sends nothing; no credential, token, code or address leaks into logs, traces or evidence; generated contracts show zero drift. Detailed ACs are in spec.md.
Human steps: operator (at start, blocks T004/T005): supply the chosen provider's SMTP credentials via named environment variables, one test recipient mailbox, and approval for the bounded live sends; operator (at smoke, blocks completion): confirm receipt of the bounded messages. Missing access blocks completion per the roadmap exit criterion.
