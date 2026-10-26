# M047 — Selected specification

Selected item: BI-001. Status: reviewed implementation plan; implementation pending.
Sources: [manifest](context.json), [backlog](backlog.md). User direction on 2026-10-26 selects Linux App Service F1, SQLite as a file within each App Service, Bicep-provisioned resources, automated bootstrap, minimal GitHub secrets/variables, Application Insights with daily caps and offline-only GitHub CI.

## Scope

Deliver deployment infrastructure for the existing combined ASP.NET Core API/React SPA. Preserve account isolation, durable accounting, HTTPS/cookies, text/secret privacy and finite runtime monetary limits (NFR-004/006; architecture §5–7). Two independent Web Apps represent Staging and Production; no deployment slots. Each owns its SQLite file and Data Protection keys. Infrastructure provisioning and backend monitoring are selected. No managed database, container registry, new end-user features, paid LLM evaluation or release-quality waiver is selected.

## Acceptance

| AC | Observable success/failure/boundary | Source |
| --- | --- | --- |
| AC-001 | GitHub environments named `Staging` and `Production` target distinct Bicep-provisioned Linux F1 plans/Web Apps with isolated Insights components and Log Analytics workspaces. No slot or paid SKU is required | User direction; plan topology |
| AC-002 | A same-repository PR opened/reopened/updated triggers offline checks and then Staging deployment of that PR head SHA. Fork/Dependabot PRs receive offline checks but no credentialed deployment; their summary explains the restriction | User direction; GitHub trust boundary |
| AC-003 | A push to `master` triggers offline checks and deploys that push SHA only to Production. Failed checks block deployment; other branch pushes do not deploy Production | User direction |
| AC-004 | Published artifact contains matching API and SPA, revision metadata and required migration tooling, but no SQLite database, keys, credentials or test account data. Check/build jobs have no Azure or provider secrets | Architecture §3/5; V-009/V-012/V-015 |
| AC-005 | CI performs only deterministic/offline application checks, including published Chromium journeys. No live LLM evaluator, paid probe, live browser suite or language-processing postdeploy request executes | User direction; verification §2.1 |
| AC-006 | Non-secret runtime settings use versioned .NET configuration and generated Bicep settings. No GitHub variables are required; only `DEEPSEEK_API_KEY` is stored as a secret in each environment. Bootstrap generates Azure identifiers and GitHub setup; missing credentials/placeholders fail clearly before mutation. Production settings/credentials/data cannot be used by Staging | Architecture §5; NFR-004/006 |
| AC-007 | Separate persistent SQLite files and Data Protection directories survive restart/redeployment on Linux App Service. Controlled initialization/migration completes before serving; failed migration leaves app unavailable and retains recovery material | User clarification; architecture §6.1; V-005/V-012/V-016 |
| AC-008 | Same-target deployments serialize; stale runs cannot overwrite newer desired revisions. A bounded provider-free smoke checks HTTPS, SPA, API and readiness, and reports the deployed SHA | Architecture §5/7; V-012 |
| AC-009 | Automated local failure/recovery rehearsals cover migration failure, existing and empty stores, rollback-compatible artifacts, persistence and environment routing. Hosted Staging then Production evidence is recorded only after authenticated bootstrap and provider keys exist | Architecture §6; V-016 |
| AC-010 | Rerunnable Bicep/bootstrap creates both RGs, F1 plans, Web Apps, Insights/workspaces, OIDC identities and scoped permissions. GitHub environments and non-secret deployment manifest are generated without manual resource or variable editing; subsequent PR/master deployments reconcile only the corresponding environment | Revised user direction |
| AC-011 | Both components and workspaces have verified 0.05 GB/day caps, finite 30-day workspace retention and source filtering/sampling before telemetry is enabled. Fractional caps are not rounded up; cap-setting failure blocks rollout. Combined nominal 31-day volume is 3.1 GB; shared allowance/overshoot limitations are documented, not represented as a zero-cost guarantee | Revised user direction; Azure Monitor research |
| AC-012 | Each environment emits sanitized request/dependency/error telemetry to its own Insights resource. Offline tests export nothing externally and detect private-text/secret leakage. A bounded hosted marker verifies routing without LLM calls or deliberately exhausting the cap | NFR-004; V-015; revised user direction |

## Constraints and decisions

F1 is the requested hosting tier; cold starts, resource quotas, downtime during migration and absence of slots/Always On are accepted infrastructure limits, not an availability or performance qualification claim. The Production environment name does not establish RG-001–008. M046's eight accepted accuracy exceptions remain recorded; required live qualification stays outside GitHub CI.

SQLite stays in each app's `/home/linguadesk/data/linguadesk.db`, never `/tmp` or `/home/site/wwwroot`. App Service persistent home is network-backed: the selected Azure profile uses rollback journaling and one serving process, with hosted durability checks required. This is a scoped exception to the existing local-volume/WAL design, not a claim that arbitrary network SQLite is safe. No scale-out or concurrent migration/serving is permitted. Failure of the storage checks blocks deployment completion; do not silently switch database products or discard data.

Trusted repository PR authors can deploy executable code to Staging; Staging must contain only non-production data and separately bounded credentials. External PRs must be brought onto a trusted repository branch before automatic Staging deployment. Do not use `pull_request_target` to execute PR code with secrets.
