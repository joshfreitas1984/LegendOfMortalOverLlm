---
name: build-game-glossary
description: Builds a game's glossary and character-gender table from its dumped data before the first full translation run - harvests the game's own genders and shipped translations, validates every seeded English name against pinyin or a hand check instead of trusting it, authors themed terminology files (stats, progression, combat, places, sects...), splits them into one file per theme, and runs a names-first pass for idiom-style skill and item names. Use after a game has a dump and Config.yaml but before translating the corpus, or when a game's names and terms are inconsistent across tables.
---

# Build a game glossary

Runs from a downstream game repo (siblings `../FanslationStudio.LlmKit`). Needs a dump in `Files/Raw/Dumped/GameData` and a
working `Config.yaml`. Read `docs/features/translation-pipeline/glossary.md` (entry shape, loading order, "Where a glossary
line lives") and `add-glossary-fix` for single fixes. This skill is the bulk build before the first run.

## Rules that hold throughout

- **The shipped English is a candidate source, never an authority.** Seed from it only where it can be verified (step 3),
  and keep the full harvest in a folder the loader does not read (`Files/Raw/GlossaryCandidates/`). Ask the owner if they
  consider the shipped English poor; if so, nothing unverified goes in `Files/Glossary/`.
- **Check sibling games' glossaries before choosing a term** (`grep -rh "result:.*Stronghold" ../*OverLlm/Files/Glossary`).
  Standardising across games matters more than a fresh choice (this game used "Stronghold" for 寨; the preset says "Fort").
- **Never guess a gender.** No entry means unknown.
- **Do not run the pipeline or commit.** Export, translation and packaging are the owner's call; the tests used here are
  read-only (run them with `--filter`, never a whole test project).
- **Windows quoting.** Write scripts and YAML with the Write tool, not shell heredocs: backslashes and `\n` are mangled.
  Emit YAML values with `json.dumps(s, ensure_ascii=False)` (valid double-quoted YAML). Python reading a Windows path
  needs `C:/...` or the real path, not a Git-bash `/g/...`.

## 1. Find what the game already knows

Inventory the dump per column (class: text / name / key-ref / json / param / remark / asset) with a script: Chinese cell
count, distinct count, average length, and **reference share** (how many of a column's short values also appear as exact
cells in another column or sheet). A high share means a name used as an identifier across tables. Names repeat everywhere, so
translate each distinct string once. Save the inventory as a CSV for later.

Look for **genders** (an NPC table with a sex column; confirm the coding against names you can read, 1/2 is not always
male/female) and name pools. Write `Files/CharacterGenders.yaml` only for names whose every row agrees; skip generic roles that
appear with both genders; never list a player who chooses. Wire it with `add-character-gender-context`.

## 2. Use the shipped translations as an oracle for columns, and as candidates for terms

If the game ships a second language, pair the Chinese and English sheets by id and, per column, count how many cells the
developers actually translated. A column left in Chinese there is an enum, key or parsed value; a translated column is display
text (and translating a translated key column is safe, because the game loads that language's sheets with the same lookups).
Beware: a "translated" sheet can still be Chinese (`NPC_EN`, `ECA_EN` here) and the real English may live in another table
(NPC names in `Touch_en`). Pair by id, never by row index; a generic column alignment mis-pairs categories ("Skill", "Normal").

## 3. Validate before promoting

- **People:** convert each Chinese name to pinyin (`pypinyin` in a venv under the scratchpad, `Style.NORMAL`,
  `heteronym=True`, accept any reading combination). Keep an entry only if the English equals the pinyin; drop the rest.
  Require a known surname (the game's name pool plus compound surnames) and 2-3 English tokens that are valid pinyin syllables,
  or common nouns ("Golden Carp Merchant") slip in.
- **Places and terms:** review by hand. Keep plain proper-noun pinyin; drop descriptive compounds that are the project's wording
  choice and any shipped English that is wrong (楼 is a tower, 青萍 is a village, not "Green").
- **Acupoints and fixed lists:** generate pinyin, skip names containing a polyphonic character.

## 4. Author terminology files by theme

One file per theme under `Files/Glossary/` (the loader reads every file directly under it; names are only organisation):
`Characters`, `Sects`, `Places`, `Strongholds`, `Lairs-and-Caves`, `Gangs-and-Dungeons`, `Inns-and-Taverns`,
`Temples-and-Palaces`, `Mansions-and-Halls`, `Terrain`, and terminology files `Stats`, `Progression`, `MartialArts`, `Combat`,
`Equipment-and-Items`, `Social-and-Gangs`, `Meridians`, `Interface`. Mine the terms from the game: UI string table, enum-like
columns (few distinct values), repeated bracket terms (【X】), stat words in descriptions, map names and the named buildings
in the object tables. Check the preset first (`BaseFiles/ChineseGlossary/*.yaml`) and follow it unless the owner says otherwise.
An entry belongs in exactly one file: dedupe across files with a script. A workspace entry with the same `raw` replaces the
preset's (that is how `寨主` was overridden), so say why in a comment when you override. Matching is by substring, so a short
entry (`弟子`) also fires inside compounds; add the compound entries too, and never add single characters.

Conventions that held: transliterate names (pinyin) and translate the type word (Sect, Stronghold, Mansion, Tower, Pavilion,
Temple, Ancestral Hall, Dungeon, Secret Lair); real places use their usual English; resources are distinct terms (体力 Stamina vs
精力 Energy); write the list of judgement calls in `docs/features/` for the owner to confirm.

## 5. Martial art, move and item names

Skill and move names are often **four-character idioms, lines of classical poetry, Buddhist/Daoist terms or historical
allusions**, and the same name appears in item names, buff names and descriptions, so settle them before the corpus run.
**A local 14B model is not reliable for these** (it mistranslated a blade art as a sword art, produced nonsense for allusions
and leaked prompt text into one name), so use its output only as a hint and **review and write every name yourself**:

1. Build the distinct name lists from the name columns (martial arts, moves/passives/buffs, then items), minus anything already
   in the glossary. A throwaway `Names__*.csv` + `NamePrompt` pass (workspace prompt in `Files/<ModelName>Prompts/`, wired with
   `AdditionalPromptName`, `PackageOutput = false`) is a cheap way to get hints; check `TranslationPackaging` skips files with
   `PackageOutput = false` or raw rows leak into `Files/Mod`. Remove the pass once the names are settled.
2. Review in batches of about 120 with the source name and the model hint side by side. For every name decide whether it is
   an idiom, a poem line or an allusion and translate the meaning in 2-5 evocative words (Title Case); keep type words
   consistent (刀法 Blade Art, 剑法 Sword Art, 通式 Common Form, numbered forms as I-IV); keep loanwords for foreign-flavoured
   schools. Write each batch to a file as `index|English` so nothing is skipped, and assemble with a script that checks every
   index is covered.
3. **Apply the names exactly through `Files/ManualTranslations.yaml`** (LlmKit replaces a split whose whole text equals `raw`,
   before the LLM). That also covers the 2-character names the substring glossary cannot hold. Names joined by `·` or `-` are
   split into fragments, so give each multi-character fragment its own entry and leave one-character fragments to the model.
   Put the names of three or more characters in `Files/Glossary/*-Names.yaml` as prompt hints for descriptions.
4. Check collisions: list every translated cell whose exact text equals a name outside the name columns. Matches that are
   the same creature, item or equipment are fine; a person who shares the name is not (skip the manual entry).
5. Stage the work: martial arts and moves first, then items (item names embed skill names).

6. **Check the header rows before slicing.** Sheets differ: some have a name row plus a type/identifier row, others only one header
   row. Slicing every sheet with `[2:]` silently dropped the first data row of the item sheets. Finish with a coverage check: every
   name cell (and every `·`-split fragment) in every name source must be found in `ManualTranslations.yaml` or the glossary.
7. **Manual translations apply when a line is translated, not to an already-translated line.** To see them on a sample, delete its
   `Files/Converted/*.yaml`, re-export (the export seeds missing Converted files) and translate again.

## 6. Run a dialogue sample before the corpus

Item and map text hides the real problems. Translate the smallest dialogue file and read it for: **runtime tokens** (names filled in by
the game, gender alternatives such as `<A||B>`, colour tags) that the splitter broke a sentence at; **speaker names** translated
differently in different lines; and tokens dropped by the model. Fix with `CompoundFieldSplitterOptions.PlaceholderPatterns` (glue the
tokens into the sentence), `IgnoreHtmlTagsInText` plus a `CustomColumnValidator` for the token check, `UnknownGenderPersonTokens`
for tokens that stand for the player, an `AdditionalPromptName` prompt, and reviewed English for each distinct token and for every speaker
and NPC name (pinyin for people, English for roles; derive corpse/number/letter variants from the base name). Re-export after changing
the splitter (delete `Files/Converted` first if it only holds sample output).

## 7. Verify

Add a read-only test that loads `Config.yaml` (`ConfigurationExtensions.GetConfiguration(dir).Runtime.GlossaryLines`) and
asserts the entry count, a few entries, and that an override replaced the preset line (`Assert.Single`). Run it with
`--filter`. Write `docs/features/glossary-seed.md`: file layout, what came from where, judgement calls, what was left out and why.

## Report back

Entries per file and total; how many seeded entries were dropped by validation and why; genders (male/female/left out);
the conventions chosen and the sibling glossaries they were checked against; the judgement calls for the owner; and what is
still unglossed (names awaiting stage 1/2).
