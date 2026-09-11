# M034 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Wire one real SMTP email adapter behind the existing
`IAccountConfirmationSender` / `IAccountPasswordResetSender` seams so the
unchanged M007/M010 journeys deliver real mail: env-configured credentials
select the adapter (absent config keeps the `Unavailable*` defaults),
plain-text templates assemble origin-bound links from the delivery records,
and a bounded live smoke through the real host proves one confirmation and
one reset end to end with redacted evidence. No endpoint, contract, policy
or UI change is intended; any behavioral mismatch found becomes a reported
finding, not a silent contract edit.

Selected canonical excerpts arrive through the context packet. Do not copy
them all here.

## Changes and order

1. Adapter: new backend email sender (SMTP client, one provider) implementing
   both sender interfaces; DI selects it only when the named email
   environment variables are present, otherwise the existing `Unavailable*`
   registrations stay. Capturing/failing test doubles stay test-only and
   unreachable from production configuration.
2. Messages and origin: minimal plain-text confirmation and reset templates
   plus one configured public origin used only for link assembly; links carry
   the existing `userId`/`code` wire values for the M012/M014 routes. No new
   token, lifetime, cooldown or category.
3. Configuration: named env vars for SMTP host/port/credentials/sender and
   public origin, validated at startup only when email is enabled; invalid
   email config fails that host start loudly, never silently falls back to
   fake delivery. No secret is committed, logged or written to evidence.
4. Live smoke (bounded, operator-gated): against a real host with the
   configured adapter, resend to the test mailbox → confirm via delivered
   material (AC-001); forgot-password to the test mailbox → reset via
   delivered material (AC-002). Bounded to two delivered messages plus
   operator-confirmed receipt; send failures keep generic acknowledgments.
5. Order: adapter + templates → config/selection wiring → deterministic
   regression (adapters unselected) → operator-gated live smoke → contract
   regenerate/check + evidence redaction review → closeout.
6. No migration, no rollout beyond host configuration, no production serving
   claim. Rollback is the prior commit plus removing the email configuration.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001 | T004 | V-004/V-016: live resend → delivered confirmation → real `confirmLocalAccountEmail` 200 with `EmailConfirmed = true`; operator confirms receipt | tasks.md completion record |
| AC-002 | T004 | V-004/V-016: live forgot → delivered reset → real `resetLocalAccountPassword` 200, old credential rejected, new credential accepted | tasks.md completion record |
| AC-003 | T002 | V-004: unconfigured host keeps unavailable-sender behavior and generic acknowledgments; prod config cannot select test doubles | tasks.md completion record |
| AC-004 | T005 | V-015: redaction inspection of logs, traces, evidence and fixtures for credentials/tokens/codes/addresses | tasks.md completion record |
| AC-005 | T005 | V-009: `bash scripts/contract.sh check` clean (zero drift); delivered links consumed by unchanged M012/M014 paths | tasks.md completion record |
| AC-006 | T003/T005 | `bash scripts/backend.sh check`, `bash scripts/backend.sh smoke`, `bash scripts/frontend.sh smoke` stay green | tasks.md completion record |
| M007/M010 reuse | T001 | Dependency evidence confirmed from delivery/current.md; no contract/policy re-proof | tasks.md completion record |
| Readiness | T006 | `python3 automation/context.py check M034` fresh at close | tasks.md completion record |

## Context boundaries and risks

Omitted domains and why: language operations, allowances, accounting and
cost bounds (no character charge or monetary admission attaches to email;
M021–M025 reused, not re-proven); translation/rewriting UI journeys
(M028–M032 own that evidence); LLM evaluation corpora and qualification
(Q-005/G1); performance workloads (G2/M037); device/AT/browser matrix
(G3/M038/M039); backup/restore lifecycle (M040/M041, Q-004). Open on demand:
M007/M010/M012/M014 specs for journey semantics the adapter touches; ADR
index only if the adapter choice changes a recorded decision.

Dependencies, assumptions, blockers: M007/M010 Done per delivery/current.md
— satisfied. Assumption: one SMTP-capable provider and one test mailbox
supplied by the operator. Blocker (known, execution-level): missing
provider/mailbox access stops T004/T005 and blocks completion; record it,
claim no gate. Human steps: operator supplies credentials + test mailbox +
bounded-send approval before T004 (blocked gate: live smoke), and confirms
receipt before closeout; owner: milestone operator; timing: start and smoke.

Risks: credentials leaking into evidence (named env vars only, redaction
review in T005); smoke asserting only its own sends as proof of journeys
(deterministic V-004 suites, not the smoke, own contract/policy evidence);
link format drifting from M012/M014 consumption (AC-005 compatibility check);
unbounded live sends (fixed bound of two delivered messages).
