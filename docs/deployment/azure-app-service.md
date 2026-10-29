# Azure App Service delivery

M047 deploys one combined API/SPA artifact to each of two independent Linux F1 Web Apps. Staging receives trusted repository PR heads; Production receives pushes to `main`. Each environment has its own resource group, plan, SQLite store, Data Protection keys, identity, Insights component and workspace. Shared Staging represents the latest eligible PR deployment. Closing a PR does not delete it.

The exact naming/region contract is shared by Bicep and scripts through `infra/azure/environments.json`:

| Resource | Production | Staging |
| --- | --- | --- |
| Resource Group | `rg-prod-linguadesk` | `rg-staging-linguadesk` |
| Web App | `linguadesk` | `staging-linguadesk` |
| Plan | `plan-prod-linguadesk` | `plan-staging-linguadesk` |
| Insights | `appi-prod-linguadesk` | `appi-staging-linguadesk` |
| Workspace | `law-prod-linguadesk` | `law-staging-linguadesk` |
| Deployment identity | `id-prod-linguadesk` | `id-staging-linguadesk` |
| Region | `swedencentral` | `indonesiacentral` |
| Required hostname | `linguadesk.azurewebsites.net` | `staging-linguadesk.azurewebsites.net` |

Bootstrap checks both exact global Web App names before creating resources. An existing app is accepted only in the expected group with the expected default hostname. The templates omit the hashed-domain option; actual default hostnames are checked before activation. Unavailable names, unexpected regions or hashed/regional hostnames fail instead of creating an alternative URL. Name availability checks do not reserve the names. No custom-domain binding or paid SKU is needed for these Azure default hostnames. Both regions are versioned individually; bootstrap has no shared `--location` override.

## Bootstrap

Install the pinned .NET/Node/npm versions, Azure CLI, GitHub CLI and PowerShell 7. The scripts install Bicep 0.48.1 and Az PowerShell 16.3.0. Authenticate `az login` to the intended subscription and `gh auth login` to an account with repository admin access. The Azure account must create resource groups, managed identities, custom role definitions and resource-group role assignments. The subscription must have Linux F1 availability/quota in `swedencentral` for Production and `indonesiacentral` for Staging; scripts never upgrade the SKU or substitute a region.

Inspect the non-mutating setup contract:

```sh
bash scripts/azure/bootstrap.sh --dry-run
```

Run bootstrap from the repository root:

```sh
bash scripts/azure/bootstrap.sh
```

Bootstrap prompts without echo for a provider key for **each environment**, then validates credentials, subscription, regional availability and templates before resource mutation. After creating the groups/identities, bootstrap separately validates both App Service modules with disposable dummy credentials before GitHub configuration or runtime provisioning; ARM can skip these checks in the outer template when monitoring outputs are unresolved. For unattended secure execution, inject `LINGUADESK_BOOTSTRAP_STAGING_KEY` and `LINGUADESK_BOOTSTRAP_PRODUCTION_KEY` from the operator's secret manager. Do not put values in command-line arguments, files in the repository, shell history or chat. The existing evaluation key is not automatically reused as both environment credentials. Missing inputs fail before resource creation.

Before requesting keys, bootstrap checks existing resource-group locations. A group created in another region cannot be updated in place; it requires explicit migration/recreation before bootstrap can apply the new selection. The recreated Production group/identity now use `swedencentral`; the recreated Staging group/identity use the operator-selected `indonesiacentral`. Bootstrap does not delete these resources or overwrite their generated identity mappings. Subscription deployment records use a region suffix to avoid reusing an immutable deployment location.

