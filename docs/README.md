# LegendOfMortalOverLlm — Documentation Hub

# Latest release
Extract the [Latest Release](https://github.com/joshfreitas1984/LegendOfMortalOverLlm/releases) into your `<Game Folder>` folder where Mortal.exe is.

# Contacting us
You can join us here: [Discord](https://discord.gg/sqXd5ceBWT)

This is the canonical navigation entry point for the repository. It exists so that a human or an
AI agent (Copilot, Claude Code, or otherwise) can find the right source of truth without relying on
vendor-specific memory.

## Repository overview

LegendOfMortalOverLlm builds an English fan-translation patch for the Unity game *Legend of
Mortal*, plus the tooling used to translate the game's dumped text via `FanslationStudio.LlmKit`.
The sub-projects are independent and are documented separately:

| Project | Purpose |
| --- | --- |
| [`Translate/`](../Translate/) | Extracts game text data, drives the LLM translation workflow (built on `FanslationStudio.LlmKit` — a **sibling repo** at `../FanslationStudio.LlmKit`, referenced via project reference, not a NuGet package), and repackages translated data for `Plugin`. |
| [`LegendOfMortalPlugin/`](../LegendOfMortalPlugin/) | Mono runtime plugin that injects translated text into the running game, including string-table dump/injection patches. |
| [`SharedAssembly/`](../SharedAssembly/) | Contracts/services shared between `Translate/` and `LegendOfMortalPlugin/` — distinct from `FanslationStudio.LlmKit`, and out of scope for it (runtime patching/resizing concerns aren't covered by LlmKit). |
| [`Tests/`](../Tests/) | Test suite for the translation pipeline. |
| [`Files/`](../Files/) | Working-directory data: raw dumped game text, glossary, manual translations, converted/translated output, and the mod drop-in folder consumed by `Plugin`. |

## Documentation taxonomy

- **Scoped instructions** in `.github/instructions/` are concise, path-specific operational rules.
- **`docs/KNOWN_ISSUES.md`** is an index only; full investigations live under `docs/investigations/`.
- **`docs/features/`** contains current behavior and project references.
- **`docs/investigations/`** contains incidents, bug investigations, and postmortems.
- **`docs/plans/`** contains active implementation plans.
- **`docs/architecture/`** contains durable structural references and decisions.
- **`AGENTS.md`** (repo root) — short, auto-loaded, current-state rules. `CLAUDE.md` is a thin
  pointer to it.
- [`Files/Learnings.md`](../Files/Learnings.md) holds informal, ongoing prompt-engineering/glossary
  lessons — not a topic doc, and not indexed here.

Shared-library mechanics belong in the sibling `FanslationStudio.LlmKit/docs/` tree; link to them
instead of duplicating their implementation details here.

## Where should I look?

| Task | Start here |
| --- | --- |
| Work on the game text extraction → LLM translation → repackaging pipeline | [`Translate/GameFileHandling.cs`](../Translate/GameFileHandling.cs), [`Translate/TranslationExport.cs`](../Translate/TranslationExport.cs), [`Translate/TranslationPackaging.cs`](../Translate/TranslationPackaging.cs) |
| Understand the `Files/` raw/converted/mod data layout | [`Translate/`](../Translate/) source above and [`Files/`](../Files/) itself |
| Fix or extend the runtime string-table dump/injection patches | [`StringTableDumpPatches.cs`](../LegendOfMortalPlugin/StringTableDumpPatches.cs), [`StringTableInjectionPatches.cs`](../LegendOfMortalPlugin/StringTableInjectionPatches.cs) |
| Investigate a known issue | [`KNOWN_ISSUES.md`](KNOWN_ISSUES.md) |
| Informal prompt-engineering/glossary lessons | [`Files/Learnings.md`](../Files/Learnings.md) |
| Work on the QC (quality review) pass | [`quality-review-pass.md`](../../../FanslationStudio.LlmKit/docs/features/translation-pipeline/quality-review-pass.md) (**sibling repo**) |
| Change or turn off joining of mid-sentence line breaks in packaged text | [`soft-line-break-joining.md`](features/translation-pipeline/soft-line-break-joining.md) |
| Investigate a packaging issue | [`packaging-reference.md`](../../../FanslationStudio.LlmKit/docs/features/packaging/packaging-reference.md) (**sibling repo**) |
| Install/play the released patch | [`readme.md`](../readme.md) |
