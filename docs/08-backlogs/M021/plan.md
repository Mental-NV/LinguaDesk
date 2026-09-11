# M021 — Implementation plan
Behavior: [spec](spec.md). Reviewed sources: [manifest](context.json).

## Execution brief

Add the shared operation-admission slice to `LinguaDesk.Api`
without any provider dispatch: a `VerifiedAccount`-authorized
submission endpoint plus a metadata-only status read, backed by a
new EF-migrated operation/ledger store and a single-transaction
admission service. One `(account, operationId)` row owns the
logical operation; a server-keyed HMAC fingerprint over canonical
family/source/settings/policy-revision decides same-payload vs
409-conflict; the same transaction tests committed + reserved + new
count against the configured user/global UTC-day allowances and
stamps the admission day. Duplicates observe pending; failures
before commit leave no record. Nothing here settles, charges,
releases, dispatches, retries or recovers.

Selected canonical excerpts arrive through the context packet. Do
not copy them all here. Essential local invariants: resolve
existing identity state before fresh allowance admission (a replay
never becomes a quota error and never re-dispatches); expiry is
checked before first use and inside admission after waits using
server time; JSON spelling (order/escapes) normalizes but CRLF/LF,
family, selectors and mode discriminate; strict bodies reject
unknown/duplicate properties; global counts and monetary amounts
never leave the server; status/usage reads dispatch nothing.

## Changes and order

1. Persistence — new `OperationSubmission` + daily-ledger entities
   in `LinguaDeskDbContext` (unique `(account, operationId)`,
   fingerprint, family, scalar count, admission UTC day, deadline,
   state `pending`, snapshot revision counter), one reviewed EF
   migration after the M006/M007 chain; immutable applied
   migrations untouched. Depends on nothing; done when model-drift
   check passes and initialization uses the production chain.
2. Admission service — `OperationAdmissionService`
   (UUIDv7/validity check via `ProductCatalog` constants +
   `TimeProvider`, M005 `ScalarInputPolicy`/catalog validation with
   defaults applied pre-match, canonical fingerprint via
   Data-Protection-purposed HMAC, single-transaction claim +
   allowance test + day stamp, race-safe unique-violation re-read).
   Depends on step 1; done when policy unit tests prove
   match/conflict/expiry/allowance ordering without HTTP or DB.
3. Endpoints + generated contract — `POST /api/operations`
   (202 pending envelope + fresh user snapshot; 400/401/403/415/
   422/409/410/429 Problem Details per §8, `no-store`, 405 on wrong
   methods) and `GET /api/operations/{operationId}` (pending
   metadata or account-scoped 404); C# DTOs/metadata own the wire,
   OpenAPI regen + TypeScript declarations reviewed, drift fails.
   Depends on step 2; done when AC-001–AC-005 hold over real HTTP
   with zero dispatches recorded.
4. Concurrency/persistence evidence — file-backed SQLite tests:
   parallel identical claims (one reservation), parallel distinct
   claims against user/global ceilings (429s, no overrun),
   restart-preserved reservation + revision with post-restart
   duplicate still pending, status/usage reads with dispatch
   counter at zero. Depends on step 3; done when AC-004/AC-006
   hold on real files across two host processes.
5. Auth/privacy/contract evidence — verified/unverified/anonymous
   matrix (V-004 portion), sentinel sweep (source/result/secret
   absent from DB/logs/traces/reports), strict-JSON matrix,
   `backend.sh`/`contract.sh` regressions. Depends on steps 3–4;
   done when AC-007 holds with no text or secret in evidence.

No rollout/rollback beyond the standard single-instance migration
discipline (tested bundle, backup before migrate, no startup
mutation); no new external package expected. M022 later adds
terminal settlement transitions on the same row; it must not
reinterpret M021 fingerprints, days or wire names.

## Verification map

| AC / prerequisite | Task | Check / exact command or procedure | Evidence target |
| --- | --- | --- | --- |
| AC-001/AC-002/AC-003 | T002/T003 | V-005: policy units + real-HTTP duplicate/conflict/expiry cases under `bash scripts/backend.sh check` with a dispatch counter asserting zero | `artifacts/test-results/*.trx` + completion record |
| AC-004 | T003 | V-005: file-backed parallel admission cases (same-user + multi-user vs both ceilings, 429 category assertions, ledger-state assertions) | TRX + ledger dumps (numerics only) |
| AC-005 | T002 | V-005: pre-admission rejection matrix (401/403/400/415/422), assert no record/reservation/dispatch | TRX |
| AC-006 | T003 | V-005: restart test on one file (two host processes), post-restart duplicate still pending; read-path dispatch counter zero | TRX + restart log |
| AC-007 | T004 | V-009: `bash scripts/contract.sh check` (regen + drift + TS compile); V-015 sentinel sweep over DB/logs/traces/reports; V-004 auth matrix | drift result + sweep result |
| Regression | T005 | `bash scripts/backend.sh check`, `bash scripts/contract.sh check`, `python3 automation/context.py check M021` | task completion record |

## Context boundaries and risks

Omitted domains and why: language dispatch/eligibility/fallback
(M015–M018 pipelines stay uninvoked; serving in M026/M027);
settlement/charging/release (M022 owns terminal transitions —
M021 rows stay `pending`); monetary admission/months (M023);
interrupted/unknown recovery mapping (M024); cross-period
attribution/ordering (M025); UI/browser (no surface), email
(M034), corpus/eval (M035+), performance percentiles (M037/V-014).
Open on-demand: M022 package when settlement starts; DeepSeek/AI
docs only on adapter drift (none expected — no dispatch here).

Dependencies and risks: M003/M005/M007 are Done and consumed
unchanged (storage, counting/catalog/validity, verified accounts);
the single-writer SQLite assumption bounds concurrency proof —
state it, do not claim multi-instance fencing; unique-constraint
re-read is the race backstop, so the violation path needs its own
test; fingerprint-key purpose string must be pinned and reviewed
so deployments cannot rebind existing operations; reservations
without M022 settlement would accumulate — this slice is not
serving until M022 lands, record that limit.
