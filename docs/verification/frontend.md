# Frontend and browser verification

Authoritative continuation of [06-verification-plan.md](../06-verification-plan.md); section numbers refer to that document; read only when relevant to the selected scope.

## 4. Frontend, browser and manual verification

### 4.1 Shared UI fixtures

Scenarios are layer-independent contracts. Use pure MSTest/Vitest units for policy/revisions and Vitest + Testing Library for visible components, paused request counts and forms. Fake only external/transport boundaries appropriate to that layer. Integrated browser smoke uses real SPA/API/local auth/migrated SQLite and fake provider/email adapters; it never intercepts the application's `/api` calls. Deterministic tests never call live services or use arbitrary sleeps.

Keep fixed time `2026-09-07T17:40:00Z`, controlled deferred responses and fake timers for the one-second pause, deadlines/cooldowns/notices. Assert no dispatch before 1,000 ms, one dispatch at expiry for the latest eligible revision, timer restart on source/settings edits, cancellation on result edit/route leave, and no unchanged-revision repeat. Count logical transformations separately from auth, usage, eligibility and status reads. DOM/component fixtures may use verified local `writer@example.test`; published E2E cases use the single predefined verified account `m013-verified@example.test` / `Maple!River2026`, seeded through ASP.NET Identity into the run-owned migrated SQLite database with user usage 7,500 and available global/budget capacity. Seed prior results explicitly and exclude seed operations from observed counts.

The published harness creates the account before serving browser traffic through the existing `Smoke`-only offline seed mode; no seed endpoint exists and non-Smoke startup must reject the mode. One E2E case must visit `/login`, fill the predefined credentials, activate **Sign in**, and assert the protected route plus visible authenticated content. A Playwright setup/helper may authenticate the same account for subsequent cases by obtaining antiforgery state and posting to the real published sign-in API, then reusing the returned same-origin cookie storage state. It may not forge cookies, mutate auth/session rows, disable middleware, intercept account or feature APIs, or substitute a test host. Keep one clean run-owned account/database baseline per suite or isolate stateful cases so usage and operation history cannot leak an undeclared precondition.

Every UI-facing feature selected by a milestone has published E2E coverage using that account. Exercise controls through semantic role/name locators and assert the visible browser result, essential error, focus, native clipboard or cleared state relevant to the feature. Network request counts and database assertions supplement the UI assertion for timer and accounting behavior. The selected package maps each UI-facing feature to one or more E2E cases and allocates its lower-level acceptance assertions to the appropriate checks; combinatorial policy cases do not require duplicate browser scripts when the visible feature outcome is already covered.

Use T-OK-A: `Hello, the meeting starts at 14:30. Please go.` (46 ASCII chars), Romanian fixture `Bună, întâlnirea începe la 14:30. Te rog să mergi.`; T-OK-B: `The report is ready.` (20), Romanian `Raportul este gata.`. W-OK source: `The report is really ready. We sends it today.` (46); result: `The report is ready. We send it today.` (38). Result fixtures carry no sentence IDs. T-LONG is 5,312 `a` characters and must be rejected without provider work, not accepted/truncated; the excess 312 is asserted in internal policy, not a visible counter. Boundary fixtures are L−1/L/L+1 for both limits; #5 supplies exact Unicode expectations for emoji, combining marks, CRLF, tabs/spaces and Chinese. Use shared counting fixtures rather than JS string length assumptions.

Supported direction fixtures cover all 12 pairs using short equivalent sentences in the four languages. Rewrite fixtures include Traditional input with Simplified output and all nine dropdown choices. Language correctness is evaluation evidence in #6; fixtures establish transport and presentation only. For M006 and later local-password slices, use #5's selected 15–128 Unicode-scalar/no-composition policy; `Maple!River2026` is a valid 15-scalar synthetic fixture. Earlier references to a possible 12-character fixture minimum were nonbinding and are superseded by the selected API policy. Use invalid/expired token fixtures and controlled known/unknown-email outcomes.

