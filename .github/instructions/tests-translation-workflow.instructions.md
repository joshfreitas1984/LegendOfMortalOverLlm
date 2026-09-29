---
applyTo: "{Translate,Tests}/**"
---

# Translate and Tests

`Translate/` owns reusable game-specific extraction, translation, packaging, and configuration.
`Tests/` owns numbered workflow facts and pure regression tests. The sibling
`FanslationStudio.LlmKit` project owns shared Line/Split/Template and compound-field behavior.

- Do not run numbered workflow facts, live LLM calls, exports, packaging, or merge steps unless
  explicitly requested; they mutate `Files/` state.
- Keep workflow execution in `Tests/`, not `Translate/`. Use pure xUnit tests for isolated behavior.
- Route CSV parsing and reconstruction through LlmKit's `CompoundFieldSplitter`; never use naive
  comma splitting for game data.
- Preserve skipped columns, structural tokens, placeholders, signed numbers, and compound-field
  boundaries. Add targeted template/fragment assertions when decomposition changes.
- Keep game-specific rules in `Translate/`; generic behavior belongs in the sibling LlmKit repo.

See [`docs/README.md`](../../docs/README.md) and the sibling LlmKit translation documentation for
current pipeline details.
