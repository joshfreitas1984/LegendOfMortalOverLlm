using FanslationStudio.LlmKit.Configuration;
using FanslationStudio.LlmKit.Support;
using FanslationStudio.LlmKit.Utility;
using FanslationStudio.LlmKit.Workflow;
using Translate;
using static FanslationStudio.LlmKit.GameFileHandlingBase;

namespace Tests;

public class QualityControlWorkflowTests
{
    // The full "I changed the glossary / got file updates / exported more dynamic strings / added a
    // bad word / needed a new game repair" workflow in one call: brute-forces Translated back to
    // clean (TranslationWorkflow.TranslateLinesBruteForce), then does the same for QcTranslated
    // (QualityReviewWorkflow.RunBruteForce - a no-op if qualityReview.enabled is false), then
    // packages. Use this instead of running "1" and "3b" separately when you want QC kept in sync
    // too.
    [Fact(DisplayName = "0. TranslateAndQualityReviewBruteForce")]
    public async Task TranslateAndQualityReviewBruteForce()
    {
        await TranslationWorkflow.TranslateLinesBruteForce(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit, GameFileHandling.Hooks);
        await QualityReviewWorkflow.RunBruteForce(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit, hooks: GameFileHandling.Hooks);
        await FileOutputWorkflowTests.PackageFinalTranslation();
    }

    // Run this BEFORE "2" the first time you try a candidate qualityReview model - reviews only a
    // small random sample (see QualityReviewWorkflow.RunAsync's sampleSize) instead of every
    // eligible column, so you can judge a model's real speed/score-distribution/correction-quality
    // on your hardware before committing an entire run to it. See docs/plans/quality-review-pass.md's
    // "Sample run before committing to a full-corpus pass". A no-op if qualityReview.enabled is
    // false.
    [Fact(DisplayName = "1. RunQualityReviewPassSample")]
    public async Task RunQualityReviewPassSample()
    {
        await QualityReviewWorkflow.RunAsync(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit, sampleSize: 300, hooks: GameFileHandling.Hooks);
    }

    // Independent of the main translate/apply-rules/translate-lines steps in TranslationWorkflowTests
    // - reviews already-translated text against a separately configured model (Config.yaml's
    // qualityReview: section), proposes corrections, and validates them before writing anything.
    // See docs/plans/quality-review-pass.md. A no-op (logs and returns) if qualityReview.enabled
    // is false, so it's safe to run even before the feature is configured for a real run.
    [Fact(DisplayName = "2. RunQualityReviewPass")]
    public async Task RunQualityReviewPass()
    {
        await QualityReviewWorkflow.RunAsync(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit, hooks: GameFileHandling.Hooks);
    }

    // Run this after a glossary/config change so already-QC'd QcTranslated text picks up the same
    // rule changes TranslationWorkflowTests' "2. ApplyRulesToCurrentTranslation" applies to
    // Translated - otherwise QC's output would silently drift out of sync with the current rules.
    [Fact(DisplayName = "3. ApplyRulesToQCReview")]
    public async Task ApplyRulesToQCReview()
    {
        await QualityReviewWorkflow.ApplyRulesToCurrentQcTranslated(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit, GameFileHandling.Hooks);
    }

    // Reporting-only, mirrors TranslationWorkflowTests' "4. Find All Failing Translations" but
    // scoped to quality-review flags (a rejected correction, or a low QcQualityScore) instead of
    // translation failures - see QualityReviewWorkflow.GetFlaggedQcReviews.
    [Fact(DisplayName = "4. Find Flagged Quality Review Items")]
    public async Task FindFlaggedQcReviews()
    {
        var workingDirectory = GameFileHandling.WorkingDirectory;
        var flagged = await QualityReviewWorkflow.GetFlaggedQcReviews(workingDirectory, TextFileConfiguration.TextFilesToSplit);

        var serializer = YamlHelper.CreateSerializer();
        var yaml = serializer.Serialize(flagged);
        FileHelper.WriteAllTextWithRetry($"{workingDirectory}/TestResults/FlaggedQcReviews.yaml", yaml);
    }

