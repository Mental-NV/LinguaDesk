# 002 — Published Web Shell: Selected Specification

**Version:** 1.0 · **Updated:** 2026-09-08
**State:** Selected behavior reviewed; implementation and runtime evidence pending
**Milestone/item:** [M002 / BI-002](../../docs/08-backlogs/M002-published-web-shell.md)

## 1. Selection and authority

Select BI-002: a basic signed-out SPA delivered by the existing host. [M001](../001-backend-foundation/tasks.md#3-completion-record) is its completed prerequisite. Baseline Git `ac1552780a0a4114308c39385c67d2a9bec7d446` contains the backend, nine HTTP checks and owned-process smoke; no frontend. The [backlog](../../docs/08-backlogs/M002-published-web-shell.md) records source revisions. Apply #0 v1.6, amended UX #2 v1.6, architecture #3 v1.8, API #5 v1.1 and verification/roadmap #6/#7 v1.3 as aligned with this package. Plans/tasks describe future work, not already available commands.

## 2. Selected story and staged route behavior

**US-001 — Open the application reliably.** As a visitor, I can open the published web shell, follow its links and refresh a deep link without an extra frontend server. I can distinguish unavailable account functionality from a functioning login or editor.

This milestone has no authentication state, token, fake verified principal or protected editor. Its signed-out route subset follows the staged boundary in [UX Section 3.1](../../docs/02-ux-specification.md#31-routes-and-account-access):

| Route | M002 observable shell |
| --- | --- |
| `/` | Replace navigation to `/login` |
| `/login` | Heading `Sign in`; message `Sign-in is not available in this build.`; link `Create account` to `/register`; no credential fields or submit action |
| `/register` | Heading `Create account`; message `Registration is not available in this build.`; link `Go to sign in` to `/login`; no account creation fields or submission |
| `/translate`, `/rewrite` | Replace navigation to `/login`; do not expose an editor or simulate access. Account return-path handling is implemented with the account milestone |
| Other eligible client paths | Heading `Page not found`; link `Go to sign in` to `/login`. No successful business response is implied by SPA document delivery |

Use the LinguaDesk text wordmark, native Translation/Rewriting links, a skip link and one `main`/`h1`. Route changes focus the destination heading; native links remain usable with keyboard and modified-click/new-tab behavior. No processing, account or usage request is triggered by navigation. The initial shell uses #2's existing colors, typography, focus treatment and responsive gutters. It has no result/workspace footer claiming an active editor session, no disabled faux forms and no later-feature widgets.

## 3. Selected acceptance

All scenarios belong to US-001/BI-002. Local AC IDs are qualified by this package, not replacements for UX-AC IDs.

| ID | Given / when | Observable outcome |
| --- | --- | --- |
| AC-001 | From clean frontend/publish outputs with pinned tooling available, the documented setup/check/publish sequence runs | Locked dependencies, type/lint/component checks and a production frontend build succeed; one publish artifact contains the backend and generated HTML/JS/CSS. No Vite server or Node process is needed to serve that artifact |
| AC-002 | A visitor opens `/`, `/login`, `/register`, `/translate` or `/rewrite` from the published host | The staged route table applies with visible headings/unavailable messages and no account fields, editor, authentication simulation or business requests |
| AC-003 | Follow shell links, use keyboard/Back/Forward, reload `/register`, or directly open an unknown extensionless page | Navigation/deep links work; the destination has one focused heading/main; unknown client pages show the shell's not-found state with recovery. Native link semantics and safe redirect history are preserved |
| AC-004 | Call the published host's `/api`, unknown `/api/...`, missing assets with and without extensions under `/assets/`, or a missing file such as `/missing.js` | API errors remain non-HTML 404 Problem Details; missing assets/files remain non-HTML 404. Non-navigation methods cannot receive SPA HTML. Probe GET remains `Healthy` and POST remains 405; unknown health paths never become SPA pages |
| AC-005 | Load the published shell at 1440×900 and 390×844; check 320 CSS px width and keyboard navigation | Wordmark/content/links are readable and unclipped, no horizontal page overflow, visible focus and working skip link, and the intended heading focus. Reduced motion does not prevent navigation. This is scoped shell evidence, not full browser/AT certification |
| AC-006 | Publish twice, including after removing a generated asset from the new frontend output; start the new artifact from outside the repository | Referenced JS/CSS load with correct non-HTML content types; removed assets are absent/404, and the host resolves its own published content without the source tree. Publication/check failure returns nonzero; cleanup affects only owned output/processes |
| AC-007 | Run ordinary backend setup/check/smoke with frontend dependencies/assets absent, and run a focused browser smoke against the published artifact | Backend commands still require no npm, browser, credential or certificate. Published smoke uses a real owned host and Chromium with no `/api` interception, no Vite server and no external runtime resources; failures propagate and resources are cleaned |
| AC-008 | Review commands and milestone evidence at closeout | README describes only verified local dev/check/publish/smoke behavior and shell limitations. Actual tool versions, commands, discovered test counts, report locations, publish/browser outcomes and elapsed time support all selected scenarios; M001 history is preserved |

HTTP fallback supplies the SPA document for eligible GET/HEAD client navigation. An unknown client page may have HTTP 200 document delivery followed by the client not-found state; missing API/assets must still be HTTP 404. This deliberately evolves M001's no-SPA missing-page behavior when assets exist, while preserving backend-only behavior with no assets. Record changed regression expectations in M002 evidence; do not rewrite M001's historical completion record.

## 4. Scope, verification and human steps

In scope: React/TypeScript/Vite shell, signed-out informational routes, static publishing and server route boundaries, focused component/browser tests and local commands. Account forms/real authentication remain M006–M014; editors/usage/AI remain later; full curated visual baselines and actual-device/AT release review remain #6's later evidence work. Existing privacy, accessibility and API boundary constraints apply to what is introduced now. No new product DTO or generated OpenAPI is needed for this static shell.

Use V-003 for DOM semantics, V-009 for HTTP errors, V-010 for history/focus/reflow and V-012 for real published delivery, with V-011's scoped visual inspection. This does not claim the full integrated auth/database/LLM journeys of those groups or pass FR-003/NFR-005/RG-006 in full.

**Human actions:** none specific to the feature. The executor owns Node/npm/Chromium provisioning and verifies access at the beginning; any indispensable human-controlled install/download permission is requested then. No certificate, real email or deployment action is requested mid-run. No planned human action is deferred to the end. Unexpected nonblocking dependencies follow #0's prepared end-handoff rule and remain pending evidence.

## 5. Readiness

M001 dependency is complete and the selected behavior is specified. [plan.md](plan.md) and [tasks.md](tasks.md) prepare the next increment. Toolchain provisioning, compatibility checks and the complete 30-minute feasibility assessment are required before implementation. If they reveal oversized scope, split before coding without deleting acceptance or weakening verification. All AC-001–008 are **Pending**.
