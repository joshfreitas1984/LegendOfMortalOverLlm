# Learnings

## Prompt Engineering

You need a lot of test scenarios so you know your new prompt has not messed with your normal scenarios.
Big prompts take a lot of the context window - you are better off dynamically adding prompts based on the input. 
Example: Do not add whole glossary, add entries for glossary when you see it in the raw
Example: Rules for placeholders or html if you see it in the raw
Use a Prompt Optimisation test to refine the prompt using the model.

## Correction Prompts
The model doesnt like it when we put corrections on new lines. It seems to work better when you are direct in a singular sentence.

With big prompts, its possible for system prompt to be lost out of the context - unless you expand the context window or minimise prompts.

## Glossary
Great for keeping consistant translation and fixing hallucinations.
Do not add too many glossary entries as it causes a lot of hallucinations.
Early findings on Auto Extraction - models do not find tags/glossary entries well enough. Need better prompts or classifications.

## Known Issue: Duplicate "Story/undefine" keys (deferred)

Several raw dump files (e.g. `Story_10.csv`) contain many rows sharing the literal key
`Story/undefine` with different Chinese text (~30 in Story_10 alone). Confirmed via
decompiling the game's own assemblies:

- `Mortal.Core.LeanLocalizationResolver.GetStoryText(key)` special-cases `key == "undefine"`
  and returns the literal string `"undefine"` immediately - it never calls `GetString`/does
  a lookup for these rows at all.
- `Lean.Localization.LeanLanguageCSV.Compile()` registers entries into a single global slot
  keyed by `entry.Name` (`LeanLocalization.RegisterTranslation`), so even the base game can't
  hold 30 distinct values under one shared name - the collision exists upstream of our tooling.
- These lines are likely rendered from raw scene-baked text (e.g. Fungus `Say.storyText`,
  confirmed to bypass localization entirely) rather than resolved by key at runtime, so a
  key-based dictionary can't disambiguate them regardless of how it's built.

Our current dump/injection code (`Plugin/StringTableDumpPatches.cs`,
`Plugin/StringTableInjectionPatches.cs`) does not handle this - it dumps them faithfully and
the injection dictionary just keeps last-write-wins per key, same as the collision already
present in the base game.

Decision: leave as-is for now. Not fixing in the current pass - if we want these lines
translated correctly in-game, it will likely require a different mechanism (e.g. a
text-content-matched hook similar to DragonHeir's DynamicStringsIL2CPP approach, or piggybacking
on XUnity.AutoTranslator which ships in the game's Managed folder) rather than key-based
injection. Revisit later.