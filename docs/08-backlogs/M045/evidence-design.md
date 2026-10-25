# M045 evidence design and prerequisites

2026-10-25: selected source lock and link audit passed before edits; clean starting tree at c1455e77c01b275351403107826596f7ac334d73. This implementation changes the selected code sources, not the behavior owners. Refresh the lock after reviewing those changes. No deployment is authorized.

Sentence references are authored synthetic cases in `contracts/fixtures/sentences.v1.json`, reviewed against UX §8.4: paragraph/CRLF gaps, scalar emoji/combining clusters, Chinese punctuation, abbreviations, dotted initials, decimals, adjacent closing quotes, duplicate occurrences and punctuation-free fragments. C# and TypeScript must consume this same file; limits additionally exercise legal maxima and invalid Unicode/partial spans.

The alternatives corpus files under `backend/tools/LinguaDesk.Ai.Evaluation/Corpus/alternatives/` contain 36 development cells, a 40-case development/calibration pool and 360 separate qualification sets. Meaning assertions were authored from explicit synthetic facts before any candidate output. Each language includes numerical prohibition, context, quoted instruction and repeated selected occurrence; Chinese alternates scripts (five Simplified/four Traditional development; five each per qualification cell). Qualification adds long contexts. Codex authored and semantically reviewed these references; this is same-agent review, not independent-review/calibrated-judge evidence. Qualification execution and additional review remain pending.

Frozen SHA-256 values:

- development.v1.json: `77e7e8d1b8486ad8735e333afb415469776e6e6c71f9c3df8c9ae193ca5f210d`
- calibration.v1.json: `35219f04e18cdbfecb423b0e619ebde8f8c4b9a96739b783b0a76952f771b937`
- qualification.v1.json: `6c75db75ab9ee2b82c3e9c0dff4118b30ba0424339ef1dc622c711fb39cd3936`

## Live authorization

In this chat the owner explicitly authorized US$5 total, then approved the local evaluator with existing profile `DeepSeek-V4.1-Flash`, model `deepseek-flash`, credential reference `deepseek`, primary only, at most 73 total provider dispatches (one access check plus two for each of 36 cases), no paid retries/model-review calls, and confirmed other serving using this credential will be quiescent. The credential presence check returned present; no key material was displayed. Full qualification and paid performance workloads are excluded.

Official [DeepSeek pricing](https://api-docs.deepseek.com/quick_start/pricing/) checked 2026-10-25: current model version DeepSeek-V4.1-Flash-1017, 1M context, maximum output 384K, JSON and non-thinking mode supported, peak cache-miss input $0.30/M and output $1.20/M. Existing tariff ceilings remain conservative. This capability/tariff check does not establish language qualification. Record actual remaining budget and dispatch capacity after every live invocation; an incomplete/failed batch stays failed.

The alternatives profile uses a separate bounded view; UTF-8 bytes of the complete decoded model prompt content plus 512 framing tokens supply a conservative byte-level-tokenizer bound. This includes inner user-message JSON escaping, duplicated selected text, instructions and metadata. Outer HTTP JSON encoding is decoded before tokenization and is not counted twice. Both chain and monthly admissions use the view/actual stage bound plus maximum output; no average characters-per-token estimate is used for alternatives. Legal-maximum fixtures must prove the 131072 input ceiling and context capacity. Full-text bounds remain unchanged.

## Early contract review

Generated from actual C# endpoint metadata with `bash scripts/contract.sh generate` (normalizer extended for the new operation). Reviewed before frontend transport adoption: separate POST route and shared status; six required alternatives inputs plus optional nonnullable mode with `correctionOnly` omission default; integer scalar offsets/bounds; UUID identity/association; original success requires options, association/span, resolved language/mode, charge/usage/deadline; kind is additive on existing responses; status/replay remain text-free. Full request fields and nine modes are unchanged. Full-text original responses require the eligibility-resolved language; it is never reconstructed from output. The migration adds one nonnullable Kind column with `fullText` default; old migrations/fingerprint encoding are untouched. Behavioral tests still need to validate the metadata claims.

Access check succeeded: one dispatch, actual USD 0.0001203 (peak-rate conservative accounting), no unresolved exposure. Batch allocation deliberately remains USD 4.98 / 72 dispatches, so total authorization cannot be exceeded even by an unresolved USD 0.02 access reservation. Remaining after access: 72 dispatches, USD 4.9998797 actual, with only USD 4.98 delegated to batch.

## Execution amendments

The initial 73-dispatch authorization was followed by an explicit 72-dispatch v2 approval under the same US$5 total cap and quiescent scope. The owner then kept further acceptance pending, before superseding that instruction: “Don't ask about spending liits for live test. Run them as much as needs. They are very cheap.” Execution continued in finite revised-prompt 36-case batches, without further spending questions, preserving the conservative original US$5 aggregate ceiling. No paid reviewer, qualification/performance run or concurrent serving was introduced. Reports/reviews retain failed batches and unresolved exposure; see [live review](live-review.md).

Calibration/qualification v2 add complete punctuation-free final paragraphs per cell, after reference review identified that missing design stratum. v1 remains retained and unexecuted; v2 is active and also unexecuted. Hashes: calibration.v2 `c319a7b8ee9504b95bc3d8e9f02e24d52f67482a3eab4a7190607c1471e836fb`; qualification.v2 `f795c315fd3c049f43a43c7669534b11d7e571b2a5f5cc45454be80d2ac84ba4`. Development.v1 was never changed to match candidate output.

Final code/generated impact review preserves old full-text request/mode IDs, fingerprint bytes, identity uniqueness and ledger/teardown rules. The only new durable operation field is Kind; alternatives context, association and options remain transient/keyed. Generated schema review additionally confirmed unknown-field rejection, nonnullable omission-default mode, exact two-option bounds and scalar/context limits. Prior-schema copy/upgrade/restart plus privacy sentinels execute in the API suite. Real serving exposure lacking authoritative ledger-scale reconciliation remains reserved; no-charge evidence is restricted to the deterministic Smoke client. No credentials, real user text or real account data are in the corpus/evidence.
