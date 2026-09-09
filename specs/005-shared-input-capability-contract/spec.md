# 005 — Shared Input and Capability Contract: Selected Specification

**Version:** 1.0 · **Updated:** 2026-09-09
**State:** Ready for implementation; evidence pending
**Milestone/item:** [M005 / BI-005](../../docs/08-backlogs/M005-shared-input-capability-contract.md)

## 1. Selection and authority

Select BI-005 only. The [backlog](../../docs/08-backlogs/M005-shared-input-capability-contract.md#1-outcome-and-authoritative-inputs) records the clean Git baseline, repository/Git inspection, dependency evidence and source versions. Apply workflow #0 v1.7, PRD #1 v0.6, UX #2 v1.6, architecture #3 v1.10, AI #4 v1.5, API #5 v1.2 as reconciled with this selection, verification #6 v1.9, roadmap #7 v1.9 and ADR-004.

M001 is complete and supplies the current host, `/api` fallback boundary, pinned SDK and reproducible check/smoke entry points. M002–M004 remain regression baselines. No live/provider/account/privacy-retention decision or unresolved PRD proposal is needed for this read-only, text-free contract slice.

## 2. Selected stories and scope

**US-001 (P1) — Discover accepted choices and limits.** As an API consumer, I can retrieve the currently contracted language/source/direction/mode/script, whole-input limit, deadline, count-policy and retry-identity metadata without executing React or authenticating.

**US-002 (P1) — Count and validate locally without changing the server contract.** As a client or backend feature developer, I can use the same versioned fixtures to obtain canonical Unicode scalar counts and locally detect malformed Unicode, empty/whitespace-only input, oversize whole input and invalid selected values. Later server handlers remain authoritative; no client-reported count is accepted.

**US-003 (P1) — Consume a generated contract.** As a TypeScript client developer, I can compile against declarations generated from a reviewed OpenAPI 3.1 artifact whose source is the implemented C# route/DTO metadata, and CI-style regeneration detects drift without starting live effects.

M005 introduces the nonempty `LinguaDesk.Core` policy boundary, its focused tests, shared fixture data, the selected public endpoint and generation/check commands. The API references Core; Infrastructure.Ai remains unchanged and does not acquire an unused Core/API reference. A TypeScript counting helper takes the server-advertised limit; the UI does not adopt it in this milestone.

## 3. Selected operation and clarification decisions

### 3.1 Public operation

`GET /api/capabilities` has operation ID `getCapabilities`. It accepts no route/query/body input and no credentials. Success is HTTP 200 with UTF-8 `application/json` and `Cache-Control: no-store`. `serverTimeUtc` is a UTC RFC 3339 `date-time` produced through `TimeProvider`. `POST` on the same path is not an alternative operation and returns 405; unknown `/api` paths retain the existing API 404 Problem Details behavior and never fall through to SPA HTML.

The response contains these exact camel-case fields; C# metadata/OpenAPI own their final JSON schema rather than a hand-maintained duplicate here:

- `serverTimeUtc`.
- `languages`: ordered `{ id, name }` entries for `en`/English, `ru`/Russian, `ro`/Romanian and `zh`/Chinese.
- `sourceSelection`: `default` = `auto`; `values` = `auto`, `en`, `ru`, `ro`, `zh`.
- `chineseScriptPolicy`: `acceptedInput` = `simplified`, `traditional`; `output` = `simplified`.
- `countingPolicy`: `id` = `unicode-scalar-v1`, `unit` = `unicodeScalar`, `normalization` = `none`, `lineEndings` = `preserve`, `invalidUnicode` = `reject`, `emptyOrWhitespace` = `reject`, and `whitespaceCodePointRanges` = `U+0009-U+000D`, `U+0020`, `U+0085`, `U+00A0`, `U+1680`, `U+2000-U+200A`, `U+2028-U+2029`, `U+202F`, `U+205F`, `U+3000`.
- `translation`: `maximumSourceCharacters` = 5000, `oversizeHandling` = `rejectWhole`, `overallDeadlineSeconds` = 30, `targetRequired` = true and `supportedDirections` entries `{ source, target }` in this order: `en→ru`, `en→ro`, `en→zh`, `ru→en`, `ru→ro`, `ru→zh`, `ro→en`, `ro→ru`, `ro→zh`, `zh→en`, `zh→ru`, `zh→ro`.
- `rewriting`: `maximumSourceCharacters` = 2000, `oversizeHandling` = `rejectWhole`, `overallDeadlineSeconds` = 30, `defaultMode` = `correctionOnly`, and ordered `modes` entries `{ id, name, kind }` from the table below.
- `operationIdentity`: `format` = `uuidV7`, `validForSeconds` = 86400 and `maximumFutureSkewSeconds` = 300.

| `id` | `name` | `kind` |
| --- | --- | --- |
| `correctionOnly` | Correction only | `correction` |
| `simple` | Simple | `style` |
| `casual` | Casual | `style` |
| `business` | Business | `style` |
| `academic` | Academic | `style` |
| `enthusiastic` | Enthusiastic | `tone` |
| `friendly` | Friendly | `tone` |
| `confident` | Confident | `tone` |
| `diplomatic` | Diplomatic | `tone` |

Every listed top-level field and nested member is required and non-null. Arrays retain the stated order and contain no extra value. The selected OpenAPI schema supplies their concrete array/object/string/integer/date-time types and descriptions from C# metadata.

The catalog reports accepted contract choices, not runtime service/account availability. M005's OpenAPI document exposes only the implemented capabilities path. The absence of Translation, Rewriting, account, usage and status paths is deliberate and prevents this slice from presenting those future handlers as working.

### 3.2 Local validation boundary

`unicode-scalar-v1` counts the exact decoded source as API #5 Section 4 requires. It performs no trim or Unicode/newline normalization; CRLF is two scalars, a surrogate pair is one scalar, combining sequences and emoji clusters retain their individual scalar count, and all scalars count when any non-whitespace content exists. Empty or entirely fixed-set whitespace is locally invalid. Isolated UTF-16 surrogates are invalid rather than replacement characters. A complete source at L is valid for the length boundary; L+1 is rejected without truncation. Translation also locally requires a target and rejects an explicit equal source/target; Rewriting accepts exactly one advertised mode and defaults an omitted mode to `correctionOnly`. Manual source selection never proves semantic eligibility.

The shared fixture is test/supporting evidence, not a runtime policy source and not arbitrary user text. It represents malformed UTF-16 structurally rather than embedding invalid JSON Unicode and compactly describes repeated-scalar limit cases. C# policy/catalog code is the server source; generated endpoint types expose values to clients, and TypeScript uses the server-advertised maximum instead of an independent limit constant.

### 3.3 Contract revision and proposal status

The canonical generated artifact is `docs/05-openapi.yaml`, OpenAPI 3.1, with `info.version` `0.1.0-m005`. The route remains under the existing unversioned `/api` boundary. The version labels the pre-release artifact/slice only; P-006 compatibility/versioning/deprecation remains proposed and is not accepted. M005 supplies semantic descriptions for its fields but no broad tutorial or stability guarantee.

The generated TypeScript declarations are a compile-time view only. A runtime `openapi-fetch` wrapper is deferred until a selected client actually calls an API operation, avoiding an unused transport dependency. This is a staging decision within ADR-004, not a replacement for it.

## 4. Selected acceptance

All IDs are local to this package.

| ID | Story | Given / when | Observable outcome |
| --- | --- | --- | --- |
| AC-001 | US-001 | An anonymous caller gets `/api/capabilities` without SPA execution | One request returns 200 JSON, no authentication challenge and `no-store`; fake time controls `serverTimeUtc`. No account, database, provider or frontend effect occurs |
| AC-002 | US-001 | The returned catalog is inspected | The four ordered languages, `auto` source default/values, all 12 distinct Translation pairs, nine exclusive Rewriting modes with Correction only default/kinds, both Chinese input scripts and Simplified output agree with the PRD and contain no deferred/retired choices |
| AC-003 | US-001 | A client reads the public policy/recovery metadata | Translation 5000/Rewriting 2000 with whole-input rejection, required Translation target, both 30-second deadlines, the exact count/whitespace/no-normalization/newline/invalid-Unicode policy, UUIDv7, 86400-second validity, 300-second future skew and current server time agree with #1/#5; no allowance, provider, routing, spend or secret data is exposed |
| AC-004 | US-002 | C# and TypeScript execute the same versioned scalar fixtures | Counts and validity agree for ASCII, precomposed/decomposed text, surrogate-pair emoji, multi-scalar emoji, Chinese, CRLF/LF, significant whitespace, every whitespace range and malformed UTF-16. Repeated runs are deterministic and no implementation substitutes byte/code-unit/grapheme counting or normalization |
| AC-005 | US-002 | Local validation runs at L−1/L/L+1 for both families and across selector cases | L−1/L are accepted at the local length boundary, L+1 is rejected whole with the correct count/excess, empty/all-whitespace/malformed input is rejected, target/same-language/mode/default behavior agrees across runtimes, and no prefix/provider/HTTP language operation exists or is claimed |
| AC-006 | US-003 | The contract command generates and repeats from the selected C# route/DTO metadata | `docs/05-openapi.yaml` is deterministic OpenAPI 3.1 with only the selected `/api/capabilities` GET, operation ID, required/enum/date-time shapes, status/content/header metadata, descriptions and truthful `0.1.0-m005` artifact version; TypeScript declarations regenerate and compile without hand edits |
| AC-007 | US-001/US-003 | Route and generation boundaries are challenged | POST capabilities returns 405; unknown `/api` still returns API 404 rather than HTML. Contract generation uses the real entry point with a mock server but creates/opens no database, starts no listener/frontend/provider/email call, reads no credential and leaves no secret/text report; deliberate drift/failure returns nonzero |
| AC-008 | all | The implementation is reviewed and closed | Focused Core/API/TypeScript/contract checks and applicable backend/frontend/published-shell regressions pass with nonzero test guards. README, #5–#7, backlog and tasks identify actual commands/revisions/evidence and preserve all later product/release work as pending |

## 5. Exclusions, verification boundaries and human steps

M005 does not implement an HTTP text-submission rejection, semantic language eligibility, transformation, charge, account, auth, usage, status or UI flow. V-001 applies to shared fixtures, pure local validation and cross-runtime agreement; its HTTP no-provider submission boundary remains pending. V-009 applies to public capability HTTP behavior and schema/type drift only; authenticated independent-client operations remain pending. API-AC-001 is covered only through its count/boundary fixture portion, and API-AC-014 only for this first selected slice. No language quality, accessibility, storage, privacy-lifecycle, cost or live evidence is claimed.

Run focused MSTest Core/API and Vitest policy tests, locked warning-free builds, deterministic generate/check commands, current backend/frontend checks, backend smoke and published-shell smoke because routing/build/package/solution surfaces change. Inspect process/filesystem effects around generation. Browser/device or manual accessibility evidence is not applicable because the rendered UI is unchanged.

**Human actions: none required at beginning or end.** The executor owns toolchain/package/audit preflight using existing authorization. No credential, certificate, account, email, provider budget, production storage or device review is requested. If an unexpected human-controlled dependency appears, use #0's batching/end-handoff rule and keep affected evidence pending rather than bypassing it.

## 6. Readiness and remaining blockers

Accepted upstream decisions provide every selected product value and shared behavior. Delegated local decisions above resolve the exact operation, fields, identifiers, visibility, artifact revision and generator staging without changing Q-010 or accepting P-005/P-006. Q-001, Q-004, Q-005 and the auth/accounting remainder of Q-006 block their later owning slices or release gates, not M005.

Ready-for-planning review passed: selected IDs/exclusions are explicit; all stories/scenarios are testable; the formal dependency is complete; nonfunctional/human boundaries are recorded; no behavior question is hidden. [plan.md](plan.md) is coherent and [tasks.md](tasks.md) maps every scenario and prerequisite. Requirements/design review and final consistency analysis have no blocking finding. Implementation evidence is pending.
