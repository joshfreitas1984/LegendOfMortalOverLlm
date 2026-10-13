---
name: diagnose-translation-failure
description: Diagnoses why specific source lines keep failing translation and end up flaggedForRetranslation (flaggedMistranslation "Failed", empty translated) by re-running the real translation path on just those lines and reading the exact prompt, glossary, line context, model output and rejection reason per retry. Use when lines are repeatedly flagged "Failed", appear in Files/TestResults/UnprocessableItems.log, or a translation keeps being rejected by validation. Needs the local LLM (Ollama) running.
---

# Diagnose a translation that keeps failing

1. **Classify the flag first** (grep `flaggedMistranslation:` in `Files/Converted/*.yaml`):
   - `"Failed"` = translation came back empty: validation rejected every retry. Go to step 2.
   - `"<result>,<raw>,"` = glossary term missing from the output (glossary/prompt issue).
   - a category name (`Bad Character`, pronoun category, validation reason) = a post-translation rule fired.
   - `Missing required token '{slot}'`, `Invented slot` or `Unknown placeholder` = a slot rule fired (see step 4).
2. **Read `Files/TestResults/UnprocessableItems.log`** (written by the last translate run): per line it has
   RAW, the last model RESULT and the REASON. Group the failing lines by REASON and look for what they share
   (a term, a pattern, a source punctuation) before theorising. Check how many *similar* lines translated fine
   (`grep -c` the shared term in the Converted yaml) so a "this term is broken" theory is tested against successes.
3. **Reproduce with the real path** instead of guessing. Put the exact `split.text` values, one per line, in a
   scratch file and run the game's diagnostic test (needs Ollama):

   ```
   cd Tests
   DIAG_TEXTS=<scratch file> DIAG_ATTEMPTS=3 dotnet test --filter "FullyQualifiedName~DiagnoseTranslationTests"
   ```
   Optional env: `DIAG_FILE` (TextFilesToSplit path, default `PlotData.csv`), `DIAG_OUT` (report path, default
   `Files/TestResults/DiagnoseTranslation.txt`). This calls `TranslationDiagnostics.DiagnoseAsync`
   (`FanslationStudio.LlmKit/Utility/TranslationDiagnostics.cs`), which prints per line: current flags, resolved
   line context, glossary entries injected, full prompt, then per attempt each request/response of the retry loop
   (system prompt omitted, output truncated), and the final validity plus rejection reason.
   A game without the test copies `Tests/DiagnoseTranslationTests.cs` from DragonHierOverLlm.
4. **Read the report for**: (a game with `namedTokenReplacers` also gets the slotted text the model is sent, the text in
   the game's form, each slot's source, restored value and description, and the result both ways) a line whose only
   problem is a `SLOT PROBLEM` (a dropped, invented or renamed `{slot}`) is the model failing to copy a token: look at the
   slot's `description` in `Config.yaml` and at whether the text around it is a natural place for it; `RESULT cannot be
   restored` is a data problem (a missing `tokens` entry, a glossary entry that is gone), not a model problem; is the line context empty or wrong; do the injected glossary entries look
   contradictory; does the *first* response already leak Chinese/commentary, or does only the correction loop
   degrade (the correction turn feeds the bad answer back and often reproduces it); do attempts differ (stochastic,
   so a retry or escalation model may pass) or repeat identically (deterministic, so only the prompt, glossary or a
   manual translation can fix it).
5. **Pick the fix by cause**: model code-switching / ignoring the output rules -> enable `escalationModelName` in
   Config.yaml or add a manual translation (`Files/ManualTranslations.yaml`); a wrong or missing term -> use the
   `add-glossary-fix` skill; a prompt/context problem -> fix in LlmKit and prove against the gold set.
   Remove the scratch file when done; do not commit report output.
