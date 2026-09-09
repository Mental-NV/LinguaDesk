# Original authoring inputs: 03-architecture

Historical provenance only; use current selected source hashes for execution.

| Input | Recorded revision | Role |
| --- | --- | --- |
| [SDD Planning Workflow](../00-SDD-Planning-Workflow.md) | v1.2 at `b5c01a1` | Process and generated contract governance |
| [Product Requirements Document](../01-PRD.md) | v0.4, 2026-09-08 provider-policy amendment | Active product baseline and canonical DF-001–DF-007 register |
| [UX/UI Specification](../02-ux-specification.md) | v1.3, aligned provider-policy references | Native controls, explicit processing, active/inactive scenario allocation |
| Architecture and ADR review baseline | Git commit `b5c01a1` | Architecture/ADRs v1.2 before provider-policy amendment |
| LLM specification authoring | User request, 2026-09-08; repository baseline `1646094` | Host-independent AI development/validation and `Microsoft.Extensions.AI`; current behavior in [document #4](../04-llm-specification.md) |
| AI infrastructure naming | User clarification, 2026-09-08 | Place the AI library in Infrastructure as `LinguaDesk.Infrastructure.Ai`; retain independent development/validation |
| API behavior and generation timing | User approval, 2026-09-08; baseline `24ffe3b`; #0 v1.3 | [API design #5](../05-api-design.md) owns shared semantics; generate OpenAPI early in selected implementation slices |
