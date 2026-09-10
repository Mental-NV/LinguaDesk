# M013 — Sign in and leave safely

Status: selected; detailed scenarios live only in [spec](spec.md)
Milestone: [M013 — Sign in and leave safely](../../07-roadmap.md#42-local-access)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-013 | A user can sign in, follow a safe return route, sign out and recover from expiry without restoring private workspace text. | Selected web-access increment | M012 done; M008 done; no blocker | selected | [spec](spec.md) |

Acceptance summary: the `/login` route exposes the local sign-in form wired to the completed M008 session operations with single-submission guards; valid verified credentials reach the remembered safe protected route with heading focus; invalid credentials show MSG-028 with cleared passwords and email focus; unverified credentials continue to `/verify-email`; session expiry clears in-memory auth state before login renders with MSG-037; inline sign out follows the §3.4 pending/failure/success states with auth-only retry; stale completions cannot redirect or restore cleared state; safe return admits only internal protected paths; no password or secret reaches storage, history, logs or reports. Detailed ACs are in [spec](spec.md).

Human steps: None required for implementation or deterministic verification. The harness uses Testing Library components, the published loopback host with isolated database/keys and synthetic `example.test` addresses. Branded-browser, physical-device and assistive-technology evidence remains release scope per verification §4.2–4.3; live email and recovery forms belong to M034/M014.
