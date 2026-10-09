# LegendOfMortalOverLlm — Documentation Hub

# Latest release
## Install with the installer (recommended)

1. Download the installer: [Windows](https://github.com/joshfreitas1984/LegendOfMortalOverLlm/releases/download/installer/Installer-win-x64.exe) or [Linux](https://github.com/joshfreitas1984/LegendOfMortalOverLlm/releases/download/installer/Installer-linux-x64). These links always give you the newest installer.
2. Run it. It finds the game through Steam (use **Browse** if it can't), installs BepInEx, and installs the [latest patch release](https://github.com/joshfreitas1984/LegendOfMortalOverLlm/releases/latest). Press **Install / Update**.
3. Start the game once and let it reach the main menu, so BepInEx can generate its files.

Windows may warn "Windows protected your PC" because the installer isn't code-signed. Choose **More info**, then **Run anyway**.

**Linux (Steam/Proton):** the game and BepInEx are the Windows builds. Paste this into the game's Steam **Properties > Launch Options** (the installer shows it with a Copy button):

```
WINEDLLOVERRIDES="winhttp=n,b" %command%
```

To remove the patch, run the installer and press **Uninstall patch**. It removes the patch files and leaves BepInEx in place.

## Updates

When the game starts it checks for a newer patch. If there is one, an **Update available** window appears about 15 seconds later. **Update now** downloads it, closes the game, applies the update and restarts the game through Steam (on Linux you may need to start the game yourself afterwards). **Later** hides it until the next start. If a check or download fails, nothing changes and the reason is written to the BepInEx log.

Your settings files under `BepInEx/config` are never overwritten by an update. To turn the check off, set `Enabled = false` under `[Updates]` in `BepInEx/config/FanslationStudio.Plugins.UIEditor.cfg`. Updates only work for a patch installed with the installer.

## Manual install

Install [BepInEx 5.4.23.3 for Windows **x86** (32-bit)](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.3) (`BepInEx_win_x86_5.4.23.3.zip`) into your `<Game Folder>` where Mortal.exe is, then extract the [latest release zip](https://github.com/joshfreitas1984/LegendOfMortalOverLlm/releases/latest) over it. Release zips no longer contain BepInEx itself. A manual install won't get the in-game update prompt.

After installing BepInEx, open `doorstop_config.ini` in the game folder and set `dll_search_path_override = "BepInEx\core"` (it is blank by default). Legend of Mortal ships its own copy of MonoMod, and without this setting BepInEx loads it, fails on startup and the game runs untranslated (a `preloader_*.log` file appears in the game folder). The installer does this for you.

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
| Work on the QC (quality control) pass | [`quality-control-pass.md`](../../../FanslationStudio.LlmKit/docs/features/translation-pipeline/quality-control-pass.md) (**sibling repo**) |
| Change or turn off joining of mid-sentence line breaks in packaged text | [`soft-line-break-joining.md`](features/translation-pipeline/soft-line-break-joining.md) |
| Investigate a packaging issue | [`packaging-reference.md`](../../../FanslationStudio.LlmKit/docs/features/packaging/packaging-reference.md) (**sibling repo**) |
| Install/play the released patch | [`readme.md`](../readme.md) |
