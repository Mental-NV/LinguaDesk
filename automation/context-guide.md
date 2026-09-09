# Bounded documentation context

## Research and design decision

Research checked 2026-09-09. OpenAI documents exact rendered-prefix reuse: stable instructions/reference material should precede changing content. Model, tools, settings, compaction, routing and cache lifetime can affect reuse; supported models differ in breakpoint controls and charges. Splitting Markdown files alone does not guarantee a cache hit. Measure actual usage rather than assuming a filename order changes API caching. This runner invokes Codex CLI and does not configure API cache breakpoints. [Official prompt caching guidance](https://developers.openai.com/api/docs/guides/prompt-caching)

OpenAI's skill design uses progressive disclosure: advertise a small description, load detailed instructions when needed, and keep supporting references separate. We apply that pattern to document #0 and stage-specific procedures; this does not require installing a new skill. [Official skill documentation](https://learn.chatgpt.com/docs/build-skills)

Our repository design prioritizes fewer necessary input tokens. A cached irrelevant document still occupies context. Use a small index, stage routing, one fact owner, exact section/ID selections and bounded outputs. Keep stable contracts separate from status/evidence. Do not pad prompts to reach caching thresholds, add timestamps to stable prefixes, rewrite plans for checkbox changes or read archives/other milestones by default. These are project workflow choices, not claims of measured cache savings.

## What changed

- All 24 M001–M006 artifacts moved into `docs/08-backlogs/<scope-id>/`, retaining acceptance, historical source revisions and completion evidence. Future packages use stable scope IDs only.
- #0 is a compact authority/routing index. Authoring procedures/templates are separate from execution instructions.
- Historical discovery, UX research, initial source tables and chronological roadmap notes moved to `docs/archive/`. Git retains full earlier versions. Duplicate completion narratives were removed where package evidence already owns the fact.
- Large UX/API/verification documents delegate named sections to smaller subject files; original section headings remain navigation stubs. ADRs have individual stable-ID files. Current delivery status, product questions and coverage are separate from stable contracts.
- `spec.md` owns selected behavior, `plan.md` owns design and AC → task → check mapping, `tasks.md` owns mutable progress/evidence. A small manifest supplies canonical excerpts alongside the plan instead of copying every specification into it.

## Select sources

From the repository root, with Python 3.9+:

```sh
python3 automation/context.py outline docs/04-llm-specification.md
python3 automation/context.py read docs/04-llm-specification.md --heading '3. Input eligibility and operation behavior'
python3 automation/context.py read docs/01-PRD.md --ids FR-004 FR-005 FR-006 FR-007
python3 automation/context.py read docs/07-roadmap.md --ids M015
```

Headings are exact titles without the leading `#`. A heading includes subsections until the next equal/higher-level heading. ID selection matches exact table-row first cells; grouped IDs require their containing heading. Selection fails if an ID/heading is missing or ambiguous. `outline` and `read` do not infer related rules: include the table's scope conventions, shared definitions and relevant proposal/question dispositions yourself. References do not authorize reading all linked files transitively.

## context.json schema

Author this file in the selected milestone folder, beside the four Markdown files:

```json
{
  "version": 1,
  "scope": "M015",
  "sources": [
    {
      "path": "docs/01-PRD.md",
      "ids": ["FR-004", "FR-005", "FR-006", "FR-007"],
      "use": "excerpt",
      "reason": "Selected language eligibility and full-input boundaries"
    },
    {
      "path": "docs/04-llm-specification.md",
      "heading": "3. Input eligibility and operation behavior",
      "use": "excerpt",
      "reason": "Canonical eligibility behavior needed during implementation"
    },
    {
      "path": "docs/decisions/ADR-012.md",
      "use": "reference",
      "reason": "Open if the implementation changes the independent AI boundary"
    }
  ]
}
```

This illustrates syntax, **not a complete or ready M015 manifest**. The planner must select all applicable architecture, prompt/parser/error/attempt rules, verification methods, dependencies and scope/status constraints. M015 remains unselected. Each source needs `path`, `use` and `reason`; add either `heading` or `ids`, or omit both to select the whole file. Paths are repository-relative. `excerpt` content enters the packet; `reference` content is hashed for freshness but only its locator/reason enters the packet. Small code/generated files can be whole-file references when needed. Do not include credentials, private data or bulky generated output.

```sh
python3 automation/context.py lock M015
python3 automation/context.py check M015
python3 automation/context.py packet M015
python3 automation/context.py measure M015
python3 automation/context.py audit
```

`lock` records baseline Git commit, selected-content SHA-256 hashes, the source map, governance and spec/plan hashes. Run only after reviewing current inputs and readiness. `check` reports freshness without printing sources. Source heading changes, selected row changes, source-map changes, missing files, or spec/plan edits fail; unrelated source sections and task progress do not. A selected parent section includes its children, so changing any child invalidates that selection. File-level reference changes invalidate the whole reference.

`packet` checks freshness first, then prints #0/execution rules, selected excerpts in deterministic path/selector order, selected scope and on-demand locators, spec/plan, then mutable backlog/tasks. It does not follow links automatically. Codex may place the milestone prompt or prior tool messages before this content; deterministic packet ordering cannot ensure identical rendered prefixes across separate runs. Missing/stale locks stop execution. A lock cannot prove that a planner found every applicable requirement or that current code satisfies dependencies; review Git changes and explicitly expand context when needed. Completed pre-convention packages have no retroactive lock and are not recertified by the move.

`measure` reports words/UTF-8 bytes and a rough bytes/4 token estimate for the full execution packet. It is not exact tokenization, billed usage or cache-hit measurement. Compare the same milestone/scope before and after, including governance, excerpts and task state; do not compare a tiny excerpt with the entire old document set as if both were complete plans. When telemetry is available, record actual input/cached-input/output usage and model/settings separately. We have not run a paid milestone or measured cache reuse in this documentation change.

## Maintenance

Run `python3 automation/context.py audit` after moving/renaming documents; it checks local Markdown links/headings and the four-file stable-ID package layout. It ignores fenced examples and external URLs. It does not verify remote pages or semantic authority/coverage. Run `python3 -m unittest discover -s automation/tests` when changing the context tool or runner behavior. Do not run milestone automation merely to test documentation: it executes coding runs and creates commits.

Preserve current requirement/scenario IDs and deferred/proposed dispositions when compacting. Review source ownership before deleting duplicated prose; preserve unique design rules and actual evidence. Archive rationale with a clear non-normative label, not current requirements. Split by audience/change frequency rather than arbitrary file length. Repeatedly loading all modules defeats the split.

The [refactor verification record](../docs/research/context-refactor-2026-09-09.md) records baseline size, preserved catalogs/evidence and the checks performed. It is historical measurement evidence, not routine execution context.
