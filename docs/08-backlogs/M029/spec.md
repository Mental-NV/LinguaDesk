# M029 — Selected specification
Selected items: BI-029. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: the real language workspace at `/rewrite`, replacing the
M013 `SignedInPlaceholder` kept for that route. Verified session only;
unverified/signed-out routing, sign-in form and session teardown stay as
M013/M028 built them. The page offers writing-language display (Detect
automatically + manual hint override per the M027 wire), one native
Writing mode dropdown (Correction only default; the eight styles/tones
from the M027 mode catalog; exactly one selected; no toggle, None set
value or Show changes preference), source editor, explicit **Rewrite**
control, result view (editable/copyable plain text), inline usage
display and the normative message set. Submission goes through the M027
rewriting operation surface with cookie auth (verified-account policy)
+ antiforgery via the generated client; deterministic fake provider
only. Requirements FR-004/005 (eligibility terminal mapping, composing
portions), FR-012–014, FR-016–018, FR-022–024, FR-028; NFR-004 (privacy
portion); UX §8 rewriting contract; UX-AC-038/039/040/043/044/045/046/
047/051/065; message IDs UX-MSG-003–006, UX-MSG-008, UX-MSG-011–017,
UX-MSG-020–022, UX-MSG-026/027, UX-MSG-046/047 (others only as already
rendered by the shell); fixtures/identity from #6 §4.1 and §4.5 E2E
contract.

Explicit exclusions: translation workspace and target selection (M028
owns that path; no combined operation exists); prefix rewriting
(DF-006); pause-based submission (DF-003); sentence alternatives,
comparison, change highlighting (DF-001/DF-002, cut metadata);
live provider dispatch, quality, performance and serving/billing
attribution (Q-001 remainder, Q-005); monetary-cap amount (unset);
account deletion/backup lifecycle (Q-004); abuse limits (Q-008/P-005);
compatibility promises (P-006); FR-015 (retired, traceability only);
email delivery (M034); full browser/device/AT claim (M038/M039, G3);
competing-event, unknown-outcome, reset and teardown journeys
(M030–M032). No backend contract change is selected: the M027 wire
shape is reused, and any unexpected handler need reopens the
contract-review gate before client adoption.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | Predefined verified user opens `/rewrite`, sees Correction only by default (or chooses one supported mode), enters W-OK, waits without causing a request, activates **Rewrite** once, receives the one complete deterministic same-language result, source unchanged, and sees authoritative usage 7,546 (seed 7,500 + 46); result is editable and copyable | FR-012/013/014/016/024/028; UX §8; UX-AC-038/039/065; #6 §4.5 |
| AC-002 | Typing, pasting, mode/language changes, elapsed time and IME composition never submit; duplicate activation while one operation is unsettled is suppressed; source/mode edits preserve the previous result, mark it outdated (UX-MSG-047) or updating while pending (UX-MSG-004), and submit nothing until **Rewrite**; activation captures final values | FR-014/016/018/022; UX-AC-039/040/043/044/046; UX-MSG-004/046/047 |
| AC-003 | Empty/whitespace-only source starts no operation and incurs no charge; W-LONG (2,312 chars) shows UX-MSG-011 with excess 312, retains all text, disables **Rewrite**, dispatches nothing and charges zero; invalid/uncertain input rejected server-side preserves fields per UX-MSG-006/008 with no charge | FR-004/005; UX §8; UX-AC-045; UX-MSG-006/008/011 |
| AC-004 | Manual result edit changes no source, starts no request and incurs no charge; copy returns the exact current edited value (UX-MSG-026); a pending response cannot overwrite a manually edited result (UX-MSG-022); a later explicit submission may replace preexisting edits; stale non-edited responses are fenced (UX-MSG-020 → UX-MSG-021 charge notice) | FR-018/022/023; UX-AC-044/047/051; UX-MSG-020/021/022/026/027 |
| AC-005 | Controlled definitive failure preserves source and prior result, shows UX-MSG-013 with **Try again**, adds zero usage; explicit retry captures current fields once with a new operation key; allowance-exhausted and unavailable paths show UX-MSG-015/016/017 with no successful-operation charge | FR-024; UX-MSG-013/014/015/016/017 |
| AC-006 | At least one published E2E case signs in through the real `/login` form; other cases reuse the documented real-auth fixture (same predefined account, same-origin cookie state; no forged sessions, test login routes, disabled middleware or intercepted `/api`); every case asserts its browser-visible outcome through semantic locators; seeded Smoke-only account/database, run-owned and removed afterward; password value absent from all artifacts | #6 §4.5; V-012; fixtures §4.1 |
| AC-007 | No source/result text, credentials, global counts, monetary values or provider internals in problems, logs, traces or TEXT columns; inline display shows only the user's own usage/remaining/reset; no global consumption or provider/routing controls | NFR-004; UX-AC-065; V-015 |
| AC-008 | Rewriting submit/status flows through the reviewed M027 wire shape via regenerated client types with zero OpenAPI/TypeScript drift; cookie-auth verified-account + antiforgery path proven against the published host | V-009; arch §8.3 |

## Constraints and decisions

- One unsettled operation per page; request/workspace revisions guard stale application; reducers own guards, effects own transport (arch §8).
- Shared `unicode-scalar-v1` counting fixtures in C#/TypeScript; both sides reject over-limit full text consistently (V-001; client uses existing `inputPolicy.ts`, no new counting rule). Rewriting limit is 2,000 scalars (UX-MSG-011); the 5,000 translation limit does not apply here.
- Deterministic provider text proves transport/presentation only; language quality is not inferred (V-013 out of scope).
- Clarifications: Q-003/Q-006 shared counting/retry/identity/usage design already specified and adopted via M026/M027/M013; Q-001 (live serving/cap amount), Q-004 (lifecycle), Q-005 (qualification), Q-008/Q-010 (safeguards/adoption) remain pending and do not block this deterministic slice. No new open question is raised.
- Omitted domains: translation/target selection (no code path selected), email (no delivery in slice), backend accounting/recovery (reused, regression-covered by M027 suites), visual baselines (curated set unchanged; M038+ owns web visual claim).
