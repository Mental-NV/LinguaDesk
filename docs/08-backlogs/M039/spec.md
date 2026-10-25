# M039 — Selected specification
Selected items: BI-001. Status: ready; completion evidence in tasks.md.
Sources: [manifest](context.json); [backlog](backlog.md).

## Scope
One bounded Chromium 390×844 group completes both current core journeys with real login, native selectors, one-second pause, editable/copyable output, essential failure, explicit retry, unknown-outcome status recovery and session expiry. Registration/sign-in, responsive native controls and semantic/contrast/reflow are included under NFR-005/007 and D-22. M044 supplies the implementation, race/unit/API evidence and seven reviewed baselines; additional engines/devices/AT, real mail, paid quality/performance and operational gates are excluded.

## Acceptance
| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | At 390×844 the predefined verified account signs in through the real form to each protected feature, selects native settings, dispatches once after the pause and receives the visible expected editable result without moving source focus. | UX §3/5/7–10; verification frontend §4.1–4.3 |
| AC-002 | Native editing/copy preserves exact result/newlines without requests or charges; essential provider-failure and retry preserve both editors and use a fresh identity; a real connection interruption retains original identity for read-only Check status without automatic paid retry; observed expiry clears before login. | UX-AC-013/015/033/036/039/044/051 |
| AC-003 | Narrow registration validation/reveal/accepted state and explicit Go to sign in follow DF-008 without automatic login or retained credentials; semantic/contrast checks have no findings. | UX §9; UX-AC-006/007/101; D-22 |
| AC-004 | Workspace reflow at 1024×768, 720×450 (effective 200% reflow) and 320×640 (effective 400% target), plus active account forms at 320×640 with text-spacing overrides, retain available native controls without horizontal overflow/clipping. | UX §4/10; UX-AC-077/081/083/095 |
| AC-005 | Full unit/published regressions and the seven M044 visual comparisons pass without automatic baseline updates; evidence records engine/viewport and honest synthetic/native limits. | Verification frontend §4.4; V-002/003/010/011/012 |

## Constraints and decisions
A bounded delay is permitted only in the existing Smoke external-provider adapters to expose interrupted admitted work; production provider registration/behavior remains unchanged. No new product feature, API/schema/storage migration or production publication is needed. Reuse shared published fixtures and the existing dedicated verified synthetic account; registration necessarily creates a distinct run-owned visitor account through the public form. No application API interception, forged session, new seed endpoint or speculative accounting. This is viewport emulation in pinned Chromium, not physical-mobile or actual-zoom-platform verification. Optional observations do not block completion.
