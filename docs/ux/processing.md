# UX processing and message contract

Authoritative continuation of [02-ux-specification.md](../02-ux-specification.md); section numbers refer to that document; read only when relevant to the selected scope.

## 6. Processing state and message contract

### 6.1 Pause processing and competing events

| Event/state | Required outcome |
| --- | --- |
| Initial / empty source | No operation or timer; target selection remains available |
| Source, language or mode edit | Recompute local validity, increment revision, mark previous result outdated, and restart a one-second timer only for an eligible candidate |
| Known invalid / oversized input | Cancel timer; preserve all text; show an essential page alert when an attempted candidate is blocked; no provider operation or character charge |
| Eligible pause expires | Keep the latest source/settings revision; when dispatch is allowed, capture the then-current result-edit revision, validate/detect and dispatch once if still current |
| Composition active | Cancel timer and suppress dispatch; after `compositionend`, start a new one-second timer for the committed eligible revision |
| Manual result edit | Cancel any undispatched timer/queued candidate; invalidate older pending text responses; edit alone starts no new timer |
| Leave feature via SPA navigation | Cancel its undispatched timer/queued candidate; preserve page memory and allow an already sent operation to settle for its owning feature. Returning alone starts no timer |
| Already pending on this page | Keep only the latest eligible paused source/settings revision queued; dispatch it once after the prior operation reaches a known terminal outcome, if still current. Capture result-edit revision at actual dispatch. The other feature may process independently |
| Current valid success | Replace result with complete output; preserve source/caret; reconcile authoritative accounting without a persistent usage summary |
| Source/settings or result edited since submission | Never apply returned text. Successful operation still updates accounting and stale-success disclosure; stale errors do not replace newer validation |
| Definitive failure / deadline | Stop busy state; preserve source/result; show classified page alert; no character charge. Do not retry an unchanged revision automatically; Try again may explicitly start a new operation |
| Unknown outcome after transport interruption | Stop spinner at overall deadline, preserve work, show Check status. That action reads status only; do not claim zero charge or blindly resubmit. Resolve the earlier outcome before offering retry on this page |
| Reconnect / usage refresh / midnight reset | Refresh availability if needed; no paid operation starts until a new eligible source/settings edit and pause |
| Sign-out / expiry / full navigation | Cancel timers, invalidate callbacks and clear text before navigation; subsequent old responses cannot restore it |

Resolving detection/eligibility may continue the same paused candidate only while its captured revisions still match. Any intervening edit cancels that continuation and restarts the timer for the new eligible revision. Avoid background remote detection while typing. The API remains authoritative for supported content, counts, allowances and output validity; clients cannot bypass validation with manual source selection.

Starting IME composition in either editor invalidates earlier pending text responses before interim text can be overwritten. Cancelling composition does not revive those responses. Result editing cancels an undispatched timer/queued candidate and never starts one; only a committed eligible source/settings revision can do so. No persistent Edited/result status line is shown.

Manual result edits are local and uncharged in both features. They do not mutate source. A later operation triggered by source/settings changes may replace edits already present at its start, but edits made during its processing remain protected. There is no sentence metadata to preserve or invalidate in MVP.

### 6.2 Response ordering and recovery

Usage snapshots follow #5 Section 7's UTC-day and durable revision ordering. Older snapshots cannot replace newer consumed/reserved/availability state; releases can legitimately increase available capacity, and a new UTC day resets current-day usage. If order is ambiguous, perform one fresh usage read. Text application and accounting are independent: stale success can charge its admission day without replacing text. The UI does not display a persistent usage total; any event disclosure uses reconciled authoritative values.

`Try again` applies only to definitive failure and captures the current fields; it is not a Translate/Rewrite control. `Check status` uses the original operation identity; it never calls the provider. Unknown-outcome message: `We couldn’t confirm whether this request completed. Your text is safe. Check its status before trying again.` A recorded success with lost output must disclose that the result is unavailable and any confirmed usage; #5 supplies the concrete outcome/category before recovery implementation. An unchanged revision must never auto-retry after a failure or unknown outcome, and a new paid operation must not masquerade as replay of the old key.

### 6.3 Exact message IDs and disposition

Current messages below are normative English copy. Deferred/retired IDs retain their former purpose without requiring a hidden UI or translation string now. Numeric examples are supplied from current configured limits/contract fields, not independent hardcoded policies.