    // Turns "4"'s flat 9000+-row dump into something a human can actually work down over time,
    // instead of eyeballing every row or hand-marking individual lines as "ok" (the data model has
    // no such field, and QualityReviewWorkflow deliberately has no manual-approval gate). See
    // QualityReviewWorkflow.QcTriageResult's doc comment (FanslationStudio.LlmKit) for what the
    // "rejected/by-reason" vs "low-score-only" split means and why they need different treatment.
    // Writes QcTriageSummary.yaml/QcTriageByReason.yaml/QcTriageLowScoreSample.yaml under
    // TestResults. Safe and cheap to re-run any time (no LLM calls) - re-run after any
    // prompt/glossary/config fix to see the reason clusters shrink and the low-score sample shift as
    // real progress is made.
    [Fact(DisplayName = "5. Triage Flagged Quality Review Items")]
    public async Task TriageFlaggedQcReviews()
    {
        await QualityReviewWorkflow.WriteTriageReportAsync(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit, GameFileHandling.Hooks);
    }

    // Turns "5"'s clusters into ready-to-paste prompts for a Claude chat (QcTriagePrompts.md),
    // rather than a fully automated fix pipeline - the actual edits (BaseQualityReviewPrompt.txt
    // wording, a glossary rule, qualityReview.minAcceptableScore) are small and judgment-heavy
    // enough that a human should read the examples and apply the change themselves, not have an
    // LLM edit prompt files unsupervised. Paste a section at a time into a chat; apply whatever fix
    // comes back by hand, then use "Reset Qc Retry Limits"/"Reset Leaked Quality Review
    // Corrections" + a re-run to see the cluster shrink next time "4"/"5" run.
    [Fact(DisplayName = "6. Generate Quality Review Fix Prompts")]
    public async Task GenerateQcFixPrompts()
    {
        await QualityReviewWorkflow.WriteFixPromptsAsync(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit, GameFileHandling.Hooks);
    }

    // Run this after fixing whatever was causing a persistent QC rule violation (e.g. removed a
    // false-positive bad word, loosened a glossary rule) so columns QualityReviewWorkflow.RunBruteForce
    // already gave up on (see TranslationSplit.QcRuleCheckFailureCount) get retried instead of
    // staying parked forever. A no-op for everything else.
    [Fact(DisplayName = "7. Reset Qc Retry Limits")]
    public async Task ResetQcRetryLimits()
    {
        await QualityReviewWorkflow.ResetQcRetryLimits(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit);
    }

    // Sweeps every already-QC'd column for a stored QcTranslated/QcRejectedCorrection that leaked QC
    // protocol text (a stray "NONE", "SCORE:", "CORRECTED:", etc. - see
    // QualityReviewWorkflow.ContainsLeakedProtocolText) rather than a clean correction. This catches
    // corruption that got past an earlier, narrower version of the leak guard - e.g. the
    // "Sword Technique Power NONE" case, where the guard only rejected a response that was *exactly*
    // "NONE", not one with "NONE" stuck onto real text. Any match is reset back to QcStatus.NotReviewed
    // (full TranslationSplit.ResetQcState) so the next "1"/"2" run gives it a genuinely fresh review.
    // Safe to run any time - a no-op once the corpus is clean.
    [Fact(DisplayName = "Reset Leaked Quality Review Corrections")]
    public async Task ResetLeakedQcCorrections()
    {
        await QualityReviewWorkflow.ResetLeakedQcCorrections(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit);
    }

