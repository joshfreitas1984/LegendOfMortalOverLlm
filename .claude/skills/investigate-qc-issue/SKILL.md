---
name: investigate-qc-issue
description: Investigates a reported quality-review (QC) issue — a wrong/missing QcQualityScore, a wrongly accepted/rejected correction, or packaging behaving oddly due to QC score-gating — by walking the fixed file map for the QC pass (Config.yaml, QualityReviewWorkflow.cs, QualityReviewHelpers, tests, architecture doc, downstream docs/KNOWN_ISSUES.md) in the order that actually explains the behavior, before concluding it's a new bug. Use whenever someone reports a QC-related defect in a downstream "OverLlm" repo.
---

# Investigate a QC issue

This repo (`FanslationStudio.LlmKit`) owns the QC pass; this skill runs from a **downstream**
repo's working directory (e.g. `DragonHierOverLlm`, `LegendOfMortalOverLlm`, `WanXiangOverLlm`),
so every LlmKit path below is relative via `../FanslationStudio.LlmKit/...`. Work through these
steps in order — each narrows down a different layer, and later steps assume earlier ones didn't
already explain the symptom.

1. **Check the downstream repo's own config first.** Read the current repo's `Files/Config.yaml`
   `qualityReview.*` block (`enabled`, `modelName`, `minAcceptableScore`, `maxRuleCheckRetries`,
   `autoAcceptDefectCategories`, `twoStageVerificationEnabled`). Confirm QC is actually enabled and
   configured as expected for the affected file — remember `enabled: false` silently reverts every
   column to pre-QC `Translated` at packaging time too, not just skipping the review pass, so a
   "QC did nothing" report is often just this flag.

2. **Check the workflow/helper code that implements gating.** Read
   `../FanslationStudio.LlmKit/FanslationStudio.LlmKit/Workflow/QualityReviewWorkflow.cs`
   (`ReviewColumnAsync`'s readiness/skip/validation-gate/acceptance logic) and
   `../FanslationStudio.LlmKit/FanslationStudio.LlmKit/Utility/QualityReviewHelpers.cs`
   (`IsQcReviewFresh`, `PassesQcScoreGate`) for the current staleness/freshness and score-gate
   logic. Most "wrongly accepted/rejected" or "stale score" reports trace to one of these two
   choke points.

3. **Check LlmKit's own unit tests for documented scenarios.** Read
   `../FanslationStudio.LlmKit/Tests/Workflow/QualityReviewWorkflowTests.cs` — its test cases
   encode the exact expected behavior (freshness, score-gate, DEFECT parsing, validation-gate
   accept/reject) precisely, often more precisely than the prose docs. Do not confuse this with the
   downstream repo's own `Tests/QualityControlWorkflowTests.cs` (see step 5) — that file is a
   manually-run, numbered *pipeline* of facts for advancing a real translation project, not a
   regression suite, and lives in each downstream repo separately.

4. **Read the canonical architecture doc, don't re-derive from code alone.**
   `../FanslationStudio.LlmKit/docs/features/translation-pipeline/quality-review-pass.md` is the current-state
   reference for the data model (`QcStatus`, `QcDefectCategory`, `QcQualityScore`, the
   `SubIndex == 0` anchor convention for templated columns), packaging score-gating, the
   DEFECT-category auto-accept policy, and reset levels. Treat it as more authoritative than
   re-reading the workflow code cold, since it documents postmortems (e.g. the low-score-discard
   bug, the PrefabText/DynamicString raw-fallback bug) that explain *why* the code looks the way it
   does.

5. **Check the downstream repo's own known-issues before assuming it's a new LlmKit bug.** Read
   this repo's `docs/KNOWN_ISSUES.md` "Quality-review (QC) pipeline" section and any linked
   `docs/investigations/qc-*.md` topic files, plus the QC section of
   `.github/instructions/tests-translation-workflow.instructions.md` if present. Many "QC bugs" are
   already-documented, already-fixed, or project-specific quirks (a `CustomQcExclusionRule`, a
   `DEFECT` category left off `autoAcceptDefectCategories` on purpose) rather than new defects.

6. **If the finding is genuinely LlmKit-internal, write it up in LlmKit's own docs, not here.**
   Per `../FanslationStudio.LlmKit/docs/README.md`'s source-of-truth rule, a finding about how
   `QualityReviewWorkflow`/`QualityReviewHelpers`/packaging actually behaves belongs in
   `../FanslationStudio.LlmKit/docs/` (e.g. extending `features/translation-pipeline/quality-review-pass.md` or
   adding a new topic file), even when you're investigating from this downstream repo's session.
   Only genuinely downstream-specific findings (a game-specific config choice, a per-file exclusion
   rule, a repo's own test-run notes) belong in this repo's own `docs/KNOWN_ISSUES.md`/`docs/investigations/`.
