# LinguaDesk

LinguaDesk contains a minimal ASP.NET Core host, the M002 published signed-out web shell, explicit SQLite persistence, an independent offline AI development boundary, the M005 shared input/capability contract, and the M006–M009 local-account registration, email-verification, browser-session and bearer-access API slices. The React shell provides the local sign-in form (M013), registration (M011) and email-verification (M012) routes with guarded Translation/Rewriting placeholders pending M028/M029; recovery forms remain staged until M014. An independent client can register a durable unverified account, submit captured confirmation material, request another verification delivery, establish/read/end a secure cookie session, or sign in for opaque bearer access/refresh credentials and read current-account status through the account API. The runtime delivery adapter intentionally remains unavailable until M034 supplies real email, while deterministic tests inject capturing/failing adapters. Password reset, account-status UI, live email, editors, language-operation submissions, eligibility decisions, provider integrations, usage/accounting, and release deployment remain unimplemented.

## Prerequisites

- .NET SDK `10.0.302` (pinned by `global.json`)
- Node.js `24.20.0` and npm `11.11.0` (pinned by `frontend/.node-version` and `frontend/package.json`)
- Bash, curl, and `pkill` (normally supplied by the operating system or `procps`)
- Access to the locked NuGet and npm packages; the repository-local EF tool and Chromium are installed explicitly by their setup commands

Backend setup/check/run/smoke still requires no Node, frontend assets, browser, Docker daemon, credentials, external service, or development certificate. Checks and smoke create uniquely owned temporary dependencies. The development `run` wrapper explicitly prepares persistent local storage outside the repository; direct/published application serving still requires an explicitly migrated database and an existing writable Data Protection key directory.

## Backend commands

Run these commands from the repository root. The script resolves the repository root itself, so it can also be invoked by absolute path from another working directory.

```sh
# Verify the SDK and restore exactly the committed dependency graph.
bash scripts/backend.sh setup

# Restore, build in Release with analyzers, run all backend tests, and verify
# that the machine-readable report contains discovered tests and no failures.
bash scripts/backend.sh check

# Trust the shared local HTTPS certificate once, then explicitly prepare/update
# persistent local-development storage and start the backend at https://localhost:5080.
dotnet dev-certs https --trust
bash scripts/backend.sh run

# Build and start the real host on an OS-assigned loopback port, probe it,
# and terminate the process and temporary output it owns within 30 seconds.
bash scripts/backend.sh smoke
```

With no storage environment variables, `run` applies the migrations and creates a restricted key directory under the sibling `../.linguadesk-development` directory. This data and its verification keys survive restarts and are not committed. Override that location and the listening address when needed:

```sh
LINGUADESK_DEVELOPMENT_DATA_PATH="/absolute/path/to/linguadesk-development" \
LINGUADESK_URL=https://localhost:5090 \
bash scripts/backend.sh run
```

If `Storage__DatabasePath` or `Security__DataProtectionKeysPath` is supplied explicitly, `run` respects that target and does not initialize that custom dependency. Use `scripts/storage.sh migrate` and provision the custom key directory yourself. The convenience behavior belongs to the development wrapper; the application itself never creates or migrates missing serving dependencies.

Operation admission enforces the configured user and global daily character allowances (`Operations__UserDailyAllowanceCharacters` defaulting to 20000, `Operations__GlobalDailyAllowanceCharacters` defaulting to 2000000; both must stay positive) with a UTC-midnight reset. On first admission the server creates a Data-Protection-protected fingerprint key file inside the configured key directory; back it up and restore it together with the database, otherwise duplicate detection after a restore cannot match earlier reservations.

`GET /health/live` remains process-only. `GET /health/ready` checks that the database still exists, has the current migration and can accept a rolled-back write, and that the configured key directory remains usable. It exposes only healthy/unhealthy and is excluded from product OpenAPI. Account-serving startup performs the same readiness validation before listening and never creates or migrates either dependency. Unsupported health methods are rejected with 405. Missing `/api` routes return a JSON Problem Details 404. With no generated frontend webroot, other unimplemented paths remain ordinary 404 responses; a published artifact serves the SPA document only for eligible GET/HEAD client routes. Missing API, health, asset, and file-like paths never fall through to HTML.

