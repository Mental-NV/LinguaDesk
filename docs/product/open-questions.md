# Open product and design questions

#1 product authority. Mutable question dispositions; consult selected Q-IDs during planning or when requirements change.

### Open questions and their scope

| ID | Open decision | Resolution phase / dependency |
| --- | --- | --- |
| Q-001 | Hosting/provider arrangement, model/fallback settings meeting quality/performance/cost criteria, provider-managed caching capabilities/pricing and monetary cap amount. Provider retention/no-training eligibility checks are removed by D-18 | Model/provider evaluation and cost specification, before launch |
| Q-002 | **Resolved for MVP by D-17.** Targeted sentence-correspondence and toggle restoration are cut. No sentence metadata exists in MVP. Future assistance uses whole-result metadata invalidation on manual edits. | No MVP blocker. Future sentence/alternative association design belongs to DF-001; change review belongs to DF-002. |
| Q-003 | **Shared design specified:** [API design Sections 4–7](../api/operations.md#4-complete-input-and-canonical-counting) resolve counting, retry identity, interruption/cancellation, period assignment and usage ordering. Exact wire fields, persistence/concurrency implementation and evidence remain | Selected API/accounting slices; shared behavior precedes handlers and generated wire review precedes client adoption |
| Q-004 | Confirm per-tab memory teardown, metadata/backup retention, local-account deletion/revocation and provider disclosure. External-account linking is deferred with Google (DF-007). | Privacy/security and local-account specifications before launch; UX fixes observable workspace lifetime. |
| Q-005 | Design specified in [verification plan #6](../06-verification-plan.md): corpus size, grading rubric, human coverage, critical-error examples and performance workloads. Executable corpus, reviews and measured evidence remain pending | Implement and execute the plan before model acceptance; product thresholds remain in Sections 7–8 |
| Q-006 | **Partially resolved:** [API design](../05-api-design.md) selects auth modes/lifecycle, identity and cancellation/recovery/error semantics. Exact operations/schemas/headers, auth bootstrap/policy/delivery details and schema version mechanics remain; P-006 compatibility guarantees are still proposed | Selected API/auth slices; generated OpenAPI begins early in implementation under #0, not as a prerequisite to finishing numbered planning documents |
| Q-007 | Provider error/refusal classification, output validity, bounded attempt/deadline policy for the two simple chains, and owner diagnostics. Alternative context bounds are deferred DF-001. | LLM/architecture specifications before provider orchestration; no advanced routing prerequisite. |
| Q-008 | Short-term abuse limits, operational alerts, provider-cost tracking/enforcement, and any additional availability SLO | Security/architecture/operations specifications, before launch; safeguard scope awaits P-005 review |
| Q-009 | Update browser/accessibility criteria, native controls, explicit-button/validation/processing/error behavior, oversize blocking, manual-edit protection and workspace teardown. | UX v1.2 resolves active presentation. Deferred surfaces/boundaries are not MVP blockers. |
| Q-010 | Acceptance or amendment of P-005 and P-006; any optional adoption/pilot success measure | PRD review; P-001 through P-004 are resolved, and no additional feature or adoption target is silently assumed |
