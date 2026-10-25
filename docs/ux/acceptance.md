# UX acceptance contracts

Authoritative continuation of [02-ux-specification.md](../02-ux-specification.md); section numbers refer to that document; read only when relevant to the selected scope.

## 12. Acceptance contracts and preserved scenario IDs

### 12.1 Harness and fixtures

Scenarios are layer-independent behavioral contracts. [Verification plan Section 4.1](../verification/frontend.md#41-shared-ui-fixtures) owns their canonical fixture text/counts, fixed time, account seeds and request-count conventions. Its Sections 2–4 allocate assertions to units, components, API/database and native browser evidence; scripted outputs do not establish language quality.

### 12.2–12.9 Scenario disposition and current acceptance

`MVP` retains the scenario's active behavior; `Amended` replaces its old acceptance within the same intent; `Deferred` is linked to later scope; `Retired` is cut and must not become a placeholder test. Original steps are in Git `54343c3`; only the current contract below governs. For active rows, #6 splits policy/DOM/API/browser assertions at the lowest sufficient layer, without replaying all rows as browser journeys.

| ID | Status | Current acceptance / inactive disposition |
| --- | --- | --- |
| UX-AC-001 | Amended | Verified direct /rewrite opens with heading focus, empty workspace, Correction Only on, Styles/Tones None set disabled, Show changes off and both panels collapsed; zero transformations. |
| UX-AC-002 | Amended | Mode links and Back/Forward restore per-page memory/scroll and focus the destination heading; leaving a feature cancels its undispatched timer, and returning alone submits nothing. |
| UX-AC-003 | MVP | Reload retains the route/auth as applicable but clears source/result/settings to current defaults; storage contains no text. |
| UX-AC-004 | Amended | Let paused Translation A dispatch, navigate to Rewrite, resolve A: only matching Translation state/accounting changes; visible focus is untouched. |
| UX-AC-005 | Amended | Let paused A dispatch, edit its source/settings through navigation, resolve A: it never replaces newer input/result; successful accounting reconciles once. |
| UX-AC-006 | Amended | Valid local registration submits once, replaces the form with MSG-034 and explicit Go to sign in, retains no credentials, and never signs in or starts processing; DF-008 temporarily creates verified accounts. |
| UX-AC-007 | MVP | Missing/invalid email or mismatched registration passwords sends no request, shows linked errors and focuses first invalid field. |
| UX-AC-008 | MVP | Unverified protected navigation redirects to verification; no editor or transformation request. |
| UX-AC-009 | MVP | Resend submits once, reports success, disables for server cooldown, then reenables without language work. |
| UX-AC-010 | MVP | Valid verification link plus explicit continuation reaches the intended protected route; signed-out variant signs in first; no transformation. |
| UX-AC-011 | Deferred | DF-007 successful Google callback and return navigation. |
| UX-AC-012 | Deferred | DF-007 Google cancellation/provider failure recovery. |
| UX-AC-013 | MVP | Local login submits once, returns to the safe requested route with heading focus, and leaves no password in DOM/storage/history. |
| UX-AC-014 | MVP | Invalid local credentials show MSG-028, clear password and focus email without account-specific detail. |
| UX-AC-015 | MVP | Session expiry clears text before login, shows MSG-037, and rejects late text callbacks; storage/history remain clean. |
| UX-AC-016 | Amended | Account opens Log out without submitting or clearing text; Log out clears immediately. Pending/failure/success follow §3.4; retry sends auth only and Back cannot reveal old text. |
| UX-AC-017 | MVP | Forgot-password known/unknown valid email produces the same confirmation, success heading focus and Back to sign in link. |
| UX-AC-018 | MVP | Invalid forgot-password email sends no request and shows an associated error with field focus. |
| UX-AC-019 | MVP | Valid password reset shows success and explicit Go to sign in; no automatic login or restored workspace. |
| UX-AC-020 | MVP | Invalid/expired reset link shows recovery action without password fields or reset submission. |
| UX-AC-021 | Amended | Whitespace remains ineligible regardless of elapsed time; Translation has the targets advertised by the shared catalog and no Translate button. |
| UX-AC-022 | Amended | Eligible T-OK-A sends no operation before one second of inactivity, then one complete translation and authoritative accounting 7,546; no unchanged-revision repeat. |
| UX-AC-023 | Amended | Repeated source/settings edits restart the one-second timer; only the latest eligible settled revision dispatches once. |
| UX-AC-024 | Amended | Source and target edits preserve the previous result and restart the timer; only the latest eligible values dispatch after the pause. |
| UX-AC-025 | Amended | No interim IME text dispatches; compositionend starts a fresh one-second timer for the committed eligible revision. Unit/event-guard cases and Chromium browser checks establish the automated contract. Native IME/device review is optional and nonblocking under D-22; synthetic events are not reported as native IME evidence. |
| UX-AC-026 | Amended | Clear source keeps focus and the prior natively copyable result, cancels the timer and causes no operation. |
| UX-AC-027 | Amended | Paused uncertain-detection candidate shows MSG-006 without transformation/charge; corrected source selection starts a new pause. |
| UX-AC-028 | Amended | Same known source/target is invalid; choosing a different target with eligible source starts a one-second pause and then dispatches once. |
| UX-AC-029 | Amended | Paused mixed/unsupported candidate is rejected without successful transformation/charge; manual language override does not bypass server validation. |
| UX-AC-030 | Amended | 5,312-character Translation input is blocked with MSG-045, retains all text, has no persistent count/limit display or boundary overlay, and incurs no charge. |
| UX-AC-031 | Deferred | DF-006 successful 5,000-character prefix translation/copy/charge; MVP instead verifies whole-input blocking in AC-030/109. |
| UX-AC-032 | Amended | Let maximum-length Translation dispatch, then clear source before success: text response is not applied, accounting charges submitted length once, and no second request occurs. |
| UX-AC-033 | Amended | Manual Translator edits, native selection/copy and Copy source text / Copy result cause no operation or charge; clipboard equals the current corresponding plain value including newlines. |
| UX-AC-034 | Amended | Deliver an old response after full-navigation teardown and a newer paused candidate: newest workspace/result alone applies. One unsettled operation per page is preserved. |
| UX-AC-035 | Amended | Outdated successful response updates authoritative accounting once without replacing text/focus; a standalone event alert discloses the charged amount and admission period. |
| UX-AC-036 | Amended | Definitive processing failure preserves source/result; explicit Try again captures current fields once, with a new operation key. Failed outcome adds zero usage. |
| UX-AC-037 | Amended | During a Translation update the previous result remains editable/natively copyable; a manual edit prevents pending output replacement. A later source/settings revision may trigger replacement. |
| UX-AC-038 | Amended | New Write shows Detect language, Correction Only on, None set disabled and Show changes off in its collapsible toolbar; no Rewrite button or request before eligible input. |
| UX-AC-039 | Amended | Eligible Rewrite source dispatches once after one second of inactivity and returns a complete mandatory-correction result with the chosen single mode. |
| UX-AC-040 | Amended | With Correction Only off, select Business then Friendly in Styles/Tones: restart the timer only for effective mode changes and dispatch one Friendly request after the final pause. |
| UX-AC-041 | Retired | Historical disable/restore contract stays retired; new reset-to-None-set toggle is covered by AC-113/114. |
| UX-AC-042 | Retired | Historical remembered-style restoration stays retired; D-23 never restores a hidden previous choice. |
| UX-AC-043 | Amended | Rewrite mode changes restart the timer; no interim composition text dispatches. After compositionend, a settled eligible candidate captures complete input/mode once. |
| UX-AC-044 | Amended | Paused rewrite retains the editable previous result while pending and preserves source/caret; current success replaces it when no protected edit intervenes. |
| UX-AC-045 | Amended | Rewrite 2,001 characters is blocked with MSG-011, no persistent count/limit display, and no charge. Shortening to an eligible revision restarts the timer. |
| UX-AC-046 | Amended | Let rewrite A dispatch, then change source/style; A cannot apply. After its known terminal outcome, the latest paused B dispatches once and alone applies; successful A accounting remains. |
| UX-AC-047 | MVP | Manual result edit during pending rewrite A causes no request; A success updates usage/notice but never overwrites the edit. |
| UX-AC-048 | Amended | Show changes on renders current rewrite insertions/replacements green and underlined; off removes diff markup without changing result, alternatives, requests or usage. |
| UX-AC-049 | Amended | Review uses the submitted source snapshot, exposes labeled deleted text including whole-sentence deletion, preserves focus and never uses later source edits as the comparison baseline. |
| UX-AC-050 | Amended | Toggle changes repeatedly and select a cached alternative: comparison updates locally, plain copy excludes annotations/deletions/options, and no paid request is created. |
| UX-AC-051 | Amended | Edit result enters native editing; Done editing retains exact edited text/newlines. Native copy and Copy result stay clean and uncharged; manual edits invalidate all assistance. |
| UX-AC-052 | Amended | Activate a current result sentence: one on-demand alternative-set request, loading within the result card, then two option rows. No automatic generation on full rewrite or typing. |
| UX-AC-053 | Amended | Select an alternative: replace only its selected sentence, preserve source and other result text, refresh comparison locally, no timer/request/charge. |
| UX-AC-054 | Amended | Reopen the same valid sentence set and switch original/generated choices: reuse memory-only cached choices without paid work. Context-changing replacement clears other sentence caches. |
| UX-AC-055 | Amended | Invalidate result/source/settings while alternatives are pending: late options cannot replace text or reopen assistance; successful selected-sentence charge settles once. |
| UX-AC-056 | Amended | Alternative failure preserves text/copy and presents explicit retry only for known failure; unknown outcome offers status-only recovery, never blind regeneration. |
| UX-AC-057 | Amended | Source/language/effective-mode changes clear all sentence caches/comparison and suppress pending associations; a later current full rewrite creates fresh metadata. |
| UX-AC-058 | Amended | Manual result edit/compositionstart protects interim and committed text from older pending output and creates no operation. Composition cancellation never revives old output; all sentence/comparison metadata is invalidated; targeted preservation stays retired. |
| UX-AC-059 | Amended | Success from an edit-invalidated pending rewrite updates accounting/disclosure once but preserves manual result text, selection and caret; old output is absent. |
| UX-AC-060 | Amended | After local result editing, only a later current rewrite triggered by a settled source/settings revision can replace the full result. The edit itself starts no operation. |
| UX-AC-061 | Amended | Any manual result edit/compositionstart invalidates all alternative/cache/comparison metadata and pending associations; undo does not restore it. |
| UX-AC-062 | Retired | Preserving non-touching sentence metadata after edits is cut, including its former alternatives-race contract. |
| UX-AC-063 | Retired | Targeted insertion/deletion correspondence and unaffected identities are cut. |
| UX-AC-064 | Retired | Targeted merge/split/boundary correspondence is cut. |
| UX-AC-065 | Amended | No persistent usage summary is shown; the server remains authoritative for accounting, while essential allowance/availability alerts appear without global consumption/provider/routing details. |
| UX-AC-066 | Amended | T-OK-A then W-OK success updates usage to 7,546 then 7,592. A successful option set charges selected sentence length once; native/button copy, review and local selection/reuse add zero. |
| UX-AC-067 | Amended | Configured optional fallback success returns one complete result and one logical charge; attempt details absent. Backend integration proves settlement. |
| UX-AC-068 | Amended | User allowance exhaustion preserves editing/native copy; reset refresh alone starts no operation. A new eligible source/settings revision may process after the pause. |
| UX-AC-069 | Amended | Global allowance exhaustion shows an essential availability alert without consumption details; recovery alone sends no paid operation. |
| UX-AC-070 | Amended | Budget suspension preserves all work and native copy; service recovery/refresh sends no paid operation. |
| UX-AC-071 | Amended | A successful result survives a failed usage refresh. No counter or Refresh usage control appears; the next relevant eligibility check retries the usage read without paid dispatch. |
| UX-AC-072 | MVP | Definitive total provider failure preserves source/result and reports processing failure with zero character charge. |
| UX-AC-073 | MVP | Overall deadline stops busy UI, preserves source/result, shows deadline error, and adds no character charge for definitive failure. |
| UX-AC-074 | Amended | Offline editing/native copy remain usable. Reconnection only refreshes availability; a new eligible source/settings edit and pause are required for paid processing. |
| UX-AC-075 | Amended | Interrupted operation preserves work through its deadline, then offers unknown-outcome/Check status recovery; never infers failure or zero charge from transport timeout. No persistent processing status line is shown. |
| UX-AC-076 | Amended | Deliver failure A after a newer current B success: B text/focus/accounting remain and no stale A alert appears. No old failure triggers a retry. |
| UX-AC-077 | Amended | Every active route at 320×640 has no horizontal page overflow/clipped controls; geometry checks use a real browser, not pixel baselines for every state. |
| UX-AC-078 | Amended | Section 4 geometry holds for both pages and independent desktop panel combinations, with equal editor width/height; mobile stacks source/result and tools without overflow or duplicate controls. |
| UX-AC-079 | Retired | Keyboard-only journey completion is outside MVP requirements; the Chromium keyboard subset persists as non-gating engine evidence. |
| UX-AC-080 | Amended | Sentence activation and alternative choice work by keyboard without hijacking Enter in text editing; drag selection copies without generating options. |
| UX-AC-081 | Amended | Active forms/editor/availability/dialog states have semantic/contrast coverage and focused browser accessibility scans with no serious/critical violations. |
| UX-AC-082 | Amended | No per-key/timer announcement. One assistive start/completion per paid operation, one current error and concise copy outcome; never auto-read full result/options. |
| UX-AC-083 | Amended | Reduced motion/text-spacing overrides preserve plain editors, native controls and essential/auth alerts without clipping or animation dependence. |
| UX-AC-084 | Retired | The New workspace control and reset dialog are removed from the active UI. |
| UX-AC-085 | Retired | Manual workspace reset is removed; teardown during full navigation/sign-out is covered by AC-003/015/087. |
| UX-AC-086 | Retired | Empty-workspace manual reset is removed; defaults on a new tab or reload are covered by AC-001/003. |
| UX-AC-087 | MVP | Distinctive text never enters URL/history/storage/cache. Real reload/full navigation/bfcache/tab-duplication checks return empty workspace; label unavailable real-device/cache evidence explicitly, not as synthetic proof. |
| UX-AC-088 | Amended | Rewrite detection uncertainty/failure occurs for a paused candidate; correcting source/language starts a new timer, with no automatic retry of the unchanged failed revision. |
| UX-AC-089 | Amended | Paused Rewrite rejects substantially unsupported/mixed input with no transformation/charge, preserves prior result, and cannot be bypassed by manual source choice. |
| UX-AC-090 | Amended | Paused Traditional-Chinese rewrite displays Simplified fixture result and clear language label, preserves original script in source, and has no target selector. |
| UX-AC-091 | Amended | All selected Translation directions dispatch after the pause; same-language remains invalid. Toolbar writing settings are disabled and never sent to Translation. |
| UX-AC-092 | Amended | Language selects retain native behavior; Styles/Tones has accessible groups, single selection, arrows/Home/End/typeahead/commit/Escape, checked pale-blue row and bounded scrolling. |
| UX-AC-093 | Amended | Navigation and Toolbar collapse independently, preserve text/settings and submit nothing. Desktop rail resizing and compact/mobile in-flow disclosures match §4; hidden controls leave focus order and collapse restores trigger focus. |
| UX-AC-094 | Amended | Native result selection/Shift/drag/paste preserves source and protected text. Drag selection does not open alternatives; Edit result permits native caret/IME behavior. |
| UX-AC-095 | Amended | Native labels and essential page alerts remain readable under forced colors/reduced motion; focus stays visible and no duplicate operation is triggered. |
| UX-AC-096 | Amended | A manual result edit cancels an undispatched timer/queued candidate and protects against an already sent operation; the edit itself never starts a new operation. |
| UX-AC-097 | Amended | Closing/reopening toolbar or the current alternatives area preserves a valid in-memory set; no duplicate generation or charge; teardown removes the set. |
| UX-AC-098 | Amended | Alternative choice is allowed only for its matching result/context. A later full-result success or manual edit prevents stale option selection; no targeted preservation is assumed. |
| UX-AC-099 | Amended | Outdated or manually edited result disables review/alternatives, hides invalid annotations and retains clean editable/copyable text until a current full rewrite rebuilds metadata. |
| UX-AC-100 | Amended | Manual result edit/undo/select-all/delete preserves source and starts no request; plain copy remains exact and no undo restores invalidated assistance. |
| UX-AC-101 | MVP | Local form reveal/selection, missing/mismatched/policy validation, duplicate Enter guard, rejection, password clearing and first-error focus follow Section 9. |
| UX-AC-102 | MVP | Email/auth network failures show the proper current error without false success; explicit retry sends one account request and no language operation. |
| UX-AC-103 | MVP | Navigate to Forgot password while local login is pending: old completion cannot redirect/replace newer route, and passwords are not retained. |
| UX-AC-104 | MVP | Verification valid/invalid/expired/already-verified variants and signed-in/out flows expose explicit safe continuation; status check sends one auth read, no transformation. |
| UX-AC-105 | Amended | Required 20 with remaining 19 is blocked; shortening to Hi. processes 3. Alternatives reserve selected sentence length, not context or generated-option length; local reuse charges zero. |
| UX-AC-106 | MVP | Out-of-order usage never reduces current-period count; newer UTC period may reset; ambiguous ordering triggers one fresh usage read, never text submission. |
| UX-AC-107 | Amended | Offline plus oversize/allowance alerts coexist; stale A error is suppressed. Restored availability alone sends no paid operation; a new eligible edit can start a pause. |
| UX-AC-108 | Amended | Unknown outcome reaches deadline, preserves text and shows Check status; that action reads original status only. Later success updates accounting but cannot replace newer/manual text. |
| UX-AC-109 | Amended | For each L=5,000/2,000, L−1/L dispatch whole text after an eligible pause and L+1 blocks. Canonical Unicode counts agree with API; no persistent counter, suffix truncation or boundary marker. |
| UX-AC-110 | Amended | Four rewriting languages × nine effective API modes retain correction and selected intent. UI on/None-set states map to correctionOnly; style/tone stays exclusive. |
| UX-AC-111 | Amended | Whole-sentence deletion remains reviewable and selecting alternatives among multiple sentences replaces only the chosen span; targeted identity preservation through arbitrary editing stays retired. |
| UX-AC-112 | MVP | Root and unknown paths route according to local auth/verification state; correct Go to action focuses destination heading and submits no language work. |
| UX-AC-113 | MVP | Correction Only off enables None set. Selecting Business then turning the switch on clears Business, disables Styles/Tones and resolves correctionOnly; one new pause only when effective mode changes. |
| UX-AC-114 | MVP | Toggling Correction Only off/on while None set does not dispatch or restart a timer; turning it off after a reset never restores an old style. |
| UX-AC-115 | MVP | Translator swap reverses languages and texts atomically using a current result/resolved source. Write reuse copies result into source without language swap. Empty/stale/unsettled states disable actions; eligible changed source starts exactly one pause and oversized input stays blocked. |
| UX-AC-116 | MVP | Clipboard success/failure is accurately announced; permission denial preserves text/selection and native copy. Copy never includes diff deletions or alternatives and creates no provider work. |
| UX-AC-117 | MVP | No document upload/drop target/helper, DeepL branding, premium controls or Customizations section appears. Toolbar has exactly Correction Only, Styles/Tones, Show changes. |
| UX-AC-118 | MVP | Both panel defaults and page settings survive SPA navigation but reset on teardown; responsive breakpoint changes and disclosures never dispatch or destroy source/result/caret. |
| UX-AC-119 | MVP | Open Styles/Tones matches #6 including None set, grouped order, Business check and pale-blue row; cropped tone entries use the existing catalog. Menu fits/scrolls at 320 px and is not clipped by panels. |

### 12.10 Curated visual regression

Section 4 owns the visual design. [Verification plan Section 4.4](../verification/frontend.md#44-curated-visual-regression) owns the revised D-23 baselines, capture setup, comparison tolerances and review procedure.
