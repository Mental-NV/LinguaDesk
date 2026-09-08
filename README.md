# LinguaDesk

LinguaDesk currently contains the M001 backend foundation: a minimal ASP.NET Core host, deterministic HTTP integration checks, and a real-process smoke check. It does not yet provide a UI, database, accounts, language operations, provider/email integrations, OpenAPI, or production deployment configuration.

## Prerequisites

- .NET SDK `10.0.302` (pinned by `global.json`)
- Bash, curl, and `pkill` (normally supplied by the operating system or `procps`)
- Access to the NuGet packages pinned in `backend/Directory.Packages.props` (a populated local package cache is sufficient)

No Node installation, frontend assets, Docker daemon, database, credentials, external service, or development certificate is required.

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

The operational endpoint is `GET /health/live`. HTTP 200 with plain-text `Healthy` means only that the process can serve the liveness probe; it does not report database, provider, email, or product readiness. `POST /health/live` is rejected with 405. Missing `/api` routes return a JSON Problem Details 404, while other unimplemented paths (including `/` and `/weatherforecast`) remain ordinary 404 responses.

`check` writes its TRX report to `artifacts/test-results/backend.trx`. Generated build, test, and temporary smoke output is not committed. Milestone scope and evidence are tracked in [`specs/001-backend-foundation/tasks.md`](specs/001-backend-foundation/tasks.md).