`check` writes collision-free TRX reports to `artifacts/test-results/backend-api.trx`, `backend-core.trx`, and `backend-ai.trx`. It retains the real file-backed SQLite migration, transaction, locking, failure, host-scope, and restart cases; exercises registration, confirmation, resend/cooldown, Identity, secure cookie sessions, bearer access/refresh, antiforgery, expiry/invalidation, verified-state authorization, key persistence and readiness; and separately guards the focused suites against zero-test success. It does not need npm or a browser. Generated build, test, and temporary smoke output is not committed. M009 scope and evidence are tracked in [`docs/08-backlogs/M009/tasks.md`](docs/08-backlogs/M009/tasks.md).

## Capability and contract commands

`GET /api/capabilities` is public and read-only. It returns JSON with `Cache-Control: no-store` and needs no account, database, frontend, provider, email service, or credential. The response is a catalog and validation contract only. Verified accounts can additionally reserve one logical operation with `POST /api/operations` (UUIDv7 `operationId`, `translation`/`rewriting` family, exact `source`, effective settings) and read it with `GET /api/operations/{operationId}`; admission is atomic against both daily allowances, duplicates observe, changed payloads conflict, and settled operations report `succeeded` (one admission-day charge, output unavailable) or `failed` (zero charge) metadata with a fresh usage snapshot. Settlement is internal only — there is no public settle/complete endpoint — and no provider dispatch or interrupted/unknown recovery exists yet. Bearer authentication exists as `POST /api/accounts/bearer-sign-in`, `POST /api/accounts/bearer-refresh` and `GET /api/accounts/me`.

The shared `unicode-scalar-v1` fixture drives the pure C# and TypeScript policy tests. The TypeScript helper takes the maximum advertised by the server; no UI imports it yet, and later language-operation handlers remain authoritative for submitted input.

```sh
# Restore the locked .NET/npm graph used by contract generation.
bash scripts/contract.sh setup

# Regenerate the OpenAPI 3.1 YAML and TypeScript declarations from the
# actual C# entry point and endpoint metadata.
bash scripts/contract.sh generate

# Regenerate into an owned temporary directory and fail on semantic or byte drift.
bash scripts/contract.sh check
```

The committed generated views are `docs/05-openapi.yaml` and `frontend/src/api/generated/linguadesk-api.d.ts`; do not edit either by hand. The document contains `GET /api/capabilities`, `POST /api/operations`, `GET /api/operations/{operationId}`, the three registration/verification operations, `GET /api/accounts/antiforgery`, `POST /api/accounts/sign-in`, `GET /api/accounts/session`, `POST /api/accounts/sign-out`, `POST /api/accounts/bearer-sign-in`, `POST /api/accounts/bearer-refresh`, `GET /api/accounts/me`, `POST /api/accounts/forgot-password`, and `POST /api/accounts/reset-password`. It uses pre-release artifact revision `0.1.0-m010` and is not served as a runtime documentation endpoint. The generated security schemes document the host-scoped session and antiforgery cookies, the antiforgery header, and the opaque Identity bearer scheme. All account responses are no-store. Contract generation registers real endpoint metadata without opening storage, writing filesystem keys/cooldown state, sending email, issuing cookies, or starting a listener. Its revision does not promise API compatibility or a deprecation period.

