# M038 — Desktop manual-evidence procedures (T004)

Owner: milestone executor. Status: **executed subset, 2026-09-16** —
owner executed M-001/M-002 and the macOS-applicable halves of M-003/M-005
on macOS Firefox/Chrome (versions below). No automated run signed this off.
NVDA/Windows, Safari/VoiceOver, Edge, previous-version and forced-colors
steps were not executed and are recorded as explicit gaps, not passes.
They no longer block RG-006 under its Firefox/Chrome-only matrix; RG-006
stays pending on the mobile matrix (M039) regardless.

Seed/identity for every script below: the published Smoke host from
`scripts/frontend.sh smoke` (fresh isolated database, deterministic
provider adapters); predefined verified user `m013-verified@example.test`;
fixtures from verification §4.1 (`T-OK-A`, `W-OK`, `T-LONG`/`W-LONG`).
Never use production accounts, live providers or real email.

## M-001 Keyboard-only desktop completion (UX-AC-079 subset)

1. Open `/login` with mouse unplugged. Tab to Email, type the predefined
   address; Tab to Password, type the password; Tab to Sign in, press Enter.
   Expect: protected `/translate` with focused Translation heading.
2. On `/translate`: Tab through Source language (arrows to English), Target
   language (arrows to Romanian), Source text (type `T-OK-A`), Translate
   (Space). Expect: complete Romanian result, `Up to date`, usage +46;
   focus visible at every stop, no trap (Shift+Tab escapes every control).
3. On `/rewrite`: confirm `Correction only` default; type `W-OK`;
   activate Rewrite from the keyboard. Expect: corrected result, source
   unchanged, usage +46.
4. Record: reviewer name, OS/browser/version, pass/fail per step, any trap
   or invisible-focus finding.

## M-002 Zoom and text spacing (UX-AC-077/083 subset)

1. At 100%, 200% and effective 400% (320 CSS px lane) on `/translate`,
   `/rewrite`, `/login`, `/register`: assert every action available, no
   horizontal page overflow, no clipped controls.
2. Apply text-spacing overrides (line height 1.5, paragraph spacing 2×,
   letter 0.12em, word 0.16em): assert status text, editor, native
   controls, auth errors and reset dialog survive without clipping.
3. Record: exact zoom levels, viewport sizes, screenshots of any overflow.

## M-003 Forced colors and reduced motion (UX-AC-083/095 subset)

1. Enable OS forced-colors/high-contrast: assert labels, helper text,
   errors and focus indicators remain readable and visible; no operation
   triggers on theme change.
2. Enable reduced motion: assert no animation-dependent status; shimmer/
   transitions off; plain editor intact.
3. Record: OS/theme names, findings.

## M-004 Screen-reader desktop review (UX-AC-081 subset)

1. NVDA + Firefox on Windows: complete the Translation and Rewriting
   journeys from M-001; narrate language/mode selection, explicit
   submission, validation, copy confirmation and session-expiry wording.
   Include actual Simplified/Traditional Chinese IME input on macOS
   (VoiceOver + Safari): activation during composition submits nothing;
   explicit post-composition activation submits once.
2. VoiceOver + Safari on macOS: same journey scripts.
3. Record: reviewer name, SR/browser/OS versions, narration findings.
   Automated scans passing does not substitute for this row.

## M-005 Branded browser smoke (§4.2 desktop lane)

1. Current and immediately previous stable Chrome and Edge (desktop):
   sign in through the real `/login` form, complete one Translation and
   one Rewriting submission, copy one result.
2. Record actual branded versions tested. Bundled Chromium evidence does
   not stand in for this row.

## Sign-off and gaps

| Row | Reviewer | Date (UTC) | Versions | Result |
| --- | --- | --- | --- | --- |
| M-001 | owner | 2026-09-16 | macOS (version unrecorded), Firefox 156.0, Chrome 154.0.8037.17 | Pass — reviewer reports no trap or invisible-focus finding. |
| M-002 | owner | 2026-09-16 | macOS (version unrecorded), Firefox 156.0, Chrome 154.0.8037.17 | Pass — reviewer reports no overflow or clipped controls (exact zoom levels/viewport sizes unrecorded). |
| M-003 | owner | 2026-09-16 | macOS (version unrecorded), Firefox 156.0, Chrome 154.0.8037.17 | Partial — reduced-motion half passes; forced-colors/high-contrast step not executed (needs Windows) → gap. |
| M-004 | owner | 2026-09-16 | — (not executed) | NOT EXECUTED — no NVDA/Windows or VoiceOver/Safari run, IME check not run → gap. |
| M-005 | owner | 2026-09-16 | Chrome 154.0.8037.17 (current) | Partial — Chrome-current smoke passes with no findings reported; Edge current/previous and Chrome previous not run → gap. |

Explicit gaps (not passing): M-004 screen-reader review in full
(NVDA/Firefox on Windows, VoiceOver/Safari on macOS, macOS IME
composition check) — dropped from this milestone by owner decision, no
screen-reader evidence ships in M038; M-003 forced-colors/high-contrast
step (needs Windows); M-005 Edge current/previous and Chrome previous;
iOS Safari/VoiceOver, Android Chrome/TalkBack and the remaining
previous-version matrix are owned by M039/successors. Baseline
design-review approval (AC-004 human gate): pending named reviewer —
initial `*-darwin` baselines captured on the implementation host await
review; container-pinned (Noto Sans/SC) Linux re-capture is a recorded
follow-up.
