# Translator and Write language expansion investigation

Investigated 2026-10-26 (Europe/Moscow), repository baseline `554493f63625b1ea10736f61206ae2aab2f7d60f`. This is feasibility research; [PRD language policy](../product/languages.md) owns selected scope, [ADR-014](../decisions/ADR-014.md) owns rationale and [M046](../08-backlogs/M046/plan.md) owns implementation. No production code or model calls were changed/run during this investigation.

## Finding and selection boundary

The architecture has no four-language transport/storage restriction. It carries Unicode strings and explicit language IDs into shared eligibility and transformation prompts. The main additions are catalog/contract/prompt synchronization, native text direction, script-capable fonts and a small shared sentence-policy extension. The selected catalog contains **124 languages, 120 new**; all participate in Translator, nine Write modes and alternatives. It includes less widely evaluated languages because lack of prior model evidence is not itself a reason to call a catalog addition architecturally difficult. Their evidence remains pending.

This is a broad investigated scope, not a verified or exhaustive provider language list. No finite public list establishes that the configured model can translate and rewrite every language at LinguaDesk's quality threshold. Additional languages can fit the same architecture; each still needs an unambiguous language/variety/script identity and reviewable reference material. The catalog explicitly separates those unassessed possibilities from features needing a different layout, input system or model routing. No language is described as tested merely because it appears in a dropdown plan.

The user's follow-up rejects exhaustive translation-pair evaluation. Accordingly, all 15,252 distinct pairs are constructed and checked offline; live Translation evidence uses **24 representative template directions plus two anchor smokes per new language (264 development cases)**. Release Translation uses 480 representative quality cases plus 240 language smokes, rather than 305,040 all-pair quality cases. This is an explicit evidence tradeoff: unmeasured cross-pairs remain a risk, not a hidden claim of per-pair qualification. English is an evaluation anchor only; it is never a runtime pivot. Write/alternatives retain the existing language/mode verification model.

## Inspected implementation boundaries

| Boundary | Current evidence | Small implementation change / risk |
| --- | --- | --- |
| Core catalog | `backend/src/LinguaDesk.Core/ProductCatalog.cs` contains four IDs and 12 manually listed directions; source selections derive from that list | Add canonical descriptors and derive all distinct pairs; dictionary/set membership if useful; retain exact old IDs |
| Public contract | `Features/Capabilities/CapabilitiesContract.cs` enums and `CapabilitiesDocument.cs` switches repeat the four languages; operation descriptions also enumerate them | Extend C# metadata/mapping, generate schema/types, assert exhaustive parity; permit three-letter codes without arbitrary locale acceptance |
| Eligibility | `EligibilityEnvelopeParser.IsSupportedLanguage` and `Prompts/eligibility.v1.txt` have a separate four-ID allowlist | Reuse Core membership and render the trusted catalog into a versioned eligibility bundle; preserve uncertainty and terminal validation |
| Translation/Write prompts | `translation.v1` and `alternatives.v9` explicitly list four codes; rewriting preserves source with a Chinese exception | New immutable revisions; serialize the selected language's name/script instructions from Core; keep existing envelopes, modes and chain limits |
| Bounds and cost | `AlternativesPrompt.Create` caps static scaffold at 4,096 bytes; alternatives profile caps input at 131,072 tokens and output at 8,192, with byte-based input proof | Do not paste the full catalog into every alternatives prompt. Eligibility needs the allowed catalog; transformations need validated selected descriptors. Measure actual full prompts and worst-case Unicode; review any finite bound change before live calls |
| Frontend selection | `TranslatePage`/`RewritePage` already render capability arrays; `api/translation.ts` still declares a handwritten four-ID union; `workspaceModel.ts` names four languages in its error | Derive types and labels from contract/capabilities; sort and fit long labels; replace message copy; no custom language picker required |
| Native text / review | `WorkspaceView.tsx` and Write presentation need content direction; `comparison.ts` already uses grapheme segmentation with bounded matching | Scoped `dir`/`lang` and bidi isolation, logical alignment and font coverage; preserve English shell, plain copy and raw scalar offsets |
| Sentence alternatives | Core `SentenceBoundaryPolicy` and `frontend/src/rewrite/sentences.ts` implement v1 ASCII/CJK terminals plus a small abbreviation list; alternatives parser reuses segmentation | Version shared punctuation/closer rules, add language-specific Greek semicolon behavior, pass resolved language into option validation; freeze identical cross-runtime fixtures |
| Evidence | Standalone runners/corpora, Smoke providers, API tests and consumer sample assume the baseline in places | Parameterize catalog coverage and versioned cases; a deterministic provider's success cannot establish language quality |
| Persistence | Existing identity/ledger operations retain metadata, not source/result text | No new table/migration expected; preserve old fingerprints/status and test new codes through real admission/recovery |

