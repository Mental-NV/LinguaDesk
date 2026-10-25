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
| UX-AC-002 | Amended | Mode links and Back/Forward restore per-page memory/scroll and focus the destination heading; leaving a feature cancels its undispatched timer, and returning alone submits nothing. |
| UX-AC-003 | MVP | Reload retains the route/auth as applicable but clears source/result/settings to current defaults; storage contains no text. |
| UX-AC-004 | Amended | Let paused Translation A dispatch, navigate to Rewrite, resolve A: only matching Translation state/accounting changes; visible focus is untouched. |
| UX-AC-005 | Amended | Let paused A dispatch, edit its source/settings through navigation, resolve A: it never replaces newer input/result; successful accounting reconciles once. |
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
| UX-AC-021 | Amended | Whitespace remains ineligible regardless of elapsed time; Translation has four targets and no Translate button. |
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
| UX-AC-033 | Amended | Manual Translation result edit and native selection/copy cause no operation or accounting change; clipboard equals the selected current edited value. |
| UX-AC-034 | Amended | Deliver an old response after full-navigation teardown and a newer paused candidate: newest workspace/result alone applies. One unsettled operation per page is preserved. |
| UX-AC-035 | Amended | Outdated successful response updates authoritative accounting once without replacing text/focus; a standalone event alert discloses the charged amount and admission period. |
| UX-AC-036 | Amended | Definitive processing failure preserves source/result; explicit Try again captures current fields once, with a new operation key. Failed outcome adds zero usage. |
| UX-AC-037 | Amended | During a Translation update the previous result remains editable/natively copyable; a manual edit prevents pending output replacement. A later source/settings revision may trigger replacement. |
| UX-AC-038 | Amended | New Rewrite shows Detect automatically and one Correction only mode, no toggle/None set/Show changes or Rewrite button, plain result, and zero requests before eligible input. |
| UX-AC-039 | Amended | Eligible Rewrite source dispatches once after one second of inactivity and returns a complete mandatory-correction result with the chosen single mode. |
| UX-AC-040 | Amended | Select Business then Friendly in native Writing mode: the timer restarts, and one request with Friendly dispatches after the final one-second pause. |
| UX-AC-041 | Retired | Separate Correction-only toggle disabling Styles is removed; single-choice behavior is AC-038/040/110. |
| UX-AC-042 | Retired | Toggle-off restoration/None set default is removed; new-workspace defaults are covered by AC-001/003. |
| UX-AC-043 | Amended | Rewrite mode changes restart the timer; no interim composition text dispatches. After compositionend, a settled eligible candidate captures complete input/mode once. |
| UX-AC-044 | Amended | Paused rewrite retains the editable previous result while pending and preserves source/caret; current success replaces it when no protected edit intervenes. |
| UX-AC-045 | Amended | Rewrite 2,001 characters is blocked with MSG-011, no persistent count/limit display, and no charge. Shortening to an eligible revision restarts the timer. |
| UX-AC-046 | Amended | Let rewrite A dispatch, then change source/style; A cannot apply. After its known terminal outcome, the latest paused B dispatches once and alone applies; successful A accounting remains. |
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
| UX-AC-059 | Amended | Success from an edit-invalidated pending rewrite updates accounting/disclosure once but preserves manual result text, selection and caret; old output is absent. |
| UX-AC-060 | Amended | After local result editing, only a later current rewrite triggered by a settled source/settings revision can replace the full result. The edit itself starts no operation. |
| UX-AC-061 | Deferred | DF-001 pending alternatives invalidated by result edits; later assistance must use whole-metadata invalidation. |
| UX-AC-062 | Retired | Preserving non-touching sentence metadata after edits is cut, including its former alternatives-race contract. |
| UX-AC-063 | Retired | Targeted insertion/deletion correspondence and unaffected identities are cut. |
| UX-AC-064 | Retired | Targeted merge/split/boundary correspondence is cut. |
| UX-AC-065 | Amended | No persistent usage summary is shown; the server remains authoritative for accounting, while essential allowance/availability alerts appear without global consumption/provider/routing details. |
| UX-AC-066 | Amended | Paused T-OK-A then W-OK success updates shared authoritative accounting to 7,546 then 7,592 without optimistic increment. Result edits/native copy add zero; eligible selector changes may trigger work; oversize rejection is uncharged. |
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
| UX-AC-078 | Amended | Curated layouts match Section 4 at desktop/narrow sizes, including equal source/result panel width and height on desktop; other breakpoints use geometry. Removed controls and status lines are absent. |
| UX-AC-079 | Retired | Keyboard-only journey completion is outside MVP requirements; the Chromium keyboard subset persists as non-gating engine evidence. |
| UX-AC-080 | Deferred | DF-001 keyboard sentence-actions path and sentence shortcut. |
| UX-AC-081 | Amended | Active forms/editor/availability/dialog states have semantic/contrast coverage and focused browser accessibility scans with no serious/critical violations. |
| UX-AC-082 | Amended | No announcement per key/selector change or timer restart; one assistive start/completion per dispatched operation and current errors announced once; no copy-status or full generated result auto-read. |
| UX-AC-083 | Amended | Reduced motion/text-spacing overrides preserve plain editors, native controls and essential/auth alerts without clipping or animation dependence. |
| UX-AC-084 | Retired | The New workspace control and reset dialog are removed from the active UI. |
| UX-AC-085 | Retired | Manual workspace reset is removed; teardown during full navigation/sign-out is covered by AC-003/015/087. |
| UX-AC-086 | Retired | Empty-workspace manual reset is removed; defaults on a new tab or reload are covered by AC-001/003. |
| UX-AC-087 | MVP | Distinctive text never enters URL/history/storage/cache. Real reload/full navigation/bfcache/tab-duplication checks return empty workspace; label unavailable real-device/cache evidence explicitly, not as synthetic proof. |
| UX-AC-088 | Amended | Rewrite detection uncertainty/failure occurs for a paused candidate; correcting source/language starts a new timer, with no automatic retry of the unchanged failed revision. |
| UX-AC-089 | Amended | Paused Rewrite rejects substantially unsupported/mixed input with no transformation/charge, preserves prior result, and cannot be bypassed by manual source choice. |
| UX-AC-090 | Amended | Paused Traditional-Chinese rewrite displays Simplified fixture result and clear language label, preserves original script in source, and has no target selector. |
| UX-AC-091 | Amended | All 12 Translation directions dispatch after an eligible one-second pause; same known source/target is invalid and no style controls appear. |
| UX-AC-092 | Amended | Native language/mode select value/options/accessibility work; changing selection restarts the timer for eligible source. Old custom active-descendant/menu contract is DF-005. |
| UX-AC-093 | Deferred | DF-005 responsive Writing tools rail/sheet lifecycle; MVP inline mode responsiveness is AC-077/078. |
| UX-AC-094 | Amended | Native result selection/Shift/drag/paste preserves source/caret and protected text. No sentence affordance opens; affected-sentence metadata assertions are retired. |
| UX-AC-095 | Amended | Native labels and essential page alerts remain readable under forced colors/reduced motion; focus stays visible and no duplicate operation is triggered. |
| UX-AC-096 | Amended | A manual result edit cancels an undispatched timer/queued candidate and protects against an already sent operation; the edit itself never starts a new operation. |
| UX-AC-097 | Deferred | DF-001 alternative loading/cache reuse across panel close/reopen. |
| UX-AC-098 | Deferred | DF-001 alternative selection versus pending response race; later design must respect whole-metadata invalidation, not targeted preservation. |
| UX-AC-099 | Deferred | DF-001/DF-002 outdated assistance/comparison behavior. |
| UX-AC-100 | Amended | Native result edit/undo/select-all/delete preserves source, creates no request, and disables Copy when empty; sentence metadata/undo restoration is not implemented. |
| UX-AC-101 | MVP | Local form reveal/selection, missing/mismatched/policy validation, duplicate Enter guard, rejection, password clearing and first-error focus follow Section 9. |
| UX-AC-102 | MVP | Email/auth network failures show the proper current error without false success; explicit retry sends one account request and no language operation. |
| UX-AC-103 | MVP | Navigate to Forgot password while local login is pending: old completion cannot redirect/replace newer route, and passwords are not retained. |
| UX-AC-104 | MVP | Verification valid/invalid/expired/already-verified variants and signed-in/out flows expose explicit safe continuation; status check sends one auth read, no transformation. |
| UX-AC-105 | Amended | Required 20 with remaining 19 shows insufficiency; shortening to Hi. (3) starts a pause and charges 3 on success. Alternatives branch is DF-001. |
| UX-AC-106 | MVP | Out-of-order usage never reduces current-period count; newer UTC period may reset; ambiguous ordering triggers one fresh usage read, never text submission. |
| UX-AC-107 | Amended | Offline plus oversize/allowance alerts coexist; stale A error is suppressed. Restored availability alone sends no paid operation; a new eligible edit can start a pause. |
| UX-AC-108 | Amended | Unknown outcome reaches deadline, preserves text and shows Check status; that action reads original status only. Later success updates accounting but cannot replace newer/manual text. |
| UX-AC-109 | Amended | For each L=5,000/2,000, L−1/L dispatch whole text after an eligible pause and L+1 blocks. Canonical Unicode counts agree with API; no persistent counter, suffix truncation or boundary marker. |
| UX-AC-110 | Amended | Four rewriting languages × nine exclusive modes validate correction plus selected intent after an eligible pause. No toggle/None set or alternatives-count matrix in MVP. |
| UX-AC-111 | Deferred | DF-002 whole-deletion comparison and DF-001 multi-sentence alternatives; old targeted-identity assertions remain retired. |
| UX-AC-112 | MVP | Root and unknown paths route according to local auth/verification state; correct Go to action focuses destination heading and submits no language work. |

### 12.10 Curated visual regression

Section 4 owns the visual design. [Verification plan Section 4.4](../verification/frontend.md#44-curated-visual-regression) owns the seven initial baselines, capture setup, comparison tolerances and review procedure.