    // Run this after changing how QC's score is judged (BaseQualityReviewPrompt.txt's scoring
    // rubric, or switching qualityReview.modelName to a model that scores on a different scale) so
    // every column currently sitting below minAcceptableScore under the OLD calculation gets a
    // genuinely fresh score under the new one. Leaves every already-accepted column with an
    // acceptable score untouched (unlike "Reset ALL Quality Review State", which re-reviews
    // everything) - only the columns actually worth another look get re-sent to the LLM. A
    // rejected-correction column (Reason set, QcQualityScore already cleared to null) is never
    // touched here - use "Reset Qc Retry Limits" for those.
    [Fact(DisplayName = "7. Reset Low-Score Quality Review State")]
    public async Task ResetLowScoreQcState()
    {
        await QualityReviewWorkflow.ResetLowScoreQcState(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit, GameFileHandling.Hooks);
    }

    // DEFECT-category counterpart to "7" (which resets by score threshold instead). Resets every
    // flagged column whose DEFECT category is NOT in Config.yaml's qualityReview.autoAcceptDefectCategories
    // back to NotReviewed for a fresh review - see Tests/docs/qc-qualityscore-noise-investigation.md's
    // "stratify by DEFECT category" policy step. QcDefectCategory.Unknown always lands in this bucket
    // (a line whose response predates the DEFECT-first prompt, or otherwise failed to parse a DEFECT:
    // line), so run this once to sweep up the ~1,100 Unknown rows left over from before DEFECT was
    // parsed and backfill them via "2" next. Safe to re-run any time autoAcceptDefectCategories
    // changes (a category's hand-validated precision verdict is added or revised) to pull the
    // newly-decided set back out of "flagged" one way or the other on the next "1"/"2" pass.
    [Fact(DisplayName = "8. Reset Non-Auto-Accepted Quality Review State")]
    public async Task ResetNonAutoAcceptedQcState()
    {
        await QualityReviewWorkflow.ResetNonAutoAcceptedQcState(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit, GameFileHandling.Hooks);
    }

    // Run this ONCE after adding the DROPPED_STUTTER defect category/stutter-handling rule to
    // BaseSystemPrompt.txt/BaseQualityReviewPrompt.txt: before that change, a Chinese stammer/stutter
    // (e.g. "思、思阁主", "你、你、你……") had no named defect to score against, so a dropped stutter
    // almost always passed QC silently at a high score instead of getting flagged - it would NOT have
    // shown up under DROPPED_CONTENT (scoped to subject/object/clause/title only) or reliably under
    // OTHER_NAMED_DEFECT (the scoring anchors explicitly told the model not to score down anything
    // outside the named defect list). This resets every column whose SOURCE actually contains the
    // pattern back to NotReviewed regardless of its old score/status, so the next "1"/"2" pass gives
    // it a genuinely fresh review under the new prompt - far cheaper than "Reset ALL Quality Review
    // State" since it targets only the columns a plain regex scan finds, with no LLM call of its own.
    [Fact(DisplayName = "9. Reset Stutter-Affected Quality Review State")]
    public async Task ResetStutterAffectedQcState()
    {
        await QualityReviewWorkflow.ResetStutterAffectedQcState(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit);
    }

    // Run this ONCE after adding the tag-seam rule to BaseQualityReviewPrompt.txt/
    // BaseQualityReviewVerificationPrompt.txt (all model families): before that rule existed, the QC
    // model had no guidance that a markup/formatting tag or placeholder (e.g. <b>, <color=...>)
    // sitting between two stitched fragments isn't a sentence boundary, so a genuinely broken seam
    // like "...here.<b>#PosText#</b>Inside" (a translated compound sentence split into two
    // independently-translated fragments that don't grammatically connect) could pass silently at
    // DEFECT: NONE / SCORE: 100 - see the /investigate-qc-issue writeup for the dumpedPrefabText line
    // that surfaced this. Scans every templated column's CURRENT effective (reconstructed) translated
    // text for that tag-seam signature (a period touching a tag open AND a tag close touching a
    // capital, both required - see QualityReviewWorkflow's TagSeamPunctBeforeRegex/
    // TagSeamCapitalAfterRegex) and resets any match back
    // to NotReviewed for a fresh review under the new prompt rule, regardless of its old score/status
    // - console output during the run lists exactly which file/split each reset column came from, so
    // you can eyeball the affected lines before/after the next "1"/"2" pass re-reviews them. Far
    // cheaper than "Reset ALL Quality Review State" since it only touches columns the regex scan
    // actually finds, with no LLM call of its own - same shape as "9" above.
    [Fact(DisplayName = "Reset Tag-Seam-Affected Quality Review State")]
    public async Task ResetTagSeamAffectedQcState()
    {
        await QualityReviewWorkflow.ResetTagSeamAffectedQcState(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit);
    }

