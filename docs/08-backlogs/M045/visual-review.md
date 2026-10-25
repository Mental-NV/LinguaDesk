# M045 visual review

Reviewed 2026-10-26 (Europe/Moscow), Codex visual inspection of all twelve final PNGs. Captures use the pinned Noto Sans/Noto Sans SC manifest, Chromium revision 1243, device scale 1, reduced animations, deterministic Smoke text and a verified isolated account. Threshold .1 / maximum differing pixel ratio .001 are unchanged. Normal comparisons use snapshot updates disabled.

The eleven workspace captures replace D-21 geometry with D-23: 64/256 px navigation, 304 px tools with 16 px gap, 12 px editor gap, rounded white cards, 28/40 desktop and 20/28 mobile text, icon controls, language bar, selected sentence and two options. Desktop semantic geometry assertions cover all four panel combinations; 320/768/1024 reflow, effective zoom, text spacing, keyboard/listbox/account focus and native clipboard have separate executable assertions.

Inspection found no text/control overlap or horizontal clipping. Menus show grouped options and the selected Business check; the remaining options scroll within the viewport. Full-page workspace images include the privacy footer; menu captures use viewport images so a screenshot resize does not dismiss the popup. The selected sentence outline, green replacement word and labeled struck-through whole deleted words are visible. Mobile editors/options stack with usable controls.

The retained registration form layout, focus and validation are unchanged. Its header navigation text alone changes from Translation/Rewriting to the required Translator/Write names; this justifies its PNG update. Two retired edited-result D-21 captures leave the active baseline directory, leaving exactly twelve. All seven original D-21 PNGs are archived byte-for-byte under M044/visuals; only artifact links in its historical review change, preserving its recorded evidence. Earlier captures that dismissed their own menu or split deleted words into letter fragments were rejected and corrected.

| Capture | Reviewed rationale | SHA-256 |
| --- | --- | --- |
| registration-long-validation-1440x900-chromium-darwin.png | Registration header labels only; form unchanged | `2f881e22a15abac8da895c480ed16f3f17d41966cea449402a9fd15bc680f10c` |
| translation-oversize-error-1440x900-chromium-darwin.png | Retained input, wrapped error, result card and copy remain available | `11893f8659242594dec718de226d630e2bf1e80fdaa40da4df26ae0d1d395e6b` |
| translation-oversize-error-390x844-chromium-darwin.png | Retained input, wrapped error, result card and copy remain available | `1c6ccd2ff1f4028a2d76083f2479ea5e23de4388542c3b10c4e3534bb9ecd2a8` |
| translation-result-1440x900-chromium-darwin.png | D-23 shell and responsive editor geometry | `bce022a94a6d1e184cbc6725e1b3a7e2b736bd082b55eb651a04b3fa92d98828` |
| translation-result-390x844-chromium-darwin.png | D-23 shell and responsive editor geometry | `9b898d9102cd3b27e9327bff6ebb075757ab33312d1b352d7953b0093342d884` |
| translator-empty-collapsed-1440x900-chromium-darwin.png | D-23 shell and responsive editor geometry | `18727fef5e74e75f2e1a0c10f53ecfd18c5c319fb4b42b7b710948e7a982492d` |
| translator-empty-expanded-1440x900-chromium-darwin.png | D-23 shell and responsive editor geometry | `2ca4e1fb8bb38f8e403f79592d5a3eca0d329a561048ff93d6a5ef2b27d4a09b` |
| write-alternatives-390x844-chromium-darwin.png | Selected sentence, both choices, clean result; changes/deletions on desktop | `6ecdd57339e3093e530e6d1d95f0b9f06cdc1da545d45d301661b421598b12fb` |
| write-alternatives-changes-1440x900-chromium-darwin.png | Selected sentence, both choices, clean result; changes/deletions on desktop | `148f755f30d5ffab14d384ec2e2db15c344226d4cbb9d1406fe4b7b0d745bba7` |
| write-business-menu-1440x900-chromium-darwin.png | Open grouped menu, selected Business, bounded scroll | `a8a8807ecbad658613c63afe6a2559f20c0ec67c09d3c620eb8f4415cc4b4059` |
| write-empty-focused-1440x900-chromium-darwin.png | D-23 shell and responsive editor geometry | `8cb3c4a46843d463a60f0a8356ec2f4452a6f7862b407f3e2fd7689faaf9810a` |
| write-tools-menu-390x844-chromium-darwin.png | Open grouped menu, selected Business, bounded scroll | `a6232a4f9809a93c6b6679c6914fcbed862f496750db3ad94a011907fbb26ec3` |

This is automated capture plus AI visual/semantic review. Physical devices, native IME, additional browser engines and full assistive-technology/WCAG verification were not observed.
