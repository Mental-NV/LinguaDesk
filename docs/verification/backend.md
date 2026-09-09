# Backend and independent AI verification

Authoritative continuation of [06-verification-plan.md](../06-verification-plan.md); section numbers refer to that document; read only when relevant to the selected scope.

## 3. Deterministic backend and independent AI verification

### 3.1 Database, HTTP and published-host harness

Use `WebApplicationFactory`/in-process `TestServer` by default for HTTP boundary tests. Direct persistence tests can construct `DbContext` without an HTTP host. TestServer removes listening-port conflicts; determinism and parallel safety still require isolated database paths, users/configuration, fake time, and controlled external responses.

Use the same EF SQLite provider and migrations as production. File-backed databases in unique temporary directories with WAL are required for concurrency, locking, crash/restart, and migration tests. Each concurrent request has its own context/connection; use barriers/deferred fakes rather than sleeps. SQLite in-memory may be used for focused relational cases without file/locking claims, with an explicitly owned connection lifetime; EF's InMemory provider and mocked `DbSet` are not substitutes. Tests may share helpers, not mutable cross-test state.

Test fresh migration and upgrade from the last released schema with representative account/ledger data; before the first release, test fresh schema plus any data-transforming migration with its prior schema. Assert retained data and constraints and check pending model changes. Simulate failures at [architecture Section 7.2](../03-architecture.md#72-failure-windows-and-limits-of-idempotency)'s commit boundaries; a small process-restart test verifies persistence survives beyond the host instance.

Use fake provider/email adapters and sanitized transport fixtures for adapter parsing/error tests. Most authorization tests may use a controlled principal, but focused tests must use the real configured cookie/bearer handlers, antiforgery, verification gate, and token flows. A test-auth handler alone cannot prove authentication security.

Browsers cannot connect to in-memory TestServer. Integrated smoke therefore launches an owned Kestrel process on an OS-assigned/discovered port, with isolated migrated storage and test-only external adapters. Seed a verified local test account and exercise the real cookie login; use loopback HTTPS with a test certificate trusted only by the harness when exercising Secure cookies. Check readiness, dispose the process on failure, and never replace the browser's `/api` calls in this suite. Test adapters must be inaccessible in production configuration. Fixture-only browser contracts remain distinct.

The in-process HTTP harness follows [ASP.NET Core integration testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0). Database fidelity follows [EF Core testing guidance](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy): alternate providers cannot prove production-provider behavior. The concrete isolation and workload rules above are LinguaDesk decisions.

### 3.2 High-risk accounting and privacy cases

V-005/V-006 exercise simultaneous identical identities, conflicting payloads, multiple users sharing the global ledger, separate request contexts, insufficient remaining capacity, rollback and duplicate/late settlement. Use #5's canonical matching and UUID validity rules, including equivalent JSON escapes, changed newlines/modes, account scoping, expired identity after cleanup, original-day character attribution and per-attempt monetary periods. Assert both returned outcomes and durable ledger/exposure state.

Inject failures before dispatch, after uncertain dispatch, before terminal commit and after commit before response delivery according to #3 Section 7.2. Restart with the same file to prove fencing and reconciliation; a missing status is never evidence of zero charge. Status reads and usage refreshes must make zero model calls. Exercise deadline-versus-success races and client disconnect independently. A successful stale result still settles once while frontend revision checks prevent text replacement.

V-015 uses distinctive synthetic source/result/secret sentinels and inspects database tables, logs, traces, error exports, browser URL/history/storage/cache, and release backup artifacts. Trigger success, rejection, timeout, adapter failure and interrupted recovery. Confirm permitted operational metadata remains usable without text. Provider-managed cache observations do not establish application persistence or reduce character charges. Account deletion/backup retention details still depend on Q-004; their absence blocks the affected readiness check rather than being assumed safe.

### 3.3 Independent AI workflows

`LinguaDesk.Infrastructure.Ai` and its tests must build/run without Api, EF/Identity, frontend, browser, production database, credentials or provider network. `LinguaDesk.Ai.Evaluation` uses the same composition, prompts, validators and configuration checks as serving. Offline CI disables outbound provider access; inspect prompts with synthetic input and test the adapter through its actual transport serialization. This boundary is required by #3/#4, not a second application architecture.

| Workflow | Inputs and effects | Completion evidence |
| --- | --- | --- |
| Build/fast AI tests | Ai/Core, scripted `IChatClient`, fake admission/time; no secrets/network | Deterministic MSTest results; failing assertions are nonzero exit |
| Inspect prompt / validate configuration | Synthetic fixture plus bundle/profile | Exact serialized prompt/settings inspectable locally; no provider call; invalid config is nonzero exit |
| Adapter conformance | Synthetic HTTP fixtures through real adapter and fake `HttpMessageHandler` | Request body/options, response mapping, bounded reads, cancellation and one-dispatch assertions |
| Live candidate evaluation | Explicit live mode, allowlisted corpus, provider credentials and finite run budget | Machine-readable report identifying actual live calls, usage/exposure, errors and quality results |
| Chain evaluation | Configured pair, same orchestrator; controlled primary failures plus live qualification separately | Fallback ordering/dispatch count and behavior with reused eligibility; primary success cannot hide an unqualified fallback |
| API/UI integration | Sections 3–4 and 6 | Auth, durable accounting, wire errors, result application and end-to-end latency; separate from standalone AI evidence |

The implementation must supply noninteractive entry points for these workflows and document actual commands in #10 once they exist. Do not present hypothetical `dotnet` commands as runnable today. Offline tests are the ordinary development/PR default. Missing credentials must not turn a requested live run into a fake passing report; report it as blocked/non-success.

Live runs require a declared maximum spend, dispatch count and concurrency. Include generation, failed attempts and any paid graders in admission; reserve before each call, including parallel cases. Use separate evaluation credentials/billing scope where available. If sharing a serving billing scope, integrate with its global monetary admission; a standalone in-memory budget cannot protect concurrent production spend. Persist metadata-only unresolved evaluation exposure across restarts or require reconciliation before another live run. Development text may be inspected in active console memory; it must not become a hidden production-text collection feature.