| ID | Status | Exact text | Placement / recovery |
| --- | --- | --- | --- |
| UX-MSG-001 | Retired | — | No visible waiting/debounce status line in the paused UI. |
| UX-MSG-002 | Retired | — | No visible waiting-to-update status line in the paused UI. |
| UX-MSG-003 | Retired | — | No persistent first-result processing status line. |
| UX-MSG-004 | Retired | — | No persistent updating status line. |
| UX-MSG-005 | Retired | — | No persistent up-to-date result status line. |
| UX-MSG-006 | MVP | `Choose the source language to continue.` | Uncertain-detection page alert; a corrected source/settings revision starts a new pause. |
| UX-MSG-007 | MVP | `Choose a target language different from {language}.` | Target selector error for same-language translation. |
| UX-MSG-008 | MVP | `This text contains too much unsupported or mixed-language content. Use one main language: English, Russian, Romanian, or Chinese.` | Server eligibility page alert; no successful transformation or charge; preserve fields and wait for a corrected paused candidate. |
| UX-MSG-009 | Deferred | — | DF-006 prefix warning; original copy is historical at `54343c3`. |
| UX-MSG-010 | Deferred | — | DF-006 partial-result warning; original copy is historical at `54343c3`. |
| UX-MSG-011 | MVP | `Rewriting is limited to 2,000 characters. Shorten the text to continue.` | Essential page alert; no request or persistent counter; source remains editable. |
| UX-MSG-012 | MVP | `You’re offline. Keep editing. When you’re back online, edit your text or settings to process it.` | Connectivity alert; reconnect alone never submits automatically. |
| UX-MSG-013 | MVP | `We couldn’t process this text. Your text and previous result are safe.` | Current operation total failure; action `Try again`; persists until retry, relevant edit, or success. |
| UX-MSG-014 | MVP | `Processing took too long. Your text and previous result are safe.` | Deadline failure; action `Try again`. |
| UX-MSG-015 | MVP | `Your daily allowance is used up. Try again after {reset}. Your text is safe.` | User allowance page alert; reset rendered as absolute `00:00 UTC` plus localized relative time when server supplies it. |
| UX-MSG-016 | MVP | `LinguaDesk’s shared daily allowance is used up. Try again after {reset}. Your text is safe.` | Global allowance error; do not show global consumption. |
| UX-MSG-017 | MVP | `LinguaDesk processing is temporarily unavailable because its service budget has been reached. Your text is safe. Try again after service resumes.` | Monetary-cap error. If server supplies a reliable availability time, append `Expected after {time}.` |
| UX-MSG-018 | MVP | `Sign in to continue. Your text was not processed.` | Authentication failure before a workspace exists. Session expiry uses the message in Section 3.4 and clears text. |
| UX-MSG-019 | MVP | `Verify your email to use Translation and Rewriting.` | Unverified access page. |
| UX-MSG-020 | Retired | — | No secondary result status line; stale text remains suppressed. |
| UX-MSG-021 | MVP | `An earlier update completed but was not applied. {count} characters counted toward your usage.` | Reconciled standalone event alert, with the admission period when different from the current UTC day; no persistent usage summary. |
| UX-MSG-022 | Retired | — | No edited-result status line; result-edit protection remains required. |
| UX-MSG-023 | Retired | — | Sentence-specific comparison invalidation is cut; future assistance invalidates whole metadata. |
| UX-MSG-024 | Retired | — | Sentence-specific alternatives invalidation is cut; future assistance invalidates whole metadata. |
| UX-MSG-025 | Deferred | — | DF-001 alternatives failure; original copy is historical at `54343c3`. |
| UX-MSG-026 | Retired | — | No Copy result control or copy-status line; native selection/copy remains. |
| UX-MSG-027 | Retired | — | No custom clipboard operation or copy-failure line. |
| UX-MSG-028 | MVP | `Email or password is incorrect.` | Login form alert; focus email field; no account enumeration. |
| UX-MSG-029 | Deferred | — | DF-007 Google cancellation; original copy is historical at `54343c3`. |
| UX-MSG-030 | Deferred | — | DF-007 Google failure; original copy is historical at `54343c3`. |
| UX-MSG-031 | Deferred (DF-008) | `If an account exists for that email, we sent a reset link.` | Retained dormant password-request success copy; no current real-delivery gate. |
| UX-MSG-032 | Deferred (DF-008) | `This verification link is invalid or has expired.` | Retained dormant verification-page alert. |
| UX-MSG-033 | Deferred (DF-008) | `This reset link is invalid or has expired.` | Retained dormant reset-page alert. |
| UX-MSG-034 | MVP | `Account registration accepted. Sign in to continue.` | Registration success replaces the form; action `Go to sign in`; no email or verification route. |
| UX-MSG-035 | Deferred (DF-008) | `Verification email sent.` | Retained dormant resend status; no current real-delivery gate. |
| UX-MSG-036 | Deferred (DF-008) | `Password updated. Sign in with your new password.` | Retained dormant reset success. |
| UX-MSG-037 | MVP | `Your session expired. Sign in again to continue.` | Login page banner after forced teardown. |
| UX-MSG-038 | MVP | `Enter your email address.` | Required email field error. |
| UX-MSG-039 | MVP | `Enter a valid email address.` | Email-shape error; no request. |
| UX-MSG-040 | MVP | `Enter your password.` | Required password field error. |
| UX-MSG-041 | MVP | `Passwords do not match.` | Confirmation field error; focus confirmation. |
| UX-MSG-042 | MVP | `Password must meet all requirements.` | Password-policy failure; requirements themselves come from the authoritative security contract. |
| UX-MSG-043 | MVP | `We couldn’t create an account with these details. Try signing in or use a different email.` | Registration conflict/rejection; form remains populated except password fields. |
| UX-MSG-044 | Deferred (DF-008) | `We couldn’t send the email. Try again.` | Retained dormant delivery failure; do not state whether an account exists. |
| UX-MSG-045 | MVP | `Translation is limited to 5,000 characters. Shorten the text to continue.` | Essential page alert; no prefix submission, charge or persistent counter; retain complete source. |
| UX-MSG-046 | Retired | — | No eligible-idle status line or action button. |
| UX-MSG-047 | Retired | — | No outdated-result status line; revision protection remains required. |

### 6.4 Announcements

An assistive-only polite `role="status"` announces one processing start and one applied completion (Translation ready / Rewrite ready), plus reconciled stale success. Do not announce the timer, each keystroke, a character count, native copy or stale failure. Essential current errors use one accessible page alert without reading the entire result. Result `aria-busy` applies only during current submitted work; no visible processing/result status line is required. An error never disappears because an older response arrives.
