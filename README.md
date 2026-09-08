# LinguaDesk

LinguaDesk contains a minimal ASP.NET Core host, the M002 published signed-out web shell, and the M003 SQLite persistence foundation. The React shell provides informational sign-in and registration routes, redirects protected feature routes to sign-in, and is served from the same published artifact as the backend. Storage is initialized only through explicit EF Core migrations and currently contains framework migration metadata only. The application does not yet provide working accounts, editors, language operations, product data schemas, provider/email integrations, OpenAPI, or deployment configuration.

## Prerequisites

- .NET SDK `10.0.302` (pinned by `global.json`)
- Node.js `24.20.0` and npm `11.11.0` (pinned by `frontend/.node-version` and `frontend/package.json`)
- Bash, curl, and `pkill` (normally supplied by the operating system or `procps`)
- Access to the locked NuGet and npm packages; the repository-local EF tool and Chromium are installed explicitly by their setup commands

Backend-only setup/check/smoke still requires no Node, frontend assets, browser, Docker daemon, pre-existing database, credentials, external service, or development certificate. Storage checks create only uniquely owned temporary databases and processes.

## Backend commands

Run these commands from the repository root. The script resolves the repository root itself, so it can also be invoked by absolute path from another working directory.

```sh
# Verify the SDK and restore exactly the committed dependency graph.
bash scripts/backend.sh setup

# Restore, build in Release with analyzers, run all backend tests, and verify
# that the machine-readable report contains discovered tests and no failures.
bash scripts/backend.sh check

# Start the backend in the foreground at http://127.0.0.1:5080.
bash scripts/backend.sh run

# Build and start the real host on an OS-assigned loopback port, probe it,
# and terminate the process and temporary output it owns within 30 seconds.
bash scripts/backend.sh smoke
```

Override the foreground run address with `LINGUADESK_URL`, for example:

```sh
LINGUADESK_URL=http://127.0.0.1:5090 bash scripts/backend.sh run
```

The operational endpoint is `GET /health/live`. HTTP 200 with plain-text `Healthy` means only that the process can serve the liveness probe; it does not report database, provider, email, or product readiness. `POST /health/live` is rejected with 405. Missing `/api` routes return a JSON Problem Details 404. With no generated frontend webroot, other unimplemented paths remain ordinary 404 responses; a published artifact serves the SPA document only for eligible GET/HEAD client routes. Missing API, health, asset, and file-like paths never fall through to HTML.

`check` writes its TRX report to `artifacts/test-results/backend.trx`. It includes the real file-backed SQLite migration, transaction, foreign-key, locking, failure, host-scope, and clean-restart cases; it does not need npm or a browser. Generated build, test, and temporary smoke output is not committed. Milestone scope and evidence are tracked in [`specs/001-backend-foundation/tasks.md`](specs/001-backend-foundation/tasks.md) and [`specs/003-durable-storage-foundation/tasks.md`](specs/003-durable-storage-foundation/tasks.md).

## Storage commands

Storage uses EF Core SQLite `10.0.10`, the repository-local `dotnet-ef` `10.0.10` tool, and the committed migration chain. The native SQLite bundle is pinned to `2.1.12` to avoid the vulnerable transitive `2.1.11` package. Setup restores tools and locked backend dependencies but does not create a directory or database:

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

To run the current shell with that initialized file available to future database-dependent scopes:

```sh
Storage__DatabasePath="$storage_directory/linguadesk.db" bash scripts/backend.sh run
```

Ordinary build, publish, host startup, and `GET /health/live` do not open, create, or migrate storage. The default configured production path is `/var/lib/linguadesk/linguadesk.db`, but the current shell does not require that path to exist. Runtime access to an absent file fails instead of creating an empty replacement.

This foundation proves clean-stop local persistence, not power-loss durability, business transaction recovery, backup/restore, a production migration bundle, multi-instance/network-filesystem operation, or storage readiness for database-dependent serving. Those remain later milestones; no product entity or synthetic probe schema is present in the production migration.

## Frontend and publishing commands

```sh
# Verify Node/npm, install locked dependencies, and install matching Chromium.
bash scripts/frontend.sh setup

# Run strict TypeScript, ESLint, component tests, and a production Vite build.
bash scripts/frontend.sh check

# Start Vite on loopback; /api and /health proxy to http://127.0.0.1:5080.
bash scripts/frontend.sh dev

# Produce one clean artifact containing LinguaDesk.Api and generated web assets.
bash scripts/publish.sh

# Republish, isolate the artifact, launch owned Kestrel, and run Chromium smoke.
bash scripts/frontend.sh smoke
```

Start the development API separately with `bash scripts/backend.sh run`. Override the Vite proxy only when needed with `LINGUADESK_API_BASE_URL`. The published shell needs neither Vite nor Node at runtime. Frontend JUnit reports are written to `artifacts/test-results/frontend-unit.xml` and `frontend-e2e.xml`; Playwright screenshots/traces use `artifacts/playwright/`. M002 evidence is tracked in [`specs/002-published-web-shell/tasks.md`](specs/002-published-web-shell/tasks.md).
