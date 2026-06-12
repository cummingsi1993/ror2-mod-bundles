# RoR2 Mod Bundles

A monorepo of themed Risk of Rain 2 mod bundles, published independently to Thunderstore under the **Isaac** team. The long-term goal is a friend-group modpack where each bundle is a self-contained theme you can pick and choose from.

| Bundle | Theme | Thunderstore |
|---|---|---|
| [DefenseBudget](mods/DefenseBudget/) | Income & debt: interest, invoices, severance fees, a void debt collector, a lunar line of credit, and the Artifact of Communism | `Isaac-DefenseBudget` |
| [SupplyChain](mods/SupplyChain/) | Item-count manipulation: bonus chest drops, automatic restocks, stack amplification, a pyramid scheme, and the Artifact of Diversification | `Isaac-SupplyChain` |

## Repo layout

```
mods/<Bundle>/
├── <Bundle>/          # BepInEx plugin project (csproj named after the bundle)
├── Thunderstore/      # manifest.json, README.md, icon.png, publish.json
├── models/            # 3D pipeline working files (refs/, raw/ [gitignored], clean/ previews)
├── tools/             # bundle-specific scripts (icons, model pipeline, packaging, audits)
└── README.md          # bundle mechanics & design notes
```

## Publishing

Publishing is automated by `.github/workflows/publish.yml`:

1. Work in a branch; merge to `main`.
2. To release, bump `version_number` in `mods/<Bundle>/Thunderstore/manifest.json` (keep `PluginVersion` in the plugin in sync) and merge.
3. The workflow compares each bundle's manifest version against Thunderstore and publishes only bundles that are ahead — no version bump, no publish.

Per-bundle publish settings (team namespace, communities, categories) live in `Thunderstore/publish.json`. **All bundles carry the `ai-generated` category, required by Thunderstore's TOS for AI-assisted mods.**

Setup (one-time): add a repository secret `TCLI_AUTH_TOKEN` containing a Thunderstore service-account token (thunderstore.io → Teams → Isaac → Service Accounts).

Manual fallback: `mods/<Bundle>/tools/publish_thunderstore.ps1 -Token <token>`.

## Adding a new bundle

1. Copy the `mods/DefenseBudget` structure; the csproj, plugin dll, and Thunderstore package name must all match the bundle directory name.
2. Each bundle builds against the shared root `nuget.config` (BepInEx feed). The publicized-assembly audit (`tools/audit_members.ps1`) and the image→3D model pipeline (`tools/make_model_refs.ps1` → `generate_models.sh` → `blender_clean_export.py`) are per-bundle copies for now — see `mods/DefenseBudget/tools/`.
3. Create `Thunderstore/publish.json` with namespace/communities/categories (include `ai-generated`).
4. Merge to main with a `1.0.0` manifest — the workflow publishes anything Thunderstore doesn't have yet.