Query by semantic role/name. Native selectors are asserted through value/options/disabled state and browser keyboard smoke, not custom menu internals. Do not preserve old sentence/boundary test IDs. Browser-only checks own actual clipboard, caret/selection, layout/history/storage and restoration; DOM emulators cannot prove them.

### 4.2 Browser lanes

PRD D-22 requires only pinned Playwright Chromium at desktop and narrow responsive viewports. Record the engine/runtime version and viewport in evidence. There is no required cross-browser, current/previous-version or physical-device matrix. Additional engines, branded browsers and devices are best-effort compatibility targets; missing access does not block RG-006. Engine/viewport evidence must not be relabeled as actual branded-browser or physical-device proof.

| Lane | Automated scope | Gate status |
| --- | --- | --- |
| Chromium desktop 1440×900 | Published core journeys, native editing/copy, focus/history/privacy, semantic/contrast scans and curated visuals per Section 4.4 | Required automated evidence; existing keyboard smoke may remain advisory per UX-AC-079 |
| Chromium narrow 390×844 (viewport emulation) | Published core journeys, reflow/native-control geometry, semantic scans and curated visuals | Required automated responsive evidence; no physical-device claim |
| Chromium 320×640 / 1024×768 | Targeted overflow/breakpoint, effective zoom and text-spacing checks; no extra screenshot matrix | Required where allocated by active UX acceptance |
| Firefox / WebKit / branded current or previous browsers / physical devices / AT | Optional compatibility and exploratory observations | Nonblocking; record tested versions and untested scope honestly |

### 4.3 Manual browser-matrix evidence

This heading is retained for existing references. D-22 removes the manual browser-matrix release requirement. Before RG-006, unattended Chromium checks cover current automatic-verification registration/sign-in, Translation, Rewriting mode selection, one-second paused processing, essential validation alerts, native copy and session expiry at desktop and narrow viewports. Real email confirmation/recovery evidence is deferred under DF-008.

Automated semantics, focus, contrast/reflow and live-region checks satisfy their allocated assertions without human verification. Screen-reader (NVDA/VoiceOver/TalkBack), native IME, forced-colors, physical-device and additional-browser checks are optional and nonblocking. Synthetic composition events prove dispatch guards only; they do not establish a real IME/platform pass. Record untested scope as untested, not as failed required evidence or a passing platform claim. Deferred sentence, comparison and custom-sheet journeys are excluded from current evidence.

### 4.4 Curated visual regression

Use **seven** initial Chromium baselines: Translation result and oversize-error layouts (each 1440×900 and 390×844), Rewriting with inline mode and a plain edited result (1440×900 and 390×844), and local registration with long validation errors (1440×900). Assert equal desktop editor-panel geometry at both feature desktop baselines. Add a baseline only for an uncovered layout risk. No reset dialog, sentence, comparison, Google, prefix, custom-dropdown or tools-sheet baseline ships now. Remaining combinations use DOM assertions or targeted browser geometry, not screenshots of every scenario.

Pin Noto Sans/Noto Sans SC, Chromium/container, scale factor 1 and deterministic synthetic content/time. Wait for fonts; hide caret; disable motion and control scrollbars. Native select popups are excluded from pixel capture. Start with per-pixel threshold 0.1 and max differing ratio 0.001; record measured reasons for adjustments. AI may inspect before/after images and accept an intentional baseline change with a recorded design rationale and passing independent structural/semantic checks. Unexplained differences remain failures; blanket regeneration or threshold changes solely to obtain green checks are prohibited. No human design approval is required. DOM emulators cannot prove layout.

Browser contracts use semantic locators and isolated contexts; prefer explicit readiness and controlled responses over timing sleeps. See [Playwright best practices](https://playwright.dev/docs/best-practices). Native clipboard, selection and restoration assertions use the actual pinned browser, not DOM emulation. Native IME/physical-device observations remain optional; lack of such access does not block RG-006. Missing required Chromium evidence still blocks its allocated gate.
