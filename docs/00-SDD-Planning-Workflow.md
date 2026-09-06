# LinguaDesk — SDD Planning Workflow

**Updated:** September 6, 2026  
**Baseline:** [docs/LinguaDesk-PRD-Review-Draft.md](https://github.com/Mental-NV/LinguaDesk/blob/HEAD/docs/LinguaDesk-PRD-Review-Draft.md)

## Starting point and authority

PRD creation is complete. Start with downstream design; use the repository PRD as the source of product requirements. This workflow defines document ownership, sequencing, and readiness only.

The fetched PRD still carries a review-draft label and distinguishes confirmed requirements, proposals, and deferred questions. Preserve those distinctions: completion of PRD authoring does not silently accept its marked proposals. Resolve only outstanding decisions needed by the next work item, recording explicit product decisions in the PRD.

Reference existing PRD requirement, question, proposal, and release-gate IDs. Do not copy its scope, behavior, defaults, limits, routing policies, quality targets, or acceptance gates into this workflow. Downstream documents add design and implementation detail; they cannot override the PRD. A product change must be recorded there before dependent specifications adopt it.

## The original ten documents

Keep one canonical file per row. Paths other than the existing PRD are proposed. Keep feature packages and ADRs as sections in their respective files; keep operating and portfolio material in the README. This workflow is the process guide for that set.

| # | Document and file | Necessary work beyond the PRD |
| --- | --- | --- |
| 1 | **Product Requirements Document** — `docs/01-PRD.md` | Existing baseline. Maintain only accepted product changes and proposal dispositions, including Q-010. Link deferred questions to their eventual resolutions. |
| 2 | **UX/UI specification** — `docs/02-ux-specification.md` | Specify layouts, wireframes, component details, interaction state transitions, messages, browser coverage, and accessibility checks. Resolve Q-002 and Q-009 with the feature/API design; address the workspace interaction portion of Q-004. Link relevant proposal decisions before relying on them. |
| 3 | **Architecture and engineering principles** — `docs/03-architecture.md` | Choose technology, component boundaries, deployment, identity integration, data lifecycle, concurrency/accounting mechanisms, configuration, secrets, observability, and cost enforcement. Own technical design for Q-001, Q-004, and Q-008; coordinate Q-003 and Q-006 with the API contract. Keep privacy, security, and cost sections here. |
| 4 | **LLM behavior and routing specification** — `docs/04-llm-specification.md` | Define prompts and versions, provider adapters/settings, configuration schema and validation, routing algorithm, output checks, error classification, context bounds, and attempt budgets. Resolve Q-007; document provider/model eligibility evidence for Q-001 with architecture and evaluation. Implement the PRD's policies by reference. |
| 5 | **API contract** — `docs/05-openapi.yaml` | Define operations, schemas, authentication, identifiers and their lifecycle, errors, counting rules, usage fields, retry/cancellation semantics, and compatibility decisions. Own Q-003 and Q-006 with architecture and feature design. Keep client-facing explanations and examples in OpenAPI descriptions. |
| 6 | **Verification and LLM evaluation plan** — `docs/06-verification-plan.md` | Define test methods, corpus construction, rubric, human review procedure, workload, and evidence capture. Resolve Q-005. Map PRD requirements and release-gate IDs to checks and results; reference existing thresholds. Keep coverage and release evidence here. |
| 7 | **Roadmap and milestone plan** — `docs/07-roadmap.md` | Order runnable milestones by dependency; list outcomes, feature references, applicable checks, and blockers. Keep current milestone and planning status here. Detail near-term work only. |
| 8 | **Per-feature specifications, implementation plans, and tasks** — `docs/08-features.md` | Add one section per feature: upstream references; unresolved details and concrete acceptance scenarios; affected components and implementation approach; ordered tasks; verification evidence. Resolve feature-specific portions of Q-002–Q-004 before dependent implementation. |
| 9 | **Architecture decision records** — `docs/09-architecture-decisions.md` | Add short entries for consequential technical choices: context, alternatives, decision, consequences, status, and links. Record rationale here; keep the current design in its owning specification. |
| 10 | **README, operating guide, and portfolio walkthrough** — `README.md` | Document working setup, configuration, deployment/rollback, diagnostics, recovery, and a guided demo. Link architecture, API, evaluation results, and actual limitations. Maintain operational procedures for the resolved Q-004/Q-008 decisions here. |

Keep diagrams, decision logs, and checklists inside their owning document. Test fixtures, executable configuration, and generated results may accompany the code; they do not require additional planning documents.

## Planning sequence

1. **Design the next slice: documents 2–6.** Begin with its UX flow and architecture boundaries, then refine LLM design, API contract, and verification together. Read the relevant PRD sections first. Resolve blocking deferred questions in the owning document; use a focused technical experiment only where evidence is needed. Confirm provider eligibility before depending on a serving arrangement.
2. **Order delivery: document 7.** Outline milestones after the initial design is coherent. Put required access, accounting, privacy, and cost controls before any user-facing LLM slice that depends on them. Keep broader release work at outcome level until it approaches implementation.
3. **Prepare the next feature: document 8.** Follow specification → clarification → implementation plan → tasks. Reference upstream behavior and add only the details needed to build and test the feature. Record consequential technical choices in document 9 as they arise.
4. **Implement and verify.** Build the planned slice, check the API implementation against its contract, and run the applicable checks from document 6. Record evidence and update affected specifications in the same change. Maintain document 10 as setup and operations become real.
5. **Repeat and release.** Prepare the next feature using what was learned. Before launch, use document 6 to collect evidence against the PRD's release-gate IDs and confirm closure of its pre-launch questions. Finish the operating and portfolio sections of document 10 using verified behavior.

Documents 2–6 need sufficient detail for the next feature, not exhaustive coverage of future implementation. ADRs and the README evolve throughout delivery.

## Clarification and readiness

For each planning conversation, supply the current PRD and relevant downstream documents. Ask at most 1–3 related questions at a time, only about unresolved choices that affect the work. Offer meaningful alternatives and a recommendation; invite a design proposal for subjective UX choices. Keep assumptions explicit, record accepted decisions in their owning document, and produce a reviewable draft once blocking questions are resolved.

A feature is **ready** when its relevant product decisions, UX states, API contract, technical approach, and verification methods are coherent; blocking questions are resolved; and tasks are ordered. Existing approvals or delegated decisions stand. Unrelated future questions do not block it.

A feature is **done** when its applicable acceptance checks pass, evidence is linked, and affected specifications and operating instructions match the implementation. Feature completion does not imply that all product release gates have passed.

Maintain one coverage table in document 6: **PRD ID → design/API/feature reference → check → evidence/status**. Maintain one progress summary in document 7. When a change exposes a contradiction, resolve it in the authoritative document first, then update affected designs, tasks, and checks. Do not create another requirements list or parallel status document.
