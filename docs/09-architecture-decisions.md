# LinguaDesk — Architecture Decision Records

**Document:** #9 · **Version:** 1.0
**Updated:** 2026-09-07

## ADR-001: Combined Hosting and SPA Fallback

**Context:** LinguaDesk requires a frontend React SPA and a backend API. Hosting these separately increases deployment complexity, CORS management overhead, and operational surface area.

**Alternatives Considered:**
1. Separate Node.js host (Next.js/Remix) for the frontend, communicating with the .NET backend.
2. Static hosting (e.g., AWS S3/CloudFront, Vercel) for the frontend, communicating with the .NET backend.

**Decision:** Build the React/Vite application into static files and publish them into the `.NET` backend's `wwwroot` directory. The ASP.NET Core host will serve the static files and use `MapFallbackToFile("index.html")` for SPA client-side routing.

**Consequences:**
* Simplifies deployment to a single artifact and single process instance.
* Eliminates cross-origin resource sharing (CORS) issues for the primary client.
* Requires careful ordering of static asset discovery during the build and publish steps to ensure frontend assets are packaged properly.

## ADR-002: SQLite WAL on Durable Volume

**Context:** LinguaDesk requires a durable, low-overhead relational database for user accounts and usage ledgers. The application targets a single instance.

**Alternatives Considered:**
1. PostgreSQL or MySQL (requires separate infrastructure provisioning).
2. SQLite without WAL (can face write concurrency contention).

**Decision:** Use SQLite with Write-Ahead Logging (WAL) enabled, stored on a persistent host volume (e.g., `/var/lib/linguadesk/linguadesk.db`). Migrations are managed via EF Core.

**Consequences:**
* Extremely low operational overhead and zero external database dependencies.
* WAL mode improves concurrent read/write performance suitable for the expected traffic.
* Precludes easy horizontal scaling to multiple application instances without migrating the database or using distributed file systems (which are often unsafe for SQLite). This matches the confirmed single-instance deployment constraint.

## ADR-003: Vertical Feature Slices without Generic Abstractions

**Context:** To optimize for maintainability and implementation by autonomous agents, the backend structure needs to minimize boilerplate and indirection.

**Alternatives Considered:**
1. Traditional N-Tier architecture with generic Repositories, Unit of Work, and MediatR.
2. Clean Architecture with fully separated domain, application, infrastructure, and presentation layers.

**Decision:** Organize the `LinguaDesk.Api` project using pragmatic vertical feature slices. Handlers will depend directly on the EF Core `DbContext` rather than abstracting it behind a generic repository. Domain logic that is completely framework-independent resides in `LinguaDesk.Core`.

**Consequences:**
* Reduces boilerplate and jumping between files for simple orchestration tasks.
* Makes the system easier to navigate and verify for automated agents.
* Cross-feature code sharing must be managed deliberately to avoid dependency cycles.

## ADR-004: Auto-Generated OpenAPI Contract

**Context:** Requiring an AI agent to manually synchronize C# backend changes with a static `docs/05-openapi.yaml` file introduces a high-friction failure point. Agents frequently update one side of the contract and forget the other, breaking the frontend build.

**Decision:** The OpenAPI specification will be auto-generated directly from the C# Minimal API definitions at build/startup time (e.g., using NSwag or Swashbuckle). A frontend tool (like Orval or RTK Query) will automatically generate TypeScript types and API clients from this generated schema.

**Consequences:**
* The single source of truth for the API contract is the executable C# code.
* Eliminates manual YAML authoring and synchronization errors.
* Ensures strict, immediate feedback for the agent when crossing the frontend/backend boundary.

## ADR-005: In-Memory Integration Testing via WebApplicationFactory

**Context:** When AI agents write API integration tests that bind to real TCP ports (e.g., `localhost:5000`), they frequently encounter port collisions, zombie processes, and parallel execution failures, leading to distracting infrastructure debugging loops.

**Decision:** All backend API integration tests must use ASP.NET Core's `WebApplicationFactory`. This spins up the application test host using an in-memory `TestServer`.

**Consequences:**
* Zero TCP port conflicts.
* Tests run faster and can be safely parallelized.
* The agent receives purely deterministic feedback focused on business logic rather than network environment issues.

## ADR-006: Stateless Schema Iteration via EnsureCreated

**Context:** EF Core Migrations are highly stateful, requiring CLI commands to add, apply, or remove migration files. When an AI agent rapidly iterates on a feature and makes incremental changes to entity models, it often botches the sequential migration chain, leading to broken snapshots and failed rollbacks that severely disrupt the autonomous workflow.

**Decision:** For local rapid development and integration testing environments, agents must use `context.Database.EnsureDeleted()` and `context.Database.EnsureCreated()` to continuously synchronize the database with the C# models statelessly. Generating an official EF Migration file (`dotnet ef migrations add`) is deferred until the **absolute final step** of the feature's development lifecycle.

**Consequences:**
* Agents can fluidly rename, add, or drop columns without fighting the EF CLI.
* Integration tests can instantly build the freshest schema without tracking migration history.
* The final generated migration file is clean, consolidated, and immutable, keeping the production migration history pristine.
* Production startup strictly uses `MigrateAsync()`; `EnsureCreated()` is forbidden in the production pathway.
