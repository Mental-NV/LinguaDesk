# M045 — Tasks and evidence

Inputs: [spec](spec.md), [plan](plan.md), [source manifest](context.json).

## Resume

Next: T001. Implementation has not started; this user request authorized planning only.
Blockers: none for local implementation. Before T010, record actual live profile/credential availability, a finite M045 run budget/dispatch allowance and shared-scope/quiescence; no new paid authorization is inferred from prior dated runs. AC-013 and Done remain gated until its live evidence exists.
Last check: `python3 automation/context.py check M045`, `python3 automation/context.py audit` (227 Markdown documents), and `git diff --check` passed. No application tests, live provider calls, migration or deployment were run in the planning task.
Changed scope: PRD D-23; selected technical boundaries now in UX §8.4, API §2.2, AI §5.4 and verification's alternatives extension. Completed packages remain unchanged.

## Ordered tasks

- [ ] T001 — Implement/review/freeze shared scalar/sentence/limit fixtures and the development/release corpus manifests; record source impacts and access status. AC-005/007/009/013 prerequisite; no dependency; done when fixture parity, corpus coverage/review and current context check pass.
- [ ] T002 — Add actual C# alternatives/capability/kind/resolved-language metadata, generate and review OpenAPI/TypeScript, then retain scaffold status until handlers work. AC-004/007/012; depends T001; done when reviewed generation and drift checks pass before client adoption.
- [ ] T003 — Implement sentence/span validation, additive kind migration, compatible fingerprints and selected-sentence admission/settlement/status. AC-007/008/012; depends T001/T002; done when real SQLite concurrency/recovery/migration and independent-auth tests pass.
- [ ] T004 — Implement bounded alternatives prompt/parser/chain/coordinator, real serving and deterministic Smoke providers. AC-007/008/009; depends T002/T003; done when offline/transport/HTTP/deadline/cost tests prove complete endpoint behavior and no text persistence.
- [ ] T005 — Implement shell/panels/account menu/language bar/Styles-Tones controls and responsive tokens. AC-001/002/003; depends T001; done when component/geometry/keyboard/disabled-control checks pass, without altering auth pages.
- [ ] T006 — Add revision-bound resolved language, snapshot/preference state, atomic swap/reuse, clipboard controls and complete teardown. AC-001/004/010; depends T002/T005; done when fake-timer/clipboard/state/lifecycle checks pass.
- [ ] T007 — Implement sentence-aware display, native editing and bounded local comparison/deletion annotations. AC-005/006; depends T001/T006; done when Unicode/diff/edit/clean-copy and whole-metadata invalidation checks pass.
- [ ] T008 — Integrate generated alternatives transport, explicit requests/cache/selection and all competing-operation recovery guards. AC-006/007/008/010; depends T004/T006/T007; done when current/stale/unknown/cached-selection cases pass through real API and ledger.
- [ ] T009 — Extend dedicated-user desktop/narrow journeys; review all 12 D-23 visual baselines and run normal comparisons. AC-001–008/010/011; depends T005–T008; done when published UI assertions, semantic/geometry/clipboard/privacy checks and visual review pass.
- [ ] T010 — Add offline/live alternatives evaluator command and run/review the bounded 36-cell live development batch only with recorded authorization. AC-009/013; depends T001/T004 and the external live prerequisite; done when real model reports, reviews and cost/dispatch evidence pass. Continue T009/local T011 checks if access is unavailable; do not mark complete.
- [ ] T011 — Verify independent consumer, upgrade/restart/rollback path and full regression gates; review generated contract/diff; close backlog/delivery/coverage only with every AC passed. AC-001–013; depends T002–T010; done when standard gate, live review, visual review and evidence audit pass without a production/release claim.

## Planning record

Reviewed on 2026-10-25 (Europe/Moscow), baseline `c1455e77c01b275351403107826596f7ac334d73`: owning workflow/template, D-23 and active product/UX/API/AI/verification excerpts, completed dependency evidence and actual frontend/store/contract/admission/profile/serving/migration paths. Confirmed Node 24.20.0, npm 11.11.0, .NET 10.0.302, installed frontend dependencies and Chromium revision 1243 cache. No secrets were read.

Semantic review: all 13 local ACs map to tasks/checks; every new visible feature has published real-boundary browser proof; accounting/migration and provider changes have lower-layer checks; required live development remains explicit; release qualification/deployment are excluded without waiving their gates. Resolved-language plumbing, full-text fingerprint compatibility, context bounds and sentence association are addressed before consumers. Source lock establishes freshness only, not runtime acceptance.

## Completion record

No implementation completion evidence yet. At execution, append task/AC, revision/configuration, exact command/procedure, UTC timestamp, environment, actual result/report link, failures/fixes and limits. Record visual hashes/rationale in `visual-review.md` and live prerequisite/outcome summaries here; keep long sanitized logs in artifacts. A fixture run cannot be labeled live or substitute for AC-013.
