# 005 — Shared Input and Capability Contract: Implementation Plan

**Version:** 1.1 · **Updated:** 2026-09-09
**State:** Implemented and verified
**Inputs:** [spec.md](spec.md) v1.1; [BI-005](../../docs/08-backlogs/M005-shared-input-capability-contract.md) v1.1

## 1. Current state, dependency and tooling

Planning baseline `a257cd1f8b0ff08147ad2770c7851e98184fa7a9` is clean and contains completed M001–M004 commits/artifacts. M001's current host dependency was rechecked on the selected baseline: `bash scripts/backend.sh check` passed 42 API/storage and 10 AI cases with a warning-free locked Release build; `bash scripts/frontend.sh check` passed 8 component cases, strict type/lint checks and the Vite production build. Tool versions matched `global.json`/scripts: .NET SDK `10.0.302`, Node `24.20.0`, npm `11.11.0`.

The API currently has only process liveness, lazy persistence service registration, static/fallback routing and explicit `/api` catch-alls. The solution has no Core project or contract tooling. Frontend TypeScript is strict with JSON-module support, but it has no `src/api`, shared input helper or generated type. There is no OpenAPI artifact. This is exactly the state expected before the first selected product API slice.

Pin `Microsoft.AspNetCore.OpenApi` and `Microsoft.Extensions.ApiDescription.Server` at **10.0.10**, matching the repository's existing .NET 10.0.10 package/runtime baseline instead of introducing an unrelated servicing update. Pin their `Microsoft.OpenApi` 2.x dependency at patched **2.7.5**: implementation preflight on 2026-09-09 found that the packages otherwise resolved vulnerable 2.0.0, and GHSA-v5pm-xwqc-g5wc identifies 2.7.5 as the patched 2.x release. Microsoft documents that the ASP.NET packages provide native OpenAPI support and build-time generation, that build-time generation runs the app entry point with a mock server, and that `--openapi-version OpenApi3_1` fixes the format. Use `openapi-typescript` **7.13.0** and `yaml` **2.9.0** as npm development dependencies; both support the pinned Node/TypeScript stack and local OpenAPI 3.1/YAML generation. Sources checked 2026-09-09: [ASP.NET Core generation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0), [Microsoft.AspNetCore.OpenApi 10.0.10](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi/10.0.10), [Microsoft.Extensions.ApiDescription.Server 10.0.10](https://www.nuget.org/packages/Microsoft.Extensions.ApiDescription.Server/10.0.10), [Microsoft.OpenApi security advisory](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc), [openapi-typescript 7.13.0](https://www.npmjs.com/package/openapi-typescript/v/7.13.0) and [yaml 2.9.0](https://www.npmjs.com/package/yaml/v/2.9.0).

Do not add a provider package, EF/Identity dependency to Core, OpenAPI UI/runtime document route, second schema source, `openapi-fetch`, auth library or API-versioning framework. Initial restore/npm lock updates are followed by locked/offline-capable checks and dependency audit; an incompatible/vulnerable selected graph is a blocker until explicitly resolved.

**Implementation preflight adjustment — 2026-09-09:** Current NuGet/npm advisory data made the originally recorded graph fail the required audit. Retain the selected OpenAPI generator pins, add the patched transitive `Microsoft.OpenApi` 2.7.5 pin, update the existing compatible frontend pins to `react-router-dom` 7.18.3, Vite 8.2.2 and Vitest 4.1.11, and override `js-yaml` to patched 4.3.2 for `openapi-typescript`'s Redocly dependency. These are audit-only prerequisite updates with locked regression coverage; they add no M005 behavior or compatibility promise.

**Published-artifact correction — 2026-09-09:** The first isolated publish smoke exposed that marking the direct `Microsoft.OpenApi` pin private omitted its runtime assembly while `AddOpenApi` registration still executes at application startup. Keep `Microsoft.Extensions.ApiDescription.Server` private because it is build tooling, but retain `Microsoft.OpenApi` as a normal direct API dependency. The corrected artifact contains the assembly and passes the real published-host smoke; no runtime OpenAPI document route is mapped.

## 2. Design and affected components

### 2.1 Pure policy and shared fixtures

