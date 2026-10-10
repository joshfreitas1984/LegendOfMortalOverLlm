---
name: add-character-gender-context
description: Adds a character-gender table and line context to a game-translation repo so the translator and the pronoun check know who is male or female (stops "he/she" being guessed for named characters). Use when a game translates named characters with the wrong or invented pronouns, when setting up pronounCheck/lineContext for a new game, or when extending an existing CharacterGenders table with aliases and titles.
---

# Add character gender context to a game

This skill runs from a downstream game-translation repository (DragonHierOverLlm, WanXiangOverLlm,
LegendOfMortalOverLlm...). It builds a name-to-gender table from data the game already has, wires it into the
translator and the pronoun check through LlmKit's `CharacterContext`, and measures what it changes. Read
`docs/features/translation-pipeline/game-hooks.md` (`LineContextProvider`, `CharacterContext`) and the "Repairing an old
corpus: pronoun defects" section of `translation-project-workflows.md` first.

## Rules that must hold

- **Never run a whole test project.** `dotnet test` with no filter ran export tests in one game that deleted tracked
  `Files/Raw` files and rewrote `Converted`. Always run with `--filter` on one test name. The dry-run test
  (`Count lines needing pronoun retranslation (dry run)`) only writes `TestResults/PronounRetranslation.yaml`.
- **Never guess a gender.** An unknown character gets no entry. The player gets an entry only if the project owner says
  what the player's gender is (in a game where the player picks, never list them).
- **Do not commit game repos for the user.** They usually hold uncommitted run state in `Files/Converted` and `Config.yaml`.

## 1. Find where the game keeps genders

Look in `Files/Raw/Dumped` for a character table with a gender column (DragonHier: `SpeHeroData.csv`, columns `名字` and
`性别`). If there is one, use `CharacterContext.FromCsv(path, nameColumn, genderColumn, stripFromNames)` and skip to step 3.
Check how names are stored: DragonHier stores `慕容.星辰` but the text has `慕容星辰`, so pass `stripFromNames: "."`.

If there is no gender column, derive the table once from character biographies or intro text (step 2). If there is
nothing at all, ask the user before going further.

## 2. Derive a table once from biographies

1. Build id to name from the character-name table, and id to biography text from the intro table (LegendOfMortal:
   `Character_zh-cn.csv`, `CharacterIntro_zh-cn.csv`; the id after the `/` links them).
2. Count 他 and 她 in each biography. Decide only on a clear majority (3 or more, ratio 3:1). Fall back to a reliable id
   prefix (`brother`, `girl`, `sister`) only when the biography has fewer than 3 pronouns. Leave everything else unknown.
3. Write `Files/CharacterGenders.yaml`: a list of `name`, `englishName` (from the glossary), `gender` (`male`/`female`),
   `aliases`, `evidence`. Put candidates you could not decide with `gender: ""` and a `# guess:` comment. The loader ignores
   an empty gender, so they cost nothing until the owner fills them in.
4. Rank the remaining named characters by how often they appear in the story text and list them with their English
   names. Ask the owner for genders; do not guess from nearby pronouns alone (noisy).
5. Genders also matter for names the text uses in short form: add nicknames and given-name-only forms as `aliases`
   (虞小梅 has 小梅). Do not alias a word that is also an ordinary noun (布衣 "commoner", 富贵 "wealth").

## 3. Wire the provider

Add a `LineContextProvider` in the game's `Translate` project (copy `LegendOfMortalOverLlm/Translate/CharacterLineContext.cs`):
load the table once, cache it for the game's own working directory (read other directories directly so tests can use a
copy), and call `CharacterContext.AddCharacterContext(contexts, lines, characters)`. Register it in
`GameFileHandling.Hooks` and set `lineContextEnabled: true` in `Files/Config.yaml`. If the game also has person tokens
that stand for an unknown person, give them `GameHooks.UnknownGenderPersonTokens` and a hint (see DragonHier's `PlotLineContext`).

`AddCharacterContext` already: skips a split whose source states a gender (他/她, 师兄, 姑娘...), skips a split naming
characters of different genders, and never replaces a speaker context that already knows a gender. Use `minNameLength`
to keep two-character names that are ordinary words from matching (DragonHier uses 3).

## 4. Add tests

Add a small test class (copy `LegendOfMortalOverLlm/Tests/CharacterLineContextTests.cs`): the table loads, a listed
alias gets the right gender, and an unlisted or empty-gender character gets no context. Run it with `--filter`.
Keep assertions about specific characters out of tests that depend on the owner's edits to the table, or the test
breaks every time they fill in a gender.

## 5. Measure, then handle false positives

1. Run the dry-run test with `--filter "DisplayName~Count lines needing pronoun"` and read
   `Files/TestResults/PronounRetranslation.yaml` (a dry run skips lines already flagged for retranslation).
2. Sample 10 to 15 hits. Real hits are a named character with the wrong pronoun. Common false positives are a line that
   also names someone of the other gender who is not in the table. The fix is to add that second person to the table, or
   an alias for how the text refers to them.
3. Titles are the main source of missing names: find the most frequent `surname + title` forms (掌门, 帮主, 道长, 大侠...)
   and map them to a character only when the surname is unique in the table. Titles that are already gendered (姑娘,
   夫人, 公子) are skipped by the helper and need no alias. Ask the owner when a surname is shared (唐掌门 with several
   Tang characters) or when the title holder's gender is not in the table. Re-run the dry run: the count can rise,
   because aliases make more lines checkable.
4. Report the counts (hits, categories, sample precision) and the open questions. Do not retranslate; flagging and
   retranslating is the owner's decision.

Character names and genders are always game-specific: they go in the game's own table and glossary, never the
LlmKit preset (see "Where a glossary line lives" in `../FanslationStudio.LlmKit/docs/features/translation-pipeline/glossary.md`).

## 6. Related settings

`Config.yaml` `pronounCheck` has `enabled`, `skipWhenTranslationNamesSomeone` (true quiets a prose game that names
characters constantly) and `autoRepairPronouns` (rewrite a still-gendered line to they/their/them; off by default,
trades a possibly-correct pronoun for a guaranteed-neutral one). Mention them; change them only on request.

## Report back

State how many characters are in the table and how many are unknown, what was added (aliases, titles), the dry-run
counts before and after, the false-positive classes still open, and exactly what the owner needs to decide.
