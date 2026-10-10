---
name: add-glossary-fix
description: Adds or changes a glossary line for a reported translation or QC defect by deciding first whether it belongs in the LlmKit preset or the game's own glossary, proving a preset change against every game with the deterministic scans, then writing it to the right file. Use whenever a fix is "add/change a glossary entry", from an investigate-qc-issue finding, a missing-translation report, or a game-update refresh.
---

# Add a glossary fix

Runs from a game repo (siblings `../FanslationStudio.LlmKit`) or from LlmKit itself. Rubric source of truth:
`../FanslationStudio.LlmKit/docs/features/translation-pipeline/glossary.md` ("Where a glossary line lives").
This skill applies it; it does not restate it.

1. **Check it is a glossary problem.** If a prompt, validator or engine cause explains the defect (see
   `investigate-qc-issue`), fix that instead. A glossary patch over an engine bug hides it.
2. **Decide the scope** with the rubric: proper noun, game stat label, single character, or a term another
   game would translate differently → the **game** glossary (`Files/Glossary/*.yaml`). A generic wuxia term every
   game would translate identically → the **preset** (`FanslationStudio.LlmKit/BaseFiles/ChineseGlossary/`). Phrases
   (idioms, fixed courtesies) may go in the preset if every game would render them the same way, but only with
   `allowalt` covering the inflections the corpus uses (read the scan's hit rate); single characters never do. A phrase
   for one exact line is a `ManualTranslations.yaml` entry.
3. **Write a game line** under `Files/Glossary/`: stable `raw`, explicit `rawSimplified`/`rawTraditional` where
   conversion could be wrong, `allowalt` only for genuinely accepted renderings, `only:` when the raw sits inside names
   or idioms. If it overrides a preset result, add a comment saying why.
4. **For a preset line, prove it first.** Add a row to
   `FanslationStudio.LlmKit/FanslationStudio.LlmKit.Assessments/Files/PresetChanges.yaml` for the old result, then run
   with `LLMKIT_ASSESSMENTS=1`:
   `dotnet test FanslationStudio.LlmKit.Assessments --filter "DisplayName~Scan:"`. Read
   `Files/TestResults/Scans/preset-audit.md` (match/hit/miss per game, shadowed injections) and
   `preset-change-impact.md` (how many shipped translations still use the old result). Do not add the line if a game
   already translates the term differently, or if it matches inside names or idioms.
5. **Then run** `dotnet test` in LlmKit (the `PresetGlossaryLintTests` reject single characters, duplicates and
   non-English results) and, in the game, `TranslationWorkflow.ApplyAllRulesToCurrentTranslation` so existing
   translations are rechecked. A changed preset result that games already shipped needs a decision (re-translate or a
   mechanical swap), which is the owner's call.
6. **Record it:** the scope decision and why in the commit message; a durable finding in LlmKit's `docs/` per the
   repo's documentation rule, not in an auto-loaded instructions file.

Report which file the line went to, why, and (for a preset change) the per-game counts.