Add `backend/src/LinguaDesk.Core/` and `backend/tests/LinguaDesk.Core.Tests/` as nonempty .NET 10 projects under the existing solution folders. Core has no package, ASP.NET Core, EF, Identity, Ai or frontend dependency. It owns immutable language/mode/direction/count catalogs, Unicode scalar decoding/counting, the fixed whitespace predicate and local input/selector validation. APIs accept immutable policy values explicitly; they do not read configuration, environment or a service locator.

Add `contracts/fixtures/unicode-scalar-v1.json` as the single cross-runtime test fixture. Valid cases store exact JSON text; malformed UTF-16 is represented as code-unit data so the fixture itself remains well-formed JSON; limit cases use a scalar plus repeat count rather than checking in thousands of arbitrary characters. The fixture includes expected scalar count, whitespace-only/Unicode validity and operation-boundary disposition. Core tests and `frontend/tests/unit/inputPolicy.test.ts` consume it directly.

Add `frontend/src/api/inputPolicy.ts` with no React or transport dependency. It validates UTF-16 before code-point iteration, returns canonical count/empty-or-whitespace/oversize information and takes the applicable advertised maximum. It contains the fixed algorithm/policy revision and choice validation but no duplicated 5000/2000 constants. No UI file imports it in M005.

### 2.2 Capability API

Add an API→Core project reference and a vertical `backend/src/LinguaDesk.Api/Features/Capabilities/` slice containing response records, mapping and endpoint registration. `Program.cs` registers `TimeProvider.System`, OpenAPI and the selected endpoint before the `/api` catch-all. First add the typed response and route metadata with an explicitly under-construction non-shipping handler, generate/review the shape, and only then connect the real synchronous read-only mapping. The completed handler obtains current UTC time, opens no persistence scope and has no Infrastructure.Ai reference. Milestone automation makes no intermediate commit, and no under-construction route is published.

Use explicit C# response records and endpoint metadata for JSON names/nullability, operation ID, response/header/content metadata, summaries/descriptions and examples. Return immutable/read-only collections in the specified order. Add `Cache-Control: no-store`. No runtime Swagger/OpenAPI endpoint is mapped. The selected endpoint group is the only group included in the `linguadesk` document, excluding health, static/fallback and future/unknown paths.

The fixed accepted values remain owned by #1/#5 and Core. The endpoint is the machine-readable discovery projection. Any future configuration of these product values must preserve one server source and update policy/fixtures/acceptance at the owning authority; M005 does not create a competing client default.

### 2.3 Generated contract and TypeScript types

Configure build-time generation only when the contract command enables it. Register one document named `linguadesk`, force OpenAPI 3.1 and transform `info` to the fixed title and `0.1.0-m005` artifact version. Emit intermediate JSON under ignored `artifacts/openapi/`; never write generated JSON into source directories.

Add `scripts/contract.sh` with strict `setup`, `generate` and `check` modes and a small `scripts/openapi-to-yaml.mjs`. The command verifies pinned tool versions, locked NuGet/npm state and expected inputs. `generate` builds the actual API entry point with build-time document generation, parses JSON, removes no semantic data, emits deterministic YAML 1.2 with fixed formatting, and runs the locally installed `openapi-typescript` binary to produce `frontend/src/api/generated/linguadesk-api.d.ts`. It atomically replaces only those two declared generated files after all stages succeed. Review these outputs against Section 3 of `spec.md` before replacing the under-construction handler with working behavior.

`check` generates both outputs in an owned temporary directory and byte-compares them with the committed artifacts, returning nonzero for missing artifacts, schema/type drift, malformed output, zero selected paths or an unexpected path. It cleans only its temporary/intermediate files. Generation never starts Kestrel/Vite, migrates or opens SQLite, resolves a provider/email client, reads credentials or connects to the network after dependencies are installed. Mark generated files as generated and exclude them only from lint rules that cannot apply; they must still type-check.

## 3. Verification and scenario coverage

