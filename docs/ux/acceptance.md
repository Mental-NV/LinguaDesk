# UX acceptance contracts

Authoritative continuation of [02-ux-specification.md](../02-ux-specification.md); section numbers refer to that document; read only when relevant to the selected scope.

## 12. Acceptance contracts and preserved scenario IDs

### 12.1 Harness and fixtures

Scenarios are layer-independent behavioral contracts. [Verification plan Section 4.1](../verification/frontend.md#41-shared-ui-fixtures) owns their canonical fixture text/counts, fixed time, account seeds and request-count conventions. Its Sections 2–4 allocate assertions to units, components, API/database and native browser evidence; scripted outputs do not establish language quality.

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

Section 4 owns the visual design. [Verification plan Section 4.4](../verification/frontend.md#44-curated-visual-regression) owns the seven initial baselines, capture setup, comparison tolerances and review procedure.
