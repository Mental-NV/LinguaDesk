# 003 — Durable Storage Foundation: Implementation Plan

**Version:** 1.1 · **Updated:** 2026-09-09
**State:** Implemented and verified; runtime evidence recorded
**Inputs:** [spec.md](spec.md) v1.1; [BI-003](../../docs/08-backlogs/M003-durable-storage-foundation.md) v1.1

## 1. Current state and tooling

Baseline `003f355b4f9073e5d9ab6e3792c046a92e44a378` contains the .NET 10 API and MSTest project, central package management/lockfiles, the published React shell and backend/frontend/publish commands. SDK `10.0.302` is installed; the local dotnet tool list is empty. There is no DbContext, EF tool manifest, migration chain or application database. `Program.cs` currently registers only host services; preserve its explicit API/asset/health fallback boundaries.

Use EF Core SQLite and Design **10.0.10**, with repository-local `dotnet-ef` **10.0.10**, aligned with the existing ASP.NET package patch. Add exact central pins, project references, a local tool manifest and regenerated locks; keep the Design package private to development tooling. Package existence was checked against the official [Design package](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design/10.0.10) and [EF tool package](https://www.nuget.org/packages/dotnet-ef/10.0.10). These are selected compatible pins, not a claim to use the newest release; actual restore/build evidence is recorded in [tasks.md](tasks.md#3-completion-record).

Execution verified those EF pins. NuGet audit rejected EF's transitive `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 because of GHSA-2m69-gcr7-jv3q, so the implementation directly pins the compatible bundle to 2.1.12; locked restore resolves its core, provider and native library to 2.1.12 without changing the selected EF version.

## 2. Persistence and initialization design

Place `LinguaDeskDbContext`, typed storage options, registration/connection setup, design-time factory and migrations under `backend/src/LinguaDesk.Api/Infrastructure/Persistence/`. Use built-in scoped DI; never share a context concurrently. Keep EF out of a future Core project. No repository abstraction, mediator, separate persistence library or empty feature layer is needed.

Bind `Storage:DatabasePath` (`Storage__DatabasePath`) to an absolute local file path, defaulting to `/var/lib/linguadesk/linguadesk.db` as architecture Section 6.1 requires. Reject empty, relative, URI and in-memory targets. Local instructions use an explicit durable directory outside `wwwroot`, `frontend/dist`, `artifacts` and published output; tests use uniquely owned temporary directories. Validate against known static/output locations so a configured database cannot accidentally be published or served. Use a connection-string builder, never concatenate a path into a connection string. Do not create a production directory during local verification.

Runtime connections use `Mode=ReadWrite`; only explicit migration tooling uses `ReadWriteCreate`. Configure `Foreign Keys=True` on every connection and `Default Timeout=5` seconds as an initial finite busy-wait setting. This is not the operation deadline or a new product SLA; revisit it when request transactions are selected. Disable pooling for this initial boundary to make connection disposal/file ownership explicit. SQLite supports these settings through its documented [connection-string options](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings).

Register the context lazily. Ordinary host startup must not open, create or migrate a database. Opening a runtime context against a missing file fails rather than creating an empty substitute. The shell currently has no database-dependent operation, so do not make `/health/live` a storage probe or require a writable production path merely to build, publish or run the shell. Before database-dependent serving is added, implement the startup/readiness validation required by architecture Section 5.2.

Use `IDesignTimeDbContextFactory<LinguaDeskDbContext>` so EF commands can create a correctly configured context without starting Kestrel or the frontend. Share connection/path rules with runtime registration but distinguish migration creation mode explicitly. Require `--database-path <absolute-path>` for the design-time target; command wrappers must forward it as a separate quoted argument. Context construction must not itself open the file. See Microsoft's [design-time context guidance](https://learn.microsoft.com/en-us/ef/core/cli/dbcontext-creation).

Generate and inspect an initial migration and model snapshot. Establish WAL during explicit initialization (outside a migration transaction where necessary) and verify it from a fresh connection. The production baseline contains migration/provider metadata only, including any EF SQLite migration-lock table. It does not need a fake domain entity. Synthetic probe tables/rows belong exclusively to the isolated test fixture, created using the application context after production migrations; they must not appear in the checked-in model or migration. Reapplying migrations preserves those rows and the existing history.

Local initialization applies the migration chain explicitly; no `EnsureCreated`, `EnsureDeleted`, automatic startup migration, reset-on-error or downgrade command. Retain a failed target for diagnosis and propagate a nonzero exit. Applied/shared migrations remain immutable. Production bundle application, drain/backup and restoration remain architecture Section 6.1/M040 work, following Microsoft's [migration application guidance](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying).

## 3. Planned commands and affected components

These are implementation targets, not commands claimed to exist yet.

| Target | Intended change |
| --- | --- |
| Central packages, API project/lock and local tool manifest | Pin EF dependencies/tool; keep existing build/reproducibility settings |
| API `Infrastructure/Persistence/` and `Program.cs` | Shared path/connection policy, scoped context, independent design-time factory and explicit migration baseline; lazy host registration |
| `scripts/storage.sh setup` | Restore pinned local tools and locked backend dependencies; no database creation |
| `scripts/storage.sh migrate <absolute-path>` | Validate an explicit file target and existing parent, run EF database update with the selected factory argument, preserve exit status; no default-target or downgrade option |
| Backend test project and any small process-test helper | Migration/model drift, isolated relational and host restart assertions; reusable owned fixtures without public diagnostic endpoints |
| `scripts/backend.sh check` | Include storage cases in the existing noninteractive backend suite/report and retain meaningful discovery/failure guards |
| `.gitignore`, `README.md` | Ignore local database/WAL/SHM artifacts and document durable path, explicit initialization, checks and current scope |

No separate storage `check` command is needed: `bash scripts/backend.sh check` runs the storage suite without npm or browsers. Use a model drift assertion through EF's pending-model-change API against the checked-in snapshot. A model-only assertion must not create a file. The implementation may consolidate helper files; it must preserve this command/behavior contract.

## 4. Verification and evidence

[#6](../../docs/06-verification-plan.md) remains the strategy/evidence-matrix owner. Add focused tests to the existing MSTest project, using real file-backed SQLite and the production context/migrations. Do not mock DbSet or use EF InMemory/SQLite in-memory to claim durability.

- Fresh migration and repeated application: observe exactly the expected applied migration IDs; insert a synthetic fixture value, reapply and verify preservation. Compare the model with its snapshot without opening a database.
- Connection/transaction fidelity: use separate scopes/connections; verify WAL and foreign-key enforcement, a rejected invalid reference, commit persistence and rollback absence. Confirm the configured finite lock-wait behavior with a controlled contention case and bounded harness timeout; avoid brittle exact-time assertions.
- Failure/isolation: missing/relative/in-memory/unsafe-output targets, paths containing spaces and an unopenable target; runtime open of an absent database does not create it. Use an invalid target such as a directory instead of relying solely on permission bits under privileged test execution. Verify command failure propagation without touching unrelated data.
- Host integration and restart: first prove DI scopes resolve the intended file through the actual host factory. Then initialize/seed one owned file, launch a real Kestrel host configured with that path, probe liveness, stop it, launch a second distinct process with the same path, and inspect the original committed value/history through the production context. Run from owned paths with OS-assigned ports and bounded readiness/teardown. Record both process instances and database assertions. No new public storage-test endpoint is needed. This proves selected clean-restart retention; it does not prove future ledger recovery.
- Startup/publication boundary: point the ordinary host at a missing sentinel file and show startup/liveness do not create it. A migrated file is unchanged by startup. Run the existing backend checks/smoke and published-shell smoke; inspect that published/static outputs contain no database/sidecar files. Reuse M002's browser cases rather than add duplicate UI assertions.

Closeout records setup and locked check commands, counts/report locations, migration IDs/model drift result, actual package/tool versions, restart/failure results and revision/environment. Preserve test diagnostics on failure without logging real data. Cleanup releases contexts/connections and owned processes before removing only fixture-owned files. Existing command-level deadlines remain valid despite removal of the milestone duration limit.

## 5. Human steps, risks and readiness review

**Human actions: none required for this feature.** At the beginning, verify SDK/local EF restore, writable isolated file paths, process/loopback access and the existing published-shell test toolchain. Use authorized setup; batch any indispensable access request before dependent execution. No production credential, certificate, email or deployment action is needed. No planned end human check exists; unexpected nonblocking human dependencies follow #0 with prepared handoff and pending evidence.

Main risks are accidental implicit creation, incorrect path/connection quoting, WAL or FK assumptions, test file collisions and publish cleanup deleting durable data. The selected explicit target, read/write mode separation and file-backed negative/restart checks address them. Shared production recovery and database-dependent serving readiness retain their later gates; they are not considered verified here.

Review outcome: scope follows M003, all six scenarios map to tasks and passing checks, human timing is explicit, no product behavior or threshold was added, and the implementation respects architecture/API/privacy boundaries. Runtime evidence is recorded in [tasks.md](tasks.md#3-completion-record); there is no fixed duration gate.