    // Run this ONCE after adding the negative-sign check to TranslationWorkflow.EvaluateRules'
    // validation gate: before that check existed, a QC correction on a stat/buff tooltip could drop
    // the leading "-" off a negative percentage ("-0.5%全属性" -> "0.5% All Attributes") and still
    // sail through as an accepted Corrected column, since nothing validated a negative number's sign
    // survived the rewrite - see the /investigate-qc-issue writeup for the dumpedPrefabText "内伤"
    // debuff tooltip that surfaced this (Files/Converted/dumpedPrefabText.txt.yaml). Scans every
    // already-Corrected column's QcTranslated against its reconstructed SOURCE and resets any match
    // back to NotReviewed for a fresh review under the new gate, regardless of its old score - same
    // shape as "Reset Tag-Seam-Affected"/"Reset Stutter-Affected" above, no LLM call of its own.
    [Fact(DisplayName = "Reset Negative-Sign-Affected Quality Review State")]
    public async Task ResetNegativeSignAffectedQcState()
    {
        await QualityReviewWorkflow.ResetNegativeSignAffectedQcState(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit);
    }

    // Dedicated single-row QC sample: forces a fresh QualityReviewWorkflow review of exactly ONE
    // known PlotData.csv row (the master's "别慌..." collapse line, split 10 - see
    // Files/Converted/PlotData.csv.yaml) instead of a random sampleSize=N slice
    // (RunQualityReviewPassSample) or a full corpus pass (RunQualityReviewPass). Useful for
    // iterating on the BaseQualityReviewPrompt/model/glossary and immediately seeing how just this
    // row's QC verdict changes, without waiting on (or perturbing the Qc state of) every other
    // already-reviewed row. Scoping textFiles to only PlotData.csv keeps RunAsync's freshness check
    // from doing any real work outside this one row - every other row/column in the file is still
    // "fresh" (its QcReviewedText already matches its current Translated) and gets skipped with no
    // LLM call, exactly like a normal RunAsync pass would treat it.
    //
    // To point this at a different row, change TargetRawFragment below to the exact `text` of the
    // split-10 (or whichever column) anchor fragment you want to compare, as it appears in
    // Files/Converted/PlotData.csv.yaml.
    [Fact(DisplayName = "RunQualityReviewSampleForOneLine")]
    public async Task RunQualityReviewSampleForOneLine()
    {
        const string TargetFile = "PlotData.csv";
        const string TargetRawFragment = "莫慌，让为师看看……";

        var workingDirectory = GameFileHandling.WorkingDirectory;
        var textFiles = TextFileConfiguration.TextFilesToSplit
            .Where(t => t.Path == TargetFile)
            .ToArray();

        Assert.True(textFiles.Length > 0, $"No configured TextFileToSplit entry for '{TargetFile}'.");

        TranslationLine? targetLine = null;
        TranslationSplit? targetAnchor = null;
        string? beforeReviewedText = null;
        string? beforeQcTranslated = null;
        QcStatus? beforeStatus = null;
        int? beforeScore = null;

        // Pass 1: locate the row, snapshot its current Qc state, then force a fresh review by
        // resetting just this one column's Qc fields (ResetQcState) - everything else in the file
        // is left untouched.
        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, textFiles, async (outputFile, textFile, fileLines) =>
        {
            foreach (var line in fileLines)
            {
                var anchor = line.Splits.FirstOrDefault(s => s.Text == TargetRawFragment && s.SubIndex == 0);
                if (anchor == null)
                    continue;

                targetLine = line;
                targetAnchor = anchor;
                beforeReviewedText = anchor.QcReviewedText;
                beforeQcTranslated = anchor.QcTranslated;
                beforeStatus = anchor.QcStatus;
                beforeScore = anchor.QcQualityScore;

                anchor.ResetQcState();
                break;
            }

            if (targetAnchor != null)
            {
                var serializer = YamlHelper.CreateSerializer();
                await FileHelper.WriteAllTextWithRetryAsync(outputFile, serializer.Serialize(fileLines));
            }
        });

