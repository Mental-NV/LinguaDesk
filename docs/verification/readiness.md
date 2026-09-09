# Verification readiness and dependencies

Authoritative continuation of [06-verification-plan.md](../06-verification-plan.md); section numbers refer to that document; read only when relevant to the selected scope.

## 9. Readiness and remaining dependencies

Q-005 is **specified at design level** by Sections 5–6. The corpus, implementation, reviewers and measurements are still pending; specifying counts is not qualifying a candidate. M001–M006's scoped evidence is complete, but it does not qualify language behavior or any release gate. M005 passes only the V-001/V-009/V-012 portions listed above; M006 passes only the V-004/V-009/V-015/V-016 and retained regression portions allocated above. Later work continues through #0's roadmap/backlog/delivery-package process.

| Dependency | Required before claiming readiness |
| --- | --- |
| Selected implementation and #10 commands | Executable suites, actual packages/test names, deterministic fixtures, report locations and CI gates |
| Q-001/Q-007 | Chosen serving/billing arrangement, credentials, verified adapter/settings/context bounds, qualified primary/optional fallback and an actual monetary ceiling before paid serving |
| Q-003/Q-006 | M005 resolves public capability/count/artifact mechanics; M006 resolves registration DTO/status/error fields, email/password/duplicate/delivery-intent behavior and unverified current-state guard for its package. Confirmation/sign-in/token/antiforgery/usage/language/status details remain due before their handlers/clients; generated review precedes dependent client adoption |
| Q-004 | M006 explicitly creates durable account/key records without deletion/retention claims. Remaining deletion, backup/aggregate/unresolved-exposure retention and reconciliation rules block their related features/launch, not selected registration; prove later lifecycle/restart/restore against them |
| Quality and performance evidence | Frozen approved corpus, qualified reviewers/judge configuration, explicit run budgets and actual candidate/chain/API reports |
| Browser/AT and operations | Access to supported current/previous actual versions/devices/AT; real email and published-host/restore evidence |
| Q-008/Q-010 / P-005/P-006 | Product-owner disposition of proposed safeguards/compatibility before making them binding; RG-008 reviews current launch dependencies only |
