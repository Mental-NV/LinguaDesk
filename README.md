# LinguaDesk

LinguaDesk contains a minimal ASP.NET Core host and the M002 published signed-out web shell. The React shell provides informational sign-in and registration routes, redirects protected feature routes to sign-in, and is served from the same published artifact as the backend. It does not provide working accounts, editors, language operations, a database, provider/email integrations, OpenAPI, or deployment configuration.

## Prerequisites

- .NET SDK `10.0.302` (pinned by `global.json`)
- Node.js `24.20.0` and npm `11.11.0` (pinned by `frontend/.node-version` and `frontend/package.json`)
- Bash, curl, and `pkill` (normally supplied by the operating system or `procps`)
- Access to the locked NuGet and npm packages; Chromium is installed explicitly by frontend setup

Backend-only setup/check/smoke still requires no Node, frontend assets, browser, Docker daemon, database, credentials, external service, or development certificate.

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

`check` writes its TRX report to `artifacts/test-results/backend.trx`. Generated build, test, and temporary smoke output is not committed. Milestone scope and evidence are tracked in [`specs/001-backend-foundation/tasks.md`](specs/001-backend-foundation/tasks.md).

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
