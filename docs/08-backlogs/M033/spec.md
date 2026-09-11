# M033 — Selected specification
Selected items: BI-033. Status: draft; completion evidence in tasks.md.
Sources: [context manifest](context.json); [backlog](backlog.md).

## Scope

Included: one small standalone consumer example, executed without the
SPA and without React, that drives the real local host through the
M026 translation and M027 rewriting operations with a local-account
Bearer token: sign-in/token acquisition, one translation submit,
one rewrite submit with exactly one selected mode, current-day usage
reads, status-based outcome recovery, duplicate/conflict identity
handling, and classified-failure surfacing. Plus a reviewed
generated-contract delta proving the committed OpenAPI YAML and
generated TypeScript client agree with actual handler semantics for
every wire shape the sample touches. Requirements FR-035/036/037;
RG-005 (independent-consumer portion).

Dependencies (Done, reused not re-proven): M026 (translation
operation, fake provider, admission/settlement/snapshot behavior);
M027 (rewriting operation with selected mode). The sample exercises
their behavior through public HTTP only; it re-proves no
accounting, eligibility, deadline or snapshot policy.

Exclusions: SPA/browser journeys (M028–M032 own those); live
provider dispatch, candidate qualification and serving/billing
attribution (Q-001 remainder); actual monetary cap amount (Q-001);
sentence alternatives or sentence/version association (DF-001, per
FR-035); public operation-cancellation endpoint (none for MVP);
operation-history listing beyond metadata-only status replay;
account deletion/backup retention claims (Q-004); compatibility or
versioning guarantees (P-006 remains proposed); email delivery
(M034).

## Acceptance

| AC | Observable success/failure/boundary | Upstream source/ID |
| --- | --- | --- |
| AC-001 | Without executing React, the sample authenticates a local account, submits one valid translation identity and prints the complete translated text with its charged count, charge day and fresh usage snapshot; a later status read reports the terminal outcome metadata. | FR-003/035/036; API-AC-013 (consumer portion) |
| AC-002 | The same sample run submits one valid rewrite identity with exactly one selected mode and prints the complete same-language result with its charged count, charge day and fresh usage snapshot. | FR-012/035/036; API-AC-013 (consumer portion) |
| AC-003 | The sample demonstrates recovery over plain HTTP: a status re-read after completion returns outcome metadata without new dispatch or charge (output unavailable on replay); a duplicate same-identity/same-payload submit returns the original outcome; a same-identity/different-payload submit surfaces the 409 conflict; a usage read shows the settled snapshot. | FR-026/028/037; API §6.1; API-AC-004/005/006 (consumer portions) |
| AC-004 | The sample surfaces classified failures distinctly from success with the API recovery guidance: one invalid-input rejection (4xx input category, zero charge) and one unauthenticated call (401) are shown with their Problem Details category, and neither is presented as output. | FR-037; API §8; API-AC-013 (consumer portion) |
| AC-005 | Regenerated OpenAPI YAML and TypeScript client show zero drift against the committed artifacts, and a recorded review confirms every touched shape (submit/status/usage envelopes, status codes, error categories, snapshot fields) matches actual runtime behavior, not just generation parity. | API §9; API-AC-014 (consumer delta); V-009 |
| AC-006 | The sample uses synthetic fixture text only, redacts the Bearer token from its output, and no source/result text, credential or provider secret appears in sample logs beyond the sample's own displayed results. | NFR-004; API §9 |

## Constraints and decisions

Q-003 shared wire/counting/recovery semantics are reused from
M026/M027; this slice fixes no new wire names. Q-006 remaining
account details do not block this slice; existing local-account
auth behavior is reused. Q-001 cap/billing verification and Q-004
retention/deletion stay open and make no new claim here. Q-005
corpus qualification is out of scope; the deterministic fake
provider is demonstration transport, not model acceptance. The
sample runs against an isolated local host with a fake provider
and seeded local account; no live credentials, email delivery or
paid serving is involved. Human gates: none required.
