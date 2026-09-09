# UX processing and message contract

Authoritative continuation of [02-ux-specification.md](../02-ux-specification.md); section numbers refer to that document; read only when relevant to the selected scope.

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
