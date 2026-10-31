# M047 — Selected specification

Selected item: BI-001. Selected specification, amended on 2026-10-30 for Production-only hosting and PR-only CI; exact Production names, in-group identity and `main` deployment branch are retained.
Sources: [manifest](context.json), [backlog](backlog.md). User direction on 2026-10-26 selects Linux App Service F1, SQLite as a file within each App Service, Bicep-provisioned resources, automated bootstrap, minimal GitHub secrets/variables, Application Insights with daily caps and offline-only GitHub CI.

## Scope

Deliver deployment infrastructure for the existing combined ASP.NET Core API/React SPA. Preserve account isolation, durable accounting, HTTPS/cookies, text/secret privacy and finite runtime monetary limits (NFR-004/006; architecture §5–7). One Production Web App owns its SQLite file and Data Protection keys; no deployment slots. PRs run only offline CI and cannot deploy. Infrastructure provisioning and backend monitoring are selected. No managed database, container registry, new end-user features, paid LLM evaluation or release-quality waiver is selected.

## Acceptance

| AC | Observable success/failure/boundary | Source |
| --- | --- | --- |
| AC-001 | The sole GitHub environment `Production` targets a Bicep-provisioned Linux F1 plan/Web App with its own Insights component and Log Analytics workspace. No Staging environment, slot or paid SKU is required | User direction; plan topology |
| AC-002 | All PR opened/reopened/updated/ready-for-review events run offline CI only, including repository, fork and Dependabot PRs. PR jobs receive no Azure/provider secrets or OIDC write permission and never deploy | User direction; GitHub trust boundary |
| AC-003 | A push to `main` triggers offline checks and deploys that push SHA only to Production. Failed checks block deployment; other branch pushes do not deploy Production | User direction |
| AC-004 | Published artifact contains matching API and SPA, revision metadata and required migration tooling, but no SQLite database, keys, credentials or test account data. Check/build jobs have no Azure or provider secrets | Architecture §3/5; V-009/V-012/V-015 |
| AC-005 | CI performs only deterministic/offline application checks, including published Chromium journeys. No live LLM evaluator, paid probe, live browser suite or language-processing postdeploy request executes | User direction; verification §2.1 |
| AC-006 | Non-secret runtime settings use versioned .NET configuration and generated Bicep settings. No GitHub variables are required; only `DEEPSEEK_API_KEY` is stored in Production. Bootstrap generates Azure identifiers and GitHub setup; missing credentials/placeholders fail clearly before mutation. PR checks cannot access Production credentials/data | Architecture §5; NFR-004/006 |
| AC-007 | The persistent Production SQLite file and Data Protection directory survive restart/redeployment on Linux App Service. Controlled initialization/migration completes before serving; failed migration leaves app unavailable and retains recovery material | User clarification; architecture §6.1; V-005/V-012/V-016 |
| AC-008 | Same-target deployments serialize; stale runs cannot overwrite newer desired revisions. A bounded provider-free smoke checks HTTPS, SPA, API and readiness, and reports the deployed SHA. Flushed progress shows deployment phases, waiting heartbeats and sanitized smoke retry status without timestamps or sensitive command/response contents | Architecture §5/7; V-012 |
| AC-009 | Automated local failure/recovery rehearsals cover migration failure, existing and empty stores, rollback-compatible artifacts and persistence. Hosted Production deployment and isolated-copy storage evidence require authenticated bootstrap and the provider key; destructive tests never use live Production data | Architecture §6; V-016 |
| AC-010 | Rerunnable Bicep/bootstrap creates only the Production RG, F1 plan/Web App, Insights/workspace, OIDC identity and scoped permissions. The GitHub environment and non-secret deployment manifest are generated; subsequent main deployments reconcile only Production. Staging resources/trust/environment are retired by explicit user direction | Revised user direction |
| AC-011 | The component and workspace have verified 0.05 GB/day caps, finite 30-day workspace retention and source filtering/sampling before telemetry is enabled. Fractional caps are not rounded up; cap-setting failure blocks rollout. Nominal 31-day ingestion allocation is 1.55 GB; shared allowance/overshoot limitations remain documented without a zero-cost guarantee | Revised user direction; Azure Monitor research |
| AC-012 | Production emits sanitized request/dependency/error telemetry to its Insights resource. Offline tests export nothing externally and detect private-text/secret leakage. A bounded hosted marker verifies routing without LLM calls or deliberately exhausting the cap | NFR-004; V-015; revised user direction |

## Selected names and separation

User direction on 2026-10-30 retires Staging and selects Production as the only hosted environment. The exact Production names, Sweden Central region and hostname in `infra/azure/environments.json` remain unchanged. PRs run CI only; pushes to `main` deploy Production after checks.

| Resource | Production |
| --- | --- |
| Resource Group | `rg-prod-linguadesk` |
| Web App | `linguadesk` |
| App Service Plan | `plan-prod-linguadesk` |
| Application Insights | `appi-prod-linguadesk` |
| Log Analytics Workspace | `law-prod-linguadesk` |
| Deployment Managed Identity | `id-prod-linguadesk` |
| Region | `swedencentral` |
| GitHub environment | `Production` |
| Required default hostname | `linguadesk.azurewebsites.net` |

Only `rg-prod-linguadesk` is provisioned. It contains the Production deployment identity/federation and runtime resources; no shared identity group is created. An RG-scoped custom deployment role grants runtime deployment/monitoring operations while excluding identity, federation and RBAC management. Incremental runtime what-if may ignore these bootstrap-owned resources but cannot change them. Bootstrap needs custom-role creation and role-assignment permission.

Check the exact global Web App name before resource mutation, accepting an existing app only when it belongs to the expected group and has the required exact hostname. Request the classic hostname without the hashed-domain option and verify the actual returned hostname before activation. An unavailable name, wrong region or hashed/regional hostname fails closed; no alternate name, region or paid SKU is selected automatically. Availability is not a reservation; actual hostname/resource provisioning remains hosted evidence under AC-001/006/008/010.

## Constraints and decisions

Operator tooling includes a validation-only scanner for subscription-specific availability of one additional Linux F1 plan across candidate regions. It requires no provider keys, does not create/delete resources or rewrite environment configuration, and distinguishes quota rejection from unknown, policy or permission failures. A successful scan is point-in-time validation rather than a reservation or hosted acceptance.

F1 is the requested hosting tier; cold starts, resource quotas, downtime during migration and absence of slots/Always On are accepted infrastructure limits, not an availability or performance qualification claim. The Production environment name does not establish RG-001–008. M046's eight accepted accuracy exceptions remain recorded; required live qualification stays outside GitHub CI.

SQLite stays in the app's `/home/linguadesk/data/linguadesk.db`, never `/tmp` or `/home/site/wwwroot`. App Service persistent home is network-backed: the selected Azure profile uses rollback journaling and one serving process, with hosted durability checks required. This is a scoped exception to the existing local-volume/WAL design, not a claim that arbitrary network SQLite is safe. No scale-out or concurrent migration/serving is permitted. Failure of the storage checks blocks deployment completion; do not silently switch database products or discard data.

Repository, fork and Dependabot PRs run the same offline checks without hosted deployment credentials. Only a checked push to `main` can deploy Production. Do not use `pull_request_target` to execute PR code with secrets.
