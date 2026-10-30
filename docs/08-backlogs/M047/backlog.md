# M047 — Azure App Service delivery through GitHub Actions

Status: In progress — repository implementation and offline verification; authenticated Azure qualification pending.
Milestone: [Roadmap](../../07-roadmap.md#46-bounded-evidence-and-release-preparation).

| Item | Outcome/value | Priority | Dependencies/blockers | State | Selected scope |
| --- | --- | --- | --- | --- | --- |
| BI-001 | Automatically publish the combined LinguaDesk API/SPA to one Linux F1 Production Web App, retaining its SQLite file across deployments and provisioning isolated capped monitoring with Bicep | P1 | M002/M003/M008/M021–M025/M043/M045 complete; M046 closed with accepted accuracy exceptions. Authenticated Azure/GitHub bootstrap access and provider keys precede hosted verification | In progress | [spec](spec.md), [plan](plan.md), [tasks](tasks.md) |

Acceptance summary: All PR updates run only offline checks; pushes to main deploy Production after the same checks. Credential isolation, persistent SQLite, migration/recovery, versioned configuration with minimal secrets, Bicep resources, capped Application Insights and no live LLM evaluation in GitHub CI are required. Detailed acceptance lives in spec.

External prerequisites: authenticated Azure access with resource/role-assignment permissions, GitHub administrative CLI access for bootstrap, available regional F1 quota and the Production runtime provider key. Bootstrap creates resources/identities/environments and generates non-secret configuration; the user need not create resources or populate GitHub variables manually. Only `DEEPSEEK_API_KEY` is stored in the Production GitHub environment. No credentials are needed to implement/test locally. Shared Azure Monitor free allowance and delayed caps preclude an unconditional zero-cost guarantee. This planning request does not execute cloud changes or authorize paid LLM evaluation.

Optional human checks: none required. Product release qualification remains separate from infrastructure deployment.
