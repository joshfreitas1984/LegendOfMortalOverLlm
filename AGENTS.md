# AGENTS.md

Universal rules for any AI coding agent working in this repository, regardless of vendor. This
file exists so no repository rule lives in only one vendor-specific format
(`.github/copilot-instructions.md`, `CLAUDE.md`, etc.) — mirrors the same pattern used by
`FanslationStudio.LlmKit` and `DragonHierOverLlm`.

## Start here

- [`docs/README.md`](docs/README.md) is the canonical documentation hub — project overview,
  documentation taxonomy, and a "where should I look?" task table.

## Repository-wide rules

- This repository contains independent sub-projects (`Translate/`, `Tests/`, `Files/`,
  `LegendOfMortalPlugin/`, and optional `SharedAssembly/`). Keep reusable workflow code in
  `Translate/`, numbered workflow facts and regression tests in `Tests/`, and runtime patches in
  `LegendOfMortalPlugin/`.
- `Translate/` consumes `FanslationStudio.LlmKit` (sibling repo `../FanslationStudio.LlmKit`) via a
  project reference, not a NuGet package — changes there take effect immediately here without a
  version bump. LlmKit's `TranslationLine`/`TranslationSplit`/`FieldTemplate` shape (the
  Line/Split/Template data model) is a golden-rule-protected contract owned by LlmKit, not this
  repo: never change its shape here, only propose additive changes upstream in LlmKit itself.
- LlmKit-internal behavior (the QC/quality-control pass, packaging/reconstruction rules, retry and
  escalation mechanics) is documented in `FanslationStudio.LlmKit`'s own `AGENTS.md`/`docs/` — read
  it there rather than duplicating or re-deriving it in this repo.
- `SharedAssembly/` is shared between `Translate/` and `LegendOfMortalPlugin/` only — it is a different project to
  `FanslationStudio.LlmKit` and is not covered by LlmKit (runtime patching/resizing concerns are out
  of scope for LlmKit).
- Keep this file short and operational. Put current behavior in `docs/features/`, investigations
  in `docs/investigations/`, plans in `docs/plans/`, and durable structure in `docs/architecture/`.
- `docs/KNOWN_ISSUES.md` is an index only. Do not create project-local issue indexes or docs trees.

## Where to look for more detail

See [`docs/README.md`](docs/README.md)'s "Where should I look?" table for task-specific starting
points (translation pipeline work, QC/packaging in LlmKit, known issues, etc.).
