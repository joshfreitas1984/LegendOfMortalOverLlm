# Workflow execution reference

The Legend of Mortal workflow is a stateful, manually-run pipeline. `Tests/` facts operate on the
working data under `Files/` and may call a local LLM or regenerate exports and mod output.

Use the numbered workflow facts only when intentionally progressing a translation run. Static
builds and pure regression tests are safe validation; live LLM calls, export, packaging, and merge
steps are not automatic validation.

The reusable implementation lives in `Translate/`. CSV parsing, split/reconstruction, validation,
retry, and quality-review mechanics are owned by the sibling `FanslationStudio.LlmKit` repository.
The Mono plugin consumes the generated mod data and handles game-specific runtime injection.