## Effort and risk classes

- **Catalog/prompt work:** Latin/Cyrillic languages, Greek, Korean and Japanese mostly reuse existing rendering and punctuation. Standard-variety choices and closely related language ambiguity still need fixtures. Three-letter codes fit existing JSON string enums.
- **Small shared UX/policy work:** Arabic/Hebrew text needs direction/isolation; Indic and Southeast Asian scripts need suitable glyphs, combining/joiner coverage and additional terminals. Armenian/Ethiopic punctuation belongs in the same versioned policy. Native Unicode textareas and conservative whole-fragment alternatives avoid a new rich-text/dictionary editor.
- **Explicit constraints:** Chinese output remains Simplified; Serbian Write preserves Latin/Cyrillic input while translation defaults Cyrillic; Punjabi uses Gurmukhi and Mongolian Cyrillic. A separate script/dialect picker, vertical layout, romanization, new input method, OCR/voice and route-specific model selection are outside this low-effort expansion.
- **Dominant uncertainty:** semantic output quality, ambiguity and reference coverage, especially for lower-resource languages. A uniform prompt may need correction; do not add a per-language subsystem or claim universal competence to make a large catalog appear complete. Failures trigger bounded targeted investigation, not a full Cartesian live run.

## Public sources and limits

Public references checked on 2026-10-26; these support design choices, not model qualification:

- [IANA language subtag registry](https://www.iana.org/assignments/language-subtag-registry/language-subtag-registry): canonical language identifiers include both two- and three-letter subtags. Product script/variety choices remain explicit policy, not something inferred from code length or an English name.
- [W3C structural direction guidance](https://www.w3.org/International/questions/qa-html-dir): content direction can be scoped to text elements, with automatic direction for unknown user text. [Inline bidi guidance](https://www.w3.org/International/articles/inline-bidi-markup/): isolate embedded mixed-direction content. This supports keeping the English shell while adding RTL text; actual editing/copy behavior still needs browser checks.
- [Unicode text segmentation, UAX #29](https://www.unicode.org/reports/tr29/): grapheme, word and sentence boundaries are different policies and may need tailoring. LinguaDesk's conservative v2 policy is not claimed to implement all of UAX #29; runtime/browser Unicode-version differences require shared fixtures.
- [DeepSeek API model documentation](https://api-docs.deepseek.com/quick_start/pricing/) and [publisher V4.1 model card](https://huggingface.co/deepseek-ai/DeepSeek-V4.1-Flash-1017): checked as current provider context. Neither inspected page establishes LinguaDesk-level Translation/Write/alternatives qualification for the proposed catalog. No provider switch, tariff change or numerical spending authorization is inferred. Recheck the configured live profile/settings before execution.

## Access and outcome

Repository inspection and public-source research needed no external credential. Existing local commands and completed M045 dependencies are available. M045 records prior live authorization and quiescent exposure; it must be examined for continuing applicability rather than asking again for an already authorized action or assuming a historical remainder is current. Actual credentials, remaining finite capacity and shared-provider/quiescence scope are prerequisites only for the live task. No key, environment dump or paid call was needed for this planning request.

Selected: catalog-driven expansion plus the shared UX/segmentation work, with template-based Translation evidence. Pending: implementation, all expanded-language live observations, full qualification and production performance. Existing four-language results are historical baseline evidence only.
