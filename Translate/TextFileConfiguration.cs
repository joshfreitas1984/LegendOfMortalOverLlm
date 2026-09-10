using FanslationStudio.LlmKit.Support;

namespace Translate;

/// <summary>
/// Authoritative translation/package configuration for Legend of Mortal's text files, mirroring
/// DragonHierOverLlm's Tests/TextFileConfiguration.cs.
///
/// *** PROVISIONAL - NEEDS HUMAN VERIFICATION AGAINST THE REAL GAME DUMP ***
/// The new BepInEx Plugin (see Plugin/StringTableDumpPatches.cs) dumps one 2-column ("key,text",
/// no header row) CSV per Lean.Localization.LeanLanguageCSV source, named after that source's
/// backing TextAsset - but this could not be verified against the real running game in this
/// sandboxed environment (no Steam/game install available). The list below is a best-effort
/// reconstruction from the OLD, pre-migration Files/Raw/StringTable.csv's "Category/subkey" key
/// prefixes (its row counts per prefix are noted alongside each entry below), assuming each
/// category becomes its own "{Category}.csv" TextAsset/dump file. A handful of legacy keys in the
/// old dump had no "/" separator at all (2 rows, grouped here under the literal prefix "_Misc" -
/// distinct from the unrelated "Misc" prefix, which had its own 86 rows) - those may not map to a
/// real separate file at all.
///
/// The FIRST TIME a human runs the game with the new plugin, they MUST:
///   1. Compare this list's Path values against the real files under BepInEx/plugins/raw/*.csv.
///   2. Fix any filename mismatches (case-sensitive - these are real Unity TextAsset names).
///   3. Add/remove entries for any category that doesn't map 1:1 the way assumed here.
///
/// Every entry uses SkipColumns = [0] because column 0 is always the CSV lookup key (never
/// translatable) and TextFileType.RawCsv/PackageOutput = true, matching the old pipeline's
/// uniform handling of every row in the single merged StringTable.csv.
/// </summary>
public static class TextFileConfiguration
{
    public static readonly TextFileToSplit[] TextFilesToSplit = [
        new() {Path = "Story.csv", PackageOutput = true, SkipColumns = [0] },              // 63670 rows - dialogue, by far the largest
        new() {Path = "LegendInfo.csv", PackageOutput = true, SkipColumns = [0] },         // 4244 rows - narrative/event text; see TranslationPackaging's paragraph-newline rowPostProcess
        new() {Path = "CombatSkill.csv", PackageOutput = true, SkipColumns = [0] },        // 1069
        new() {Path = "CombatTalking.csv", PackageOutput = true, SkipColumns = [0] },      // 858
        new() {Path = "Character.csv", PackageOutput = true, SkipColumns = [0] },          // 393
        new() {Path = "Library.csv", PackageOutput = true, SkipColumns = [0] },            // 384
        new() {Path = "PlayerTalent.csv", PackageOutput = true, SkipColumns = [0] },       // 261
        new() {Path = "System.csv", PackageOutput = true, SkipColumns = [0] },             // 202
        new() {Path = "Book.csv", PackageOutput = true, SkipColumns = [0] },               // 190
        new() {Path = "Position.csv", PackageOutput = true, SkipColumns = [0] },           // 161
        new() {Path = "Special.csv", PackageOutput = true, SkipColumns = [0] },            // 130
        new() {Path = "CombatCharacter.csv", PackageOutput = true, SkipColumns = [0] },     // 117
        new() {Path = "Misc.csv", PackageOutput = true, SkipColumns = [0] },               // 86
        new() {Path = "Equip.csv", PackageOutput = true, SkipColumns = [0] },              // 74
        new() {Path = "PlayerStat.csv", PackageOutput = true, SkipColumns = [0] },         // 52
        new() {Path = "Credit.csv", PackageOutput = true, SkipColumns = [0] },             // 46
        new() {Path = "CombatAction.csv", PackageOutput = true, SkipColumns = [0] },       // 43
        new() {Path = "Flag.csv", PackageOutput = true, SkipColumns = [0] },               // 42
        new() {Path = "CombatInfo.csv", PackageOutput = true, SkipColumns = [0] },         // 41
        new() {Path = "CharacterTitle.csv", PackageOutput = true, SkipColumns = [0] },     // 37
        new() {Path = "PlayerInfo.csv", PackageOutput = true, SkipColumns = [0] },         // 34
        new() {Path = "CharacterIntro0.csv", PackageOutput = true, SkipColumns = [0] },    // 33
        new() {Path = "CharacterIntro1.csv", PackageOutput = true, SkipColumns = [0] },    // 30
        new() {Path = "BattleSkill.csv", PackageOutput = true, SkipColumns = [0] },        // 29
        new() {Path = "Facility.csv", PackageOutput = true, SkipColumns = [0] },           // 29
        new() {Path = "BattleDialog.csv", PackageOutput = true, SkipColumns = [0] },       // 29
        new() {Path = "StatLevel.csv", PackageOutput = true, SkipColumns = [0] },          // 28
        new() {Path = "PlayerStatDesc.csv", PackageOutput = true, SkipColumns = [0] },     // 27
        new() {Path = "UpgradeItemRelationDesc.csv", PackageOutput = true, SkipColumns = [0] }, // 25
        new() {Path = "EnemyTeam.csv", PackageOutput = true, SkipColumns = [0] },          // 21
        new() {Path = "CharacterIntro2.csv", PackageOutput = true, SkipColumns = [0] },    // 21
        new() {Path = "Work.csv", PackageOutput = true, SkipColumns = [0] },               // 20
        new() {Path = "MapPosition.csv", PackageOutput = true, SkipColumns = [0] },        // 17
        new() {Path = "CharacterIntro3.csv", PackageOutput = true, SkipColumns = [0] },    // 17
        new() {Path = "Free.csv", PackageOutput = true, SkipColumns = [0] },               // 16
        new() {Path = "BattleKey.csv", PackageOutput = true, SkipColumns = [0] },          // 15
        new() {Path = "SystemSetting.csv", PackageOutput = true, SkipColumns = [0] },      // 15
        new() {Path = "CharacterIntro4.csv", PackageOutput = true, SkipColumns = [0] },    // 13
        new() {Path = "Middle.csv", PackageOutput = true, SkipColumns = [0] },             // 12
        new() {Path = "MartialCondition.csv", PackageOutput = true, SkipColumns = [0] },   // 10
        new() {Path = "MiddleMenu.csv", PackageOutput = true, SkipColumns = [0] },         // 5
        new() {Path = "MartialStatDisplay.csv", PackageOutput = true, SkipColumns = [0] }, // 5
        new() {Path = "DevelopRefund.csv", PackageOutput = true, SkipColumns = [0] },      // 4
        new() {Path = "DevelopCondition.csv", PackageOutput = true, SkipColumns = [0] },   // 3
        // Legacy keys with no "/" separator at all in the old dump (2 rows) - may not correspond
        // to a real separate dumped file; grouped here as a provisional catch-all.
        new() {Path = "_Misc.csv", PackageOutput = true, SkipColumns = [0] },              // 2
        new() {Path = "Develop.csv", PackageOutput = true, SkipColumns = [0] },            // 2
    ];
}
