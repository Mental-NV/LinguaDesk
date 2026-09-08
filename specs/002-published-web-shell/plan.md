# 002 — Published Web Shell: Implementation Plan

**Version:** 1.1 · **Updated:** 2026-09-08
**State:** Implemented and verified; runtime evidence recorded
**Inputs:** [spec.md](spec.md) v1.1 and [BI-002](../../docs/08-backlogs/M002-published-web-shell.md) v1.1

## 1. Current state and tooling

At baseline `ac1552780a0a4114308c39385c67d2a9bec7d446`, Program.cs maps liveness and API Problem Details catch-alls only. The Web SDK project targets net10.0. `HttpBoundaryTests.cs` has nine cases, including bare-host `/`, `/weatherforecast` and `/sample` 404s. `scripts/backend.sh` owns locked setup/build/test, a minimum-test guard and a 30-second backend smoke. Reuse these boundaries; do not replace the working backend or its dependency pins.

Authoring inspection found Node **25.8.1**, npm **11.11.0**, and cached Chromium revision 1228. Execution selected and verified **Node 24.20.0 LTS**, npm **11.11.0**, Playwright **1.63.0** and its Chromium revision 1243 without replacing global tooling. Node/npm are pinned in repository configuration and the installed dependency graph is locked.

Verified dependency pins include React/react-dom 19.2.4, react-router-dom 7.13.2, Vite 8.0.3 with plugin-react 6.0.1, TypeScript 5.9.3, Vitest 4.1.2, Testing Library React 16.3.2/user-event 14.6.1, jsdom 27.4.0, Playwright Test 1.63.0, ESLint 9.39.4 and typescript-eslint 8.58.0. Remaining direct pins are recorded in the manifest; `package-lock.json` and `npm ci` provide the verified graph.

