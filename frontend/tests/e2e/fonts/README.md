# Pinned visual-fixture fonts

Noto Sans and Noto Sans SC fixture subsets (400/600/700), retrieved from Google Fonts on 2026-10-25. The manifest pins each downloaded file by SHA-256 and source URL. OFL.txt contains the upstream SIL Open Font License. These font files are used only by the screenshot harness via data URLs; the product keeps its system-font stack and downloads no fonts.

The subset covers printable ASCII, Cyrillic U+0400–045F, Romanian diacritics and the Chinese fixture glyphs. It is not a production font distribution or full-language glyph-support claim. Add and review a fixture subset when introducing uncovered glyphs.

Visual changes are captured explicitly with `LINGUADESK_UPDATE_VISUALS=1 bash scripts/frontend.sh smoke`; a normal smoke run requires existing matching baselines and never accepts differences. Inspect captures alongside independent geometry/semantic checks and record the rationale in the selected milestone before accepting intentional changes.
