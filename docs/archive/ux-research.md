# Historical UX research

Non-normative September 7 research. Current UX contracts supersede historical adaptations; do not load for ordinary implementation.

## 2. Historical reference research (non-normative)

The following September 7 observations remain research provenance. Custom tools, sentence assistance and automatic processing described as earlier design adaptations are deferred by PRD v0.3. This section creates no MVP UI or test requirement; current behavior is in Sections 3–14.

### 2.1 Inspection record

Research was conducted on September 7, 2026 in an anonymous, signed-out browser. Synthetic text only was entered. Desktop inspection used `1440 × 900` CSS px (the initial Write capture was `1280 × 720`); mobile inspection used `390 × 844` CSS px with the viewport checked in the page. Direct application URLs were [DeepL Translator](https://www.deepl.com/en/translator), [DeepL Write](https://www.deepl.com/en/write), [DeepL login](https://auth.deepl.com/login), [DeepL sign-up](https://auth.deepl.com/signup), and [DeepL password reset](https://auth.deepl.com/passwordreset).

| Evidence | Classification | Relevant observation |
| --- | --- | --- |
| Translator, empty, desktop and mobile | Directly observed | Compact product-mode controls; source/target language row above editor; paired desktop editor regions; compact header and stacked mobile content. |
| Write, empty/result, desktop and mobile | Directly observed | Source and improved-result editors; language row; desktop editing-tools surface; mobile stacked cards; result action icons. |
| Synthetic Write correction | Directly observed | `Yesterday I go…` produced a complete corrected result; changed words were green and underlined while source remained intact. This is evidence of presentation only, not a LinguaDesk quality result. |
| Word/sentence interaction | Directly observed | Activating a changed word selected the related sentence and exposed actions named `Revert to original sentence`, `Rephrase sentence`, `Replace word`, and `Close`. |
| Public authentication | Directly observed | Centered, single-column forms; Google/Apple/SSO before email/password; password visibility; forgot-password and sign-up links; email-only reset-request form. |
| Processing and some popovers | Limitation | Cloudflare challenge intermittently overlaid the anonymous app. A correction result was observed, but error, long-input, alternative-result, authenticated, and email-verification states were not directly accessible. No behavior below is attributed to DeepL unless separately documented. |
| Keyboard, editing, copy, and comparison completion | Limitation | Visible names/roles and an action menu were inspected; selector expansion, stable keyboard traversal, actual clipboard write, alternative selection, deleted-text comparison, real mobile IME/software keyboard, and backend error recovery were not established. The mobile run was CSS viewport emulation, not a physical device. LinguaDesk behavior for these states is a design decision, not a DeepL observation. |
| [Use DeepL Write](https://support.deepl.com/hc/en-us/articles/11673757647388-Use-DeepL-Write) | Official documentation | Paired source/improved text, automatic improvement on web, green change marks, Show changes, alternatives, and a copy action. |
| [Customize your text with DeepL Write](https://support.deepl.com/hc/en-us/articles/9710730337820-Customize-your-text-with-DeepL-Write) | Official documentation | Four named writing styles, four tones, menu-based application, alternative phrasing, and platform/language limitations. |
| [Select alternatives](https://support.deepl.com/hc/en-us/articles/4407359201938-Select-alternatives) | Official documentation | Word/phrase alternatives and a sentence-rephrase action; options may be in a dropdown and can change sentence structure. |
| [Reset your password](https://support.deepl.com/hc/en-us/articles/360020693260-Reset-your-password) | Official documentation | Email-request/link/new-password journey; email is sent when an account exists; social-account passwords are handled by their provider. LinguaDesk's non-enumerating UI confirmation is a design choice, not an observed DeepL state. |

### 2.2 Current disposition of historical patterns

| Significant reference pattern | Disposition | LinguaDesk rationale |
| --- | --- | --- |
| Translation/Rewriting mode switch near the workspace | **Adopt** | Makes the two principal tasks prominent while preserving separate routes required by FR-003. |
| Language controls above large paired editors | **Adopt** | High scanability and direct source/result relationship. |
| Source left/result right on wide screens; stacked on narrow screens | **Adopt** | Familiar editor ergonomics, adapted to deterministic breakpoints in Section 4. |
| Editing tools in a right rail or compact sheet | **Defer DF-005** | Current MVP uses the single native Writing mode select inline at every width. |
| Green result-only change emphasis and contextual sentence actions | **Defer DF-001/002** | Current results are plain editable text; targeted metadata preservation is retired. |
| Immediate/automatic processing | **Defer DF-003** | Explicit Translate/Rewrite now; retain complete results, IME safety, previous-result preservation and accounting disclosures. |
| Broad language catalog, dictionary, files, speech, glossary, formality, terms, TTS, feedback, and cross-product actions | **Omit** | Not authorized by the PRD. |
| DeepL language/platform limitations | **Omit** | Current Translation/Rewriting cover all four PRD languages; sentence alternatives are DF-001. |
| DeepL authentication's Apple and SSO choices and marketing consent | **Omit** | Current MVP has local accounts only; Google is DF-007. |
| Centered, narrow authentication forms | **Adapt** | Retain local email/password, verification and recovery with LinguaDesk copy; no provider buttons in MVP. |
| Reference branding, logo, exact colors, and marketing content | **Omit** | LinguaDesk uses its own visual tokens and product scope. |
