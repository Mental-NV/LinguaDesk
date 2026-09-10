# M009 — Independent client access

Status: selected
Milestone: [M009 — Independent client access](../../07-roadmap.md#42-local-access)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-009 | A verified API consumer can sign in for opaque bearer access/refresh credentials and use the same authenticated API as the browser session, with credential precedence and security-stamp revocation following the shared contract. | Selected local-access increment | M008 done; no readiness blocker | selected | [spec](spec.md) |

Acceptance summary: bearer sign-in issues a 15-minute access / 7-day refresh pair only on valid credentials; refresh replaces the pair without replaying language work; bearer and cookie credentials never mix or fall back; expiry, stamp change and deletion invalidate subsequent access and refresh; generated contract and secret-safe boundaries match the cookie slice. Detailed ACs are in [spec](spec.md).

Human steps: None required for implementation or deterministic verification. The harness uses owned loopback HTTPS, isolated database/key paths and fake time. M010 owns password reset; M013 owns browser UX; M026+ own language operations; M034 owns live email.
