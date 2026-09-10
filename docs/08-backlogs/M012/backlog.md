# M012 — Verify email in the web app

Status: selected
Milestone: [M012 — Verify email in the web app](../../07-roadmap.md#42-local-access)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-012 | An unverified user can understand verification status, resend and explicitly continue after verification using the specified link variants. | Selected web-access increment | M011 done; M007 done; no blocker | selected | [spec](spec.md) |

Acceptance summary: the `/verify-email` route exposes verification status with the in-memory email, one-shot resend bound to the server-provided cooldown, explicit link-variant consumption that posts delivered material once and clears it from the URL, a read-only status check and explicit safe continuation; invalid/expired material shows MSG-032 with a resend path; unverified protected navigation redirects without language work. Detailed ACs are in [spec](spec.md).

Human steps: None required for implementation or deterministic verification. The harness uses Testing Library components, the published loopback host with isolated database/key paths and synthetic `example.test` addresses. Branded-browser, physical-device and assistive-technology evidence remains release scope per verification §4.2–4.3; M034 owns live email delivery/templates/origin.
