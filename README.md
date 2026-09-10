# LinguaDesk

LinguaDesk contains a minimal ASP.NET Core host, the M002 published signed-out web shell, explicit SQLite persistence, an independent offline AI development boundary, the M005 shared input/capability contract, and the M006–M009 local-account registration, email-verification, browser-session and bearer-access API slices. The React shell still provides informational sign-in and registration routes only. An independent client can register a durable unverified account, submit captured confirmation material, request another verification delivery, establish/read/end a secure cookie session, or sign in for opaque bearer access/refresh credentials and read current-account status through the account API. The runtime delivery adapter intentionally remains unavailable until M034 supplies real email, while deterministic tests inject capturing/failing adapters. Password reset, account-status UI, live email, editors, language-operation submissions, eligibility decisions, provider integrations, usage/accounting, and release deployment remain unimplemented.

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

# Explicitly prepare/update persistent local-development storage, then start
# the backend in the foreground at http://127.0.0.1:5080.
bash scripts/backend.sh run

# Build and start the real host on an OS-assigned loopback port, probe it,
# and terminate the process and temporary output it owns within 30 seconds.
bash scripts/backend.sh smoke
```

With no storage environment variables, `run` applies the migrations and creates a restricted key directory under the sibling `../.linguadesk-development` directory. This data and its verification keys survive restarts and are not committed. Override that location and the listening address when needed:

```sh
LINGUADESK_DEVELOPMENT_DATA_PATH="/absolute/path/to/linguadesk-development" \
LINGUADESK_URL=http://127.0.0.1:5090 \
bash scripts/backend.sh run
```

If `Storage__DatabasePath` or `Security__DataProtectionKeysPath` is supplied explicitly, `run` respects that target and does not initialize that custom dependency. Use `scripts/storage.sh migrate` and provision the custom key directory yourself. The convenience behavior belongs to the development wrapper; the application itself never creates or migrates missing serving dependencies.

`GET /health/live` remains process-only. `GET /health/ready` checks that the database still exists, has the current migration and can accept a rolled-back write, and that the configured key directory remains usable. It exposes only healthy/unhealthy and is excluded from product OpenAPI. Account-serving startup performs the same readiness validation before listening and never creates or migrates either dependency. Unsupported health methods are rejected with 405. Missing `/api` routes return a JSON Problem Details 404. With no generated frontend webroot, other unimplemented paths remain ordinary 404 responses; a published artifact serves the SPA document only for eligible GET/HEAD client routes. Missing API, health, asset, and file-like paths never fall through to HTML.

`check` writes collision-free TRX reports to `artifacts/test-results/backend-api.trx`, `backend-core.trx`, and `backend-ai.trx`. It retains the real file-backed SQLite migration, transaction, locking, failure, host-scope, and restart cases; exercises registration, confirmation, resend/cooldown, Identity, secure cookie sessions, bearer access/refresh, antiforgery, expiry/invalidation, verified-state authorization, key persistence and readiness; and separately guards the focused suites against zero-test success. It does not need npm or a browser. Generated build, test, and temporary smoke output is not committed. M009 scope and evidence are tracked in [`docs/08-backlogs/M009/tasks.md`](docs/08-backlogs/M009/tasks.md).

## Capability and contract commands

`GET /api/capabilities` is public and read-only. It returns JSON with `Cache-Control: no-store` and needs no account, database, frontend, provider, email service, or credential. The response is a catalog and validation contract only: Translation/Rewriting submission, usage, operation status, and provider operations do not exist yet. Bearer authentication exists as `POST /api/accounts/bearer-sign-in`, `POST /api/accounts/bearer-refresh` and `GET /api/accounts/me`.

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

The committed generated views are `docs/05-openapi.yaml` and `frontend/src/api/generated/linguadesk-api.d.ts`; do not edit either by hand. The document contains `GET /api/capabilities`, the three registration/verification operations, `GET /api/accounts/antiforgery`, `POST /api/accounts/sign-in`, `GET /api/accounts/session`, `POST /api/accounts/sign-out`, `POST /api/accounts/bearer-sign-in`, `POST /api/accounts/bearer-refresh`, and `GET /api/accounts/me`. It uses pre-release artifact revision `0.1.0-m009` and is not served as a runtime documentation endpoint. The generated security schemes document the host-scoped session and antiforgery cookies, the antiforgery header, and the opaque Identity bearer scheme. All account responses are no-store. Contract generation registers real endpoint metadata without opening storage, writing filesystem keys/cooldown state, sending email, issuing cookies, or starting a listener. Its revision does not promise API compatibility or a deprecation period.

Browser-session calls must use HTTPS so the `__Host-` Secure cookies are accepted. Bootstrap with `GET /api/accounts/antiforgery`, retain its HttpOnly cookie, and send the returned request token in `X-LinguaDesk-Antiforgery` on sign-in/sign-out. After identity changes, bootstrap again before the next mutation. Sign-in accepts only `{ "email", "password" }`; session cookies are nonpersistent and the protected ticket expires eight hours after issue without renewal. Independent clients sign in with only `{ "email", "password" }` at `POST /api/accounts/bearer-sign-in`, receive an opaque 15-minute access / 7-day refresh pair with `expiresIn` 900 and `tokenType` Bearer, refresh with the held `{ "refreshToken", "accessToken" }` pair, and read `GET /api/accounts/me` with `Authorization: Bearer ...`; bearer calls need no antiforgery token and never set cookies. Deploy behind HTTPS, or explicitly configure a local HTTPS Kestrel certificate/reverse proxy; the default HTTP development URL is suitable for non-cookie endpoints but cannot exercise browser session cookies.

## Independent AI development commands

The M004 inner loop uses `Microsoft.Extensions.AI.Abstractions` `10.9.0` and does not reference the API, ASP.NET Core, EF/Identity, a provider SDK, or the full AI middleware package. Run setup once when the locked graph is absent locally, then use the focused commands from any directory:

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
```

