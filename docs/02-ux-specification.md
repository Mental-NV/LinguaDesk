# LinguaDesk — UX/UI Specification

**Document:** #2 · **Version:** 1.5 · **Status:** Simplified MVP ready for scoped planning; verification methods consolidated in #6; runtime evidence pending
**Updated:** September 8, 2026 (UTC)

## 1. Authority, inputs, and scope

[Document #0](00-SDD-Planning-Workflow.md) owns process; [PRD](01-PRD.md) owns product scope and the seven DF-* deferred groups. The original simplification inputs were Git revision `54343c3` and the user's ten approvals on 2026-09-08, previously incorporated into PRD v0.3. Provider-policy amendment D-18 is based on `b5c01a1` and incorporated with PRD v0.4 / architecture v1.3. The v1.4 handoff amendment follows the approved API-design workflow at baseline `24ffe3b`; current shared API behavior is in [#5](05-api-design.md). Existing UI scenario dispositions remain. No runtime implementation has been inspected or verified.

This document owns current routes, layout, controls, messages, state transitions and acceptance contracts. `Must` is binding for the **active MVP**; rows marked **Deferred** or **Retired** impose no current implementation/test gate. The 17 UX-US, 112 UX-AC, and original 44 UX-MSG IDs remain traceable; some are amended and some inactive. In compact references, AC/US/MSG mean UX-AC/UX-US/UX-MSG. IDs are never reused for unrelated behavior.

No sentence alternatives, sentence identities, comparison/highlighting, Show changes, debounce, Google controls, truncation overlays, custom selectors or tools sheets ship in MVP. No replacement read-only diff is required yet. Use native inputs/selects, inline settings, explicit processing buttons, and plain editable results. Removed targeted metadata preservation and the duplicate correction toggle are not later-phase obligations. PRD Section 3.1 is the sole deferred-feature register; Git `54343c3` retains the old detailed design only as historical input.

Automatic **source detection remains**; automatic **submission does not**. A workspace is one verified tab's in-memory source, settings, result and operation state. A current response must match its workspace generation, feature, submitted source/settings revision and result-edit revision. Source/settings edits, manual result edits, reset and session teardown can make a response outdated.

API counting/identity/recovery and auth/error semantics are owned by [#5's behavioral design](05-api-design.md); the generated wire contract follows during selected implementation. Model eligibility/output checks stay with #4; full verification/evaluation with #6. Only current-scope dependencies block selected implementation.

## 2. Historical reference research (non-normative)

The following September 7 observations remain research provenance. Custom tools, sentence assistance and automatic processing described as earlier design adaptations are deferred by PRD v0.3. This section creates no MVP UI or test requirement; current behavior is in Sections 3–14.

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

### 2.2 Current disposition of historical patterns

| Significant reference pattern | Disposition | LinguaDesk rationale |
| --- | --- | --- |
| Translation/Rewriting mode switch near the workspace | **Adopt** | Makes the two principal tasks prominent while preserving separate routes required by FR-003. |
| Language controls above large paired editors | **Adopt** | High scanability and direct source/result relationship. |
| Source left/result right on wide screens; stacked on narrow screens | **Adopt** | Familiar editor ergonomics, adapted to deterministic breakpoints in Section 4. |
| Editing tools in a right rail or compact sheet | **Defer DF-005** | Current MVP uses the single native Writing mode select inline at every width. |
| Green result-only change emphasis and contextual sentence actions | **Defer DF-001/002** | Current results are plain editable text; targeted metadata preservation is retired. |
| Immediate/automatic processing | **Defer DF-003** | Explicit Translate/Rewrite now; retain complete results, IME safety, previous-result preservation and accounting disclosures. |
| Broad language catalog, dictionary, files, speech, glossary, formality, terms, TTS, feedback, and cross-product actions | **Omit** | Not authorized by the PRD. |
| DeepL language/platform limitations | **Omit** | Current Translation/Rewriting cover all four PRD languages; sentence alternatives are DF-001. |
| DeepL authentication's Apple and SSO choices and marketing consent | **Omit** | Current MVP has local accounts only; Google is DF-007. |
| Centered, narrow authentication forms | **Adapt** | Retain local email/password, verification and recovery with LinguaDesk copy; no provider buttons in MVP. |
| Reference branding, logo, exact colors, and marketing content | **Omit** | LinguaDesk uses its own visual tokens and product scope. |

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

### 6.1 Explicit submission and competing events

| Event/state | Required outcome |
| --- | --- |
| Initial / empty source | No operation; source counter present; prompt to enter text and select any required target |
| Source, language or mode edit | Recompute local validity, increment revision, mark previous result outdated; never call a transformation or start a debounce |
| Known invalid / oversized input | Preserve all text; show field error and excess; no provider operation or character charge |
| Eligible button activation | Capture source/settings/result-edit revision; validate/detect as needed for this submission; show busy state; dispatch once |
| Composition active | No submission or queued auto-submit; after compositionend the user must explicitly activate |
| Already pending on this page | Suppress duplicate submissions until its terminal outcome is known; source/result/settings stay editable. The other feature can submit independently |
| Current valid success | Replace result with complete output; mark Up to date; preserve source/caret; reconcile authoritative usage |
| Source/settings or result edited since submission | Never apply returned text. Successful operation still updates usage and stale-success disclosure; stale errors do not replace newer validation |
| Definitive failure / deadline | Stop busy state; preserve source/result; show classified error; no character charge. Try again is explicit and submits current fields as a new operation |
| Unknown outcome after transport interruption | Stop spinner at overall deadline, preserve work, show Check status. That action reads status only; do not claim zero charge or blindly resubmit. Resolve the earlier outcome before offering retry on this page |
| Reconnect / usage refresh / midnight reset | Refresh availability/usage if needed; reenable action when eligible, but never submit text automatically |
| Reset / sign-out / expiry | Invalidate callbacks and clear text before navigation; subsequent old responses cannot restore it |

For an explicitly activated submission, resolving detection/eligibility can continue that same submission if its captured revisions still match. This is not a second automatic submission. Any intervening edit cancels that continuation; the user activates again. Avoid background remote detection while typing. The API remains authoritative for supported content, counts, allowances and output validity; clients cannot bypass validation with manual source selection.

Starting IME composition in either editor invalidates earlier pending text responses before interim text can be overwritten. Cancelling composition does not revive those responses; it never submits new work. A plain result edited without a pending update shows `Edited`; editing during a pending update shows MSG-022.

Manual result edits are local and uncharged in both features. They do not mutate source. An explicit later submission may replace edits already present at its start, but edits made during its processing remain protected. There is no sentence metadata to preserve or invalidate in MVP.

### 6.2 Response ordering and recovery

Usage snapshots follow #5 Section 7's UTC-day and durable revision ordering. Older snapshots cannot replace newer consumed/reserved/availability state; releases can legitimately increase available capacity, and a new UTC day resets current-day usage. If order is ambiguous, perform one fresh usage read. Text application and accounting are independent: stale success can charge its admission day without replacing text. Display only reconciled authoritative counts; no client-calculated deductions.

`Try again` applies only to definitive failure and captures the current fields. `Check status` uses the original operation identity; it never calls the provider. Unknown-outcome message: `We couldn’t confirm whether this request completed. Your text is safe. Check its status before trying again.` A recorded success with lost output must disclose that the result is unavailable and any confirmed usage; #5 supplies the concrete outcome/category before recovery implementation. A new paid operation must always require explicit action and must not masquerade as replay of the old key.

### 6.3 Exact message IDs and disposition

Current messages below are normative English copy. Deferred/retired IDs retain their former purpose without requiring a hidden UI or translation string now. Numeric examples are supplied from current configured limits/contract fields, not independent hardcoded policies.

| ID | Status | Exact text | Placement / recovery |
| --- | --- | --- | --- |
| UX-MSG-001 | Deferred | — | DF-003 waiting/debounce status; original copy is historical at `54343c3`. |
| UX-MSG-002 | Deferred | — | DF-003 waiting-to-update status; original copy is historical at `54343c3`. |
| UX-MSG-003 | MVP | `Processing…` | First-result status; polite announcement once when request begins. |
| UX-MSG-004 | MVP | `Updating… Previous result shown.` | Current submitted update; polite announcement once. |
| UX-MSG-005 | MVP | `Up to date` | Persistent result status; announce `Translation ready.` or `Rewrite ready.` only when a new result applies. |
| UX-MSG-006 | MVP | `Choose the source language to continue.` | Uncertain-detection error; correct the source choice and explicitly submit again. |
| UX-MSG-007 | MVP | `Choose a target language different from {language}.` | Target selector error for same-language translation. |
| UX-MSG-008 | MVP | `This text contains too much unsupported or mixed-language content. Use one main language: English, Russian, Romanian, or Chinese.` | Server eligibility rejection; no successful transformation or charge; preserve fields and require corrected explicit submission. |
| UX-MSG-009 | Deferred | — | DF-006 prefix warning; original copy is historical at `54343c3`. |
| UX-MSG-010 | Deferred | — | DF-006 partial-result warning; original copy is historical at `54343c3`. |
| UX-MSG-011 | MVP | `Rewriting is limited to 2,000 characters. Remove {excess} characters to continue.` | Rewrite source error; no request; source remains editable. |
| UX-MSG-012 | MVP | `You’re offline. Keep editing. When you’re back online, select Translate or Rewrite to process your text.` | Connectivity banner; reconnect never submits automatically. |
| UX-MSG-013 | MVP | `We couldn’t process this text. Your text and previous result are safe.` | Current operation total failure; action `Try again`; persists until retry, relevant edit, or success. |
| UX-MSG-014 | MVP | `Processing took too long. Your text and previous result are safe.` | Deadline failure; action `Try again`. |
| UX-MSG-015 | MVP | `Your daily allowance is used up. Try again after {reset}. Your text is safe.` | User allowance inline error; reset rendered as absolute `00:00 UTC` plus localized relative time when server supplies it. |
| UX-MSG-016 | MVP | `LinguaDesk’s shared daily allowance is used up. Try again after {reset}. Your text is safe.` | Global allowance error; do not show global consumption. |
| UX-MSG-017 | MVP | `LinguaDesk processing is temporarily unavailable because its service budget has been reached. Your text is safe. Try again after service resumes.` | Monetary-cap error. If server supplies a reliable availability time, append `Expected after {time}.` |
| UX-MSG-018 | MVP | `Sign in to continue. Your text was not processed.` | Authentication failure before a workspace exists. Session expiry uses the message in Section 3.4 and clears text. |
| UX-MSG-019 | MVP | `Verify your email to use Translation and Rewriting.` | Unverified access page. |
| UX-MSG-020 | MVP | `This update is no longer current and was not applied.` | Visual-only secondary result line for a stale success whose usage data has not yet been reconciled; never for a stale failure. Replaced by UX-MSG-021 after reconciliation. |
| UX-MSG-021 | MVP | `An earlier update completed but was not applied. {count} characters counted toward your usage.` | Polite notice and persistent inline usage event until next current success; report its period if different from the displayed day. |
| UX-MSG-022 | MVP | `Edited result — the current update won’t replace your changes.` | Either result was manually edited after submission; preserve it and suppress earlier response text. |
| UX-MSG-023 | Retired | — | Sentence-specific comparison invalidation is cut; future assistance invalidates whole metadata. |
| UX-MSG-024 | Retired | — | Sentence-specific alternatives invalidation is cut; future assistance invalidates whole metadata. |
| UX-MSG-025 | Deferred | — | DF-001 alternatives failure; original copy is historical at `54343c3`. |
| UX-MSG-026 | MVP | `Result copied to clipboard.` | Two-second polite status. |
| UX-MSG-027 | MVP | `Couldn’t copy the result. Select the text and copy it manually.` | Result inline alert; dismissible or replaced by next success. |
| UX-MSG-028 | MVP | `Email or password is incorrect.` | Login form alert; focus email field; no account enumeration. |
| UX-MSG-029 | Deferred | — | DF-007 Google cancellation; original copy is historical at `54343c3`. |
| UX-MSG-030 | Deferred | — | DF-007 Google failure; original copy is historical at `54343c3`. |
| UX-MSG-031 | MVP | `If an account exists for that email, we sent a reset link.` | Password-request success; replace form body; link `Back to sign in`. |
| UX-MSG-032 | MVP | `This verification link is invalid or has expired.` | Verification page alert; action `Send a new verification email`. |
| UX-MSG-033 | MVP | `This reset link is invalid or has expired.` | Reset page alert; action `Request a new reset link`. |
| UX-MSG-034 | MVP | `Check your email to verify your account.` | Registration success page; includes `Resend verification email`. |
| UX-MSG-035 | MVP | `Verification email sent.` | Resend success status; resend disabled for server-provided cooldown, visible countdown. |
| UX-MSG-036 | MVP | `Password updated. Sign in with your new password.` | Reset success; action `Go to sign in`. |
| UX-MSG-037 | MVP | `Your session expired. Sign in again to continue.` | Login page banner after forced teardown. |
| UX-MSG-038 | MVP | `Enter your email address.` | Required email field error. |
| UX-MSG-039 | MVP | `Enter a valid email address.` | Email-shape error; no request. |
| UX-MSG-040 | MVP | `Enter your password.` | Required password field error. |
| UX-MSG-041 | MVP | `Passwords do not match.` | Confirmation field error; focus confirmation. |
| UX-MSG-042 | MVP | `Password must meet all requirements.` | Password-policy failure; requirements themselves come from the authoritative security contract. |
| UX-MSG-043 | MVP | `We couldn’t create an account with these details. Try signing in or use a different email.` | Registration conflict/rejection; form remains populated except password fields. |
| UX-MSG-044 | MVP | `We couldn’t send the email. Try again.` | Verification/reset delivery failure; action `Try again`; do not state whether an account exists. |
| UX-MSG-045 | MVP | `Translation is limited to 5,000 characters. Remove {excess} characters to continue.` | Oversize error; no prefix submission or charge; retain complete source. |
| UX-MSG-046 | MVP | `Ready to process. Select Translate or Rewrite.` | Eligible idle state; use the actual feature action name, no pending timer. |
| UX-MSG-047 | MVP | `Input or settings changed. Previous result shown. Select Translate or Rewrite to update.` | Outdated result after input/settings change; use the actual feature action name. |

### 6.4 Announcements

One polite `role="status"` announces one processing start, one applied completion (Translation ready / Rewrite ready), copy, connectivity changes, and reconciled stale success. Do not announce each keystroke, counter mutation or stale failure. Use associated inline alerts for current errors without reading the entire result. Result `aria-busy` applies only during submitted current work. Use text with the spinner; reduced motion has a static glyph. An error never disappears because an older response arrives.

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

Support current and immediately previous stable major releases at each release candidate for Chrome, Edge, Firefox, desktop Safari, iOS Safari and Android Chrome. [Verification plan Section 4](06-verification-plan.md#4-frontend-browser-and-manual-verification) owns automated lane allocation, actual-version evidence and manual browser/device/assistive-technology procedures. Bundled engines alone do not establish this support contract.

### 10.2 Verification ownership

The accessibility outcomes above remain UX contracts. Automated checks and required manual evidence are consolidated in [verification plan Sections 4.2–4.4](06-verification-plan.md#42-browser-lanes); deferred journeys create no MVP evidence requirement.

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

### 12.1 Harness and fixtures

Scenarios are layer-independent behavioral contracts. [Verification plan Section 4.1](06-verification-plan.md#41-shared-ui-fixtures) owns their canonical fixture text/counts, fixed time, account seeds and request-count conventions. Its Sections 2–4 allocate assertions to units, components, API/database and native browser evidence; scripted outputs do not establish language quality.

### 12.2–12.9 Scenario disposition and current acceptance

`MVP` retains the scenario's active behavior; `Amended` replaces its old acceptance within the same intent; `Deferred` is linked to later scope; `Retired` is cut and must not become a placeholder test. Original steps are in Git `54343c3`; only the current contract below governs. For active rows, #6 splits policy/DOM/API/browser assertions at the lowest sufficient layer, without replaying all rows as browser journeys.

| ID | Status | Current acceptance / inactive disposition |
| --- | --- | --- |
| UX-AC-001 | MVP | Verified direct /rewrite opens with heading focus, Correction only selected, empty workspace and zero transformations. |
| UX-AC-002 | MVP | Mode links and Back/Forward restore per-page memory/scroll, focus the destination heading and submit nothing. |
| UX-AC-003 | MVP | Reload retains the route/auth as applicable but clears source/result/settings to current defaults; storage contains no text. |
| UX-AC-004 | Amended | Submit Translation A, navigate to Rewrite, resolve A: only the matching Translation state/usage changes; visible focus is untouched. |
| UX-AC-005 | Amended | Submit A, edit its source/settings through navigation, resolve A: it never replaces newer input/result; successful usage reconciles once. |
| UX-AC-006 | MVP | Valid local registration submits once, enters unverified verification page and never opens a protected workspace before verification. |
| UX-AC-007 | MVP | Missing/invalid email or mismatched registration passwords sends no request, shows linked errors and focuses first invalid field. |
| UX-AC-008 | MVP | Unverified protected navigation redirects to verification; no editor or transformation request. |
| UX-AC-009 | MVP | Resend submits once, reports success, disables for server cooldown, then reenables without language work. |
| UX-AC-010 | MVP | Valid verification link plus explicit continuation reaches the intended protected route; signed-out variant signs in first; no transformation. |
| UX-AC-011 | Deferred | DF-007 successful Google callback and return navigation. |
| UX-AC-012 | Deferred | DF-007 Google cancellation/provider failure recovery. |
| UX-AC-013 | MVP | Local login submits once, returns to the safe requested route with heading focus, and leaves no password in DOM/storage/history. |
| UX-AC-014 | MVP | Invalid local credentials show MSG-028, clear password and focus email without account-specific detail. |
| UX-AC-015 | MVP | Session expiry clears text before login, shows MSG-037, and rejects late text callbacks; storage/history remain clean. |
| UX-AC-016 | Amended | Inline Sign out clears immediately; pending/failure/success states follow 3.4; retry sends auth only and Back cannot expose old text. |
| UX-AC-017 | MVP | Forgot-password known/unknown valid email produces the same confirmation, success heading focus and Back to sign in link. |
| UX-AC-018 | MVP | Invalid forgot-password email sends no request and shows an associated error with field focus. |
| UX-AC-019 | MVP | Valid password reset shows success and explicit Go to sign in; no automatic login or restored workspace. |
| UX-AC-020 | MVP | Invalid/expired reset link shows recovery action without password fields or reset submission. |
| UX-AC-021 | Amended | Whitespace remains ineligible regardless of elapsed time; Translation has four targets and an explicit Translate button. |
| UX-AC-022 | Amended | Eligible T-OK-A typing/waiting sends zero transformations; activate Translate once to obtain complete fixture and usage 7,546. Delayed eligibility continues only the same still-current activation. |
| UX-AC-023 | Deferred | DF-003 debounce restart across repeated typing; no timer implementation in MVP. Absence of auto-submit is covered by AC-022/039. |
| UX-AC-024 | Amended | Source and target edits preserve previous result, show outdated status, and submit nothing until Translate; activation captures final values. |
| UX-AC-025 | Amended | Typing/composition/time sends nothing. Activation during composition is ignored without queued work; explicit post-composition activation submits once. Unit event cases plus native browser/manual IME evidence. |
| UX-AC-026 | Amended | Clear source keeps focus and prior copyable result with Source cleared status; no operation now or after waiting. |
| UX-AC-027 | Amended | Explicit uncertain-detection submission shows MSG-006 without transformation/charge; manual source selection alone sends nothing; a new activation processes. |
| UX-AC-028 | Amended | Same known source/target is invalid; selecting a different target enables Translate but only activation submits. |
| UX-AC-029 | Amended | Explicit mixed/unsupported submission is rejected without successful transformation/charge; manual language override does not bypass server validation. |
| UX-AC-030 | Amended | 5,312-character translation input shows excess 312 and MSG-045, retains all text, disables Translate and has no boundary overlay. |
| UX-AC-031 | Deferred | DF-006 successful 5,000-character prefix translation/copy/charge; MVP instead verifies whole-input blocking in AC-030/109. |
| UX-AC-032 | Amended | Submit valid maximum-length Translation, clear source before success: text response is not applied, usage charges submitted length once, and no second request occurs. |
| UX-AC-033 | MVP | Manual Translation result edit and copy cause no operation/usage change; clipboard equals current edited value. |
| UX-AC-034 | Amended | Deliver an old response after a workspace reset and a newer explicit submission: newest workspace/result alone applies. UI also suppresses duplicate submission while its own operation is pending. |
| UX-AC-035 | Amended | Outdated successful response updates authoritative usage once without replacing text/focus; notice reports charged amount without assuming it belongs to the current UTC day. |
| UX-AC-036 | Amended | Definitive processing failure preserves source/result; explicit Try again captures current fields once, with a new operation key. Failed outcome adds zero usage. |
| UX-AC-037 | Amended | During Translation update the previous result remains editable/copyable; a manual edit prevents pending output replacement. Later explicit submission can replace preexisting edits. |
| UX-AC-038 | Amended | New Rewrite shows Detect automatically and one Correction only mode, no toggle/None set/Show changes, plain result, explicit Rewrite and zero requests. |
| UX-AC-039 | Amended | Eligible Rewrite source remains idle after any wait; activate Rewrite once for a complete mandatory-correction result with the chosen single mode. |
| UX-AC-040 | Amended | Select Business then Friendly in native Writing mode: one final value, zero requests before explicit Rewrite, then one request with Friendly. |
| UX-AC-041 | Retired | Separate Correction-only toggle disabling Styles is removed; single-choice behavior is AC-038/040/110. |
| UX-AC-042 | Retired | Toggle-off restoration/None set default is removed; reset-to-Correction-only is AC-085/086. |
| UX-AC-043 | Amended | Rewrite composition and mode changes submit nothing. Ignore activation during composition; explicit activation after end captures current complete input/mode once. |
| UX-AC-044 | Amended | Explicit rewrite retains editable previous result while pending and preserves source/caret; current success replaces it completely when no intervening protected edit exists. |
| UX-AC-045 | Amended | Rewrite 2,001 chars shows excess 1, blocks with no charge and preserves copy. Delete one char: still no request until explicit Rewrite. |
| UX-AC-046 | Amended | Submit rewrite A then change source/style; A cannot apply. After it settles, activate Rewrite for B; B alone applies, with successful A usage retained. |
| UX-AC-047 | MVP | Manual result edit during pending rewrite A causes no request; A success updates usage/notice but never overwrites the edit. |
| UX-AC-048 | Deferred | DF-002 change highlighting and Show changes presentation. |
| UX-AC-049 | Deferred | DF-002 comparison opening, original/deletion annotations and focus behavior. |
| UX-AC-050 | Deferred | DF-002 comparison/highlight toggling and clean-copy markup exclusion; plain-copy MVP covered by AC-051. |
| UX-AC-051 | Amended | Plain rewritten result accepts native edits and copies the exact current text/newlines; zero extra requests/charges and no markup or comparison controls. |
| UX-AC-052 | Deferred | DF-001 sentence alternatives generation/loading/options. |
| UX-AC-053 | Deferred | DF-001 local alternative selection and sentence replacement. |
| UX-AC-054 | Deferred | DF-001 cache reuse without new operation/charge. |
| UX-AC-055 | Deferred | DF-001 selected-sentence change versus pending alternatives race and stale-success accounting. |
| UX-AC-056 | Deferred | DF-001 alternative failure/copy preservation/recovery. |
| UX-AC-057 | Deferred | DF-001 assistance invalidation after source/mode change; no menu/cache exists now. |
| UX-AC-058 | Amended | Manual result edit/compositionstart protects interim and committed text from older pending output and creates no operation. Composition cancellation never revives old output; no sentence metadata or targeted invalidation exists. |
| UX-AC-059 | Amended | Success from an edit-invalidated pending rewrite updates usage/notice once but preserves manual result text, selection and caret; old output is absent. |
| UX-AC-060 | Amended | After local result editing, only a later explicitly submitted current rewrite can replace the full result. Source/mode changes alone never replace it. |
| UX-AC-061 | Deferred | DF-001 pending alternatives invalidated by result edits; later assistance must use whole-metadata invalidation. |
| UX-AC-062 | Retired | Preserving non-touching sentence metadata after edits is cut, including its former alternatives-race contract. |
| UX-AC-063 | Retired | Targeted insertion/deletion correspondence and unaffected identities are cut. |
| UX-AC-064 | Retired | Targeted merge/split/boundary correspondence is cut. |
| UX-AC-065 | MVP | Inline user usage/remaining/reset is authoritative; global consumption/provider/routing controls are absent. |
| UX-AC-066 | Amended | Explicit T-OK-A then W-OK success updates shared authoritative usage to 7,546 then 7,592 with no optimistic increment. Local edits/copy/selector changes add zero; oversize rejection is uncharged. |
| UX-AC-067 | Amended | Configured optional fallback success returns one complete result and one logical charge; attempt details absent. Backend integration proves settlement. |
| UX-AC-068 | Amended | User allowance exhaustion preserves editing/copy; reset refresh enables eligible action but never submits automatically. |
| UX-AC-069 | Amended | Global allowance exhaustion shows shared-availability error without consumption details; recovery requires explicit new activation. |
| UX-AC-070 | Amended | Budget suspension preserves all work and copy; service recovery/refresh causes no paid submission. |
| UX-AC-071 | Amended | A successful result survives a failed usage refresh. Show Usage update unavailable and Refresh usage; activating it sends a usage read only and replaces the stale counter on success. |
| UX-AC-072 | MVP | Definitive total provider failure preserves source/result and reports processing failure with zero character charge. |
| UX-AC-073 | MVP | Overall deadline stops busy UI, preserves source/result, shows deadline error, and adds no character charge for definitive failure. |
| UX-AC-074 | Amended | Offline editing/copy remain usable. Reconnection only refreshes availability; explicit Translate/Rewrite is required. |
| UX-AC-075 | MVP | Interrupted operation uses bounded spinner then unknown-outcome/status recovery; never infers failure or zero charge from transport timeout. |
| UX-AC-076 | Amended | Deliver failure A after a newer workspace/current B success: B text/status/focus/usage remain and no stale A alert/toast appears. No old failure triggers a retry. |
| UX-AC-077 | Amended | Every active route at 320×640 has no horizontal page overflow/clipped controls; geometry checks use a real browser, not pixel baselines for every state. |
| UX-AC-078 | Amended | Curated current layouts match Section 4 at desktop/narrow sizes; other breakpoints use geometry; no old rails/sheets/diff baselines. |
| UX-AC-079 | Amended | Keyboard completes local auth, language/mode selection, explicit Translation/Rewrite, result edit/copy and reset without trap; Chromium subset plus targeted other-engine/manual checks. |
| UX-AC-080 | Deferred | DF-001 keyboard sentence-actions path and sentence shortcut. |
| UX-AC-081 | Amended | Active forms/editor/availability/dialog states have semantic/contrast coverage and focused browser accessibility scans; no serious/critical violations; manual AT still required. |
| UX-AC-082 | Amended | No announcement per key/selector change; one start/completion per activation, one copy, and current errors announced once; no full generated result auto-read. |
| UX-AC-083 | Amended | Reduced motion/text-spacing overrides preserve status, plain editor, native controls, auth errors and reset dialog without clipping/animation dependence. |
| UX-AC-084 | Amended | Inline Start new workspace opens exact dialog with initial Cancel focus; Escape/Cancel preserves state and returns trigger focus. |
| UX-AC-085 | Amended | Confirm reset with pending feature operations: both pages clear/default, Translation Source text focused; late responses restore no text, only independently authorized usage may reconcile. |
| UX-AC-086 | Amended | Empty workspace reset has no dialog; restores Correction only/detection/empty target defaults, focuses source and submits nothing. |
| UX-AC-087 | MVP | Distinctive text never enters URL/history/storage/cache. Real reload/full navigation/bfcache/tab-duplication checks return empty workspace; label unavailable real-device/cache evidence explicitly, not as synthetic proof. |
| UX-AC-088 | Amended | Rewrite detection uncertainty/failure occurs within explicit submission; recovery/selecting language alone does not submit; next activation validates again. |
| UX-AC-089 | Amended | Explicit Rewrite rejects substantially unsupported/mixed input with no transformation/charge, preserves prior result, and cannot be bypassed by manual source choice. |
| UX-AC-090 | Amended | Explicit Traditional-Chinese rewrite displays Simplified fixture result and clear language label, preserves original script in source, and has no target selector. |
| UX-AC-091 | Amended | All 12 Translation direction fixtures submit only on explicit activation; same known source/target is invalid and no style controls appear. |
| UX-AC-092 | Amended | Native language/mode select value/options/accessibility and platform keyboard behavior work; changing selection sends zero transformations. Old custom active-descendant/menu contract is DF-005. |
| UX-AC-093 | Deferred | DF-005 responsive Writing tools rail/sheet lifecycle; MVP inline mode responsiveness is AC-077/078. |
| UX-AC-094 | Amended | Native result selection/Shift/drag/paste preserves source/caret and protected text. No sentence affordance opens; affected-sentence metadata assertions are retired. |
| UX-AC-095 | Amended | Native labels and inline helper text remain readable under forced colors/reduced motion; no custom tooltip required; focus stays visible and no operation is triggered. |
| UX-AC-096 | Deferred | DF-003 pending debounce versus manual result edit. No debounce exists in MVP; later automation must respect whole-result edit protection without targeted metadata logic. |
| UX-AC-097 | Deferred | DF-001 alternative loading/cache reuse across panel close/reopen. |
| UX-AC-098 | Deferred | DF-001 alternative selection versus pending response race; later design must respect whole-metadata invalidation, not targeted preservation. |
| UX-AC-099 | Deferred | DF-001/DF-002 outdated assistance/comparison behavior. |
| UX-AC-100 | Amended | Native result edit/undo/select-all/delete preserves source, creates no request, and disables Copy when empty; sentence metadata/undo restoration is not implemented. |
| UX-AC-101 | MVP | Local form reveal/selection, missing/mismatched/policy validation, duplicate Enter guard, rejection, password clearing and first-error focus follow Section 9. |
| UX-AC-102 | MVP | Email/auth network failures show the proper current error without false success; explicit retry sends one account request and no language operation. |
| UX-AC-103 | MVP | Navigate to Forgot password while local login is pending: old completion cannot redirect/replace newer route, and passwords are not retained. |
| UX-AC-104 | MVP | Verification valid/invalid/expired/already-verified variants and signed-in/out flows expose explicit safe continuation; status check sends one auth read, no transformation. |
| UX-AC-105 | Amended | Required 20 with remaining 19 shows insufficiency; shortening to Hi. (3) sends nothing until activation, then charges 3 on success. Alternatives branch is DF-001. |
| UX-AC-106 | MVP | Out-of-order usage never reduces current-period count; newer UTC period may reset; ambiguous ordering triggers one fresh usage read, never text submission. |
| UX-AC-107 | Amended | Offline plus oversize/allowance errors coexist; stale A error suppressed. Restored eligibility/availability enables action but never automatically processes. |
| UX-AC-108 | MVP | Unknown outcome reaches deadline, stops spinner, preserves text and shows Check status; that action reads original status only. Later success updates usage but cannot replace newer/manual text. |
| UX-AC-109 | Amended | For each L=5,000/2,000, L−1/L submit whole text only on activation and L+1 blocks. Canonical Unicode counts agree with API; no suffix truncation or boundary marker. |
| UX-AC-110 | Amended | Four rewriting languages × nine exclusive modes validate correction plus selected intent; every case needs explicit activation. No toggle/None set or alternatives-count matrix in MVP. |
| UX-AC-111 | Deferred | DF-002 whole-deletion comparison and DF-001 multi-sentence alternatives; old targeted-identity assertions remain retired. |
| UX-AC-112 | MVP | Root and unknown paths route according to local auth/verification state; correct Go to action focuses destination heading and submits no language work. |

### 12.10 Curated visual regression

Section 4 owns the visual design. [Verification plan Section 4.4](06-verification-plan.md#44-curated-visual-regression) owns the seven initial baselines, capture setup, comparison tolerances and review procedure.

## 13. Verification boundaries

[Verification plan Sections 2–4](06-verification-plan.md#2-verification-layers-and-check-catalog) define the lowest sufficient evidence for each assertion and distinguish fixtures, integrated smoke and live/manual checks. The [local acceptance allocation](06-verification-plan.md#82-local-acceptance-allocation) preserves every UX-AC ID and its current disposition. No scenario here is a claim of a passing test.

## 14. MVP traceability

The sole cross-document [product and release coverage matrix](06-verification-plan.md#81-product-and-release-coverage) now lives in #6. This document retains UX behaviors, stories and scenario IDs; it does not maintain a second product-to-evidence table.

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
