# M007 — Verify a local account

Status: done
Milestone: [M007 — Verify a local account](../../07-roadmap.md#42-local-access)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-007 | An unverified local account can confirm its email through deterministic delivery material, or request another link after a failed, invalid, or expired one, without account enumeration or authentication side effects. | Selected local-access increment | M006 done; no completion blocker | done | [spec](spec.md) |

Acceptance summary: a captured registration or resend delivery can complete durable verification; invalid/expired material has a generic recoverable result; resend is cooldown-bound and intentionally indistinguishable for known, unknown, and already verified accounts; detailed ACs are in [spec.md](spec.md).

Human steps: None required for implementation or deterministic verification. M012 owns browser link consumption and human UX evidence; M034 owns a real email adapter, mailbox delivery, and live click evidence. Those gates remain pending but do not block this API milestone.
