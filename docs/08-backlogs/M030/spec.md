# M030 — Selected specification
Selected items: BI-030. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: competing-event behavior across the real `/translate` and
`/rewrite` workspaces built by M028/M029. Cross-page navigation via mode
links and browser Back/Forward preserves each page's in-memory
text/settings/result/scroll and focuses the destination `h1`; navigation
never submits. A pending operation may settle for its matching hidden
feature but never changes the visible feature's focus or text. In-page
source/settings/result edits defeat stale callbacks on both pages per
FR-017/018/022/023; successful discarded work still updates displayed
authoritative usage per FR-026 (stale-success disclosure UX-MSG-020 →
UX-MSG-021; manual-edit protection UX-MSG-022). Verified session only;
routing, sign-in form and session teardown stay as M013/M028 built them.
Deterministic fake provider only. Requirements FR-017/018/022/023/026;
UX §3.2 and §6; UX-AC-022/024/040/043/044/046/047/065; message IDs
UX-MSG-004/020/021/022/026/046/047 (others only as already rendered by
the shell); fixtures/identity from #6 §4.1 and §4.5 E2E contract.

Explicit exclusions: unknown-outcome Check-status and retry-as-replay
rules (M031); reset, session teardown and restoration clearing (M032);
translation/rewriting happy-path behavior itself (M028/M029 own it;
regression-covered here, not re-proven); live provider dispatch,
quality, performance and serving/billing attribution (Q-001 remainder,
Q-005); monetary-cap amount (unset); account deletion/backup lifecycle
(Q-004); abuse limits (Q-008/P-005); compatibility promises (P-006);
email delivery (M034); full browser/device/AT claim (M038/M039, G3).
No backend contract change is selected: the M026/M027 wire shapes are
reused, and any unexpected handler need reopens the contract-review
gate before client adoption.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | Predefined verified user enters text on `/translate`, navigates to `/rewrite` and back via mode links: each page keeps its own source/settings/result text, zero transformation operations are dispatched by navigation, and the destination `h1` receives focus | UX §3.2; FR-022 |
| AC-002 | Submit A on `/translate`, navigate to `/rewrite` before A settles: A's settlement never changes the visible `/rewrite` focus or text; returning to `/translate` shows A's outcome applied to its own page only when its captured revisions still match, with authoritative usage reconciled on the page that owns the operation | UX §3.2; UX §6.1; FR-018/026 |
| AC-003 | On each page: source/settings edit after submission defeats the stale success (returned text not applied; usage updated with UX-MSG-020 → UX-MSG-021 stale-charge notice); manual result edit during pending is never overwritten (UX-MSG-022) while the successful operation still updates usage; a later explicit submission may replace edits present at its start | FR-017/018/022/023/026; UX-AC-024/044/046/047; UX-MSG-020/021/022 |
| AC-004 | Composition, mode/language changes and elapsed time still submit nothing on either page; duplicate activation while one operation is unsettled stays suppressed per page while the other feature can submit independently | UX §6.1; UX-AC-022/040/043 |
| AC-005 | At least one published E2E case signs in through the real `/login` form; other cases reuse the documented real-auth fixture (same predefined account, same-origin cookie state; no forged sessions, test login routes, disabled middleware or intercepted `/api`); every case asserts its browser-visible outcome through semantic locators; seeded Smoke-only account/database, run-owned and removed afterward; password value absent from all artifacts | #6 §4.5; V-012; fixtures §4.1 |
| AC-006 | No source/result text, credentials, global counts, monetary values or provider internals in problems, logs, traces or TEXT columns; inline display shows only the user's own usage/remaining/reset | NFR-004; UX-AC-065; V-015 |
| AC-007 | Translate submit/status and rewrite submit/status flows reuse the reviewed M026/M027 wire shapes via regenerated client types with zero OpenAPI/TypeScript drift | V-009; arch §8.3 |

## Constraints and decisions

- Current code keeps each workspace in per-page `useReducer` state and
  aborts its flight on unmount (`TranslatePage`/`RewritePage`); UX §3.2
  requires per-page preservation across navigation and allows a hidden
  feature's pending operation to settle for its own page. The plan lifts
  each feature's workspace state above the router so navigation preserves
  text/settings/result and in-flight work without leaking across pages.
- One unsettled operation per page; request/workspace revisions guard
  stale application; reducers own guards, effects own transport
  (arch §8). Cross-page settlement updates only its owning page's stored
  state plus authoritative usage; it never focuses or writes the visible
  page.
- Shared `unicode-scalar-v1` counting fixtures in C#/TypeScript; limits
  unchanged (translation 5,000, rewriting 2,000 scalars).
- Deterministic provider text proves transport/presentation only;
  language quality is not inferred (V-013 out of scope).
- Clarifications: Q-003/Q-006 shared design already specified and
  adopted via M026/M027/M013; Q-001 (live serving/cap amount), Q-004
  (lifecycle), Q-005 (qualification), Q-008/Q-010 (safeguards/adoption)
  remain pending and do not block this deterministic slice. No new open
  question is raised.
- Omitted domains: unknown-outcome recovery (no status-read path
  selected), reset/teardown (no clearing behavior selected), backend
  accounting/recovery internals (reused, regression-covered by M021–M025
  suites), email (no delivery in slice), visual baselines (curated set
  unchanged; M038+ owns web visual claim).
