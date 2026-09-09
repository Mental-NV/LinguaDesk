# 001 — Backend Foundation: Implementation Plan

**Version:** 1.2 · **Updated:** 2026-09-09
**State:** Implemented and verified; runtime evidence recorded
**Selected scope:** [spec.md](spec.md) v1.1, US-001 / AC-001–006; [BI-001](backlog.md) v1.1

**Policy maintenance — 2026-09-09:** Removed superseded milestone duration rules under #0 v1.7. Completed scope, acceptance results and measured durations are unchanged.

## 1. Context and readiness

The repository at `2cd06b1333c710f9db36fddb47137658ce4f112b` contained only shared specs and the roadmap. No applicable repository/ancestor AGENTS.md or existing build convention was found. Read-only inspection on 2026-09-08 found SDK `10.0.302`, runtime `Microsoft.AspNetCore.App 10.0.10`, and macOS arm64. The package pins below exist in the local NuGet cache; transitive restore, compilation, test discovery and process execution have not been verified. Do not equate installed tooling with a passing build.

The [specification](spec.md) fixes acceptance; [architecture #3](../../03-architecture.md) fixes Minimal APIs, MSTest, project boundaries and ordinary backend independence. This plan introduces only the two projects with actual responsibilities: `LinguaDesk.Api` and `LinguaDesk.Api.Tests`. Core policy begins when there is policy to implement; AI, EF tools, frontend and empty parallel test projects remain later milestones. Existing ADR-005 covers the harness choice; no consequential architecture replacement requires another ADR.

## 2. Technical decisions and targets

| Implemented target | Responsibility |
| --- | --- |
| `global.json` | Pin observed SDK `10.0.302`, disallow prerelease and use `rollForward: disable`; a missing SDK is diagnosed rather than silently selecting another |
| `backend/LinguaDesk.slnx` | Include only the host and its actual HTTP test project |
| `backend/Directory.Build.props` | Nullable, implicit usings, fixed .NET analyzer level `10.0` with recommended analysis; no floating language/analyzer defaults |
| `backend/Directory.Packages.props` | Central explicit versions for current test dependencies only |
| `backend/src/LinguaDesk.Api/` | `net10.0` Web SDK project, minimal entry point, process probe and absent-route error handling |
| `backend/tests/LinguaDesk.Api.Tests/` | `net10.0` MSTest project, actual host reference and isolated `WebApplicationFactory` checks |
| `scripts/backend.sh` | Thin noninteractive setup/check/run/smoke entry point with root resolution, clear failures and owned-process cleanup |
| `.gitignore`, per-project `packages.lock.json` | Ignore generated build/test/temp output; commit resolved package locks for reproducible restore |
| `README.md` | #10's working backend-only setup/check/run/smoke instructions, scaffold limitations and current evidence links |

Implemented package pins: `Microsoft.AspNetCore.Mvc.Testing` **10.0.10**, `Microsoft.NET.Test.Sdk` **18.0.1**, `MSTest.TestFramework` and `MSTest.TestAdapter` **4.0.2**. The ordinary VSTest-compatible `dotnet test` path is used; no runner migration or extra coverage/reporting package was needed. Locked restore, compilation and test execution verified their compatibility. These versions are deliberate pins, not a claim that they are the latest.

Use built-in health-check registration/mapping for `GET /health/live`, limited to GET so POST yields 405; no database/provider registrations or custom health framework. Return the plain health body without exposing metadata. Allow the test project to reference the real entry point with the minimal visibility hook needed by `WebApplicationFactory`.

Register the absent `/api` and `/api/{**path}` boundary as a 404 Problem Details response; do not redirect or return HTML. Leave other missing paths to ordinary 404 handling and avoid the sample weather endpoint, Swagger UI, HTTPS redirect middleware and launch-profile certificate requirements in this local scaffold. No product capabilities are served; the operational route is outside the product schema. M002 adds SPA hosting; M005 or an earlier selected product slice introduces reviewed generated OpenAPI. Production protocol/auth/data controls remain governed by #3/#5 when those capabilities are introduced.

## 3. Verification and command contract

The following implemented interfaces are exposed by `scripts/backend.sh`, which runs from any working directory by resolving the repository root. No additional script framework or global tool installation is required.

| Planned invocation | Required behavior |
| --- | --- |
| `bash scripts/backend.sh setup` | Check the pinned SDK, restore with committed locks in locked mode after initial lock creation, and fail with an actionable error if tooling/packages are unavailable |
| `bash scripts/backend.sh check` | Build in Release with analyzers and run all current backend tests; emit a machine-readable test report and fail if no intended tests run or a test/build fails |
| `bash scripts/backend.sh run` | Start the backend in the foreground on an explicit loopback HTTP address; default local port is documented and configurable; a conflict fails visibly |
| `bash scripts/backend.sh smoke` | Start the real built host on `127.0.0.1:0`, discover its actual port, verify the liveness response and stop its owned process on success/error/interruption |

Keep setup/restoration outside repeated `--no-restore`/`--no-build` checks where appropriate without hiding missing prerequisites. The aggregate check must not report success if the test runner discovered zero tests. A controlled disposable failure validates exit-code propagation; do not leave an intentionally failing test in the committed suite.

Use one factory per independently owned fixture with explicit environment and disposal. Verify probe success/method behavior and the missing API/non-API cases; test the actual request pipeline, not a manually returned health string. No unit test is needed solely to test framework health internals. Two simultaneous factory instances demonstrate lack of fixed listening-port/shared-state dependence; no database is invented for isolation.

The real-process smoke complements TestServer, which does not prove Kestrel listening or process cleanup. Bound readiness polling and the entire smoke to 30 seconds, use short per-probe request timeouts, and terminate the owned process/tree in cleanup. Start the built application directly so process ownership is unambiguous; discover its ephemeral port rather than reserving and releasing a port. Cleanup must also work on partial startup/failure. This is a harness bound, not a product latency target. It verifies local HTTP only, not production TLS or the future published SPA pipeline.

Scoped allocation: AC-001/004/006 use build, dependency inspection and evidence; AC-002/003 use V-009 HTTP-boundary checks; AC-005 uses only V-012's real-process lifecycle. Frontend/browser, database/migration, auth, AI/live-quality, cost and complete schema/client gates remain pending for later milestones; M001 does not silently pass or reduce them.

## 4. Rollout, data and diagnostics

There is no production rollout, migration or retained application data in this increment. Use temporary output owned by each verification run and preserve unrelated workspace files/processes. The scaffold has no request-body logging, provider/email credentials or source/result handling. Keep reports free of environment dumps and secrets; record only relevant versions, commands, commit and results. README must clearly state that liveness is not service readiness and business APIs/UI remain absent.

At closeout, update tasks and BI-001/M001 states and link the actual evidence in #6. Use the selected package's completion record in tasks.md for the command/result summary, linking generated test reports rather than inventing an additional status file. Update shared behavior only if implementation revealed a real design change; a generated artifact never changes authority by itself.

## 5. Budget, risks and human handoff

**Human actions: none required or deferred for this scope.** At the beginning, recheck SDK/package access, available shell/curl and loopback process permissions. These are autonomous checks. Resolve routine issues with existing authorization. If human-controlled access/install action is indispensable, request it in the initial preflight and do not start dependent work. Queue any unexpected nonblocking human step to the end with the prepared action and affected evidence; if nothing independent can proceed, report the blocker rather than inventing success. No certificate trust prompt, email-link click or paid-service consent should arise in M001.

Execution readiness includes tooling/access, coherent scope and all required verification. The 2026-09-09 workflow amendment removes the former fixed-duration allowance and time-based stop/split rules. Recorded durations remain historical evidence. Complete acceptance and applicable checks before closeout; preserve incomplete work and describe any real blocker honestly.

Main risk is restore or harness behavior consuming the reserve. Probe access early; avoid live dependencies and broad test infrastructure. If pins/restoration require investigation beyond a narrow correction, keep implementation unstarted and resize the milestone. A later missing human prerequisite must not turn into repeated mid-run questioning.

## 6. Source checks and review

Microsoft documents [WebApplicationFactory/TestServer](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0) as the HTTP integration boundary, [health checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0) as host probes, and [global.json](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json) SDK selection/roll-forward rules. Checked 2026-09-08; use these capabilities within the repository's narrower choices above.

Execution review: the story, six scenarios, implemented targets, tasks and scoped #6 references agree; the probe/loopback boundary remains as recorded in #3. Locked restore, clean Release build, nine HTTP tests, repeated isolation checks and both real-process smoke paths passed. No product schema or deferred feature was added, and no human follow-up is required. See the [completion record](tasks.md#3-completion-record).
