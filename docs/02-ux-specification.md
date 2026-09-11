# LinguaDesk — UX/UI Specification

**Document:** #2 · **Version:** 1.9 · **Status:** Simplified MVP ready for scoped planning; authenticated UI/E2E handoff explicit; runtime evidence pending
**Updated:** September 11, 2026 (UTC)

## 1. Authority, inputs, and scope

[Document #0](00-SDD-Planning-Workflow.md) owns process; [PRD](01-PRD.md) owns active and deferred product scope and D-17–D-19. Current shared API behavior is in [#5](05-api-design.md). Runtime evidence is tracked separately in [delivery status](delivery/current.md).

This document owns current routes, layout, controls, messages, state transitions and acceptance contracts. `Must` is binding for the **active MVP**; rows marked **Deferred** or **Retired** impose no current implementation/test gate. The 17 UX-US, 112 UX-AC, and original 44 UX-MSG IDs remain traceable; some are amended and some inactive. In compact references, AC/US/MSG mean UX-AC/UX-US/UX-MSG. IDs are never reused for unrelated behavior.

No sentence alternatives, sentence identities, comparison/highlighting, Show changes, debounce, Google controls, truncation overlays, custom selectors or tools sheets ship in MVP. No replacement read-only diff is required yet. Use native inputs/selects, inline settings, explicit processing buttons, and plain editable results. Removed targeted metadata preservation and the duplicate correction toggle are not later-phase obligations. PRD Section 3.1 is the sole deferred-feature register; Git `54343c3` retains the old detailed design only as historical input.

The minimum usable workspace outcome is two end-user journeys: a verified user can translate text in `/translate`, and a verified user can correct or restyle text in `/rewrite`. Account pages, protected routes, capability selectors, API operations and standalone AI behavior are prerequisites or independent-client outcomes; none is evidence that either web journey exists. Each journey is complete only when the published SPA exposes the controls in Sections 5, 7 and 8 and returns an editable/copyable result through the integrated application path.

Automatic **source detection remains**; automatic **submission does not**. A workspace is one verified tab's in-memory source, settings, result and operation state. A current response must match its workspace generation, feature, submitted source/settings revision and result-edit revision. Source/settings edits, manual result edits, reset and session teardown can make a response outdated.

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
| `/login`, `/register` | Local email/password forms; no Google control | Redirect to last protected route unless sign-out confirmation is pending/failed |
| `/verify-email` | Verification/link/resend/status UI; signed-out success offers Sign in to continue | Success offers explicit continuation to the last protected route |
| `/forgot-password`, `/reset-password` | Local recovery forms; no automatic login after reset | Same recovery result behavior |
| Unknown path | Page not found; Go to sign in / Verify email | Page not found; Go to Translation |

**M002 staged shell — selected 2026-09-08:** Before account functionality exists, [package 002](08-backlogs/M002/spec.md#2-selected-story-and-staged-route-behavior) implements only signed-out informational navigation: root/protected feature routes lead to `/login`; `/login` and `/register` clearly state that their functionality is unavailable and expose no credential fields or submission. Unknown client pages offer Go to sign in. This is a bounded pre-account delivery state, not fake authentication, an accessible protected workspace or completed UX account acceptance. M011–M013 replace these informational states with the existing account contracts; the route table above remains the complete intended behavior. Shell links, heading focus, visual tokens and responsive access already apply.

Only a verified local account may process text. Errors must not reveal whether an email belongs to an account. Form fields, password checklist, links, cooldowns and server categories come from the account contract, with UX states in Section 9. There is no Google callback route in the active UX.

### 3.2 Navigation and per-tab state

Translation and Rewriting are separate SPA routes sharing auth/usage. Mode links and browser Back/Forward preserve each page's in-memory text/settings/result/scroll and focus the destination `h1`; navigation never submits. A pending operation may finish for its matching hidden feature but never changes the visible feature's focus/text. Opening or duplicating a new tab starts an empty workspace; text is never transferred.

### 3.3 Refresh and restoration

Refresh, full-document navigation and tab close end the workspace. After authentication bootstrap the requested page opens empty with current PRD defaults. A real bfcache restoration must not reveal old text: teardown on `pagehide` and defensively reset on `pageshow` as described in #3. Synthetic events only prove handlers, not actual browser restoration. Backgrounding, offline intervals and switching SPA modes do not end a workspace.

### 3.4 Workspace and session boundary

A verified tab's workspace ends at Start new workspace confirmation, reload/full-document navigation/tab close, sign-out, session expiry or account invalidation. Source/result/settings live only in memory, never URLs/history payloads, local/session storage, IndexedDB, service-worker/HTTP caches, autofill-restored workspace values or analytics. Disable workspace editor autocomplete/restoration where controllable; account password-manager support remains allowed. Invalidate pending callbacks with the workspace generation before rendering cleared/login state. Backend accounting may still settle an already submitted operation without restoring its text.

Footer: `Text and settings are cleared when this workspace ends, including refresh, sign-out, or session expiry.` No text is saved as history. This footer describes the browser workspace lifetime, not a provider retention or no-training promise. D-18 removes provider eligibility requirements of that kind; the clarified provider-managed caching choice does not change workspace teardown or introduce application response storage.

Provide inline account actions **Start new workspace** and **Sign out**, avoiding a bespoke account menu. If both pages are empty, reset immediately. Otherwise use an accessible native modal dialog: heading `Start a new workspace?`; body `Source text, results, and workspace settings in this tab will be cleared. This cannot be undone.`; buttons `Cancel` (initial focus) and `Start new workspace`. Escape/Cancel preserves state and returns focus to its trigger. Confirm clears both pages, restores defaults, navigates to `/translate`, and focuses Source text. It submits no operation.

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
| Focus | 2 px solid `color-focus`, 2 px offset | All keyboard-focusable elements |
| Shadow | `0 8px 24px rgba(23,33,43,.12)` | Dialogs only |
| Motion | 120 ms color/border; 160 ms opacity/transform, ease-out | Removed under `prefers-reduced-motion: reduce` |
| Icons | 20 px, 1.75 px stroke, rounded monochrome SVG | Always paired with an accessible name; decorative icons hidden |

Disabled text uses `#7C8794` on `#EEF1F4`; disabled state also removes pointer affordance and is exposed programmatically. Placeholder text is `#6C7784` (4.56:1 on white). No state is communicated by color alone.

Use `#788594` for textbox/control outlines that are necessary to identify a control; the lighter panel border is decorative. Authentication content is 400 px wide maximum with 24 px vertical field gaps, 48 px inputs/buttons, and 16 px page gutters on mobile; its top margin is 48 px desktop/24 px mobile. LinguaDesk branding is the text wordmark at 20/28 px, weight 700; no external logo asset is required.

### 4.2 Layout

| Width | Current composition |
| --- | --- |
| ≥1200 px | Max width 1280 px, 32 px outer gutters; two equal editor columns with 16 px gap |
| 768–1199 px | 24 px gutters; two equal editor columns |
| 320–767 px | 16 px gutters (12 px below 360); one column: mode links, selectors, source/action, result/copy, usage/status |

Both features use the same shell. Rewriting's single native Writing mode select stays inline above its source at every width; no tools rail/sheet or responsive duplicate controls. Header is at least 64 px desktop / 56 px mobile and wraps/grows as needed without clipping. Main content starts 24 px below mode navigation. Editors are at least 420/360/240 px tall at wide/compact/mobile widths. Desktop bodies scroll after `min(60dvh, 640px)`; mobile editors grow to 360 px then scroll. Toolbars never overlay text. Preserve ordinary page scrolling and caret visibility when the software keyboard opens.

```text
Translation / Rewriting       Usage       New workspace / Sign out
[Source language v] [Target language v]   — Translation
[Writing language v] [Writing mode v]    — Rewriting
[Source text                 ] [Result text                  ]
[count / limit] [Clear] [Translate or Rewrite] [Copy result]
[validation / result status / usage outcome]
```

At narrow widths the result follows the source/action. Source and result use plain native textareas with visible labels. Long English/Cyrillic/Chinese content and errors wrap without horizontal page overflow. Native select popup rendering is platform-owned; no pixel matching of its open menu is required. Do not implement a rich-text editor, syntax/diff overlay, sentence hit targets, or custom caret mapping.

## 5. Components and keyboard behavior

| Component | Current contract |
| --- | --- |
| Mode navigation | Native links Translation / Rewriting with current-page state |
| Source selector | Native select named Source language on Translation, Writing language on Rewriting; Detect automatically plus four languages |
| Target selector | Native select named Target language; explicit Choose target language placeholder; four languages, same known source disabled |
| Writing mode | Native select named Writing mode; Correction only, Simple, Casual, Business, Academic, Enthusiastic, Friendly, Confident, Diplomatic; exactly one selection |
| Source editor | Textarea named Source text; canonical count, visible limit, Clear source text button; remains editable during work |
| Submit | Native button named Translate or Rewrite, labelled Translating… / Rewriting… while submitted; one activation starts at most one logical operation |
| Result editor | Textarea named Translation result or Improved result; empty placeholder read-only, complete result editable even during an update; editing protects it from earlier responses |
| Copy | Copy result; disabled for empty text, copies exact current plain value; Copied for two seconds on success, failure preserves result and offers manual copy |
| Usage | Inline authoritative user count/allowance/reset and availability messages; no global spend, provider/model/rule controls |
| Account forms | Visible email/password/confirmation labels, native submit, password reveal, field errors and summary; no custom auth widgets |

Tab order: skip link; wordmark/home; mode links; account actions; source language; target language or Writing mode; Source text; Clear source text; Translate/Rewrite; result; Copy result; actionable recovery links. Usage text is readable in document order and creates no unnecessary tab stops. Disabled controls are skipped. Exactly one visible `main` and `h1`; editor headings are `h2`.

Use platform keyboard behavior for native selectors, text selection, undo/redo, clipboard and IME. Enter inside a textarea inserts a newline, never submits. Native button activation submits only when not composing; no special processing shortcut is required in MVP. If activation occurs during composition, ignore it without queueing a later submission. Native dropdown Escape/commit behavior is platform-owned, replacing the old bespoke listbox/active-descendant contract.

Keep the submit control focusable while busy using `aria-disabled` and an activation guard; prevent repeat click/Enter from dispatching. Known invalid input disables submission with associated visible helper text. Moving focus after explicit route changes, submit validation, dialog open/close and account continuation is allowed; async completion/usage/background validation never steals focus or moves the source caret.

## 6. Processing state and message contract

See [UX processing and message contract](ux/processing.md#6-processing-state-and-message-contract).

### 6.1 Explicit submission and competing events

See [UX processing and message contract](ux/processing.md#61-explicit-submission-and-competing-events).

### 6.2 Response ordering and recovery

See [UX processing and message contract](ux/processing.md#62-response-ordering-and-recovery).

### 6.3 Exact message IDs and disposition

See [UX processing and message contract](ux/processing.md#63-exact-message-ids-and-disposition).

### 6.4 Announcements

See [UX processing and message contract](ux/processing.md#64-announcements).

## 7. Translation contract

- Initial source is Detect automatically; target is Choose target language. Chinese is labelled `Chinese (Simplified output)` and accepts both Chinese scripts. Source detection occurs within explicit submission; a manual choice does not waive supported-content validation.
- Target lists the four PRD languages. Disable the known source language as target; if a later source change equals an already selected target, retain the invalid value with MSG-007 until corrected. Selecting a valid target enables Translate but sends no operation.
- Count complete input using #5's Unicode contract. At or below the configured limit, Translate submits all entered text. Above it, show MSG-045, retain text/caret and previous result, and block submission. The backend rejects oversized independent-client requests too. There is no boundary marker, suffix omission, or partial-result state.
- Copy returns the exact current result including manual edits/newlines; failure preserves the result selection and offers native selection/copy. Empty source does not erase an existing result; Clear source text preserves that result with `Source cleared — previous result shown.` and sends no operation.

## 8. Rewriting contract

The page uses Writing language plus one native Writing mode dropdown from Section 5. Correction only is the default and makes minimal grammar/spelling corrections without discretionary restyling. Selecting any of the eight styles/tones preserves mandatory correction. There is no second checkbox, None set value, disabled-style restoration or Show changes preference.

At or below the configured rewriting limit, explicit Rewrite processes the entire source. Above it, MSG-011 blocks processing and retains all text. Selecting a mode, correcting length, changing language, or finishing IME composition never submits by itself.

A successful current operation returns a complete plain result; source stays untouched. Manual result editing is ordinary native editing, creates no charge/request, and protects against older responses. A later explicit rewrite can replace the complete edited result. There is no sentence segmentation, identity mapping, cached alternative, comparison view, deletion annotation, or sentence shortcut in the MVP frontend/API contract.

DF-001/DF-002 later design must invalidate **all** assistance metadata on any manual result edit. It must not revive targeted sentence preservation, and neither deferred capability is required to implement this plain-text editor.

## 9. Local-account and availability flows

Registration contains Email, Password, Confirm password, actual policy checklist, Show password, Create account, and Sign in. Login contains Email, Password, Show password, Sign in, Create account and Forgot password. Password reveal preserves selection. Invalid local submission sends no request, shows `Check the highlighted fields.` with linked errors, focuses the first invalid field, and exposes `aria-invalid`/descriptions. A current valid submission disables fields and guards duplicate click/Enter. Failed credential/policy/network responses clear password values and permit explicit recovery; keep email only in this in-memory auth journey.

Registration success opens verification with MSG-034 and the in-memory email. Resend has a server-provided cooldown/countdown, announced success/failure, and no language operation. `I’ve verified my email` reads auth status only. Valid link success offers explicit continuation (sign-in first when signed out); invalid/expired link shows MSG-032 with a resend path. No verification action starts language processing.

Forgot password returns identical MSG-031 for known/unknown accounts. Reset uses current policy and confirmation; success shows MSG-036 and Go to sign in, never auto-login. Invalid/expired links show MSG-033 and no submit fields. Email delivery failure shows MSG-044 without account disclosure. Generic account network failure: `We couldn’t complete this request. Try again.` No Google copy/helper or linking screen exists.

Old auth completions cannot navigate over a newer auth route or restore cleared passwords/text. Reconcile actual account status on the next protected navigation. Keep safe return paths only, not arbitrary external redirects.

A failed usage refresh does not turn a successful transformation into failure: keep its result, label the counter `Usage update unavailable`, and offer `Refresh usage`, which sends only a read. `Check availability` similarly performs one status read while global/budget suspension persists, never an automatic retry loop.

Usage shows the user's authoritative consumed/remaining allowance and UTC reset; global/budget amounts and provider routing remain private. Exhaustion uses MSG-015/016/017 and disables processing; text/edit/copy remain usable. An operation requiring more than remaining allowance shows `This request needs {required} characters, but you have {remaining} remaining today.` Shortening or restored capacity reenables the action without submission. Validation plus offline/availability messages may coexist; reconnect does not bypass server limits. Unknown usage/status triggers a read, never a speculative charge or paid retry.

## 10. Accessibility and browser coverage

Target [WCAG 2.2 AA](https://www.w3.org/TR/WCAG22/) with native semantics and keyboard access. Keep 44×44 px standalone touch targets where possible (24×24 minimum except valid inline exceptions), 4.5:1 ordinary text / 3:1 large text, and 3:1 focus/control boundaries. Use a 2 px focus ring with 2 px offset. Errors/status use text as well as color. The dialog traps focus, Escape cancels, and close returns to its trigger. All other controls keep native behavior; no custom active-descendant or sheet-focus system is required.

At 200% zoom all actions remain available; at effective 400% / 320 CSS px there is no horizontal page overflow. Text-spacing overrides (1.5 line height, 2× paragraph spacing, .12em letter and .16em word spacing) cause no clipping. Reduced motion removes transitions/shimmer. Native Chinese composition, selection and undo/redo remain intact; explicit activation during composition is ignored without a delayed submission. Do not move focus on completion or scroll a background result into view.

### 10.1 Supported browsers

Support current and immediately previous stable major releases at each release candidate for Chrome, Edge, Firefox, desktop Safari, iOS Safari and Android Chrome. [Verification plan Section 4](verification/frontend.md#4-frontend-browser-and-manual-verification) owns automated lane allocation, actual-version evidence and manual browser/device/assistive-technology procedures. Bundled engines alone do not establish this support contract.

### 10.2 Verification ownership

The accessibility outcomes above remain UX contracts. Automated checks and required manual evidence are consolidated in [verification plan Sections 4.2–4.4](verification/frontend.md#42-browser-lanes); deferred journeys create no MVP evidence requirement.

## 11. User stories and disposition

| Story | Status | User outcome | Product scope |
| --- | --- | --- | --- |
| UX-US-001 | MVP | Navigate between Translation and Rewriting while preserving the active workspace | FR-003/038 |
| UX-US-002 | MVP | Register a local account and verify its email | FR-001/002 |
| UX-US-003 | Amended | Sign in locally and recover cleanly from authentication errors; Google deferred | FR-001/037; DF-007 |
| UX-US-004 | MVP | Reset a forgotten local password | FR-002 |
| UX-US-005 | Amended | Choose language/mode and validate on explicit submission | FR-004–006/014 |
| UX-US-006 | Amended | Translate on explicit activation and receive a complete result | FR-008–011 |
| UX-US-007 | Amended | Understand input rejection, whole-input limits and no-charge failures | FR-004/005/007/024 |
| UX-US-008 | MVP | Edit/copy translations and reject outdated responses | FR-011/023/026/028 |
| UX-US-009 | Amended | Choose one correction/style/tone mode and explicitly rewrite | FR-012–016 |
| UX-US-010 | Amended | Preserve source and edits while an explicitly submitted rewrite completes | FR-017/018/022/023 |
| UX-US-011 | Amended | Edit and copy the plain rewrite result; change review is deferred | FR-023; DF-002 |
| UX-US-012 | Deferred | Refine a sentence with reusable alternatives | DF-001 |
| UX-US-013 | Amended | Protect manual result edits from older responses; targeted metadata preservation retired | FR-018/023 |
| UX-US-014 | MVP | See authoritative usage and availability | FR-024–028 (FR-025 deferred) |
| UX-US-015 | Amended | Recover explicitly from failure, offline periods and unknown outcomes | FR-034/037 |
| UX-US-016 | Amended | Use native controls and current core journeys with keyboard/touch/assistive technology | NFR-005/007 |
| UX-US-017 | MVP | Reset and tear down the per-tab workspace without restoration | FR-038/NFR-004 |

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

Every selected UI-facing feature must have a published end-to-end case executed as #6's dedicated verified synthetic user. At least one suite case performs the visible Email/Password/Sign in flow; subsequent feature cases may begin from the documented authenticated fixture, but they still exercise the real protected route and application API and must assert the resulting controls, messages, text and usage in the UI. A successful request observed only in network logs is not UX acceptance.

## 14. MVP traceability

The sole cross-document [product and release coverage matrix](verification/coverage.md#81-product-and-release-coverage) now lives in #6. This document retains UX behaviors, stories and scenario IDs; it does not maintain a second product-to-evidence table.

## 15. Decisions, handoffs, and deferred reactivation

UX-D-001–UX-D-007 remain stable historical decision IDs. Their current disposition is explicit below; obsolete text is preserved in Git `54343c3`, not an active alternative contract.

| Decision | Current disposition |
| --- | --- |
| UX-D-001 / Q-009 | Amended: current native-control/explicit-processing/browser/accessibility contract in this document |
| UX-D-002 / Q-002 | Retired: correction-toggle restoration is removed; single mode replaces it |
| UX-D-003 / Q-002 | Retired: targeted sentence correspondence is cut, including future default behavior |
| UX-D-004 / Q-002 | Retained/amended: manual edits protect both plain result fields from earlier responses; usage may still settle |
| UX-D-005 / Q-004 | Retained: one verified tab lifetime and no durable text restoration |
| UX-D-006 | Amended: plain Translation result remains editable during processing, protected by result-edit revision |
| UX-D-007 | Retained: server-authoritative usage and explicit stale-success disclosure, no provider/global/budget amounts |
| UX-D-008 | Accepted 2026-09-08: PRD D-17 replaces affected prior UX; DF register controls later scope, no dormant MVP widgets/tests |

API design #5 now defines canonical scalar counting, operation identity/recovery, ordered usage, cookie/bearer lifecycle, error semantics, cancellation and rollover. Selected slices still fix exact wire operations/fields, auth policy/bootstrap/delivery details and the concrete recovery UI mapping before dependent handlers/clients. OpenAPI is generated and reviewed early in implementation, not hand-authored during planning. #3/#4 own serving capability, cost bounds and the simple family chains' quality/output/deadline checks; provider retention/no-training certification is not a prerequisite. Provider-managed caching changes no current UI, character accounting or application text-lifecycle contract. #6 supplies current-scope evidence mapping. Q-002 targeted matching and Google linking no longer block MVP. Advanced routing and sentence context are deferred dependencies only.

Deferred work is selected through PRD Section 3.1 and the existing #8 package workflow; no new full deferred implementation spec is authored now. At reactivation, refine only the selected capability, reassess affected historical UX IDs, and update its owning contracts/tests. Custom visual tools do not restore obsolete toggles or targeted sentence preservation.

## 16. Validation and readiness

This revision replaces the active v1.1 UI contract to remove obsolete branch/timer/sentence obligations instead of hiding them behind an amendment. All original story/scenario/message IDs have an explicit current disposition; additional MSG-045–047 describe current oversize and explicit-action states. No scenario is a claim of a passing test.

Ready for current-MVP scoped planning. Resolve only the selected slice's Section 15 dependencies before implementation. Release evidence, provider evaluation, actual browser/AT behavior and runtime verification remain pending. Deferred/retired rows neither block MVP nor count as passed release checks.
