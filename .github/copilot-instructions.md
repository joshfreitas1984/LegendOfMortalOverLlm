---
applyTo: "**"
---

# LegendOfMortalOverLlm

This repository contains a Mono BepInEx translation project for *Legend of Mortal*.

- Read the matching `.github/instructions/*.instructions.md` file before editing a sub-project.
- Keep reusable extraction, translation, packaging, and configuration code in `Translate/`.
- Keep numbered workflow facts and regression tests in `Tests/`; do not run state-mutating workflow
  facts, live LLM calls, exports, or packaging unless explicitly requested.
- Keep runtime dumping, Harmony patches, and injection in `LegendOfMortalPlugin/`.
- Treat `Files/Raw/Export`, `Files/Converted`, and `Files/Mod` as working data; do not overwrite them
  during static validation.
- Preserve the Mono game assembly references and game-specific hooks. Generic translation behavior
  belongs in the sibling `FanslationStudio.LlmKit` repository.
- `docs/README.md` is the documentation hub. Keep `docs/KNOWN_ISSUES.md` as an index only, with
  durable behavior in `docs/features/`, investigations in `docs/investigations/`, plans in
  `docs/plans/`, and architecture in `docs/architecture/`.
