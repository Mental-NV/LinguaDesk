# M034 — Tasks and evidence
Inputs: [spec](spec.md), [plan](plan.md).

## Resume
Next: T001. Blockers: operator email access (provider credentials + test mailbox + bounded-send approval) required before T004; missing access blocks completion.
Last check: not run. Changed scope: none.

## Ordered tasks
- [ ] T001 — Confirm M007/M010 dependency evidence and current sender seams (`IAccountConfirmationSender`, `IAccountPasswordResetSender`, `Unavailable*` defaults, coordinator cooldown gates); no AC; depends on none; done when seams and wire surface are identified with no behavior change.
- [ ] T002 — Implement the real SMTP adapter, message templates, public-origin link assembly and env-gated DI selection; AC-003/AC-004/AC-005; depends on T001; done when the unconfigured default still sends nothing and test doubles stay unreachable from production config.
- [ ] T003 — Run deterministic regression (`bash scripts/backend.sh check`, `bash scripts/contract.sh check`) with email unconfigured; AC-003/AC-006; depends on T002; done when suites pass and contracts show zero drift.
- [ ] T004 — Bounded operator-gated live smoke: resend → delivered confirmation → 200 verify (AC-001); forgot → delivered reset → 200 reset with stamp rotation (AC-002); depends on T003 and operator access; done when both delivered messages complete their journeys with operator-confirmed receipt.
- [ ] T005 — Redaction review (AC-004), delivered-link compatibility with M012/M014 paths plus `contract.sh check` (AC-005), full regression and published smoke (AC-006); depends on T004; done when all AC checks pass with redacted evidence.
- [ ] T006 — Refresh `python3 automation/context.py check M034`, write the completion record; all ACs; depends on T005; done when the manifest is fresh and evidence is recorded.

## Completion record
Pending execution. No production code/tests written during planning; no commit made here (runner owns commits).
