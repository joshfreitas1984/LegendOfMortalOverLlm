# Legend of Mortal project layout

The repository follows the shared downstream translation-project structure while retaining the
optional `SharedAssembly/` boundary required by this game.

| Path | Responsibility |
| --- | --- |
| `Translate/` | Reusable game-specific extraction, translation, packaging, and configuration code. |
| `Tests/` | xUnit regressions and manually-run numbered workflow facts. |
| `Files/` | Raw dumps, converted translation state, glossary/configuration data, and mod output. |
| `LegendOfMortalPlugin/` | Mono BepInEx dumping, Harmony patches, and runtime string-table injection. |
| `SharedAssembly/` | Contracts shared across tooling/runtime where LlmKit does not own them. |

The solution references the sibling `FanslationStudio.LlmKit` project directly. This project does
not include Dragon Heir's IL2CPP converter, prefab-text workflow, dynamic-string workflow, or Verify
harness because those are not part of Legend of Mortal's current pipeline.
