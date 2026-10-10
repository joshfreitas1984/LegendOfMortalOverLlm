---
name: expand-goldset
description: Grows the shared QC gold set in LlmKit (FanslationStudio.LlmKit.Assessments/Files/GoldSets) by mining flagged and FailedValidation QC items from every registered game into candidate cases, then labelling them by hand with a glossary snapshot per case. Use when asked to expand the gold set, add cases from a game's QC flagged queue, or add a regression case for a QC defect found in any game.
---

# Expand the gold set (cross-game)

The gold set lives in LlmKit, not in a game: `FanslationStudio.LlmKit.Assessments/Files/GoldSets/ChineseToEnglishWuxia.yaml`.
Every case is **self-contained**: `game`, `sourceFile`, `source`, the translation under test, its labels, and a
`glossary` snapshot of the entries that applied when it was judged. This skill never runs an LLM pass and never edits
a game's `Converted/`. Every label is your own judgment against real text already on disk; never invent a translation or
a defect.

## 1. Mine candidates

From LlmKit, with the sibling game checkouts present (`Files/Games.yaml`):

```
LLMKIT_ASSESSMENTS=1 dotnet test FanslationStudio.LlmKit.Assessments --filter "DisplayName~Mine gold-set"
```

This writes `Files/TestResults/Mining/candidates.yaml` (gitignored): up to 8 candidates per game and defect category,
spread across files, lowest QC score first, skipping sources already in the gold set. Each carries the game's own QC
verdict (`qcStatus`, `qcQualityScore`, `qcDefectCategory`) for context only. **Do not copy the QC verdict into the
label**: that makes the gold set circular. Judge the text yourself.

For a single known defect, skip mining and write the case by hand (step 3).

## 2. Check the newline convention before judging "\n"

A source can hold a **real newline** or the **literal two-character `\n` token**. For a real-newline source, a real
newline in the translation is correct; it is a defect only if the separator is dropped, duplicated or moved. Check
per sample; do not assume one file's convention for another.

## 3. Label

Per candidate set `label` (`Pass`, `Defect`, rarely `Abstain`), `defectCategories` (reuse the existing vocabulary
before inventing one), `correctionSafety` (`safe`, `unnecessary`, `harmful`) and a one-paragraph `reviewNote` naming the
words involved. Use a `correctionSamples` entry (source, `currentTranslation`, `proposedCorrection`, one label) only
when you have a real adopted correction to compare against; otherwise an `items` entry with `candidates` and `labels`.
Keep the candidate's `game`, `sourceFile` and `glossary` fields as mined.

Include clean `Pass` cases on purpose: a gold set built from failures only skews toward defects.

## 4. Write and validate

1. Append the labelled cases to the gold set. A plain YAML scalar containing `": "` breaks parsing; use a folded block
   (`>-`) for long notes.
2. Check the whole file loads: `dotnet test FanslationStudio.LlmKit.Assessments` (the gold-set test asserts every case has
   a game and a glossary snapshot).
3. For cases without a snapshot, run "2. Import a game's gold set (snapshot glossaries)" or set the glossary by hand.
4. Bump `labelVersion` at the top when existing labels change, so old archived runs are not compared to new labels.

## 5. Run and report

Run "1. Assess configured QC models" (`LLMKIT_ASSESSMENTS=1`, needs Ollama) and compare with the last archived
`Comparison.yaml` within the run-to-run noise (about 1-2 items). Report: new case count against the old, which games
and categories were added, and any case where the QC evaluator disagrees with your label. Do not call the gold set
"done"; the target is 300-500 cases.