Locked dependency restore, frontend build and browser execution completed during implementation. Runtime results are recorded in [tasks.md](tasks.md#3-completion-record).

## 2. Changes and implementation boundaries

| Target during implementation | Purpose |
| --- | --- |
| `frontend/package.json`, lockfile, Node pin and TS/Vite/Vitest/ESLint configuration | Reproducible client build/check stack; TypeScript strict; browser runtime pinned; no frontend tooling in backend-only commands |
| `frontend/index.html`, `src/main.tsx`, `src/shell/` | React root, small browser router, native shell links, staged routes, heading-focus behavior and CSS using UX tokens |
| `frontend/tests/unit/` | Component route/semantic/unavailable-state assertions using Testing Library |
| `frontend/tests/e2e/`, Playwright configuration | Small actual-published-host navigation, asset-boundary and geometry smoke; machine-readable report and failure diagnostics |
| `backend/src/LinguaDesk.Api/Program.cs` | Serve the built web root and eligible SPA navigation while keeping API/health/asset namespaces distinct |
| `backend/tests/LinguaDesk.Api.Tests/HttpBoundaryTests.cs` | Preserve no-assets baseline semantics explicitly and add relevant server boundary assertions without npm/browser dependency |
| `scripts/frontend.sh`, `scripts/publish.sh`, small owned-host test launcher if needed | Setup/check/dev, one explicit full publish, and a published-artifact browser smoke |
| `.gitignore`, `README.md` | Ignore generated frontend/webroot/publish/test output and document only verified commands and scope |

Use React Router's browser and test-memory routing, native link rendering and replace redirects; no hand-rolled router, fake auth context, business fetch wrapper, state-management framework or component library. `/translate` and `/rewrite` lead to the signed-out shell only. Component tests cover the specified informational content and route decisions; real browser tests own history, focus and document refresh. Use system fonts/text wordmark and local assets; no remote fonts, analytics or service worker.

### Publishing and server boundaries

Use an explicit publish script: locked npm install as needed → type/lint/tests through the check command → Vite production build into `frontend/dist` → synchronize generated files into the Api `wwwroot` → invoke `dotnet publish` to a clean owned artifact directory. Frontend compilation happens once per publish, **before MSBuild evaluates/collects static assets**, so first publish from an empty checkout includes them. Do not add an unconditional npm MSBuild target to ordinary dotnet build/test. `wwwroot` and the publish destination are generated-only for this increment; validate their known paths and remove only owned stale generated content, never arbitrary user/source directories. Publication must fail on any failed step, not serve a previous successful artifact.

Serve static files and apply a low-priority `MapFallbackToFile("index.html")` only to eligible GET/HEAD client navigation. Explicitly exclude reserved `/api`, `/health` and `/assets` namespaces, including extensionless missing assets, and file-like paths. Existing `/health/live` POST must still return 405. Do not let a new catch-all defeat exact-route method behavior. Preserve API Problem Details behavior for all relevant methods, with no HTML substitution. Inspect/test route precedence and reserved-prefix boundaries rather than assuming middleware source order proves them.

No frontend assets means the bare host still has no page to serve. Make the existing HTTP fixtures use an explicitly owned empty web root so tests do not change depending on whether publish previously ran; preserve their health/API/isolated-instance assertions. Add targeted populated-root boundary fixtures where useful; **real generated asset packaging is proved by the published artifact**, not by hand-authored fake index files. Published `/` now serves the shell document; extensionless unknown pages render client not-found. Those are intentional later behavior changes, not a reason to erase M001 evidence or drop its other checks.

Run the published DLL with its own content root/working directory from an isolated output location, with repository frontend/dist/source assets unavailable to it. Test referenced hashed JS/CSS and stale asset removal. This catches wrong web-root resolution, missing first-build files and accidental dependence on source-tree content. Node is a build/test prerequisite, never a serving dependency.

### Local development and planned command contract

Names below are planned, not implemented commands. All wrappers resolve their own repository root and preserve meaningful exit codes.

| Command | Intended behavior |
| --- | --- |
| `bash scripts/frontend.sh setup` | Verify Node/npm pins, npm ci; install/verify the matching Chromium in an explicit setup step, not during an ordinary backend check |
| `bash scripts/frontend.sh check` | Strict typecheck, lint, noninteractive component tests with zero-test failures and actual counts; production frontend build |
| `bash scripts/frontend.sh dev` | Loopback Vite, with `/api` and `/health` proxied to the separately started M001 host; no broad CORS, fake API or automatic paid request |
| `bash scripts/publish.sh` | Produce the single complete host/static artifact using one fresh frontend build and locked backend restore; no deployment |
| `bash scripts/frontend.sh smoke` | Use a freshly published artifact; launch its owned Kestrel on an OS-assigned loopback port, run the focused pinned Chromium suite and clean up |

Keep the working `backend.sh` commands independent. Do not put frontend build/browser downloads inside its 30-second backend smoke. The published-shell smoke has a separate bounded setup/readiness/test timeout, recorded in its implementation and within the milestone budget; downloads happen in initial setup. Use bounded readiness, per-test timeouts, teardown on failure/interruption and no fixed shared listening port or `/api` interception. Do not reuse an unrelated running development server as evidence.

## 3. Verification and regression allocation

| Acceptance | Lowest sufficient evidence |
| --- | --- |
| AC-001 | Pinned install/type/lint/component results plus inspection of a fresh publish artifact |
| AC-002 | Parameterized DOM route/content checks; one published route smoke for actual mount/hydration |
| AC-003 | Browser link/Back/Forward/deep-link/reload/focus checks against the published host |
| AC-004 | Focused HTTP tests and actual-artifact HTTP probes for reserved namespaces, missing assets/files and method/liveness behavior |
| AC-005 | Chromium desktop/narrow/320px geometry and keyboard/skip-link checks; inspect screenshots without starting a new permanent baseline matrix |
| AC-006 | Clean and repeated publication with controlled stale generated-asset fixture; referenced assets load when served away from the repository |
| AC-007 | Existing backend regressions with absent/isolated frontend output; published real-host smoke and controlled failure cleanup |
| AC-008 | README command verification, actual test counts/reports, traceable completion record and scoped shared-document updates |

#6 remains the test strategy owner. No full editor/account accessibility journey, curated production screenshot matrix, manual AT certification or live service gate is selected. Current browser assertions are deliberately small; do not replay each DOM permutation across browsers. M001's root/unknown-page assertions must distinguish empty-root backend behavior from the newly published SPA behavior. Record why expectations changed and keep minimum-test-count checks accurate without using count alone as proof of coverage.

## 4. Human steps, timing and risk

**Feature-specific human actions: none, at either beginning or end.** The execution preflight must provision/verify Node 24.20.0, npm pins, packages and matching Chromium before dependent coding. Existing Node 25/cache presence is not sufficient. Use authorized autonomous setup; batch any indispensable human-only installation/access request at the beginning. No certificate, real email, provider account, domain or deployment permission belongs to this scope. Unexpected nonblocking human steps are prepared for the end under #0, with affected evidence pending.

M001 consumed 22m18s. M002 adds a toolchain and a publish boundary, so **do not assume its size is proven**. Initial full-envelope allowance: preparation/tooling/readiness 5 minutes; shell/configuration 6; publishing/boundary integration 6; scoped tests and regression runs 8; correction/docs/closeout 5. Count necessary package revisions, downloads and test waiting. Before execution, if prerequisites or the full increment cannot credibly fit 30 minutes, split M002 into fresh dependent milestone IDs under #7; do not weaken acceptance or silently treat a half-working publish as done. No split is claimed necessary solely from an unmeasured estimate, but readiness is conditional on this explicit check.

No deployment, persistent data migration, credentials or paid calls are involved. Cleanup owns generated output only; reports should contain synthetic shell content and relevant tool/runtime/commit data. Do not modify a completed M001 package to make new behavior look previously verified.

## 5. Review and handoff

Behavior, boundaries, tasks and scope references are aligned; no new product feature or generated wire schema was introduced. Toolchain compatibility and all selected runtime checks passed. [tasks.md](tasks.md#3-completion-record) records actual commands/counts/reports and published/browser outcomes; #6/#7, BI-002 and README link the verified result.
