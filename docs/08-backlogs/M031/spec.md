# M031 — Selected specification
Selected items: BI-031. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: visible recovery on the real `/translate` and `/rewrite`
workspaces built by M028/M029 and hardened by M030, through the
published SPA → auth → API → accounting → deterministic-provider path.
Both pages distinguish three recovery kinds with the normative §6.2/§6.3
copy: definitive failure (UX-MSG-013/014, `Try again` submits current
fields as a new operation with a new key, zero charge for the failed
outcome per FR-026); unavailable usage after durable success
(UX-AC-071: result survives, `Usage update unavailable` plus a
read-only `Refresh usage` action); unknown outcome after transport
interruption (§6.1/§6.2: bounded spinner, then `We couldn’t confirm
whether this request completed. Your text is safe. Check its status
before trying again.`, with a read-only `Check status` action using
the original operation identity that never dispatches provider work).
Check-status resolution follows API §6.1: `succeeded` with unavailable
output discloses the confirmed charge and requires an explicit new
operation for regeneration; `failed`/`interrupted` report zero charge
and permit retry; `pending` keeps the unknown state; no record (404)
or expired window (410) reports that nothing is known and permits an
explicit new submission only after that terminal guidance is shown.
Retry-as-replay is forbidden: no new paid operation may masquerade as
replay of the old key. A failed usage refresh never converts success
into failure and never submits text. Requirements FR-026/028/034/037;
UX §6; UX-AC-036/068/069/070/071/072/073/074/075/076; message IDs
UX-MSG-003/004/005/013/014/015/016/017/020/021 plus the §6.2
unknown-outcome copy (others only as already rendered by the shell);
fixtures/identity from #6 §4.1 and §4.5 E2E contract. Deterministic
fake provider only.

Explicit exclusions: reset, session teardown and restoration clearing
(M032); translation/rewriting happy-path behavior itself (M028/M029
own it; regression-covered here); competing-event hardening itself
(M030 owns it; regression-covered here); live provider dispatch,
quality, performance and serving/billing attribution (Q-001 remainder,
Q-005); monetary-cap amount (unset); account deletion/backup
lifecycle (Q-004); abuse limits (Q-008/P-005); compatibility promises
(P-006); email delivery (M034); full browser/device/AT claim
(M038/M039, G3). No backend contract change is selected: submit,
status-read (`getLanguageOperationStatus`) and usage shapes are reused
from the reviewed M026/M027 wire, and any unexpected handler need
reopens the contract-review gate before client adoption.

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | On each page, a definitive processing failure preserves source/result, shows UX-MSG-013 with `Try again`, adds zero usage; explicit `Try again` captures current fields once with a new operation key (tracked request bodies differ) | FR-026/034/037; UX §6.1; UX-AC-036/072; UX-MSG-013 |
| AC-002 | On each page, a deadline failure shows UX-MSG-014 with `Try again` under the same new-key/zero-charge rules; allowance/budget states keep UX-MSG-015/016/017 with no retry affordance and no auto-submit on reset/refresh | FR-028/037; UX-AC-068/069/070/073; UX-MSG-014/015/016/017 |
| AC-003 | On each page, a transport interruption (offline submit) stops the spinner at the bounded deadline, preserves all work, shows the §6.2 unknown-outcome copy with `Check status`, and asserts neither success nor zero charge | UX §6.1/§6.2; UX-AC-075; FR-026 |
| AC-004 | `Check status` reads `GET /api/operations/{originalId}` only: `succeeded`/output-unavailable discloses the confirmed charge and requires explicit new submission for regeneration (never reconstructs output); `failed`/`interrupted` report zero charge and enable `Try again`; `pending` keeps the unknown state; 404/410 report that nothing is known and only then permit a new explicit submission | API §6.1/§8; UX §6.2; FR-026 |
| AC-005 | After durable success with a failed usage read, the result stands with `Usage update unavailable` and a `Refresh usage` action; activating it sends a usage read only (zero operation posts) and replaces the counter on success | UX-AC-071; FR-028 |
| AC-006 | Across all recovery actions, request counts prove no unintended language work: Check status / Refresh usage issue zero `POST /api/operations`; an edit after failure clears the alert and reenables explicit submission; a stale failure never replaces newer workspace state nor triggers retry | UX §6.1; UX-AC-076 |
| AC-007 | At least one published E2E case signs in through the real `/login` form; other cases reuse the documented real-auth fixture (same predefined account, same-origin cookie state; no forged sessions, test login routes, disabled middleware or intercepted `/api`); every case asserts its browser-visible outcome through semantic locators; seeded Smoke-only account/database, run-owned and removed afterward; password value absent from all artifacts | #6 §4.5; V-012; fixtures §4.1 |
| AC-008 | No source/result text, credentials, global counts, monetary values or provider internals in problems, logs, traces or TEXT columns; inline display shows only the user's own usage/remaining/reset | NFR-004; UX-AC-065; V-015 |
| AC-009 | Translate submit/status/usage and rewrite submit/status/usage flows reuse the reviewed M026/M027 wire shapes via regenerated client types with zero OpenAPI/TypeScript drift | V-009; arch §8.3 |

## Constraints and decisions

- The backend status read already exists (`GET /api/operations/{id}`
  returning `OperationStatusResponse` with `OutputAvailable=false` plus
  a fresh usage snapshot; M024). This slice adds only the frontend
  status client and the workspace/page recovery states — no handler,
  DTO, or OpenAPI change.
- One unsettled operation per page (M030 invariant unchanged). The
  unknown-outcome state retains the original operation ID for
  Check-status; `Try again` from any failure state always mints a new
  key via the existing `createOperationId`.
- Offline simulation in E2E uses browser-level `context.setOffline`,
  not `/api` interception or stubbed responses; the unknown-outcome
  submit genuinely never reaches the server, so Check-status
  deterministically observes the no-record path. `succeeded`/
  `failed`/`interrupted`/`pending` status mappings are covered by
  focused client/reducer units against the real generated shapes.
- Deterministic provider text proves transport/presentation only;
  language quality is not inferred (V-013 out of scope).
- Clarifications: Q-003/Q-006 shared design already specified and
  adopted via M026/M027/M013; Q-001 (live serving/cap amount), Q-004
  (lifecycle), Q-005 (qualification), Q-008/Q-010 (safeguards/adoption)
  remain pending and do not block this deterministic slice. No new open
  question is raised.
- Omitted domains: reset/teardown (no clearing behavior selected),
  backend accounting/recovery internals (reused, regression-covered by
  M021–M025 suites), email (no delivery in slice), visual baselines
  (curated set unchanged; M038+ owns web visual claim), UX §9
  account/availability flows (no account-state change selected; only
  operation-recovery copy from §6 is used).
