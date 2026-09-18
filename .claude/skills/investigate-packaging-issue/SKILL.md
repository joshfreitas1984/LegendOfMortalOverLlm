---
name: investigate-packaging-issue
description: Fixed investigation order for a reported packaging problem (wrong output in Files/Mod, a line unexpectedly fell back to raw, QC gating misbehaved during packaging, or a column/line was skipped that shouldn't have been). Use whenever a packaging-shaped bug is reported in a downstream repo that consumes FanslationStudio.LlmKit's packaging workflows.
---

# Investigate a packaging issue

This skill assumes you are running from a downstream repo (DragonHierOverLlm, LegendOfMortalOverLlm,
or WanXiangOverLlm) that consumes FanslationStudio.LlmKit via project reference — always reference
LlmKit through the relative sibling path `../FanslationStudio.LlmKit/...`, never assume the working
directory is inside LlmKit itself.

Work through these steps in order; do not skip ahead to reading code before step 1 identifies which
workflow actually owns the reported line.

1. **Identify the `TextFileType` and owning workflow.** Figure out which of `RawCsv`, `RawJson`,
   `PrefabText`, or `DynamicStringsIL2CPP` the affected file/line is, then map it to its
   `PackageAsync`/`PackagePrefabTextAsync`/`PackageDynamicStringsAsync` method
   (`CsvGameDataWorkflow`, `JsonGameDataWorkflow`, `PrefabTextWorkflow`, `DynamicStringWorkflow` in
   `../FanslationStudio.LlmKit/Workflow/`). Consult the per-workflow differences table in
   `../FanslationStudio.LlmKit/docs/features/packaging/packaging-workflows.md` before assuming behavior from one file
   type (especially CSV) transfers to another — reconstruction unit, failure granularity, and
   raw-fallback behavior all differ by workflow.

2. **Check the downstream repo's own packaging entry point** — e.g.
   `Tests/TranslationPackaging.cs`'s `PackageFinalTranslationAsync` — to see how it dispatches each
   configured `TextFileToSplit` to the matching LlmKit workflow by `TextFileType`, and what
   project-specific post-processing it layers on top (e.g. `CsvGameDataWorkflow.PackageAsync`'s
   optional `onColumnPackaged`/`rowPostProcess` hooks, or result overrides/filters the downstream
   repo applies after the workflow returns). A bug can live in this dispatch/post-processing layer
   rather than in LlmKit itself.

3. **Check `PackageOutput` and `SkipColumns`** for the affected file in the downstream repo's own
   `TextFileConfiguration.TextFilesToSplit` (or equivalent). These are the two per-file/per-column
   opt-outs that most commonly explain "why didn't this get packaged": `PackageOutput = false` is a
   per-file kill switch (whole file falls back/omits regardless of translation state); `SkipColumns`
   (CSV/JSON only) excludes specific columns from decomposition at export time, so packaging leaves
   them byte-for-byte from the raw row.

4. **If QC gating is involved** (a `QcTranslated` correction being used, discarded, or a
   `QcRejected` count looking wrong), do not re-derive the score-gate/freshness mechanism here —
   cross-check with the `investigate-qc-issue` skill and
   `../FanslationStudio.LlmKit/docs/features/translation-pipeline/quality-review-pass.md` instead. Packaging only
   consumes the QC gate's result; it does not decide it.

5. **Read `../FanslationStudio.LlmKit/docs/features/packaging/packaging-workflows.md` in full** as the canonical
   current-state reference — it covers the shared `(Passed, QcRejected, RawFallback)` return shape,
   the templated-vs-plain reconstruction logic, and the raw-fallback rules.

6. **Check the downstream repo's own `docs/KNOWN_ISSUES.md` and topic docs** (e.g. a
   startup-crash or packaging-specific postmortem under `docs/investigations/`) before assuming a newly
   reported symptom is a new LlmKit bug — it may already be a known, understood issue with an
   existing investigation doc.

7. **If the finding turns out to be about LlmKit-internal behavior** (a genuine bug or an
   undocumented rule in one of the four workflows), remember FanslationStudio.LlmKit's
   `docs/README.md` source-of-truth rule: write the finding into
   `../FanslationStudio.LlmKit/docs/` (e.g. updating `packaging-workflows.md`), not into the
   downstream repo's own notes — the downstream repo's docs are for downstream-specific behavior
   only.
