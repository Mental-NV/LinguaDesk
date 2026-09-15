# M043 — Serve live translation/rewriting in the portal with bounded spend
Status: selected
Milestone: [M043 — Serve live translation/rewriting in the portal with bounded spend](../../07-roadmap.md#45-useful-end-to-end-product-increments)

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-043 | The portal translates and rewrites through the serving candidate under a $0.05-per-operation cap with fail-fast startup when unconfigured, plus opt-in live e2e suites proving the portal path end-to-end against real LLM calls | Must | M026/M027/M028/M029 Done (operation paths and UI exist); M019 Done (adapter conformance); Q-001 (monetary cap decision, owner-locked at $0.05); serving API key in env at run time | selected | [proposal](proposal.md) |

Acceptance summary: verified user completes Translate and Rewrite in the portal against DeepSeek-V4.1-Flash with per-operation spend bounded and recorded; `run` without serving configuration exits non-zero instead of serving a dead portal; no retry loop ever presents for an unconfigured service; opt-in API-level and Playwright live suites (env-gated, capped, excluded from default gates) assert 201 + real translated text and an honest invalid-key error; reports carry credentialRef + presence only; detailed ACs are in spec.md (automation phase).
Human steps: owner confirms the DeepSeek key remains exported in the run environment if serving smoke or e2e-live ever reports unconfigured (timing: on demand, not a pre-gate — the key is already set); owner reviews the $0.05 cap and fail-fast behavior as the monetary-bound disposition (timing: close, blocked gate: report).
