# M008 — Browser session access

Status: done
Milestone: [M008 — Browser session access](../../07-roadmap.md#42-local-access)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-008 | A local account can establish, inspect and end a secure browser cookie session through a generated API contract, with antiforgery on cookie mutations and immediate rejection after session invalidation. | Selected local-access increment | M007 done; no readiness blocker | done | [spec](spec.md) |

Acceptance summary: the anonymous antiforgery bootstrap supports strict cookie sign-in; valid credentials expose only verified/unverified session status, invalid credentials do not enumerate accounts, current session reads use the real cookie handler, sign-out is idempotent and clears the cookie, and expiry/security-stamp changes invalidate the next protected access. Detailed ACs are in [spec.md](spec.md).

Human steps: None required for implementation or deterministic verification. The harness may create an owned loopback HTTPS certificate and isolated database/key paths. M013 owns browser login/sign-out/expiry UX and its manual accessibility/privacy evidence; M009 owns bearer/refresh access; M010 owns password reset; M034 owns live email.
