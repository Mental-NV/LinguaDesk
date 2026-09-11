# M032 — Selected specification
Selected items: BI-032. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: safe workspace ending on the real `/translate` and `/rewrite`
workspaces built by M028/M029 and hardened by M030/M031, through the
published SPA → auth → API → accounting → deterministic-provider path.
Inline **Start new workspace** and **Sign out** actions on both pages;
the §3.4 native reset dialog with exact heading, body and buttons;
empty-workspace immediate reset; confirm-reset clearing of both pages
with defaults restoration, navigation to `/translate` and Source-text
focus; immediate sign-out and session-expiry teardown ahead of the
login render; reload, full-document navigation, tab close and real
bfcache restoration clearing; footer lifetime copy; per-tab memory-only
text with autocomplete/storage/cache exclusion. Late operation
responses after any teardown must restore no text; only independently
settled usage may reconcile. Deterministic fake provider only.
Requirements FR-038 and NFR-004; UX §3; UX-AC-003/015/016/084/085/086/
087 (UX-AC-002 as the preserved-navigation regression guard); message
copy UX-MSG-037 plus the §3.4 dialog/body/footer strings; fixtures/
identity from #6 §4.1 and §4.5 E2E contract.

Explicit exclusions: translation/rewriting happy-path behavior itself
(M028/M029 own it; regression-covered here); competing-event hardening
itself (M030 owns it; regression-covered here); failure/unknown-outcome
recovery itself (M031 owns it; regression-covered here); live provider
dispatch, quality, performance and serving/billing attribution (Q-001
remainder, Q-005); monetary-cap amount (unset); account deletion,
backup/aggregate/unresolved-exposure retention rules (Q-004 remainder);
abuse limits (Q-008/P-005); compatibility promises (P-006); email
delivery (M034); full browser/device/AT claim (M038/M039, G3). No
backend contract change is selected: submit, status-read and usage
shapes are reused from the reviewed M026/M027 wire, and any unexpected
handler need reopens the contract-review gate before client adoption.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | On each non-empty page, activating inline `Start new workspace` opens the accessible native dialog with heading `Start a new workspace?`, the §3.4 clearing body, `Cancel` initially focused and `Start new workspace`; Escape/Cancel preserves all state and returns focus to its trigger | UX §3.4; UX-AC-084 |
| AC-002 | Confirming reset with pending feature operations clears both pages to current defaults, navigates to `/translate`, focuses Source text, submits no operation, and late responses restore no text; only independently authorized usage may reconcile | UX §3.4; UX-AC-085; FR-038 |
| AC-003 | When both pages are empty, reset acts immediately with no dialog: Correction-only/detection/empty-target defaults restored, source focused, nothing submitted | UX §3.4; UX-AC-086; FR-038 |
| AC-004 | Real reload, full-document navigation, tab close and bfcache restoration end the workspace: the requested page opens empty with current defaults and no text survives in storage, URL/history payloads, HTTP caches or autofill-restored values; restoration is proven with a real browser navigation/restoration check, not synthetic events alone | UX §3.3/§3.4; UX-AC-003/087 |
| AC-005 | Inline `Sign out` clears the workspace immediately (including lifted-store text), displays `/login` with `Signing out…`; failure shows `Workspace cleared. Sign-out could not be confirmed. Try again.` with auth-only `Try sign-out again`, suppresses authenticated redirect until confirmed, and Back cannot expose old text | UX §3.4; UX-AC-016 |
| AC-006 | Session expiry clears text before the login form renders, shows UX-MSG-037, rejects late text callbacks, and leaves storage/history clean | UX §3.4; UX-AC-015; UX-MSG-037 |
| AC-007 | At least one published E2E case signs in through the real `/login` form; other cases reuse the documented real-auth fixture (same predefined account, same-origin cookie state; no forged sessions, test login routes, disabled middleware or intercepted `/api`); every case asserts its browser-visible outcome through semantic locators; seeded Smoke-only account/database, run-owned and removed afterward; password value absent from all artifacts | #6 §4.5; V-012; fixtures §4.1 |
| AC-008 | No source/result text, credentials, global counts, monetary values or provider internals in problems, logs, traces, TEXT columns, backups, storage, history or caches; footer states the workspace lifetime; inline display shows only the user's own usage/remaining/reset | NFR-004; UX-AC-087; V-015 |
| AC-009 | Translate submit/status/usage and rewrite submit/status/usage flows reuse the reviewed M026/M027 wire shapes via regenerated client types with zero OpenAPI/TypeScript drift | V-009; arch §8.3 |

## Constraints and decisions

- Current code keeps both workspaces in the lifted store above the
  router with per-page reducers; `handleSignOut` aborts flights but
  dispatches no clearing action, reducers define no reset action, no
  Start-new-workspace control or dialog exists, and only auth pages
  handle `pageshow`. This slice adds a `workspaceCleared`-style reset
  action to both reducers (clearing text/settings/result/errors/pending
  identity with stale-response fencing, preserving the settled usage
  snapshot), a store-level reset-all, dialog/teardown/restoration
  wiring, footer copy and editor-autocomplete/storage exclusions.
- Mode-link and Back/Forward in-memory preservation (UX-AC-002) stays:
  ordinary SPA navigation between the two pages neither clears nor
  submits; only the §3.4 boundary events end the workspace.
- Offline/backgrounding the tab or switching SPA modes does not end a
  workspace; `unload`-only cleanup is not acceptable, `pagehide` plus
  defensive `pageshow` handling is required; synthetic event dispatch
  proves handlers only.
- Clarifications: Q-004 observable workspace lifetime is implemented
  here; durable deletion/backup/retention rules remain pending and do
  not block this slice. Q-001/Q-005/Q-008/Q-010 remain pending and
  non-blocking for this deterministic slice. No new open question is
  raised.
- Omitted domains: backend accounting/recovery internals (reused,
  regression-covered by M021–M025 suites), email (no delivery in
  slice), visual baselines (curated set unchanged; M038+ owns the web
  visual claim), provider retention/training promises (D-18 excludes
  them; NFR-004 governs LinguaDesk storage only).
