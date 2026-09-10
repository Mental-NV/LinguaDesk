# M011 — Register in the web app

Status: selected
Milestone: [M011 — Register in the web app](../../07-roadmap.md#42-local-access)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-011 | A visitor can submit the registration form, understand validation and reach the unverified state without exposing passwords. | Selected web-access increment | M002 done; M006 done; no blocker | selected | [spec](spec.md) |

Acceptance summary: valid registration submits once through the generated `registerLocalAccount` client shape and reaches the verification page with MSG-034 and the in-memory email; local validation failures send no request, show linked errors and focus the first invalid field; server/policy/network failures clear passwords, keep only the email in memory and permit one explicit retry; no password ever enters URL, storage, history, logs or reports. Detailed ACs are in [spec](spec.md).

Human steps: None required for implementation or deterministic verification. The harness uses Testing Library components, the published loopback host with isolated database/key paths and synthetic `example.test` addresses. Branded-browser, physical-device and assistive-technology evidence remains release scope per verification §4.2–4.3; M012 owns verification/resend continuation and M034 owns live email delivery.
