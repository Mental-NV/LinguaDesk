# Frontend and browser verification

Authoritative continuation of [06-verification-plan.md](../06-verification-plan.md); section numbers refer to that document; read only when relevant to the selected scope.

## 4. Frontend, browser and manual verification

### 4.1 Shared UI fixtures

Scenarios are layer-independent contracts. Use pure MSTest/Vitest units for policy/revisions and Vitest + Testing Library for visible components, explicit request counts and forms. Fake only external/transport boundaries appropriate to that layer. Integrated browser smoke uses real SPA/API/local auth/migrated SQLite and fake provider/email adapters; it never intercepts the application's `/api` calls. Deterministic tests never call live services or use arbitrary sleeps.

Keep fixed time `2026-09-07T17:40:00Z`, UTC display assertions, controlled deferred responses and fake timers only for deadlines/cooldowns/notices. There is **no debounce timer**. Count logical transformations separately from auth, usage, eligibility and status reads. Default auth is a verified local `writer@example.test` with user usage 7,500 and available global/budget capacity; seed prior results explicitly and exclude seed operations from observed counts.

Use T-OK-A: `Hello, the meeting starts at 14:30. Please go.` (46 ASCII chars), Romanian fixture `Bună, întâlnirea începe la 14:30. Te rog să mergi.`; T-OK-B: `The report is ready.` (20), Romanian `Raportul este gata.`. W-OK source: `The report is really ready. We sends it today.` (46); result: `The report is ready. We send it today.` (38). Result fixtures carry no sentence IDs. T-LONG is 5,312 `a` characters and must be rejected with excess 312, not accepted/truncated. Boundary fixtures are L−1/L/L+1 for both limits; #5 supplies exact Unicode expectations for emoji, combining marks, CRLF, tabs/spaces and Chinese. Use shared counting fixtures rather than JS string length assumptions.

Supported direction fixtures cover all 12 pairs using short equivalent sentences in the four languages. Rewrite fixtures include Traditional input with Simplified output and all nine dropdown choices. Language correctness is evaluation evidence in #6; fixtures establish transport and presentation only. For M006 and later local-password slices, use #5's selected 15–128 Unicode-scalar/no-composition policy; `Maple!River2026` is a valid 15-scalar synthetic fixture. Earlier references to a possible 12-character fixture minimum were nonbinding and are superseded by the selected API policy. Use invalid/expired token fixtures and controlled known/unknown-email outcomes.

Query by semantic role/name. Native selectors are asserted through value/options/disabled state and browser keyboard smoke, not custom menu internals. Do not preserve old sentence/boundary test IDs. Browser-only checks own actual clipboard, caret/selection, layout/history/storage and restoration; DOM emulators cannot prove them.

### 4.2 Browser lanes

Support current and immediately previous stable major releases at each release candidate for Chrome, Edge, Firefox, desktop Safari, iOS Safari and Android Chrome. Record actual tested versions in the release evidence. Bundled Playwright Chromium/WebKit represent engines, not branded/browser-version or physical-device proof. Previous-major support requires bounded actual-version smoke or an explicit evidence gap before release support is claimed.

| Lane | Automated scope | Release/manual scope |
| --- | --- | --- |
| Chromium desktop 1440×900 | Core keyboard/focus/edit/copy/history/privacy subset, curated visuals per Section 4.4, small integrated smoke | Branded Chrome/Edge and keyboard/zoom smoke |
| Chromium narrow 390×844 | Curated visuals and ordinary reflow/native-control geometry | Android Chrome/TalkBack at 360×800 |
| Chromium 320×640 / 1024×768 | Small overflow/breakpoint checks, no extra screenshot matrix | Zoom and text-spacing checks |
| Firefox desktop 1024×768 | Short native-editor/selector/keyboard/composition-event smoke when affected or at release | NVDA and actual supported Firefox versions |
| WebKit desktop 1440×900 | Short focus/navigation/editor smoke when affected or at release | Actual macOS Safari/VoiceOver |
| iOS Safari 390×844 | No separate full automated narrow-engine suite | VoiceOver, editing/copy, native selects and software keyboard |

### 4.3 Manual accessibility and device evidence

Before RG-006, check local registration/login/recovery, Translation, Rewriting mode selection, explicit submission, validation, copy and session expiry with NVDA/Firefox on Windows and VoiceOver/Safari on macOS. Include actual Simplified/Traditional Chinese IME on macOS. Check source/result editing, native selectors, copy and keyboard visibility with iOS Safari/VoiceOver and Android Chrome/TalkBack. Check keyboard-only Windows/macOS at 100%, 200% and effective 400%, forced colors, text spacing, and speech-control labels.

Automated semantics, focus, contrast/reflow and live-region checks do not prove understandable screen-reader narration or native mobile keyboard behavior. Deferred sentence, comparison and custom-sheet journeys are excluded from current evidence, not reported as passing.

### 4.4 Curated visual regression

Use **seven** initial Chromium baselines: Translation ready and oversize-error layouts (each 1440×900 and 390×844), Rewriting with inline mode and a plain edited result (390×844), local registration with long validation errors (1440×900), and workspace-reset dialog (390×844). Add a baseline only for an uncovered layout risk. No sentence, comparison, Google, prefix, custom-dropdown or tools-sheet baseline ships now. Remaining combinations use DOM assertions or targeted browser geometry, not screenshots of every scenario.

Pin Noto Sans/Noto Sans SC, Chromium/container, scale factor 1 and deterministic synthetic content/time. Wait for fonts; hide caret; disable motion and control scrollbars. Native select popups are excluded from pixel capture. Start with per-pixel threshold 0.1 and max differing ratio 0.001; record measured reasons for adjustments and never automatically accept changed baselines. DOM emulators cannot prove layout. Keep structural/semantic checks independent of screenshots.

Browser contracts use semantic locators and isolated contexts; prefer explicit readiness and controlled responses over timing sleeps. See [Playwright best practices](https://playwright.dev/docs/best-practices). Native clipboard, composition, selection and restoration require their actual platform evidence; synthetic DOM events or bundled engines cannot stand in for physical-device or assistive-technology review. Unsupported access to a required device/version is an explicit evidence gap, blocking RG-006 until resolved.
