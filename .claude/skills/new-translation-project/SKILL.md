---
name: new-translation-project
description: Scaffolds a brand-new sibling "OverLlm" game-translation repo (e.g. DragonHierOverLlm, LegendOfMortalOverLlm, WanXiangOverLlm) that consumes FanslationStudio.LlmKit via project reference. Asks a handful of setup questions, then creates the new repo's directory/git init, working-directory data layout, starter Config.yaml, BepInEx plugin project (IL2CPP or Mono), AGENTS.md/CLAUDE.md/docs/README.md, and copies this repo's .claude/skills/ into it. Use when starting a brand-new game translation project from scratch, not for changes to an existing downstream repo.
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
   will use — leave `modelPreset`/`model` as placeholders the user fills in), a `qualityReview:`
   block using LlmKit's current defaults (`enabled: true`, placeholder `modelName`,
   `minAcceptableScore: 70`, leave `autoAcceptDefectCategories` empty/commented — that list is
   populated later from real hand-triage, never guessed up front), `glossaryPreset`,
   `useContinuousWorkerPool: true`, and placeholder `maxConcurrency`/`retryCount`/`batchSize`. Also
   create an empty `Files/ManualTranslations.yaml`. Note: the actual per-file `TextFileToSplit[]`
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
   had before this restructuring: `../FanslationStudio.LlmKit/docs/features/translation-pipeline/quality-review-pass.md`
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

9. **Summarize what was created and what's still manual.** Done: repo/git init, `Files/` data
   layout, starter `Config.yaml`/`ManualTranslations.yaml`, the plugin project scaffold (compiles,
   loads, does nothing yet), `AGENTS.md`/`CLAUDE.md`/`docs/README.md`, copied skills. Still
   game-specific and manual: writing the actual dumper (extracting the game's real data files into
   `Raw/Dumped`), IL2CPP interop generation against the real game install if applicable, populating
   `GameTextFiles.cs`'s `TextFilesToSplit` list against real dumped files, the first real
   translation run, and the `SkillSyncTests.cs` edit from step 8.
