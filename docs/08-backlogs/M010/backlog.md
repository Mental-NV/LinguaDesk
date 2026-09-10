# M010 — Recover API account access

Status: selected
Milestone: [M010 — Recover API account access](../../07-roadmap.md#42-local-access)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-010 | A local user can request and complete password reset through the API; invalid tokens, non-enumeration and subsequent access invalidation are verified. | Selected local-access increment | M009 done; no readiness blocker | selected | [spec](spec.md) |

Acceptance summary: forgot-password acknowledgments are indistinguishable for known/unknown accounts; a valid reset sets a new M006-policy password without signing the user in; consumed/invalid/expired reset material fails generically; password change rotates the security stamp so prior cookie/Bearer [REDACTED] fail on next use. Detailed ACs are in [spec](spec.md). Implementation is complete; AC-001–006 are behaviorally evidenced via socket-free substitute execution (146/146) and AC-007 partially (contract, frontend, compilation, host-free suites). Focused-harness, aggregate backend/smoke, published smoke, and dependency audits remain for the closing runner (see [tasks](tasks.md#completion-record)); AC-001–007 passed is not yet claimed.

Human steps: None required for implementation or deterministic verification. The harness uses owned loopback HTTPS, isolated database/key paths, fake time and capturing/failing delivery adapters. M014 owns browser reset forms; M034 owns live email delivery.
