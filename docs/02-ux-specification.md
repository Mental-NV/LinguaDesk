# LinguaDesk — UX/UI Specification

**Document:** #2 · **Version:** 3.0 · **Status:** Screenshot-aligned design; implementation and fresh verification pending
**Updated:** October 25, 2026 (Europe/Moscow)

## 1. Authority, inputs, and scope

[Document #0](00-SDD-Planning-Workflow.md) owns process; [PRD](01-PRD.md) owns active and deferred product scope and D-17–D-23. Current shared API behavior is in [#5](05-api-design.md). Runtime evidence is tracked separately in [delivery status](delivery/current.md).

This document owns current routes, layout, controls, messages, state transitions and acceptance contracts. `Must` is binding for the **active MVP**; rows marked **Deferred** or **Retired** impose no current implementation/test gate. The original 17 UX-US, 112 UX-AC, and 44 UX-MSG IDs remain traceable; new contracts append IDs; some are amended and some inactive. In compact references, AC/US/MSG mean UX-AC/UX-US/UX-MSG. IDs are never reused for unrelated behavior.

D-23 adopts the supplied desktop screenshots and the user's written clarifications. The active shell has independently collapsible left navigation and right toolbar, a shared language bar and two equal editor cards. Write adds Correction Only, Styles/Tones, Show changes and sentence alternatives. Copy buttons and swap/reuse actions are active. One-second pause processing, supported languages, plain-text fidelity, manual editing, privacy and accounting remain binding. No Translate/Rewrite button, New workspace control, usage summary, persistent character counter or dedicated processing/status line is added.

Document upload is explicitly excluded. Do not reproduce the screenshot's upload helper, DeepL branding, Pro badges, Formality, Glossaries, Style profiles, Style rules, Terms or Customizations section. The screenshot text is reference content, not instructions to execute or an implicit requirement to import every depicted feature. Only the remaining capabilities in PRD Section 3.1 stay deferred.

The minimum usable workspace outcome is two end-user journeys: a verified user can translate text in `/translate`, and a verified user can correct or restyle text in `/rewrite`. Account pages, protected routes, capability selectors, API operations and standalone AI behavior are prerequisites or independent-client outcomes; none is evidence that either web journey exists. Each journey is complete only when the published SPA exposes the controls in Sections 5, 7 and 8 and returns an editable/copyable result through the integrated application path.

Automatic source detection and pause-based submission are active. A workspace is one verified tab's in-memory source, settings, result and operation state. A current response must match its workspace generation, feature, submitted source/settings revision and result-edit revision. Source/settings edits, manual result edits and session teardown can make a response outdated.

API counting/identity/recovery and auth/error semantics are owned by [#5's behavioral design](05-api-design.md); the generated wire contract follows during selected implementation. Model eligibility/output checks stay with #4; full verification/evaluation with #6. Only current-scope dependencies block selected implementation.

## 2. Screenshot reference and interpretation

### 2.1 Inspection record

The six user-supplied PNGs were reviewed on October 25, 2026. Filenames identify the attachments; their absolute Desktop paths are not runtime dependencies. The first five are approximately 3,014–3,018 physical pixels wide; screenshot #6 is a toolbar crop. Browser zoom/device scale are unknown, so Section 4 supplies implementable CSS dimensions rather than treating image pixels as CSS pixels.

| Screenshot | Attachment | Page/state | Navigation | Right toolbar | Observed design |
| --- | --- | --- | --- | --- | --- |
| #1 | `LinguDesk001.png` | Translator, empty | Collapsed | Collapsed | Narrow icon rail, centered detection/swap/target group, Toolbar button, equal blank cards |
| #2 | `LinguDesk002.png` | Translator, empty | Expanded | Expanded | Labeled navigation, account row at bottom, toolbar aligned from language bar to editor bottom; center workspace shrinks |
| #3 | `LinguDesk003.png` | Translator, result | Collapsed | Collapsed | English (detected) → Russian, clear at source top-right, copy at both card bottom-right corners |
| #4 | `LinguDesk004.png` | Write, empty/focused | Collapsed | Collapsed | One writing-language selector and reuse icon; prominent placeholder; blue source focus border |
| #5 | `LinguDesk005.png` | Write, result | Collapsed | Expanded | Business selected, main result with green underlined changes, two Alternatives rows separated by rules, result copy |
| #6 | `LinguaDesk006.png` | Styles/Tones menu detail | Not established | Expanded | None set; WRITING STYLE heading; Simple, Business (checked/pale-blue), Academic, Casual; TONE heading; scrollable white popover |

Screenshot #6 cuts off tone values. Retain the existing Enthusiastic, Friendly, Confident, Diplomatic catalog; do not infer extra options. English (British), Russian target, Business selection and the sample account identity are example state, not new language variants, defaults or fixture credentials. Use LinguaDesk branding and the authenticated user's own account data.

### 2.2 Confirmed scope and conservative assumptions

Confirmed by the user: desktop-only references; independent left/right collapse; navigation contains Translator, Write and an account button for Log out; only Correction Only, Styles/Tones and Show changes are adopted as toolbar settings; document upload is excluded; sentence alternatives are included.

The following are explicit design assumptions, not facts established by a still image:

- Preserve existing new-workspace detection and explicit target selection. Correction Only starts on, Styles/Tones at None set, Show changes off; both panels start collapsed. These preserve minimal-correction behavior and emphasize the editor.
- Translator exposes the same toolbar shell but its three writing tools are disabled with `Writing tools are available on Write.` Translation remains a faithful separate operation; no combined rewrite is implied. On Write the three controls are usable as specified below.
- In Write, the two-arrow icon means Use result as source; in Translator it means Swap languages and texts. Screenshot #4's empty-state icon is disabled.
- Show changes controls actual diff visibility. Screenshot #5 shows green markup despite an apparently gray switch; the still image does not establish toggle semantics. Off means clean result; on means marked changes. Link/selection affordances for alternatives are distinct from diff marks.
- Sentence alternatives are requested by selecting a sentence, not generated on every keystroke or full rewrite. Two choices are displayed as in #5. Result editing remains available through a compact Edit result action (not visible in the supplied crop/state).
- Mobile keeps the existing 767 px breakpoint, gutters, vertical source-before-result order and native editing. Navigation and tools expand in flow, avoiding new modal sheets or a permanent narrow-screen sidebar.

Historical research remains in [the archive](archive/ux-research.md); it does not override this reference record.

## 3. Information architecture and workspace lifecycle

### 3.1 Routes and account access

| Route | Signed-out / unverified behavior | Verified behavior |
| --- | --- | --- |
| `/` | `/login` or `/verify-email` respectively | `/translate` |
| `/translate`, `/rewrite` | Require local sign-in, then verification; remember only the safe destination path | Requested feature, with heading focus |
| `/login`, `/register` | Local email/password forms; registration success stays on the page with explicit Go to sign in; no Google control | Redirect to last protected route unless sign-out confirmation is pending/failed |
| `/verify-email` | Retained unverified-account/link/resend/status UI; real delivery is dormant under DF-008 | Success offers explicit continuation to the last protected route |
| `/forgot-password`, `/reset-password` | Retained recovery forms; real delivery is dormant under DF-008 | Same retained recovery result behavior |
| Unknown path | Page not found; Go to sign in / Verify email | Page not found; Go to Translator |

**M002 staged shell — selected 2026-09-08:** Before account functionality exists, [package 002](08-backlogs/M002/spec.md#2-selected-story-and-staged-route-behavior) implements only signed-out informational navigation: root/protected feature routes lead to `/login`; `/login` and `/register` clearly state that their functionality is unavailable and expose no credential fields or submission. Unknown client pages offer Go to sign in. This is a bounded pre-account delivery state, not fake authentication, an accessible protected workspace or completed UX account acceptance. M011–M013 replace these informational states with the existing account contracts; the route table above remains the complete intended behavior. Shell links, heading focus, visual tokens and responsive access already apply.

Only a verified local account may process text. Errors must not reveal whether an email belongs to an account. Form fields, password checklist, links, cooldowns and server categories come from the account contract, with UX states in Section 9. There is no Google callback route in the active UX.

### 3.2 Navigation and per-tab state

Translator (`/translate`) and Write (`/rewrite`) are separate SPA routes sharing auth/usage. The existing route names and API operation names stay unchanged. Mode links and browser Back/Forward preserve each page's in-memory text/settings/result/scroll and focus the destination `h1`; navigation cancels an undispatched pause/queued candidate and never submits. A pending operation may finish for its matching hidden feature but never changes the visible feature's focus/text. Returning to a feature alone does not restart its timer. Opening or duplicating a new tab starts an empty workspace; text is never transferred. Navigation expansion is shared within the tab; toolbar expansion and Write settings belong to their page. Collapse, expansion, account-menu opening, Show changes and clipboard actions never start or restart processing. Responsive reflow preserves state and never submits.

### 3.3 Refresh and restoration

Refresh, full-document navigation and tab close end the workspace. After authentication bootstrap the requested page opens empty with current PRD defaults. A real bfcache restoration must not reveal old text: teardown on `pagehide` and defensively reset on `pageshow` as described in #3. Synthetic events only prove handlers, not actual browser restoration. Backgrounding, offline intervals and switching SPA modes do not end a workspace.

### 3.4 Workspace and session boundary

A verified tab's workspace ends at reload/full-document navigation/tab close, sign-out, session expiry or account invalidation. Source/result/settings live only in memory, never URLs/history payloads, local/session storage, IndexedDB, service-worker/HTTP caches, autofill-restored workspace values or analytics. Disable workspace editor autocomplete/restoration where controllable; account password-manager support remains allowed. Invalidate pending callbacks and pause timers with the workspace generation before rendering cleared/login state. Backend accounting may still settle an already submitted operation without restoring its text.

Footer: `Text and settings are cleared when this workspace ends, including refresh, sign-out, or session expiry.` No text is saved as history. This footer describes the browser workspace lifetime, not a provider retention or no-training promise. D-18 removes provider eligibility requirements of that kind; the clarified provider-managed caching choice does not change workspace teardown or introduce application response storage.

The bottom navigation account button opens a small menu containing **Log out**. Collapsed navigation shows an avatar/initials with accessible name Account; expanded navigation adds available account name/email. Do not invent profile fields or copy the reference account. Log out invokes the existing sign-out flow; opening the account menu does not log out. There is no manual workspace reset action or confirmation dialog. Refresh or full navigation starts a new empty workspace with current defaults and does not replay an old pause timer.

Sign out clears immediately and displays `/login` with `Signing out…`. Failure shows `Workspace cleared. Sign-out could not be confirmed. Try again.` and `Try sign-out again`; suppress authenticated redirect until confirmed. Retry sends only the sign-out action. Success exposes the ordinary login form. Session expiry clears text before login renders and shows MSG-037. No late response may restore it.

## 4. Visual system and responsive layout

### 4.1 Design tokens

The visual direction is a white application shell around a very light neutral canvas, large rounded white cards, thin gray borders, dark neutral text, pale-blue active navigation and green change marks. Preserve system-font fallbacks for Cyrillic, Romanian and Chinese. Values below are normalized design targets from the screenshots, not sampled CSS source.

| Token | Value | Use |
| --- | --- | --- |
| `font-sans` | `Inter, ui-sans-serif, -apple-system, BlinkMacSystemFont, "Segoe UI", "Noto Sans", "Noto Sans SC", Arial, sans-serif` | UI and editors; no required font download |
| Type sizes/line heights | 12/16, 14/20, 16/24, 20/28, 28/40 px | Caption, secondary, controls, mobile editor, desktop editor |
| Type weights | 400 body, 500–600 controls, 600 placeholder | Large empty-state prompt; regular entered text |
| `color-bg` / `color-surface` | `#F5F5F5` / `#FFFFFF` | Canvas / rail, bar, cards and popovers |
| `color-text` / `color-muted` | `#292929` / `#666666` | Primary / helper and placeholder text |
| `color-brand` | `#183B56` | LinguaDesk mark |
| `color-action` / `color-selected-bg` | `#005CB9` / `#E1EFFF` | Active icon/label and selected menu/navigation row |
| `color-focus` | `#005FCC` | Focused source border and keyboard focus |
| `color-border` | `#E2E2E2` | Decorative panel outlines and dividers |
| `color-control-border` | `#808080` | Where an outline is required to identify a control |
| `color-change` | `#00805E` | Changed result text plus underline; also selected sentence affordance with distinct accessible labeling |
| Warning bg/text | `#FFF7ED` / `#8A4B08` | Essential warnings |
| Error bg/text | `#FFF1F0` / `#B42318` | Essential errors |
| Spacing | 4, 8, 12, 16, 24, 32, 48, 64 px | 12 px editor gap, 16 px workspace-to-toolbar gap |
| Radius | 8 px controls/menu rows, 16 px cards/language bar, 12 px popover, 999 px switch/avatar | Rounded but restrained |
| Control targets | 44 px default, 48 px mobile/forms; 20–24 px icons inside targets | A small glyph does not mean a small hit area |
| Borders/focus | 1 px neutral; focused editor 2 px blue; keyboard ring 2 px with 2 px offset | Avoid layout shift by reserving border space |
| Shadow | `0 8px 24px rgba(0,0,0,.10)` | Open popovers only; cards remain flat |
| Motion | 120 ms color, 160 ms disclosure; removed for reduced motion | No required animation |

Switches have a gray track/white thumb when off and a blue track/white thumb when on, plus programmatic checked state. Disabled controls use muted gray and native/ARIA disabled state. Selected style/tone rows show a check as well as pale-blue fill. Essential labels, text and boundaries meet Section 10 contrast; light card borders are decorative. Do not weaken contrast to imitate screenshot anti-aliasing.

Authentication retains its existing 400 px maximum content width, 24 px field gaps, 48 px inputs/buttons and 16 px mobile gutters. Desktop navigation uses a compact LinguaDesk mark with accessible name, and the expanded rail may show its wordmark; do not import the DeepL logo.

### 4.2 Layout

Dimensions are **CSS pixels**. Use border-box sizing, `min-width: 0` on grid children and wrapping text. The desktop shell uses available width rather than the former centered 1280 px cap. The white top band is 64 px high; canvas content begins 40 px below it with 16 px side gutters. Both cards fill the same row height: at least 420 px wide desktop / 360 px compact desktop, growing toward the viewport bottom with 24 px lower space. Short viewports use ordinary page scrolling. Text bodies scroll within cards; action footers reserve space so icons never cover text.

| Viewport | Navigation | Workspace / tools | Editors |
| --- | --- | --- | --- |
| ≥1200 px | 64 px collapsed; 256 px expanded | Available width after nav and 16 px gutters; toolbar 304 px when open with 16 px gap | Equal columns, 12 px gap; language bar spans both |
| 768–1199 px | 64 px rail; expansion displays a 256 px overlay over the shell with outside-click/Escape dismissal | Available width after rail and gutters; tools open in flow below language bar, full width | Equal columns, 12 px gap; no simultaneous narrow side-by-side tools rail |
| 360–767 px | Existing top header/navigation becomes compact with a Navigation disclosure; no fixed left rail | 16 px gutters; Toolbar disclosure opens tools below language controls | One column, source then result, 16 px gaps |
| 320–359 px | Same compact header/disclosures | 12 px gutters | One column, full usable width |

At 1440 px, center width is 1344 px with both panels collapsed (666 px per editor); expanded navigation alone gives 1152 px (570 px per editor); open toolbar alone gives 1024 px (506 px per editor); both expanded gives 832 px (410 px per editor). At 768 px, the rail plus gutters leaves 672 px (330 px per editor); expanded tools are in flow. Mobile retains **358 px at 390 px** and **296 px at 320 px**. Borders/padding are included; do not subtract gutters twice. At zoom/reflow use the effective CSS viewport.

Each desktop source/result card has 24 px inner padding and a 44 px action area at the bottom; source clear sits top-right and text reserves its space. Empty prompts and text share the same origin. Empty result cards are visually blank with an accessible label. Headings/editor labels remain available to assistive technology but are visually hidden in the workspace, matching screenshots without stacked title rows. The single page `h1` still receives route-navigation focus.

#### 4.2.1 Translator — desktop

```text
┌ navigation ┐  white top band
│ brand     │  ┌────────── Detect language  ⇄  Target language ─── Toolbar ┐
│ Translator│  └─────────────────────────────────────────────────────────┘
│ Write     │  ┌──────────────────────────┐ ┌────────────────────────────┐
│           │  │ Type to translate.    × │ │                            │
│           │  │                        │ │                            │
│ Account   │  │                   Copy │ │                       Copy │
│ collapse  │  └──────────────────────────┘ └────────────────────────────┘
└───────────┘
```

Clear/copy icons appear only when their respective text exists. The source/swap/target group is centered in the full language bar; Toolbar is at the far right when closed. With tools open, remove that bar button and place the collapse control at the top-left of the separate right card. The right card aligns with the language bar top and editors bottom, as #2. Center width shrinks; cards retain equal dimensions. Detection updates the source label to `{language} (detected)` only for the matching source revision. Source placeholder is exactly `Type to translate.`; no file-upload helper/drop target.

#### 4.2.2 Translator — mobile

Retain the current stacked language selectors above the source, then result. Each selector fills the available width; swap and Toolbar occupy their own wrapping action row. Navigation expands in the header; the three tool rows expand in flow before the source. Editors use 20/28 px text, 16 px padding, minimum 240 px body height and grow to 360 px before internal scrolling. Essential errors, dropdowns and controls wrap; the software keyboard must not obscure the caret. No modal sheet or permanent rail consumes editor width.

#### 4.2.3 Write — desktop

```text
┌ nav ┐  ┌────────── Writing language  ⇄ ───────────┐  ┌ Toolbar ──────────┐
│     │  └────────────────────────────────────────┘  │ collapse          │
│     │  ┌──────────────────┐ ┌───────────────────┐  │ Editing tools     │
│     │  │ Source         × │ │ Improved result   │  │ Correction Only ○ │
│     │  │                  │ │ ───────────────── │  │ Styles/Tones    ▼ │
│     │  │                  │ │ Alternatives:     │  │ Show changes    ○ │
│     │  │                  │ │ option 1          │  │                   │
│     │  │                  │ │ ───────────────── │  │                   │
│     │  │                  │ │ option 2     Copy │  │                   │
└─────┘  └──────────────────┘ └───────────────────┘  └───────────────────┘
```

The single writing-language selector and Use result as source icon are centered in the bar; there is no target-language selector. The collapsed-toolbar layout matches #4. Source empty copy is `Type or paste text to see ideas for improvement.` with helper `Click a sentence in the result to see alternatives.` The helper describes the supported sentence interaction without promising word-level suggestions. Alternatives live **inside the result card**, below its main text and a thin divider; they are not a third editor or toolbar item. Main result uses 28/40 px; Alternatives label 14/20 px and option rows 16/24 px. Main and alternative changed wording can be green/underlined when Show changes is on; unchanged text stays dark.

#### 4.2.4 Write — mobile

Use the existing stacked source/result layout and language selector width. Put Use result as source and Toolbar in a wrapping action row; expanded tools appear once, in flow before source. Alternatives wrap below the main result in the same card. Keep result copy reachable in its reserved footer. The Styles/Tones menu fits the viewport and scrolls vertically; mobile never forces desktop columns or duplicates tool controls.

### 4.3 Navigation, toolbar and menu interactions

The full-height desktop left rail is white with a right divider. Translator and Write sit below the brand with icon plus label when expanded, icon only when collapsed. Current route uses a pale-blue rounded row and blue icon/text with `aria-current="page"`. Account is anchored near the bottom above a divider; the navigation collapse/expand control is last. Expansion changes layout, never route or content. Mobile preserves the same destinations in the header disclosure. Accessible labels/tooltips identify icon-only controls.

The right toolbar is a white rounded card with collapse control, **Editing tools**, then exactly three setting rows in this order: **Correction Only**, **Styles/Tones**, **Show changes**. No Customizations heading or placeholder premium controls. Collapsing it removes the entire card and restores the Toolbar button in the language bar. Expanded/collapsed state is independent of navigation and does not change settings. Move focus to the visible counterpart when collapsing; Escape closes a menu before its containing panel.

Styles/Tones is an anchored single-select listbox popover, same width as its row, with selected value on the right and a chevron. Order: **None set**; group **WRITING STYLE** with Simple, Business, Academic, Casual; group **TONE** with Enthusiastic, Friendly, Confident, Diplomatic. Headings are nonselectable. A checkmark and pale-blue background identify the selected option. Limit menu height to `min(360px, available viewport height)` and scroll; flip above the row if needed. Arrow keys move the active option; Home/End and typeahead navigate; Enter/Space commit; Escape/outside click closes without changing the committed value; return focus to the trigger. Expose expanded/selected/group semantics. Native language selects remain acceptable; only Styles/Tones needs the pictured grouped popover.

Account's menu contains only Log out. Escape/outside click dismisses it and returns focus appropriately. No account settings destination is introduced.

## 5. Components and processing behavior

| Component | Current contract |
| --- | --- |
| Navigation | Translator and Write links with current-page state, Account → Log out, independent collapse control |
| Language | Native selects named Source language / Writing language / Target language; source visible default Detect language, target Choose target language; four existing languages |
| Swap / reuse | Translator: Swap languages and texts. Write: Use result as source. Conditions in Sections 7–8 |
| Toolbar | Correction Only, Styles/Tones, Show changes only; Section 4.3 defines layout and behavior |
| Source | Native textarea named Source text; Clear source text icon at top-right; editable during requests |
| Processing | One second after eligible source/effective settings change; no primary submit button; one logical operation per settled revision |
| Result | Named Translation result / Improved result; complete editable text with clean native/copy-button clipboard; Write additionally supports local review and selectable sentences |
| Copy | Translator: Copy source text and Copy result. Write: Copy result. Only shown for nonempty corresponding text; no provider request |
| Account forms | Existing visible labels, native submission, password reveal and errors remain |

Exactly one visible `main` and one accessible `h1`; visually hidden workspace heading/labels preserve screenshot density. Account-page headings stay visible. Platform text editing, undo/redo, clipboard and IME remain available. Enter in an editor inserts a newline and never requests alternatives. Composition suppresses dispatch; `compositionend` starts a fresh timer only for a committed eligible source/settings revision. Custom menu keys apply only while focus is in that menu.

Copy writes only the current corresponding plain text, including manual edits and line breaks. Successful copy briefly announces MSG-048; failure uses MSG-049 and leaves native selection usable. Do not move editor focus/selection, copy annotations or claim success before the clipboard write completes. Clear source returns focus to the source, preserves the prior result and cancels pending auto-dispatch; late responses cannot apply.

One pending paid operation per page is retained, including alternatives on Write. A new eligible revision waits for its own timer and, if another operation is unsettled, for that operation to resolve. Manual result editing or leaving the feature cancels an undispatched timer/queued candidate, but cannot cancel a provider operation already sent. An unchanged settled revision never runs again. Known invalid input cancels the timer and keeps the source editable; essential blocking/failure feedback uses a page alert rather than dedicated source/result status lines. Route changes and account continuation may move focus; async completion/usage/background validation never steals focus or moves the source caret.

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

- Initial source is Detect language; target is Choose target language. Chinese is labelled `Chinese (Simplified output)` and accepts both Chinese scripts. Source detection occurs for a current paused candidate; a manual choice does not waive supported-content validation.
- Target lists the four PRD languages. Disable the known source language as target; if a later source change equals an already selected target, retain the invalid value and block dispatch until corrected. Selecting a valid target with eligible source restarts the timer.
- Count complete input internally using #5's Unicode contract. At or below the configured limit, the paused candidate submits all entered text. Above it, retain text/caret and previous result, show an essential blocking alert, and cancel dispatch. The backend rejects oversized independent-client requests too. There is no persistent counter, boundary marker, suffix omission, or partial-result state.
- Native selection and Copy result use the exact current result including manual edits/newlines. Empty source does not erase an existing result; Clear source text preserves that result and cancels any pending timer without dispatch.

Swap is disabled until source language is known and a current, unedited-by-new-source/settings result exists. Activating Swap languages and texts atomically puts the current result (including manual edits) into source, the old source into result, and reverses resolved source/target languages. Detection becomes an explicit source selection. This is a deliberate source/settings edit: validate the new source and start one fresh one-second pause if eligible. Disable while a paid operation is unsettled; above-limit swapped text stays editable and blocked. Never infer a language from a stale detection label. Toolbar writing settings are disabled on Translator and never enter translation requests.

## 8. Rewriting contract

### 8.1 Correction and style/tone mapping

Correction Only defaults on; Styles/Tones defaults None set and is disabled while the switch is on. Turning Correction Only off enables None set. Turning it on resets the selected style/tone to None set; turning it back off does not restore an old choice. Off + None set and on both map to existing API `correctionOnly`; a chosen style/tone maps to its one existing mode. Never send two modes or UI switch state to the transformation API. A switch change that leaves the effective mode unchanged creates no new processing revision/request. Selecting a different effective mode restarts the one-second pause for eligible source. Grammar/spelling correction applies to every mode.

Language/source edits and effective mode changes use the existing whole-input 2,000-character limit and processing guards. The source remains untouched by successful results. The language catalog has English, Russian, Romanian and Chinese (Simplified output); English (British) in the screenshot does not add a dialect setting.

Use result as source is disabled for empty/stale results or unsettled paid work. When enabled, it copies the entire current plain result into source, retains that result and language/mode, invalidates assistance and starts a pause only if the source changed and is eligible. It does not swap languages. Result text exceeding the source limit remains editable and blocked rather than truncated.

### 8.2 Result editing and Show changes

Write initially displays its complete result as selectable sentence text. Provide a compact **Edit result** action within the result card to enter an ordinary native textarea and **Done editing** to return to presentation; these are result actions, not extra toolbar settings. Focus enters the textarea without modifying text. A manual edit or composition start cancels undispatched work, protects text against pending responses and invalidates **all** sentence, alternative-cache and comparison metadata. Done editing creates no request; the edited result remains plain and copyable until a later successful full rewrite rebuilds assistance. Undo does not resurrect invalidated metadata. Merely entering/leaving edit mode without changes preserves metadata. This conservative editing mechanism keeps native caret/IME behavior without requiring rich-text caret mapping.

Show changes defaults off. On uses the exact source snapshot submitted for the displayed current successful rewrite, not a later source edit. Inserted/replaced result spans use green plus underlines. Deleted source text is available as labeled, struck-through deletion annotations in the result review; a complete deleted sentence remains reviewable. Annotation markup never becomes result text or clipboard content. Diff computation is local and must preserve Unicode clusters, paragraph breaks and plain result fidelity; it requires no model-generated markup or server sentence IDs.

Turning Show changes off hides all diff/deletion annotations and leaves the same plain result and alternative choices. Toggling never dispatches or alters accounting. Manual edits, source/language/effective mode changes and new full results invalidate old comparison/alternative metadata as a whole. Retain the user's Show changes preference in page memory, but show plain text and disable review/alternatives when no valid comparison metadata exists. A current full rewrite restores them. Selecting a cached alternative recomputes comparison against that same submitted source snapshot locally.

### 8.3 Sentence alternatives

Click/tap a sentence in the current result to select it and request two alternatives; keyboard focus plus Enter/Space has the same effect. Preserve ordinary drag selection for native copy; dragging must not request alternatives. Show the selected sentence with an identifiable outline/underline and accessible selection state. No word-level synonym contract is introduced. Do not request alternatives for empty, stale or manually edited results, or while the page has unsettled paid work or a queued full rewrite.

Show **Alternatives:** beneath the main result, with a loading state followed by two sentence rows separated by fine rules, as in #5. Opening a current cached set reuses it without a request. Each option preserves the sentence's meaning, language and effective mode in its bounded surrounding context. An explicit option click replaces only the selected sentence locally; it creates no full rewrite, timer or character charge. Keep the original and both generated choices associated with that sentence so the user can switch back/reuse them; after replacement, show the other available choices in the two rows. No hidden background generation or automatic retry.

Tie each request/set to workspace generation, full-result version, selected sentence span/text, language/mode and context revision. Source/settings changes, a new full result, manual editing or teardown invalidate all pending associations and cached sets. A late success can settle accounting but cannot replace text or reopen stale assistance. A controlled alternative replacement may retain the selected set, updating its association to the new local result revision; clear other sentence caches because their context changed. This is not targeted preservation through arbitrary edits.

Failure leaves source/result intact and offers an explicit sentence-level retry only after a known failure. Unknown outcomes use Check status, never paid regeneration. Charge the selected sentence's canonical scalar length once per successful generated set; context/options, cache reuse and selection add no character charge. All provider work still requires cost admission. Section 5's one-pending-operation guard applies; another sentence may be selected for focus, but cannot dispatch until the prior operation settles and the user explicitly activates it again. If selection moved meanwhile, the completed old set may be cached only while its result/context is valid; it must not replace the newly selected sentence or reopen the old options.

M045 selects deterministic sentence segmentation in Section 8.4 and bounded context/association in API §2.2 for all four languages. Its early implementation gate verifies the shared fixtures and generated wire shape before dependent handlers/clients. No sentence identity is persisted across workspace teardown; targeted merge/split preservation remains retired.

### 8.4 M045 sentence and comparison boundaries

The selected `sentence-boundaries-v1` implementation is deterministic across frontend and backend through shared fixtures; it does not depend on a browser/OS sentence segmenter. Split at paragraph line breaks (CRLF is one boundary, preserved as two scalars), at runs of `!`, `?`, `。`, `！`, `？`, and at a period followed by whitespace/end. Include adjacent closing quotes/brackets with the sentence. Chinese/full-width terminal punctuation does not require following whitespace. A trailing punctuation-free nonempty fragment is a sentence. Inter-sentence whitespace is retained as untouched gaps; spans exclude leading/trailing whitespace and never split surrogate pairs or CRLF. The longest valid selected span is 2,000 scalars; a longer unbroken sentence remains editable/copyable but has no alternatives action.

Do not split decimal periods between ASCII digits, dotted initialisms, or the fixed case-insensitive abbreviation tokens `Mr.`, `Mrs.`, `Ms.`, `Dr.`, `Prof.`, `e.g.`, `i.e.`, `etc.`, `dl.`, `dna.`, `dr.`, `г.`, `ул.`, `т.д.`, `т.п.`. Suppress a boundary after a single-letter initial when another word follows on the same line. Match whole abbreviation tokens, not suffixes. This conservative rule set favors a larger sentence over splitting known abbreviations; freeze ambiguous examples and version any later algorithm change. The backend verifies the selected span with the same rule/fixture version; plain client offsets do not waive validation.

Comparison uses grapheme-safe tokens and retains raw source/result strings unchanged. Use deterministic matching with a maximum 200,000 token-pair comparisons; on exhaustion show the unmatched region as deletion plus insertion instead of blocking editing or approximating the copied text. Preserve paragraph gaps and whole deletions. Bounds are an implementation safeguard, not a language-quality claim. All highlights remain disposable local presentation, with no targeted identity reconstruction after manual edits.

## 9. Local-account and availability flows

Registration contains Email, Password, Confirm password, actual policy checklist, Show password, Create account, and Sign in. Login contains Email, Password, Show password, Sign in, Create account and Forgot password. Password reveal preserves selection. Invalid local submission sends no request, shows `Check the highlighted fields.` with linked errors, focuses the first invalid field, and exposes `aria-invalid`/descriptions. A current valid submission disables fields and guards duplicate click/Enter. Failed credential/policy/network responses clear password values and permit explicit recovery; keep email only in this in-memory auth journey.

Registration success replaces the form with MSG-034 and an explicit Go to sign in link. It retains no email or password in the rendered success state and creates no pending-verification navigation guard. Registration neither signs in nor starts language processing. The created account is already verified by the temporary DF-008 substitute.

Existing unverified-account verification and password-recovery pages retain their previously implemented safe behavior for dormant compatibility, but real delivery and their MVP evidence obligation are deferred under DF-008. Generic account network failure remains `We couldn’t complete this request. Try again.` No Google copy/helper or linking screen exists.

Old auth completions cannot navigate over a newer auth route or restore cleared passwords/text. Reconcile actual account status on the next protected navigation. Keep safe return paths only, not arbitrary external redirects.

A failed usage refresh does not turn a successful transformation into failure: keep its result and retry a usage read on the next relevant eligibility check. `Check availability`, when offered after a suspension alert, performs one status read and never starts a paid operation.

Usage remains server-authoritative but has no persistent UI summary. Exhaustion uses MSG-015/016/017 in an essential page alert and blocks processing; text/edit/native copy remain usable. An operation requiring more than remaining allowance shows `This request needs {required} characters, but you have {remaining} remaining today.` A new eligible source/settings revision starts a timer only after capacity is confirmed; restored capacity or reconnect alone starts no paid operation. Unknown usage/status triggers a read, never a speculative charge or paid retry.

## 10. Accessibility and browser coverage

Target [WCAG 2.2 AA](https://www.w3.org/TR/WCAG22/) with native semantics. Keep 44×44 px standalone touch targets where possible (24×24 minimum except valid inline exceptions), 4.5:1 ordinary text / 3:1 large text, and 3:1 focus/control boundaries. Use a 2 px focus ring with 2 px offset. Errors/status use text as well as color. Close returns to its trigger. Language selectors and text editing keep native behavior. The Styles/Tones listbox, navigation/account disclosures and sentence options require accessible names, selected/expanded states, keyboard operation and focus return; no mobile modal sheet is required.

At 200% zoom all controls remain available; at effective 400% / 320 CSS px there is no horizontal page overflow. Text-spacing overrides (1.5 line height, 2× paragraph spacing, .12em letter and .16em word spacing) cause no clipping. Reduced motion removes transitions/shimmer. Native Chinese composition, selection and undo/redo remain intact; no interim composition text starts processing. Do not move focus on completion or scroll a background result into view.

### 10.1 Supported browsers

Under PRD D-22, release assurance covers pinned Playwright Chromium at desktop and narrow responsive viewports. [Verification plan Section 4](verification/frontend.md#4-frontend-browser-and-manual-verification) owns executable checks and version recording. Firefox, WebKit, branded/current/previous versions and physical devices are best-effort compatibility targets; their checks are optional and nonblocking. Viewport emulation establishes responsive layout in the tested engine, not physical-device or additional-browser verification.

### 10.2 Verification ownership

The accessibility outcomes above remain implementation targets. Required release evidence consists of executable semantic/contrast/reflow/focus and native interaction checks in [verification plan Sections 4.2–4.4](verification/frontend.md#42-browser-lanes). Human sign-off, screen-reader/native-IME review and physical-device checks are optional and nonblocking. Automated checks do not establish complete WCAG conformance or untested platform behavior; record their limits. Deferred journeys create no MVP evidence requirement.

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
| UX-US-011 | Amended | Review changes, edit and copy a Write result | FR-019/023 |
| UX-US-012 | Amended | Request, select and reuse contextual sentence alternatives | FR-020/021/025 |
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

[Verification plan Sections 2–4](06-verification-plan.md#2-verification-layers-and-check-catalog) define the lowest sufficient evidence for each assertion and distinguish fixtures, integrated smoke, required live evidence and optional manual observations. AI may perform semantic/visual review under D-22. The [local acceptance allocation](verification/coverage.md#82-local-acceptance-allocation) preserves every UX-AC ID and its current disposition. No scenario here is a claim of a passing test.

[Roadmap M028](07-roadmap.md#45-useful-end-to-end-product-increments) owns the first complete Translation UI outcome and [M029](07-roadmap.md#45-useful-end-to-end-product-increments) owns the first complete Rewriting UI outcome. [Verification plan Section 4.5](06-verification-plan.md#45-required-user-visible-transformation-evidence) defines the minimum evidence that prevents API, LLM or mocked-component work from being reported as either end-user outcome.

Every selected UI-facing feature must have a published end-to-end case executed as #6's dedicated verified synthetic user. At least one suite case performs the visible Email/Password/Sign in flow; subsequent feature cases may begin from the documented authenticated fixture, but they still exercise the real protected route and application API and must assert the resulting controls, essential errors and result text in the UI. Server-side accounting is verified through the API/database boundary. A successful request observed only in network logs is not UX acceptance.

## 14. MVP traceability

The sole cross-document [product and release coverage matrix](verification/coverage.md#81-product-and-release-coverage) now lives in #6. This document retains UX behaviors, stories and scenario IDs; it does not maintain a second product-to-evidence table.

## 15. Decisions, handoffs, and deferred reactivation

UX-D-001–UX-D-007 remain stable historical decision IDs. Their current disposition is explicit below; obsolete text is preserved in Git `54343c3`, not an active alternative contract.

| Decision | Current disposition |
| --- | --- |
| UX-D-001 / Q-009 | Amended: current native-control/pause-processing/browser/accessibility contract in this document |
| UX-D-002 / Q-002 | Historical restore behavior stays retired; FR-039 defines the new reset-to-None-set switch under D-23 |
| UX-D-003 / Q-002 | Retired: targeted sentence correspondence is cut, including future default behavior |
| UX-D-004 / Q-002 | Retained/amended: manual edits protect both plain result fields from earlier responses; usage may still settle |
| UX-D-005 / Q-004 | Retained: one verified tab lifetime and no durable text restoration |
| UX-D-006 | Amended: plain Translation result remains editable during processing, protected by result-edit revision |
| UX-D-007 | Amended: server-authoritative accounting and stale-success disclosure without a persistent usage summary or provider/global/budget amounts |
| UX-D-008 | Accepted 2026-09-08: PRD D-17 replaces affected prior UX; DF register controls later scope, no dormant MVP widgets/tests |
| UX-D-009 | Accepted 2026-09-20: PRD D-21 replaces explicit processing and removes the listed workspace controls/status lines; preserve native copy, essential errors and stale-response protection |
| UX-D-010 | Accepted 2026-10-25: PRD D-22 replaces required manual/browser-matrix evidence with unattended Chromium desktop/narrow checks and AI visual review; other-platform/human checks are optional |
| UX-D-011 | Accepted 2026-10-25: PRD D-23 and Section 2 adopt the screenshot shell, limited toolbar, copy/swap, change review and sentence alternatives; document upload remains excluded. Conservative assumptions are explicit; new implementation/evidence is pending. |

API design #5 now defines canonical scalar counting, operation identity/recovery, ordered usage, cookie/bearer lifecycle, error semantics, cancellation and rollover. Selected slices still fix exact wire operations/fields, auth policy/bootstrap/delivery details and the concrete recovery UI mapping before dependent handlers/clients. OpenAPI is generated and reviewed early in implementation, not hand-authored during planning. #3/#4 own serving capability, cost bounds and the simple family chains' quality/output/deadline checks; provider retention/no-training certification is not a prerequisite. Provider-managed caching changes no current UI, character accounting or application text-lifecycle contract. #6 supplies current-scope evidence mapping. Q-002 targeted matching and Google linking no longer block MVP. Advanced routing remains deferred; alternatives context/association is now an active selected-slice dependency.

Deferred work is selected through PRD Section 3.1 and the existing #8 package workflow; no new full deferred implementation spec is authored now. At reactivation, refine only the selected capability, reassess affected historical UX IDs, and update its owning contracts/tests. D-23 activates only the identified controls and assistance; hidden-style restoration and targeted sentence preservation remain retired.

## 16. Validation and readiness

This revision records the screenshot-derived design, explicitly includes sentence alternatives and excludes document upload. It preserves pause processing, server counting/accounting and response protection. Original IDs retain dispositions; new IDs append contracts for the reintroduced controls. Screenshot observations and conservative design assumptions are separated in Section 2. No screenshot, scenario or prior M044/M039 result proves the new design implemented or verified.

Ready for current-MVP scoped planning. Resolve only the selected slice's Section 15 dependencies before implementation. Release evidence, provider evaluation, actual browser behavior and runtime verification remain pending. Deferred/retired rows neither block MVP nor count as passed release checks.