Bootstrap creates exactly two resource groups. Each contains its own runtime resources, deployment managed identity/federation and an RG-scoped custom role/assignment. The deployment role grants only runtime deployment/monitoring operations and resource-group reads, with no ManagedIdentity or Authorization writes. Routine deployment cannot change its own federation, identity, RBAC or the other environment; there is no shared identity group. Bootstrap reads GitHub's repository OIDC settings before requesting keys or performing mutations. It validates the default template and reported subject mode/prefix, then Bicep appends the exact environment suffix. This repository uses immutable subjects: `repo:Mental-NV@7886867/LinguaDesk@1359148631:environment:Staging` and `repo:Mental-NV@7886867/LinguaDesk@1359148631:environment:Production`. Older repositories are supported only when GitHub explicitly reports legacy mode and a matching name-only prefix. Custom templates, unknown modes or mismatched prefixes fail closed. See [GitHub's immutable subject format](https://docs.github.com/en/actions/reference/security/oidc#immutable-subject-claims). No Azure client secrets, publish profiles or persistent PATs are stored in GitHub.

GitHub environments are created with no reviewer/wait gates. Production permits only the `main` branch. Staging permits `refs/pull/*/merge`, matching GitHub's PR execution reference even though checkout builds the head SHA. Bootstrap must fail if the repository plan cannot apply these protections. Only `DEEPSEEK_API_KEY` is stored in each environment; no GitHub variables are required. The optional manual secret fallback is GitHub Settings → Environments → the intended environment → `DEEPSEEK_API_KEY`.

Commit the generated **non-secret** `infra/azure/deployment.json`. It contains subscription/tenant IDs and each environment's client ID/resource-group name. Bootstrap never commits or pushes. Placeholders such as `<AZURE_SUBSCRIPTION_ID>`, `<AZURE_TENANT_ID>`, `<STAGING_CLIENT_ID>` or `<PRODUCTION_CLIENT_ID>` are documentation examples, not valid configuration. Do not hand-edit generated target identities; a rerun against a different subscription is rejected. Bootstrap is rerunnable for the same subscription/repository. Its initial runtime resources remain stopped, with telemetry disabled, until a checked artifact is deployed.

If bootstrap stops, rerun the same command after correcting the reported error; keep the generated manifest and existing resources. The monitoring step authenticates Az PowerShell from the current Azure CLI account using an ARM token held only in memory. `Connect-AzAccount -AccessToken` expects a string; converting that token to `SecureString` produces an invalid-token error that can misleadingly report missing subscription access. The bridge verifies account/token metadata and the resulting tenant/subscription, disables context autosave for the process, and clears token variables on every exit. Authentication failure stops before cap setters and prints a sanitized stage label. No additional login is required when the existing CLI session is valid. See the [AccessToken parameter contract](https://learn.microsoft.com/en-us/powershell/module/az.accounts/connect-azaccount#-accesstoken). Provisioning reads the monitoring module output only after creation; activation reads the existing Insights component. Azure diagnostics show recognized error codes and fixed hints while withholding raw messages, templates and secure configuration.

On 2026-10-28, an authenticated scan ran direct App Service module validation with `--validation-level Provider`, the actual Production app/plan names, Linux F1 and disposable dummy inputs. `austriaeast`, `westeurope` and `polandcentral` passed with provisioning state `Succeeded` and no diagnostic short-circuit; `northeurope` returned `SubscriptionIsOverQuotaForSku`. A later full scan also passed `swedencentral` (`artifacts/m047-f1-regions.json`), which the user now explicitly selects for Production. The previous Austria East and Germany West Central selections are superseded. Validation is a point-in-time preflight, not a quota/capacity reservation or a completed deployment. Sanitized scan: `artifacts/m047-production-f1-region-scan.json`.

The earlier Australia Central quota blocker is historical: the operator has selected Indonesia Central for Staging and recreated both environment groups/identities. On 2026-10-29, Staging's F1 plan and monitoring resources existed successfully, while Web App creation failed with `Conflict` (extended code `01020`) because the template requested a 64-bit worker. The F1 template now sets `siteConfig.use32BitWorkerProcess=true`, matching [Microsoft's F1/shared hosting sample](https://github.com/Azure-Samples/app-service-javascript-sap-cloud-sdk-quickstart/blob/main/infra/main.bicep). Azure can accept provider validation yet reject this setting during creation; validation is not deployment evidence. Existing resources and the regenerated identity manifest are retained. Rerun bootstrap with secure inputs to reconcile the corrected template; no cleanup or SKU upgrade is needed for this correction.

To find alternative regions with current subscription quota, run the validation-only scanner after `az login`:

```sh
# Check every advertised Linux F1 region, using the existing Staging group.
bash scripts/azure/find-f1-regions.sh

# Limit the scan, or save a machine-readable report (progress goes to stderr).
bash scripts/azure/find-f1-regions.sh --regions swedencentral westeurope polandcentral indonesiacentral
bash scripts/azure/find-f1-regions.sh --json > artifacts/f1-regions.json

# Optional: select another subscription and any existing validation group.
bash scripts/azure/find-f1-regions.sh --subscription <subscription-id> --resource-group <existing-group>
```

The scanner needs Azure CLI, Python 3 and an authenticated identity with provider-validation permissions. It installs the repository's pinned Bicep compiler locally. It checks **one additional Linux F1 plan** in each candidate region using a unique new plan name and explicit `Provider` validation, with four parallel checks by default (`--workers 1-8`, per-command `--timeout 120`). The flat probe contains no app, credentials, unresolved references or nested deployments. No provider key, GitHub login or bootstrap is required; the validation group must already exist and can be located in a different region from the candidate plan. No Azure resources are created/deleted, no region is automatically selected, and versioned configuration is unchanged.

`passed` means Azure confirmed complete provider validation at the reported time. `quota_exceeded` is a quota rejection; `failed`, `timeout` and `unverified` are distinct unknown/other failures, not evidence that quota is zero. Exit codes: **0** at least one region passed, **1** none passed, **2** setup/discovery failure. A pass does not reserve quota/capacity or establish that all other App Service/monitoring constraints pass. Changing an already provisioned environment's group/identity region still requires explicit migration/recreation. The scanner checks allocation separately from that existing-location constraint. See [Azure CLI provider validation](https://learn.microsoft.com/en-us/cli/azure/deployment/group#az-deployment-group-validate).

Rotate credentials by rerunning bootstrap with replacement environment inputs. For routine one-environment rotation, update that environment's GitHub secret using secure stdin and trigger a new eligible PR update or main push. Runtime configuration changes only when the deployment reconciles the corresponding environment. Bootstrap is an operator setup/maintenance action: do not run it concurrently with Actions deployments; it stops/reconciles both apps.

## Automatic delivery and checks

The workflow `.github/workflows/azure-app-service.yml` checks out the exact PR head or push SHA with no persisted checkout credentials. The offline job has read-only repository access and receives no environment/Azure/provider secrets. Fork and Dependabot PRs run the same checks and report why deployment is skipped. Do not use `pull_request_target` to run PR code with credentials.

`bash scripts/ci-offline.sh` drops ambient keys/tokens/secrets, forces offline telemetry, sets up dependencies and runs backend, contract, scripted AI, frontend, published Chromium, infrastructure/workflow/cap fixtures and documentation checks. It accepts no command arguments and has no live-evaluation path. Offline means no actual LLM calls; dependency downloads are allowed. For local execution, PowerShell, Azure CLI and the pinned application toolchains must be on PATH.

The reviewed screenshot baselines are Darwin-only. Checks use pinned `macos-15`; deployments use `ubuntu-24.04`. No screenshot baseline is generated or relaxed by CI. Linux storage and proxy behavior require hosted verification. The framework-dependent publish contains the Linux SQLite native assets; preflight refuses deployment unless Azure advertises `DOTNETCORE:10.0`.

Published browser tests create their own temporary migrated/seeded Smoke database and deterministic providers. Hosted profiles reject all Smoke/E2E environment flags. Runtime ordinary startup still never creates/migrates schema.

The Chromium smoke builds `artifacts/publish` through the existing `scripts/publish.sh`. Packaging reuses that output:

```sh
bash scripts/azure/package.sh --existing-publish
# Standalone: bash scripts/azure/package.sh
```

The ZIP root contains the API, SPA, migrations in the API assembly, startup wrapper and matching `revision.json` files. Filename/content checks reject databases, private keys, logs, test accounts and secret-bearing appsettings. Artifacts include a SHA-256 checksum and revision/run ID. Local dirty working-tree builds are labeled and cannot pass the deployment verifier as a committed SHA. Deploy jobs download that run's exact artifact. Package retention is 14 days; sanitized offline reports are retained seven days. Runtime backups and keys are never CI artifacts.

Environment deployment concurrency never cancels a deployment mid-migration. Before mutation, deployment validates the manifest, artifact/checksum, key, current repository head/PR status, F1/runtime availability and Bicep what-if scope. A newer eligible PR run owns shared Staging even if an older run builds slowly. The target app stops before resource/settings/package changes. No deletes, paid-plan changes or resources outside the selected runtime group are allowed by the reconciliation guard. Untouched in-group identity/RBAC resources reported as Ignore/NoChange by incremental what-if are accepted; any attempted change to them is blocked. The exact auto-created `Failure Anomalies - <selected Insights name>` alert rule in the selected subscription/group is also accepted only for Ignore/NoChange. Other alert names and all writes to that rule remain blocked. Boundary errors identify the offending resource ID and change type without exposing app settings or resource payloads.

Versioned environment parameter files own the daily cap policy. The deployment compiles them into a restricted temporary parameter file containing the secure provider input, then deletes that directory on every exit. Secure input is never passed as a command-line literal or logged. Bicep owns the complete app-settings collection. Final activation reconciles only the app/plan; it does not re-PUT monitoring resources after the cap gate. Its dynamic/secret values are environment, provider credential, generated monitoring connection, exporter enable gate and platform startup/build settings. Ordinary provider selection, character/money bounds, paths, logging and sampling come from .NET configuration. The existing finite application monetary values are preserved; their units are not reinterpreted as cents.

Distinct keys/databases do not enforce a shared provider billing ceiling. Before enabling paid user serving, allocate any shared authorized budget across these consumers. Infrastructure deployment and provider-free smoke do not authorize paid evaluation or establish product release qualification.

## Persistent storage, migration and proxy trust

Each app uses:

| Material | Durable location |
| --- | --- |
| SQLite store | `/home/linguadesk/data/linguadesk.db` |
| Initialization marker and OS leases | `/home/linguadesk/data/` |
| Data Protection key ring | `/home/linguadesk/keys` |
| Consistent pre-migration DB/key snapshots (original plus four newest) | `/home/linguadesk/backups` |
| Current/previous ready release ZIPs | `/home/linguadesk/releases` |

Startup requires `bash`, `flock`, `ip`, `awk`, `python3` and the .NET 10 runtime in the App Service image. Missing tools fail closed. The wrapper keeps an OS lock across preparation and serving. The API also holds a runtime storage lease; preparation cannot run while that lease is held. App configuration fixes one instance/worker. No scale-out, slots, Always On, database copying to ephemeral storage or concurrent migration/serving is supported.

Preparation provisions restrictive directories, acquires exclusive leases, preserves a SQLite backup and key snapshot, rejects migrations unknown to the artifact, runs migrations, checkpoints legacy WAL, selects DELETE, verifies journal mode/integrity/foreign keys and writes the initialization marker. Existing migrations are unchanged. The API validates actual journal mode on every EF connection and sets FULL synchronous mode, foreign keys and a five-second busy timeout. Local deployments retain WAL. A missing established store fails instead of resetting accounts/ledger. Failed preparation starts no host and keeps recovery material. OS leases release on abrupt process death; restarts retry preparation safely. Backup cleanup runs only after successful preparation and removes only integrity-checked superseded backups, retaining the original recovery point plus the four newest. Corrupt/failed material can consume disk and needs explicit operator disposition.

The previous ready artifact remains on persistent private storage. A release is marked ready only after startup validators pass. Failed/unknown archives remain recovery material. Monitor F1 disk usage; do not delete the sole usable backup, key ring or active DB to make a deployment fit.

The wrapper disables the framework's universal forwarded-header switch and derives one trusted `/32` gateway from the container's private/link-local default route. It never trusts a broad network. Offline tests reject spoofing from other addresses. The hosted antiforgery/cookie smoke, including a spoofed protocol header, must verify that this gateway is the actual immediate App Service proxy and that the platform overwrites untrusted forwarding headers. If the image topology differs, leave deployment failed and review observed hosting inputs; do not clear trusted-proxy restrictions to make smoke green.

App Service `/home` is network-backed. DELETE mode is a scoped selected profile, not proof of filesystem locking/durability. Hosted contention, interrupted writes, restart/redeployment and restore must pass before M047 is complete. Failure blocks the storage gate; no automatic switch of database/storage products is permitted.

## Monitoring

Each dedicated PerGB2018 workspace has 30-day retention. Both its workspace and Insights component must read back **50 MB = 0.05 decimal GB per day** before the app or exporter is enabled. The workspace template preserves the exact fractional cap; PowerShell reconciles it with a cap-only ARM PATCH (API 2023-09-01) containing a numeric JSON value and independently reads it back with retention through ARM GET. The pinned `Set-AzOperationalInsightsWorkspace -DailyQuotaGb` parameter is Int32: 0.05 becomes zero and is rejected. Do not use that cmdlet for fractional caps. The component setter accepts Double; its exact cap is independently verified through `CurrentBillingFeatures`. See the [workspace PATCH API](https://learn.microsoft.com/en-us/rest/api/loganalytics/workspaces/update?view=rest-loganalytics-2023-09-01) and [cmdlet parameter contract](https://learn.microsoft.com/en-us/powershell/module/az.operationalinsights/set-azoperationalinsightsworkspace#-dailyquotagb). Rejection, rounding or unavailable readback leaves serving stopped. Caps can be lowered in versioned environment parameters; they cannot exceed 50 MB through these templates.

Nominal combined 31-day ingestion at the selected caps is 3.1 GB. The published 5 GB allowance is shared at billing-account scope; other workloads and delayed/overshooting enforcement can cause charges. Bootstrap reports that shared consumption is unverified. Cap exhaustion suspends monitoring until reset and must not be used as the normal volume-control mechanism. No zero-invoice guarantee is claimed. No availability tests, paid alerts, profiler, snapshot debugger, browser agent, duplicated diagnostic export or extended retention are provisioned.

Only the `LinguaDesk.SafeTelemetry` activity source is exported. It constructs allowlisted request/dependency/error spans; raw HTTP/SQL/log activities are never subscribed for export. Request routes are templates, statuses/durations are numeric, names/errors are fixed, and incoming baggage/tracestate are removed. It exports no prompts, text, URL queries, email/account IDs, cookies, SQL values, headers or exception messages. There is no external log/metric exporter or duplicate agent. Sampling is 5% Staging and 10% Production. One sanitized startup marker carries environment and SHA independently of normal sampling. Export buffering is bounded in memory with disk persistence disabled. Export is disabled in CI/Testing/Smoke even if a connection string is supplied; hosted startup requires the generated connection. Exporter outages do not gate request serving.

After a real deployment, operator credentials with read access to both groups can verify marker isolation and exact caps without an LLM request:

```sh
python3 scripts/azure/verify-monitoring.py Staging <40-character-deployed-sha>
# Repeat for Production after its main deployment.
```

This polls at most five minutes, queries only the sanitized marker and reports counts/metadata. Do not fill the cap to test enforcement. Record pending ingestion as unverified instead of claiming routing passed.

## Hosted smoke, recovery and evidence

The deploy job polls HTTPS liveness/readiness, SPA root/deep link, capabilities, antiforgery Secure/HttpOnly cookie, missing API/assets, forwarding spoof resistance and revision with bounded retries/timeouts. It never registers an account or calls language-processing routes. A failed deploy stops the target and preserves home data. Published authenticated Chromium journeys remain offline.

To qualify hosted storage, use only isolated Staging synthetic accounts/stores and an authenticated private App Service SSH/SCM session, never a public maintenance endpoint or Production data. Execute the existing deployment preparation against an isolated copy while the serving target is stopped; verify `PRAGMA integrity_check`, `foreign_key_check`, account identity, operation IDs and character/monetary ledger totals. Exercise competing locks, terminate a writer before commit and reopen, restart, redeploy a second checked SHA, and repeat checks. Restore a stopped consistent backup into the isolated target and compare those invariants and protected cookies. Run Staging PR open/update delivery first, then Production main delivery; record actual Actions URLs, SHAs, target IDs, caps, revision and results in M047 tasks. These cloud-storage semantics cannot be discharged by local fixtures.

Rollback is an explicit recovery operation:

1. Disable new automatic runs with `gh workflow disable azure-app-service.yml`, then wait for the `deploy-Staging` or `deploy-Production` concurrency group to be idle. Keep it reserved for the whole recovery; do not run bootstrap concurrently.
2. Stop the target Web App and confirm it is stopped. Preserve the current store, keys, unresolved monetary exposure and operation identities. Obtain the previous ready ZIP from private `/home/linguadesk/releases`, or the exact retained checked Actions artifact; verify its recorded SHA/checksum.
3. Rehearse that artifact's preparation against an isolated backup first. Unknown/newer applied migrations fail; never delete migration history, auto-downgrade schema or bypass that guard. If schema-compatible, deploy the previous ZIP with server build/restart disabled, then start through its wrapper and run provider-free smoke against that revision.
4. If a DB restore is required, explicitly assess all ledger changes since the backup. Do not overwrite accrued charges/reservations. With the app stopped, retain the failed store and matching WAL/journal material privately, restore the consistent DB/key snapshot under the selected recovery scope, then prepare/start/smoke. Automatic restore is forbidden.
5. Record recovery evidence and restore automatic delivery only after the target passes readiness/invariants. The current desired Git revision must reflect the intended recovery before resuming automation; stale ordinary Actions runs intentionally cannot roll back a newer desired revision.

Troubleshooting: missing manifest means bootstrap/config commit is pending; missing key fails before mutation; unsupported runtime/quota never upgrades; rejected caps leave the app unavailable; initialized-but-missing DB needs recovery; lock contention means an old worker or maintenance task still owns the store; unknown migrations mean the artifact cannot safely roll back; failed HTTPS/cookie smoke needs verified proxy topology. Logs and evidence must remain sanitized.

References: [App Service storage](https://learn.microsoft.com/en-us/azure/app-service/operating-system-functionality), [SQLite network filesystem constraints](https://www.sqlite.org/useovernet.html), [GitHub environment execution refs](https://docs.github.com/en/actions/reference/workflows-and-actions/deployments-and-environments), [automated cap configuration](https://learn.microsoft.com/en-us/azure/azure-monitor/app/create-workspace-resource), [daily-cap limits](https://learn.microsoft.com/en-us/azure/azure-monitor/logs/daily-cap).