        Assert.True(targetAnchor != null, $"Could not find a split-10 anchor in '{TargetFile}' with text '{TargetRawFragment}'.");

        // Pass 2: the actual QC pass, scoped to just this one file (and, thanks to the reset above,
        // effectively just this one row - every other row is still fresh and gets skipped for free).
        await QualityReviewWorkflow.RunAsync(workingDirectory, textFiles, hooks: GameFileHandling.Hooks);

        // Pass 3: re-read and report the before/after comparison, reusing the same shape
        // QualityReviewWorkflow.FlaggedQcReview already uses so this slots into the existing
        // FlaggedQcReviews.yaml reporting conventions.
        string? afterReviewedText = null;
        string? afterQcTranslated = null;
        QcStatus? afterStatus = null;
        int? afterScore = null;
        string? afterRejectedCorrection = null;
        string? afterFailureReason = null;
        string rawText = TargetRawFragment;

        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, textFiles, async (_, textFile, fileLines) =>
        {
            foreach (var line in fileLines)
            {
                var anchor = line.Splits.FirstOrDefault(s => s.Text == TargetRawFragment && s.SubIndex == 0);
                if (anchor == null)
                    continue;

                var template = line.Templates.FirstOrDefault(t => t.Split == anchor.Split);
                var fragments = line.Splits.Where(s => s.Split == anchor.Split).OrderBy(s => s.SubIndex).ToList();
                rawText = template != null
                    ? CompoundFieldSplitter.Reconstruct(template.Template, fragments.Select(f => f.Text).ToList())
                    : anchor.Text;

                afterReviewedText = anchor.QcReviewedText;
                afterQcTranslated = anchor.QcTranslated;
                afterStatus = anchor.QcStatus;
                afterScore = anchor.QcQualityScore;
                afterRejectedCorrection = string.IsNullOrEmpty(anchor.QcRejectedCorrection) ? null : anchor.QcRejectedCorrection;
                afterFailureReason = string.IsNullOrEmpty(anchor.QcFailureReason) ? null : anchor.QcFailureReason;
                break;
            }

            await Task.CompletedTask;
        });

        var comparison = new
        {
            filePath = TargetFile,
            text = rawText,
            before = new { qcReviewedText = beforeReviewedText, qcTranslated = beforeQcTranslated, qcStatus = beforeStatus, qcQualityScore = beforeScore },
            after = new { qcReviewedText = afterReviewedText, qcTranslated = afterQcTranslated, qcStatus = afterStatus, qcQualityScore = afterScore, rejectedCorrection = afterRejectedCorrection, reason = afterFailureReason },
        };

        var reportSerializer = YamlHelper.CreateSerializer();
        var yaml = reportSerializer.Serialize(comparison);
        FileHelper.WriteAllTextWithRetry($"{workingDirectory}/TestResults/QcReviewSample_SingleLine.yaml", yaml);

        Console.WriteLine(yaml);
    }

    // Regression test for the omitted-subject/mechanical-subject-carryover QC bug: a Chinese line
    // that omits its subject ("还好还好，只是精疲力竭昏厥过去了。" - no "I"/"she"/anyone stated) was
    // being translated as first-person ("No need to worry, I just fainted from exhaustion.") when
    // the surrounding dialogue ("莫慌，让为师看看……" - "Don't panic, let me take a look...") makes
    // clear the speaker is examining someone ELSE, who is the one who fainted. Two real bugs
    // combined to let this slip through QC even after BaseQualityReviewPrompt.txt's rule against
    // it: (1) the model would score this low (correctly flagging it) but still answer
    // "CORRECTED: NONE" - fixed by BaseQualityReviewPrompt.txt's added CONSISTENCY rule; (2) even
    // when the model DID propose a correction, it often joined the corrected sentences with a real
    // line break instead of the SOURCE/TRANSLATION convention's literal "\n", and
    // QualityReviewWorkflow.CorrectedLineRegex was Multiline-anchored without Singleline, so it
    // silently truncated the captured correction to just its first physical line - fixed by adding
    // RegexOptions.Singleline. See the conversation history in this repo's task log for the full
    // diagnosis (direct Ollama reproduction that isolated each bug).
    //
    // Deliberately bypasses the corpus (Files/Converted/PlotData.csv.yaml) entirely and calls
    // QualityReviewWorkflow.GetLlmVerdictAsync directly with a fixed, known-bad SOURCE/TRANSLATION
    // pair - so this test (a) survives corpus edits/repackaging, (b) can be re-run immediately with
    // no ResetQcRetryLimits/ResetQcState dance, and (c) automatically re-validates against whichever
    // model Config.yaml's qualityReview.modelName currently points at, so swapping QC models
    // re-checks this exact regression case with zero test changes. Samples the model a few times
    // (temperature is low but non-zero) since a single call could get a differently-worded but
    // still-correct answer, or vice versa - treat ANY sample reproducing the bug as a real
    // regression, not something to average away.
    //[Fact(DisplayName = "QcOmittedSubjectRegression")]
    //public async Task QcOmittedSubjectRegression()
    //{
    //    const string Source = "莫慌，让为师看看……\\n还好还好，只是精疲力竭昏厥过去了。\\n此次战况如何，云裳又是为何受伤啊？";
    //    const string BadTranslation = "Don't panic, let me take a look......\\nNo need to worry, I just fainted from exhaustion.\\nHow did the battle go, and why was Yunshang injured?";
    //    const int Samples = 3;

    //    var workingDirectory = GameFileHandling.WorkingDirectory;
    //    var config = ConfigurationExtensions.GetConfiguration(workingDirectory, GameFileHandling.Hooks);

    //    if (string.IsNullOrEmpty(config.QualityReview.ModelName)
    //        || !config.Runtime.Models.TryGetValue(config.QualityReview.ModelName, out var modelConfig))
    //        throw new InvalidOperationException(
    //            $"QualityReview.ModelName '{config.QualityReview.ModelName}' does not match any configured model - " +
    //            "set qualityReview.enabled/modelName in Config.yaml to run this test.");

    //    var tokenReplacer = new StringTokenReplacer();
    //    var maskedRaw = tokenReplacer.Replace(Source);
    //    var maskedTranslated = tokenReplacer.Replace(BadTranslation);
    //    var glossaryPrompt = GlossaryLine.AppendPromptsFor(Source, config.Runtime.GlossaryLines, "PlotData.csv");

    //    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(300) };

    //    var results = new List<(int Score, string? Corrected)>();
    //    for (var i = 0; i < Samples; i++)
    //    {
    //        var verdict = await QualityReviewWorkflow.GetLlmVerdictAsync(config, modelConfig, client, Source, maskedRaw, maskedTranslated, glossaryPrompt);
    //        Assert.True(verdict.Success, $"Sample {i + 1}: QC response did not parse - see console output above for the raw response.");

    //        var corrected = verdict.CorrectedRawMasked == null ? null : tokenReplacer.Restore(verdict.CorrectedRawMasked);
    //        results.Add((verdict.Score, corrected));
    //    }

    //    Console.WriteLine(YamlHelper.CreateSerializer().Serialize(
    //        results.Select(r => new { r.Score, r.Corrected })));

    //    foreach (var (score, corrected) in results)
    //    {
    //        // Bug 1 (CONSISTENCY): a low score with no correction at all means the model flagged a
    //        // real problem but hedged with NONE instead of fixing it.
    //        if (score < config.QualityReview.MinAcceptableScore)
    //            Assert.True(corrected != null,
    //                $"QC scored this {score} (below MinAcceptableScore={config.QualityReview.MinAcceptableScore}) but proposed no correction.");

    //        // Bug 2 (regex truncation) and the underlying translation bug both manifest the same
    //        // way here: the known-bad first-person phrasing survives into whatever we end up with.
    //        Assert.DoesNotContain("I just fainted", corrected ?? BadTranslation, StringComparison.OrdinalIgnoreCase);
    //    }
    //}

    // Full do-over, NOT a routine step - unlike "Reset Qc Retry Limits"/"Reset Leaked Quality
    // Review Corrections" (which only un-stick/repair specific stuck-or-corrupted columns), this
    // wipes every column's Qc* state back to NotReviewed
    // regardless of its current status, so the next "1"/"2"/TranslationWorkflowTests' "1a" pass
    // reviews the ENTIRE corpus again from scratch. IsQcReviewFresh only tracks whether Translated
    // changed, never whether the QC model/prompt that produced an existing verdict did - so
    // swapping the QC model, or a prompt change significant enough that already-recorded
    // Passed/Corrected verdicts can no longer be trusted (see
    // docs/quality-review-pass-architecture.md's "Postmortems" section, FanslationStudio.LlmKit -
    // the omitted-subject rule and low-score-discard retry bug fixed there both mean a PRIOR
    // verdict may be less trustworthy than its stored status suggests), is when to run this - never
    // as a matter of routine, since a full re-review is the same many-hours job a first full run
    // already was.
    [Fact(DisplayName = "Reset ALL Quality Review State (full re-review)")]
    public async Task ResetAllQcState()
    {
        await QualityReviewWorkflow.ResetAllQcState(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit);
    }

    // Regression coverage for GameFileHandling.ExcludePlotChoiceColumnFromQc (registered via
    // GameFileHandling.Hooks.CustomQcExclusionRule): PlotData.csv column 9's interaction-choice
    // cells - both the multi-choice "text;FunctionName;0|text;FunctionName;1"-joined shape and a
    // single "text;FunctionName[;param]" entry with no '|' at all (real triage examples: the "|"
    // cluster's ForceFightChooseStartSkill row, and the pipe-less RobHeroItemChoose/
    // StartYoungHeroFightMatch rows) - must never reach the QC pass, since the model reliably
    // flattens their structural delimiters into prose and ValidateGameSpecificColumn then rejects
    // every such correction, forever. Plain column-9 choice text with no delimiter, and column 9
    // content in any other file, must still go through QC as normal.
    [Theory(DisplayName = "ExcludePlotChoiceColumnFromQc covers pipe-joined and pipe-less shapes")]
    [InlineData("内功 吐纳法;ForceFightChooseStartSkill;0|轻功 轻身术;ForceFightChooseStartSkill;1", true)]
    [InlineData("给我拿来吧！;RobHeroItemChoose;公孙防", true)]
    [InlineData("上场比试;StartYoungHeroFightMatch", true)]
    [InlineData("推辞不受", false)]
    public void ExcludePlotChoiceColumnFromQc_PlotDataColumn9(string raw, bool expectedExcluded)
    {
        var textFile = new TextFileToSplit { Path = "PlotData.csv" };

        var excluded = GameFileHandling.Hooks.CustomQcExclusionRule!(textFile, 9, raw);

        Assert.Equal(expectedExcluded, excluded);
    }

    [Fact(DisplayName = "ExcludePlotChoiceColumnFromQc does not exclude other PlotData.csv columns")]
    public void ExcludePlotChoiceColumnFromQc_OtherColumnNotExcluded()
    {
        var textFile = new TextFileToSplit { Path = "PlotData.csv" };
        const string raw = "上场比试;StartYoungHeroFightMatch";

        var excluded = GameFileHandling.Hooks.CustomQcExclusionRule!(textFile, 8, raw);

        Assert.False(excluded);
    }
}
