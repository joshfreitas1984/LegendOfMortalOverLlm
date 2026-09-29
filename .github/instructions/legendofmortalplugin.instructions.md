---
applyTo: "LegendOfMortalPlugin/**"
---

# Legend of Mortal Plugin

This is a Mono Unity BepInEx plugin compiled against the game's deployed Managed assemblies.

- Verify namespaces, signatures, and Harmony targets against the installed game assemblies.
- Preserve the existing string-table dump/injection behavior and fail safely when runtime lookups
  or files are unavailable.
- Keep plugin code focused on dumping, Harmony patches, runtime injection, and game integration;
  translation workflow/configuration belongs in `Translate/`.
- Preserve the plugin GUID/product metadata, Mono `HintPath` references, and post-build deployment
  behavior. Do not run deployment or launch the game during static validation.
- Keep durable investigations and feature references under the root `docs/` taxonomy.
