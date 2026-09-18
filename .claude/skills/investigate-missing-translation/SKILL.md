---
name: investigate-missing-translation
description: Investigates a specific string/line that shows up untranslated (still in the original language) in the running game, or is missing entirely from the mod output — by walking the pipeline stage-by-stage (extraction → Converted → QC/packaging raw-fallback → runtime injection) to localize which stage silently dropped it, before assuming it's a generic LlmKit bug. Different from investigate-qc-issue (wrong quality score) and investigate-packaging-issue (packaging logic bug) — use this one when the complaint is "this specific line is still Chinese/Japanese/etc. in-game" or "this line never made it into the mod at all."
---

# Investigate a missing/untranslated string

A single line can fail to appear translated at almost any stage of the pipeline, and — importantly
— the actual root cause is frequently **game-specific**, not a generic LlmKit bug: each downstream
repo has its own extraction quirks, duplicate/collision keys, IL2CPP function-routed entries,
dash/character-normalization rules, or runtime injection code. Work through these steps **in
order**, on the exact source text/key involved, and check this repo's *own* docs before assuming
LlmKit's generic docs will explain it — they generally won't, because the weirdness lives here.

1. **Confirm it was extracted/dumped at all.** Find the source game data file (CSV/JSON/prefab
   text/dynamic string) under `Raw/Dumped` (or this repo's equivalent raw-extraction folder) and
   confirm the exact line/key is actually present. If it isn't, the failure is at extraction time,
   not translation. Check this repo's own extraction docs and `docs/KNOWN_ISSUES.md` for that file type
   first — duplicate keys, encoding issues, and extraction skip-rules are common silent causes (see
   step 4; don't skip straight to code-diving here).

2. **Check `Files/Converted/*.yaml` for the line.** Find the matching `TranslationLine`/
   `TranslationSplit` and confirm it exists and has a non-empty `Translated` value. If the line or
   split is simply missing here, the failure is upstream (extraction/decomposition/splitting), not
   translation or packaging — go back to step 1's extraction docs rather than looking at QC or
   packaging code. If it's present with a `Translated` value but still shows raw in the final
   output, continue to step 3.

3. **Check whether it fell to raw-fallback at QC or packaging time.** A translated line can still
   end up raw/missing in `Files/Mod` if QC's score-gate/freshness logic held back a correction, or
   if packaging's raw-fallback rule kicked in (`SkipColumns`, `PackageOutput: false`, unsafe/flagged
   split, or the `PrefabText`/`DynamicString` "omit rather than write raw Chinese" rule). Don't
   re-derive this logic from scratch — read
   `../FanslationStudio.LlmKit/docs/features/packaging/packaging-workflows.md`'s (sibling repo, relative to this
   downstream repo's working directory) raw-fallback section and, if a QC score/rejection looks
   implicated, hand off to the
   `investigate-qc-issue` skill; if it's a packaging-logic question (reconstruction, `SkipColumns`,
   `PackageOutput`), hand off to `investigate-packaging-issue`. Both skills already own that
   investigation in depth — this skill's job is only to localize the failure to "QC" vs.
   "packaging" vs. somewhere else.

4. **Check for game-specific known causes — look at THIS repo's own docs first.** Before assuming a
   generic bug, read this repo's own `docs/KNOWN_ISSUES.md` (or equivalent index) and its
   `docs/README.md` "Where should I look?" table for the affected file type/pipeline. Common
   game-specific root causes that live here, not in LlmKit:
   - **Duplicate/collision keys** — multiple source rows sharing one lookup key, where injection is
     last-write-wins (or the game's own localization resolver special-cases the key and never looks
     it up at all), so one entry's translation silently never applies even though it packaged fine.
   - **IL2CPP function-routed entries** — a cell that's really `"{label};FunctionName;params"` or
     similar, excluded from QC/translation on purpose (a `CustomQcExclusionRule` or extraction
     skip-rule) — check whether the specific string is a structural/routing entry rather than prose.
   - **Per-game dumping/injection quirks** — dash/character normalization, encoding mismatches
     between dump and injection, or a field the dumper doesn't treat as a primary text field.
   These are exactly the kind of causes already diagnosed and written up per-repo (e.g. a duplicate-
   key collision documented in one downstream repo's docs, or an IL2CPP-routed exclusion documented
   in another's) — search this repo's docs for the file/column name before concluding it's new.

5. **If it exists in `Files/Mod` but still doesn't show in-game, check runtime injection.** Look at
   this repo's `Plugin`/runtime injection code and Harmony patches (dump/injection patch classes,
   whatever this repo calls them) — a translation can be correctly packaged and still never reach
   the screen if the injection patch doesn't hook the code path that renders that string, or if a
   key-based lookup misses at runtime for a reason packaging can't see (e.g. the collision case in
   step 4). This layer is entirely downstream-repo-specific code; LlmKit has no visibility into it.

6. **Only after ruling out game-specific causes, treat it as a possible LlmKit-generic bug.** If
   steps 1-5 didn't explain it, follow `investigate-qc-issue` or `investigate-packaging-issue` as
   appropriate for a deeper dive into that shared logic. If you do confirm a genuine LlmKit-internal
   bug, the finding belongs in `FanslationStudio.LlmKit`'s own `docs/` per its source-of-truth rule
   — not in this downstream repo's `docs/KNOWN_ISSUES.md`/`docs/`, even though you found it from here.
