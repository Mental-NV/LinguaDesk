# LinguaDesk — Roadmap and Milestone Plan

**Document:** #7 · **Version:** 1.12 · **Status:** M001–M006 done; M015 is the next recommended eligible candidate
**Updated:** 2026-09-09

## 1. Authority and planning basis

This roadmap follows [planning workflow #0](00-SDD-Planning-Workflow.md), including its v1.7 amendment removing the milestone duration limit. It orders small user/API outcomes and necessary infrastructure. Concrete backlog items, acceptance scenarios, implementation decisions and tasks are authored through #8 when a milestone approaches execution.

| Authoritative input | Revision read at Git `dede27c7e14409dff54c04ea0aaafc13cc0f3af9` | Responsibility |
| --- | --- | --- |
| [Workflow #0](00-SDD-Planning-Workflow.md) | v1.4; amended to v1.5 with this roadmap | Scope selection, artifact ownership, readiness and change control |
| [PRD #1](01-PRD.md) | v0.6 | Product scope, priorities, deferred/proposed status and release gates |
| [UX #2](02-ux-specification.md) | v1.5 | User journeys, workspace behavior and browser/accessibility contracts |
| [Architecture #3](03-architecture.md) | v1.7 | Stack, independent AI, hosting, persistence and accounting boundaries |
| [LLM specification #4](04-llm-specification.md) | v1.3 | Eligibility, prompts/checkers, family chains and serving qualification |
| [API design #5](05-api-design.md) | v1.1 | Shared auth, counting, identity/recovery/usage behavior and contract lifecycle |
| [Verification plan #6](06-verification-plan.md) | v1.0 | Checks, corpus/rubric/reviews, workloads, coverage and release evidence |
| [ADRs #9](09-architecture-decisions.md) | v1.7 | Existing decision rationale |
| User direction | 2026-09-08 | Infrastructure first; small feature/story milestones; easy rearrangement (duration rule superseded below) |

**Current policy — user direction, 2026-09-09:** No fixed milestone duration limit; this supersedes the original time cap and associated stop/split rules.

At the original roadmap baseline, the repository contained specifications only. No backend/frontend, generated OpenAPI, #8 backlog, delivery package or runtime evidence was present at this baseline. No implementation or model-speed benchmark is claimed here. The roadmap is a delivery hypothesis to revise after observing actual work.

MVP scope remains the PRD's two explicit whole-text operations, local accounts, independent API, shared allowances/cost bounds and accessible private workspace. Deferred capabilities remain in [PRD Section 3.1](01-PRD.md#31-deferred-scope-register-product-authority) with no scheduled milestone. Retired behavior is not restored; P-005/NFR-008 and P-006 are not accepted by scheduling adjacent work. Product thresholds and the full requirement-to-evidence matrix stay in #1/#6.

## 2. Milestone size and completion rules

Each executable milestone delivers a small, coherent outcome with explicit dependencies and observable acceptance. There is no fixed execution-duration limit. At #8 selection, inspect current code and previous evidence to assess scope, verification effort and risks. Split a candidate when independently valuable boundaries or dependencies justify it; elapsed time alone does not require stopping or splitting an active milestone.

Apply [#0's human-step timing](00-SDD-Planning-Workflow.md#selecting-work-and-creating-a-package): batch indispensable requests at the beginning; queue nonblocking human steps for the end with prepared instructions and pending evidence. Resolve necessary access, product decisions and external evidence dependencies before dependent execution. Section 5 tracks aggregate human/live-service gates separately; preparation and evidence-producing milestones can proceed without claiming those gates passed.

A milestone is done only when its selected exit criteria, applicable #6 checks and documentation/evidence updates are complete under #0's gates. Account isolation, no unintended submissions, privacy, durable accounting and cost admission apply when a capability is first introduced. Later cross-feature verification adds evidence; it does not authorize temporarily violating those contracts. Run all applicable regressions and record actual results. Durations may inform planning but are not readiness or completion gates. If work cannot continue because of a real blocker, preserve it and record the remaining outcome honestly. There is no fixed calendar schedule or total-duration promise.

## 3. Priority, dependency and current milestone summary

**Most recently completed: [M005 — Shared input and capability contract](08-backlogs/M005-shared-input-capability-contract.md).** State: **Done**. [Package 005](../specs/005-shared-input-capability-contract/spec.md) passed all eight scenarios for public capability discovery, shared C#/TypeScript scalar/validation fixtures and the first generated OpenAPI/type slice. Its [completion record](../specs/005-shared-input-capability-contract/tasks.md#3-completion-record) preserves exact evidence and exclusions.

**Most recently completed milestone: [M006 — Register a local API account](08-backlogs/M006-register-local-api-account.md).** State: **Done**. [Package 006](../specs/006-register-local-api-account/spec.md) AC-001–008 passed anonymous local registration, durable unverified Identity state, confirmation intent, current-state verification guard, the Identity migration, durable key configuration, account-serving readiness and generated-contract updates. Its [completion record](../specs/006-register-local-api-account/tasks.md#3-completion-record) owns the exact evidence. No later milestone is selected by this completion update.

**Next recommended eligible milestone after M006:** M015 — Validate language eligibility. M004 and M005 satisfy its named dependencies, and the priority policy below still brings the independent AI path forward. M015 remains a Candidate until its own backlog/package is authored from the then-current baseline; selecting M006 now does not create a concurrent implementation request or change M015's identity/dependencies.

The numbered sections below group related outcomes; **M IDs are identities, not execution positions**. The current priority policy is:

1. Complete M001 first, then establish only the infrastructure needed by the next chosen outcome (M002–M005).
2. Bring forward the independent AI path M015–M020 as soon as its dependencies permit, to expose provider/prompt risks while local access and accounting are developed. It does not wait for React or a production database. This ordering does not request concurrent agents.
3. Enable a verified independent API operation with durable limits and recovery, then connect its web journey. Prefer demonstrated Translation followed by Rewriting over polishing all account screens or all visuals first.
4. Integrate accessibility, privacy and failure checks with each feature. Begin bounded corpus/evaluation and operational preparation as soon as inputs are available; do not leave qualification risk until the final release review.

Dependencies in Section 4 are the minimum named prerequisites, not substitutes for #8's code/design readiness check. Satisfy transitive dependencies and the applicable Section 5 blockers too. Choose the highest-value eligible milestone; a blocked independent track need not stop another eligible one. M001–M006 are done as linked above. All milestones after M006 remain **Candidate**, with **Backlog: not created; Package: none; Evidence: pending**. These shared values are stated once instead of repeating empty columns.

M001–M006 have owning backlogs, delivery packages and completion evidence linked from their rows. Later milestones use #0's `Mxxx-<slug>.md` convention, or link an existing owning feature backlog. Replace the corresponding milestone title with a link to its actual backlog when created; record actual package/evidence links there and in #6. Do not create empty backlogs or package placeholders for this entire roadmap.

## 4. Provisional milestone outcomes

The exit column describes the increment's observable value. #8 supplies concrete acceptance and implementation scope only near execution. V-IDs refer to [verification check groups](06-verification-plan.md#22-stable-check-groups); they allocate relevant assertions, not every future test within a group. Infrastructure milestones are justified prerequisites, not user-visible features or empty layers created to match a diagram.

### 4.1 Basic infrastructure

| ID | Outcome | Exit criterion | Prerequisites | Basis / verification |
| --- | --- | --- | --- | --- |
| M001 | [Backend foundation](08-backlogs/M001-backend-foundation.md) | An agent can build and start the minimal backend and run an isolated meaningful smoke check with reproducible commands. | — | Architecture §2–3/5; V-009/V-012 (host portions only) |
| M002 | [Published web shell](08-backlogs/M002-published-web-shell.md) | A user can open the basic SPA through the API host; navigation works and missing API/assets return proper errors. | M001 | FR-003; architecture §3; V-003/V-012 |
| M003 | [Durable storage foundation](08-backlogs/M003-durable-storage-foundation.md) | A clean isolated store is created through migrations; persisted data survives a host restart and the storage checks are repeatable. | M001 | Architecture §6; V-005/V-016 |
| M004 | [Independent AI development](08-backlogs/M004-independent-ai-development.md) | An agent can exercise the shared AI boundary offline and inspect a synthetic prompt without starting API/UI or accessing secrets. | M001 | AI §2/9; V-007 |
| M005 | [Shared input and capability contract](08-backlogs/M005-shared-input-capability-contract.md) | An API consumer can discover the supported choices/limits; client and server agree on canonical input counts and validation fixtures. | M001 | FR-004/006/007/014/036; API §4/9; V-001/V-009 |

### 4.2 Local access

| ID | Outcome | Exit criterion | Prerequisites | Basis / verification |
| --- | --- | --- | --- | --- |
| M006 | [Register a local API account](08-backlogs/M006-register-local-api-account.md) | An API consumer can register; invalid registration is rejected and the new unverified account cannot perform language work. | M003, M005 | FR-001/002; V-004/V-009 |
| M007 | Verify a local account | A local account can complete verification or recover from an invalid/expired link; deterministic email evidence covers delivery intent. | M006 | FR-002; V-004 |
| M008 | Browser session access | A verified account can sign in/out through the cookie API with the required antiforgery and session invalidation behavior. | M007 | FR-001/002; API §3; V-004 |
| M009 | Independent client access | A verified API consumer can authenticate and refresh bearer access; credential precedence and revocation follow the shared contract. | M008 | FR-036; API §3; V-004/V-009 |
| M010 | Recover API account access | A local user can request and complete password reset; invalid tokens, non-enumeration and subsequent access invalidation are verified. | M009 | FR-002; V-004 |
| M011 | Register in the web app | A visitor can submit the registration form, understand validation and reach the unverified state without exposing passwords. | M002, M006 | UX §9; V-003/V-010/V-015 |
| M012 | Verify email in the web app | An unverified user can understand verification status, resend and explicitly continue after verification using the specified link variants. | M011, M007 | UX §9; V-003/V-004/V-010 |
| M013 | Sign in and leave safely | A user can sign in, follow a safe return route, sign out and recover from expiry without restoring private workspace text. | M012, M008 | FR-001/038; UX §3/9; V-002/V-003/V-010/V-015 |
| M014 | Recover access in the web app | A user can complete the forgot/reset-password forms and explicitly return to sign-in; failures and focus are understandable. | M013, M010 | FR-002; UX §9; V-003/V-010 |

### 4.3 Independently testable language behavior

| ID | Outcome | Exit criterion | Prerequisites | Basis / verification |
| --- | --- | --- | --- | --- |
| M015 | Validate language eligibility | The shared pipeline classifies synthetic eligibility outcomes correctly and never transforms locally invalid or rejected input. | M004, M005 | FR-004–007; AI §3–4; V-001/V-007 |
| M016 | Translate complete text through the AI boundary | A caller receives a validated complete Translation outcome from scripted providers; meaning/script expectations are represented in prompt/checker fixtures. | M015 | FR-008–011; AI §3–4; V-007 |
| M017 | Rewrite with one requested mode | A caller receives a validated full rewrite through the shared boundary; correction and the exclusive mode catalog are represented in fixtures. | M015 | FR-012–014/016; AI §3–4; V-007 |
| M018 | Bound failures and fallback | Both family pipelines obey eligible fallback, dispatch/admission bounds and the original deadline under controlled failures. | M016, M017 | FR-029/030/032–034; AI §5–7; V-007 |
| M019 | Connect one candidate adapter | One selected adapter proves its actual request settings, output/error/usage mapping and single-dispatch behavior with sanitized transport fixtures. | M018 | AI §5; Q-001/Q-007; V-008 |
| M020 | Produce trustworthy evaluation reports | An evaluator can run a bounded synthetic batch and obtain a versioned report distinguishing fixtures, live requests, failures and unresolved cost. | M018 | AI §9; verification §3/7; V-006/V-007/V-013 |

### 4.4 Shared allowance, cost and recovery behavior

| ID | Outcome | Exit criterion | Prerequisites | Basis / verification |
| --- | --- | --- | --- | --- |
| M021 | Reserve one logical operation | Concurrent clients share user/global capacity; identical identities are admitted once and changed payloads conflict without extra dispatch. | M003, M005, M007 | FR-024/026/027/035; API §5/7; V-005 |
| M022 | Settle a result exactly once | Complete success becomes one character charge; definitive failure releases capacity and duplicate or late settlement cannot charge twice. | M021 | FR-024/026/027; API §6–7; V-005 |
| M023 | Bound paid-attempt exposure | Every paid attempt reserves finite monetary exposure; denied capacity, unknown spend and reconciliation cannot bypass the configured ceiling. | M021 | NFR-006; architecture §7; V-006 |
| M024 | Recover an interrupted operation | A client can inspect its original operation after disconnect/restart without regeneration, lost-charge claims or a late result reviving failure. | M022, M023 | FR-026/037; API §5–6; V-005/V-016 |
| M025 | Report ordered usage across period changes | Clients receive authoritative availability/usage; midnight and month changes preserve correct charge/exposure attribution and snapshot ordering. | M022, M023 | FR-027/028; API §7; V-005/V-006 |

### 4.5 Useful end-to-end product increments

| ID | Outcome | Exit criterion | Prerequisites | Basis / verification |
| --- | --- | --- | --- | --- |
| M026 | Translate through the authenticated API | An independent verified client explicitly translates through the real host/accounting pipeline with a fake provider and receives complete output or a classified failure. | M009, M016, M018, M022, M023, M024, M025 | FR-003/009/011/024/035–037; V-005/V-007/V-009 |
| M027 | Rewrite through the authenticated API | The same client explicitly rewrites with the selected mode and the same durable limits, recovery and complete-result semantics. | M026, M017 | FR-012–018/024/035–037; V-005/V-007/V-009 |
| M028 | Translate in the web workspace | A signed-in user explicitly submits valid text, retains source/prior result on failure, and edits/copies a current result through the real integrated path. | M013, M026 | UX §6–7; V-001/V-002/V-003/V-010/V-012 |
| M029 | Rewrite in the web workspace | A user explicitly corrects/restyles text with one native mode and a complete editable result, preserving source and protected edits. | M028, M027 | UX §6/8; V-002/V-003/V-010/V-012 |
| M030 | Keep editing across competing events | Cross-page and native editing checks prove source/settings/result edits defeat stale callbacks while successful discarded work still updates usage. | M029 | FR-017/018/022/023/026; UX §6; V-002/V-005/V-010 |
| M031 | Recover visibly from unavailable or unknown outcomes | The user distinguishes definitive failure, unavailable usage and unknown outcome; explicit retry/status/refresh actions do not cause unintended language work. | M030, M024, M025 | FR-028/034/037; UX §6/9; V-002/V-003/V-005 |
| M032 | End and reset the workspace safely | Reset, session teardown and real navigation/restoration checks clear private text and restore defaults while independently settled usage remains correct. | M031 | FR-038; NFR-004; UX §3; V-002/V-010/V-015 |
| M033 | Demonstrate independent API use | A small consumer example completes both operations and usage recovery without the SPA; reviewed generated contracts and actual semantics agree. | M026, M027 | FR-035–037; RG-005; V-009 |
| M034 | Deliver real local-account email | The chosen email adapter conforms to the verified account journeys and a bounded live smoke records delivery evidence; missing access blocks completion. | M007, M010 | FR-002; Q-006; V-004/V-016 |

### 4.6 Bounded evidence and release preparation

| ID | Outcome | Exit criterion | Prerequisites | Basis / verification |
| --- | --- | --- | --- | --- |
| M035 | Prepare one reviewed evaluation batch | One small approved corpus batch has stable cases, reference constraints and coverage tags ready for the evaluation process. | M020 | Q-005; verification §5; V-013 |
| M036 | Evaluate one candidate batch | One budgeted candidate/batch run yields reproducible observations, grades and explicit review disposition without fixture/live confusion. | M019, M020, M035 | Q-001/Q-005/Q-007; V-008/V-013 |
| M037 | Make API performance measurable | A bounded workload rehearsal proves the real-API benchmark harness records the required timings, denominators, counts and cost/cache distinctions. | M026, M027, M020 | NFR-002; verification §6; V-014 |
| M038 | Verify one desktop journey group | One bounded desktop browser/visual journey group has the required native interaction evidence and reviewed baselines. | M014, M032 | NFR-005/007; verification §4; V-010/V-011 |
| M039 | Verify one narrow-screen journey group | One bounded narrow-screen journey group has reflow, control and accessibility evidence with actual-device/manual gaps explicitly recorded. | M014, M032 | NFR-005; verification §4; V-010/V-011 |
| M040 | Recover a published installation | An isolated published installation can migrate, restart and restore a consistent backup using verified operating instructions. | M003, M024, M032 | Architecture §6–7; RG-004; V-012/V-016 |
| M041 | Verify application data lifecycle | A bounded lifecycle check covers agreed metadata retention and text/secret exclusion in application diagnostics/storage and recovery artifacts. | M032, M040 | NFR-004; Q-004; V-015/V-016 |
| M042 | Present an evidenced release candidate | The candidate has a current walkthrough and evidence index; all active release gates and outstanding questions have an explicit supported disposition. | M033, M034, M040, M041; G1–G4 | RG-001–008; verification §7–9; V-016 |

M005 is the first completed product API contract increment in this ordering. It generated and reviewed OpenAPI from actual C# contracts before dependent client adoption. Subsequent API milestones extend and review those generated artifacts under #0/#5; they must not replace them with a provisional all-MVP schema or contract-only host.

M026–M029 establish integrated behavior with deterministic external adapters. They do not permit unqualified live serving. M019 supplies adapter conformance, not full model acceptance; a structurally valid evaluation profile may test an unqualified candidate under #4's budget rules. Production startup still requires qualification and monetary bounds. Core edit/stale/failure guards are part of the first consuming feature, while M030–M032 add cross-feature/native/restoration evidence.

M035–M039 are deliberately **one bounded batch or journey group each**, not labels for finishing the entire corpus, all providers, every browser or all release measurements. #8 chooses that first group's scope. Allocate new stable successor milestone IDs for remaining batches/groups after measuring the first; retain the overall coverage/workload in #6. The roadmap intentionally does not invent hundreds of future review tasks. Every required uncovered case/browser/gate remains pending until those successors or qualified external evidence close it. A harness rehearsal is not a live performance pass, and a recorded manual gap is not browser-support evidence.

## 5. External dependencies and release checkpoints

G1–G4 are **aggregate release checkpoints, not individual delivery increments**. They collect results over however many bounded milestones and external reviews are needed; they add no product scope or replacement thresholds. They cannot be marked complete just because a batch-producing milestone completed. Any corrective engineering becomes a new bounded milestone selected through #8.

| Checkpoint | Evidence required from its owners | Relevant milestones / blockers |
| --- | --- | --- |
| G1 — Candidate qualification | #4/#6 capability and full-family quality reports, complete corpus coverage, required human review and no unresolved critical findings for every configured candidate | M019/M020 and M035/M036 plus successors; Q-001/Q-005/Q-007, approved fixtures, reviewers, provider access and bounded evaluation spend |
| G2 — API performance | #6's complete live workloads and maximum-length/fallback/deadline evidence against PRD NFR-002/RG-003; retain failures and original denominators | M026/M027/M037 plus bounded measurement successors; deployment-like environment, capacity, live evaluation profile and budget. Scripted rehearsal cannot discharge this gate |
| G3 — Supported web access | Full agreed current/previous actual-browser, device, keyboard and AT evidence for current core journeys under #2/#6 | M038/M039 plus successors and qualified manual review; access to actual versions/devices/reviewers. Bundled engines alone are insufficient |
| G4 — Operational and specification readiness | Actual monetary cap and serving configuration; email evidence; retention/reconciliation/account lifecycle decisions; published migration/restore/privacy evidence and disposition of applicable launch questions | M034/M040/M041 plus successors; Q-001/Q-004/Q-006/Q-008/Q-010. PRD owners review proposals before any new safeguard or compatibility obligation is adopted |

Q-003/Q-006's selected wire/auth-policy/bootstrap/delivery details are resolved before their affected handlers/clients, not postponed to G4. M006 explicitly stages durable account/key records without a deletion or backup-retention claim; Q-004 still blocks those lifecycle behaviors and launch evidence when introduced. Offline scaffolding or scripted AI work does not need live credentials or a production cap; every live run needs its own finite admitted budget. Provider-managed caching does not waive application privacy, successful-character accounting or cost verification, and provider retention/no-training certification is not a release checkpoint.

M042 assembles **all RG-001–008** from the canonical [#6 coverage/evidence matrix](06-verification-plan.md#81-product-and-release-coverage), including functional/accounting/access evidence from earlier milestones and successors. Its prerequisite is that all active MVP obligations have evidence, not merely that the specifically named predecessors are done. The operating walkthrough describes actual working commands and limitations. Release readiness does not itself publish the product or create a new pilot/adoption gate.

## 6. Revising the roadmap without losing history

After each completed or interrupted milestone, update this document's current summary with its ID/state, measured elapsed time, outcome/evidence link, blockers and next recommended eligible ID. Item/task detail stays in its owning backlog/package. Use measured work to refine the next few milestones; leave later rows at outcome level.

| Change | Update rule |
| --- | --- |
| Reprioritize | Move rows or change the next recommendation and dependency edges; keep milestone IDs, backlog identities and evidence unchanged |
| Add an accepted feature/story | Update PRD/shared design first where needed; assign the next unused milestone ID, add its outcome/dependencies and link its future or existing owning backlog |
| Split an oversized candidate | Mark its existing ID **Superseded — split into ...**; allocate fresh IDs for independently verifiable outcomes, update dependents and preserve its original exit/history. Do not call the old milestone completed |
| Move an existing backlog item | Keep its item ID and single owning backlog, change target milestone and links, and reassess active package readiness; do not duplicate the item |
| Discover follow-up work after completion | Keep the completed milestone's original scope/evidence; add a new milestone referencing it. Mark evidence affected by changed behavior as superseded where appropriate |
| Defer/drop scope | Record the reason and replacement/dependency effect. Scheduling cannot remove an active MVP obligation; product deferral requires the PRD's authority first |
| Resume after upstream changes | Compare recorded revisions and current code, revisit affected acceptance, dependencies, estimate and checks before implementation; do not regenerate unrelated completed packages |

Use milestone summary states **Candidate → Selected → In progress → Done**, with **Blocked**, **Deferred** or **Superseded** and a reason where appropriate. A blocker is also recorded against the affected scope; #8 keeps its own item states under #0. Never reuse an ID or renumber completed milestones to make ordering look sequential. A short dated change note records what moved/split and why; preserve previous plans and measured evidence in their packages/Git history.

**Initial change record — 2026-09-08:** Created M001–M042 as provisional small outcome boundaries. M001 is recommended first; no backlog item/package was selected. G1–G4 distinguish aggregate external/release evidence from bounded engineering execution. New successor IDs will be allocated as evidence batches and actual scope become known.

**Selection update — 2026-09-08:** Created M001 backlog/BI-001 and package 001 with six acceptance scenarios and six unchecked execution tasks. Added the initial/end human-step rule from #0 v1.6; no human action is required for M001 as scoped. Updated its check references to the actual host portions of V-009/V-012; independent AI verification remains M004.

**M001 completion — 2026-09-08:** Implemented the pinned two-project backend scaffold, process-only liveness and absent-route boundary. Locked clean/repeated Release checks executed nine passing HTTP cases; successful and controlled-failure real-process smokes verified ephemeral loopback startup, nonzero propagation and cleanup. All package scenarios passed; no product or release gate is claimed.

**M002 selection — 2026-09-08:** Selected BI-002/package 002 after inspecting the completed M001 host, tests, commands and evidence. The increment adds a bounded signed-out informational shell, real static publication and focused navigation/asset-boundary verification; account forms and language features remain later milestones. Toolchain provisioning is explicitly front-loaded under #0.

**M002 completion — 2026-09-08:** Implemented the pinned React/Vite shell, explicit ASP.NET static/fallback boundaries and clean single-artifact publication. Locked checks passed 8 component and 20 HTTP cases; 6 Chromium cases passed against an isolated published Kestrel host at desktop/390/320 widths, repeated publication removed stale assets, and backend-only checks passed without a webroot. All package scenarios passed. The recorded full turn took approximately 35 minutes; this is historical duration evidence, with no continuing time-limit requirement. No account/editor product capability or release gate is claimed.

**M003 selection and policy update — 2026-09-09:** Selected BI-003/package 003 for explicit storage initialization, connection policy and isolated persistence/restart verification. Account/ledger schemas and production backup/recovery remain their own later outcomes. Applied #0 v1.7: the fixed milestone duration limit and associated time-based stop/split rules are removed. M001/M002 completion evidence is preserved.

**M003 completion — 2026-09-09:** Implemented EF Core SQLite migration and lazy runtime boundaries with explicit safe paths, WAL, per-connection foreign keys and finite lock waiting. Locked checks passed 42 backend cases; the documented path-with-spaces migration was repeatable, same-file data/history survived two real host processes, invalid targets failed without fallback and published-shell regressions remained green. The initial production schema contains framework migration metadata only. No account/ledger, backup/restore, storage-readiness or product/release acceptance is claimed.

**M004 selection — 2026-09-09:** Selected BI-004/package 004 after confirming M001 and the current SDK/solution/package baseline. The increment adds only the host-independent Infrastructure.Ai library, focused tests and standalone development runner needed to inspect `eligibility.v1` with fixed synthetic data and exercise one scripted `IChatClient` call. Eligibility decisions, transformations, fallback, adapters, live evaluation, API/UI composition and product/release evidence remain later milestones. No human action or blocking planning question remains.

**M004 completion — 2026-09-09:** Implemented the host-independent Infrastructure.Ai library, shared embedded `eligibility.v1` composition, exactly-one-call complete-response boundary, focused MSTest suite and fixed-only inspect/probe runner. Locked warning-free checks passed 10 AI plus 42 retained API/storage cases; backend smoke, 8 component cases and 6 isolated published Chromium cases passed. Offline execution used no credentials/provider/database/API host and left no process or persistent evaluation report. This is synthetic boundary evidence only: eligibility decisions, transformations, fallback, adapters, live evaluation and all product/release gates remain pending.

**M005 selection — 2026-09-09:** Selected BI-005/package 005 after confirming M001 in Git/evidence and rerunning the clean current baseline. The slice adds one anonymous read-only capabilities operation, the first nonempty Core count/catalog policy, shared C#/TypeScript scalar/validation fixtures, and deterministic OpenAPI 3.1/TypeScript type generation from actual endpoint metadata. It excludes text submission, semantic eligibility, auth, usage/accounting, persistence changes, AI/provider work, UI adoption and compatibility promises. No human action or blocking planning question remains; all eight scenarios and seven tasks are pending implementation.

**M005 completion — 2026-09-09:** Implemented the Core catalog and `unicode-scalar-v1` policy, anonymous `GET /api/capabilities`, shared C#/TypeScript fixtures, deterministic OpenAPI 3.1 generation and compile-checked TypeScript declarations. Locked checks passed 47 API/storage, 10 Core, 10 AI and 36 frontend unit cases; backend smoke and 6 isolated published Chromium cases passed after correcting the published OpenAPI runtime dependency. Audit and deliberate drift/CLI/zero-test guards passed. All eight package scenarios passed; language submissions, semantic eligibility, auth, usage/accounting, UI adoption, live services and release gates remain pending.

**M006 selection — 2026-09-09:** Selected BI-006/package 006 at clean Git `4aca2c52` after confirming M003/M005 completion in Git/evidence and rerunning 67 backend/Core/AI plus 36 frontend checks. The slice adds only anonymous local registration, durable unverified Identity state, deterministic confirmation intent, a current-state verified-account guard and indispensable migration/key/readiness/contract updates. It excludes sign-in, confirmation completion/resend, cookie/bearer/antiforgery, UI adoption, real email, language/accounting and deletion. #3/#5 resolve the selected technical/behavioral details; Q-004 lifecycle and P-005/P-006 retain their status. All eight scenarios and tasks are pending implementation. Contract preflight found a reproducible wrapper restore stall with a successful `--disable-build-servers` workaround; T001 owns the bounded correction before dependent work.

**M006 completion — 2026-09-09:** Implemented anonymous strict local registration, durable user-only Identity state, non-enumerating duplicate/concurrency behavior, deterministic confirmation intent, current-state `VerifiedAccount` authorization, the additive `LocalAccounts` migration, explicit durable Data Protection keys and pre-listen/current readiness. Generated OpenAPI 3.1/types now contain only capabilities plus registration. Locked checks passed 67 API/storage/Identity/readiness, 10 Core, 10 AI, 36 frontend unit and 6 published Chromium cases; audits and deliberate drift/CLI/startup failure guards passed. All eight package scenarios passed. Sign-in, confirmation/resend/status/reset, web adoption, real email, language/accounting, deletion/backup lifecycle, full FR-001/002, RG-005 and release evidence remain pending.

## 7. Next applicable gate and validation

M001–M006 are complete with linked evidence. M006 passed selection, specification, plan, implementation, requirements/design/source review, all T001–T008 checks and AC-001–008. The next applicable workflow action is to select and package an eligible candidate; M015 remains the recommendation but is not selected by this document update.

M003–M006 establish only explicit storage, registration and durable unverified account state; they do not make sign-in/verification, accounting, backup/restore, editor, semantic eligibility/language-operation or release acceptance complete. M015 remains eligible/recommended after M006, but that recommendation does not authorize live provider work or bypass its package-specific Q-005/Q-007 decisions and verification boundaries.

**M004 consistency and evidence review:** The planning comparison against Git `6f86f9f` remains historical. Implementation started from clean planning HEAD `762a0acd`, and the complete working-tree diff was reviewed against #0, BI-004, package acceptance and the shared designs. Package, lock, command, test/report, prompt-hash, no-effect, security/privacy and limitation evidence is linked from [tasks.md](../specs/004-independent-ai-development/tasks.md#3-completion-record). M001–M003 evidence remains linked; all active product requirements and release gates remain pending.

**M005 consistency and evidence review:** Planning used clean Git `a257cd1f`; implementation used the working tree based on planning HEAD `75647db`. The complete diff, package/reference graphs, generated artifacts and commands were reviewed against #0, BI-005, package acceptance, #1/#3/#5/#6 and ADR-004. [Tasks/evidence](../specs/005-shared-input-capability-contract/tasks.md#3-completion-record) records tool versions, test/report counts, hashes, audit, deterministic/failure checks, the corrected publish defect and scope limitations. All active language behavior and product/release gates beyond M005's narrow discovery/local-validation contract remain pending.

**M006 consistency and evidence review:** Implementation used the working tree based on planning HEAD `6b4055d`. The complete diff, additive migration, route/policy/readiness boundaries, dependency graph and generated artifacts were reviewed against #0, BI-006, package acceptance, #1/#2/#3/#5/#6 and ADR-003/004/007/009. [Tasks/evidence](../specs/006-register-local-api-account/tasks.md#3-completion-record) records tool versions, counts, hashes, audits, deliberate failures, no-effect/secret checks and limitations. M006 proves only registration and durable unverified state; full account access and every release gate remain pending.