Browser-session calls must use HTTPS so the `__Host-` Secure cookies are accepted. Bootstrap with `GET /api/accounts/antiforgery`, retain its HttpOnly cookie, and send the returned request token in `X-LinguaDesk-Antiforgery` on sign-in/sign-out. After identity changes, bootstrap again before the next mutation. Sign-in accepts only `{ "email", "password" }`; session cookies are nonpersistent and the protected ticket expires eight hours after issue without renewal. Independent clients sign in with only `{ "email", "password" }` at `POST /api/accounts/bearer-sign-in`, receive an opaque 15-minute access / 7-day refresh pair with `expiresIn` 900 and `tokenType` Bearer, refresh with the held `{ "refreshToken", "accessToken" }` pair, and read `GET /api/accounts/me` with `Authorization: Bearer ...`; bearer calls need no antiforgery token and never set cookies. Direct deployments terminate HTTPS in Kestrel. A deployment that terminates TLS at a reverse proxy must set `Security__ForwardedHeaderTrustedNetworks__0` (and subsequent entries) to the proxy's exact CIDR networks; only one symmetric `X-Forwarded-For`/`X-Forwarded-Proto` hop from those networks is honored before authentication.

## Independent AI development commands

The M004 inner loop uses `Microsoft.Extensions.AI.Abstractions` `10.9.0` and does not reference the API, ASP.NET Core, EF/Identity, a provider SDK, or the full AI middleware package. M019 adds the multi-profile candidate registry, the OpenAI-compatible Chat Completions adapter, and the evaluation credential/live-access workflows on the same host-independent surface. Run setup once when the locked graph is absent locally, then use the focused commands from any directory:

```sh
# Verify .NET 10.0.302 and restore only the three locked AI projects.
bash scripts/ai.sh setup

# Build the shared library, standalone runner, and tests in Release; run the
# focused tests offline and verify deterministic inspect plus scripted probe.
bash scripts/ai.sh check

# Print the exact eligibility.v1 system/user messages and resource SHA-256.
bash scripts/ai.sh inspect

# Invoke the shared boundary once with a fixed scripted raw response.
bash scripts/ai.sh probe

# Run sanitized transport conformance for every registry profile offline.
bash scripts/ai.sh conformance

# Make at most one low-output fixed-synthetic budget-admitted live request
# through the profile's actual endpoint/model/auth/settings/parser.
bash scripts/ai.sh verify-access --profile DeepSeek-V4.1-Flash --max-dispatches 1 --max-spend-usd 0.05

# Run the allowlisted synthetic eligibility slice offline (scripted
# classifications, no credential, no network) or live (--live) through the
# selected profile under the explicit finite budget. Both write a sanitized
# metadata-only report under artifacts/eligibility/.
bash scripts/ai.sh evaluate-eligibility --profile DeepSeek-V4.1-Flash --max-dispatches 12 --max-spend-usd 0.50
bash scripts/ai.sh evaluate-eligibility --live --profile DeepSeek-V4.1-Flash --max-dispatches 12 --max-spend-usd 0.50

# Run the allowlisted synthetic translation slice offline (scripted
# classification/transformation pairs, no credential, no network) or live
# (--live) through the selected profile under the explicit finite budget.
# The slice covers every Translation direction; the refusal/malformed edge
# cases are offline-only scripted proofs and are skipped live. Both write a
# sanitized metadata-only report under artifacts/translation/.
bash scripts/ai.sh evaluate-translation --profile DeepSeek-V4.1-Flash --max-dispatches 34 --max-spend-usd 0.50
bash scripts/ai.sh evaluate-translation --live --profile DeepSeek-V4.1-Flash --max-dispatches 34 --max-spend-usd 0.50

# Run the allowlisted synthetic rewriting slice offline (scripted
# classification/transformation pairs, no credential, no network) or live
# (--live) through the selected profile under the explicit finite budget.
# The slice covers every language/mode cell (4 languages x 9 modes, both
# Chinese input scripts); the refusal/malformed edge cases are offline-only
# scripted proofs and are skipped live. Both write a sanitized metadata-only
# report under artifacts/rewriting/.
bash scripts/ai.sh evaluate-rewriting --profile DeepSeek-V4.1-Flash --max-dispatches 80 --max-spend-usd 0.50
bash scripts/ai.sh evaluate-rewriting --live --profile DeepSeek-V4.1-Flash --max-dispatches 80 --max-spend-usd 0.50

# Run the small chain-bounds slice offline (scripted pairs through the
# primary-only chain, no credential, no network) or live (--live) through
# the selected profile under the explicit finite dispatch, spend, and
# overall-deadline budget. The slice covers five translation directions
# (both Chinese input scripts) plus one rewriting cell per language. The
# live path refuses any configured fallback: injected faults are proven
# deterministically offline, and the live slice records only natural
# primary-only calls with attempt/usage/exposure/deadline metadata.
# Both write a sanitized metadata-only report under artifacts/chain-bounds/.
bash scripts/ai.sh evaluate-chain-bounds --profile DeepSeek-V4.1-Flash --max-dispatches 24 --max-spend-usd 0.50
bash scripts/ai.sh evaluate-chain-bounds --live --profile DeepSeek-V4.1-Flash --max-dispatches 24 --max-spend-usd 0.50

# Run the combined development report offline (all four slices scripted,
# injected faults labeled fault_injected, zero provider dispatches) or live
# (--live runs the same fixture batch plus the budgeted primary-only live
# development slices under one shared EvaluationBudget with a leading
# live_access probe). Offline-only fault cases are skipped live, never
# relabeled. Both write one versioned metadata-only report with
# per-family/route/language aggregates and conservative exposure totals
# under artifacts/evaluation/.
bash scripts/ai.sh evaluate-report --profile DeepSeek-V4.1-Flash --max-dispatches 150 --max-spend-usd 2.00
bash scripts/ai.sh evaluate-report --live --profile DeepSeek-V4.1-Flash --max-dispatches 150 --max-spend-usd 2.00
```

