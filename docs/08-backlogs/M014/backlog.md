# M014 — Recover access in the web app

Status: selected
Milestone: [M014 — Recover access in the web app](../../07-roadmap.md#42-local-access)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-014 | A user can complete the forgot/reset-password forms and explicitly return to sign-in; failures and focus are understandable. | Selected web-access increment | M013 done; M010 done; no blocker | selected | [spec](spec.md) |

Acceptance summary: `/forgot-password` accepts an email and shows the identical non-enumerating confirmation with a Back to sign in link; invalid email sends no request with a linked field error and focus; `/reset-password` consumes a `userId`/`code` link, enforces the current password policy with confirmation, shows reset success with an explicit Go to sign in (never auto-login), and shows a recovery action with no password fields for invalid/expired links; stale auth completions cannot overtake newer routes and no credential material reaches storage, history, logs or reports. Detailed ACs are in [spec](spec.md).

Human steps: None required.
