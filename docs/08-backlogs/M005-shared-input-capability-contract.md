# M005 — Shared Input and Capability Contract Backlog

**Document:** #8 · **Version:** 1.0 · **Updated:** 2026-09-09
**State:** Selected; ready for implementation
**Roadmap:** [M005 — Shared input and capability contract](../07-roadmap.md#41-basic-infrastructure)

## 1. Outcome and authoritative inputs

An API consumer can retrieve LinguaDesk's current language, direction, rewriting-mode, whole-input-limit, counting, deadline and retry-identity choices without executing the SPA. The backend and TypeScript client use the same `unicode-scalar-v1` fixtures and locally reject malformed Unicode, empty/whitespace-only input, invalid selectors and L+1 whole input consistently. This is the first product API contract slice and therefore also establishes reproducible OpenAPI 3.1 and TypeScript type generation from the actual C# endpoint metadata.

Selection baseline: clean Git `a257cd1f8b0ff08147ad2770c7851e98184fa7a9`. Inputs read at that revision: [workflow #0](../00-SDD-Planning-Workflow.md) v1.7, [PRD #1](../01-PRD.md) v0.6, [UX #2](../02-ux-specification.md) v1.6, [architecture #3](../03-architecture.md) v1.10, [AI #4](../04-llm-specification.md) v1.5, [API #5](../05-api-design.md) v1.1, [verification #6](../06-verification-plan.md) v1.8, [roadmap #7](../07-roadmap.md) v1.8 and [ADRs #9](../09-architecture-decisions.md) v1.7. This selection updates #5–#7 for the package link and current status; it changes no product requirement, proposal disposition or shared architecture decision.

M001, the formal prerequisite, is done. Its [completion record](../../specs/001-backend-foundation/tasks.md#3-completion-record) identifies the pinned host, HTTP boundary and process-smoke evidence. Git history contains separate M001 planning/implementation commits, and the clean M005 preflight reran the current locked baseline: 42 API/storage plus 10 AI tests passed, as did 8 frontend component tests and the production frontend build. M002–M004 are current regression baselines but are not additional formal dependencies.

The inspected repository has the .NET 10 Minimal API host, API and AI tests, the published React shell, lazy storage registration and no `LinguaDesk.Core`, capabilities endpoint, product DTO, shared counting fixture, generated OpenAPI, generated TypeScript API types or contract command. The existing `/api` catch-all must be ordered after the selected route. No account, provider or language-operation handler exists.

## 2. Item

| Item ID | Outcome/title | Priority | Target milestone | Dependencies/blockers | State | Delivery package |
| --- | --- | --- | --- | --- | --- | --- |
| BI-005 | Publish the shared input/capability contract | Next / first product API contract gate | M005 | M001 done; no remaining blocker | selected | [005-shared-input-capability-contract](../../specs/005-shared-input-capability-contract/spec.md) |

### BI-005 — Publish the shared input/capability contract

**API/client value:** Add one anonymous, read-only `GET /api/capabilities` operation that returns the current supported language identifiers and names, automatic-source choice, all 12 translation directions, the nine exclusive rewriting modes/default, Simplified/Traditional Chinese input policy, Simplified Chinese output policy, operation-family source limits/deadlines, `unicode-scalar-v1` rules, UUIDv7 recovery bounds and current server time. The endpoint describes the accepted contract; the generated document contains only implemented selected API paths, so it does not imply that Translation, Rewriting, account or usage handlers already exist. Detailed behavior and exact selected wire fields are owned by [package AC-001–008](../../specs/005-shared-input-capability-contract/spec.md#4-selected-acceptance).

**Shared-input value:** Introduce the first nonempty pure Core project for fixed language/mode/count policy and Unicode-safe local validation. C# and TypeScript implementations consume one checked-in fixture format covering ASCII, composed/decomposed text, emoji clusters, Chinese, CRLF/LF, significant whitespace, the fixed whitespace set, malformed UTF-16, and L−1/L/L+1 for both operation limits. Client counts are advisory; later API handlers remain authoritative and must reuse the Core policy.

**Contract value:** Generate `docs/05-openapi.yaml` from actual C# DTOs/endpoint metadata and generate compile-checked TypeScript declarations from that artifact. Generation is deterministic, side-effect-free and drift-checked. `docs/05-api-design.md` remains the behavioral authority, C# remains the editable wire-shape source, and the YAML/types are generated views.

**Exclusions:** No Translation/Rewriting submission or eligibility result, provider call, AI reference/composition, authentication/account endpoint, usage/allowance/accounting, operation-status handler, persistence schema/migration, frontend page adoption, browser journey, live service, deployment, compatibility/deprecation guarantee, abuse-rate policy or API usage tutorial. `openapi-fetch` and its transport wrapper wait for a selected client that actually calls an operation; M005 generates types but adds no unused runtime fetch dependency. The HTTP no-provider rejection boundary and semantic language eligibility remain M015/later API slices.

**Edge and nonfunctional checks:** Preserve exact decoded source with no normalization; reject isolated surrogates rather than counting replacements; count CRLF as two scalars; apply the fixed whitespace set only to the all-whitespace decision; accept L and reject L+1 without truncation. The capabilities route needs no auth/database/provider/frontend startup, returns `Cache-Control: no-store` because it includes server time, and preserves API 404/SPA/static boundaries. Generation runs without listening, migration, storage creation, provider/email access or secrets.

## 3. Human steps and prerequisites

**None required for implementation or autonomous verification.** At the beginning, the executor checks the pinned .NET/Node/npm versions, locked NuGet/npm access, package audit and local shell/process permissions. Adding already-selected OpenAPI generation dependencies and deterministic serializer/type-generator packages is routine autonomous setup. If indispensable package access or an audit finding cannot be resolved within the selected pins, stop before dependent work and report it instead of weakening reproducibility.

No certificate, account, credential, email action, provider access, monetary approval, database content, browser/device review or design approval is needed. There is no planned end-of-milestone human verification. Unexpected human-only dependencies follow [#0's timing rule](../00-SDD-Planning-Workflow.md#selecting-work-and-creating-a-package): continue independent authorized work, prepare the exact action for handoff, and keep affected evidence pending; if no safe independent work remains, report the blocker.

## 4. Selection and readiness

BI-005 is the sole item selected in [package 005](../../specs/005-shared-input-capability-contract/spec.md). The product owners already fixed languages, directions, Chinese scripts/output, whole-input limits, rewriting choices and 30-second operation deadlines. API #5 already fixed `unicode-scalar-v1`, whitespace/no-normalization behavior, UUIDv7's 24-hour validity and five-minute future skew, public capability access and the generated-contract lifecycle. The package makes only delegated local wire/tooling choices and records them explicitly.

P-005/NFR-008 and P-006 remain proposed. An unversioned `/api` path and OpenAPI `info.version` artifact revision do not create a compatibility/deprecation promise. Q-001, Q-004 and the account-specific remainder of Q-006 do not affect this public no-effect slice. No unresolved product, behavior or technical question blocks implementation.

Eight observable scenarios cover public discovery, the complete accepted catalog, policy/recovery metadata, cross-runtime counting/validation, the first generated contract/types, route/no-effect boundaries and truthful closeout. Every scenario maps to ordered tasks and verification. Requirements/design review and cross-artifact analysis found no missing selected-scope decision or unsupported task; implementation evidence remains pending.
