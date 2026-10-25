# M045 live development review

Reviewed by Codex against meaning constraints authored before candidate generation. Candidate model is DeepSeek; no paid model reviewer or model-produced reference was used. This same-agent development review is not independent/calibrated release adjudication. Credential reference `deepseek`, local evaluator, primary-only, serving quiescent as confirmed by owner. Authorization history is in [evidence design](evidence-design.md#execution-amendments).

Each batch uses the frozen 36-case development.v1 corpus (four languages × nine modes, five Simplified/four Traditional Chinese inputs, negation/numerals/time boundaries, repeated sentence occurrence, quoted hostile instruction and longer contexts). The production eligibility/prompt/parser/chain execute with a finite 72-dispatch batch allowance and no repair calls or paid review. Early batches used the remaining $4.98; later batches use a $1 ceiling each, always checked against the original $5 aggregate cap including unresolved exposure.

## Preserved attempts

| Run / prompt | Dispatches | Spent plus unresolved USD | Unresolved USD | Report SHA-256 | Disposition |
| --- | ---: | ---: | ---: | --- | --- |
| 01 / v1 | 72 | 0.0232305 | 0 | `482f5761d0b34f82beaaf65cab09ff2f7bfb6796804fe92089da680f8c1af212` | Failed: malformed/duplicate sets and reversed or softened prohibitions; added multilingual semantic-force checks. |
| 02 / v2 | 72 | 0.0260553 | 0 | `8c5676b55d7f03608536adc3c49c7edf19cd641ff82cc1b28bf39f491abeed3b` | Failed: four invalid/copied-original sets and softened force/new belief claim; strengthened distinctness/person guards. |
| 03 / v3 | 72 | 0.0283893 | 0 | `a46c561a6ffa85c6273d8ed1deef98f86639bd970eced5f2522738c7f49e0775` | Failed semantic review: Romanian ungrammatical nominative permission construction; revised grammar guidance. |
| 04 / v4 | 70 | 0.0504473 | 0.022568 | `3261915bd88c810b6c58833364eedc9f9c9092dccdd62ed2b79a94ba1636351d` | Failed/incomplete: two eligibility timeouts, unresolved exposure retained; Romanian nominative case defect persisted. |
| 05 / v5 | 72 | 0.0288321 | 0 | `7323460b4ed3d81d442113bb194f2240102dfcfd7cc875cf5699a395ad2b7f5b` | Not final acceptance: options reviewed, but total static scaffolding exceeded 4 KiB; shortened prompt. |
| 06 / v6 | 72 | 0.0284829 | 0 | `aeb4992244e24af1bfe277c61c76fbdbc32a194e30749e040e3830cdeb6bbc11` | Failed semantic review: English enthusiastic added “and that’s final”; prohibited extra finality. |
| 07 / v7 | 72 | 0.0286473 | 0 | `d66ba5122e1c626e50218b5658efa6e0b704b39591b2a1cdf89bcb593cb196d3` | Failed semantic review: Russian friendly addressed Mira directly and softened prohibition; final person/force veto added. |
| 08 / v8 | 72 | 0.0286845 | 0 | `65df1ea3d6f3c5121fb8ef08b644dd3f7a378d5dc60dfabd755c0120bb97cd07` | Failed semantic review: Chinese casual option 2 used inability “发不了” instead of prohibition; explicitly excluded inability. |

Run 01 did not preserve rejected raw envelopes; accepted outputs and failure categories remain recorded. Runs 02 onward preserve candidate envelopes. All run JSON files remain under ignored `artifacts/m045/`; the hashes above identify the original reports without changing their awaiting-review status. Versions v1–v8 remain in source for reproducibility, while only the selected final resource is embedded. Offline scaffolding tests rejected an initial v8 draft before any paid call; redundant wording was shortened without increasing the fixed limit.

## Final batch

**Development pass: 36/36 cells, both options accepted after semantic review (72/72 options).** Final prompt `alternatives.v9`, SHA-256 `1650d61dd48067d8e359569d2a5001c9df2543488d57194a1ccdaceab6fe34a9`; report SHA-256 `927ca61b252d9688c6aad26a32ded8d259ad685910a839bf1c220b14cd235581`, completed `2026-10-25T21:44:23.845596+00:00`. Checker `alternatives-envelope-v1`, segmentation `sentence-boundaries-v1`, evaluator `alternatives-evaluator-v1`. Candidate `DeepSeek-V4.1-Flash`, requested/returned model `deepseek-flash`, temperature 0, non-thinking, JSON mode, no tools. Every final attempt has known usage, model recorded and zero unresolved exposure. Final batch: 72 dispatches, USD 0.0287913.

Reviewed both decoded options in every cell against the preauthored meaning assertions: Mira remains the subject, sending is prohibited (not merely optional/unnecessary or impossible), 12 reports and the before-Friday boundary remain, no quoted instruction executes, both alternatives differ from the selected text and each other, and Chinese outputs use Simplified script. EN “can't” and ZH “不能” in the casual cells read deontically in the supplied prohibition context; the earlier ZH inability construction “发不了” is absent. Enthusiastic exclamation/emphasis preserves the prohibition without adding finality or another factual clause. Romanian permission constructions use correct dative Mirei. Reference constraints and synthetic context remain unchanged.

Register separation is intentionally conservative when a style/tone could soften the prohibition; several modes share wording. This narrow development slice does not grade broad style diversity or establish language qualification. No calibrated judge, independent release review, 360-set qualification run, performance measurement or serving/deployment qualification is claimed.

Aggregate across access plus nine preserved batches: **647 provider dispatches, USD 0.2491128 known spend, USD 0.022568 unresolved retained, USD 0.2716808 conservative exposure**; remaining original USD 5 capacity is 4.7283192. The unresolved exposure belongs to run 04 and is not treated as zero cost. Access actual USD 0.0001203; its original bound was released on authoritative known usage. Final review is separately saved as `artifacts/m045/alternatives-live-09-review.json`; original report status remains awaiting-semantic-review, with this linked disposition providing the review.

| Cell | Option 1 | Option 2 | Review |
| --- | --- | --- | --- |

| dev-en-correctionOnly-0 | Mira is not permitted to send the 12 reports before Friday. | Mira is prohibited from sending the 12 reports before Friday. | Both accepted |
| dev-en-simple-1 | Mira is not allowed to send the 12 reports before Friday. | Mira is prohibited from sending the 12 reports before Friday. | Both accepted |
| dev-en-business-2 | Mira is not permitted to send the 12 reports before Friday. | Mira is prohibited from sending the 12 reports before Friday. | Both accepted |
| dev-en-academic-3 | Mira is prohibited from sending the 12 reports before Friday. | Mira is not permitted to send the 12 reports before Friday. | Both accepted |
| dev-en-casual-4 | Mira can't send the 12 reports before Friday. | Mira isn't allowed to send the 12 reports before Friday. | Both accepted |
| dev-en-enthusiastic-5 | Mira absolutely must not send the 12 reports before Friday! | Mira is strictly prohibited from sending the 12 reports before Friday! | Both accepted |
| dev-en-friendly-6 | Mira isn't permitted to send the 12 reports before Friday. | Mira is prohibited from sending the 12 reports before Friday. | Both accepted |
| dev-en-confident-7 | Mira is not permitted to send the 12 reports before Friday. | Mira is prohibited from sending the 12 reports before Friday. | Both accepted |
| dev-en-diplomatic-8 | Mira is not permitted to send the 12 reports before Friday. | Mira is prohibited from sending the 12 reports before Friday. | Both accepted |
| dev-ru-correctionOnly-0 | Мире запрещено отправлять 12 отчётов до пятницы. | Мира не имеет права отправлять 12 отчётов до пятницы. | Both accepted |
| dev-ru-simple-1 | Мире запрещено отправлять 12 отчётов до пятницы. | Мира не имеет права отправлять 12 отчётов до пятницы. | Both accepted |
| dev-ru-business-2 | Мире запрещено отправлять 12 отчётов до пятницы. | Мира не имеет права отправлять 12 отчётов до пятницы. | Both accepted |
| dev-ru-academic-3 | Мире запрещено отправлять 12 отчётов до пятницы. | Мира не имеет права отправлять 12 отчётов до пятницы. | Both accepted |
| dev-ru-casual-4 | Мира не имеет права отправлять 12 отчётов до пятницы. | Мире запрещено отправлять 12 отчётов до пятницы. | Both accepted |
| dev-ru-enthusiastic-5 | Мире запрещено отправлять 12 отчётов до пятницы! | Мира не имеет права отправлять 12 отчётов до пятницы! | Both accepted |
| dev-ru-friendly-6 | Мире запрещено отправлять 12 отчётов до пятницы. | Мира не имеет права отправлять 12 отчётов до пятницы. | Both accepted |
| dev-ru-confident-7 | Мире запрещено отправлять 12 отчётов до пятницы. | Мира не имеет права отправлять 12 отчётов до пятницы. | Both accepted |
| dev-ru-diplomatic-8 | Мире не разрешается отправлять 12 отчётов до пятницы. | Мира не имеет права отправлять 12 отчётов до пятницы. | Both accepted |
| dev-ro-correctionOnly-0 | Mira nu are voie să trimită cele 12 rapoarte înainte de vineri. | Mirei nu îi este permis să trimită cele 12 rapoarte înainte de vineri. | Both accepted |
| dev-ro-simple-1 | Mira nu are voie să trimită cele 12 rapoarte înainte de vineri. | Mirei nu îi este permis să trimită cele 12 rapoarte înainte de vineri. | Both accepted |
| dev-ro-business-2 | Mirei nu îi este permis să trimită cele 12 rapoarte înainte de vineri. | Mira nu are voie să trimită cele 12 rapoarte înainte de vineri. | Both accepted |
| dev-ro-academic-3 | Mirei nu îi este permis să trimită cele 12 rapoarte înainte de vineri. | Mira nu are voie să trimită cele 12 rapoarte înainte de vineri. | Both accepted |
| dev-ro-casual-4 | Mira nu are voie să trimită cele 12 rapoarte înainte de vineri. | Mirei nu îi este permis să trimită cele 12 rapoarte înainte de vineri. | Both accepted |
| dev-ro-enthusiastic-5 | Mira nu are voie să trimită cele 12 rapoarte înainte de vineri! | Mirei nu îi este permis să trimită cele 12 rapoarte înainte de vineri! | Both accepted |
| dev-ro-friendly-6 | Mira nu are voie să trimită cele 12 rapoarte înainte de vineri. | Mirei nu îi este permis să trimită cele 12 rapoarte înainte de vineri. | Both accepted |
| dev-ro-confident-7 | Mira nu are voie să trimită cele 12 rapoarte înainte de vineri. | Mirei nu îi este permis să trimită cele 12 rapoarte înainte de vineri. | Both accepted |
| dev-ro-diplomatic-8 | Mirei nu îi este permis să trimită cele 12 rapoarte înainte de vineri. | Mira nu are voie să trimită cele 12 rapoarte înainte de vineri. | Both accepted |
| dev-zh-correctionOnly-0 | 米拉在星期五之前禁止发送12份报告。 | 米拉不被允许在星期五之前发送12份报告。 | Both accepted |
| dev-zh-simple-1 | 米拉在星期五之前不许发送12份报告。 | 星期五之前，米拉不得发送12份报告。 | Both accepted |
| dev-zh-business-2 | 米拉在星期五之前发送12份报告是不被允许的。 | 米拉被禁止在星期五之前发送12份报告。 | Both accepted |
| dev-zh-academic-3 | 米拉在星期五之前发送12份报告的行为是不被允许的。 | 米拉被禁止在星期五之前发送12份报告。 | Both accepted |
| dev-zh-casual-4 | 米拉在星期五之前不许发送12份报告。 | 星期五之前，米拉不能发送那12份报告。 | Both accepted |
| dev-zh-enthusiastic-5 | 米拉在星期五之前不得发送12份报告！ | 星期五之前，米拉绝对不许发送那12份报告！ | Both accepted |
| dev-zh-friendly-6 | 米拉在星期五之前是不允许发送12份报告的。 | 米拉在星期五之前发送12份报告是不被允许的。 | Both accepted |
| dev-zh-confident-7 | 米拉在星期五之前不得发送12份报告。 | 星期五之前，米拉不允许发送12份报告。 | Both accepted |
| dev-zh-diplomatic-8 | 米拉在星期五之前发送12份报告是不被允许的。 | 米拉在星期五之前发送12份报告属于禁止行为。 | Both accepted |
