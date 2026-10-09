using FanslationStudio.LlmKit.Support;

namespace Translate;

/// <summary>
/// Authoritative translation/package configuration for Legend of Mortal's text files, mirroring
/// DragonHierOverLlm's Tests/TextFileConfiguration.cs.
///
/// Confirmed live against the real game (BepInEx/plugins/raw/*.csv after running the Plugin
/// project) - the game ships one LeanLanguageCSV source per category per language
/// (ChineseSimplified/ChineseTraditional/Korean); LegendOfMortalPlugin/StringTableDumpPatches.cs now filters to
/// ChineseSimplified only, so every file below is a single unambiguous dump. Most categories'
/// backing TextAsset names carry a "_zh-cn" suffix (the other two language variants use "_zh-tw"/
/// "_kr", but those are never dumped); Story is split into 17 numbered chapter files
/// ("Story_1".."Story_17") that carry no per-language suffix at all in the asset name itself
/// (disambiguated purely by StringTableDumpPatches' Language filter, not by filename). Every row's
/// column 0 key retains its ORIGINAL category prefix regardless of which physical file it lives in
/// (e.g. Legend_01_zh-cn.csv's rows are still keyed "LegendInfo/...", CombatEffects_zh-cn.csv's
/// rows are still keyed "CombatSkill/..." - these do not match the file names below at all), which
/// is why Translate/LegacyDataMigration.cs's plain by-key lookup against the old merged
/// StringTable.csv donor works unchanged despite the physical file split.
///
/// Every entry uses SkipColumns = [0] because column 0 is always the CSV lookup key (never
/// translatable) and TextFileType.RawCsv/PackageOutput = true, matching the old pipeline's
/// uniform handling of every row in the single merged StringTable.csv.
/// </summary>
public static class TextFileConfiguration
{
    public static readonly TextFileToSplit[] TextFilesToSplit = [
        new() {Path = "dynamicStrings.txt", PackageOutput = true, TextFileType = TextFileType.DynamicStrings },
        
        //Traditional Chinese (has most of them)
        new() {Path = "dumpedPrefabText.txt", PackageOutput = true, TextFileType = TextFileType.PrefabText },

        //Simplified
        new() {Path = "prefabText.txt", PackageOutput = true, TextFileType = TextFileType.PrefabText },

        new() {Path = "BattleSkill_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "BattleTalking_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "CharacterIntro_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "CharacterTitle_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Character_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "CombatCharacter_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "CombatEffects_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "CombatInfo_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "CombatTalking_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Credit_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "EnemyTeam_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Equipment_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Facility_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Flag_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "ItemBook_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "ItemMisc_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "ItemSpecial_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        // LegendInfo/... narrative text - mid-sentence line wraps are joined at packaging, see SoftLineBreakJoiner.
        new() {Path = "Legend_01_zh-cn.csv", PackageOutput = true, SkipColumns = [0], EnableQualityControl = false },
        new() {Path = "Library_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "PlayerInfo_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Position_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Stat_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_1.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_2.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_3.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_4.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_5.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_6.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_7.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_8.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_9.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_10.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_11.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_12.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_13.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_14.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_15.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_16.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_17.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Story_Message_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "System_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Talent_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "UpgradeItem_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
        new() {Path = "Work_zh-cn.csv", PackageOutput = true, SkipColumns = [0] },
    ];
}