`check`, `inspect`, and `probe` clear common provider credential variables and point outbound HTTP proxies at an unreachable loopback address. They start no API/frontend process, open no database, and write no persistent evaluation report; `check` writes only generated build output and `artifacts/test-results/ai.trx`. The runner accepts only `inspect` or `probe`, exposes no live mode, and accepts no source, configuration, credential, or arbitrary text argument.

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

# Start Vite on loopback; /api and /health proxy to http://127.0.0.1:5080.
bash scripts/frontend.sh dev

# Produce one clean artifact containing LinguaDesk.Api and generated web assets.
bash scripts/publish.sh

# Republish, isolate the artifact, launch owned Kestrel, and run Chromium smoke.
bash scripts/frontend.sh smoke
```

Start the development API separately with `bash scripts/backend.sh run`. Override the Vite proxy only when needed with `LINGUADESK_API_BASE_URL`. The published shell needs neither Vite nor Node at runtime. Frontend JUnit reports are written to `artifacts/test-results/frontend-unit.xml` and `frontend-e2e.xml`; Playwright screenshots/traces use `artifacts/playwright/`. The published smoke also verifies the real capability route remains JSON/no-store and rejects POST without making the UI consume it. M002 evidence is tracked in [`docs/08-backlogs/M002/tasks.md`](docs/08-backlogs/M002/tasks.md); M005 contract evidence is tracked in [`docs/08-backlogs/M005/tasks.md`](docs/08-backlogs/M005/tasks.md).

## Documentation and milestone context

Start at [document #0](docs/00-SDD-Planning-Workflow.md). Planning and execution use separate reading sets; each milestone keeps `backlog.md`, `spec.md`, `plan.md`, `tasks.md` and its selected source manifest together under `docs/08-backlogs/<ID>/`. See [context commands and rationale](automation/context-guide.md) and [automation usage](automation/README.md).
