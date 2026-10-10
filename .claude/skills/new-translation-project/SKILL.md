---
name: new-translation-project
description: Scaffolds a brand-new sibling "OverLlm" game-translation repo (e.g. DragonHierOverLlm, LegendOfMortalOverLlm, WanXiangOverLlm) that consumes FanslationStudio.LlmKit via project reference. Asks a handful of setup questions, then creates the new repo's directory/git init, working-directory data layout, starter Config.yaml, BepInEx plugin project (IL2CPP or Mono), installer host (installer.json), release packaging workflows and Files/Packaging, AGENTS.md/CLAUDE.md/docs/README.md, and copies this repo's .claude/skills/ into it. Use when starting a brand-new game translation project from scratch, not for changes to an existing downstream repo.
---

# Scaffold a new translation project

This skill runs from `FanslationStudio.LlmKit`'s own working directory and creates a **new sibling
repo** next to it (same convention as `DragonHierOverLlm`, `LegendOfMortalOverLlm`, `WanXiangOverLlm`).
It gets a project off the ground; it does not write the game-specific dumper/patch logic. The
baseline layout comes from the canonical downstream translation-project structure documentation —
don't reinvent it. The required baseline is separate `Translate/` and `Tests/` projects; do not
scaffold a combined tooling/test project. **`docs/architecture/downstream-project-structure/downstream-project-structure.md`** (and its
`downstream-test-organization.md`/`downstream-config-shape.md`/`downstream-plugin-project-layout.md`/
`downstream-repository-docs-taxonomy.md`
siblings) is now the primary source for what to scaffold — it's a deliberately-maintained target
shape, not a snapshot of whatever `DragonHierOverLlm`/`WanXiangOverLlm` currently look like. Read
those docs first; cross-check `WanXiangOverLlm` (Mono) or `DragonHierOverLlm` (IL2CPP) only where
the shape docs don't pin a concrete detail the scaffold needs, e.g. an exact package version to
pin.

