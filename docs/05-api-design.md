# LinguaDesk — API Behavioral Design

**Document:** #5 · **Version:** 1.5 · **Status:** Current design; implementation/evidence status is maintained in delivery/current.md and verification/coverage.md
**Updated:** 2026-09-09

## 1. Authority, sources, and artifact lifecycle

Original authoring inputs are in the [archive](archive/05-api-design-inputs.md). Current scope follows the owning documents linked from [#0](00-SDD-Planning-Workflow.md).

This document owns shared observable API behavior under Q-003/Q-006, coordinated with #3 for identity, accounting and privacy. The technical choices below use #0's delegated design authority. They do not accept P-005/NFR-008, P-006, a new product feature or a provider privacy requirement. Product values remain canonical in PRD Sections 5–8; configuration exposes those values rather than creating independent defaults here.

M001–M004 established the backend host, published shell, durable-storage boundary and independent AI boundary. [M005 / package 005](08-backlogs/M005/spec.md) has now implemented and verified the first product API slice: its exact public capability operation/fields, shared scalar fixtures and slice-local artifact mechanics are represented by actual C# metadata, generated [`docs/05-openapi.yaml`](05-openapi.yaml) and generated TypeScript declarations. [Verification plan #6](06-verification-plan.md) supplies shared verification methods and coverage. The YAML remains a generated artifact, not a hand-edited source. This document supplies cross-operation rules and behavioral examples, not endpoint signatures or a complete DTO catalog. #6 owns the requirement-to-test/evidence matrix. M005 evidence does not make any language-operation, auth, usage/status, accounting or release behavior complete.

For each selected API slice:

1. Its delivery `spec.md` identifies operations, inputs, outputs, applicable shared decisions and acceptance scenarios. Resolve slice-blocking questions before handlers are written.
2. At the start of implementation, add actual typed C# contracts and endpoint metadata/descriptions; generate OpenAPI and review it against the design and selected specification. Do not create a separate fake contract host or copy of the DTOs.
3. Generate dependent client types from that reviewed artifact. Implement handlers against the reviewed shape, then test behavior and schema/client drift independently.
4. Commit the generated artifacts with the slice. Keep unfinished handler status explicit and prevent contract scaffolding from shipping as working functionality. Add other MVP operations when selected; document numbering does not require an all-MVP schema first.

Document #0 owns this lifecycle. C# owns editable wire structure; generated OpenAPI is the machine-readable contract view. A generated change cannot redefine a shared decision silently. Paths, methods, operation IDs, JSON names/nullability, exact response/header schemas and client-facing examples are fixed in the selected slice and then represented in C# metadata/OpenAPI. Markdown retains shared semantics and scenario references, avoiding a second schema to maintain.

## 2. Capability and transport boundaries

| Capability | Required observable behavior | Principal dependencies |
| --- | --- | --- |
| Local accounts | Register, sign in/out, confirm email, resend confirmation, recover/reset password and read current account/verification status | FR-001/002; UX #2 Section 9; identity design in Section 3 |
| Translation | Submit one complete source and an explicit target; automatic/manual source choice; return complete validated target text | FR-003–011; #4 eligibility/output contracts |
| Rewriting | Submit complete source and exactly one current PRD writing mode; default to Correction only when omitted | FR-012–018; no combined translation-and-rewrite operation |
| Capabilities | Read supported languages/modes, limits, counting policy, operation deadline and retry-identity validity | FR-036; accessible without SPA execution |
| Usage/availability | Read authenticated user usage and shared-service availability, including pending reservations | FR-027/028/036; Section 7 |
| Operation status | Read the outcome of a known submission identity with ownership checks; no provider call or saved-text retrieval | FR-026/035/037; Sections 5–6 |

Capabilities may be public because they contain no account or operational secrets. Account-specific usage and operation status require an authenticated, currently valid account. A verified account is required for paid language admission. Reading account/status information needed to recover or verify access is not itself a paid transformation.

Use same-origin HTTPS under the architecture's `/api` boundary, JSON requests/responses and UTF-8. Text-bearing inputs use request bodies, never URLs. Return one complete result from the original transformation request; no streaming text, background result queue or provisional output becomes editable user output. Normal submission waits for bounded processing and durable settlement. Status reads are a separate recovery capability, not an async job system with persisted text.

Do not expose Google, alternatives, sentence/version identities, comparison, automatic submission, prefix translation, routing administration or file/voice processing. Do not expose all framework-provided account endpoints merely because a helper maps them; select the confirmed account surface. The independent client uses the same validation, identity and allowance rules as the SPA. Backend endpoints never depend on React revisions or client-reported charges.

Unknown API paths return an API 404, never SPA HTML. Requests with incompatible content types, malformed JSON or invalid text encoding fail before provider work. Request-size bounds must accommodate every legal source, JSON escaping and required metadata; the selected transport slice defines and tests finite body/header limits without creating a smaller hidden text limit.

## 3. Authentication, verification, and account lifecycle

See [API account behavior](api/accounts.md#3-authentication-verification-and-account-lifecycle).

### 3.1 Cookie and bearer access

See [API account behavior](api/accounts.md#31-cookie-and-bearer-access).

### 3.2 Expiry, refresh, and revocation

See [API account behavior](api/accounts.md#32-expiry-refresh-and-revocation).

### 3.3 Verification and recovery

See [API account behavior](api/accounts.md#33-verification-and-recovery).

### 3.4 M006 registration contract

See [API account behavior](api/accounts.md#34-m006-registration-contract).

## 4. Complete input and canonical counting

See [API input, operations, accounting and recovery](api/operations.md#4-complete-input-and-canonical-counting).

### 4.1 Counting decision: `unicode-scalar-v1`

See [API input, operations, accounting and recovery](api/operations.md#41-counting-decision-unicode-scalar-v1).

### 4.2 Validation and settings

See [API input, operations, accounting and recovery](api/operations.md#42-validation-and-settings).

## 5. Submission identity, duplicates, and replay

See [API input, operations, accounting and recovery](api/operations.md#5-submission-identity-duplicates-and-replay).

### 5.1 Identity and bounded validity

See [API input, operations, accounting and recovery](api/operations.md#51-identity-and-bounded-validity).

### 5.2 Payload matching and atomic admission

See [API input, operations, accounting and recovery](api/operations.md#52-payload-matching-and-atomic-admission).

## 6. Completion, cancellation, and lost-response recovery

See [API input, operations, accounting and recovery](api/operations.md#6-completion-cancellation-and-lost-response-recovery).

### 6.1 Outcomes and finality

See [API input, operations, accounting and recovery](api/operations.md#61-outcomes-and-finality).

### 6.2 Deadlines and cancellation

See [API input, operations, accounting and recovery](api/operations.md#62-deadlines-and-cancellation).

## 7. Allowances, UTC boundaries, and usage ordering

See [API input, operations, accounting and recovery](api/operations.md#7-allowances-utc-boundaries-and-usage-ordering).

### 7.1 Daily character ledger

See [API input, operations, accounting and recovery](api/operations.md#71-daily-character-ledger).

### 7.2 Monetary admission and month boundaries

See [API input, operations, accounting and recovery](api/operations.md#72-monetary-admission-and-month-boundaries).

### 7.3 Usage snapshots

See [API input, operations, accounting and recovery](api/operations.md#73-usage-snapshots).

## 8. Error semantics and client recovery

See [API input, operations, accounting and recovery](api/operations.md#8-error-semantics-and-client-recovery).

## 9. Privacy, compatibility, and contract generation

Source/result exist only during bounded processing and original delivery. All text-bearing and auth/status/usage responses use `Cache-Control: no-store`; exclude them from server response/output-cache middleware and service-worker caches. Production logs/traces are metadata-only. Store no text for operation replay, exceptions or generated examples. Provider-managed cache behavior from #4 does not change these application rules.

Client workspace revisions and operation identities may live in active memory, with no text restoration after the teardown boundaries in #2. UUID timestamps reveal approximate submission timing, so identifiers are operational metadata and still require ownership checks. Server-keyed payload fingerprints are private bounded matching data, not analytics identifiers. Public status access is by a known account-scoped operation identity, not a history browser.

Retain automatic OpenAPI generation per ADR-004. During the selected implementation slice, pin OpenAPI 3.1, native ASP.NET Core generation, deterministic YAML serialization and client generation as #3 specifies. Generation must run without migrations, live database/provider/email access, production secrets or frontend startup. ASP.NET Core's build-time generator invokes the application entry point with a mock server; isolate startup effects while preserving the real route/contract registrations. [ASP.NET Core generation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0)

Generation parity proves reproducibility of the artifact, not correctness of handlers. Review semantic schema diffs and test documented validation, status codes, ownership, auth/antiforgery, accounting and recovery. A test that merely reproduces current handler behavior does not discharge this design. Unexpected code-plus-schema changes must be compared with the selected acceptance and shared rules, even when generation is clean.

P-006 remains proposed: this document does not introduce published-version compatibility, deprecation periods or external client stability guarantees. Schema `info.version`, OpenAPI format version and a URL API-version prefix are separate decisions. Use a truthful artifact revision when generation begins; the selected contract slice fixes naming/version mechanics without assuming a product support promise. Revisit schema-first server generation only if future independently developed consumers or external contract obligations justify changing ADR-004.

## 10. Acceptance scenarios and readiness

These shared scenarios guide selected packages; [#6 Section 8](verification/coverage.md#8-canonical-coverage-and-acceptance-allocation) owns the canonical coverage matrix and check allocation. They are not an all-MVP implementation task list. Only the M005 portions of API-AC-001 and API-AC-014 have passing runtime evidence; all other portions remain pending until selected.

| ID | Observable outcome | Upstream |
| --- | --- | --- |
| API-AC-001 | The scalar fixtures and L−1/L/L+1 agree in C#/TypeScript; invalid Unicode and oversize input never reach a provider | FR-007/024 |
| API-AC-002 | Both auth modes access the same API; invalid bearer cannot fall back to cookie; cookie mutations require antiforgery | FR-001/036, #3 |
| API-AC-003 | Unverified account can complete its verification journey but cannot invoke an LLM; reset/revocation invalidates subsequent protected access/refresh | FR-002, Q-004/Q-006 |
| API-AC-004 | Concurrent identical identity/payload claims dispatch one logical operation; changed payload conflicts; no extra charge | FR-026/027 |
| API-AC-005 | Equivalent JSON escapes match; changed newline/mode does not; expired UUID cannot re-dispatch after recovery metadata cleanup | FR-026/035, Q-003 |
| API-AC-006 | Success lost in transport is recovered as success/output unavailable with original charge; no result storage/regeneration | FR-026/028/037, NFR-004 |
| API-AC-007 | Pre-commit crash is fenced as interrupted with zero character charge; unknown/missing status never falsely asserts zero | NFR-003, RG-004 |
| API-AC-008 | Disconnect/aborted fetch may still settle success; late output cannot resurrect terminal failure; status reads never call the model | FR-026, NFR-002/003 |
| API-AC-009 | Cross-midnight success charges the original day; current-day usage and stale-charge disclosure remain correct | FR-024/027/028 |
| API-AC-010 | Cross-month fallback has its own monetary period; unresolved old exposure survives rollover; no double counting on settlement | NFR-006 |
| API-AC-011 | Snapshot ordering rejects older same-day/global availability states, accepts a new UTC day and survives restart; current usage failure does not erase success | FR-028, NFR-003 |
| API-AC-012 | Registration duplicates and known/unknown reset/resend acknowledgments reveal no account existence through intentional status/body/header distinctions; no error/log contains source, result or secret | UX #2, NFR-004 |
| API-AC-013 | Two independent-client operations work without SPA execution; one complete result, correct mode/language, usage and classified errors | FR-003/035–037, RG-005 |
| API-AC-014 | Generated schema/client match actual contract metadata with no live effects; review/tests catch behavior drift despite clean generation | ADR-004, #0 |

| Owner / question | Resolved here | Remaining decision and blocking stage |
| --- | --- | --- |
| Q-003, #5/#3 | Scalar/whitespace/newline count; identity matching/expiry; interrupted outcomes; daily/monthly attribution and ordered usage. M005 fixes the public count-policy/retry-bound fields and cross-runtime fixtures for its capability slice | Language-operation/accounting wire fields, fingerprint/key cleanup implementation and concurrency evidence before their selected slice completion |
| Q-006, #5/#3 | Cookie plus Identity opaque bearer; auth precedence/lifecycle; recovery/error semantics; no public cancellation endpoint. M005 fixes capabilities/artifacts; M006 fixes registration wire, email/password policy, generic duplicate/delivery intent and unverified-state guard | Confirmation/resend/status/sign-in/token/antiforgery/usage/language/status operations and any compatibility/versioning policy before their selected handlers/clients; P-006 remains proposed |
| Q-004, #3/account design/#10 | Bounded operation recovery/stamp behavior; M006 creates durable account/key records without deletion/retention claims | Account deletion, backup/aggregate/unresolved-exposure retention and any further logout guarantees before those related features/launch; not a blocker to M006 creation |
| Q-001, #4/#3 | Cost-month attribution and conservative unresolved carryover | Serving/billing bounds, attribution verification and actual cap before paid serving |
| Q-005, #6 | Local acceptance scenarios retained here; [#6](06-verification-plan.md) specifies coverage and workloads | Executable release evidence |
| Q-008/Q-010, PRD then #3/#10 | Proposal status preserved | No new abuse rate or compatibility obligation until its owning proposal is accepted |

The shared design and completed M005 package now provide the contract-generation baseline for later selected API slices. Unrelated wire/auth/accounting questions remain explicit and block only their affected handlers/clients. Shared rules continue to preserve whole-source processing, success-only charging, no saved text, simple LLM chains and the PRD's deferred/proposed distinctions; M005 did not implement those later behaviors.
