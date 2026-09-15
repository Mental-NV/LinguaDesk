# M038 — Selected specification
Selected items: BI-038. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: one bounded desktop evidence group over the working
`/translate` and `/rewrite` journeys built by M028/M029 and hardened by
M030–M032, exercised through the published SPA → auth → API →
accounting → deterministic-provider path at desktop sizes. V-010
desktop contracts: native editing/caret/selection/clipboard,
keyboard-only completion of both journeys, IME composition guard (no
submit during composition), desktop reflow/geometry including zoom and
text-spacing checks. V-011 desktop slice: three curated desktop-size
baselines (Translation ready 1440×900, Translation oversize-error
1440×900, local registration long-validation 1440×900) with pinned
capture, starting thresholds and reviewed baselines; automated
semantic/contrast scans with no serious/critical violations. V-012
desktop lane: dedicated-user published E2E Translation and Rewriting
journeys at 1440×900 with at least one real `/login` form path.
Desktop manual subset: keyboard-only, 100%/200%/effective-400% zoom,
forced colors, text spacing and reduced motion on desktop, plus
NVDA/Firefox and VoiceOver/Safari desktop review of both journeys and
branded Chrome/Edge smoke; gaps recorded explicitly.
Requirements NFR-005 (Must) and NFR-007 (Should); UX §4/5/10;
UX-AC-025/043/077/078/079/081/083/095; message/behavior contracts
reused from M028–M032 with no copy change; fixtures/identity from #6
§4.1 and the §4.5 E2E contract.

Explicit exclusions: the narrow-screen group itself (M039 owns
390×844/360×800 baselines, reflow and mobile evidence); iOS
Safari/VoiceOver, Android Chrome/TalkBack and remaining actual-device
work (M039/successors); the remaining previous-version matrix and AT
coverage beyond the desktop subset (successors; G3/RG-006 stay
pending); sentence/comparison/sheet baselines (retired/deferred, never
added); live provider dispatch, quality, performance and
serving/billing attribution (Q-001 remainder, Q-005); monetary-cap
amount (unset); deletion/backup retention rules (Q-004 remainder);
safeguards/compat proposals (Q-008/P-005/P-006, unadopted); email
delivery (DF-008). No backend contract change is selected: submit,
status-read and usage shapes are reused from the reviewed M026/M027
wire, and any unexpected handler need reopens the contract-review gate
before client adoption.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | The predefined verified user completes the Translation journey at 1440×900 Chromium through the published path: opens `/translate`, selects a valid source/target, enters text, waits without causing a request, activates **Translate** once, receives one complete result, sees authoritative usage, edits the result and copies its exact value; oversize/invalid input and a controlled definitive failure preserve source and prior result without a successful-operation charge | #6 §4.5; UX-AC-025/077; V-012 |
| AC-002 | The predefined verified user completes the Rewriting journey at 1440×900 Chromium through the published path: opens `/rewrite`, sees **Correction only** by default, optionally chooses one supported mode, enters text, activates **Rewrite** once, receives one complete same-language result, sees authoritative usage, edits/copies the result while the source remains unchanged; oversize/invalid input and a controlled definitive failure preserve work without a successful-operation charge | #6 §4.5; UX-AC-043/077; V-012 |
| AC-003 | Focused desktop browser contracts prove native editing (caret, selection, undo/redo intact), real clipboard copy of the exact result value, keyboard-only completion of both journeys with visible focus and no trap, ignored activation during IME composition with exactly one submit on explicit post-composition activation, and no horizontal page overflow or clipped controls at desktop sizes including 200% zoom and text-spacing overrides | UX §10; UX-AC-025/043/077/079/083; V-010 |
| AC-004 | Three desktop-size curated baselines (Translation ready 1440×900, Translation oversize-error 1440×900, registration long-validation 1440×900) are captured with pinned fonts/container/scale-1/synthetic content, pass at the §4.4 starting thresholds, and carry explicit human design-review approval; no baseline is auto-accepted and no sentence/comparison/rails/sheet baseline is added | §4.4; UX §4; UX-AC-078; V-011 |
| AC-005 | Automated semantic/contrast scans cover both workspaces and the selected states at desktop with no serious/critical violations, independent of screenshots; the desktop manual subset (keyboard-only, zoom, forced colors, text spacing, reduced motion, NVDA/Firefox and VoiceOver/Safari review of both journeys, branded Chrome/Edge smoke with recorded actual versions) is executed with a named reviewer sign-off, and every uncovered browser/device/AT combination is recorded as an explicit gap — not as passing | §4.2/4.3; UX §10; UX-AC-079/081/083/095; V-011 |
| AC-006 | At least one published E2E case signs in through the real `/login` form; other cases reuse the documented real-auth fixture (same predefined account, same-origin cookie state; no forged sessions, test login routes, disabled middleware or intercepted `/api`); every case asserts its browser-visible outcome through semantic locators; seeded Smoke-only account/database, run-owned and removed afterward; password value absent from all artifacts including screenshots and traces | #6 §4.5; V-012; fixtures §4.1 |
| AC-007 | New artifacts (baselines, screenshots, traces, reports) carry only deterministic synthetic content with no source/result text, credentials, global counts, monetary values or provider internals; translate/rewrite submit/status/usage flows reuse the reviewed M026/M027 wire shapes via regenerated client types with zero OpenAPI/TypeScript drift | NFR-004; V-009; V-015 |

## Constraints and decisions

- Current code has a single Chromium 1440×900 Playwright project, no
  screenshot/baseline assertions, no accessibility scan dependency and
  no desktop manual-evidence procedure; M038 introduces that harness
  without changing product behavior or the backend wire.
- Q-009's active browser/accessibility criteria are resolved by UX
  v1.2 — no product-decision blocker. Actual-version/device/reviewer
  access (readiness gate) is the gating external dependency: arrange
  at the start, verify at the end; unsupported access becomes an
  explicit gap blocking RG-006, never silent passing.
- NFR-007 is Should priority: the design-review sign-off in AC-004
  judges minimalist office-like presentation and monochrome icons
  without inventing new visual requirements.
- This group does not close G3 or RG-006: M039 plus successors own
  the narrow/mobile remainder and the full current/previous matrix.
