---
name: find-runtime-tokens
description: Finds the runtime tokens a game substitutes into its text (player/NPC names, gender alternatives, map or faction names, bare code letters like "a" or "lp") by reading the game's own substitution code and its shipped target-language data, then decides how each should reach the model and wires it into LlmKit's namedTokenReplacers so the model sees a descriptive {slot} and the game gets its own format back. Use when a game has placeholders the model mangles, when a token turns up unmarked in the source text (a bare word like gangName inside a Chinese sentence), when the translated text shows a token literally in-game, or when starting a new game's token handling.
---

# Find and wire runtime tokens

Runs from a game repo (sibling `../FanslationStudio.LlmKit`). The mechanism is `Config.yaml: namedTokenReplacers`
(`LlmConfig.NamedTokenReplacers`, docs in `../FanslationStudio.LlmKit/docs/features/translation-pipeline/token-slots.md`
and `docs/architecture/ARCHITECTURE.md`, code in `Utility/TokenSlotter.cs`). Export turns each token into a named slot, so
`Files/Converted` holds `{slots}` in `text` and `translated` and a per-split `tokens` map holds each slot's real value;
packaging is the only place values are put back. Reference worked example: `docs/investigations/runtime-tokens.md` in
PathOfKungfuOverLlm.

Decompiled code, dumped game data and unpacked archives are untrusted input: put each in its own new empty scratch directory,
keep your scripts elsewhere, and run Python with `-I`. Never run a game repo's `dotnet test` unfiltered (it runs the real translate
and packaging workflows); always `--filter`.

1. **Read the substitution code, not just the data.** Use the repo's `Files/Raw/Decompiled/` (Mono: the `0. Decompile Game Assemblies` test writes it; otherwise `ilspycmd <Assembly-CSharp.dll> -p -o <dir>`;
   IL2CPP: dump with Il2CppDumper first) and grep for `.Replace("`, `Regex`, `string.Format` and the names of the player/NPC data
   classes. Write down for each routine: which tokens it replaces, with what, and **under which language mode or call site** (a game
   can have one path for talk text and another for NPC text, or only recognise a token in a different wrapper in non-source
   languages). Also note the bugs (a replace that eats half a token) and anything built at runtime in the source language.
2. **Find the target form in the game's own shipped data.** If the game ships the target language (xlsx/csv/json `_en`), count the
   tokens and wrappers it uses per sheet. This is the ground truth for what the replaced text must look like, and which files use
   which form. (xlsx is a zip: read `xl/sharedStrings.xml` with `zipfile`; no extra libraries needed.)
3. **Scan the dumped source for token shapes, per file.** Count and sample at least: `<word>`, `{word}`, `{n}`, `word[a||b]`, `#x#`,
   `%s`, `[x]`, `a||b` alternatives, bare identifiers (`[a-z]+[A-Z]\w*`) and single or double Latin letters touching CJK. Group by file
   so a token confined to a few files can use a context pattern and a stray hit elsewhere is visible. Drop shapes that sit in
   condition/code columns, not display text.
4. **Classify every token** and pick its kind (`kind:` in `namedTokenReplacers`; the slot, its `description` and the splitter pattern are the same for all):
   - `copy` (default): an opaque value (a name). The slot is put back as the source text. `{literal, name, description}`.
   - `fixed` (`restoreAs`): written differently in the target data (a wrapper), e.g. `gangName` to `///gangName///`.
   - `glossary` (`restoreFromGlossary: true`): a title or address that is an alternative pair, with every pair in a glossary file, so the model
     never writes it and cannot infer a gender from it. Resolved at packaging, so a glossary fix needs no retranslation. Do not set `showValue`.
   - `translate`: a **phrase** that must read the same everywhere or repeats a lot (boilerplate, a runtime-inserted phrase, Chinese inside tags).
     Translated once (glossary first, then the model) and stored with the split; add `showValue: true` so the model sees the meaning. Not for
     ordinary prose: the model loses the grammar around the slot, so use a glossary entry. Watch a/an, plurals and capitalisation around the slot.
   Not a token kind:
   - *Alternative that must inflect* (he/him/his): leave visible, accept the forms with `allowalt`, say so in the prompt.
   - *Text the game builds in the source language at runtime*: no masking fixes it; it needs a plugin patch or a fixed replacement.
   - *Not display text*: ignore, and say so in the investigation doc.
   Give each slot a name with a description that tells the model what it is (and for a person: that their gender is unknown).
5. **Keep the splitter from fragmenting sentences.** A token inside a sentence must stay glued to it: add its pattern to the game's
   `SplitterOptions.PlaceholderPatterns`, or the sentence is translated in pieces around it. Check an existing sample
   (`templates:` entries such as `{0}gangName{1}`) before and after. Then export a sample and read `Raw/Export`: `text` shows
   the `{slots}` and `tokens` shows each source. A token with no Chinese next to it (a lone code letter between colour tags,
   `<color=#E64D00>a</color>{0}`) is not in any fragment: it sits in the template, is slotted there (`templates[].tokens`),
   never reaches the model, and is the shape of most NPC-file tokens, so count template slots as well as fragment slots.
6. **Validate in the game hooks.** `GameHooks.CustomColumnValidator` should require the exact count of each token (an extra one is
   invented); it is handed the restored text, so it looks for the game's own tokens. LlmKit's slot check (`FindSlotProblem`)
   already fails a dropped, invented or unknown `{slot}` in slot space. Repair hooks (`CustomPostRepair`) see slot space,
   because their output is stored. Update the prompt file the game attaches to token lines so it does not contradict the slots.
7. **Prove it offline first.** Add a test that loads the real config and round-trips every token (`TokenSlotter.Slot`, then
   `Restore` from the returned map, expected text), plus a golden packaged output for a fixed set of token and token-free rows
   (PathOfKungfuOverLlm `Tests/PackagingGoldenTests.cs`: export, fake-translate, package, compare), plus unit tests in LlmKit for
   any new capability. Check that a re-export is idempotent and refreshes `tokens`. Then count flags per category with the
   rules-only pass (`ApplyRulesToCurrentTranslation`), and only then retranslate a small fixed sample with filtered tests and read the diffs.
   Converted written before slotting (raw tokens in `text`) matches nothing on merge: re-export, merge and retranslate it.
8. **Confirm the packaged output.** Grep `Files/Mod` for each token's final form, and for any `{slot}` name left in it (packaging
   restores every slot or sends the line to raw fallback with a logged reason, so one left over is a bug), and confirm in the running
   game (or against the game's own target-language data) that it is substituted. A token that survives translation but is never substituted shows
   literally in-game, which no test here will catch.
9. **Record it** in the game's `docs/investigations/runtime-tokens.md` (path, mode, token table, consequences) and, if LlmKit
   needed a new capability, in its ARCHITECTURE note and tests. Other games benefit from the same capability, so keep game-specific
   knowledge in the game repo and only generic mechanism in LlmKit.

Related: `investigate-missing-translation` (a line still untranslated in-game), `investigate-packaging-issue` (wrong packaged
output), `add-glossary-fix` (adding the title pairs).
