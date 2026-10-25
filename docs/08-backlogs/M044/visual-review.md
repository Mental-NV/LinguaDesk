# M044 — Visual review

AI review completed 2026-10-25, after explicit intentional capture and a normal baseline-enforcing rerun. The seven current captures were inspected as images, alongside independent geometry/semantic tests. The historical Translation-ready baseline is removed because explicit processing/usage/copy/reset surfaces were retired; original package records are preserved.

Translation now shows equal 632 px desktop panels, selectors anchored to their corresponding columns, an always-present result, native editors and Clear below source. Rewriting has two 308 px selector tracks above source and aligned equal result geometry. Both narrow layouts stack controls before source, Clear and result, at 358 px content width. Error copy wraps within the frame; editable result lines, focus rings, registration errors and footer are readable. No removed control or persistent usage/status/count surface appears.

Intentional differences: the previous 400 px workspace card is replaced with the specified frame; the editor row is 540 px at the 900 px viewport; the current 48 px account controls and 24 px field gaps replace the old spacing. Oversize captures set editor scroll to the top for deterministic presentation while the browser interaction suites separately verify native caret behavior. Capture stabilization pins fonts, scale 1, motion, caret and scroll; it does not replace application DOM or usage text. Native select open popups are excluded.

An initial capture briefly rendered an empty native Writing language label during font/layout settling; explicit selected-option assertions and final stabilized captures resolve it. Final images display Detect automatically. The registration narrow reflow test now verifies that the focused first error and the summary are reachable through ordinary page scrolling, rather than requiring both to remain simultaneously in view.

Environment: macOS 27.0.1, Playwright 1.63.0, Chromium 153.0.8010.12 / revision 1243, device scale 1; Noto Sans/Noto Sans SC 400/600/700 fixture subsets pinned in the font manifest. This is host-native Chromium evidence, not Linux-container, branded-browser, physical-device, screen-reader or native-IME verification. No threshold increase: per-pixel 0.1, differing ratio 0.001. Normal smoke passed all seven comparisons.

| Reviewed capture | SHA-256 |
| --- | --- |
| [registration-long-validation-1440x900-chromium-darwin.png](visuals/registration-long-validation-1440x900-chromium-darwin.png) | `df022a8eca11b1ecd0a56c9f3bd93b977319145a5d1cc9952c1d141da09e8184` |
| [rewriting-edited-result-1440x900-chromium-darwin.png](visuals/rewriting-edited-result-1440x900-chromium-darwin.png) | `6f906f1a8e087a9e78d9c4580f9cc8e7a68ce80cc9c2a11a5e207773b7579117` |
| [rewriting-edited-result-390x844-chromium-darwin.png](visuals/rewriting-edited-result-390x844-chromium-darwin.png) | `902227e0e0568b87989b57735f6fb07946a733b08d96cc078f774844ef633e46` |
| [translation-oversize-error-1440x900-chromium-darwin.png](visuals/translation-oversize-error-1440x900-chromium-darwin.png) | `7cabf873c8a48038f676e51c28a8d9c88f16a420537f89f0a3bec6ff9bedaba2` |
| [translation-oversize-error-390x844-chromium-darwin.png](visuals/translation-oversize-error-390x844-chromium-darwin.png) | `02e7b06acb95e379d50daa48af29887c4b9bc0ce924d9dfe8e2f67004022a5c6` |
| [translation-result-1440x900-chromium-darwin.png](visuals/translation-result-1440x900-chromium-darwin.png) | `4ba290abbbc2adb48884042e42794a6ea51586e4980b0968013af168d0c813ca` |
| [translation-result-390x844-chromium-darwin.png](visuals/translation-result-390x844-chromium-darwin.png) | `e4d7c050a0ef55197d9f7335cfdfa0835b5c08110b5d0b2d728b11f41c1fd514` |
