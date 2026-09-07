# LinguaDesk — UX/UI Specification

**Document:** #2 · **Version:** 1.1 · **Status:** Ready for implementation planning; runtime verification pending · **Updated:** September 7, 2026 (UTC)

**Owner:** UX/UI specification. This document resolves Q-009 and the UX-owned portions of Q-002 and Q-004. Product policy remains owned by [document #1](01-PRD.md); architecture, persistence mechanics, API schemas, model behavior, and product-wide verification remain owned by documents #3–#6.

## 1. Authority, inputs, and scope

### 1.1 Authoritative inputs

| Input | Recorded revision | Role |
| --- | --- | --- |
| [SDD Planning Workflow](00-SDD-Planning-Workflow.md) | Version 1.0, updated 2026-09-06; SHA-256 `fda111d59938d6b25013cd7a3ede7b47fb7e419fae88a0ec93243325638d7edc` | Process, ownership, traceability, and readiness authority |
| [Product Requirements Document](01-PRD.md) | Version 0.2, product baseline dated 2026-09-06; SHA-256 `0f3a0f7326e42bbc0718ed873bba2e0180ca3984fd5c5b9289e514e2e519916c` | Product intent and accepted requirements |
| Repository revision | Git commit `430f474d83a079defb91ef3398402301b8678b5a` (`Resolved contradictions`, 2026-09-06T18:21:44+03:00); clean worktree before this document was created | Reproducible authoring baseline |
| Existing UX draft at continuation | Git commit `bb231f1db7bb704eeae1d09e5af78daf8f40e0ef` (`UX specification draft`); upstream input hashes unchanged | Preserved existing valid content and stable IDs while completing review |

Document #0's statement that the PRD is a review draft is stale status information. The PRD's current baseline status and the recorded dispositions of P-001–P-004 govern. P-005 and P-006 remain proposals and are not introduced by this specification.

### 1.2 Normative language and terminology

`Must` is binding UX behavior for the MVP. `Should` is the chosen default unless an owning downstream specification records a justified exception without changing product policy. `May` is permitted behavior, not required behavior. Exact quoted interface text is normative English copy.

| Term | Meaning in this document |
| --- | --- |
| Workspace | The in-memory text, settings, results, sentence metadata, and pending-operation state owned by one authenticated browser tab. It is not saved history. |
| Workspace session | The lifetime defined in Section 3.4. It is distinct from the account's authentication lifetime. |
| Current operation | The latest submitted operation whose source, applicable settings, and result-edit revision still match the current workspace. |
| Outdated operation | A submitted operation superseded by newer source/settings, sentence selection, navigation teardown, or a protected manual rewrite-result edit. |
| Previous result | The last complete result still displayed while a newer operation is waiting or processing. |
| Affected sentence | A result sentence whose text or boundary was manually changed, plus every sentence touching a changed boundary. |
| Clean text | Current visible result text without highlight, deletion, control, or status markup. |

### 1.3 Scope and boundaries

This document specifies all user-visible MVP pages, responsive layouts, components, states, copy, keyboard/focus behavior, and deterministic UI acceptance contracts for FR-001–FR-028, FR-034, FR-037–FR-038, NFR-003–NFR-007, and the visible consequences of the remaining confirmed requirements. It does not add file/voice translation, saved history, billing, admin UI, Apple sign-in, SSO, dictionaries, glossaries, text-to-speech, feedback voting, or routing controls.

The following are intentionally left to their owning specifications and do not block coherent UI implementation with fixtures:

- Document #3: authentication/session transport, secure teardown, temporary-memory mechanics, account linking/deletion, metadata retention, provider disclosure, and monetary-cap enforcement.
- Document #4: provider/model selection, prompts, output validation, fallbacks, and diagnostic classification.
- Document #5: request/response schemas, opaque revision/sentence identifiers, authoritative character counting, usage fields, retry/cancellation semantics, and error codes.
- Document #6: real integration evidence, language quality, provider performance, complete assistive-technology assessment, and product-wide coverage/evidence.

## 2. DeepL reference research

### 2.1 Inspection record

Research was conducted on September 7, 2026 in an anonymous, signed-out browser. Synthetic text only was entered. Desktop inspection used `1440 × 900` CSS px (the initial Write capture was `1280 × 720`); mobile inspection used `390 × 844` CSS px with the viewport checked in the page. Direct application URLs were [DeepL Translator](https://www.deepl.com/en/translator), [DeepL Write](https://www.deepl.com/en/write), [DeepL login](https://auth.deepl.com/login), [DeepL sign-up](https://auth.deepl.com/signup), and [DeepL password reset](https://auth.deepl.com/passwordreset).

| Evidence | Classification | Relevant observation |
| --- | --- | --- |
| Translator, empty, desktop and mobile | Directly observed | Compact product-mode controls; source/target language row above editor; paired desktop editor regions; compact header and stacked mobile content. |
| Write, empty/result, desktop and mobile | Directly observed | Source and improved-result editors; language row; desktop editing-tools surface; mobile stacked cards; result action icons. |
| Synthetic Write correction | Directly observed | `Yesterday I go…` produced a complete corrected result; changed words were green and underlined while source remained intact. This is evidence of presentation only, not a LinguaDesk quality result. |
| Word/sentence interaction | Directly observed | Activating a changed word selected the related sentence and exposed actions named `Revert to original sentence`, `Rephrase sentence`, `Replace word`, and `Close`. |
| Public authentication | Directly observed | Centered, single-column forms; Google/Apple/SSO before email/password; password visibility; forgot-password and sign-up links; email-only reset-request form. |
| Processing and some popovers | Limitation | Cloudflare challenge intermittently overlaid the anonymous app. A correction result was observed, but error, long-input, alternative-result, authenticated, and email-verification states were not directly accessible. No behavior below is attributed to DeepL unless separately documented. |
| Keyboard, editing, copy, and comparison completion | Limitation | Visible names/roles and an action menu were inspected; selector expansion, stable keyboard traversal, actual clipboard write, alternative selection, deleted-text comparison, real mobile IME/software keyboard, and backend error recovery were not established. The mobile run was CSS viewport emulation, not a physical device. LinguaDesk behavior for these states is a design decision, not a DeepL observation. |
| [Use DeepL Write](https://support.deepl.com/hc/en-us/articles/11673757647388-Use-DeepL-Write) | Official documentation | Paired source/improved text, automatic improvement on web, green change marks, Show changes, alternatives, and a copy action. |
| [Customize your text with DeepL Write](https://support.deepl.com/hc/en-us/articles/9710730337820-Customize-your-text-with-DeepL-Write) | Official documentation | Four named writing styles, four tones, menu-based application, alternative phrasing, and platform/language limitations. |
| [Select alternatives](https://support.deepl.com/hc/en-us/articles/4407359201938-Select-alternatives) | Official documentation | Word/phrase alternatives and a sentence-rephrase action; options may be in a dropdown and can change sentence structure. |
| [Reset your password](https://support.deepl.com/hc/en-us/articles/360020693260-Reset-your-password) | Official documentation | Email-request/link/new-password journey; email is sent when an account exists; social-account passwords are handled by their provider. LinguaDesk's non-enumerating UI confirmation is a design choice, not an observed DeepL state. |

### 2.2 Pattern disposition

| Significant reference pattern | Disposition | LinguaDesk rationale |
| --- | --- | --- |
| Translation/Rewriting mode switch near the workspace | **Adopt** | Makes the two principal tasks prominent while preserving separate routes required by FR-003. |
| Language controls above large paired editors | **Adopt** | High scanability and direct source/result relationship. |
| Source left/result right on wide screens; stacked on narrow screens | **Adopt** | Familiar editor ergonomics, adapted to deterministic breakpoints in Section 4. |
| Editing tools in a right rail or compact sheet | **Adapt** | Only PRD-authorized Correction only, Styles, and Show changes are included. |
| Green result-only change emphasis and contextual sentence actions | **Adapt** | LinguaDesk uses accessible non-color cues, comparison details, and stable manual-edit invalidation rules. |
| Immediate/automatic processing | **Adapt** | LinguaDesk uses the accepted one-second pause, complete results, IME rules, previous-result status, and accounting disclosures. |
| Broad language catalog, dictionary, files, speech, glossary, formality, terms, TTS, feedback, and cross-product actions | **Omit** | Not authorized by the PRD. |
| DeepL language/platform limitations for style or sentence alternatives | **Omit** | LinguaDesk must cover its four rewriting languages per FR-012 and FR-020. |
| DeepL authentication's Apple and SSO choices and marketing consent | **Omit** | LinguaDesk authorizes Google and local accounts only. |
| Centered, narrow authentication forms with provider option first | **Adapt** | Retains clarity with only Google and local flows, LinguaDesk copy, verification, and recovery. |
| Reference branding, logo, exact colors, and marketing content | **Omit** | LinguaDesk uses its own visual tokens and product scope. |

## 3. Information architecture, routes, and lifecycle

### 3.1 Route inventory

| Route | Access state | Entry and visible outcome |
| --- | --- | --- |
| `/` | Any | Verified account → replace-navigate to `/translate`; signed out → `/login`; unverified local account → `/verify-email`. |
| `/translate` | Verified | Translation workspace and `Translation` mode link marked current. |
| `/rewrite` | Verified | Rewriting workspace and `Rewriting` mode link marked current. |
| `/login` | Signed out | Google action, email/password form, links to registration and recovery. Verified users replace-navigate to `/translate`. |
| `/register` | Signed out | Google action and local email/password registration. |
| `/verify-email` | Unverified local account or verification link outcome | Verification instructions, current email when safely available, resend action, and verification-link states. |
| `/forgot-password` | Signed out | Email request form. |
| `/reset-password` | Valid/invalid reset-link context | New-password form or expired/invalid-link recovery state. Token representation is document #5's concern and is never shown. |
| `/auth/google/callback` | Transient | Busy state `Signing you in…`; success replace-navigates to the originally requested protected route, otherwise `/login` with an error. Exact protocol is not defined here. |
| Any unmatched route | Any | Page heading `Page not found`; `Go to Translation` when verified, otherwise `Go to sign in`. |

There is no combined transformation page. Header mode links are ordinary route links, not an ARIA tablist. The current link uses `aria-current="page"`.

For `/login`, `/register`, and `/forgot-password`, a confirmed verified session redirects to the requested protected route or `/translate`; a confirmed unverified session redirects to `/verify-email`. Link-outcome routes remain viewable for their explicit verification/reset purpose. The pending/failed sign-out page below suppresses this automatic redirect until sign-out resolves or the user deliberately starts a new auth journey.

### 3.2 Primary navigation

The authenticated header contains, in order: LinguaDesk home link, flexible space, usage disclosure button, and account-menu button. At every width, the two mode links occupy one segmented row below this header. Usage and account remain icon/text buttons in the header. There is no hamburger-only path to a core mode.

There is exactly one ModeNav. The home link navigates to `/translate` using the same preservation rules as ModeNav. Initial bootstrap and every route transition focus the `h1`; only explicit `Start new workspace`/Clear actions focus Source text. Initial page load does not summon the mobile keyboard.

`Translation` and `Rewriting` use client-side navigation. Switching mode preserves both page states in the current workspace, closes menus/dialogs, places focus on the destination page's `h1`, and does not start a request merely because of navigation. If a destination page already has valid changed input waiting on its debounce, returning does not restart the timer; the original controlled-clock deadline remains. Pending operations continue while the other route is visible and may update only their matching hidden page state.

### 3.3 Direct navigation, history, and refresh

- Direct navigation to a protected route first resolves authentication. After successful login, return to that exact route; after verification, return to the last requested protected route, otherwise `/translate`.
- Browser Back/Forward within the SPA changes routes and restores the current in-memory page state and scroll position. Focus moves to the restored page's `h1` after popstate navigation.
- Refresh is a workspace boundary. After authentication is re-established, the requested route opens with empty editors and new-workspace defaults. No prompt to restore text appears.
- Closing or duplicating a tab does not transfer workspace text. A duplicated tab opens a new empty workspace.
- Navigating to an external site or a full document unload ends the workspace. The browser's back-forward cache must not expose prior source/result text; on pageshow restoration the app clears the workspace and renders a new one.

### 3.4 Workspace and session decision (Q-004 UX portion)

For observable UX, one active workspace exists per verified browser tab from protected-page bootstrap until the earliest of: `Start new workspace` confirmation, full reload/unload, tab close, sign-out, authentication expiry, or account invalidation. Translation and Rewriting are two views within that workspace.

Settings and text live only in runtime memory. They must not be placed in URL parameters, browser history state payloads, local/session storage, IndexedDB, service-worker caches, autofill values, or analytics. Architecture defines teardown mechanics; a different observable workspace boundary requires coordination back to this owning specification. Backgrounding a tab, switching modes, an offline interval, and a processing failure do not end the workspace.

A persistent workspace footer explains: `Text and settings are cleared when this workspace ends, including refresh, sign-out, or session expiry.` This describes client-visible lifetime, not provider retention. Provider disclosure facts remain with Q-004/#3.

`Start new workspace` is in the account menu. If both feature pages are empty and have no result, activation resets immediately. Otherwise it opens a dialog:

- Heading: `Start a new workspace?`
- Body: `Source text, results, and workspace settings in this tab will be cleared. This cannot be undone.`
- Buttons: `Cancel` and `Start new workspace`.

Confirmation invalidates all pending responses, clears both feature pages and usage-event notices (not account usage), restores FR-038 defaults, navigates to `/translate`, and focuses `Source text`. Sign-out uses the same teardown without a second workspace dialog because its account-menu label and action are explicit. Authentication expiry immediately clears content and replace-navigates to `/login`; the login page shows `Your session expired. Sign in again to continue.` No source excerpt appears in the message.

Sign-out immediately shows `/login` with `Signing out…` while its one logical auth action is pending; no editor remains. On failure show `Workspace cleared. Sign-out could not be confirmed. Try again.` with `Try sign-out again`; stay on login without automatic authenticated redirection. Retry sends an auth action only. Success clears the status and exposes the ordinary form. Account-session revocation mechanics remain #3/#5; the interface never falsely claims a successful server sign-out.

### 3.5 Authentication-state matrix

| State | Protected route | LLM controls | Header/account state |
| --- | --- | --- | --- |
| Signed out | Redirect to `/login` | Not rendered | LinguaDesk mark only on auth pages |
| Unverified local | Redirect to `/verify-email` | Not rendered | Email and `Sign out`; resend permitted |
| Verified local/Google | Render requested workspace | Enabled subject to validation/availability | Usage and account menu |
| Expired/revoked session | Clear workspace, redirect to `/login` | Removed immediately | Session-expired banner; no stale content |

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
| `color-change-bg/text` | `#DDF7EC` / `#0A5B43` | Inserted/revised text; 7.18:1 |
| `color-delete-bg/text` | `#FDE7E7` / `#8C2E2E` | Deleted comparison text; 6.98:1 |
| Spacing | 4, 8, 12, 16, 24, 32, 48, 64 px | Use only this scale |
| Control heights | 36 compact, 44 default, 48 touch/form px | Never below 44 px for standalone mobile targets |
| Radius | 6 controls, 10 panels, 14 dialogs/sheets, 999 pills px | Consistent containment |
| Border | 1 px solid `color-border`; selected 1 px `color-action` | Panels and controls |
| Focus | 2 px solid `color-focus`, 2 px offset | All keyboard-focusable elements |
| Shadow | `0 8px 24px rgba(23,33,43,.12)` | Menus, sheets, dialogs only |
| Motion | 120 ms color/border; 160 ms opacity/transform, ease-out | Removed under `prefers-reduced-motion: reduce` |
| Icons | 20 px, 1.75 px stroke, rounded monochrome SVG | Always paired with an accessible name; decorative icons hidden |

Disabled text uses `#7C8794` on `#EEF1F4`; disabled state also removes pointer affordance and is exposed programmatically. Placeholder text is `#6C7784` (4.56:1 on white). No state is communicated by color alone.

Use `#788594` for textbox/control outlines that are necessary to identify a control; the lighter panel border is decorative. Authentication content is 400 px wide maximum with 24 px vertical field gaps, 48 px inputs/buttons, and 16 px page gutters on mobile; its top margin is 48 px desktop/24 px mobile. LinguaDesk branding is the text wordmark at 20/28 px, weight 700; no external logo asset is required.

### 4.2 Grid and breakpoints

| Range | Layout |
| --- | --- |
| `≥ 1200 px` wide desktop | Max content width 1280 px; 32 px outer gutters. Translation: two equal editor columns with 1 px shared divider. Rewriting: two editor columns (`minmax(0,1fr)` each) plus 280 px tools rail; 16 px gaps. |
| `768–1199 px` compact desktop/tablet | 24 px gutters. Two equal editor columns. Rewriting tools open as a 360 px right-side sheet; trigger sits beside Writing language above the editors. |
| `320–767 px` mobile/reflow | 16 px gutters (12 px at 320–359). One column: mode row, language/settings row, source, result, status. Tools and sentence details use full-width bottom sheets. No horizontal page scroll. |

Representative visual-test viewports are `1440×900`, `1024×768`, `390×844`, and `320×640`. At 200% zoom on a 1280 px-wide desktop, the effective 640 CSS px layout uses the mobile/reflow composition. At 400% zoom the 320 CSS px composition has no two-dimensional scrolling, consistent with [WCAG 2.2 SC 1.4.10 Reflow](https://www.w3.org/TR/WCAG22/#reflow).

The header is 64 px high on desktop and 56 px on mobile. Main content begins 24 px below the mode row. Editors are at least 420 px high on wide desktop, 360 px on compact desktop, and 240 px each on mobile. On desktop each editor body scrolls after `min(60dvh, 640px)`; on mobile each editor grows to 360 px then scrolls internally. Editor toolbars stay at the bottom of their panel but never overlay text. The page itself remains scrollable when the on-screen keyboard reduces the visual viewport.

### 4.3 Wireframes

Wide Translation (`1440×900`):

```text
┌ LinguaDesk ───────────────────────────────── Usage ▾  Account ▾ ┐
│ [Translation] [Rewriting]                                         │
│ ┌ Detect language ▾ ───────────┬ Target language ▾ ─────────────┐ │
│ ├ Source text                  │ Translation result              │ │
│ │                              │ [status: Up to date]            │ │
│ │ editable source             │ editable complete result        │ │
│ │                              │                                 │ │
│ ├ warning/error + boundary    │                                 │ │
│ └ 1,248 / 5,000     Clear     ┴ Copy                         ───┘ │
└───────────────────────────────────────────────────────────────────┘
```

Wide Rewriting (`≥1200`):

```text
┌ LinguaDesk ───────────────────────────────── Usage ▾  Account ▾ ┐
│ [Translation] [Rewriting]                                         │
│ ┌ Automatic ▾ ─────────┬──────────────────────┬ Editing tools ──┐ │
│ │ Source text          │ Improved result      │ Correction only │ │
│ │ editable source      │ editable sentences   │ [off]           │ │
│ │                      │ revised words marked │ Styles          │ │
│ │                      │                      │ [None set     ▾]│ │
│ │                      │ [comparison/details] │ Show changes    │ │
│ └ count ───────────────┴ status ─ Copy ───────┴ [on] ───────────┘ │
└───────────────────────────────────────────────────────────────────┘
```

Mobile (`390×844`), both modes:

```text
┌ LinguaDesk                         Usage  Account ┐
│ [ Translation ] [ Rewriting ]                    │
│ [source language ▾] [target ▾ / Writing tools]  │
│ ┌ Source text ─────────────────────────────────┐ │
│ │ editable text; internal scroll after 360 px  │ │
│ └ count / validation / Clear ──────────────────┘ │
│ ┌ Result ──────────────────────────────────────┐ │
│ │ status; editable result                     │ │
│ │ changes / selected sentence                 │ │
│ └ Sentence actions     Show changes     Copy ─┘ │
│ [inline availability or accounting message]     │
└─────────────────────────────────────────────────┘
```

Authentication (`320–1440 px`):

```text
┌ LinguaDesk ──────────────────────────────────────┐
│               Sign in to LinguaDesk              │
│               [ Continue with Google ]           │
│               ───────── or ───────────            │
│               Email                              │
│               [____________________]              │
│               Password                 Show      │
│               [____________________]              │
│               Forgot password?                   │
│               [ Sign in ]                         │
│               New to LinguaDesk? Create account  │
└───────────────────────────────────────────────────┘
```

### 4.4 Long and international content

- Source/result preserve plain-text newlines, list indentation, URLs, and all Unicode glyphs; `white-space: pre-wrap`, `overflow-wrap: anywhere`, and language-appropriate line breaking are required. Do not hyphenate names or URLs automatically.
- Chinese content uses the same 16/24 px editor typography with `Noto Sans SC` fallback and no artificial letter spacing. IME composition is never interrupted, counted as committed input, or submitted until `compositionend`.
- Language and style labels wrap to two lines only in menus; closed controls truncate with an ellipsis after one line and expose the full selected value programmatically and on focus/hover tooltip. The control's accessible name remains its stable field label. The four supported base language names fit at 320 px without abbreviation; the Chinese output qualifier can truncate with its complete description available.
- When the mobile keyboard opens, keep the focused editor/caret visible using the visual viewport, do not scroll the result into view on background completion, and keep bottom sheets above the keyboard. Escape closes a hardware-keyboard sheet; the mobile close button is always visible.
- Selection and caret remain stable when status, counter, highlight overlay, or usage changes. Automatic processing never steals focus.

## 5. Component contract

### 5.1 Shared interaction states

Every interactive component must implement the applicable states below. These rules supplement, rather than replace, the component-specific behavior in Sections 5.2–10.

| State | Visual and programmatic contract |
| --- | --- |
| Default | White or transparent surface, `color-text`, 1 px neutral border where bounded. Name, role, and current value are programmatically determinable. |
| Hover | Pointer-only background `#EEF4FF` or border `#9CB8E8`; no layout shift and no information available only on hover. |
| Focus-visible | Two-pixel `color-focus` ring with 2 px offset. The focused control is not obscured by sticky content or a sheet. |
| Active/pressed | Background `#DCE9FF`; buttons expose `aria-pressed` only when they are true toggles. |
| Selected/current | Brand-tinted background, action-colored 2 px inset edge/checkmark, and programmatic selected/current state. |
| Disabled | Muted foreground/background, no hover change, `disabled` or `aria-disabled="true"`; tooltip/helper says why when the reason is not obvious. |
| Loading | Existing label remains, followed by an 16 px spinner and specific status text; repeated activation is blocked. A global cursor spinner is forbidden. |
| Error | Error icon plus text, error border, `aria-invalid="true"`, and `aria-describedby` connection. Color is supplementary. |

Tooltips appear after 500 ms hover or immediately on keyboard focus, never for disabled form fields whose helper is already visible, and do not receive focus. They remain visible while the pointer is over the trigger or tooltip, close on Escape or leaving both/blur, and are never the sole carrier of required instructions. Escape and explicit Close return focus to the trigger. Outside pointer dismissal leaves focus on the clicked control instead of stealing it back. Modal dialogs trap focus, start on the heading or safest action (`Cancel`), close on Escape unless an irreversible submission is already underway, and return focus to their trigger.

### 5.2 Inventory

| Component | Purpose, content, variants, and accessible contract |
| --- | --- |
| `AppHeader` | Landmark `banner`; home link accessible name `LinguaDesk home`; contains `UsageDisclosure` and `AccountMenu`, followed by one `ModeNav` row. Never overlays focused content. |
| `ModeNav` | Landmark `navigation`, label `Main`; links named `Translation` and `Rewriting`; current route has `aria-current="page"`. Pointer/touch/click and ordinary Enter activation. |
| `AccountMenu` | Button `Account menu, <email>`; menu items `Start new workspace` and `Sign out`. Escape/outside click closes and returns focus. |
| `UsageDisclosure` | Button label such as `Usage: 7,500 of 20,000 characters used today`; opens a nonmodal panel with user usage, remaining characters, and reset time. Global allowance and monetary amount are not exposed. |
| `LanguageSelect` | Select-only combobox. Source variant name `Source language`; target variant `Target language`; writing variant `Writing language`. Listbox supports arrows, Home/End, typeahead, Enter, Escape. Selected option has a checkmark and `aria-selected`. Source includes `Detect automatically`, English, Russian, Romanian, Chinese. Target retains the resolved source disabled with reason text `Same as source`; it is never silently changed when detection/source changes. |
| `EditorPanel` | Region labelled by visible `h2` (`Source text`, `Translation result`, or `Improved result`). A multiline textbox, status line, counter where applicable, and toolbar. The result textbox is empty/read-only before first result; rewriting result becomes editable after success and remains editable while updating. Translation result is temporarily read-only during a submitted update to avoid undisclosed edit replacement. |
| `ClearSource` | Icon button `Clear source text`; enabled only when source contains any character. Clears source after activation without confirmation, preserves the last result as outdated, cancels debounce logically, invalidates pending work, and focuses the empty source. |
| `CopyResult` | Icon/text button `Copy result`; disabled without a result. On success label becomes `Copied` for 2 seconds and polite status says `Result copied to clipboard.` Clipboard receives clean text. On denial/error, alert `Couldn’t copy the result. Select the text and copy it manually.` persists until next copy or dismiss. |
| `StatusLine` | Persistent visual text adjacent to the affected panel; one shared polite, atomic live region announces only meaningful state transitions defined in Section 6.4. Typing/debounce restarts are not repeatedly announced. |
| `CharacterCounter` | Visible `current / limit characters`; description states `Character count`. At/over 90% uses warning icon/text; error at rewrite over-limit. Translation over-limit shows `5,000 processed · <total> entered`. The canonical API count replaces a provisional local count without moving focus. |
| `CorrectionOnly` | Native checkbox styled as a switch, label `Correction only`; off by default. It never disables correction. Helper when on: `Style and tone are ignored while Correction only is on.` |
| `StyleSelect` | Combobox named `Writing style or tone`; grouped listbox headings `No style or tone`, `Writing styles`, `Tones`; exact values from FR-014. Disabled while Correction only is on but continues to display the stored prior value. |
| `ShowChanges` | Native checkbox styled as switch, label `Show changes`; on by default. Toggles presentation only and starts no request. |
| `ChangedText` | Revised spans use change background plus 2 px underline. They are not separately tabbable. The result's accessible text is clean text, not annotations. Pure deletions are discoverable through the sentence action even if no result word is highlighted. |
| `SentenceActions` | Toolbar button `Sentence actions`. Clicking/tapping result text first places the caret normally; a separate sentence affordance opens the actions. Keyboard users place the result caret in a sentence then press `Alt+ArrowDown`, or open the toolbar button and choose a sentence by accessible excerpt. The chosen sentence gets a 2 px outline and is exposed in the sheet heading. |
| `SentenceMenu` | Desktop anchored popover; mobile bottom sheet. Heading `Sentence actions`; actions `Compare with source` and `Generate alternatives`. Cached state changes the second label to `Show alternatives`. Unavailable comparison remains visible disabled with helper. |
| `ComparisonPanel` | Nonmodal desktop panel or mobile sheet headed `Sentence comparison`. Sections `Original` and `Rewritten`; deletions in Original use strike-through, delete colors, and screen-reader prefix `Deleted:`. Insertions in Rewritten use change styling and prefix `Added:` in an optional annotations description. Close returns to sentence trigger/caret. |
| `AlternativesPanel` | Heading `Alternatives for: <sentence excerpt>`; loading skeletons plus `Generating alternatives…`; 2–4 option buttons named `Use alternative: <full option>`; selected option has `aria-current="true"` and a visible checkmark. Current text shown first as nonselectable `Current sentence`. After an option is applied, also offer `Use original wording: <full original result sentence>` as a separate local action, not counted among generated options. Escape/close returns focus. |
| `InlineAlert` | Region adjacent to the field/panel for validation, truncation, allowance, and persistent processing failure. Variants information, warning, error; heading omitted unless needed. Optional named action. Not dismissible when the condition remains. |
| `Toast` | Transient success/informational overlay at top-end desktop and bottom above safe-area mobile; no focus; 8 seconds for stale-success accounting, 2 seconds for copy. Errors are not toast-only. |
| `Dialog` | `alertdialog` for destructive workspace reset, ordinary `dialog` for informational actions. Has labelled heading and described body; maximum width 480 px. |
| `AuthForm` | One `main` heading; visible labels above fields; password visibility button `Show password`/`Hide password`; provider button `Continue with Google`; submit label matches page. First invalid field receives focus after submit. |

### 5.3 Tab order

Every route starts with the skip link. At ≥1200 px the workspace order is home, Usage, Account, Translation, Rewriting, language controls, source textbox, Clear, result textbox, Sentence actions (Rewriting only), Copy, then tools rail controls: Correction only, Styles, Show changes. At smaller widths: home, Usage, Account, Translation, Rewriting, language controls, Writing tools (Rewriting only), source textbox, Clear, result textbox, Sentence actions, Show changes, Copy. Translation omits all rewriting-only controls. Empty result textboxes remain focusable/read-only; disabled buttons and the disabled Styles selector are skipped. Popovers insert content after their trigger; modal sheets trap focus until closed. No generated sentence or changed word adds dozens of tab stops.

### 5.4 Surface transitions and editing ergonomics

Language/style selectors are select-only; four languages do not require search. Their English accessible names are stable and separate from the selected value. Use the [W3C combobox pattern](https://www.w3.org/WAI/ARIA/apg/patterns/combobox/) for expanded/active-descendant semantics and the [modal-dialog pattern](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/) for modal sheets. The choices below settle pattern options.

| Current state / precondition | Trigger / guard | Next visible state and controls | Focus/selection | Processing effect | Scenarios |
| --- | --- | --- | --- | --- | --- |
| Selector closed/enabled | Click, Enter, Space, or ArrowDown | Open listbox, chosen option highlighted | DOM focus on combobox; active descendant chosen option, otherwise first enabled | None | UX-AC-092 |
| Selector open | Up/Down, Home/End, typeahead (700 ms reset) | Highlight next/first/last/matching enabled option; no wrap | Active descendant scrolls into view; stored selection unchanged | None | UX-AC-092 |
| Selector open | Enter/Space or pointer option | Close; chosen value committed with checkmark | Combobox focus for keyboard; trigger for pointer | Only a changed language/style schedules debounce | UX-AC-092 |
| Selector open | Escape, outside click, or Tab | Close without committing highlighted candidate | Escape → trigger; outside → clicked target; Tab → next control | None | UX-AC-092 |
| Account menu closed | Click/Enter/Space | Menu open | First menu item; arrows/Home/End move, Enter activates | None until item activated | UX-AC-084, 092 |
| Usage panel closed | Activate usage | Panel open with heading, text, progress, Close | Panel heading (`tabindex=-1`); Tab visits Close then exits/ closes panel | At most one usage refresh if stale | UX-AC-065, 071 |
| Tools hidden below 1200 px | Activate `Writing tools` | Modal right sheet (768–1199) or bottom sheet (<768); heading `Writing tools` | Heading first; trap Tab; Close/Escape returns trigger | None | UX-AC-093 |
| Tools sheet open | Change Correction only or Styles | Sheet remains open; field value/helper updates; sentence surfaces close if any | Initiating control retains focus | Debounce according to Section 8 | UX-AC-093 |
| Any modal open | Close/Escape or viewport breakpoint change | Close on explicit dismissal; breakpoint change closes then renders destination composition | Trigger if still rendered, otherwise result heading | No text/mode change | UX-AC-093 |
| Result editable | Single click/tap or caret movement | Caret placed; inline affordance appears, accessible name `Actions for sentence {ordinal}` | Normal text caret; no automatic menu on mere caret placement | None | UX-AC-080, 094 |
| Result sentence selected | Activate its Sentence actions affordance or Alt+ArrowDown | Sentence menu opens; selected outline stays | First enabled action per Section 8.2; heading only if all actions disabled | No generation until Generate alternatives is activated | UX-AC-052, 094 |
| Result editable | Drag/Shift selection, paste, typing, cut, undo/redo | Normal clean-text editing | Native caret/selection; preserve plain newlines, strip pasted HTML formatting | Manual-edit rules in Section 8 | UX-AC-058–064, 094 |
| Tooltip visible | Hover tooltip itself; then Escape | Remains while hovered; Escape hides until next focus/hover entry | Unchanged | None | UX-AC-095 |

The two-step pointer affordance preserves ordinary text selection/editing. The `Sentence actions` toolbar button always offers the equivalent keyboard/touch path. Desktop sentence/alternative/comparison panels are 360 px wide maximum, minimum 280 px, with 8 px anchor gap and 16 px viewport inset; flip above if insufficient space below. Bottom sheets have 16 px padding, maximum 85dvh, sticky 48 px heading/Close row, scrollable body, and safe-area bottom padding. All mobile sentence/details sheets are modal; desktop panels are nonmodal. The disabled loading placeholders are never tab stops. Opening another independent surface closes the first, preserving the saved result caret and source selection; only the newly opened surface receives focus. A child selector inside Writing tools stays within its modal parent; Escape closes the selector first, then the sheet on a second press. Opening comparison or alternatives replaces the sentence menu and focuses the new panel heading; closing restores the original sentence trigger/caret, not a removed menu item.

Render Show changes exactly once: in the tools rail at ≥1200 px; in the result toolbar at smaller widths (omit it from the tools sheet). Reading/tab order follows Section 5.3.

## 6. Shared processing, precedence, and messages

### 6.1 State precedence

When conditions overlap, render and enforce the first applicable item:

1. Session invalid/expired: clear and leave the workspace.
2. Monetary-cap or service-wide unavailability returned for the attempted operation.
3. User/global allowance exhausted.
4. Field validation: empty, over rewriting limit, unsupported/mixed, unresolved source, or same-language target.
5. Offline: preserve editability; do not submit.
6. Pending submitted current operation (`Updating…`).
7. Valid input in debounce (`Waiting to update…`).
8. Manual-edited rewrite result.
9. Current successful result (`Up to date`).
10. Initial empty state.

The list orders the primary processing status, not deletion of other information: field errors, truncation disclosures, edited/outdated badges, and allowance banners remain concurrently visible in their own regions. Within validation use empty → rewrite over-limit → unsupported/mixed → uncertain → missing target → same-language, with all field-specific errors visible. Current final failure/interruption is terminal until Retry or a later relevant source/settings edit; it precedes idle/debounce labels. A stale failure cannot set terminal state. Authoritative authentication invalidation is checked against the current account/session and takes precedence even if its originating text operation is old; an old account's response cannot invalidate a newly authenticated account.

An operation is current only for the exact logical edit generation, not merely equal text. A→B→A typing does not revive the first A. Local events invalidate older operations before asynchronous responses are considered. Once a terminal deadline/failure is displayed, late transport success cannot apply text; reconcile any reported accounting according to #5. Controls using unchanged values, focus changes, opening/closing menus, Copy, and Show changes do not schedule processing.

An error from an outdated operation never replaces a current message. A successful outdated operation updates server-authoritative usage when reported and may produce the accounting notice in Section 6.3, but never replaces result text. A failure of an outdated operation is silent unless it is the only operation capable of producing the first result and the input/settings are still current—which by definition makes it current, not outdated.

### 6.2 Shared visible states

| State | Source | Existing result | Controls | Focus/selection |
| --- | --- | --- | --- | --- |
| Empty | Editable, canonical counter (whitespace may count even though ineligible) | Feature-specific placeholder from Sections 7/8, unless Clear retained a previous result | Language/settings enabled; Copy enabled only when retained nonempty result exists; new generation disabled | Heading on initial entry; source only after Clear/reset |
| Debouncing | Editable | Kept; status `Waiting to update… Previous result shown.` or `Waiting to process…` | All applicable settings enabled | Unchanged |
| Processing current | Editable | Kept; status `Updating… Previous result shown.`; first result uses skeleton and `Processing…` | Source/settings enabled; Copy enabled for previous result; alternative selection disabled if its sentence is changing | Unchanged; `aria-busy=true` on result region |
| Outdated result | Editable | Kept at normal contrast with warning status until replacement | Copy and permitted editing remain available | Unchanged |
| Success | Editable | Complete new text; status `Up to date` | Result edit/Copy/Sentence actions enabled | Source caret unchanged; no automatic focus |
| Total failure | Editable | Previous result kept; persistent error beneath result | `Try again` enabled if condition recoverable | Focus stays; alert announced once |
| Offline | Editable | Previous result kept | No request; UX-MSG-012 informational state | Focus stays |

### 6.3 Exact English message catalog

| ID | Exact text | Placement, persistence, and recovery |
| --- | --- | --- |
| UX-MSG-001 | `Waiting to process…` | Result status before first request; visual only, not live-announced on each edit. |
| UX-MSG-002 | `Waiting to update… Previous result shown.` | Result status during debounce with prior result. |
| UX-MSG-003 | `Processing…` | First-result status; polite announcement once when request begins. |
| UX-MSG-004 | `Updating… Previous result shown.` | Current submitted update; polite announcement once. |
| UX-MSG-005 | `Up to date` | Persistent result status; announce `Translation ready.` or `Rewrite ready.` only when a new result applies. |
| UX-MSG-006 | `Choose the source language to continue.` | Source selector error for uncertain detection; persists until manual selection/source change resolves it. |
| UX-MSG-007 | `Choose a target language different from {language}.` | Target selector error for same-language translation. |
| UX-MSG-008 | `This text contains too much unsupported or mixed-language content. Use one main language: English, Russian, Romanian, or Chinese.` | Source inline error; no request; updates after validation. |
| UX-MSG-009 | `Only the first 5,000 characters will be translated. A successful translation uses 5,000 characters of your allowance. {omitted} characters will not be sent.` | Translation source warning once canonical count exceeds limit; nondismissible while true. |
| UX-MSG-010 | `Partial translation — this result covers characters 1–5,000. The remaining {omitted} characters were not processed.` | Translation result warning after success; remains with result until full input becomes ≤5,000 and a new full result applies. |
| UX-MSG-011 | `Rewriting is limited to 2,000 characters. Remove {excess} characters to continue.` | Rewrite source error; no request; source remains editable. |
| UX-MSG-012 | `You’re offline. Keep editing; processing will resume when you’re back online.` | Workspace information alert. On `online`, if still valid, restart a full one-second debounce. |
| UX-MSG-013 | `We couldn’t process this text. Your text and previous result are safe.` | Current operation total failure; action `Try again`; persists until retry, relevant edit, or success. |
| UX-MSG-014 | `Processing took too long. Your text and previous result are safe.` | Deadline failure; action `Try again`. |
| UX-MSG-015 | `Your daily allowance is used up. Try again after {reset}. Your text is safe.` | User allowance inline error; reset rendered as absolute `00:00 UTC` plus localized relative time when server supplies it. |
| UX-MSG-016 | `LinguaDesk’s shared daily allowance is used up. Try again after {reset}. Your text is safe.` | Global allowance error; do not show global consumption. |
| UX-MSG-017 | `LinguaDesk processing is temporarily unavailable because its service budget has been reached. Your text is safe. Try again after service resumes.` | Monetary-cap error. If server supplies a reliable availability time, append `Expected after {time}.` |
| UX-MSG-018 | `Sign in to continue. Your text was not processed.` | Authentication failure before a workspace exists. Session expiry uses the message in Section 3.4 and clears text. |
| UX-MSG-019 | `Verify your email to use Translation and Rewriting.` | Unverified access page. |
| UX-MSG-020 | `This update is no longer current and was not applied.` | Visual-only secondary result line for a stale success whose usage data has not yet been reconciled; never for a stale failure. Replaced by UX-MSG-021 after reconciliation. |
| UX-MSG-021 | `An earlier update completed but was not applied. {count} characters counted toward today’s usage.` | 8-second polite toast plus persistent usage-panel event until next current success. |
| UX-MSG-022 | `Edited result — the current update won’t replace your changes.` | Rewrite result status immediately after a manual edit invalidates a pending response. |
| UX-MSG-023 | `Comparison unavailable after this sentence was edited.` | Disabled comparison helper for affected sentence. |
| UX-MSG-024 | `Alternatives changed because this sentence was edited.` | Information text when reopening an affected sentence after cached alternatives were invalidated; next generation is a new chargeable operation. |
| UX-MSG-025 | `We couldn’t generate alternatives. Your result is unchanged.` | Alternatives panel error; action `Try again`. |
| UX-MSG-026 | `Result copied to clipboard.` | Two-second polite status. |
| UX-MSG-027 | `Couldn’t copy the result. Select the text and copy it manually.` | Result inline alert; dismissible or replaced by next success. |
| UX-MSG-028 | `Email or password is incorrect.` | Login form alert; focus email field; no account enumeration. |
| UX-MSG-029 | `Google sign-in was cancelled. Try again or use email and password.` | Login/register form alert. |
| UX-MSG-030 | `We couldn’t sign you in with Google. Try again or use email and password.` | Login/register form alert for provider failure. |
| UX-MSG-031 | `If an account exists for that email, we sent a reset link.` | Password-request success; replace form body; link `Back to sign in`. |
| UX-MSG-032 | `This verification link is invalid or has expired.` | Verification page alert; action `Send a new verification email`. |
| UX-MSG-033 | `This reset link is invalid or has expired.` | Reset page alert; action `Request a new reset link`. |
| UX-MSG-034 | `Check your email to verify your account.` | Registration success page; includes `Resend verification email`. |
| UX-MSG-035 | `Verification email sent.` | Resend success status; resend disabled for server-provided cooldown, visible countdown. |
| UX-MSG-036 | `Password updated. Sign in with your new password.` | Reset success; action `Go to sign in`. |
| UX-MSG-037 | `Your session expired. Sign in again to continue.` | Login page banner after forced teardown. |
| UX-MSG-038 | `Enter your email address.` | Required email field error. |
| UX-MSG-039 | `Enter a valid email address.` | Email-shape error; no request. |
| UX-MSG-040 | `Enter your password.` | Required password field error. |
| UX-MSG-041 | `Passwords do not match.` | Confirmation field error; focus confirmation. |
| UX-MSG-042 | `Password must meet all requirements.` | Password-policy failure; requirements themselves come from the authoritative security contract. |
| UX-MSG-043 | `We couldn’t create an account with these details. Try signing in or use a different email.` | Registration conflict/rejection; form remains populated except password fields. |
| UX-MSG-044 | `We couldn’t send the email. Try again.` | Verification/reset delivery failure; action `Try again`; do not state whether an account exists. |

`{reset}`, `{time}`, `{count}`, `{language}`, `{omitted}`, and `{excess}` are populated only from authoritative operation/usage responses or the #5 local counting contract during typing. Missing reset time renders `when the daily allowance resets` rather than inventing a time. Every numeric policy value in example copy, counters, and fixtures is the current PRD baseline (FR-007, FR-024–FR-028, NFR-002), not a hardcoded independent policy: render the configured canonical value with English thousands separators. Error categories and fields remain document #5's responsibility.

### 6.4 Live-region policy

One `role="status"`, `aria-live="polite"`, `aria-atomic="true"` region announces: request start once after debounce, applied completion, copy completion, offline/online transition, alternatives completion, and stale-success charge. Validation and operation errors use one adjacent `role="alert"` only when first appearing or materially changing. It does not announce every keystroke, counter change, debounce restart, highlight change, or spinner frame. This follows [WCAG 2.2 SC 4.1.3 guidance](https://www.w3.org/WAI/WCAG22/Understanding/status-messages) to expose status without unnecessarily interrupting work.

## 7. Translation behavior

### 7.1 Controls and content

The Translation page has `h1` text `Translation`. Source defaults to `Detect automatically`. Target begins as the explicit placeholder `Choose target language`; no operation is eligible until selected. Target options are English, Russian, Romanian, and Chinese (Simplified). If Chinese is selected, the closed control reads `Chinese (Simplified output)` and its description says `Traditional and Simplified Chinese input are accepted; output is Simplified Chinese.`

The source editor placeholder is `Enter text to translate`. The result placeholder is `Your translation will appear here.` There is no Translate button. Input, source-language change, and target-language change each reset the accepted one-second debounce. Selecting the already-selected value does nothing.

While detected language is unresolved, the source control reads `Detecting…` only after a request-like local/server validation has begun; otherwise it remains `Detect automatically`. A confident resolution is presented as `Detected: English` (or other language) beneath the selector and is not treated as a manual selection. Manual selection reads the chosen language and suppresses detection until the user chooses `Detect automatically` again.

The same detection presentation applies to Rewriting. The pause deadline is measured from the last source/settings edit or composition end. If eligibility finishes after that deadline, submit immediately once all guards pass; do not add a second pause. If it resolves before the deadline, wait for the remaining pause. A blocked uncertain/mixed result remains blocked until a relevant source/manual-language edit; such an edit starts a new full pause. A missing target shows the helper `Choose a target language to start translating.` without an alert on initial entry. Detection transport failure shows UX-MSG-013 with Try again and never submits an operation or claims a detected language.

### 7.2 Oversized translation

When the authoritative input count exceeds 5,000, the entire source remains editable. The UI renders a non-editing boundary marker aligned between canonical characters 5,000 and 5,001 labelled `Translation stops here`; the suffix gets a neutral hatched background and description `Not sent`. The source textbox's clean accessible value remains the complete source. UX-MSG-009 appears before submission and the submitted operation covers only the server-confirmed prefix.

Here “authoritative count” means the #5 counting contract applied locally to the complete source; do not send the omitted suffix to a validation/counting/provider endpoint to determine that boundary. The service confirms the submitted prefix count. The marker is a visual overlay/decoration and accessible description, never a character inserted into the source value. A `Show processed boundary` button in the warning scrolls the source to the boundary without moving its caret; this works with keyboard and touch. While typing, disclose truncation as soon as the canonical local count exceeds the limit and before starting its debounce. Changing content invalidates old detection; no paid language operation starts until current eligibility and distinct target are established. Non-chargeable eligibility checks, if remote, are distinguished from translation/rewrite operations by the #5 contract; the UI tests count both separately.

On success, UX-MSG-010 remains above the result, using the omitted count from that result's submission snapshot, never a newer source count. `Copy result` copies only the returned prefix translation. Editing the source suffix still restarts the debounce because the canonical first 5,000 boundary may change under document #5's counting rules; the client must not assume suffix-only edits are free. Fixture intent supplies a submitted count of 5,000 and an omitted count; this does not prescribe API field names.

### 7.3 Translation state transitions

| Current state / preconditions | Trigger and guard | Next state and visible content | Controls and focus | Logical effect | Scenarios |
| --- | --- | --- | --- | --- | --- |
| Empty or whitespace-only | Input remains ineligible | Empty; any previous result retained/outdated | No error; target selectable; Copy enabled only for retained nonempty result; source focus/caret unchanged | Cancel debounce; no request | UX-AC-021, 026 |
| Valid source, no target | User selects a target | Debouncing; UX-MSG-001 | All selectors/source enabled | Schedule one operation at +1,000 ms | UX-AC-022 |
| Valid source and target | Input/source/target changes; IME not composing | Debouncing; prior result, if any, gets UX-MSG-002 | Result remains copyable; result editing disabled only after request submits | Replace debounce deadline | UX-AC-023, 024 |
| IME composition | `compositionupdate` events | Composition | No status churn; caret unchanged | No count finalization, validation, debounce, or request | UX-AC-025 |
| IME composition | `compositionend` with eligible value | Debouncing | UX-MSG-001/002 | Start full 1,000 ms clock now | UX-AC-025 |
| Debouncing | Clock reaches 1,000 ms and local eligibility still holds | Processing current; UX-MSG-003/004; prior result kept | Source/selectors enabled; prior result read-only and copyable | Submit once with current source/settings revision | UX-AC-022–025 |
| Any pre-submit state | Validation resolves uncertain, same-language, unsupported/mixed | Validation blocked; UX-MSG-006/007/008 | Problem control receives `aria-invalid`; do not steal focus | Cancel debounce; no provider operation/charge | UX-AC-027–029 |
| Any eligible source over limit | Canonical count received | Debounce/processing with truncation warning | Full source editable; boundary and warning visible | Submit only authoritative prefix | UX-AC-030, 031 |
| Processing A | User changes input/settings, creating B | B debouncing; A outdated; previous result unchanged | Source/caret unchanged | A may complete/charge but cannot apply; B scheduled | UX-AC-034 |
| A outdated, B current processing | B succeeds before A | Success B, `Up to date` | Result editable again; Copy enabled | Apply B once; update usage | UX-AC-034 |
| A outdated, B current processing | A succeeds before/after B | Current state unchanged except usage; UX-MSG-021 | No focus change | Discard A text; reconcile server usage | UX-AC-035 |
| Processing current | Success matches current revision | Success; complete result replaces previous; UX-MSG-005 | Result editable; source selection unchanged | Apply once; update usage | UX-AC-022 |
| Processing current | Total failure/deadline | Failure; previous result or empty placeholder retained; UX-MSG-013/014 | `Try again`; source/settings enabled | No charge per returned outcome | UX-AC-036 |
| Success | User edits result | Edited translation result; status `Edited` | Result editable; Copy copies edit | Local only, no request/charge | UX-AC-033 |
| Processing current with previous result | User attempts result edit | No state change | Result is read-only; helper `Wait for the update to finish before editing this result.` | Prevent undisclosed overwrite | UX-AC-037 |
| Any with pending | Clear source | Empty source; prior result marked `Source cleared — previous result shown.` | Focus empty source; Copy prior result enabled | Invalidate pending; no new request | UX-AC-032 |
| Any | Navigate within workspace | Hidden page state retained | Destination `h1` focus | Current operation may continue; apply only to its matching page | UX-AC-002, 005 |

## 8. Rewriting behavior

### 8.1 Defaults, language, and modes

The page `h1` is `Rewriting`. The source placeholder is `Enter text to improve`; the result placeholder is `Your improved text will appear here.` `Writing language` defaults to `Detect automatically` and can be manually set to English, Russian, Romanian, or Chinese. Rewriting never offers a target language. Chinese output is labelled Simplified as in Translation.

Grammar/spelling correction is always active. The tools surface always includes `Grammar and spelling correction is always on.` New-workspace defaults are Correction only off, Styles `None set`, and Show changes on. Style menu values and order are:

1. `None set`
2. Writing styles: `Simple`, `Casual`, `Business`, `Academic`
3. Tones: `Enthusiastic`, `Friendly`, `Confident`, `Diplomatic`

Selecting any one replaces the prior selection; style and tone cannot coexist. Selection immediately closes the menu, returns focus to `Writing style or tone`, marks any result outdated, closes sentence surfaces, and restarts the one-second debounce.

Enabling Correction only stores the last Styles value in the workspace, disables the dropdown without clearing its visible value, exposes helper text, closes sentence surfaces, and restarts debounce. Disabling Correction only restores that stored value automatically and restarts debounce. A new workspace forgets the stored value and shows `None set`. This resolves the restoration part of Q-002 without changing FR-015.

### 8.2 Change review and sentence tools

With Show changes on, changed/revised words in the result use `ChangedText`. Deletions never appear inline in the clean result; `Compare with source` opens `ComparisonPanel`, where original deletions are struck through and both versions are explicitly labelled. With Show changes off, styling and inline comparison affordance disappear but `Sentence actions` and `Alt+ArrowDown` remain available. Toggling Show changes is local, does not alter result text/metadata, and does not submit or charge.

Clicking/tapping result text places the caret and exposes the separate sentence affordance defined in Section 5.4; activating that affordance opens `SentenceMenu` without changing text. Keyboard activation uses the caret sentence. If no caret is in the result, `Sentence actions` opens a listbox labelled `Choose a sentence`, each option named by its 40-character excerpt and ordinal. The sentence list uses the LanguageSelect listbox keys; choosing closes it and opens the sentence menu. Focus moves to `Compare with source` when available, otherwise the next enabled action; if neither action is enabled, focus the menu heading. Source text/selection never change.

### 8.3 Sentence correspondence and targeted invalidation (Q-002 UX portion)

The UI observes sentence boundaries supplied/maintained through the owning rewrite/API design; this document does not define identifier representation or the matching algorithm. It defines these outcomes:

| Manual edit | Affected metadata | Preserved metadata | Visible result |
| --- | --- | --- | --- |
| Characters changed wholly inside sentence A; boundaries unchanged | A comparison and cached alternatives invalidated | Every unchanged sentence retains identity, comparison, selected alternative, and cache | A loses change marks; sentence helper UX-MSG-023/024 |
| New complete sentence inserted between A and B without altering their text/boundaries | New sentence has no comparison/cache | A, B, and all others preserved | New sentence treated as manually edited; alternatives can be generated |
| Whole sentence A deleted without changing neighbor text/boundaries | A metadata removed | Remaining sentences preserved | No placeholder for deleted sentence |
| A and B merged by deleting/changing their boundary | A and B invalidated; merged sentence gets new edited state | All non-touching sentences preserved | Merged sentence has no comparison/cache |
| A split into A1/A2 by inserting/changing boundary punctuation/newline | A invalidated; A1/A2 get edited state | All non-touching sentences preserved | Both new sentences have no comparison/cache |
| Boundary punctuation/whitespace changed ambiguously between A and B | Every sentence touching the changed boundary invalidated | Non-touching sentences preserved | Conservative local invalidation; no whole-result reset |
| Edit undone exactly to the pre-edit sentence text/boundaries within the local undo stack | Affected comparison/cache remains invalidated | Other metadata unchanged | Text restores, but marks/comparison/cache do not reappear until supplied by a fresh eligible operation |

If the UI cannot establish that a sentence is unchanged, it must classify only the ambiguous local boundary group as affected; it must not invalidate the whole result by convenience. Concrete correspondence algorithms and identifier lifecycles remain with documents #3/#5 and the selected rewriting feature specification.

Manual result input increments the result-edit revision. Every earlier pending full-rewrite or alternatives response becomes outdated immediately, even when it concerns a non-touching sentence; this is the unambiguous precedence rule that protects the edited result. Only cached comparison/alternative metadata for affected sentences is invalidated. Any open sentence/alternatives surface closes. UX-MSG-022 appears if a full rewrite was pending. A later source/language/style/Correction-only change starts a new full rewrite and, on current success, replaces the entire result including manual edits and resets all sentence choices, as required by FR-022.

Result `compositionstart` establishes the same response protection before any interim IME text can be overwritten. Defer sentence-metadata classification until composition commits; a cancelled composition preserves unchanged metadata but never revives an earlier response/debounce. Neither interim result composition nor its completion starts a language operation.

A manual result edit also cancels an earlier scheduled full-rewrite debounce: work triggered before the manual edit must not subsequently overwrite it. With no submitted current update, show `Edited result — change the source or writing settings to generate a new rewrite.` Editing a source/mode later re-enables automatic processing. A local undo restores text but does not revive an invalidated response or debounce; affected comparison/cache remains unavailable until a fresh operation supplies it. Clearing the entire result manually keeps the result textbox editable, disables Copy/Sentence actions, and makes no request.

While the result is outdated due to source/settings changes, sentence comparison may show its original snapshot labelled `Previous result comparison`; generation and cached option selection are disabled with `Wait for a current rewrite before choosing alternatives.` This remains true after a failed update; Retry or a later source/settings edit can produce a current rewrite. A manually edited result with unchanged source/settings remains eligible for explicit sentence alternatives. When the result was already outdated before the manual edit, it stays outdated and only local edits/copy/comparison are available. An empty/whitespace-only edited sentence is not an alternatives target.

Selecting an alternative replaces only that sentence span, even when the alternative contains multiple sentences; those segments are one selectable replacement group until another full rewrite or manual boundary edit. Its original comparison uses the same full-rewrite source span, and changed marks are recomputed against that original. Reopening preserves the original option set, including selected-state indication; original full-result wording can be reselected without a request. Other sentence choices/caches remain intact even though context changed. All pending earlier alternatives responses are invalidated by selection; existing cached metadata remains. There is no Generate more action. On an edited sentence, fresh alternatives can be generated but cannot restore a missing original comparison. Purely deleted original sentences appear as a labelled `Deleted sentence` comparison entry in Sentence actions; they are not generation targets.

### 8.4 Additional competing-event transitions

| Current state / precondition | Trigger / guard | Next visible state and controls | Focus/selection | Logical effect | Scenarios |
| --- | --- | --- | --- | --- | --- |
| Full rewrite debouncing | Manual result edit before submission | Edited/outdated badge as applicable; no spinner | Result caret stays | Cancel earlier debounce; no request | UX-AC-096 |
| Alternatives loading, panel closed by explicit Close | Reopen same unchanged sentence | Same loading operation shown | Panel heading | Reuse in-flight operation; no second request | UX-AC-097 |
| Alternatives loading; panel dismissed, no invalidation | Success | Cache ready silently if page/surface hidden | No focus movement; current visible panel announces completion only | One charge; cache reused on next open | UX-AC-097 |
| Alternatives A loading | Select cached option on another sentence | Local selected result; A discarded when it resolves | Chosen sentence caret | No full rewrite/new charge from selection; successful A still counts | UX-AC-098 |
| Result outdated from source/settings | Activate sentence | Compare available for snapshot; generation/selection disabled with helper | Menu heading | No request | UX-AC-099 |
| Manual-edited result | Undo changed text | Text restored; comparison/cache for affected sentence still unavailable | Native undo caret | No request; no pending operation resurrected | UX-AC-100 |
| Result/source current | Edit outside a sentence span or delete all result | Only boundary-touching group invalidated; empty result disables Copy/Sentence actions | Native caret | No request; protect full source | UX-AC-100 |

### 8.5 Full-rewrite state transitions

| Current state / preconditions | Trigger and guard | Next state and visible content | Controls and focus | Logical effect | Scenarios |
| --- | --- | --- | --- | --- | --- |
| Empty/whitespace | Any edit still empty | Empty | Settings enabled; result disabled | No debounce/request | UX-AC-038 |
| Eligible ≤2,000 | Source/language/style/Correction-only change outside IME | Debouncing; UX-MSG-001/002 | Source and result editable; sentence surface closes | New source/settings revision; schedule +1,000 ms | UX-AC-039–044 |
| IME active | Composition events | Composition | No focus/status change | No request until compositionend + full pause | UX-AC-043 |
| Canonical count >2,000 | Any change | Validation blocked; UX-MSG-011 | Source editable; result and Copy preserved | Cancel debounce; no request/charge | UX-AC-045 |
| Any pre-submit state | Detection is uncertain or input is unsupported/substantially mixed | Validation blocked; UX-MSG-006 or UX-MSG-008 | Source selector/input invalid; source remains editable | Cancel debounce; no provider operation/charge; manual supported source resumes only if content is eligible | UX-AC-088, 089 |
| Debouncing | Clock expires and eligible | Processing current; UX-MSG-003/004 | Source and prior result remain editable; alternatives surface closed | Submit current full rewrite | UX-AC-044 |
| Processing A | Source/settings change creates B | B debouncing; A outdated | No focus change | A cannot apply; B may submit | UX-AC-046 |
| Processing current | Manual result edit | Manual-edited, pending invalidated; UX-MSG-022 | Caret/selection preserved; Copy enabled | Increment edit revision; no request/charge | UX-AC-058 |
| Outdated A | A succeeds | Current text preserved; usage updates; UX-MSG-021 | No focus change | Discard A text, count successful A | UX-AC-047, 059 |
| Processing current | Matching success | Success; replace complete result; Show changes presentation applied | Source caret unchanged; result remains editable | Reset all manual edits/alternative choices for new full-result revision | UX-AC-044 |
| Processing current | Failure/deadline | Failure; source/result preserved; UX-MSG-013/014 | Retry enabled | No charge | UX-AC-072, 073 |
| Manual-edited success | Later source/settings operation succeeds | Success with complete replacement | Result caret removed; focus stays where user currently is | Accepted full rewrite replaces edits and resets metadata | UX-AC-060 |
| Any pending | Source cleared or workspace/session ends | Empty or teardown respectively | Menus close; focus per lifecycle rule | Pending invalidated; session end clears all | UX-AC-032, 085 |
| Any pending | SPA mode link or Back/Forward | Page hidden; state retained | Destination heading; sentence surfaces close | Current pending work continues on its own page; no navigation-generated request | UX-AC-004–005 |

### 8.6 Alternatives state transitions

| Current state / preconditions | Trigger and guard | Next state and visible content | Focus | Logical effect | Scenarios |
| --- | --- | --- | --- | --- | --- |
| Current result, sentence selected, no cache | `Generate alternatives` | Loading; 3 skeleton rows and `Generating alternatives…` | Panel heading; skeletons not focusable | Submit one sentence operation with current context/settings | UX-AC-052 |
| Loading A | User selects another sentence B | A panel closes; B menu opens | B trigger/menu | A outdated; does not apply; B request starts only on explicit action | UX-AC-055 |
| Loading A | User edits A | Panel closes; UX-MSG-024 on next open | Result caret preserved | A response outdated; affected cache invalidated | UX-AC-061 |
| Loading A | User edits non-touching B | Panel closes | Result caret preserved | A response outdated to protect the newer edited result; only B's cached metadata is invalidated, while pre-existing A metadata remains | UX-AC-062 |
| Loading current | Success with 2–4 options | Ready list; usage updates | No asynchronous focus change; Tab from heading reaches first option | Cache one option set for sentence revision; charge once | UX-AC-052 |
| Loading outdated | Success | Current result unchanged; UX-MSG-021 and usage update | No focus change | Discard options; successful operation counted | UX-AC-055, 061 |
| Loading current | Failure | Error UX-MSG-025; source/result unchanged | No asynchronous focus change; Try again becomes reachable by Tab | No cache/charge | UX-AC-056 |
| Ready options | Select one option | Result sentence replaced; panel closes; status `Alternative applied` | Return result caret to end of replaced sentence | No new request/charge; preserve other choices/metadata | UX-AC-053 |
| Selected/cache exists | Reopen sentence | Ready cached options; label `Show alternatives` | Panel heading; selected option exposed programmatically | No request/charge | UX-AC-054 |
| Cache exists | Source/language/style/Correction-only change | Panel closes; result outdated | Focus stays on initiating control | All old-result alternative choices reset on next current full success | UX-AC-057 |
| Cache exists | Manual edit affects same sentence/boundary | Cache invalidated | Result caret preserved | Other sentence caches preserved | UX-AC-063, 064 |

## 9. Usage, availability, and recovery

### 9.1 Usage presentation

The header usage button always uses the latest server-authoritative account values. Its panel contains:

- Heading `Today’s usage`.
- `Used: 7,500 characters` and `Remaining: 12,500 characters` (fixture example only; the 20,000 configured allowance comes from FR-027/server capability data).
- A determinate progress bar named `Daily character usage`, with text in addition to fill.
- `Resets at 00:00 UTC` and, when reliable, `in 6 hours 20 minutes`.
- Explanation: `Successful Translation, Rewriting, and alternative requests count toward the same daily allowance. Failed requests do not count. A successful result can count even when a newer edit means it is not shown.`

Do not present a call count, provider token count, global consumption, monetary amount, model name, retry count, or fallback event. After each operation outcome, replace usage from the server response; never increment optimistically. If usage refresh fails but the operation succeeded, show the result and retain the last value labelled `Usage update unavailable`; retry usage fetch in the background and on panel open.

“Latest” refers to server observation order/reset period, not arrival order: an older snapshot cannot roll the meter backward. #5 supplies ordering or a fresh usage read to resolve ambiguous ordering. Usage may decrease only for a newer reset period or authoritative correction. Do not show stale success notices from a ended workspace/account in a new workspace; fresh account usage can still include those charges. Before the first usage response show `Usage loading…`; if unavailable, show `Usage unavailable` with `Retry usage update` and allow the server to judge operation eligibility. Never show missing usage as zero. Retry background usage once after 5 controlled seconds, then only on explicit panel open/action until success.

Permanent workspace helper below the editors: `After you pause, eligible text is processed automatically. Each successful update uses the full submitted character count, even for a small edit.` Alternatives panel helper before generation: `Generating alternatives uses {count} characters if successful. Choosing or reopening these options uses no more characters.` The count is the selected sentence's canonical current length. It is not the full context length.

The source counter is not the allowance meter. Before submission it may be provisional; after a server validation/operation response it displays the authoritative submitted/canonical values. When an outdated success is reported, usage changes even though result text does not; UX-MSG-021 explains the change.

### 9.2 Availability behavior

- User/global allowance and monetary-cap failures preserve source, result, selection, settings, and clipboard ability. Automatic retries are forbidden until a relevant server-reported availability change or daily reset; user edits continue but do not schedule network operations while known blocked.
- At a daily reset signal/time, refresh usage once. If availability is confirmed, clear UX-MSG-015/016 and start a fresh one-second debounce for still-valid input. A local clock reaching midnight is not proof of reset; the server remains authoritative.
- Monetary-cap unavailability has no local automatic retry loop. `Check availability` requests status only and never submits text; on available, valid input resumes after one-second debounce.
- Internal fallback is invisible. The only user-visible distinction is complete success versus the final classified failure.
- A network interruption after submission is `Processing interrupted. Checking the operation status…` while reconciliation is pending. Do not resubmit automatically or claim no charge. If reconciliation is unavailable, proceed directly to the unknown-outcome state below; #5 supplies status mechanics, not a different UX outcome.

Recovery is bounded by the operation's PRD overall deadline from first submission, excluding the typing pause: translation/rewrite 30 seconds, alternatives 15 seconds (NFR-002). If no reconciled outcome is available by that deadline, stop the spinner and show `We couldn’t confirm whether this operation completed. Your text is safe. Usage may still update.` with `Check status`; copying/local editing remain available. Check status never submits content. A new deliberate source/settings edit may submit newer work, but there is no duplicate automatic resubmission of the uncertain revision. #5 owns reconciliation/identity mechanics; this observable unknown-outcome state is fixed here.

Exhaustion includes a remaining allowance smaller than the next submitted count, not only zero. Display `This request needs {needed} characters, but only {remaining} remain in your daily allowance. Shorten the source or try again after {reset}.` Global insufficiency uses `LinguaDesk’s shared daily allowance cannot cover this request. Try again after {reset}. Your text is safe.` A shorter eligible edit may resume automatically if the known user remainder can cover it; global/budget blocks require authoritative availability. The alternatives-specific user action is `Choose a shorter sentence or try again after {reset}.` Keep every editor available; locally generated/selected text is never blocked by allowance. Server enforcement remains decisive for concurrent use by other clients.

### 9.3 Authentication and form behavior

Local registration fields are `Email`, `Password`, and `Confirm password`; Google is offered above an `or` separator. Password requirements are displayed as a server/architecture-owned checklist before typing and update without color-only cues. This specification deliberately does not invent the security policy; until documents #3/#5 supply it, automated UI fixtures use `At least 12 characters` solely as contract-shape data, not a product requirement. Client validation handles email shape, required fields, password confirmation, and the supplied password policy; server errors remain authoritative.

Registration submit is `Create account`. Success replace-navigates to `/verify-email` with UX-MSG-034. Verification links show `Verifying your email…`, then `Email verified. You can now use LinguaDesk.` and `Continue to Translation`; invalid links use UX-MSG-032. Resend remains on the page, announces UX-MSG-035, and shows a server-provided cooldown without inventing its duration.

The continuation label is `Continue to Rewriting` when `/rewrite` is the recorded destination, otherwise `Continue to Translation`. A successful verification link does not itself authenticate a signed-out user unless the auth contract separately confirms a session; signed-out success offers `Sign in to continue`, then returns to the intended route. Resend on an invalid link without an authenticated email context shows an Email field and the generic success `If verification is needed for that email, we sent a new link.` Authenticated verification pages show the full signed-in email, wrap long addresses, and offer `I’ve verified my email` to refresh status once. Still unverified: `Your email is not verified yet. Open the link in your email, then check again.` No indefinite polling or assumed success from elapsed time.

Login fields are `Email` and `Password`; submit `Sign in`. Google success returns to the protected route and is considered verified because local email verification applies only to local accounts. Cancellation/failure returns to the originating auth page with UX-MSG-029/030. No live Google page, CAPTCHA, or email delivery is required for deterministic UI tests.

Forgot-password submit is `Send reset link`; every syntactically valid email receives UX-MSG-031 regardless of account existence. Reset form fields are `New password` and `Confirm new password`, submit `Update password`; success shows UX-MSG-036. Expired links show UX-MSG-033. Password reset does not restore a workspace.

Auth headings are `Sign in to LinguaDesk`, `Create your account`, `Verify your email`, `Reset your password` (request), and `Choose a new password` (completion). All auth routes have a LinguaDesk home link, and form footers offer `Back to sign in` except login, which offers `Create account` and `Forgot password?`. Inputs permit paste, password-manager autofill, and password visibility toggling; do not persist password values in application storage. Confirmation fields have their own `Show confirm password` / `Hide confirm password` buttons. Password reveal preserves selection and does not submit. Field labels are never placeholders alone.

Form transitions apply to login, registration, reset, resend, and verification status:

| Current state / precondition | Trigger / guard | Next visible state and controls | Focus | Logical effect | Scenarios |
| --- | --- | --- | --- | --- | --- |
| Form idle | Submit with invalid local fields | Inline UX-MSG-038–042 plus summary `Check the highlighted fields.` | First invalid field | Zero requests | UX-AC-007, 018, 101 |
| Valid form | Submit | Label `Signing in…`, `Creating account…`, `Sending…`, or `Updating password…`; submit and fields disabled | Submit remains focused (`aria-disabled` while busy) | One logical auth request; repeat click/Enter ignored | UX-AC-101 |
| Submitting | Current success | Destination/success content per Section 9.3 | Destination heading; no automatic keyboard | One success transition | UX-AC-006, 010, 013, 017, 019 |
| Submitting | Current credential/policy/registration rejection | UX-MSG-028/042/043; fields enabled; password values cleared | First relevant field | No operation request | UX-AC-014, 101 |
| Submitting email action | Delivery/network failure | UX-MSG-044, fields enabled, Try again | Keep submit focus | No success claim; explicit retry only | UX-AC-102 |
| Submitting login/register/reset | Network/unclassified failure | `We couldn’t complete this request. Try again.`; inputs enabled; passwords cleared | Submit stays focused | Explicit retry only | UX-AC-102 |
| Any auth page pending | User navigates Back/to another auth route | New route retains email only within same auth journey; passwords clear | Destination heading | Old UI completion cannot navigate or override newer route; current account bootstrap reconciles independently | UX-AC-103 |
| Workspace verified | Sign out; then auth success/failure | Immediate cleared login, pending status; success → form, failure → retry banner per 3.4 | Login heading; no async focus change for failure | One sign-out action, no language operation; stale text cannot return | UX-AC-016 |
| Verification link | Invalid/expired or already verified | Invalid UX-MSG-032 with resend, or normal verified success | Heading | No language request | UX-AC-104 |

Form submission errors clear on next submit or a relevant field edit, but unrelated errors remain. Typed email may remain in runtime memory while switching auth forms; it is not copied to a URL. An email already belonging to a Google-only account receives the same reset-request confirmation; helper `If you use Google to sign in, continue with Google instead.` links to login without revealing account type. Account linking/deletion is not introduced here.

## 10. Accessibility and browser coverage

### 10.1 Acceptance baseline

Target WCAG 2.2 Level AA for the MVP, plus the stronger 44×44 CSS px touch-target default from WCAG's Level AAA target-size guidance where layout permits. The normative reference is [WCAG 2.2](https://www.w3.org/TR/WCAG22/). Specific applicable criteria include reflow 1.4.10, non-text contrast 1.4.11, text spacing 1.4.12, content on hover/focus 1.4.13, keyboard 2.1.1/2.1.2, focus order 2.4.3, focus visible 2.4.7, focus not obscured 2.4.11, target size 2.5.8, error identification/suggestion 3.3.1/3.3.3, accessible authentication 3.3.8, name/role/value 4.1.2, and status messages 4.1.3. Standalone targets must be at least 24×24 CSS px under [SC 2.5.8](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html); LinguaDesk's default is 44×44 px.

### 10.2 Semantic and keyboard requirements

- Exactly one `main` and one visible `h1` per route. Editor headings are `h2`; modal headings are `h2`. A first-focus skip link named `Skip to main content` becomes visible on focus.
- All functionality is available without pointer or drag. Native links, buttons, checkboxes, and textboxes are preferred. Custom combobox/listbox behavior follows the ARIA Authoring Practices pattern and exposes name, expanded state, active option, selected option, and errors.
- Tab order is Section 5.3. Arrow keys move only within an open listbox/menu. Escape closes the topmost dismissible surface. Enter/Space activates buttons; Space toggles focused checkboxes without scrolling.
- Focus is never moved by typing, validation arrival, automatic request start/completion, usage refresh, stale response, or inline alert. Focus moves only after explicit route activation, submit with errors, opening/closing a surface, destructive confirmation, or user-requested auth continuation.
- Textbox undo/redo, selection, clipboard shortcuts, IME, spellcheck, and platform editing keys remain native. LinguaDesk does not bind plain arrow keys or common editing shortcuts.
- Sentence tools have the redundant `Sentence actions` button and `Alt+ArrowDown` caret shortcut; the shortcut is exposed in help text and is not the sole path.

### 10.3 Visual, reflow, and motion requirements

- Body text and controls meet 4.5:1 contrast; large text 3:1; focus indicators and component boundaries/state indicators 3:1 against adjacent colors. The explicit tokens in Section 4.1 satisfy these intended pairs and must be rechecked if changed.
- Browser zoom to 200% preserves every action; 400%/320 CSS px reflows to one column with no horizontal page scrolling. Text spacing overrides of 1.5 line height, 2× paragraph spacing, 0.12em letter spacing, and 0.16em word spacing cause no clipping/loss.
- Result highlights use underline/background in addition to color. Deleted text uses `Deleted:` accessible annotation plus strike-through and color. Updating uses text plus spinner; errors use icon plus text.
- Touch targets are 44×44 px except inline text links, which meet WCAG's inline exception and have at least 8 px vertical separation. Sentence pointer hit regions are at least one full 24 px line box and the 44 px toolbar path is always available.
- Under reduced motion, use a static progress glyph with the same status text. Transitions become 0 ms, skeleton shimmer is disabled, and no content flashes more than three times per second.

### 10.4 Assistive-technology announcements

The result region exposes `aria-busy` only while the current operation is submitted. Applying a result announces a short status, not the result text. Users reach and read the result normally. Alternative count completion announces `3 alternatives ready for the selected sentence.` Comparison annotations are readable in document order. Usage progress has numeric text. Authentication errors are summarized and linked to fields.

Automated checks can establish DOM semantics, accessible names, keyboard reachability, focus placement, contrast calculations, reflow screenshots, and live-region mutations. They cannot establish understandable screen-reader phrasing, rotor/navigation usability, speech-control discoverability, magnifier comfort, or real mobile keyboard behavior.

Required manual checks before RG-006 evidence:

- NVDA with Firefox on Windows: register/login fixture, Translation, Rewriting, alternatives, comparison, validation, and session-expiry journeys.
- VoiceOver with Safari on macOS: same core journeys, plus IME composition with Simplified and Traditional Chinese.
- VoiceOver with Safari on iOS at `390×844`: source/result editing, sheets, copy, zoom, and software-keyboard visibility.
- TalkBack with Chrome on Android at `360×800`: mode navigation, language listboxes, sentence selection fallback, and error recovery.
- Keyboard-only on Windows and macOS at 100%, 200%, and effective 400% zoom; high-contrast/forced-colors smoke check; speech-control check that visible labels match accessible names.

### 10.5 Browser/device policy

Support the current and immediately previous major stable releases at the date of each release candidate for Chrome, Edge, Firefox, desktop Safari, iOS Safari, and Android Chrome. Record exact tested versions and manual evidence in #6. Playwright's bundled Chromium/WebKit are engine coverage, not proof of branded Chrome/Edge/Safari versions or physical devices. A browser-major change triggers affected smoke checks; it does not require a new screenshot baseline for every supported version. No Internet Explorer, embedded legacy WebView, native-app, or extension support is implied.

| Representative lane | Viewport | Automated scope | Manual scope |
| --- | ---: | --- | --- |
| Chromium desktop | 1440×900 | Small browser-sensitive set (clipboard, editing, keyboard/focus, history/privacy), curated visuals, accessibility scan, integrated smoke per #3 | Keyboard/zoom smoke; branded Chrome/Edge release checks |
| Firefox desktop | 1024×768 | Targeted composition-event contract and short keyboard/editor smoke; no full state matrix | NVDA core journeys and actual supported Firefox versions |
| WebKit desktop | 1440×900 | Short focus/navigation/editor smoke; no screenshot matrix | Actual Safari/VoiceOver core journeys |
| Chromium narrow | 390×844 | Curated narrow visuals and sheet/reflow smoke | Android Chrome/TalkBack at 360×800 |
| Chromium reflow/breakpoint probes | 320×640 and 1024×768 | Small geometry/overflow and rail-to-sheet checks; no extra baseline matrix | Zoom/text-spacing checks per 10.4 |
| WebKit narrow | 390×844 | No separate automated lane | Actual iOS Safari/VoiceOver and software keyboard |
| Previous-major supported releases | Representative desktop/mobile | No full automated matrix | Bounded core access/editor/copy/navigation smoke before release support is claimed; #6 records actual versions or an explicit unresolved evidence gap |

Most acceptance assertions execute in pure units and Vitest DOM components, with focused MSTest persistence/API checks. Chromium owns the comprehensive **browser-sensitive subset**, not every acceptance scenario. Firefox/WebKit smoke and actual-device/manual evidence are release checks or triggered by changes to the covered behavior. Preserve required supported-browser evidence without multiplying all scenarios across engines. Manual AT assessment in Section 10.4 remains necessary for RG-006.

## 11. User stories

All acceptance IDs refer to Section 12. Shared behavior is normative in Sections 3–10 rather than duplicated per story. In compact tables, `US-nnn` and `AC-nnn` mean the canonical `UX-US-nnn` and `UX-AC-nnn`; comma-separated numbers and inclusive ranges inherit the immediately preceding prefix. These are references, never new IDs.

| Story | User story | PRD links | Preconditions, entry point, and value | Screens/components/rules | Acceptance scenarios |
| --- | --- | --- | --- | --- | --- |
| UX-US-001 | As a verified user, I want to move between Translation and Rewriting without losing active work, so that I can use both tools in one workspace. | FR-003, FR-038, NFR-004 | Verified fixture; enter `/translate`, direct route, or mode link; continuity without saved history | Routes, ModeNav, Sections 3.2–3.4 | UX-AC-001–005, 112 |
| UX-US-002 | As a new local user, I want to register and verify my email, so that I can use protected language operations. | FR-001, FR-002, FR-037 | Signed out; `/register`; controlled registration/verification outcomes | AuthForm, `/verify-email`, Section 9.3 | UX-AC-006–010, 101–102, 104 |
| UX-US-003 | As a user, I want to sign in with Google or local credentials and recover from auth errors, so that I can access my workspace securely. | FR-001, FR-002, FR-037 | Signed out or expired; protected route/login entry; deterministic auth callback | AuthForm, login banner, Sections 3.5/9.3 | UX-AC-011–016, 101, 103 |
| UX-US-004 | As a local user who forgot my password, I want to request and complete a reset, so that I can regain access without account disclosure. | FR-002, FR-037 | Signed out; `/forgot-password` or valid/invalid link | AuthForm, messages 031/033/036 | UX-AC-017–020, 101–102 |
| UX-US-005 | As a language user, I want automatic detection or a manual supported source and an explicit translation target, so that the requested operation is unambiguous. | FR-004–FR-006, FR-009, FR-012 | Verified; either workspace page | LanguageSelect, Sections 7.1/8.1 | UX-AC-021, 027–029, 038, 088–091, 092 |
| UX-US-006 | As a translation user, I want valid text to translate automatically after I pause, so that I get a complete target-language result without a submit button. | FR-008–FR-011, NFR-003 | Verified, usage available; `/translate`; selected target | EditorPanel, debounce, Section 7.3 | UX-AC-022–025 |
| UX-US-007 | As a translation user, I want invalid, mixed, same-language, and oversized input handled clearly, so that I know what is processed and charged. | FR-004, FR-005, FR-007, FR-024, P-003 | Verified; `/translate`; controlled validation/count fixtures | Alerts, counter, boundary marker, Section 7.2 | UX-AC-021, 026–032, 109 |
| UX-US-008 | As a translation user, I want to edit/copy a complete result and be protected from stale responses, so that I can safely use the current text. | FR-011, FR-026, FR-028, NFR-003 | Existing result; `/translate` | Result editor, Copy, response ordering | UX-AC-033–037 |
| UX-US-009 | As a rewriting user, I want mandatory correction with one optional style or tone and a clear Correction-only override, so that the transformation is predictable. | FR-012–FR-016, FR-038 | New/active workspace; `/rewrite` | CorrectionOnly, StyleSelect, Section 8.1 | UX-AC-038–043, 090, 092–093, 110 |
| UX-US-010 | As a rewriting user, I want automatic complete rewrites that preserve my source and apply only current responses, so that typing remains safe during processing. | FR-016–FR-018, FR-022, NFR-003 | Verified/available; `/rewrite`; eligible input | EditorPanel, full-rewrite transitions | UX-AC-044–047, 109 |
| UX-US-011 | As a rewriting user, I want result-only change marks and original/revised sentence comparison, so that I can review corrections and copy clean text. | FR-019, FR-023, NFR-005 | Current rewrite result | ShowChanges, ComparisonPanel, Copy | UX-AC-048–051, 094, 111 |
| UX-US-012 | As a rewriting user, I want to generate, choose, and reuse sentence alternatives, so that I can refine one sentence without redoing the whole result. | FR-020, FR-021, FR-025, FR-035 | Current result with sentence metadata | SentenceMenu, AlternativesPanel, Section 8.6 | UX-AC-052–057, 097–099, 110–111 |
| UX-US-013 | As a rewriting user, I want manual edits to invalidate only affected sentence data and defeat earlier responses, so that my edits and unrelated choices remain intact. | FR-018, FR-022, FR-023, P-002, Q-002 | Current/pending rewrite result | Correspondence table, edit revision, messages 022–024 | UX-AC-058–064, 094, 096, 098, 100 |
| UX-US-014 | As a user, I want authoritative usage and clear allowance/budget states, so that I understand availability and successful charges. | FR-024–FR-028, NFR-006 | Verified; any protected page; usage fixtures | UsageDisclosure, counters, Section 9 | UX-AC-065–071, 105–107 |
| UX-US-015 | As a user, I want failures, offline periods, and interrupted work to preserve text and offer safe recovery, so that transient problems do not destroy my work or cause blind retries. | FR-034, FR-037, NFR-002, NFR-003 | Any operation state; controlled failures | InlineAlert, retry, precedence | UX-AC-072–076, 102, 107–108 |
| UX-US-016 | As a keyboard, touch, zoom, or assistive-technology user, I want the full interface to remain operable and understandable, so that I can complete core journeys independently. | NFR-005, NFR-007, RG-006 | All routes; representative viewports/browsers | Sections 4, 5, 10 | UX-AC-077–083, 092–095 |
| UX-US-017 | As a privacy-conscious user, I want an explicit workspace reset and teardown on session boundaries, so that my text is not restored later. | FR-038, NFR-004, Q-004 | Active workspace with/without text | AccountMenu, reset dialog, Section 3.4 | UX-AC-084–087 |

## 12. Deterministic UI acceptance contract

### 12.1 Harness and fixture contract

Scenarios are layer-independent acceptance contracts, not a Playwright test inventory or claims of passing tests. Pure logic defaults to MSTest/Vitest units; visible UI behavior defaults to Vitest with React Testing Library in a DOM environment. Retain Playwright only for behavior requiring a real browser and the small integrated smoke in #3 Section 9. A compound scenario may map to several focused checks without replaying its complete journey at each layer.

In fixture UI tests, replace transport at the application boundary and control all responses. In integrated smoke, run the real frontend, API, authentication, and migrated SQLite; replace only external provider/email dependencies. Neither deterministic suite calls live LLM, Google, email, or production account services. No test uses arbitrary sleeps.

Use fake timers for the 1,000 ms debounce, fixed system time `2026-09-07T17:40:00Z`, timezone `UTC` for assertions that include reset text, and controllable deferred responses for races. Flush framework work explicitly after advancing time. Each logical request has a harness correlation key solely for counting/ordering; this does not prescribe the API identifier format.

| Fixture | Deterministic state/outcome |
| --- | --- |
| `AUTH-OUT` | Signed out. |
| `AUTH-LOCAL-UNVERIFIED` | Local user `writer@example.test`, unverified. |
| `AUTH-OK` | Verified user `writer@example.test`; user usage 7,500/20,000; global available; budget available. |
| `AUTH-EXPIRED` | Bootstrap or next request returns session expired. |
| `USAGE-USER-END` | User remaining 0; reset `2026-09-08T00:00:00Z`. |
| `USAGE-GLOBAL-END` | Global unavailable; reset same; global consumption omitted. |
| `USAGE-BUDGET-END` | Monetary cap unavailable; no reliable resume time. |
| `T-OK-A` | Source `Hello, the meeting starts at 14:30. Please go.` (46 ASCII characters); detect English, target Romanian; output `Bună, întâlnirea începe la 14:30. Te rog să mergi.`; charge 46; initial 7,500 becomes 7,546. |
| `T-OK-B` | Source `The report is ready.` (20 ASCII characters); target Romanian; output `Raportul este gata.`; charge 20; standalone usage 7,520, A+B race final usage 7,566. |
| `T-UNCERTAIN` | Source `...`; detection uncertain; manual English override accepted by eligibility fixture; result `...`; no provider operation until override. |
| `T-MIXED` | Source `Hello everyone. Bonjour tout le monde. Всем привет.`; fixture rejects substantially mixed/unsupported, including manual overrides; no provider operation. |
| `T-LONG` | Source `a` repeated 5,312 times, synthetic eligibility fixture accepts English; output `Prefix translated.`; canonical total 5,312, prefix 5,000, omitted 312; charge 5,000; initial usage becomes 12,500. Boundary after the 5,000th `a`. |
| `W-OK` | Source `The report is really ready. We sends it today.` (46 ASCII characters). Result `The report is ready. We send it today.` (38 characters). Sentence keys w1=`The report is ready.` (20), w2=`We send it today.` (17), one inter-sentence space. w1 original=`The report is really ready.`; deletion=`really `; w2 original=`We sends it today.`; replacement sends→send. Charge 46; initial usage 7,546. |
| `ALT-OK` | For w1 length 20: ordered options `The report has been completed.`, `We have finished the report.`, `The completed report is available.`; one 20-character charge; after W-OK usage 7,566. w2 option fixture: `Today we send it.`, `We are sending it today.`, `It will be sent today.`; charge 17. |
| `FAIL-PROCESS` | Final processing failure; charge 0. |
| `FAIL-DEADLINE` | Final overall deadline; charge 0. |
| `FAIL-COPY` | Clipboard permission/write rejects. |
| `DEFER-A/B` | Independently resolve/reject A and B in any order and report authoritative usage. |

Additional deterministic content: `W-FOUR` source/result `One is ready. Two is ready. Three is ready. Four is ready.` with keys f1–f4 in order; no initial changes. For insert/delete use `New is ready. ` before f2, then remove f3 including its following separator. For merge remove the space/period between f1/f2 and replace with ` and `; for split use `Three is ready. It is checked.` in f3. Run each edit in a separate reset fixture so unaffected f4 is always asserted. `W-THREE` is f1–f3 only. `W-ZH` source `這份報告已準備好。`, result `这份报告已准备好。`, Chinese, no style change. All length boundaries use repeated ASCII `a` at L−1, L, L+1; the contract's Unicode fixtures additionally contain `A😀é\r\n中`, tabs, spaces, and a URL with supplied canonical total/prefix boundary (the actual Unicode policy comes from #5, never JS string length).

The additional boundary edit between f3/f4 is its own reset run and asserts unchanged f1/f2 instead of f4. Cached option fixtures for f1–f4 are `Item one/two/three/four is ready.` and `The first/second/third/fourth item is ready.` respectively (expand each paired word to the matching sentence). Selecting the first fixture option supplies the exact visible choice to preserve in non-touching-sentence assertions.

For all 12 Translation direction fixtures, use the source sentence `The report is ready.` / `Отчёт готов.` / `Raportul este gata.` / `报告已准备好。` for the selected source; return the corresponding sentence for the different target. For rewriting, use those same four sentences as unchanged-output fixtures, plus W-OK/W-ZH changes. Parameterize all eight style/tone values and both grammar-only settings against all four languages; fixture output remains exactly the source unless W-OK/W-ZH is selected. These tests establish controls/routing intent, not real linguistic transformation.

For 2-option and 4-option sets, take the first two ALT-OK entries or append `The report is finished and ready.` respectively. The multi-sentence alternative is `The report is complete. It is ready.` in place of the first option. The generated whole-deletion fixture uses W-FOUR source and result `One is ready. Three is ready. Four is ready.`, preserving f1/f3/f4 with f2 available only in original comparison. These fixtures specify display correspondence, not the algorithm that derives it.

Auth fixture data: email `writer@example.test`; accepted password `Maple!River2026`; rejected credentials return UX-MSG-028; password checklist fixture is minimum 12 characters only. Missing/mismatch run uses `short` and `different`; rejected-policy response returns UX-MSG-042. `invalid-link` and `expired-link` are harness route contexts, not prescribed token formats. Verification success starts from AUTH-LOCAL-UNVERIFIED; a separate signed-out variant requires sign-in. Auth duplicate-submit and navigation races use deferred promises exactly as language races do.

All scenarios start afresh with AUTH-OK, zero observed requests, 1,000 ms debounce, and initial usage 7,500 unless overridden. Browser checks default to Chromium 1440×900; pure unit/DOM checks require no browser or viewport. “Existing result” means seed W-OK/T-OK-A with its corresponding post-success usage and exclude that seed from observed request counts.

Execute state/race/counting parameter sets at the lowest sufficient layer. UI usage/messages still need component assertions even when server accounting is covered in MSTest. All fixture success/failure responses are explicitly released by the harness; never infer success from timing. Count logical language operations separately from eligibility, usage, status, and authentication requests.

Component queries and browser locators must use roles and accessible names (Testing Library equivalents are valid): `getByRole('link', {name:'Translation'})`, `getByRole('textbox', {name:'Source text'})`, `getByRole('textbox', {name:'Translation result'})`, `getByRole('textbox', {name:'Improved result'})`, `getByRole('combobox', {name:'Source language'})`, `getByRole('combobox', {name:'Target language'})`, `getByRole('combobox', {name:'Writing style or tone'})`, and named buttons/checkboxes from Section 5. Stable test IDs are permitted only for the invisible processed-boundary anchor (`translation-boundary`), sentence fixture wrapper (`sentence-<opaque-fixture-key>`), and live-region probe (`app-status`), because semantic locators cannot uniquely observe those internals. Production behavior must not depend on test IDs.

Common assertions include visible exact text, role/name/value/checked/disabled/busy/invalid state, focus target, URL, clean clipboard text, absence of source mutation, and logical request count/payload category. Storage assertions inspect localStorage, sessionStorage, IndexedDB database names, Cache Storage entries, URL, and history state for absence of source/result text.

### 12.2 Navigation and authentication scenarios

| ID / story | Given | When | Then |
| --- | --- | --- | --- |
| UX-AC-001 / US-001 | `AUTH-OK`, direct `/rewrite`, desktop | Bootstrap completes | URL is `/rewrite`; `Rewriting` is current; `h1` is focused; defaults are visible; zero operation requests. |
| UX-AC-002 / US-001 | Both pages contain distinct source/result fixtures; operation idle | Activate `Translation`, then browser Back, then Forward | Each route restores its own values and scroll; focus is destination `h1`; no request is caused by navigation. |
| UX-AC-003 / US-001 | `/translate` has text/settings | Reload | Same route returns with empty source/result, Detect automatically, no target, and default writing settings in memory; storage contains no text. |
| UX-AC-004 / US-001 | Translation A is deferred; navigate to Rewriting | Resolve A, then return to Translation | A applies only to Translation if still current; Rewriting value/focus never changes; usage reconciles once. |
| UX-AC-005 / US-001 | Translation A deferred; navigate and edit Translation through Back/Forward to create B | Resolve A after B is submitted | A never replaces B/previous result; current route/focus stays; stale-success notice appears only if A succeeds. |
| UX-AC-006 / US-002 | `AUTH-OUT`, `/register`, valid local fields and password-policy fixture | Activate `Create account`, registration succeeds with an unverified session | One registration request; URL `/verify-email`; UX-MSG-034 and `writer@example.test` visible; no protected workspace. |
| UX-AC-007 / US-002 | Registration form missing/invalid email and mismatched confirmation | Submit | No request; summary and field errors visible; first invalid field focused; fields have `aria-invalid` and descriptions. |
| UX-AC-008 / US-002 | `AUTH-LOCAL-UNVERIFIED`, direct `/translate` | Bootstrap | Replace-navigation `/verify-email`; UX-MSG-019; no editor/operation request; `Resend verification email` available. |
| UX-AC-009 / US-002 | Verification page, resend success with server cooldown 60 seconds | Activate resend | One resend request; UX-MSG-035 announced; control disabled with visible controlled countdown; fake-clock expiry reenables it. |
| UX-AC-010 / US-002 | Verification link fixture valid, last requested `/rewrite` | Open link; resolve success; activate `Continue to Rewriting` | Success text visible; protected navigation reaches `/rewrite`; zero language-operation requests. |
| UX-AC-011 / US-003 | `AUTH-OUT`, protected `/rewrite` | Complete mocked Google callback successfully | Callback busy text appears; return URL `/rewrite`; verified workspace shown; one auth exchange, no live Google page. |
| UX-AC-012 / US-003 | Login entry | Mock Google cancellation, then provider failure | UX-MSG-029 then UX-MSG-030 appear on respective runs; focus returns to `Continue with Google`; local form remains usable. |
| UX-AC-013 / US-003 | `/login`, valid local credentials | Submit; auth succeeds with original route `/translate` | One login request; replace-navigation `/translate`; Translation heading focused; no password remains in DOM/storage/history. |
| UX-AC-014 / US-003 | `/login`, invalid credentials fixture | Submit | UX-MSG-028; email focused; password cleared; no account-specific detail. |
| UX-AC-015 / US-003 | Active workspace has sensitive fixture text | Next operation returns `AUTH-EXPIRED` | Text/results disappear before `/login` renders; UX-MSG-037; storage/history have no text; stale response cannot restore it. |
| UX-AC-016 / US-003 | Verified workspace with text; deferred sign-out success/failure variants | Open account menu and activate `Sign out`; resolve; retry failed variant | One sign-out action per activation; immediate teardown and Signing out status on `/login`; Back does not expose text. Failure shows exact 3.4 banner and Try sign-out again; no auto-redirect; successful retry exposes login form. |
| UX-AC-017 / US-004 | `/forgot-password`, syntactically valid unknown email | Submit success fixture | One request; UX-MSG-031 exactly; same result as known-email fixture; success heading focused, with Back to sign in next in tab order. |
| UX-AC-018 / US-004 | Forgot-password invalid email | Submit | No request; field error visible/associated; email focused. |
| UX-AC-019 / US-004 | Valid reset-link fixture and password-policy fixture | Enter matching valid passwords; submit success | One reset request; UX-MSG-036; `Go to sign in` reaches `/login`; no auto-login/workspace restoration. |
| UX-AC-020 / US-004 | Invalid/expired reset-link fixture | Open `/reset-password` | UX-MSG-033 and `Request a new reset link`; no password fields or reset submission. |

### 12.3 Language and Translation scenarios

| ID / story | Given | When | Then |
| --- | --- | --- | --- |
| UX-AC-021 / US-005, US-007 | `AUTH-OK`, Translation initial state | Enter three ASCII spaces, advance 5 seconds; then choose each target option | Zero operations for whitespace; counter displays the #5 fixture's count (3 here), not a charge; target offers exactly four languages and no Translate button exists. |
| UX-AC-022 / US-006 | Target Romanian, `T-OK-A` | Fill source, advance 999 ms, then 1 ms; resolve | 0 then 1 request; UX-MSG-003; complete Romanian fixture replaces placeholder; UX-MSG-005; source focus/value unchanged; usage 7,546. Parameterize eligibility completion at 500/1,500 ms: submission at 1,000/1,500 ms respectively, never another full pause after eligibility. |
| UX-AC-023 / US-006 | Valid source/target | Type three edits 400 ms apart, then advance 999/1 ms | Debounce restarts; exactly one request with final value; no arbitrary wait. |
| UX-AC-024 / US-006 | Existing result; source edit then target change inside debounce | Advance controlled clock | Previous result stays with UX-MSG-002 then 004; exactly one request carries final source/target; no partial result appears. |
| UX-AC-025 / US-006 | Chinese IME-capable source and target English | Dispatch compositionstart/update events, advance 5 seconds, compositionend, then 999/1 ms | Zero requests during composition; one only after full post-composition pause; caret/value preserved. Run the timing permutations in Vitest and one composition-event smoke in Chromium/Firefox; actual native IME remains Section 10.4 manual evidence. |
| UX-AC-026 / US-007 | Existing translation result | Clear source; advance 2 seconds | Source empty/focused; no new request; old result and `Source cleared — previous result shown.` remain copyable. |
| UX-AC-027 / US-005, US-007 | Detect automatically, `T-UNCERTAIN`, valid target | Validation resolves | UX-MSG-006; source combobox invalid; zero provider operations/charge. Select English and advance 1 second → one request automatically. |
| UX-AC-028 / US-005, US-007 | Source Russian, target English, no text; change source to English | Make source eligible | Retained target English is disabled in its menu and invalid as current value; UX-MSG-007; zero operation. Select Romanian and advance 1 second → one request. |
| UX-AC-029 / US-005, US-007 | `T-MIXED` | Enter mixed fixture and advance | UX-MSG-008; zero provider requests/charge; manually choosing a language does not bypass server unsupported validation. |
| UX-AC-030 / US-007 | `T-LONG`, target Romanian | Enter 5,312 canonical characters | UX-MSG-009 says 312; counter `5,000 processed · 5,312 entered`; boundary test ID lies at fixture boundary; full textbox value remains 5,312. |
| UX-AC-031 / US-007 | Prior scenario | Advance 1 second; resolve success | Request fixture declares 5,000 submitted; UX-MSG-010; usage rises exactly 5,000; copy returns only result clean text; source suffix remains editable. |
| UX-AC-032 / US-007 | Long request deferred | Clear source before response; resolve success | Source stays empty; result is not applied; server usage updates and UX-MSG-021 reports 5,000; no second request. |
| UX-AC-033 / US-008 | Applied translation | Edit result then copy | No operation request/usage change; result status `Edited`; clipboard exactly equals edited value; UX-MSG-026. |
| UX-AC-034 / US-008 | Existing result; submit deferred A, then edit source and submit B | Resolve B before A | B alone applies; A cannot overwrite; source equals newest text; request count 2. |
| UX-AC-035 / US-008 | Same race | Resolve A success after B with authoritative usage | Result stays B; usage becomes returned value once; UX-MSG-021 count matches A; focus unchanged. |
| UX-AC-036 / US-008 | Existing result; `FAIL-PROCESS` then retry success | Submit/resolve failure; activate `Try again`; resolve | UX-MSG-013 and prior result persist; retry makes one new logical UI request; success replaces result; failed outcome added zero usage. |
| UX-AC-037 / US-008 | Submitted translation update with previous result | Attempt typing in result; copy previous | Result textbox is read-only with helper; value unchanged; Copy enabled and clipboard gets prior result; becomes editable after completion. |

### 12.4 Rewriting full-result scenarios

| ID / story | Given | When | Then |
| --- | --- | --- | --- |
| UX-AC-038 / US-005, US-009 | New workspace `/rewrite` | Inspect controls, enter whitespace | Detect automatically; Correction only unchecked; Styles `None set`; Show changes checked; zero requests. |
| UX-AC-039 / US-009 | Eligible source and style `None set` | Advance 1 second; resolve `W-OK` | One full rewrite request expresses mandatory correction and no style/tone; complete same-language result applies. |
| UX-AC-040 / US-009 | Result current | Open Styles and select `Business`, then `Friendly` before debounce | Only Friendly selected; menu closes/focus returns; one final request after 1 second; result marked waiting/updating. |
| UX-AC-041 / US-009 | Styles `Diplomatic` | Enable Correction only | Dropdown disabled but displays Diplomatic; helper exact; one correction-only request after debounce; grammar correction remains active. |
| UX-AC-042 / US-009 | Prior scenario with current correction-only result | Disable Correction only | Diplomatic restored without extra choice; dropdown enabled; exactly one new full rewrite after debounce. New workspace later resets None set. |
| UX-AC-043 / US-009 | Chinese composition in Rewrite | Compose, finish, advance controlled clock | No request during composition; full one-second delay after end; setting changes during composition defer to same final request. |
| UX-AC-044 / US-010 | Existing W result; valid source edit | Advance 999/1 ms; defer then resolve current success | Previous result editable with UX-MSG-002/004; source/caret intact; complete replacement applies once; all old alternative choices reset. |
| UX-AC-045 / US-010 | Canonical 2,001-character rewrite fixture | Enter text and advance | UX-MSG-011 says remove 1; source fully editable; previous result/copy preserved; zero full rewrite requests/charge. Delete one char → automatic request after 1 second. |
| UX-AC-046 / US-010 | Submit A; change style/source and submit B | Resolve A first, B second | A never applies; if successful usage updates with UX-MSG-021; B applies; source and controls retain newest values. |
| UX-AC-047 / US-010 | Submit A; manually edit result before A resolves | Resolve A success | Manual edit remains; UX-MSG-022 then 021; A counts as returned; no model request caused by result edit. |

### 12.5 Change, alternatives, and manual-edit scenarios

| ID / story | Given | When | Then |
| --- | --- | --- | --- |
| UX-AC-048 / US-011 | `W-OK` with changed and deleted spans | Resolve current rewrite | Show changes checked; revised result words have underline/background; accessible result value equals clean text; no deleted words in clean result. |
| UX-AC-049 / US-011 | Prior state; result caret in changed sentence | Activate Sentence actions → Compare with source | Panel heading/Original/Rewritten visible; deleted spans struck through and programmatically described; panel heading focused; Escape returns to sentence trigger/caret. |
| UX-AC-050 / US-011 | Change display on | Toggle Show changes off/on | Highlight nodes hide/return; clean result and sentence metadata unchanged; request count/usage unchanged; focus stays toggle. |
| UX-AC-051 / US-011 | Result includes highlights, deletion metadata, and a selected alternative | Activate Copy result | Clipboard equals current clean text including chosen alternative, with no annotation markup/deleted text; UX-MSG-026. `FAIL-COPY` run shows UX-MSG-027 and preserves selection. |
| UX-AC-052 / US-012 | Current W-OK, selected w1 length 20, no cache, `ALT-OK` | Activate Generate alternatives; resolve | One alternatives request; loading text/skeletons then exactly 3 option buttons; usage +20 once; focus stays on panel heading on completion, then Tab reaches first option. |
| UX-AC-053 / US-012 | Three alternatives ready; other sentence already has chosen option | Activate second option | Only selected sentence text changes; panel closes; caret at replacement end; other sentence choice persists; zero new requests/charge. |
| UX-AC-054 / US-012 | Cached options and chosen alternative | Reopen same sentence; activate Use original wording | `Show alternatives`; same 3 options and selected state on reopening; local action restores the original result sentence, closes panel, and places caret at its end; request count and usage unchanged; generated options remain cached. |
| UX-AC-055 / US-012 | Alternatives A loading | Select sentence B, generate B; resolve A after B | A options never appear in B; A successful charge is reflected with UX-MSG-021; B remains loading/current then applies its options. |
| UX-AC-056 / US-012 | Current alternatives request, `FAIL-PROCESS` | Resolve failure, then activate Try again and resolve `ALT-OK` | UX-MSG-025; result unchanged; failed request adds no usage; retry produces options and one successful sentence charge. |
| UX-AC-057 / US-012 | Cached alternatives open | Change style or source | Panel closes immediately; prior result marked waiting/outdated; focus initiating control; next full success resets all old caches/choices. |
| UX-AC-058 / US-013 | Full rewrite A pending, result editable | Change characters inside sentence 1; repeat with result IME composition | UX-MSG-022; source unchanged; no request/charge; sentence 1 comparison/cache invalid after commit; unaffected metadata intact. A response arriving after compositionstart but before compositionend cannot overwrite interim text. Cancelling composition preserves unchanged metadata without reviving A. |
| UX-AC-059 / US-013 | Prior scenario | Resolve A success | Manual result remains; usage updates once with UX-MSG-021; result focus/caret stays; A text absent. |
| UX-AC-060 / US-013 | Manual-edited result | Edit source and resolve later current full rewrite | Manual edits and all prior choices are replaced by complete result; source stays current; metadata comes only from new result. |
| UX-AC-061 / US-013 | Alternatives for sentence A loading | Edit A and resolve alternatives success | Panel closes; A options never appear; UX-MSG-024 on next activation; successful charge shown; no full rewrite. |
| UX-AC-062 / US-013 | Alternatives A loading | Edit non-touching sentence B; then resolve A success | Panel closes and A's new options never apply because every earlier response is outdated; successful A usage is reconciled. Only B's cached metadata is invalidated; any pre-existing A metadata and source remain unchanged. |
| UX-AC-063 / US-013 | Three-sentence result with caches/choices | Insert new sentence between 1/2, delete whole sentence 3 | New sentence has no comparison/cache; deleted metadata gone; unchanged sentences keep exact visible choices; no model request. |
| UX-AC-064 / US-013 | Four-sentence result with metadata | Merge 1/2, split 3, edit boundary between 3/4 | Only touching groups lose comparison/cache and get edited state; every non-touching sentence fixture retains opaque key and option; no whole-result reset. |

### 12.6 Usage, availability, and failure scenarios

| ID / story | Given | When | Then |
| --- | --- | --- | --- |
| UX-AC-065 / US-014 | `AUTH-OK` | Open Usage | Button/panel show 7,500 used, 12,500 remaining, progress numeric value, reset 00:00 UTC, and accounting explanation; no model/global/budget amount. |
| UX-AC-066 / US-014 | T-OK-A translation of 46 then W-OK rewrite of 46 | Resolve responses | Usage is replaced with 7,546 then 7,592; no optimistic increment; shared meter reflects both operations. |
| UX-AC-067 / US-014 | A provider retry/fallback fixture succeeds once | Resolve logical operation | Result applies, one authoritative charge only; retry/model details absent. Integrated single-charge proof is deferred to integration tests. |
| UX-AC-068 / US-014 | `USAGE-USER-END`, valid source | Advance clock; then server reset signal changes availability | UX-MSG-015, no operation; text safe. After authoritative refresh, message clears and exactly one operation submits after a fresh 1,000 ms. |
| UX-AC-069 / US-014 | `USAGE-GLOBAL-END` | Attempt either operation | UX-MSG-016 and reset; no global used number; no operation; source/result/settings preserved. |
| UX-AC-070 / US-014 | `USAGE-BUDGET-END` | Attempt; activate Check availability while still blocked | UX-MSG-017; status-only request once; no text operation/automatic loop; work/copy preserved. |
| UX-AC-071 / US-014 | Successful result whose usage refresh subrequest fails | Resolve operation, fail usage refresh, then open Usage and resolve refresh | Result remains successful; label `Usage update unavailable`; opening panel retries; returned usage replaces stale value. |
| UX-AC-072 / US-015 | Existing rewrite result, `FAIL-PROCESS` | Submit current operation; resolve | UX-MSG-013; source/result/settings/caret preserved; charge 0; Try again visible. |
| UX-AC-073 / US-015 | First translation, `FAIL-DEADLINE` | Resolve at controlled overall deadline | Empty result placeholder remains; UX-MSG-014; source preserved; charge 0; no intermediate provider text. |
| UX-AC-074 / US-015 | Browser offline with valid changed source | Advance 5 seconds; dispatch online; advance 999/1 ms | UX-MSG-012; zero offline requests; on online one new debounce then one request; no focus movement. |
| UX-AC-075 / US-015 | Submitted operation; transport disconnect; reconciliation fixture pending | Trigger disconnect | Text/result preserved; `Processing interrupted. Checking the operation status…`; no blind resubmit. Resolve success/failure → exactly matching final state and charge. |
| UX-AC-076 / US-015 | Failure A arrives after newer B success | Resolve B success then A failure | B result/status remain; no alert/toast for stale failure; focus/usage unchanged by A failure. |

### 12.7 Responsive, accessibility, and lifecycle scenarios

| ID / story | Given | When | Then |
| --- | --- | --- | --- |
| UX-AC-077 / US-016 | Each route, `320×640` and fixed fonts/content | Run browser geometry/overflow assertions; no pixel baseline required | One column, all controls visible/reachable, no horizontal page overflow, language labels usable, sheets fit viewport. |
| UX-AC-078 / US-016 | Workspace at `1440×900`, `1024×768`, `390×844` | Check curated screenshots from 12.10 and breakpoint geometry; check remaining state rendering in components | Layout matches Section 4; no clipped long English/Chinese content; only the curated screenshots require pixel baselines, with semantic state coverage retained in components. |
| UX-AC-079 / US-016 | Keyboard-only; Chromium full browser-sensitive subset, WebKit/Firefox focused smoke plus manual AT per 10.4–10.5 | Traverse translation/rewrite/alternative/comparison keyboard flows in Chromium; sample shared primitives in other engines | Focus order matches 5.3; every focus visible/unobscured; Escape closes/returns; no keyboard trap; source caret unchanged by operations. |
| UX-AC-080 / US-016 | Result caret in sentence; pointer unavailable | Use Sentence actions list and `Alt+ArrowDown` | Same menu/actions/options as pointer path; accessible sentence excerpt/ordinal; focus restoration exact. |
| UX-AC-081 / US-016 | Automated accessibility scan and token contrast check on all important states | Run scanner/calculator | No serious/critical automated violations; prescribed foreground/background and focus pairs meet ratios; errors/states have non-color cues. Manual limitations remain stated. |
| UX-AC-082 / US-016 | Fake timer and live-region observer | Type repeatedly, submit, resolve, copy, produce validation error | No announcement per key/debounce restart; one start, one completion, one copy; changed error announced once; applied result text not auto-read. |
| UX-AC-083 / US-016 | Reduced motion and text-spacing overrides | Exercise loading, menus, long auth error | No shimmer/transition; status still understandable; prescribed spacing causes no clipping/overlap/loss. |
| UX-AC-084 / US-017 | Active text/results/settings | Activate Start new workspace, then Cancel | Dialog exact copy; initial focus Cancel; Escape/Cancel preserves everything and returns account-menu trigger. |
| UX-AC-085 / US-017 | Same state with two pending operations | Confirm Start new workspace; later resolve both | Both pages clear/default; `/translate`; source focused; no response applies or restores content; usage may reconcile only from authoritative stale successes without text. |
| UX-AC-086 / US-017 | Empty new workspace | Activate Start new workspace | No dialog; defaults remain; `/translate`; source focused; zero operation requests. |
| UX-AC-087 / US-017 | Workspace uses distinctive source/result strings | Switch pages, inspect URL/history/storage; reload; simulate bfcache pageshow and tab duplication fixture | Strings exist only in live DOM memory before boundary; never in URL/history/storage/cache; all boundary restorations are empty. A synthetic pageshow only proves handler behavior; real navigation/reload/restoration browser checks plus backend privacy integration establish the wider contract. #6 records if actual bfcache entry or tab duplication could not be exercised; it must not label synthetic events as that evidence. |

### 12.8 Additional language scenarios

| ID / story | Given | When | Then |
| --- | --- | --- | --- |
| UX-AC-088 / US-005 | Rewrite uses Detect automatically and T-UNCERTAIN eligibility fixture; separate failed-detection variant | Enter ambiguous `...`; advance clock; resolve eligibility | UX-MSG-006; zero provider operations/charge. Select English and advance 1 second → one request with output `...`. Failed-detection variant shows UX-MSG-013/Try again, no fabricated language or operation; explicit retry plus successful eligibility resumes under 7.1's timing. |
| UX-AC-089 / US-005 | Rewrite uses `T-MIXED`-equivalent validation | Enter substantially mixed/unsupported text | UX-MSG-008; source remains editable; previous result/copy preserved; manual source selection cannot bypass content validation; zero provider operations. |
| UX-AC-090 / US-005, US-009 | Traditional Chinese source fixture; service returns Simplified Chinese rewrite | Select Chinese or detect it, then resolve current rewrite | Closed language control describes Simplified output; source retains Traditional text; result shows fixture Simplified text; no translation target is present. Script quality remains document #6 evidence. |
| UX-AC-091 / US-005 | Manual source is each of English, Russian, Romanian, Chinese in turn | Open Target language and exercise fixtures for each other target | Exactly the three different targets are enabled, same-language option is disabled with reason, and the harness can submit each of all 12 directed pairs once. No style/tone controls appear. |

### 12.9 Completion and recovery scenarios

| ID / story | Given | When | Then |
| --- | --- | --- | --- |
| UX-AC-092 / US-005, US-009, US-016 | Source/style select with a committed value; use keyboard, repeat with touch | Open, highlight another option using arrows/typeahead, Escape; reopen and Enter; repeat Tab/outside dismissal | Escape/Tab/outside do not commit or request; Enter commits once and returns focus as 5.4 specifies; selected value and active descendant distinct. Account menu arrows/Enter/Escape work equivalently without premature action. |
| UX-AC-093 / US-009, US-016 | W-OK at 1024×768 and 390×844 | Open Writing tools, toggle Correction only, close; reopen and resize to 1440 | Modal traps focus while open, exact controls/helper shown; closes with focus return; desktop renders rail and only one Show changes control; one debounce per actual mode change. |
| UX-AC-094 / US-011, US-013, US-016 | W-OK source caret saved at offset 5 | Select result words by drag/Shift; paste plain text; activate sentence affordance | Ordinary text selection/editing works without unsolicited menu; explicit action opens it; source text/caret remain intact; result editing invalidates only affected metadata. |
| UX-AC-095 / US-016 | Truncated control-label fixture, tooltip, forced-colors/reduced-motion lane | Focus trigger; hover tooltip; Escape | Full label exposed, tooltip stays while hovered, closes on Escape; focus visible and unchanged; no request. |
| UX-AC-096 / US-013 | Current W-OK; source edit schedules rewrite | At 500 ms manually edit result; advance 2 seconds | Earlier debounce cancelled; no request; manual result survives, outdated badge/helper displayed. Later source edit plus 1 second submits a new rewrite. |
| UX-AC-097 / US-012 | ALT-OK deferred, source/settings unchanged | Close then reopen the same sentence before completion; close, resolve, reopen again | One logical alternatives request throughout; loading reused; completion while closed steals no focus; cached ready options shown on reopening; one charge. |
| UX-AC-098 / US-012, US-013 | w1 alternatives pending, cached w2 options ready | Close w1, choose a cached w2 option, resolve w1 success | w2 replacement preserved; w1 pending response discarded; only its successful charge updates usage; no full rewrite/new request from selection. |
| UX-AC-099 / US-012 | W-OK outdated after source change, then current update fails | Activate old sentence actions | Previous result comparison uses old Original with explicit label; Generate/Show alternatives disabled; zero alternatives requests; editable result/Copy remain available; retry success unlocks new revision. |
| UX-AC-100 / US-013 | W-FOUR with f4 cached alternatives | Edit/undo f1, then clear whole result with select-all/delete | Undo restores clean text but not f1 metadata/pending responses; f4 stays cached until its deletion; empty result remains editable; Copy/Sentence actions disabled; source unchanged; zero requests. |
| UX-AC-101 / US-002, US-003, US-004 | Auth forms with fixture password policy | Toggle password visibility; submit missing/mismatched/short values; then valid values with deferred rejection and duplicate Enter | Reveal preserves selection; exact UX-MSG-038–042 and first-error focus; invalid run sends zero; pending valid run sends one; rejection UX-MSG-043 for register or UX-MSG-028 for login; password cleared and recovery controls enabled. |
| UX-AC-102 / US-002, US-004, US-015 | Resend/reset email forms and registration/reset-password forms | Release delivery failure or network failure, then explicitly retry success | Email failure UX-MSG-044; other form failure exact generic copy; no false success; fields enabled; one request per activation, no automatic retry. |
| UX-AC-103 / US-003 | Local login deferred | Navigate to Forgot password, then resolve old login | Forgot-password route/heading remain, old completion does not redirect or show error there; auth state reconciles on next protected navigation; no passwords retained. |
| UX-AC-104 / US-002 | Valid, invalid, expired, already-verified link variants, signed-in and signed-out | Open link, resend where allowed; invoke I’ve verified my email with unverified then verified outcomes | Exact link error/success and email-entry variant; no unauthenticated protected access; status check sends one request per click; signed-out success uses Sign in to continue; no automatic language request. |
| UX-AC-105 / US-014 | User remaining 19; T-OK-B needs 20, then shorter supported fixture `Hi.` needs 3 | Try 20-character input, then edit to Hi. | Insufficient-allowance message reports 20/19; no successful operation; shorter input resumes after 1 second and usage reflects +3. Repeat alternatives with w1=20 → choose shorter w2=17; copy/local choices remain usable. |
| UX-AC-106 / US-014 | Usage response B is newer than delayed response A; reset period changes in another run | Deliver snapshots out of order | Old snapshot never reduces current used count; new reset period legitimately resets value; no stale text/counter update; one fresh usage read if ordering is ambiguous. |
| UX-AC-107 / US-014, US-015 | Offline plus over-limit rewrite plus exhausted usage, old failure A pending | Deliver A error and change validation state | Source error and availability banner coexist; no processing; stale A alert suppressed; reconnect does not bypass allowance; authoritative availability plus eligible text is needed to resume. |
| UX-AC-108 / US-015 | Submitted operation interrupted, status never resolves | Advance to PRD deadline, then activate Check status | Spinner stops at deadline; exact unknown-outcome text; Check status sends only status request; no blind retry/no claim of zero charge; source/result preserved. A later success updates usage but cannot replace a manually edited/newer result. |
| UX-AC-109 / US-007, US-010 | Counts L−1, L, L+1 for L=5,000 Translation and 2,000 Rewrite; Unicode count/boundary fixtures | Enter, process when eligible, activate Show processed boundary on long Translation | Exact canonical counts; Translation never sends suffix even to eligibility; marker never changes textbox value/caret; rewrite blocks only L+1; deleting excess automatically resumes. |
| UX-AC-110 / US-009, US-012 | Four rewriting languages; each of eight styles/tones plus None set/off and Correction only/on; options-count variants 2, 3, 4 | Select each mode, resolve full fixture; generate and select alternatives | Exactly one exclusive setting with correction always requested; complete same-language fixture; 2–4 buttons accepted, default 3; Correction only still permits explicit alternative phrasing. Invalid option sets 1/5/empty/duplicate are final processing failure fixtures with UX-MSG-025, no usable cache or charge. |
| UX-AC-111 / US-011, US-012 | W-OK pure deletion w1 and W-FOUR variant deleting f2 entirely in generated output; cached multi-sentence alternative | Open comparison, select multi-sentence alternative, reopen | Deleted wording appears only in Original/comparison; deleted-sentence entry discoverable without inline insertion; replacement group stays selectable and cache reused; other identities preserved; Copy is clean current result. |
| UX-AC-112 / US-001 | AUTH-OUT/AUTH-LOCAL-UNVERIFIED/AUTH-OK root or unknown route | Navigate `/` and `/missing` | Root destinations match 3.1; unknown route shows Page not found and correct Go to action; keyboard activation reaches expected route with heading focus; zero language requests. |

### 12.10 Visual regression suite

Screenshots cover distinct layout structures, not every state × viewport × engine. The initial curated baseline inventory is:

| Layout | Baselines |
| --- | --- |
| Translation: long-input boundary with previous result updating | 1440×900 and 390×844 |
| Rewriting: changed result and comparison surface | 1440×900 and 390×844 |
| Rewriting: alternatives ready beside the desktop tools rail; narrow Writing tools sheet in a separate state | 1440×900 and 390×844 respectively |
| Registration: long validation errors | 1440×900 and 390×844 |
| Workspace-reset dialog | 390×844 |

These nine baselines are the starting set, not a permanent test-count limit. Add one only for a distinct layout risk unsupported by existing evidence. Empty/loading/success/failure/expired-link copy and control-state combinations remain DOM component checks; small browser geometry assertions cover any unique layout risk. The 320×640 reflow and 1024×768 breakpoint probes remain real-browser checks without separate pixel baselines. DOM emulators do not perform real layout; moving tests to Vitest does not prove clipping, reflow, zoom, caret positioning, or screenshots. Do not introduce a second browser runner through Vitest Browser Mode solely to rename browser tests as component tests.

For screenshot runs, substitute locally bundled Noto Sans and Noto Sans SC for the system-font stack; pin files and the Chromium/container version in #6's evidence. Wait for font readiness, use device scale factor 1, disabled caret, fixed time, reduced motion, deterministic scrollbars, and fixture output. Start with per-pixel color threshold 0.1 and maximum differing-pixel ratio 0.001. If rendering noise requires adjustment, record the measured reason; never automatically accept regenerated baselines or loosen thresholds to hide missing controls. Structural/semantic assertions pass independently. No Firefox/WebKit screenshot baselines are required by this contract.

Snapshots establish layout/state, not wording quality, native device behavior, or complete accessibility.

## 13. Verification boundaries

| Evidence class | What this specification's UI contract can establish | What it must not claim |
| --- | --- | --- |
| Unit / DOM component tests | State policy, forms/messages, request counts, ordering, semantic names/states, live-region mutations | Real browser layout/clipboard/IME/history/bfcache or server accounting |
| Fixture browser contracts | Real routing/editing/focus/clipboard, storage observation, reflow, curated layout | Real account/provider behavior or server atomicity when API responses are intercepted |
| Integrated browser smoke | Published frontend/API/auth/database wiring with external adapters faked | Live Google/email/provider delivery or language quality |
| API/accounting integration | Error-category mapping, authoritative counts/usage, successful stale charging, single logical charge, allowance concurrency, reset, reconciliation | Language usefulness or full browser accessibility |
| Document #6 language/model evaluation | Correct languages/scripts, meaning/factual/structural preservation, usable outputs, option distinctness/context fit, fallback quality | Pixel/UI behavior unless explicitly coupled to UI fixtures |
| Performance/reliability tests | NFR-002 timings/deadlines, concurrency, fallback, interrupted-operation semantics, no overrun/duplicate charge | A mocked spinner duration as proof of performance |
| Automated accessibility | Semantic names/roles/states, keyboard paths, focus, contrast, reflow, live-region mutations | Complete screen-reader comprehension, speech input, cognitive usability, real mobile keyboard behavior |
| Manual AT/device checks | Practical NVDA/VoiceOver/TalkBack, zoom, mobile keyboard, speech-control experience | Universal accessibility across untested combinations |

No scenario above is a test result. Document #6 must assign executable checks, environments, evidence, and status.

## 14. UX-scoped traceability matrix

The default allocation below replaces the earlier browser-wide reading of Section 12. #6 assigns executable check IDs and records any further split by assertion; none of the 112 stable UX-AC IDs is dropped.

| Assertion family / example IDs | Primary verification | Retained boundary evidence |
| --- | --- | --- |
| State machines, races, revision/cache correspondence: 023–024, 034–035, 046–047, 058–064, 096–100 | Vitest pure units; a focused component check for each distinct visible outcome | Browser only for native editing/caret behavior |
| Form validation and auth state: 007–020, 101–104 | Vitest form components; MSTest policy and real-auth API integration | Small login/focus/navigation browser path; actual external-auth/email release evidence |
| Accounting/recovery display: 065–076, 105–108 | MSTest pure policy plus SQLite/API integrity checks; Vitest counters/messages/status-request behavior | Integrated smoke confirms one real usage update; no browser accounting permutation matrix |
| Language/mode/count permutations: 021, 027–032, 038–045, 088–091, 109–110 | Units and components with shared canonical fixtures; API authoritative validation | Native composition and processed-boundary browser probes |
| Semantics/live regions: 081–082 | Component semantics, live-region assertions, pure color calculations | Real-browser accessibility scans for curated states; manual AT for interpretation |
| Browser editing, clipboard, history, privacy, keyboard, layout: 001–003, 015–016, 025, 033, 037, 049–051, 077–080, 083–087, 092–095 | Focused Playwright subset per 10.5; split logic branches into units/components | Manual device/AT evidence per 10.4; curated visuals per 12.10 |

IDs can appear in more than one row because assertions cross boundaries, not because every scenario must be duplicated. Unlisted IDs follow the same lowest-sufficient-layer rule. `UI` in the traceability matrix means component evidence by default, with browser evidence only when its assertion needs a real browser. `UI + integration` means visible component assertions plus focused server contract/accounting checks; `Evaluation` means document #6 language/provider evaluation. This matrix feeds but does not replace #6's product-wide coverage table.

| PRD ID | UX section / component | Story | Scenarios | Verification / boundary |
| --- | --- | --- | --- | --- |
| FR-001 | Routes `/login`, `/register`; AuthForm | US-002, US-003 | AC-006–007, 011–016, 101–103 | Mocked UI; live Google/local integration in #6 |
| FR-002 | `/verify-email`, recovery routes | US-002, US-004 | AC-008–010, 017–020, 101–104 | Mocked UI; email/token integration in #5/#6 |
| FR-003 | IA, ModeNav | US-001 | AC-001–005, 112 | UI routes/history |
| FR-004 | LanguageSelect; validation states | US-005, US-007 | AC-021, 027–029, 088–089, 092 | UI fixtures + API validation contract |
| FR-005 | Supported/mixed input message | US-005, US-007 | AC-021, 027, 029, 088–089 | UI fixture; language classification evaluation |
| FR-006 | Chinese labels, fonts, IME | US-005, US-006, US-009 | AC-025, 043, 078, 090 | UI IME/visual; output script in evaluation |
| FR-007 | Counters, translation boundary, rewrite block | US-007, US-010 | AC-026, 030–032, 045, 109 | UI + authoritative count integration (#5) |
| FR-008 | Plain-text editor/copy preservation | US-006, US-008, US-011 | AC-022, 033, 051 | UI structure/clipboard; fidelity evaluation |
| FR-009 | Exact language options | US-005 | AC-021, 027–029, 091 | UI enumerates/submits all 12 fixture directions; quality in evaluation |
| FR-010 | Translation result, no style controls | US-006 | AC-022, 038 | UI absence/target; output quality in #6 |
| FR-011 | Debounce, complete/current result, edit/copy | US-006, US-008 | AC-022–025, 033–037 | Controlled clock/races/clipboard |
| FR-012 | Writing-language selector, no target | US-005, US-009 | AC-038–039, 043, 088–090 | UI; same-language output in evaluation |
| FR-013 | Mandatory correction copy/mode | US-009 | AC-038–042, 110 | UI payload intent; quality in #4/#6 |
| FR-014 | Exclusive list/defaults | US-009 | AC-038–040, 092–093, 110 | UI selection/request contract |
| FR-015 | Correction-only override/restoration | US-009 | AC-041–042, 093 | UI; restoration resolves Q-002 presentation |
| FR-016 | Rewrite timing/IME | US-009, US-010 | AC-039–044 | Controlled clock/events |
| FR-017 | Source protection | US-010–US-013 | AC-044–047, 052–064, 094, 100 | DOM value/caret assertions |
| FR-018 | Previous/current/failed result | US-010, US-013, US-015 | AC-044, 046–047, 058–060, 072, 076, 096, 099 | Response-order UI; reliability integration |
| FR-019 | ChangedText, ComparisonPanel, Copy | US-011 | AC-048–051, 094, 111 | UI/clipboard/accessibility |
| FR-020 | SentenceMenu/AlternativesPanel | US-012 | AC-052, 055–056, 097–099, 110–111 | UI fixture; 2–4/distinct/context quality in #6 |
| FR-021 | Alternative select/cache | US-012 | AC-053–054, 057, 097–099, 111 | Request-count and text assertions |
| FR-022 | Mode/source invalidation/full replacement | US-010, US-012, US-013 | AC-044, 046, 057, 060, 096–099 | Controlled races |
| FR-023 | Result editing/copy/targeted invalidation | US-011, US-013 | AC-051, 058–064, 094, 096, 098, 100 | UI fixture identities; correspondence mechanics in #3/#5 |
| FR-024 | Counter/usage/truncated charge disclosure | US-007, US-014 | AC-030–032, 065–070 | UI + accounting integration |
| FR-025 | Alternatives charge once | US-012, US-014 | AC-052–056, 067, 098, 105, 110 | UI request count + server accounting integration |
| FR-026 | Success-only/stale accounting | US-008, US-013–US-015 | AC-032, 035–036, 047, 055–061, 067, 072–076, 098, 106, 108 | UI outcomes + atomic accounting integration |
| FR-027 | User/global limit/reset | US-014 | AC-068–070, 105, 107 | Visible UI + concurrent server tests in #6 |
| FR-028 | Usage disclosure | US-014 | AC-065–071, 106 | UI authoritative replacement + API fields #5 |
| FR-029 | No administration UI | N/A | AC-065 (absence smoke) | Backend-owned #3; UI must expose no routing settings |
| FR-030 | Shared configured chains | N/A | N/A | Backend-only #3/#4; invisible successful outcome, final errors per Section 9 |
| FR-031 | Rule precedence | N/A | N/A | Backend-only #3/#4/#6; no UI consequence |
| FR-032 | Defaults/fallback order | N/A | N/A | Backend-only #3/#4; fallback invisible |
| FR-033 | Section 9.2, final fallback outcomes | US-015 | AC-067, 072–076 | Final UI outcome; fallback classification/integration #4/#6 |
| FR-034 | Sections 6.2, 9.2, final success/failure | US-015 | AC-036, 056, 072–076, 108 | UI preservation + provider/fallback integration |
| FR-035 | Sentence/revision state tables | US-012, US-013 | AC-052–064, 097–100, 111 | UI with opaque fixture keys; representation/lifecycle #5 |
| FR-036 | Independent API capability | N/A | N/A | API-only #5/#6; SPA must not be part of API consumer test |
| FR-037 | Message catalog/auth/availability | US-002–US-004, US-007, US-014, US-015 | AC-007–020, 027–032, 068–076, 101–108 | UI categories; schemas #5 |
| FR-038 | Sections 3.4, 8.1, defaults/session/English copy | US-001, US-009, US-017 | AC-001–003, 038, 042, 084–087 | UI/storage + teardown integration |
| NFR-001 | Result surfaces only | US-006, US-010–US-012 | UI scenarios establish display only | Language/model evaluation #6; not proven by UI fixtures |
| NFR-002 | Loading/deadline copy | US-006, US-010, US-015 | AC-022, 044, 073, 075 | UI controlled time; real benchmark #6 |
| NFR-003 | Precedence/race tables | US-006, US-008, US-010, US-012–US-015 | AC-023–025, 032–037, 043–047, 052–076 | UI races + integrated concurrency/accounting |
| NFR-004 | Workspace boundary/no storage | US-001, US-017 | AC-003, 015–016, 084–087 | UI/storage observation; architecture/privacy proof #3/#6 |
| NFR-005 | Accessibility/responsive sections | US-016 | AC-077–083, 092–095 | Automated + required manual AT matrix |
| NFR-006 | Availability UI | US-014 | AC-067–071 | UI fixture; configured cap/provider exposure #3/#6 |
| NFR-007 | Visual tokens/layout | US-016 | AC-078, 081, 083 | Visual regression/contrast |
| NFR-008 / P-005 | No user-facing rate-control design added | N/A | N/A | Proposed, not binding; security/architecture owner |
| RG-001 | Core UI behaviors | All feature stories | AC-021–064 | Aggregate executable UI + integration evidence later |
| RG-002 | Result surfaces | US-006, US-010–US-012 | N/A | Quality: document #6 evaluation only |
| RG-003 | Loading/deadline states | US-015 | AC-073, 075, 108 | UI state only; performance benchmark #6 |
| RG-004 | Usage and race behavior | US-007, US-008, US-012–US-015 | AC-030–036, 052–076, 096–100, 105–108 | UI + integrated accounting suite |
| RG-005 | Auth journeys | US-002–US-004 | AC-006–020, 101–104 | UI auth plus independent API consumer #5/#6 |
| RG-006 | Accessibility/responsive behavior | US-016 | AC-077–083, 092–095 | Browser matrix + manual AT |
| RG-007 | Workspace/availability | US-014, US-017 | AC-070, 084–087 | UI plus architecture/provider evidence |
| RG-008 | Sections 15–16 | N/A | N/A | This document resolves UX questions only; other owners remain |

## 15. Decision record and external dependencies

### 15.1 Resolved UX decisions

| Decision | Resolution and rationale | Upstream status |
| --- | --- | --- |
| UX-D-001 / Q-009 | Browser policy, WCAG 2.2 AA criteria, layouts, focus, live regions, and every requested message/state are defined in Sections 3–12. | UX-owned; resolved by this document |
| UX-D-002 / Q-002 | Correction only temporarily ignores and disables Styles while retaining the prior visible selection; disabling restores it and triggers a rewrite. | Consistent with FR-015/P-002; no product change |
| UX-D-003 / Q-002 | Manual edits invalidate only the edited sentence and boundary-touching sentences. Insert/delete/merge/split outcomes are explicit; uncertain correspondence invalidates only the local ambiguous group. | UX presentation resolved; algorithm/IDs remain #3/#5 |
| UX-D-004 / Q-002 | A manual result edit immediately protects the displayed rewrite from older full/alternative responses; successful outdated work can still update usage and receives explicit disclosure. | Direct application of FR-023/FR-026/P-002 |
| UX-D-005 / Q-004 | One workspace is one verified tab lifetime; SPA route changes preserve it, while refresh/unload/tab close/sign-out/expiry/new-workspace confirmation end it. | UX boundary resolved; teardown mechanics remain #3 |
| UX-D-006 | Translation results are read-only only during a submitted update when a previous result is displayed; copy remains enabled. | UX safeguard that avoids silently overwriting a manual edit not protected by the translation product policy |
| UX-D-007 | Usage is server-authoritative and stale-success charges are explicitly disclosed; no model/fallback/global/budget amount is shown. | Direct application of FR-024–FR-028 |

No product-owner clarification was required: accepted requirements were mutually consistent, and routine interaction/presentation choices were within document #2's delegated ownership. No PRD requirement or proposal disposition was changed.

### 15.2 Dependencies that do not leave a UX choice open

| Dependency | Required contract from owner | UX behavior already fixed here |
| --- | --- | --- |
| Character counting (Q-003, #5) | Canonical total, submitted prefix, omitted/excess, and charged counts | Counters, boundary, messages, blocking/truncation behavior |
| Error taxonomy/reconciliation (Q-003/Q-006, #5) | Stable categories, operation status after interruption, reset/availability timestamps | Message mapping, no blind retry, precedence, preservation |
| Sentence correspondence/IDs (Q-002, #3/#5/feature spec) | Opaque identities/revisions and mechanics that satisfy affected-group outcomes | Which metadata is invalidated/preserved and what users see |
| Session/privacy (Q-004, #3) | Secure auth/session lifecycle, memory teardown, bfcache defense, retention rules | Observable per-tab lifetime, clearing, routing, and no restoration |
| Password policy (#3/#5) | Actual rule checklist and validation response | Form layout, checklist behavior, error/focus behavior; no policy invented |
| Monetary cap (Q-001, #3) | Configured amount and availability status; amount need not be client-visible | Exact unavailability message and recovery action |
| Provider/model behavior (Q-001/Q-007, #4) | Eligible routes, output validity, deadlines, fallback outcomes | Loading/final-result/final-error presentation only |
| Evaluation/browser evidence (#6) | Executable check IDs, pinned browser versions, results, AT reports | Required lanes, scenarios, and evidence boundaries |

An implementation package is not ready if it cannot map its concrete API fields/error categories or sentence-revision behavior to these observable contracts. That is an external contract dependency, not permission to change the UX ad hoc.

## 16. Specification validation and readiness

### 16.1 Original authoring checks (v1.0)

- Re-read document #0 and current PRD v0.2; preserved FR/NFR/P/Q/RG IDs and P-001–P-004 dispositions.
- Covered every user-facing confirmed requirement with a story and deterministic UI scenario path; backend-only/proposed items are explicitly marked.
- Defined competing-event precedence, debounce versus processing, stale successes/failures, navigation/session teardown, manual result edits, alternative races, and source protection.
- Defined exact English messages, placement/recovery rules, numeric layout/tokens/breakpoints, desktop/mobile wireframes, browser policy, and manual accessibility checks.
- Distinguished mocked UI checks, integration/accounting, model evaluation/performance, and manual accessibility evidence.
- Recorded directly observed DeepL evidence separately from official documentation, design adaptation, and inaccessible states.
- Validated 17 unique story IDs, 112 unique acceptance IDs, 44 message IDs, story/scenario references, complete FR/NFR/RG matrix membership, Markdown table widths/fences, and existing local links. Rechecked official DeepL help links and unchanged upstream SHA-256 hashes. `git diff --check` is clean. These are document checks, not executed application acceptance tests.

### 16.2 Architecture/testing review (2026-09-07)

Review inputs: architecture/UX/ADR baseline at `a07c0e2`, unchanged PRD v0.2, and document #0 v1.1 amended in the same review. The hashes in Section 1.1 identify original authoring inputs; they are not hashes of the amended workflow.

Version 1.1 consolidates the earlier verification amendments: acceptance contracts are layer-independent, accounting UI assertions remain covered, browser-only evidence stays in real browsers, and screenshots are bounded to distinct compositions. Sections 10.5, 12.1, 12.10, 13, and 14 govern allocation; the affected scenario rows are aligned. Product behavior, manual accessibility obligations, and all 112 acceptance IDs are retained. This is a specification review, not executed runtime evidence.

### 16.3 Readiness statement

This specification is **ready for implementation planning** as document #2. It is not an implementation plan, task list, test implementation, or claim of passing runtime checks. Documents #3–#6 must resolve the external contracts in Section 15.2 before the corresponding implementation package can satisfy document #0's ready-for-implementation gate.