1. **Ask the setup questions up front — do not scaffold anything until answered:**
   - Game name (used for the repo folder name `<GameName>OverLlm`, the plugin `RootNamespace`/
     `AssemblyName`, and the BepInEx plugin GUID).
   - Path to the game's install directory on disk (used for `GameDir`/interop paths in the plugin
     csproj).
   - BepInEx version to target.
   - IL2CPP or Mono? If unsure, check whether `GameAssembly.dll` sits next to the game's `.exe` —
     present means IL2CPP, absent (with `Assembly-CSharp.dll` directly under `<Game>_Data\Managed`)
     means Mono. This decides the whole plugin csproj shape (step 5).
   - Unity version, if known/discoverable (e.g. from `<Game>_Data\globalgamemanagers` or the
     game's `Player.log`) — used for `UnityEngine.Modules` version on Mono projects only.
   - Where the new repo should live on disk — default to a sibling of `FanslationStudio.LlmKit`,
     i.e. `../<GameName>OverLlm` relative to this repo's root.

2. **Create the new repo directory and initialize git.** `mkdir <path>` then `git init` inside it.
   Do not commit yet — later steps still need to populate it.

3. **Scaffold the working-directory data layout** under the new repo's `Files/`, per
   `docs/architecture/downstream-project-structure/downstream-project-structure.md`'s required project layout section (cross-check
   `WanXiangOverLlm/Files/`'s real layout for exact folder names if the doc is ambiguous):
   `Raw/Dumped/` (raw game-dumped files land here),
   `Raw/Export/` (per-file `.yaml` export of the split lines), `Converted/` (translated `.yaml`
   output), `Mod/` (final packaged output the game-facing plugin consumes), `Glossary/` (empty,
   holds `Glossary.yaml` once populated), and an empty prompt-override folder (see
   `customPromptsPath` in Config.yaml — leave the folder empty, don't invent prompt files). Add a
   `Files/Files.csproj` (`net9.0`, matching `WanXiangOverLlm/Files/Files.csproj`) so the data folder
   is a buildable/browsable project like the templates, with `<Folder Include="Mod\..." />` entries
   for the otherwise-empty output folders so git/VS keep them.

4. **Create a starter `Files/Config.yaml`** based on `docs/architecture/downstream-project-structure/downstream-config-shape.md`'s documented
   shape (cross-check `WanXiangOverLlm/Files/Config.yaml` only for a field the doc doesn't pin): a
   `models:` list (at least one entry pointing at whatever local model preset this project
   will use — leave `modelPreset`/`model` as placeholders the user fills in), a `qualityControl:`
   block using LlmKit's current defaults (`enabled: true`, placeholder `modelName`,
   `minAcceptableScore: 70`, leave `autoAcceptDefectCategories` empty/commented — that list is
   populated later from real hand-triage, never guessed up front), `glossaryPreset`,
   `useContinuousWorkerPool: true`, and placeholder `maxConcurrency`/`retryCount`/`batchSize`. Also
   create an empty `Files/ManualTranslations.yaml`. Do **not** add `translationAssessment`, `qualityControlAssessment`, a
   gold set or assessment tests: model and QC assessments run from LlmKit's `FanslationStudio.LlmKit.Assessments`, and
   glossary lines follow the preset-or-game rubric in `docs/features/translation-pipeline/glossary.md`. Note: the actual per-file `TextFileToSplit[]`
   list is **not** in `Config.yaml` — it's C# in the tooling project (see `GameTextFiles.cs` in
   WanXiang's `Translate/` for the pattern) — so step 5's tooling project should get a starter
   `GameTextFiles.cs` with an empty `TextFilesToSplit` array for the user to fill in once dumping
   is wired up.

    Create the two separate code projects required by the canonical layout:
    - `Translate/Translate.csproj` contains reusable game-specific extraction, translation,
       packaging, configuration, and workflow code. It references `FanslationStudio.LlmKit`.
    - `Tests/Tests.csproj` references `Translate/` and contains xUnit regression tests plus the
       numbered manual pipeline/runbook facts. Keep test execution and operational steps here;
       `Translate/` is not a combined test runner.

5. **Scaffold the BepInEx plugin project**, per `docs/architecture/downstream-project-structure/downstream-plugin-project-layout.md`'s IL2CPP/
   Mono branches (cross-check `WanXiangOverLlm`/`DragonHierOverLlm` only for an exact package
   version pin the doc doesn't specify). Create `<GameName>Plugin/<GameName>Plugin.csproj` (or
   `EnglishPatch/`, following WanXiang's naming) with a `ProjectReference` to LlmKit at the exact
   relative path used by every existing consumer:
   `../../FanslationStudio.LlmKit/FanslationStudio.LlmKit/FanslationStudio.LlmKit.csproj` (verified
   against `WanXiangOverLlm/Translate/Translate.csproj`) from a project one level under the new
   repo's root, adjusting `../..` to match actual nesting depth. Branch on the IL2CPP/Mono answer
   from step 1:
   - **Mono** (WanXiang's shape): `TargetFramework netstandard2.1`, `PackageReference
     BepInEx.Core 5.*`, `BepInEx.Analyzers 1.*`, `BepInEx.PluginInfoProps 2.*`,
     `UnityEngine.Modules` pinned to the discovered Unity version, plain `<Reference>` +
     `HintPath` entries straight into `<Game>_Data\Managed\*.dll` for any Unity/game assemblies
     the plugin needs.
   - **IL2CPP** (DragonHierOverLlm's shape): `TargetFramework net6.0`,
     `PackageReference BepInEx.Unity.IL2CPP` (pin to the BepInEx version from step 1, e.g.
     `6.0.0-be.785`), `HintPath` references into `BepInEx\interop\*.dll` (generated by BepInEx's
     IL2CPP unhollower/interop step — this must be run once against the real game before those
     DLLs exist), plus `Il2CppSystem`/`Il2Cppmscorlib` references. If any dependency needs to be
     embedded into the plugin DLL (BepInEx's IL2CPP SDK sets
     `CopyLocalLockFileAssemblies=false` project-wide), add `Costura.Fody`/`Fody` like
     DragonHierOverLlm does — skip this unless a real dependency turns out to need it.
   Add a `PostBuild` target that `XCOPY`s the built DLL into `<GameDir>\BepInEx\plugins`. Write a
   minimal `Plugin.cs`: `[BepInPlugin(guid, name, version)]` class deriving from `BasePlugin`
   (IL2CPP) or `BaseUnityPlugin` (Mono), empty `Load()`/`Awake()` — no dump/patch logic, that's
   game-specific work for later.

6. **Create the new repo's own `AGENTS.md` and `CLAUDE.md`**, templated from
   `WanXiangOverLlm/AGENTS.md` and `WanXiangOverLlm/CLAUDE.md`: state that the tooling project
   consumes `FanslationStudio.LlmKit` via project reference (not a NuGet package) from sibling repo
   `../FanslationStudio.LlmKit`, that the `TranslationLine`/`TranslationSplit`/`FieldTemplate`
   contract is LlmKit-owned (propose changes upstream, never redefine it downstream), and point to
   the new repo's own `docs/README.md` as the documentation hub. `CLAUDE.md` stays a thin pointer to
   `AGENTS.md` + `docs/README.md`, same as every existing repo.

7. **Create the new repo's `docs/README.md`**, per `docs/architecture/downstream-project-structure/downstream-repository-docs-taxonomy.md`'s file
   list/shape (cross-check `WanXiangOverLlm/docs/README.md`'s structure for wording/formatting
   details the taxonomy doc doesn't spell out): repository overview table of sub-projects,
   documentation taxonomy, "Where should I look?" table. From day one, include rows pointing at
   LlmKit's canonical docs so this repo doesn't accumulate the cross-referencing debt older repos
   had before this restructuring: `../FanslationStudio.LlmKit/docs/features/translation-pipeline/quality-control-pass.md`
   for QC questions, `../FanslationStudio.LlmKit/docs/features/packaging/packaging-workflows.md` for packaging
   questions, and `../FanslationStudio.LlmKit/docs/architecture/downstream-project-structure/downstream-project-structure.md` for future
   structure reconciliation.

8. **Copy all skill directories in this repo's `.claude/skills/`** into the new repo's
   `.claude/skills/` verbatim, so it has the same canonical skill set from day one. This currently
   includes `investigate-qc-issue`, `investigate-packaging-issue`, `investigate-missing-translation`,
   and `new-translation-project`; keep this step aligned with `Tests/SkillSyncTests.cs`, which copies
   every source skill directory. Then flag, as a manual follow-up for whoever runs this skill (do not
   edit the file yourself): add the new repo's folder name to `DownstreamRepos` in
   `Tests/SkillSyncTests.cs` in `FanslationStudio.LlmKit`, so future skill updates sync to it
   automatically too.

9. **Create the `docs/README.md`** documentation hub with a release/contact block at the top.
   Immediately after the project title, include a `# Latest release` section linking to the
   repository's GitHub releases page and explaining where to extract the release. Include a
   `# Contacting us` section containing the repository's self-link (`#contacting-us`) followed by
   the shared Discord invite: `https://discord.gg/sqXd5ceBWT`.

10. **Scaffold the installer, auto-updater and release packaging.** The reusable code lives in
   LlmKit (`FanslationStudio.Installer.App`, `Installer.Core`, `LlmKit.Release`); the new repo only
   gets a thin host and data. Copy the shape from `LegendOfMortalOverLlm` (Mono) or
   `DragonHierOverLlm` (IL2CPP). Do not reopen these decisions: unsigned installer, no GitHub
   Actions (plugins reference copyrighted game DLLs, so releases are built locally), manual
   publishing, installer on one rolling `installer` pre-release, BepInEx never inside the patch zip.
   - **Ask first** (extra setup questions): Steam app ID, Steam folder name, exe name (the exe may
     be nested a level or two below the folder, e.g. WanXiang), GitHub `owner/repo`, and whether
     the exe is 32- or 64-bit (check the PE header; a 32-bit game needs the **x86** BepInEx build,
     the x64 one silently fails to load).
   - **`Installer/`**: `Installer.csproj` and `Program.cs` (`return InstallerHost.Run(args);`)
     copied from `LegendOfMortalOverLlm/Installer/`, with `installer.json` as an
     `EmbeddedResource`. Add it to the `.sln` together with `FanslationStudio.Installer.App` and
     `FanslationStudio.Installer.Core`.
   - **`Installer/installer.json`**: `gameName`, `steamAppId`, `steamFolderName`, `exeName`,
     `gitHubRepo`, `ghAccount` (`null`), `patchZipPrefix` (`EnglishPatch`), `wineLaunchOption`
     (`WINEDLLOVERRIDES="winhttp=n,b" %command%`), and a `bepInEx` block: `flavour` (`il2Cpp` or
     `mono`), `architecture`, `version`, `url` and `sha256`. Pin the exact URL and compute the
     SHA256 of the downloaded zip yourself. **Check `<Game>_Data/Managed` for `MonoMod*.dll`
     (Mono games):** if present, set `"dllSearchPathOverride": "BepInEx\\core"`, otherwise BepInEx
     loads the game's older MonoMod and dies in the preloader (`MethodAccessException` in
     `preloader_*.log`). Leave it out when there is no MonoMod. Do not add console or
     UnityLogListening settings.
   - **`Files/Packaging/`** (not `Release/`, which `.gitignore` ignores): `GameVersion.txt` (one
     line, the current game version; ask the user) and `BepInEx.cfg`, a copy of the tailored
     `BepInEx.cfg` from a working local install after the game has been launched once (or a
     placeholder to fill in). The patch ships it seed-only; the installer never edits it.
   - **Plugin csproj PostBuild**: besides `GameDir`, copy the built DLL to `ReleaseFolder` =
     `<game>\ReleaseFolder\Files\BepInEx\plugins`, as in `LegendOfMortalPlugin`'s csproj.
     `Tests/Tests.csproj` also needs project references to `FanslationStudio.LlmKit.Release` and
     `FanslationStudio.Installer.Core`.
   - **`Tests/FileOutputWorkflowTests.cs`**: step "6. Package to Game Files" copies `Mod` into the
     game and then calls `TextResizerTests.MoveResizersIntoPathBasedFiles`, `MoveSpritesIntoPathBasedFiles`
     and `MoveLayoutsIntoPathBasedFiles` (they wrap `EditorFileSplitter`). Add "7. Package Release"
     and "7b. Package Installer" from `LegendOfMortalOverLlm` with explicit mappings (never copy
     whole local `BepInEx/config` or `plugins` folders), `OwnedFolders`, `RemoveAfterStaging`
     `BepInEx/resizers/zzAddedResizers.yaml`, `SeedOnly` `BepInEx/config/**`, and
     `PrepareStagingFolder` (removes stray BepInEx files, fixes `BepinEx` casing, checks the plugin
     DLLs exist). Adjust the plugin DLL names and the mod folder mapping for the game.
     Never run 7 or 7b as a smoke test on a dev machine: they rewrite the real `ReleaseFolder` and
     open a browser. Use a throwaway probe project instead.
   - **In-game update prompt**: comes from `FanslationStudio.Plugins` (`UpdateHost` in
     `UnityShared`), wired into the host for the runtime (IL2CPP, BepInEx 5 Mono or BepInEx 6
     Mono). The plugin reads `BepInEx/release-manifest.json`, so there is no per-game plugin config.
   - **Player docs**: the `docs/README.md` "Latest release" section needs the installer
     download links (`.../releases/download/installer/Installer-win-x64.exe` and
     `Installer-linux-x64`), the Linux launch option, the Uninstall patch note, an "Updates"
     section, and a "Manual install" section naming the exact pinned BepInEx zip (and the
     `dll_search_path_override` note when the game needs it). Copy the wording from
     `LegendOfMortalOverLlm/docs/README.md`.
   - Publishing is manual and never uses ambient GitHub auth: the workflows open the release folder
     and a prefilled releases URL. The first `installer` pre-release must be created by hand
     (tick "pre-release", tag `installer`).

11. **Summarize what was created and what's still manual.** Done: repo/git init, `Files/` data
   layout, starter `Config.yaml`/`ManualTranslations.yaml`, the plugin project scaffold (compiles,
   loads, does nothing yet), installer host and packaging workflows, `AGENTS.md`/`CLAUDE.md`/
   `docs/README.md`, copied skills. Still
   game-specific and manual: writing the actual dumper (extracting the game's real data files into
   `Raw/Dumped`), IL2CPP interop generation against the real game install if applicable, populating
   `GameTextFiles.cs`'s `TextFilesToSplit` list against real dumped files, the first real
   translation run, the `SkillSyncTests.cs` edit from step 8, filling in `Files/Packaging/`
   (`GameVersion.txt`, the tailored `BepInEx.cfg`), and the first manual publish of the rolling
   `installer` pre-release.
