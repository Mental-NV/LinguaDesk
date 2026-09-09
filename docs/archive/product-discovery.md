# Product discovery decisions D-01–D-16

Historical provenance only. Current PRD requirements and D-17/D-18 supersede affected clauses; no archived clause authorizes implementation.

| ID | Historical decision |
| --- | --- |
| D-01 | Everyday/professional communication; owner, 10–100 early users, and prospective hiring clients; React SPA and independent API integration. |
| D-02 | All 12 translation directions and four rewriting languages; automatic source detection with override; explicit translation target. |
| D-03 | Both Chinese scripts accepted, Simplified output; one main supported language; plain text and factual/structural preservation. |
| D-04 | Mandatory correction; mutually exclusive style/tone dropdown; default **None set** and **Correction only = false**. Independent simultaneous style/tone controls were superseded. |
| D-05 | Separate translation and rewriting APIs/pages; automatic processing; editable/copyable results; confirmed detailed rewriting interaction and sentence-alternatives operation. |
| D-06 | **20-call daily allowance discarded.** Use 20,000 characters/user/day and 2,000,000 globally, resetting at midnight UTC. |
| D-07 | Charge successful full submissions; alternatives charge selected sentence only. Retries/failures add no charge; successful outdated responses count. |
| D-08 | Configuration-only routing; shared writing/alternatives chain per language; nonnegative integer rule priorities, 0 highest; defaults plus ordered fallbacks. Decimal priorities superseded; no traffic weights. |
| D-09 | Fallback for provider failures/invalid output; same minimum quality for all candidates; recovery invisible; total failure preserves work without charge. |
| D-10 | Open registration with Google/local accounts; local email verification and password reset; no saved text history/logging; no provider training use and limited disclosed retention. |
| D-11 | Default DeepSeek V4 Flash, non-thinking; percentile targets **10/10/5 seconds**, deadlines **30/30/15 seconds** for translation/rewriting/alternatives; monetary cap value deferred. |
| D-12 | **90%** usable-output threshold, no critical factual/meaning errors in release evaluation; fixed set plus AI-assisted grading and human checks in every language; responsive desktop/mobile web with keyboard/screen-reader access. |
| D-13 | P-001 accepted: translation matches rewriting’s configurable one-second pause, waits for completed IME composition before starting that pause, retains the prior result while updating, rejects stale responses, and preserves the prior result on total failure. |
| D-14 | P-002 amended: manual result edits are local and uncharged, invalidate earlier pending responses, and are replaced by the next source/mode-triggered full rewrite; comparison and alternative data are invalidated only for affected sentences. |
| D-15 | P-003 amended: empty input is not processed; uncertain and same-language cases require language correction; all resume automatically when valid. Oversized translation processes and charges a clearly disclosed 5,000-character prefix, while oversized rewriting remains blocked. |
| D-16 | P-004 accepted: the MVP UI is English; preferences and source/result text do not persist across sessions; session-only control state is permitted; new workspaces use the confirmed defaults. |
