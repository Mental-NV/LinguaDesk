# LinguaDesk — UX/UI Specification

**Document:** #2 · **Version:** 2.2 · **Status:** Simplified MVP with pause processing and automatic registration verification; real account-email journeys deferred
**Updated:** September 20, 2026 (UTC)

## 1. Authority, inputs, and scope

[Document #0](00-SDD-Planning-Workflow.md) owns process; [PRD](01-PRD.md) owns active and deferred product scope and D-17–D-21. Current shared API behavior is in [#5](05-api-design.md). Runtime evidence is tracked separately in [delivery status](delivery/current.md).

This document owns current routes, layout, controls, messages, state transitions and acceptance contracts. `Must` is binding for the **active MVP**; rows marked **Deferred** or **Retired** impose no current implementation/test gate. The 17 UX-US, 112 UX-AC, and original 44 UX-MSG IDs remain traceable; some are amended and some inactive. In compact references, AC/US/MSG mean UX-AC/UX-US/UX-MSG. IDs are never reused for unrelated behavior.

No sentence alternatives, sentence identities, comparison/highlighting, Show changes, Google controls, truncation overlays, custom selectors or tools sheets ship in MVP. No replacement read-only diff is required yet. Use native inputs/selects, inline settings, one-second pause processing, and plain editable results. There is no Translate/Rewrite button, New workspace control, usage summary, persistent character counter, dedicated source/result status line, or Copy result control. Removed targeted metadata preservation and the duplicate correction toggle are not later-phase obligations. PRD Section 3.1 is the sole deferred-feature register; Git `54343c3` retains the old detailed design only as historical input.

The minimum usable workspace outcome is two end-user journeys: a verified user can translate text in `/translate`, and a verified user can correct or restyle text in `/rewrite`. Account pages, protected routes, capability selectors, API operations and standalone AI behavior are prerequisites or independent-client outcomes; none is evidence that either web journey exists. Each journey is complete only when the published SPA exposes the controls in Sections 5, 7 and 8 and returns an editable/copyable result through the integrated application path.

Automatic source detection and pause-based submission are active. A workspace is one verified tab's in-memory source, settings, result and operation state. A current response must match its workspace generation, feature, submitted source/settings revision and result-edit revision. Source/settings edits, manual result edits and session teardown can make a response outdated.

API counting/identity/recovery and auth/error semantics are owned by [#5's behavioral design](05-api-design.md); the generated wire contract follows during selected implementation. Model eligibility/output checks stay with #4; full verification/evaluation with #6. Only current-scope dependencies block selected implementation.

## 2. Historical reference research (non-normative)

See [Historical UX research](archive/ux-research.md#2-historical-reference-research-non-normative).

### 2.1 Inspection record

See [Historical UX research](archive/ux-research.md#21-inspection-record).

### 2.2 Current disposition of historical patterns

See [Historical UX research](archive/ux-research.md#22-current-disposition-of-historical-patterns).

## 3. Information architecture and workspace lifecycle

### 3.1 Routes and account access

| Route | Signed-out / unverified behavior | Verified behavior |
| --- | --- | --- |
| `/` | `/login` or `/verify-email` respectively | `/translate` |
| `/translate`, `/rewrite` | Require local sign-in, then verification; remember only the safe destination path | Requested feature, with heading focus |
| `/login`, `/register` | Local email/password forms; registration success stays on the page with explicit Go to sign in; no Google control | Redirect to last protected route unless sign-out confirmation is pending/failed |
| `/verify-email` | Retained unverified-account/link/resend/status UI; real delivery is dormant under DF-008 | Success offers explicit continuation to the last protected route |
| `/forgot-password`, `/reset-password` | Retained recovery forms; real delivery is dormant under DF-008 | Same retained recovery result behavior |
| Unknown path | Page not found; Go to sign in / Verify email | Page not found; Go to Translation |

**M002 staged shell — selected 2026-09-08:** Before account functionality exists, [package 002](08-backlogs/M002/spec.md#2-selected-story-and-staged-route-behavior) implements only signed-out informational navigation: root/protected feature routes lead to `/login`; `/login` and `/register` clearly state that their functionality is unavailable and expose no credential fields or submission. Unknown client pages offer Go to sign in. This is a bounded pre-account delivery state, not fake authentication, an accessible protected workspace or completed UX account acceptance. M011–M013 replace these informational states with the existing account contracts; the route table above remains the complete intended behavior. Shell links, heading focus, visual tokens and responsive access already apply.

Only a verified local account may process text. Errors must not reveal whether an email belongs to an account. Form fields, password checklist, links, cooldowns and server categories come from the account contract, with UX states in Section 9. There is no Google callback route in the active UX.

### 3.2 Navigation and per-tab state

Translation and Rewriting are separate SPA routes sharing auth/usage. Mode links and browser Back/Forward preserve each page's in-memory text/settings/result/scroll and focus the destination `h1`; navigation cancels an undispatched pause/queued candidate and never submits. A pending operation may finish for its matching hidden feature but never changes the visible feature's focus/text. Returning to a feature alone does not restart its timer. Opening or duplicating a new tab starts an empty workspace; text is never transferred.

### 3.3 Refresh and restoration

Refresh, full-document navigation and tab close end the workspace. After authentication bootstrap the requested page opens empty with current PRD defaults. A real bfcache restoration must not reveal old text: teardown on `pagehide` and defensively reset on `pageshow` as described in #3. Synthetic events only prove handlers, not actual browser restoration. Backgrounding, offline intervals and switching SPA modes do not end a workspace.

### 3.4 Workspace and session boundary

A verified tab's workspace ends at reload/full-document navigation/tab close, sign-out, session expiry or account invalidation. Source/result/settings live only in memory, never URLs/history payloads, local/session storage, IndexedDB, service-worker/HTTP caches, autofill-restored workspace values or analytics. Disable workspace editor autocomplete/restoration where controllable; account password-manager support remains allowed. Invalidate pending callbacks and pause timers with the workspace generation before rendering cleared/login state. Backend accounting may still settle an already submitted operation without restoring its text.

Footer: `Text and settings are cleared when this workspace ends, including refresh, sign-out, or session expiry.` No text is saved as history. This footer describes the browser workspace lifetime, not a provider retention or no-training promise. D-18 removes provider eligibility requirements of that kind; the clarified provider-managed caching choice does not change workspace teardown or introduce application response storage.

Provide an inline **Sign out** action, avoiding a bespoke account menu. There is no manual workspace reset action or confirmation dialog. Refresh or full navigation starts a new empty workspace with current defaults and does not replay an old pause timer.

Sign out clears immediately and displays `/login` with `Signing out…`. Failure shows `Workspace cleared. Sign-out could not be confirmed. Try again.` and `Try sign-out again`; suppress authenticated redirect until confirmed. Retry sends only the sign-out action. Success exposes the ordinary login form. Session expiry clears text before login renders and shows MSG-037. No late response may restore it.

## 4. Visual system and responsive layout

### 4.1 Design tokens

The interface uses system fonts to avoid a font-download layout shift and to render Cyrillic, Romanian diacritics, and Simplified/Traditional Chinese consistently.

| Token | Value | Use |
| --- | --- | --- |
| `font-sans` | `Inter, ui-sans-serif, -apple-system, BlinkMacSystemFont, "Segoe UI", "Noto Sans", "Noto Sans SC", Arial, sans-serif` | All UI and editor text |
| `font-mono` | `ui-monospace, SFMono-Regular, Consolas, monospace` | Never for user text; diagnostic fixtures only |
| Type sizes | 12/16, 14/20, 16/24, 20/28, 28/36 px | Caption, body-small, body/editor, section heading, page heading |
| Type weights | 400, 500, 600, 700 | Body, controls, headings, wordmark only |
| `color-bg` | `#F6F7F9` | Page background |
| `color-surface` | `#FFFFFF` | Editors, menus, dialogs |
| `color-text` | `#17212B` | Primary text; 16.29:1 on white |
| `color-muted` | `#5E6B78` | Secondary text; 5.45:1 on white |
| `color-brand` | `#183B56` | Wordmark and selected emphasis |
| `color-action` | `#0B5FFF` | Links, primary controls; 5.13:1 with white text |
| `color-focus` | `#005FCC` | Focus ring; 5.98:1 against white |
| `color-border` | `#D6DCE3` | Neutral borders |
| `color-success` | `#0B6E4F` | Success text/icons |
| `color-warning-bg/text` | `#FFF7ED` / `#8A4B08` | Warnings; 6.40:1 |
| `color-error-bg/text` | `#FFF1F0` / `#B42318` | Errors; error text 6.57:1 on white |
| Spacing | 4, 8, 12, 16, 24, 32, 48, 64 px | Use only this scale |
| Control heights | 36 compact, 44 default, 48 touch/form px | Never below 44 px for standalone mobile targets |
| Radius | 6 controls, 10 panels, 14 dialogs, 999 pills px | Consistent containment |
| Border | 1 px solid `color-border`; selected 1 px `color-action` | Panels and controls |
| Focus | 2 px solid `color-focus`, 2 px offset | All focusable elements |
| Shadow | `0 8px 24px rgba(23,33,43,.12)` | Dialogs only |
| Motion | 120 ms color/border; 160 ms opacity/transform, ease-out | Removed under `prefers-reduced-motion: reduce` |
| Icons | 20 px, 1.75 px stroke, rounded monochrome SVG | Always paired with an accessible name; decorative icons hidden |

Disabled text uses `#7C8794` on `#EEF1F4`; disabled state also removes pointer affordance and is exposed programmatically. Placeholder text is `#6C7784` (4.56:1 on white). No state is communicated by color alone.

Use `#788594` for textbox/control outlines that are necessary to identify a control; the lighter panel border is decorative. Authentication content is 400 px wide maximum with 24 px vertical field gaps, 48 px inputs/buttons, and 16 px page gutters on mobile; its top margin is 48 px desktop/24 px mobile. LinguaDesk branding is the text wordmark at 20/28 px, weight 700; no external logo asset is required.

### 4.2 Layout

All widths below are **CSS pixels of the layout viewport**. The workspace frame includes the heading, selectors and editors. Its stated width is the usable content width *after* outer gutters; `max-width` is not reduced again by padding. Apply `box-sizing: border-box` to controls and panels, `min-width: 0` to grid children, and `width: 100%` to textareas so borders/padding remain inside their assigned track.

| Layout viewport | Outer gutter on each side | Workspace content width | Editor arrangement and track width |
| --- | ---: | --- | --- |
| ≥1200 px, wide desktop | 32 px minimum | `min(1280px, 100% - 64px)` | Two equal columns, each `(content width - 16px) / 2` |
| 768–1199 px, compact desktop | 24 px | `100% - 48px` | Two equal columns, each `(content width - 16px) / 2` |
| 360–767 px, mobile | 16 px | `100% - 32px` | One column; each editor occupies `100%` of content width |
| 320–359 px, small mobile | 12 px | `100% - 24px` | One column; each editor occupies `100%` of content width |

The gap between desktop editor columns is 16 px; adjacent mobile controls and editor groups use 16 px vertical gaps. At the verification viewports this gives **1280 px content and 632 px per editor column at 1440 px**, **720 px content and 352 px per editor column at 768 px**, **358 px content/editor width at 390 px**, and **296 px content/editor width at 320 px**. Both desktop editor panels have the same width and height; use equal grid tracks and stretch the panels to the same row height. Panel borders and any inner padding belong inside these widths. Selects, textareas, buttons and essential error alerts must shrink or wrap within their assigned width; none sets a minimum width that forces page overflow.

The frame widths transfer directly to CSS (with no horizontal padding added to `.workspace-frame`):

```css
.workspace-frame { width: min(1280px, calc(100% - 64px)); margin-inline: auto; }
@media (max-width: 1199px) { .workspace-frame { width: calc(100% - 48px); } }
@media (max-width: 767px) { .workspace-frame { width: calc(100% - 32px); } }
@media (max-width: 359px) { .workspace-frame { width: calc(100% - 24px); } }
```

The same shell serves both features. Header is at least 64 px desktop / 56 px mobile and wraps/grows as needed without clipping. Main content starts 24 px below mode navigation. Editors are at least 420/360/240 px tall at wide/compact/mobile widths. Desktop bodies scroll after `min(60dvh, 640px)`; mobile editors grow to 360 px then scroll. Toolbars never overlay text. Preserve ordinary page scrolling and caret visibility when the software keyboard opens.

#### 4.2.1 Translation — wide desktop (≥1200 px)

```text
Translation / Rewriting
Translation
[Source language ▼]                 [Target language ▼]
┌───────────────────────────────┐   ┌───────────────────────────────┐
│ Source text                   │   │ Translation result            │
│                               │   │                               │
│                               │   │                               │
└───────────────────────────────┘   └───────────────────────────────┘
[Clear source text]
```

Source language starts at the left edge of the source column; Target language starts at the left edge of the result column. Each select is `width: min(100%, 320px)` within its column. The two editor panels use identical dimensions and their textareas fill the available inner width. A valid source and target trigger translation after the processing pause specified in Section 6.

#### 4.2.2 Translation — mobile (320–767 px)

```text
Translation / Rewriting
Translation
[Source language ▼]
[Target language ▼]
┌───────────────────────────────┐
│ Source text                   │
│                               │
└───────────────────────────────┘
[Clear source text]
┌───────────────────────────────┐
│ Translation result            │
│                               │
└───────────────────────────────┘
```

The two language selects stack before the source editor and each fills the workspace width. The result follows the source in reading, focus and visual order. Selects and panels are `width: 100%` of the workspace: 358 px at a 390 px viewport and 296 px at a 320 px viewport.

#### 4.2.3 Rewriting — wide desktop (≥1200 px)

```text
Translation / Rewriting
Rewriting
[Writing language ▼] [Writing mode ▼]
┌─────────────────────────────────────────┐   ┌─────────────────────────────────────────┐
│ Source text                             │   │ Improved result                         │
│                                         │   │                                         │
│                                         │   │                                         │
└─────────────────────────────────────────┘   └─────────────────────────────────────────┘
[Clear source text]
```

The Writing language and single Writing mode selects sit directly above the source column. Their group is `width: 100%` of that column, with two equal `minmax(0, 1fr)` tracks and a 16 px gap; at 1440 px each select track is 308 px. Reserve the corresponding settings row in the result column so the editor top edges align. Both editor panels have identical dimensions. A valid source triggers rewriting after the processing pause in Section 6.

#### 4.2.4 Rewriting — mobile (320–767 px)

```text
Translation / Rewriting
Rewriting
[Writing language ▼]
[Writing mode ▼]
┌───────────────────────────────┐
│ Source text                   │
│                               │
└───────────────────────────────┘
[Clear source text]
┌───────────────────────────────┐
│ Improved result               │
│                               │
└───────────────────────────────┘
```

Writing language and Writing mode stack above the source and fill the workspace width. The result follows the source in reading, focus and visual order. Selects and panels are `width: 100%` of the workspace: 358 px at a 390 px viewport and 296 px at a 320 px viewport. Do not duplicate the Writing mode control at a breakpoint.

Across both features, use a 16 px gap between controls on a row and between adjacent stacked controls. The desktop editor grid is equivalent to `grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; align-items: stretch`, with editor panels filling the common row height; at `max-width: 767px` it becomes `grid-template-columns: minmax(0, 1fr)`. Source and result use plain native textareas with visible labels. Long English/Cyrillic/Chinese content and errors wrap without horizontal page overflow. Native select popup rendering is platform-owned; no pixel matching of its open menu is required. Do not implement a rich-text editor, syntax/diff overlay, sentence hit targets, or custom caret mapping.

## 5. Components and processing behavior

| Component | Current contract |
| --- | --- |
| Mode navigation | Native links Translation / Rewriting with current-page state |
| Source selector | Native select named Source language on Translation, Writing language on Rewriting; Detect automatically plus four languages |
| Target selector | Native select named Target language; explicit Choose target language placeholder; four languages, same known source disabled |
| Writing mode | Native select named Writing mode; Correction only, Simple, Casual, Business, Academic, Enthusiastic, Friendly, Confident, Diplomatic; exactly one selection |
| Source editor | Textarea named Source text; Clear source text button; remains editable during work; no persistent character counter/limit line |
| Processing | One-second timer starts or restarts on eligible source/settings changes; no Translate/Rewrite button; at most one logical operation per settled revision |
| Result editor | Textarea named Translation result or Improved result; empty placeholder read-only, complete result editable even during an update; editing protects it from earlier responses |
| Copy | Native text selection and clipboard copy of the editable result; no Copy result control or copy-status line |
| Usage | Server-authoritative accounting controls eligibility; no persistent usage summary or global spend/provider/model/rule controls |
| Account forms | Visible email/password/confirmation labels, native submit, password reveal, field errors and summary; no custom auth widgets |

Exactly one visible `main` and `h1`; editor headings are `h2`.

Use platform native behavior for selectors, text selection, undo/redo, clipboard and IME. Enter inside a textarea inserts a newline; the final committed source edit restarts the one-second timer. Never dispatch during composition. On `compositionend`, start a fresh timer only if the committed source/settings revision is eligible. Native dropdown Escape/commit behavior is platform-owned, replacing the old bespoke listbox/active-descendant contract.

One pending operation per page is retained. A new eligible revision waits for its own timer and, if another operation is unsettled, for that operation to resolve. Manual result editing or leaving the feature cancels an undispatched timer/queued candidate, but cannot cancel a provider operation already sent. An unchanged settled revision never runs again. Known invalid input cancels the timer and keeps the source editable; essential blocking/failure feedback uses a page alert rather than dedicated source/result status lines. Route changes and account continuation may move focus; async completion/usage/background validation never steals focus or moves the source caret.

## 6. Processing state and message contract

See [UX processing and message contract](ux/processing.md#6-processing-state-and-message-contract).

### 6.1 Pause processing and competing events

See [UX processing and message contract](ux/processing.md#61-pause-processing-and-competing-events).

### 6.2 Response ordering and recovery

See [UX processing and message contract](ux/processing.md#62-response-ordering-and-recovery).

### 6.3 Exact message IDs and disposition

See [UX processing and message contract](ux/processing.md#63-exact-message-ids-and-disposition).

### 6.4 Announcements

See [UX processing and message contract](ux/processing.md#64-announcements).

## 7. Translation contract

- Initial source is Detect automatically; target is Choose target language. Chinese is labelled `Chinese (Simplified output)` and accepts both Chinese scripts. Source detection occurs for a current paused candidate; a manual choice does not waive supported-content validation.
- Target lists the four PRD languages. Disable the known source language as target; if a later source change equals an already selected target, retain the invalid value and block dispatch until corrected. Selecting a valid target with eligible source restarts the timer.
- Count complete input internally using #5's Unicode contract. At or below the configured limit, the paused candidate submits all entered text. Above it, retain text/caret and previous result, show an essential blocking alert, and cancel dispatch. The backend rejects oversized independent-client requests too. There is no persistent counter, boundary marker, suffix omission, or partial-result state.
- Native selection/copy uses the exact current result including manual edits/newlines. Empty source does not erase an existing result; Clear source text preserves that result and cancels any pending timer without dispatch.

## 8. Rewriting contract

The page uses Writing language plus one native Writing mode dropdown from Section 5. Correction only is the default and makes minimal grammar/spelling corrections without discretionary restyling. Selecting any of the eight styles/tones preserves mandatory correction. There is no second checkbox, None set value, disabled-style restoration or Show changes preference.

At or below the configured rewriting limit, a current paused candidate processes the entire source. Above it, MSG-011 blocks processing and retains all text. Source, language or mode edits restart the timer. Finishing IME composition starts the timer for the committed eligible revision; no interim composition text is submitted.

A successful current operation returns a complete plain result; source stays untouched. Manual result editing is ordinary native editing, creates no charge/request, and protects against older responses. A later operation triggered by a source/settings change can replace the complete edited result. There is no sentence segmentation, identity mapping, cached alternative, comparison view, deletion annotation, or sentence shortcut in the MVP frontend/API contract.

DF-001/DF-002 later design must invalidate **all** assistance metadata on any manual result edit. It must not revive targeted sentence preservation, and neither deferred capability is required to implement this plain-text editor.

## 9. Local-account and availability flows

Registration contains Email, Password, Confirm password, actual policy checklist, Show password, Create account, and Sign in. Login contains Email, Password, Show password, Sign in, Create account and Forgot password. Password reveal preserves selection. Invalid local submission sends no request, shows `Check the highlighted fields.` with linked errors, focuses the first invalid field, and exposes `aria-invalid`/descriptions. A current valid submission disables fields and guards duplicate click/Enter. Failed credential/policy/network responses clear password values and permit explicit recovery; keep email only in this in-memory auth journey.

Registration success replaces the form with MSG-034 and an explicit Go to sign in link. It retains no email or password in the rendered success state and creates no pending-verification navigation guard. Registration neither signs in nor starts language processing. The created account is already verified by the temporary DF-008 substitute.

Existing unverified-account verification and password-recovery pages retain their previously implemented safe behavior for dormant compatibility, but real delivery and their MVP evidence obligation are deferred under DF-008. Generic account network failure remains `We couldn’t complete this request. Try again.` No Google copy/helper or linking screen exists.

Old auth completions cannot navigate over a newer auth route or restore cleared passwords/text. Reconcile actual account status on the next protected navigation. Keep safe return paths only, not arbitrary external redirects.

A failed usage refresh does not turn a successful transformation into failure: keep its result and retry a usage read on the next relevant eligibility check. `Check availability`, when offered after a suspension alert, performs one status read and never starts a paid operation.

Usage remains server-authoritative but has no persistent UI summary. Exhaustion uses MSG-015/016/017 in an essential page alert and blocks processing; text/edit/native copy remain usable. An operation requiring more than remaining allowance shows `This request needs {required} characters, but you have {remaining} remaining today.` A new eligible source/settings revision starts a timer only after capacity is confirmed; restored capacity or reconnect alone starts no paid operation. Unknown usage/status triggers a read, never a speculative charge or paid retry.

## 10. Accessibility and browser coverage

Target [WCAG 2.2 AA](https://www.w3.org/TR/WCAG22/) with native semantics. Keep 44×44 px standalone touch targets where possible (24×24 minimum except valid inline exceptions), 4.5:1 ordinary text / 3:1 large text, and 3:1 focus/control boundaries. Use a 2 px focus ring with 2 px offset. Errors/status use text as well as color. Close returns to its trigger. All other controls keep native behavior; no custom active-descendant or sheet-focus system is required.

At 200% zoom all controls remain available; at effective 400% / 320 CSS px there is no horizontal page overflow. Text-spacing overrides (1.5 line height, 2× paragraph spacing, .12em letter and .16em word spacing) cause no clipping. Reduced motion removes transitions/shimmer. Native Chinese composition, selection and undo/redo remain intact; no interim composition text starts processing. Do not move focus on completion or scroll a background result into view.

### 10.1 Supported browsers

Support current and immediately previous stable major releases at each release candidate for Firefox and Chrome on desktop and mobile. [Verification plan Section 4](verification/frontend.md#4-frontend-browser-and-manual-verification) owns automated lane allocation, actual-version evidence and manual browser-matrix procedures. Bundled engines alone do not establish this support contract.

### 10.2 Verification ownership

The accessibility outcomes above remain UX contracts. Automated checks and required manual evidence are consolidated in [verification plan Sections 4.2–4.4](verification/frontend.md#42-browser-lanes); deferred journeys create no MVP evidence requirement.

## 11. User stories and disposition

| Story | Status | User outcome | Product scope |
| --- | --- | --- | --- |
| UX-US-001 | MVP | Navigate between Translation and Rewriting while preserving the active workspace | FR-003/038 |
| UX-US-002 | MVP | Register a local account and verify its email | FR-001/002 |
| UX-US-003 | Amended | Sign in locally and recover cleanly from authentication errors; Google deferred | FR-001/037; DF-007 |
| UX-US-004 | MVP | Reset a forgotten local password | FR-002 |
| UX-US-005 | Amended | Choose language/mode and validate the paused candidate | FR-004–006/014 |
| UX-US-006 | Amended | Receive a complete translation after an eligible pause | FR-008–011 |
| UX-US-007 | Amended | Understand input rejection, whole-input limits and no-charge failures | FR-004/005/007/024 |
| UX-US-008 | MVP | Edit/copy translations and reject outdated responses | FR-011/023/026/028 |
| UX-US-009 | Amended | Choose one correction/style/tone mode and receive a paused rewrite | FR-012–016 |
| UX-US-010 | Amended | Preserve source and edits while an automatically submitted rewrite completes | FR-017/018/022/023 |
| UX-US-011 | Amended | Edit and copy the plain rewrite result; change review is deferred | FR-023; DF-002 |
| UX-US-012 | Deferred | Refine a sentence with reusable alternatives | DF-001 |
| UX-US-013 | Amended | Protect manual result edits from older responses; targeted metadata preservation retired | FR-018/023 |
| UX-US-014 | Amended | Receive essential allowance/availability feedback without a usage summary | FR-024–028 (FR-025 deferred) |
| UX-US-015 | Amended | Recover explicitly from failure, offline periods and unknown outcomes | FR-034/037 |
| UX-US-016 | Amended | Use native controls and current core journeys with touch | NFR-005/007 |
| UX-US-017 | Amended | Tear down the per-tab workspace without restoration | FR-038/NFR-004 |

## 12. Acceptance contracts and preserved scenario IDs

See [UX acceptance contracts](ux/acceptance.md#12-acceptance-contracts-and-preserved-scenario-ids).

### 12.1 Harness and fixtures

See [UX acceptance contracts](ux/acceptance.md#121-harness-and-fixtures).

### 12.2–12.9 Scenario disposition and current acceptance

See [UX acceptance contracts](ux/acceptance.md#122129-scenario-disposition-and-current-acceptance).

### 12.10 Curated visual regression

See [UX acceptance contracts](ux/acceptance.md#1210-curated-visual-regression).

## 13. Verification boundaries

[Verification plan Sections 2–4](06-verification-plan.md#2-verification-layers-and-check-catalog) define the lowest sufficient evidence for each assertion and distinguish fixtures, integrated smoke and live/manual checks. The [local acceptance allocation](verification/coverage.md#82-local-acceptance-allocation) preserves every UX-AC ID and its current disposition. No scenario here is a claim of a passing test.

[Roadmap M028](07-roadmap.md#45-useful-end-to-end-product-increments) owns the first complete Translation UI outcome and [M029](07-roadmap.md#45-useful-end-to-end-product-increments) owns the first complete Rewriting UI outcome. [Verification plan Section 4.5](06-verification-plan.md#45-required-user-visible-transformation-evidence) defines the minimum evidence that prevents API, LLM or mocked-component work from being reported as either end-user outcome.

Every selected UI-facing feature must have a published end-to-end case executed as #6's dedicated verified synthetic user. At least one suite case performs the visible Email/Password/Sign in flow; subsequent feature cases may begin from the documented authenticated fixture, but they still exercise the real protected route and application API and must assert the resulting controls, essential errors and result text in the UI. Server-side accounting is verified through the API/database boundary. A successful request observed only in network logs is not UX acceptance.

## 14. MVP traceability

The sole cross-document [product and release coverage matrix](verification/coverage.md#81-product-and-release-coverage) now lives in #6. This document retains UX behaviors, stories and scenario IDs; it does not maintain a second product-to-evidence table.

## 15. Decisions, handoffs, and deferred reactivation

UX-D-001–UX-D-007 remain stable historical decision IDs. Their current disposition is explicit below; obsolete text is preserved in Git `54343c3`, not an active alternative contract.

| Decision | Current disposition |
| --- | --- |
| UX-D-001 / Q-009 | Amended: current native-control/pause-processing/browser/accessibility contract in this document |
| UX-D-002 / Q-002 | Retired: correction-toggle restoration is removed; single mode replaces it |
| UX-D-003 / Q-002 | Retired: targeted sentence correspondence is cut, including future default behavior |
| UX-D-004 / Q-002 | Retained/amended: manual edits protect both plain result fields from earlier responses; usage may still settle |
| UX-D-005 / Q-004 | Retained: one verified tab lifetime and no durable text restoration |
| UX-D-006 | Amended: plain Translation result remains editable during processing, protected by result-edit revision |
| UX-D-007 | Amended: server-authoritative accounting and stale-success disclosure without a persistent usage summary or provider/global/budget amounts |
| UX-D-008 | Accepted 2026-09-08: PRD D-17 replaces affected prior UX; DF register controls later scope, no dormant MVP widgets/tests |
| UX-D-009 | Accepted 2026-09-20: PRD D-21 replaces explicit processing and removes the listed workspace controls/status lines; preserve native copy, essential errors and stale-response protection |

API design #5 now defines canonical scalar counting, operation identity/recovery, ordered usage, cookie/bearer lifecycle, error semantics, cancellation and rollover. Selected slices still fix exact wire operations/fields, auth policy/bootstrap/delivery details and the concrete recovery UI mapping before dependent handlers/clients. OpenAPI is generated and reviewed early in implementation, not hand-authored during planning. #3/#4 own serving capability, cost bounds and the simple family chains' quality/output/deadline checks; provider retention/no-training certification is not a prerequisite. Provider-managed caching changes no current UI, character accounting or application text-lifecycle contract. #6 supplies current-scope evidence mapping. Q-002 targeted matching and Google linking no longer block MVP. Advanced routing and sentence context are deferred dependencies only.

Deferred work is selected through PRD Section 3.1 and the existing #8 package workflow; no new full deferred implementation spec is authored now. At reactivation, refine only the selected capability, reassess affected historical UX IDs, and update its owning contracts/tests. Custom visual tools do not restore obsolete toggles or targeted sentence preservation.

## 16. Validation and readiness

This revision activates the one-second pause and removes the listed UI controls/status lines while preserving server-side counting and current response protection. All original story/scenario/message IDs retain an explicit current disposition. No scenario is a claim of a passing test.

Ready for current-MVP scoped planning. Resolve only the selected slice's Section 15 dependencies before implementation. Release evidence, provider evaluation, actual browser behavior and runtime verification remain pending. Deferred/retired rows neither block MVP nor count as passed release checks.
