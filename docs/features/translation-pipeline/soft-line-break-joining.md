# Soft line-break joining

At packaging time, every `RawCsv` file goes through `Translate/SoftLineBreakJoiner.cs`, which
replaces the Chinese source's manual mid-sentence line wraps with a space in the English output.

## Why

The game's descriptions wrap lines by hand to fit a fixed number of CJK characters per line:

```
唐门弟子人手一柄的标准配备。\n外型看似与脱手标相去不远，\n实则是柄机关剑，\n剑身中空，内有钢球。
```

Each segment between breaks is translated separately and rejoined with the same `\n`. So the
English kept a break after every clause ("It looks much like a throwing dart," / "but is in
fact…"), which reads as broken text once Unity word-wraps the line anyway.

## Rule

A `\n` in the source is a **soft wrap**, and is replaced with a single space, only when the Chinese
text just before it ends in `，`, `、` or `；`. Every other break is kept:

- after `。！？…` or a closing `」』）)]】` (the game lays descriptions out as one sentence per line)
- after `：` (a header line such as `嘴攻：`)
- after a line with no punctuation (list items and headers, e.g. `机关箧笥・捕鸟型态`, stat lines)
- a leading `\n` (spacing at the top of `LegendInfo` entries)

Matching is done per column against the row's original Chinese cell, looked up by key. If the
source and the packaged translation have a different number of breaks, which can happen when a QC
rewrite of the whole column moves them, the column is left unchanged.

## Affected files

Counts come from `Files/Converted` as of 2026-09-28: entries with at least one soft break / all
entries containing a break, and the number of breaks joined.

| File | Entries | Breaks joined |
| --- | --- | --- |
| `Equipment_zh-cn.csv` | 37 / 37 | 83 |
| `Legend_01_zh-cn.csv` | 51 / 2,609 | 59 |
| `Library_zh-cn.csv` | 40 / 73 | 47 |
| `CharacterIntro_zh-cn.csv` | 16 / 100 | 47 |
| `CombatTalking_zh-cn.csv` | 35 / 148 | 36 |
| `Talent_zh-cn.csv` | 9 / 115 | 9 |
| `ItemBook_zh-cn.csv` | 6 / 81 | 9 |
| `ItemMisc_zh-cn.csv` | 4 / 23 | 8 |
| `Position_zh-cn.csv` | 4 / 26 | 4 |
| `ItemSpecial_zh-cn.csv` | 3 / 56 | 3 |
| `CombatInfo_zh-cn.csv` | 3 / 14 | 3 |
| `Work_zh-cn.csv` | 2 / 10 | 2 |
| `CombatEffects_zh-cn.csv` | 1 / 51 | 1 |
| `PlayerInfo_zh-cn.csv` | 1 / 6 | 1 |

`Story_*.csv` files contain no `\n` and are unaffected.

`CombatTalking` is the least certain case: some of its breaks may be a pause between two beats of
a battle taunt, not a width wrap. It was included on purpose; exclude it first if taunts read worse.

## Turning it off or narrowing it

- **Everything:** set `SoftLineBreakJoiner.Enabled = false` in `Translate/SoftLineBreakJoiner.cs`
  and re-run the "6. Package to Game Files" test. Packaging then goes straight through
  `CsvGameDataWorkflow.PackageAsync` with no row post-processing.
- **One file (e.g. `CombatTalking`):** in `TranslationPackaging.PackageCsvAsync`, return the plain
  `CsvGameDataWorkflow.PackageAsync(workingDirectory, textFile)` call when `textFile.Path` matches.
- **Removing it entirely:** delete `SoftLineBreakJoiner.cs` and `Tests/SoftLineBreakJoinerTests.cs`,
  and change the `RawCsv` arm in `TranslationPackaging.PackageFinalTranslationAsync` back to
  `CsvGameDataWorkflow.PackageAsync(workingDirectory, textFile)`.

Only the packaged `Files/Mod` output changes. `Files/Converted` translations are untouched, so
turning it off needs no retranslation.

## Possible follow-up

Soft wraps also split a sentence in half before translation, so each half is translated on its own
(e.g. "Help the people by opening up resources…"). Merging those fragments before translation
would give whole-sentence translations, at the cost of retranslating the ~312 affected entries.