`check`, `inspect`, `probe`, and `conformance` clear common provider credential variables and point outbound HTTP proxies at an unreachable loopback address. They start no API/frontend process, open no database, and write no persistent evaluation report; `check` writes only generated build output and `artifacts/test-results/ai.trx`. The default offline `evaluate-eligibility`, `evaluate-translation`, `evaluate-rewriting`, `evaluate-chain-bounds`, and `evaluate-report` runs work the same way: scripted responses for the allowlisted synthetic slice, no credential read, no network, plus a metadata-only report under `artifacts/eligibility/`, `artifacts/translation/`, `artifacts/rewriting/`, `artifacts/chain-bounds/`, or (for the combined report) `artifacts/evaluation/`. Only `verify-access` and the `--live` evaluation runs use the network and the host credential: each reads the key exclusively from `LINGUADESK_AIEVALUATION__CREDENTIALS__<REF>__APIKEY` (for example `...__DEEPSEEK__APIKEY` for `CredentialRef` `deepseek`), stays within the explicit finite dispatch and spend budget, and prints a sanitized report naming only the reference, disposition, usage, and exposure — never the key. A missing or blank credential exits blocked (`3`) with zero dispatches and no scripted fallback. The runner accepts no source, configuration, credential, or arbitrary text argument.

Inspection deliberately prints the checked-in synthetic source containing a quote, newline, and instruction-like phrase. `probe` labels its observation `raw_scripted_observation` and reports `live: false` and `validated: false`. Neither command classifies a product input, validates a response, transforms text, qualifies a model/provider, measures quality/cost/performance, or proves a release gate. Those behaviors remain later selected milestones.

## Storage commands

Storage uses EF Core SQLite and Identity Entity Framework Core `10.0.10`, the repository-local `dotnet-ef` `10.0.10` tool, and the committed `InitialStorage` plus `LocalAccounts` migration chain. The native SQLite bundle is pinned to `2.1.12` to avoid the vulnerable transitive `2.1.11` package. Setup restores tools and locked backend dependencies but does not create a directory or database:

```sh
bash scripts/storage.sh setup
```

Choose an absolute file path whose parent already exists and is writable. Keep it outside this repository's `wwwroot`, `frontend/dist`, `artifacts`, and build/publish output. For example, from the repository root:

