# 001 — Backend Foundation: Selected Specification

**Version:** 1.1 · **Updated:** 2026-09-08
**State:** Implemented and verified; AC-001–006 passed
**Milestone/item:** [M001 / BI-001](backlog.md#bi-001--start-and-verify-a-minimal-backend)

## 1. Selection and authoritative inputs

The user requested generation of the first milestone on 2026-09-08. Select the whole small BI-001 outcome: build/start the minimal backend and verify it without other application services. This is developer enablement for the independent API, not an account or translation feature. The newly authored backlog v1.0 and this package are created together against Git `2cd06b1333c710f9db36fddb47137658ce4f112b`.

Apply [#0](../../00-SDD-Planning-Workflow.md) v1.6, [#7](../../07-roadmap.md) v1.1, [#3](../../03-architecture.md) v1.8, [#5](../../05-api-design.md) v1.1 and [#6](../../06-verification-plan.md) v1.1 as aligned in this change. [PRD #1](../../01-PRD.md) v0.6 and [ADRs #9](../../09-architecture-decisions.md) v1.7 retain authority. Relevant sources are architecture Sections 2–3/5/8, API Section 2's absent-route boundary and ADR-005's test-host choice. UX and LLM feature acceptance is not selected.

## 2. Selected story and boundaries

**US-001 — Start from a dependable backend.** As the developer/AI agent implementing subsequent features, I can build, start and check a minimal host from repository instructions without first configuring the UI, database, account system or external services.

The host reports process liveness only. Its success does not report storage, email/provider connectivity, model qualification or product readiness. There is no sample business API or endpoint pretending to translate text. All unknown paths remain unimplemented.

In scope: backend build/test scaffolding, an operational liveness probe, baseline absent-route handling, isolated HTTP evidence, one real local process smoke, and operating instructions for exactly this scaffold. Out of scope: frontend/SPA fallback, Core/AI libraries without current behavior, SQLite/migrations, Identity, secrets/certificates, public product DTOs/OpenAPI/client generation, paid calls, CI-provider configuration and deployment. Local reproducible checks are required now; future hosted CI may invoke them.

## 3. Selected acceptance

These scenario IDs are local to this package. All belong to US-001/BI-001; task and evidence links are maintained in [tasks.md](tasks.md).

| ID | Given / when | Observable outcome |
| --- | --- | --- |
| AC-001 | A clean checkout has the pinned SDK and package-source/cache access; the documented backend setup and check commands run | Restore/build and the selected tests complete without Node, frontend assets, a database, credentials, Docker or a certificate. Failures return nonzero status; test discovery runs the intended assertions rather than reporting a zero-test success |
| AC-002 | The scaffold is running; a client sends `GET /health/live` | HTTP 200 with plain-text `Healthy`, no secret/version/configuration details, no dependency probes and no cookie; this means only that the host can serve the probe. A POST to this path returns 405 and never performs work |
| AC-003 | A client requests `/api`, an unknown `/api/...` path, or an unimplemented page/sample path | Unknown API paths return 404 with an API error, never HTML or a successful placeholder. Unknown non-API paths, including `/` and `/weatherforecast`, return 404; no SPA fallback or template business endpoint exists |
| AC-004 | Two isolated HTTP-host instances run checks and are disposed; application-service configuration is absent | Results are independent of instance order, fixed ports or shared application state. Startup/checks need no application datastore or external provider/email service and introduce no source/result persistence |
| AC-005 | The documented local smoke starts the actual backend on an OS-assigned loopback port, probes it and ends; or a probe/startup fails | A successful run establishes real process listening and the liveness response. Both success and failure paths terminate only the process/tree they own and clean only their temporary output; readiness polling and total smoke time are bounded, with nonzero failure status |
| AC-006 | Another developer follows the implemented instructions and reviews the milestone evidence | Exact prerequisites and executable setup/check/run/smoke commands describe this scaffold; recorded SDK/commit, actual test count/results and process-smoke outcome support AC-001–005. Later product/release capabilities remain explicitly pending |

Use the canonical [#6 layers and check groups](../../06-verification-plan.md#2-verification-layers-and-check-catalog): V-009's HTTP boundary and V-012's owned-host lifecycle portions only. This does not pass those entire groups, LLM-AC-001, independent authenticated API acceptance or any RG gate. Clean startup without application-service registrations is a bounded dependency fact, not production privacy/security certification.

## 4. Human steps, assumptions and clarifications

| Timing | Human action | Disposition / effect |
| --- | --- | --- |
| Beginning | None presently required | SDK and candidate package pins are locally available; the executor checks restore and local process access before coding |
| Beginning, only if environment changed | Provide access/install capability only when existing authorization and autonomous setup cannot resolve a required prerequisite | Batch the precise request then; affected execution waits. Missing tooling is not resolved by omitting its checks |
| End | None planned | No certificate, real email or human release review belongs to this scope; unforeseen human-only follow-up is handed off with prepared instructions and pending status |

Clarifications resolved through existing delegated design authority: the probe is process-only; local loopback HTTP needs no TLS provisioning for this no-account scaffold; real product HTTPS remains required later; product OpenAPI generation is not selected. No new privacy/provider policy, product requirement or compatibility promise is accepted here. Necessary human actions are handled under [#0](../../00-workflow/planning.md#select-and-gather-bounded-context), not requested in the middle as avoidable setup.

## 5. Readiness and completion

Behavioral scope was implemented without expansion: one story, six testable scenarios and the stated exclusions. The [plan](plan.md) records the technical approach and [tasks/evidence](tasks.md#3-completion-record) records the verified execution. AC-001–006 are **Passed** for the M001 enabling scope. Any relevant upstream change still requires impact assessment; later product and release capabilities remain pending.