| Scenario | Verification method and evidence target |
| --- | --- |
| AC-001 | `WebApplicationFactory` request with fake `TimeProvider`; exact status/content/cache/time, anonymous access and no storage file/provider/frontend effect |
| AC-002 | Core catalog units plus API serialization assertions for ordered languages/source values, 12 unique directions, modes/kinds/default and Chinese policy |
| AC-003 | API/Core assertions for limits, deadlines, counting metadata, UUID bounds/server time and absence of private/unselected fields |
| AC-004 | Core MSTest and Vitest execute `contracts/fixtures/unicode-scalar-v1.json`; explicit repeat/determinism and malformed-code-unit cases |
| AC-005 | Cross-runtime L−1/L/L+1 and selector/default cases; source preservation/excess values; project/reference inspection proves no provider or submission route |
| AC-006 | `scripts/contract.sh generate/check`, semantic schema assertions and TypeScript compilation; repeated bytes and a clean regeneration comparison |
| AC-007 | HTTP 405/404/content checks; deliberate generated-file drift/invalid command failure; before/after file/process inspection around generation |
| AC-008 | Positive test guards; backend/frontend check, backend smoke and published-shell smoke; complete diff/artifact/README/#5–#7 review |

This implements the selected pure portions of V-001 and capability/schema/type portions of V-009. Add positive minimum guards for the new Core/API/TypeScript tests without hard-coding a forever exact total. Existing API/AI and frontend suites remain separately observable so zero-test success cannot hide a missing project. API-AC-001's future HTTP submission/provider boundary and V-009's authenticated operation are not selected.

Requirements-quality review checks field/status/default clarity, complete active catalog, invalid/boundary cases, exclusions and proposal status. Contract review checks actual generated semantics rather than byte identity alone: only the selected path, required fields, enums/arrays/date-time, status/content/header metadata and descriptions. The final analysis compares backlog → AC → approach → tasks → checks with no orphan requirement or unselected production work.

## 4. Migration, rollout, operations and risks

**Data/migration:** None. Do not change `LinguaDeskDbContext`, migrations, database initialization or storage readiness. Contract generation must retain M003's lazy no-I/O startup boundary.

**Rollout/rollback:** This is an additive public read-only route and generated contract. Publish only after route behavior and artifacts agree. Rollback removes the route/Core reference and matching generated views together; no data rollback exists. The route is not a readiness/health endpoint and no transformation availability claim is made.

**Observability/privacy/security:** No request body, user text, account data or secret exists. Do not log response bodies or add telemetry fields. Public values are non-secret; provider/routing/global allowance/spend/configuration remain absent. `no-store` prevents stale server time. P-005 adds no abuse control to this read-only slice.

**Principal risks and mitigations:**

- C#/JavaScript Unicode APIs differ for isolated surrogates. Validate UTF-16 explicitly before scalar enumeration and use one structural fixture.
- Native OpenAPI build-time generation executes startup. Keep generation conditional and assert no listener/database/external effect with the real entry point.
- Generated output can reproducibly encode a wrong shape. Review semantic assertions against `spec.md` in addition to drift bytes.
- A public catalog could be mistaken for implemented language handlers. Generate only `/api/capabilities`, state the catalog's scope in descriptions, and keep future paths absent.
- Package/tool upgrades can change ordering/output. Pin exact versions and serializer options; review any future update as contract-tooling work.
- Adding unused client transport would create speculative code. Generate declarations now and defer `openapi-fetch` until a selected consumer.

## 5. Dependencies, human steps and final consistency

M001 is satisfied; all other milestone dependencies are deliberately excluded. Initial package access/audit is the only foreseeable setup dependency and uses routine existing authorization. **No human action is required at the beginning or end.** No credential, live service, certificate, database, browser/device or manual review is needed.

No blocking technical question remains. Q-001/Q-004/Q-005, later Q-006 auth/accounting wire details, Q-008 and Q-010 keep their existing owners/stages. The selected unversioned path and `info.version` are artifact mechanics, not acceptance of P-006. ADR-004 already owns the generation direction, so no new ADR is warranted.

Consistency review passed: every AC has a planned check and task; every affected file/component is justified by a selected scenario or generation prerequisite; no later endpoint, persistence, AI, auth, UI adoption or release evidence is included. The plan conforms to #0's ownership/readiness rules and #3's staged Core/API boundaries. No implementation blocker remains.
