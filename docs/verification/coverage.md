# Coverage and acceptance allocation

Authoritative continuation of [06-verification-plan.md](../06-verification-plan.md); section numbers refer to that document; read only when relevant to the selected scope.

## 8. Canonical coverage and acceptance allocation

### 8.1 Product and release coverage

This is the sole cross-document product coverage matrix; the source documents retain their behavioral acceptance IDs. Active product rows remain **Unselected / Pending** except for the explicitly linked, passing M005–M014 portions below; no narrow allocation satisfies a complete product requirement or release gate. Separately identified milestone rows link completed packages. Once product work is selected, link its actual #8 backlog/package and state partial coverage honestly. Deferred, retired and proposed rows are intentionally not current test gaps or passing gates. Design references plus Section 8.2 connect product rows to local scenario checks without copying their full acceptance prose.

| Requirement / gate | Shared design / acceptance owner | Checks | Backlog/package → evidence/status |
| --- | --- | --- | --- |
| FR-001 | UX §9; API §3 | V-004, V-003, V-010 | [M006 / BI-006](../08-backlogs/M006/backlog.md) registration portion, [M008 / BI-008](../08-backlogs/M008/backlog.md) → [AC-001–008](../08-backlogs/M008/spec.md#acceptance) cookie sign-in/session/sign-out portion, [M009 / BI-009](../08-backlogs/M009/backlog.md) → [AC-001–007](../08-backlogs/M009/spec.md#acceptance) bearer sign-in portion and [M010 / BI-010](../08-backlogs/M010/backlog.md) → [AC-001–007](../08-backlogs/M010/spec.md#acceptance) API reset/invalidation portion → Passed; [M011 / BI-011](../08-backlogs/M011/backlog.md) → [AC-001–007](../08-backlogs/M011/spec.md#acceptance) web registration-form portion → Passed; [M012 / BI-012](../08-backlogs/M012/backlog.md) → [AC-001–008](../08-backlogs/M012/spec.md#acceptance) web verification/status/resend/continuation portion → Passed; [M013 / BI-013](../08-backlogs/M013/backlog.md) → [AC-001–008](../08-backlogs/M013/spec.md#acceptance) web sign-in/safe-return/sign-out/expiry portion → Passed; full FR-001 remains pending |
| FR-002 | UX §9; API §3 | V-004, V-003, V-016 | [M006 / BI-006](../08-backlogs/M006/backlog.md) durable unverified/delivery-intent portion, [M007 / BI-007](../08-backlogs/M007/backlog.md) confirmation/resend portion, [M008 / BI-008](../08-backlogs/M008/backlog.md) unverified-session/current-state portion and [M010 / BI-010](../08-backlogs/M010/backlog.md) → [AC-001–007](../08-backlogs/M010/spec.md#acceptance) API password-recovery portion → Passed; [M012 / BI-012](../08-backlogs/M012/backlog.md) → [AC-001–008](../08-backlogs/M012/spec.md#acceptance) web verification/status/resend/continuation portion → Passed; [M013 / BI-013](../08-backlogs/M013/backlog.md) unverified sign-in continuation → Passed; [M014 / BI-014](../08-backlogs/M014/backlog.md) → [AC-001–008](../08-backlogs/M014/spec.md#acceptance) web forgot/reset-password recovery journey portion → Passed; live email remains unselected |
| FR-003 | UX §3; API §2 | V-003, V-009, V-012 | Unselected → Pending |
| FR-004 | UX §6–8; AI §3; API §2/4 | V-001, V-002, V-003, V-007, V-009, V-013 | [M005 AC-002/005](../08-backlogs/M005/spec.md#4-selected-acceptance) source-choice discovery/local selector portion → Passed; [M015 AC-001–004](../08-backlogs/M015/spec.md#acceptance) strict envelope parser, zero-dispatch gates, terminal mapping and resolved-language reuse → Passed; API/UI adoption remain unselected |
| FR-005 | AI §3; UX §7–8; API §2 | V-003, V-007, V-009, V-013 | [M005 AC-002](../08-backlogs/M005/spec.md#4-selected-acceptance) supported-language discovery portion → Passed; [M015 AC-002–005](../08-backlogs/M015/spec.md#acceptance) supported-language/both-scripts/negative-class evidence offline and on the bounded live slice → Passed; quality/UI remain unselected |
| FR-006 | AI §3; UX §7–8; API §2 | V-003, V-007, V-009, V-013 | [M005 AC-002](../08-backlogs/M005/spec.md#4-selected-acceptance) Chinese script-policy discovery portion → Passed; [M015 AC-005](../08-backlogs/M015/spec.md#acceptance) Simplified and Traditional input classified `zh` offline and live → Passed; [M016 AC-002/004](../08-backlogs/M016/spec.md#acceptance) both-script input translated and Simplified-output recorded on the three zh-target live development cases → Passed (development evidence; fidelity qualification stays with M020/M035+); quality/UI remain unselected |
| FR-007 | API §2/4; UX §7–8 | V-001, V-002, V-003, V-009 | [M005 AC-003–005](../08-backlogs/M005/spec.md#4-selected-acceptance) counts/limits/pure-validation portion → Passed; [M015 AC-002](../08-backlogs/M015/spec.md#acceptance) translation-5000/rewriting-2000 zero-dispatch gates with excess counts → Passed; [M016 AC-002/003](../08-backlogs/M016/spec.md#acceptance) translation-limit reuse through the same gates (empty/oversize end with zero transformation dispatches) → Passed; HTTP submission/UI remain unselected |
| FR-008 | AI §3–4; UX §7–8 | V-007, V-013, V-010 | [M016 AC-001–004](../08-backlogs/M016/spec.md#acceptance) strict translation envelope plus one validated complete plain-text result per direction, offline and on the bounded live slice → Passed (development evidence; quality evaluation stays with M020/M035+) |
| FR-009 | AI §3; UX §7; API §2 | V-013, V-003, V-009 | [M005 AC-002](../08-backlogs/M005/spec.md#4-selected-acceptance) direction-discovery portion → Passed; [M016 AC-002/004](../08-backlogs/M016/spec.md#acceptance) all 12 directed pairs translated through the shared pipeline offline and live → Passed (development evidence; quality evaluation/UI remain unselected) |
| FR-010 | AI §3–4; UX §7 | V-013, V-007, V-003 | [M016 AC-002/004](../08-backlogs/M016/spec.md#acceptance) faithful-translation pipeline portion (meaning/style/tone-preserving prompt, complete validated output, no rewriting mode combined) offline and live → Passed (development evidence; quality evaluation stays with M020/M035+) |
| FR-011 | UX §6–7; AI §4; API §6 | V-002, V-003, V-007, V-010, V-012 | [M016 AC-002/004](../08-backlogs/M016/spec.md#acceptance) complete validated plain-text result portion (single transformation dispatch, terminal recorded failure, no auto-submit/fallback in this slice) → Passed (development evidence); explicit UI activation/staleness remain unselected |
| FR-012 | AI §3; UX §8; API §2 | V-003, V-009, V-013, V-007 | [M005 AC-002](../08-backlogs/M005/spec.md#4-selected-acceptance) rewrite-language-discovery portion → Passed; [M017 AC-002/004](../08-backlogs/M017/spec.md#acceptance) same-language rewriting in all 4 languages (both Chinese input scripts, Simplified-output mandate) offline and live → Passed (development evidence; quality evaluation/UI remain unselected) |
| FR-013 | AI §3–4; UX §8 | V-013, V-007, V-003 | [M017 AC-001/002](../08-backlogs/M017/spec.md#acceptance) mandatory-correction portion (correction in every mode, minimal edits under correction-only, unchanged correct text permitted) offline and live → Passed (development evidence; quality evaluation/UI remain unselected) |
| FR-014 | UX §8; AI §3; API §2/4 | V-002, V-003, V-007, V-009, V-013 | [M005 AC-002/005](../08-backlogs/M005/spec.md#4-selected-acceptance) mode catalog/default/local-selector portion → Passed; [M017 AC-001/004](../08-backlogs/M017/spec.md#acceptance) exclusive-mode pipeline portion (exactly one catalog mode per dispatch, default `correctionOnly`, unknown/empty/multi-mode rejection with zero dispatches) offline and live → Passed (development evidence; UI remains unselected) |
| FR-015 | PRD FR-015; UX-AC-041/042 | — | Not applicable — Retired |
| FR-016 | UX §6/8; API §5 | V-002, V-003, V-010, V-012 | Unselected → Pending |
| FR-017 | UX §6/8 | V-002, V-003, V-010 | Unselected → Pending |
| FR-018 | UX §6/8; API §6 | V-002, V-003, V-005, V-010 | Unselected → Pending |
| FR-019 | PRD DF-002; UX deferred comparison | — | Not applicable — Deferred |
| FR-020 | PRD DF-001 | — | Not applicable — Deferred |
| FR-021 | PRD DF-001 | — | Not applicable — Deferred |
| FR-022 | UX §6/8 | V-002, V-003, V-010 | Unselected → Pending |
| FR-023 | UX §6/8 | V-002, V-003, V-010 | Unselected → Pending |
| FR-024 | API §4/7; architecture §7 | V-001, V-005, V-003 | Unselected → Pending |
| FR-025 | PRD DF-001 | — | Not applicable — Deferred |
| FR-026 | API §5–7; architecture §7; UX §6 | V-005, V-002, V-003 | Unselected → Pending |
| FR-027 | API §7; architecture §7 | V-005, V-003 | Unselected → Pending |
| FR-028 | API §7; UX §6/9 | V-005, V-002, V-003, V-009 | Unselected → Pending |
| FR-029 | AI §5; architecture §5 | V-007, V-008, V-013 | [M018 AC-001/006](../08-backlogs/M018/spec.md#acceptance) per-family chain validation and primary-only live wire settings portion → Passed (development evidence; qualification and serving startup remain unselected) |
| FR-030 | AI §5–6 | V-007, V-013 | [M018 AC-001](../08-backlogs/M018/spec.md#acceptance) one primary plus zero-or-one fallback per family with no route/mode selection portion → Passed (development evidence; DF-004 routing remains deferred) |
| FR-031 | PRD DF-004 | — | Not applicable — Deferred |
| FR-032 | AI §6 | V-007, V-008, V-014 | [M018 AC-002](../08-backlogs/M018/spec.md#acceptance) bounded-order traversal portion (both three-dispatch paths per family, eligibility reuse, exactly-once visits, no retry/repair/hedge) → Passed (development evidence; percentile measurement stays with V-014) |
| FR-033 | AI §4/6–7; API §8 | V-007, V-008, V-005, V-009 | [M018 AC-003](../08-backlogs/M018/spec.md#acceptance) eligible-trigger versus terminal mapping including credential-scope skip → Passed (development evidence; API error mapping remains unselected) |
| FR-034 | AI §6–7; UX §6; API §6 | V-007, V-013, V-014, V-005, V-003 | [M018 AC-005](../08-backlogs/M018/spec.md#acceptance) same-criteria fallback success, zero-charge failure and invisible-fallback shape portion → Passed (development evidence; quality evaluation and durable settlement remain unselected) |
| FR-035 | API §2/5/9 | V-009, V-005 | Unselected → Pending |
| FR-036 | API §2–3/7 | V-004, V-009, V-005 | [BI-005](../08-backlogs/M005/backlog.md) → [AC-001–006](../08-backlogs/M005/spec.md#4-selected-acceptance) public choices/limits and generated-type portion and [M009 / BI-009](../08-backlogs/M009/backlog.md) → [AC-001–007](../08-backlogs/M009/spec.md#acceptance) bearer auth portion → Passed; language/usage independence remains unselected |
| FR-037 | API §8; AI §7; UX §6/9 | V-009, V-003, V-004, V-005, V-007 | Unselected → Pending |
| FR-038 | UX §3/8; architecture §6/8 | V-002, V-003, V-010, V-015 | [M013 / BI-013](../08-backlogs/M013/backlog.md) in-memory auth/safe-return teardown portion → Passed; workspace-text lifecycle and full requirement remain pending |
| NFR-001 | PRD §8; AI §3–5 | V-013 | Unselected → Pending |
| NFR-002 | PRD §7; AI §6; API §6 | V-014, V-007, V-005 | [M018 AC-004/006](../08-backlogs/M018/spec.md#acceptance) original-deadline enforcement portion (captured once, no-fit refusal, cancellation fencing, fake-time proof, live per-case timings) → Passed (development evidence; percentile/load measurement stays with V-014) |
| NFR-003 | UX §6; architecture §7; API §5–7 | V-002, V-005, V-007, V-010, V-016 | Unselected → Pending |
| NFR-004 | UX §3; architecture §6; AI §8; API §9 | V-015, V-010, V-005 | [M007 AC-002/004/006](../08-backlogs/M007/spec.md#acceptance) account-secret/no-store slice → Passed; text lifecycle, browser and operation persistence coverage remain unselected |
| NFR-005 | UX §4–5/10 | V-003, V-010, V-011 | Unselected → Pending |
| NFR-006 | architecture §7; AI §5–6; API §7 | V-006, V-008, V-013, V-014 | [M018 AC-005/006](../08-backlogs/M018/spec.md#acceptance) per-dispatch admission and conservative-exposure portion (reserve-before-launch, denial stop, unresolved retention, live settled actuals) → Passed (development evidence; monetary cap and durable accounting remain unselected) |
| NFR-007 | UX §4–5; Should priority retained | V-011 | Unselected → Pending |
| NFR-008 | PRD P-005; no added acceptance | — | Not applicable — Proposed |
| RG-001 | PRD §8; active functional rows above | V-001, V-002, V-003, V-004, V-005, V-007, V-009, V-010, V-012 | Unselected → Pending |
| RG-002 | PRD §8; Section 5 | V-013 | Unselected → Pending |
| RG-003 | PRD §8; Section 6 | V-014 | Unselected → Pending |
| RG-004 | architecture §7; API §5–7; UX §6 | V-005, V-006, V-002, V-003 | Unselected → Pending |
| RG-005 | API §2–3/8; UX §9 | V-004, V-009, V-012, V-016 | Unselected → Pending |
| RG-006 | UX §10; Section 4 | V-010, V-011 | Unselected → Pending |
| RG-007 | architecture §6–7; AI §5/8–9 | V-015, V-006, V-008, V-013, V-014 | Unselected → Pending |
| RG-008 | PRD §11; Section 9 | V-016 | Unselected → Pending |
| M001 / BI-001 — enabling scope | Architecture §2–3/5; backend build/process and absent-route boundary; prerequisite for FR-035/036, no product acceptance claimed | V-009 HTTP-host portion; V-012 process-lifecycle portion | [BI-001](../08-backlogs/M001/backlog.md) → [package AC-001–006](../08-backlogs/M001/spec.md#3-selected-acceptance) → [tasks/evidence](../08-backlogs/M001/tasks.md#3-completion-record); Done / Passed — 9 HTTP tests plus successful and controlled-failure process smoke |
| M002 / BI-002 — enabling scope | Architecture §3.1; UX §3–5 staged shell; partial enablement for FR-003/NFR-005/007, no full product acceptance claimed | V-003 shell components; V-009 API/static boundaries; V-010 navigation/reflow; V-011 visual inspection; V-012 published-shell portion | [BI-002](../08-backlogs/M002/backlog.md) → [package AC-001–008](../08-backlogs/M002/spec.md#3-selected-acceptance) → [tasks/evidence](../08-backlogs/M002/tasks.md#3-completion-record); Done / Passed — 8 component, 20 HTTP and 6 isolated published-host Chromium cases plus stale-asset and screenshot evidence |
| M003 / BI-003 — enabling scope | Architecture §4.1/5.2/6.1; explicit initialization and clean-restart persistence; no account/ledger or full product acceptance claimed | V-016 fresh migrations/model drift; V-005 file-backed transaction/restart foundation only; V-009/V-012 host/published regressions | [BI-003](../08-backlogs/M003/backlog.md) → [package AC-001–006](../08-backlogs/M003/spec.md#3-selected-acceptance) → [tasks/evidence](../08-backlogs/M003/tasks.md#3-completion-record); Done / Passed — 22 storage cases within 42 backend tests, explicit repeat migration and same-file two-process restart, plus backend/published-shell regressions |
| M004 / BI-004 — enabling scope | Architecture §3.2/4.1/8.2; AI §2/4.1/9; host-independent shared boundary and synthetic prompt inspection only | V-007 independent build/prompt/scripted-client portions; V-009/V-012 host/published regressions | [BI-004](../08-backlogs/M004/backlog.md) → [package AC-001–007](../08-backlogs/M004/spec.md#3-selected-acceptance) → [tasks/evidence](../08-backlogs/M004/tasks.md#3-completion-record); Done / Passed — 10 focused AI and 42 retained API/storage cases, deterministic prompt hash/inspection, controlled failures, backend smoke and 8 component/6 published Chromium regressions; M015–M020 and all product/release assertions remain unselected |
| M005 / BI-005 — product contract slice | PRD FR-004–007/009/012/014/036; API §2/4/9; public discovery, pure local validation and generated contract/types only | V-001 shared scalar/limit/selector fixtures; V-009 public capability HTTP and schema/type drift portions; V-012 route/publish regressions | [BI-005](../08-backlogs/M005/backlog.md) → [package AC-001–008](../08-backlogs/M005/spec.md#4-selected-acceptance) → [tasks/evidence](../08-backlogs/M005/tasks.md#3-completion-record); Done / Passed — 47 API/storage, 10 Core, 10 AI, 36 frontend unit and 6 published Chromium cases plus deterministic generation/drift and failure guards; text submission, auth, usage, AI behavior, accounting, UI adoption and release checks remain unselected |
| M015 / BI-015 — language eligibility slice | PRD FR-004–007; AI §3–5; shared pipeline parser/gates plus bounded live development slice only | V-001 gate fixtures; V-007 parser/pipeline offline portions; V-008 live slice portion | [BI-015](../08-backlogs/M015/backlog.md) → [package AC-001–006](../08-backlogs/M015/spec.md#acceptance) → [tasks/evidence](../08-backlogs/M015/tasks.md#completion-record); Done / Passed — 56 focused AI cases (39 parser, 17 pipeline) within 101 AI and 225 backend checks, offline scripted slice 12/12 plus bounded live slice 12/12 with 10 dispatches, known usage and no unresolved spend; transformation, fallback/deadline, corpus qualification, API/UI adoption and release checks remain unselected |
| M016 / BI-016 — translation slice | PRD FR-006–011; AI §3–5/7–9; shared translation prompt/pipeline plus bounded live development slice only | V-007 parser/pipeline offline portions; V-008 live slice portion | [BI-016](../08-backlogs/M016/backlog.md) → [package AC-001–005](../08-backlogs/M016/spec.md#acceptance) → [tasks/evidence](../08-backlogs/M016/tasks.md#completion-record); Done / Passed — 61 focused AI cases (28 parser, 33 pipeline) within 162 AI and 286 backend checks, offline scripted slice 17/17 plus bounded live slice (all 12 directions succeeded, 25 dispatches, known usage, no unresolved spend); rewriting modes, fallback/deadline traversal, corpus qualification, API/UI adoption and release checks remain unselected |
| M017 / BI-017 — rewriting slice | PRD FR-012–014/016; AI §3–5/7–9; shared rewriting prompt/pipeline plus bounded live development slice only | V-007 parser/pipeline offline portions; V-008 live slice portion | [BI-017](../08-backlogs/M017/backlog.md) → [package AC-001–005](../08-backlogs/M017/spec.md#acceptance) → [tasks/evidence](../08-backlogs/M017/tasks.md#completion-record); Done / Passed — 67 focused AI cases (29 parser, 38 pipeline) within 229 AI and 353 backend checks, offline scripted slice 42/42 plus bounded live slice (all 36 language/mode cells succeeded, 73 dispatches, known usage, no unresolved spend); fallback/deadline traversal, corpus qualification, API/UI adoption and release checks remain unselected |
| M006 / BI-006 — local registration slice | PRD FR-001/002; architecture §5–6; API §3/9; anonymous local registration, durable unverified state and indispensable account-serving readiness only | V-004 Identity registration/non-enumeration/current-state guard; V-009 independent registration wire/schema/type drift; V-015 secret sentinels; V-016 Identity migration/key/startup/readiness; retained V-003/V-012 shell/publish regressions | [BI-006](../08-backlogs/M006/backlog.md) → [package AC-001–008](../08-backlogs/M006/spec.md#4-selected-acceptance) → [tasks/evidence](../08-backlogs/M006/tasks.md#3-completion-record); Done / Passed — 67 API/storage/Identity/readiness, 10 Core, 10 AI, 36 frontend unit and 6 published Chromium cases plus deterministic generation/drift/startup failure guards; sign-in, confirmation/resend/status/reset, web form, live email, language work, deletion and RG-005 remain outside this slice |
| M007 / BI-007 — local verification slice | PRD FR-002; architecture §5–6; API §3/9; anonymous durable email confirmation and non-enumerating cooldown-bound resend only | V-004 real Identity confirmation/restart/expiry/cooldown/concurrency/delivery recovery; V-009 generated operation/schema/type drift; V-015 no-store and secret-safe errors; retained V-003/V-012 shell/publish regressions | [BI-007](../08-backlogs/M007/backlog.md) → [package AC-001–008](../08-backlogs/M007/spec.md#acceptance) → [tasks/evidence](../08-backlogs/M007/tasks.md#completion-record); Done / Passed — 78 API/storage/Identity/readiness including 11 focused verification cases, 10 Core, 10 AI, 36 frontend unit and 6 published Chromium cases plus deterministic generation/drift and dependency audits; sign-in/status/reset, browser verification, live email, language work, deletion, full FR-002/RG-005 and release gates remain outside this slice |
| M013 / BI-013 — web sign-in slice | PRD FR-001/038; UX §3/9; completed M008 browser-session API | V-002 stale-completion/teardown ordering; V-003 components; V-010 published Chromium navigation/reflow; V-015 credential sentinels | [BI-013](../08-backlogs/M013/backlog.md) → [package AC-001–008](../08-backlogs/M013/spec.md#acceptance) → [tasks/evidence](../08-backlogs/M013/tasks.md#completion-record); Done / Passed — 104 frontend component/policy, 134 retained backend and 25 published HTTPS Chromium cases with real verified/unverified sign-in and real sign-out; recovery, workspace text, full FR-001/038 and release gates remain pending |
| M014 / BI-014 — web recovery slice | PRD FR-002; UX §9; completed M010 recovery API and M013 sign-in conventions | V-002 stale-completion ordering; V-003 components; V-009 generated drift; V-010 published Chromium navigation/reflow; V-012 published smoke; V-015 credential sentinels | [BI-014](../08-backlogs/M014/backlog.md) → [package AC-001–008](../08-backlogs/M014/spec.md#acceptance) → [tasks/evidence](../08-backlogs/M014/tasks.md#completion-record); Done / Passed — 139 frontend component/policy, 134 retained backend and 32 published HTTPS Chromium cases with real 202 acknowledgment and real generic 400 plus synthetic-link browser journeys; live email, full FR-002 and release gates remain pending |

### 8.2 Local acceptance allocation

Read the complete contracts at [UX Section 12](../ux/acceptance.md#12-acceptance-contracts-and-preserved-scenario-ids), [LLM Section 10](../04-llm-specification.md#10-local-acceptance-scenarios-and-handoffs) and [API Section 10](../05-api-design.md#10-acceptance-scenarios-and-readiness). The following indexes allocate every existing scenario. Active checks remain pending except for the explicitly linked M005–M014 portions. M013 includes real antiforgery, sign-in, verified/unverified session and sign-out browser trips over the owned HTTPS published host. Multiple V-IDs split different assertions; they do not require repeating a journey at every layer. For example, UX-AC-106 uses V-002 for ordering, V-003 for display and V-005 for durable snapshot order.

| UX scenario IDs | Current status | Check allocation |
| --- | --- | --- |
| UX-AC-001 | MVP | V-003, V-010, V-012 |
| UX-AC-002 | MVP | V-003, V-010, V-012 |
| UX-AC-003 | MVP | V-002, V-003, V-010, V-015 |
| UX-AC-004 | Amended | V-002, V-003, V-005, V-010 |
| UX-AC-005 | Amended | V-002, V-003, V-005 |
| UX-AC-006 | MVP | V-003, V-004 |
| UX-AC-007 | MVP | V-003, V-004, V-010 |
| UX-AC-008 | MVP | V-003, V-004 |
| UX-AC-009 | MVP | V-003, V-004 |
| UX-AC-010 | MVP | V-003, V-004 |
| UX-AC-011 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-012 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-013 | MVP | V-003, V-004, V-010, V-015 |
| UX-AC-014 | MVP | V-003, V-004, V-010 |
| UX-AC-015 | MVP | V-002, V-003, V-004, V-010, V-015 |
| UX-AC-016 | Amended | V-002, V-003, V-004, V-010, V-015 |
| UX-AC-017 | MVP | V-003, V-004, V-010 |
| UX-AC-018 | MVP | V-003, V-004, V-010 |
| UX-AC-019 | MVP | V-003, V-004, V-010 |
| UX-AC-020 | MVP | V-003, V-004 |
| UX-AC-021 | Amended | V-001, V-002, V-003 |
| UX-AC-022 | Amended | V-002, V-003 |
| UX-AC-023 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-024 | Amended | V-002, V-003 |
| UX-AC-025 | Amended | V-002, V-003, V-010, V-011 |
| UX-AC-026 | Amended | V-002, V-003, V-010 |
| UX-AC-027 | Amended | V-003, V-007, V-013 |
| UX-AC-028 | Amended | V-001, V-002, V-003 |
| UX-AC-029 | Amended | V-003, V-007, V-013 |
| UX-AC-030 | Amended | V-001, V-002, V-003 |
| UX-AC-031 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-032 | Amended | V-002, V-003, V-005 |
| UX-AC-033 | MVP | V-002, V-003, V-010 |
| UX-AC-034 | Amended | V-002, V-003 |
| UX-AC-035 | Amended | V-002, V-003, V-005 |
| UX-AC-036 | Amended | V-002, V-003, V-005 |
| UX-AC-037 | Amended | V-002, V-003, V-010 |
| UX-AC-038 | Amended | V-003, V-010 |
| UX-AC-039 | Amended | V-002, V-003 |
| UX-AC-040 | Amended | V-002, V-003 |
| UX-AC-041 | Retired | Not applicable; owning UX row records disposition |
| UX-AC-042 | Retired | Not applicable; owning UX row records disposition |
| UX-AC-043 | Amended | V-002, V-003, V-010, V-011 |
| UX-AC-044 | Amended | V-002, V-003, V-010 |
| UX-AC-045 | Amended | V-001, V-002, V-003 |
| UX-AC-046 | Amended | V-002, V-003, V-005 |
| UX-AC-047 | MVP | V-002, V-003, V-005 |
| UX-AC-048 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-049 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-050 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-051 | Amended | V-002, V-003, V-010 |
| UX-AC-052 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-053 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-054 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-055 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-056 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-057 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-058 | Amended | V-002, V-003, V-010 |
| UX-AC-059 | Amended | V-002, V-003, V-005, V-010 |
| UX-AC-060 | Amended | V-002, V-003, V-010 |
| UX-AC-061 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-062 | Retired | Not applicable; owning UX row records disposition |
| UX-AC-063 | Retired | Not applicable; owning UX row records disposition |
| UX-AC-064 | Retired | Not applicable; owning UX row records disposition |
| UX-AC-065 | MVP | V-002, V-003, V-005 |
| UX-AC-066 | Amended | V-002, V-003, V-005 |
| UX-AC-067 | Amended | V-002, V-003, V-005 |
| UX-AC-068 | Amended | V-002, V-003, V-005 |
| UX-AC-069 | Amended | V-002, V-003, V-005 |
| UX-AC-070 | Amended | V-002, V-003, V-006 |
| UX-AC-071 | Amended | V-002, V-003 |
| UX-AC-072 | MVP | V-002, V-003, V-005 |
| UX-AC-073 | MVP | V-002, V-003, V-005 |
| UX-AC-074 | Amended | V-002, V-003 |
| UX-AC-075 | MVP | V-002, V-003, V-005 |
| UX-AC-076 | Amended | V-002, V-003 |
| UX-AC-077 | Amended | V-010 |
| UX-AC-078 | Amended | V-003, V-010, V-011 |
| UX-AC-079 | Amended | V-003, V-010, V-011 |
| UX-AC-080 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-081 | Amended | V-003, V-010, V-011 |
| UX-AC-082 | Amended | V-002, V-003 |
| UX-AC-083 | Amended | V-003, V-010, V-011 |
| UX-AC-084 | Amended | V-002, V-003, V-010 |
| UX-AC-085 | Amended | V-002, V-003, V-005, V-010, V-015 |
| UX-AC-086 | Amended | V-002, V-003, V-010, V-015 |
| UX-AC-087 | MVP | V-002, V-003, V-010, V-015 |
| UX-AC-088 | Amended | V-003, V-007, V-013 |
| UX-AC-089 | Amended | V-003, V-007, V-013 |
| UX-AC-090 | Amended | V-002, V-003, V-013 |
| UX-AC-091 | Amended | V-002, V-003, V-013 |
| UX-AC-092 | Amended | V-003, V-010 |
| UX-AC-093 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-094 | Amended | V-002, V-003, V-010 |
| UX-AC-095 | Amended | V-003, V-010, V-011 |
| UX-AC-096 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-097 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-098 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-099 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-100 | Amended | V-002, V-003, V-010 |
| UX-AC-101 | MVP | V-003, V-004, V-010 |
| UX-AC-102 | MVP | V-003, V-004 |
| UX-AC-103 | MVP | V-003, V-004, V-010, V-015 |
| UX-AC-104 | MVP | V-003, V-004, V-010 |
| UX-AC-105 | Amended | V-002, V-003, V-005 |
| UX-AC-106 | MVP | V-002, V-003, V-005 |
| UX-AC-107 | Amended | V-002, V-003 |
| UX-AC-108 | MVP | V-002, V-003, V-005 |
| UX-AC-109 | Amended | V-001, V-002, V-003 |
| UX-AC-110 | Amended | V-002, V-003, V-013 |
| UX-AC-111 | Deferred | Not applicable; owning UX row records disposition |
| UX-AC-112 | MVP | V-003, V-010, V-012 |

| LLM scenario | Check allocation |
| --- | --- |
| LLM-AC-001 | V-007 |
| LLM-AC-002 | V-007 |
| LLM-AC-003 | V-001, V-007, V-013; M015 AC-002 passed the empty/oversize/equal-selection zero-dispatch gate portion offline and on the live slice |
| LLM-AC-004 | V-007, V-013; M015 AC-003 passed the negative/mismatch/refused terminal portion with failure recording, offline and live |
| LLM-AC-005 | V-007, V-008, V-013, V-015; M015 AC-004/005 passed the prompt-data/hint and resolved-language-reuse portion with no transformation dispatch; M016 AC-002 passed the translation prompt-data portion (original complete source as serialized data with resolved source plus explicit target, no history/prior-result/repair transcript, plain-text-only return); M017 AC-002 passed the rewriting prompt-data portion (original complete source as serialized data with resolved source plus exactly one mode, no history/prior-result/repair transcript, plain-text-only return) |
| LLM-AC-006 | V-007, V-013; M016 AC-002/004 passed the translation prompt/mandate portion (Simplified Chinese output, per-mode scope excluded, refusal-wording-in-source translated as data) as development evidence; M017 AC-001/002/004 passed the rewriting prompt/mandate portion (9-mode catalog with correction-only minimality, same-language output, Simplified Chinese output, refusal-wording-in-source rewritten as data) as development evidence; quality evaluation stays with M020/M035+ |
| LLM-AC-007 | V-007, V-008, V-013; M015 AC-001 passed the strict eligibility-envelope validity portion (hint-contradiction rule included); M016 AC-001/003 passed the strict translation-envelope validity portion (result/refused pairing rules, no keyword refusal, no salvage); M017 AC-001/003 passed the strict rewriting-envelope validity portion (same pairing rules, no keyword refusal, no salvage or retry) |
| LLM-AC-008 | V-007, V-008; M015 AC-004 passed the reuse-accepted-eligibility portion with exactly one classification dispatch and no transformation dispatch; M016 AC-002/003 passed the one-transformation-dispatch portion with accepted eligibility reused and no same-candidate retry or fallback; M017 AC-002/003 passed the rewriting one-transformation-dispatch portion (mode validated before dispatch, accepted eligibility reused, no same-candidate retry or fallback) |
| LLM-AC-009 | V-007, V-005, V-014 |
| LLM-AC-010 | V-006, V-007 |
| LLM-AC-011 | V-008; M019 AC-002/003 passed the wire-settings/bound/cancel/error-mapping portion through fake-handler fixtures and AC-005 passed one bounded live access dispatch; quality/capability qualification remains pending |
| LLM-AC-012 | V-015, V-005 |
| LLM-AC-013 | V-007, V-006, V-013; M019 runner distinguishes fixture/conformance, live, and missing-credential-blocked outcomes on the production bundle and recorded known usage/cost for one access dispatch; M015 `evaluate-eligibility` adds the budgeted allowlisted eligibility slice (offline scripted pass plus 10-dispatch live success with retained usage/exposure); M016 `evaluate-translation` adds the budgeted allowlisted translation slice (offline 17/17 pass plus 25-dispatch live success covering all 12 directions with retained usage/exposure); M017 `evaluate-rewriting` adds the budgeted allowlisted rewriting slice (offline 42/42 pass plus 73-dispatch live success covering all 36 language/mode cells with retained usage/exposure); M020 `evaluate-report` adds the versioned combined development report (offline 80/80 fixture pass with zero provider dispatches plus one shared-budget live batch: access probe and 76 live-development rows all matching, 127 dispatches, settled actuals, no unresolved spend; missing-credential live sections blocked with exit 3); live qualification and grader-cost evidence remain pending |
| LLM-AC-014 | V-013, V-014 |
| LLM-AC-015 | V-007, V-005, V-002, V-003, V-012 |

| API scenario | Check allocation |
| --- | --- |
| API-AC-001 | V-001; M005 AC-004/005 passed for the fixture/local-boundary portion, HTTP submission boundary later |
| API-AC-002 | V-004; M008 AC-001–005 passed the cookie/antiforgery/session-invalidation portion; M013 passed browser routing/teardown over the real HTTPS cookie boundary and M009 AC-001/003 passed the bearer precedence portion |
| API-AC-003 | V-004; M007 AC-001–003 passed confirmation/current-state, M008 AC-002–004 passed cookie credential/verified-state portions; M013 passed web sign-in/session browser evidence and M009 AC-001/002/004 passed the bearer credential portion, reset portion later |
| API-AC-004 | V-005; M021 AC-001/AC-004/AC-006 passed the admission portion (one reservation for concurrent identical claims, shared user/global ceilings with 429 categories and no overrun, restart-preserved reservation/revision); M022 AC-001/AC-003/AC-004 passed the settlement portion (one charge for settled success with idempotent duplicates, fenced late cross-outcome attempts with post-settlement 409, one consumed entry for 8 parallel settlements) |
| API-AC-005 | V-001, V-005; M021 AC-002/AC-003/AC-005/AC-007 passed the admission/matching portion (changed-payload 409, equivalent-spelling match, 24h/5min-skew expiry and 410-after-cleanup, pre-admission rejection matrix, metadata-only records) |
| API-AC-006 | V-005, V-009, V-015; M022 AC-007 passed the succeeded/failed terminal-read portion (original charge metadata with output unavailable, fresh usage, unknown stays 404 without asserting zero charge) |
| API-AC-007 | V-005, V-016; M022 AC-002/AC-005/AC-006 passed the failure-window portion (failure releases with zero charge, cross-midnight success charges the admission day, restart-preserved settlement/revision with fenced late attempts and zero-dispatch reads) |
| API-AC-008 | V-005, V-014 |
| API-AC-009 | V-005, V-002, V-003 |
| API-AC-010 | V-006 |
| API-AC-011 | V-005, V-002, V-003 |
| API-AC-012 | V-004, V-015; M006 registration duplicate, M007 resend, M008 invalid-credential/strict secret-safe portions and M009 bearer non-enumeration/strict secret-safe portions passed, reset later |
| API-AC-013 | V-009 |
| API-AC-014 | V-009; M005 capability, M006 registration, M007 verification, M008 browser-session and M009 bearer contract-generation portions passed |

The UX index contains 86 active scenarios (29 MVP, 57 Amended), 21 Deferred and five Retired, retaining all 112 IDs. The LLM index retains 15 and the API index 14 active scenarios. Deferred alternative branches within active rows do not create current checks. Native browser/AT evidence uses Section 4's representative journeys, not 86 full end-to-end scripts.
