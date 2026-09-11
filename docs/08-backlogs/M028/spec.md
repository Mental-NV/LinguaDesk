# M028 — Selected specification
Selected items: BI-028. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: the first real language workspace at `/translate`, replacing the
M013 signed-in placeholder. Verified session only; unverified/signed-out
routing, sign-in form and session teardown stay as M013 built them. The page
offers source selector (Detect automatically default + manual override),
target selector (Choose target language default; four PRD languages; known
source disabled as target), source editor, explicit **Translate** control,
result view (editable/copyable), inline usage display and the normative
message set. Submission goes through the M026 translation operation surface
with cookie auth (verified-account policy) + antiforgery via the generated
client; deterministic fake provider only. Requirements FR-003–011, FR-023,
FR-024, FR-028; NFR-004 (privacy portion); UX §7 translation contract;
UX-AC-021/022/024/030/033/036/037/065; message IDs UX-MSG-003–008,
UX-MSG-013–015, UX-MSG-020–022, UX-MSG-026/027, UX-MSG-045–047 (others only
as already rendered by the shell); fixtures/identity from #6 §4.1 and
§4.5 E2E contract.

Explicit exclusions: rewriting workspace and modes (M027/M029); combined
translation+rewriting operation (none exists); prefix translation (DF-006);
pause-based submission (DF-003); sentence alternatives/comparison
(DF-001/DF-002, cut metadata); live provider dispatch, quality,
performance and serving/billing attribution (Q-001 remainder, Q-005);
monetary-cap amount (unset); account deletion/backup lifecycle (Q-004);
abuse limits (Q-008/P-005); compatibility promises (P-006); email delivery
(M034); full browser/device/AT claim (M038/M039, G3); competing-event,
unknown-outcome, reset and teardown journeys (M030–M032). No backend
contract change is selected: the M026 wire shape is reused, and any
unexpected handler need reopens the contract-review gate before client
adoption.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | Predefined verified user opens `/translate`, chooses a valid source/target, enters T-OK-A, waits without causing a request, activates **Translate** once, receives the one complete deterministic result, and sees authoritative usage 7,546 (seed 7,500 + 46); result is editable and copyable | FR-003/009/010/011/023/024/028; UX §7; UX-AC-022/065; #6 §4.5 |
| AC-002 | Typing, pasting, language changes, elapsed time and IME composition never submit; duplicate activation while one operation is unsettled is suppressed; source/target edits preserve the previous result, mark it outdated (UX-MSG-047), and submit nothing until **Translate**; activation captures final values | FR-011; UX-AC-021/022/024/037; UX-MSG-004/005/046/047 |
| AC-003 | Empty/whitespace-only source starts no operation and incurs no charge; T-LONG (5,312 chars) shows UX-MSG-045 with excess 312, retains all text, disables **Translate**, dispatches nothing and charges zero; same-language target retains the invalid value with UX-MSG-007 until corrected | FR-004/007; UX §7; UX-AC-021/030; UX-MSG-007/045 |
| AC-004 | Manual result edit changes no source, starts no request and incurs no charge; copy returns the exact current edited value (UX-MSG-026); a pending response cannot overwrite a manually edited result (UX-MSG-022); a later explicit submission may replace preexisting edits | FR-023; UX-AC-033/037; UX-MSG-022/026/027 |
| AC-005 | Controlled definitive failure preserves source and prior result, shows UX-MSG-013 with **Try again**, adds zero usage; explicit retry captures current fields once with a new operation key; allowance-exhausted and unavailable paths show UX-MSG-015/016/017 with no successful-operation charge | FR-011/024; UX-AC-036; UX-MSG-013/014/015/016/017 |
| AC-006 | At least one published E2E case signs in through the real `/login` form; other cases reuse the documented real-auth fixture (same predefined account, same-origin cookie state; no forged sessions, test login routes, disabled middleware or intercepted `/api`); every case asserts its browser-visible outcome through semantic locators; seeded Smoke-only account/database, run-owned and removed afterward; password value absent from all artifacts | #6 §4.5; V-012; fixtures §4.1 |
| AC-007 | No source/result text, credentials, global counts, monetary values or provider internals in problems, logs, traces or TEXT columns; inline display shows only the user's own usage/remaining/reset; no global consumption or provider/routing controls | NFR-004; UX-AC-065; V-015 |
| AC-008 | Translation submit/status flows through the reviewed M026 wire shape via regenerated client types with zero OpenAPI/TypeScript drift; cookie-auth verified-account + antiforgery path proven against the published host | V-009; arch §8.3 |

## Constraints and decisions

- One unsettled operation per page; request/workspace revisions guard stale application; reducers own guards, effects own transport (arch §8).
- Shared `unicode-scalar-v1` counting fixtures in C#/TypeScript; both sides reject over-limit full text consistently (V-001; client uses existing `inputPolicy.ts`, no new counting rule).
- Deterministic provider text proves transport/presentation only; language quality is not inferred (V-013 out of scope).
- Clarifications: Q-003/Q-006 shared counting/retry/identity/usage design already specified and adopted via M026/M013; Q-001 (live serving/cap amount), Q-004 (lifecycle), Q-005 (qualification), Q-008/Q-010 (safeguards/adoption) remain pending and do not block this deterministic slice. No new open question is raised.
- Omitted domains: rewriting/assistance (no code path selected), email (no delivery in slice), backend accounting/recovery (reused, regression-covered by M026 suites), visual baselines (curated set unchanged; M038+ owns web visual claim).
