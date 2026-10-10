---
name: compare-translation-models
description: Compares ordinary translation models after a TranslationAssessmentWorkflow run in the LlmKit Assessments host by combining Comparison.yaml metrics with per-model Results.yaml samples over the shared gold set. Use when choosing between translation models, reviewing assessment output, ranking speed versus quality, or deciding whether a prompt/model variant is better.
---

# Compare translation models

This skill runs from `FanslationStudio.LlmKit` against its `FanslationStudio.LlmKit.Assessments`
host, not from a game repository. The host translates the shared QC gold set's own sources
(`Files/GoldSets/ChineseToEnglishWuxia.yaml`), each case with its own glossary snapshot, so no game
is needed and every game's defects are in the sample. The assessment is an ordinary-translation
benchmark; do not invoke the QC workflow as part of this comparison. Reference:
`docs/features/translation-pipeline/translation-assessment.md`.

## 1. Locate and validate the assessment

1. Read `FanslationStudio.LlmKit.Assessments/Files/Config.yaml` and record the `translationAssessment`
   `source` (must be `goldSet`), `goldSetPath`, `modelNames` and `outputPath`. The host config lists
   only the models it defines under `models:`; comparing several models means adding them there and
   to `modelNames` first.
2. Locate the output directory, normally `FanslationStudio.LlmKit.Assessments/Files/TestResults/ModelAssessment/` (gitignored).
3. Read `Comparison.yaml` and confirm that its sample size and fingerprint correspond to the current
   gold set (one sample per detection item and correction sample; the fingerprint changes when the gold
   set does). The gold-set run has no `sampleSeed` or `fullCellSampleRatio`.
4. For every model in `Comparison.yaml`, read `<model-name>/Results.yaml`. Treat a missing file,
   mismatched fingerprint, incomplete status, or unexpected sample IDs as an assessment-integrity
   issue, not as evidence that the model is better or worse.
5. Do not rerun the live assessment unless explicitly asked. To run it, use
   `LLMKIT_ASSESSMENTS=1 dotnet test FanslationStudio.LlmKit.Assessments --filter "DisplayName~1b"`
   (manual test "1b. Assess configured translation models (gold set)"; needs the local model backend).
   It does not touch any game's `Converted` or package output.

If the output does not exist or is incomplete, report exactly what is missing and stop before making
model recommendations.

## 2. Prepare YAML for the external frontier harness

Do not use a configured translation model, Ollama, or a downstream xUnit fact as the judge. The
frontier model is supplied by the user's external harness through the skills workflow.

Prepare one YAML input artifact, normally `JudgeInput.yaml`, containing:

- `sampleFingerprint`;
- `models`, in the same order as `Comparison.yaml`;
- selected `samples`, each with `sampleId`, `sampleKind`, `source`, `reason`, and a `candidates`
  map keyed by model name.

Select all failed or structurally invalid samples and all samples where candidate translations
differ, plus any with `detectorFindings`. Add a deterministic 10% sample of the remaining rows,
stratified by `game`, or use the user's requested fraction. Keep the source once per sample and include all
candidate translations together. This is the token-saving comparison unit.

Pass `JudgeInput.yaml` to the user's frontier harness. Require YAML-only output with this shape:

```yaml
sampleFingerprint: "..."
judge:
  name: "..."
  version: "..."
judgments:
  - sampleId: "..."
    winner: HyMT2-30B-A3B
    scores:
      Qwen25-Standard: 2
      HyMT2-7B: 3
      HyMT2-30B-A3B: 3
    confidence: 0.9
    issues: [terminology]
```

The harness should write `Judgments.yaml` atomically and resume by `sampleId` or batch ID. A
second pass should include only low-confidence judgments, ties, and close scores. It may also write
`QualityComparison.yaml` with aggregated wins, average scores, confidence, issue counts, and
per-`game` breakdowns.

Use batches of about 12 samples unless the frontier harness has a different context budget. Keep
the source and all candidate outputs in one batch so the model does not pay repeated source/context
overhead. The comparison artifacts and harness payload are YAML. Do not add a judge model to
the host `Config.yaml` or the ordinary translation model list.

## 3. Compare the summary metrics

Build a compact comparison table with one row per model containing completed and failed sample
counts, structural pass rate, average and p95 latency, characters per second, estimated full-corpus
time (a gold-set total, not a full-game estimate), and manually recorded `humanAccuracyScore` coverage and average when present.

Use `Comparison.yaml` for summary metrics. `StructuralPassRate` measures format/validation success
only; it is not fluency, accuracy, or naturalness. `EstimatedFullCorpusMilliseconds` is an estimate,
not a runtime guarantee. A model with zero completed samples or a high failure rate cannot be ranked
reliably on quality. Do not invent quality scores from structural validity.

## 4. Inspect per-sample evidence

Join samples by `sampleId` across every model's `Results.yaml`, and to the gold set by the same
`sampleId` for each case's `game`, `sourceFile` and labelled defect. Every gold-row sample has
`sampleKind: split`, so stratify by `game` and `sourceFile` instead of `split`/`fullCell`. Each result
also carries deterministic `detectorFindings` (`LeftoverCjk`, `SelfReferenceLost`); count them per model
as hard evidence, separately from structural pass rate.

Prioritize samples where models disagree, translations pass structure but differ in meaning,
failures or `detectorFindings` show leftover source-language text, and cases whose source contains
separators, placeholders, tags, numbers, or formatting tokens. Use `humanReviewNotes` and
`humanAccuracyScore` as reviewer evidence. If manual fields are absent, produce a review queue
rather than claiming a quality winner.

## 5. Make the recommendation

Return one outcome:

- **Adopt** one model when evidence supports the best practical quality/speed balance.
- **Prefer with caveat** when one leads but evidence has a clear limitation.
- **Keep comparing** when samples are incomplete, human scoring is absent, or quality differences
  remain unclear.

Use this default ordering: manually reviewed accuracy and terminology; preservation of placeholders,
tags, separators, numbers, and compound structure; failure and structural rates; estimated runtime
and tail latency; then model size/resource cost. Do not recommend a model solely because it is
larger, faster, or has a higher structural pass rate.

## 6. Prompt/model variant comparisons

When comparing prompt variants or Hy-MT versus Qwen-derived prompts, require the same sample
fingerprint (the same gold set). Compare per `game` where the sample is large enough. If evidence is
inconclusive, keep the gold set fixed, change one prompt/model variable, and add judgments to the same
YAML contract.
