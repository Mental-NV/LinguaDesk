# Language catalog and expansion policy

Product authority: continuation of [PRD #1](../01-PRD.md), FR-004–006/009/012 and D-24. **Selected catalog, 2026-10-26; required M046 live quality gate is not passed.** The local M046 runtime and generated contract expose this catalog. The reduced 100-case live development and bounded investigations ran: 92 usable/eight unresolved findings. M046 is closed with user-accepted accuracy exceptions; accuracy improvements are deferred and the original all-usable gate remains failed ([case results](../08-backlogs/M046/case-results.md)). Earlier four-language reports do not qualify additions. [Research](../research/language-expansion-2026-10-26.md) records the feasibility boundary; [M046](../08-backlogs/M046/backlog.md) owns delivery.

## Selected catalog

The target is **124 language choices (120 additions)** for both Translator and Write, including all nine rewriting modes and sentence alternatives. These are the investigated languages that can reuse the current Unicode plain-text pipeline with catalog/prompt changes and the shared RTL, punctuation and font work below. No language-specific service, model routing, transliteration engine or morphological analyzer is selected. Architecture fit does not establish model competence. This catalog is a broad concrete implementation scope, not a claim to enumerate every language in the world or every language a model can sometimes produce.

Codes are case-sensitive canonical language identifiers; English labels belong to the product. Most are two-letter identifiers; `ceb`, `fil` and `ckb` demonstrate that code length is not a validation rule. Unknown codes, locale aliases and script/region suffixes are rejected rather than silently normalized. `auto` is only a source selector. Existing `en`, `ru`, `ro`, `zh` wire meanings stay unchanged.

| Code | English language label | Selected writing system | Delivery |
| --- | --- | --- | --- |
| `af` | Afrikaans | Latin | M046 addition |
| `ak` | Akan | Latin | M046 addition |
| `sq` | Albanian | Latin | M046 addition |
| `am` | Amharic | Ethiopic | M046 addition |
| `ar` | Arabic | Arabic (RTL) | M046 addition |
| `hy` | Armenian | Armenian | M046 addition |
| `as` | Assamese | Bengali | M046 addition |
| `az` | Azerbaijani | Latin | M046 addition |
| `bm` | Bambara | Latin | M046 addition |
| `eu` | Basque | Latin | M046 addition |
| `be` | Belarusian | Cyrillic | M046 addition |
| `bn` | Bengali | Bengali | M046 addition |
| `bs` | Bosnian | Latin | M046 addition |
| `br` | Breton | Latin | M046 addition |
| `bg` | Bulgarian | Cyrillic | M046 addition |
| `my` | Burmese | Myanmar | M046 addition |
| `ca` | Catalan | Latin | M046 addition |
| `ceb` | Cebuano | Latin | M046 addition |
| `ny` | Chichewa | Latin | M046 addition |
| `zh` | Chinese | Chinese | Existing |
| `co` | Corsican | Latin | M046 addition |
| `hr` | Croatian | Latin | M046 addition |
| `cs` | Czech | Latin | M046 addition |
| `da` | Danish | Latin | M046 addition |
| `nl` | Dutch | Latin | M046 addition |
| `en` | English | Latin | Existing |
| `eo` | Esperanto | Latin | M046 addition |
| `et` | Estonian | Latin | M046 addition |
| `ee` | Ewe | Latin | M046 addition |
| `fil` | Filipino | Latin | M046 addition |
| `fi` | Finnish | Latin | M046 addition |
| `fr` | French | Latin | M046 addition |
| `gl` | Galician | Latin | M046 addition |
| `ka` | Georgian | Georgian | M046 addition |
| `de` | German | Latin | M046 addition |
| `el` | Greek | Greek | M046 addition |
| `gu` | Gujarati | Gujarati | M046 addition |
| `ht` | Haitian Creole | Latin | M046 addition |
| `ha` | Hausa | Latin | M046 addition |
| `haw` | Hawaiian | Latin | M046 addition |
| `he` | Hebrew | Hebrew (RTL) | M046 addition |
| `hi` | Hindi | Devanagari | M046 addition |
| `hu` | Hungarian | Latin | M046 addition |
| `is` | Icelandic | Latin | M046 addition |
| `ig` | Igbo | Latin | M046 addition |
| `id` | Indonesian | Latin | M046 addition |
| `ga` | Irish | Latin | M046 addition |
| `it` | Italian | Latin | M046 addition |
| `ja` | Japanese | Japanese | M046 addition |
| `jv` | Javanese | Latin | M046 addition |
| `kn` | Kannada | Kannada | M046 addition |
| `kk` | Kazakh | Cyrillic | M046 addition |
| `km` | Khmer | Khmer | M046 addition |
| `rw` | Kinyarwanda | Latin | M046 addition |
| `ko` | Korean | Korean | M046 addition |
| `ku` | Kurdish (Kurmanji) | Latin | M046 addition |
| `ckb` | Kurdish (Sorani) | Arabic (RTL) | M046 addition |
| `ky` | Kyrgyz | Cyrillic | M046 addition |
| `lo` | Lao | Lao | M046 addition |
| `la` | Latin | Latin | M046 addition |
| `lv` | Latvian | Latin | M046 addition |
| `ln` | Lingala | Latin | M046 addition |
| `lt` | Lithuanian | Latin | M046 addition |
| `lg` | Luganda | Latin | M046 addition |
| `lb` | Luxembourgish | Latin | M046 addition |
| `mk` | Macedonian | Cyrillic | M046 addition |
| `mg` | Malagasy | Latin | M046 addition |
| `ms` | Malay | Latin | M046 addition |
| `ml` | Malayalam | Malayalam | M046 addition |
| `mt` | Maltese | Latin | M046 addition |
| `mi` | Maori | Latin | M046 addition |
| `mr` | Marathi | Devanagari | M046 addition |
| `mn` | Mongolian | Cyrillic | M046 addition |
| `ne` | Nepali | Devanagari | M046 addition |
| `nb` | Norwegian Bokmål | Latin | M046 addition |
| `nn` | Norwegian Nynorsk | Latin | M046 addition |
| `oc` | Occitan | Latin | M046 addition |
| `or` | Odia | Odia | M046 addition |
| `om` | Oromo | Latin | M046 addition |
| `ps` | Pashto | Arabic (RTL) | M046 addition |
| `fa` | Persian | Arabic (RTL) | M046 addition |
| `pl` | Polish | Latin | M046 addition |
| `pt` | Portuguese | Latin | M046 addition |
| `pa` | Punjabi | Gurmukhi | M046 addition |
| `qu` | Quechua | Latin | M046 addition |
| `ro` | Romanian | Latin | Existing |
| `ru` | Russian | Cyrillic | Existing |
| `sm` | Samoan | Latin | M046 addition |
| `sa` | Sanskrit | Devanagari | M046 addition |
| `gd` | Scottish Gaelic | Latin | M046 addition |
| `sr` | Serbian | Cyrillic | M046 addition |
| `st` | Sesotho | Latin | M046 addition |
| `sn` | Shona | Latin | M046 addition |
| `sd` | Sindhi | Arabic (RTL) | M046 addition |
| `si` | Sinhala | Sinhala | M046 addition |
| `sk` | Slovak | Latin | M046 addition |
| `sl` | Slovenian | Latin | M046 addition |
| `so` | Somali | Latin | M046 addition |
| `es` | Spanish | Latin | M046 addition |
| `su` | Sundanese | Latin | M046 addition |
| `sw` | Swahili | Latin | M046 addition |
| `sv` | Swedish | Latin | M046 addition |
| `tg` | Tajik | Cyrillic | M046 addition |
| `ta` | Tamil | Tamil | M046 addition |
| `tt` | Tatar | Cyrillic | M046 addition |
| `te` | Telugu | Telugu | M046 addition |
| `th` | Thai | Thai | M046 addition |
| `ti` | Tigrinya | Ethiopic | M046 addition |
| `ts` | Tsonga | Latin | M046 addition |
| `tn` | Tswana | Latin | M046 addition |
| `tr` | Turkish | Latin | M046 addition |
| `tk` | Turkmen | Latin | M046 addition |
| `uk` | Ukrainian | Cyrillic | M046 addition |
| `ur` | Urdu | Arabic (RTL) | M046 addition |
| `ug` | Uyghur | Arabic (RTL) | M046 addition |
| `uz` | Uzbek | Latin | M046 addition |
| `vi` | Vietnamese | Latin | M046 addition |
| `cy` | Welsh | Latin | M046 addition |
| `fy` | Western Frisian | Latin | M046 addition |
| `wo` | Wolof | Latin | M046 addition |
| `xh` | Xhosa | Latin | M046 addition |
| `yi` | Yiddish | Hebrew (RTL) | M046 addition |
| `yo` | Yoruba | Latin | M046 addition |
| `zu` | Zulu | Latin | M046 addition |

## Script, variety and detection policy

- The table's writing system is the accepted/default output form unless specified here. Labels with a constrained form expose it where needed, such as Punjabi (Gurmukhi), Mongolian (Cyrillic), Kurdish (Kurmanji) and Chinese (Simplified output). These are language choices, not a new script or dialect picker.
- `zh` continues accepting Simplified and Traditional input and producing Simplified output. Traditional output, Cantonese and classical Chinese are not implied. `ar` uses Modern Standard Arabic; dialect conversion is not promised. `fa` is Persian; `ku` selects Latin-script Kurmanji, while `ckb` selects Arabic-script Sorani. `pa` selects Gurmukhi; Shahmukhi needs separate scoping. `mn` selects Cyrillic; traditional vertical Mongolian is excluded. `az`, `uz`, `tk` and `ha` select Latin; Arabic-script variants are not implied.
- Serbian accepts Cyrillic and Latin input: Write/alternatives preserve the main input script, while Translator target `sr` defaults to Cyrillic. No cross-script same-language translation is added. Closely related language identities (including Bosnian/Croatian/Serbian and Bokmål/Nynorsk) stay distinct; do not fabricate certainty on shared short text. A plausible manual hint may resolve ambiguity under FR-004; conflicting hints still fail.
- Filipino uses `fil`; Tagalog is not a duplicate picker item or a separately promised `fil` ↔ `tl` route. Portuguese, English, Spanish and French have no regional variants. Write preserves the source variety where possible; translation uses natural standard language without promising a locale.
- Combining marks, joiners, accents and input normalization form remain untouched by counting/transport. Output correction may change spelling naturally. Do not strip bidirectional controls from submitted text or insert formatting controls into copied text as a display fix.

## Operation coverage and admission

For N selected languages, support every distinct ordered pair, **N × (N−1)**; with this catalog that is **15,252 translation directions**, **1,116 full-rewrite language/mode cells** and **1,116 alternatives cells**. Same-language translation remains invalid. Both features share the catalog; no translation-only additions, English-pivot translation, per-language models or reduced mode set is silently introduced.

All entries are implemented catalog scope. Live quality remains partially measured, not established merely because a provider accepts arbitrary text. The user closed M046 with the eight recorded accuracy exceptions accepted for closure; see the final case results. The following describes its original validation target, which remains unmet for those cases. M046 completion uses the user-selected 100-case live development sample across writing systems other than Latin/Cyrillic/Chinese (Japanese also omitted to avoid Han overlap), plus the complete deterministic and UI/API checks; production still requires #6's qualification/performance gates for each serving primary/fallback. Translation uses representative deep evaluation and lightweight per-language smokes, not exhaustive all-pairs live evaluation, under the user's D-24 clarification. Unsampled pairs are available through the common template but remain unmeasured. Failed or untested required cases stay unresolved: do not quietly delete cases or advertise them as passed. A concrete inability to pass within bounded work is recorded at this owner with a scoped follow-up/disposition before changing scope.

## Low-effort boundary and future candidates

The scope includes Latin/Cyrillic, RTL, Indic, CJK and common Southeast Asian writing systems because shared rendering/segmentation changes suffice; script complexity alone is not grounds to omit a selected language. Additional languages using these systems are technically eligible for a catalog-only follow-up when their standard variety, code, reference material and evaluation are specified. Remaining regional/minority, historical or constructed languages require the same assessment; absence is **unassessed language/variety/reference coverage, not a claimed architectural impossibility**. Do not represent the selected catalog as exhaustive provider support.

New vertical layouts, custom input methods, OCR/voice/sign-language input, a romanization/transliteration feature, arbitrary dialect/script conversion, UI localization and language-specific routing require separate scope. No existing exclusion is lifted merely because its language family appears here. Further additions should be inexpensive data changes after M046; each still needs the same honest evidence.