```sh
storage_directory="$(cd .. && pwd)/linguadesk-data"
mkdir -p "$storage_directory"
bash scripts/storage.sh migrate "$storage_directory/linguadesk.db"
```

The migration command has no default target, accepts paths containing spaces, and exits nonzero for missing, relative, in-memory, URI, directory, unsafe-output, or unopenable targets. Reapplying it is safe: it retains existing migration history and data. Explicit initialization uses read/write-create mode and establishes WAL; application contexts use read/write-only mode with foreign keys enabled, pooling disabled, and a finite five-second default timeout.

To run the account-serving host, also create an existing key directory outside the repository and configure both paths:

```sh
mkdir "$storage_directory/keys"
chmod 700 "$storage_directory/keys"
Storage__DatabasePath="$storage_directory/linguadesk.db" \
Security__DataProtectionKeysPath="$storage_directory/keys" \
bash scripts/backend.sh run
```

Build, publish, and contract generation do not open, create, or migrate storage. Ordinary runtime startup requires the current database and usable key directory before listening. The defaults are `/var/lib/linguadesk/linguadesk.db` and `/var/lib/linguadesk/keys`; runtime access to absent or stale dependencies fails instead of creating, migrating, repairing, or falling back. Data Protection uses the fixed application name `LinguaDesk`; retain and back up its generated key files with account data.

M006–M009 prove local migrated account persistence, serving readiness, deterministic confirmation, single-process durable resend cooldown behavior, real Identity cookie/antiforgery session behavior, and real Identity bearer access/refresh with per-request stamp validation. They do not prove power-loss durability, backup/restore, a production migration bundle, multi-instance/network-filesystem operation, account deletion/retention, browser account UX, password recovery, account-wide logout, or live email. The production schema remains the standard user-only Identity tables; M007 reuses `AspNetUserTokens`, while M008–M009 add no migration, role, product profile, ledger, language text, outbox, session row, or key table.

## Frontend and publishing commands

```sh
# Verify Node/npm, install locked dependencies, and install matching Chromium.
bash scripts/frontend.sh setup

# Run strict TypeScript, ESLint, component/input-policy tests, and a production Vite build.
bash scripts/frontend.sh check

# Export the trusted development certificate and start Vite at https://localhost:5173;
# /api and /health proxy to https://localhost:5080.
bash scripts/frontend.sh dev

# Produce one clean artifact containing LinguaDesk.Api and generated web assets.
bash scripts/publish.sh

# Republish, isolate the artifact, launch owned Kestrel, and run Chromium smoke.
bash scripts/frontend.sh smoke
```

Start the development API separately with `bash scripts/backend.sh run` after trusting the certificate once with `dotnet dev-certs https --trust`. Both wrappers fail with a concise instruction when the trusted certificate is absent. Override the Vite proxy only when needed with an HTTPS `LINGUADESK_API_BASE_URL`. The published shell needs neither Vite nor Node at runtime. Frontend JUnit reports are written to `artifacts/test-results/frontend-unit.xml` and `frontend-e2e.xml`; Playwright screenshots/traces use `artifacts/playwright/`. The published smoke creates an owned one-day loopback certificate, trusts it only in its Playwright process, seeds verified and unverified synthetic accounts into its isolated migrated database through a non-hosting Smoke-only process mode, and verifies the real capability and account routes without exposing a test endpoint. M002 evidence is tracked in [`docs/08-backlogs/M002/tasks.md`](docs/08-backlogs/M002/tasks.md); M005 contract evidence is tracked in [`docs/08-backlogs/M005/tasks.md`](docs/08-backlogs/M005/tasks.md).

## Documentation and milestone context

Start at [document #0](docs/00-SDD-Planning-Workflow.md). Planning and execution use separate reading sets; each milestone keeps `backlog.md`, `spec.md`, `plan.md`, `tasks.md` and its selected source manifest together under `docs/08-backlogs/<ID>/`. See [context commands and rationale](automation/context-guide.md) and [automation usage](automation/README.md).
