# DefenseBudget

A Risk of Rain 2 mod adding a pack of **income-themed items** built around one economy: earn, owe, insure, and bleed gold.

| Item | Tier | Effect |
|---|---|---|
| **Defense Budget** | Lunar | Income tax + a line of credit with interest; default and pay in blood (details below) |
| **Savings Bond** | White | Every 10s, earn 2% (+2%/stack) interest on held gold |
| **Accounts Receivable** | Green | Damaging an enemy marks it; marked enemies pay you +10% (+10%/stack) of their bounty when killed by anyone |
| **Golden Parachute** | Red | Lethal damage instead costs a difficulty-scaled severance fee (drawing on Defense Budget credit if short) and leaves you at 15% HP; 45s cooldown (-25%/stack) |
| **Final Notice** | Void (corrupts Roll of Pennies) | 30% (+10%/stack, max 70%) of incoming damage is billed to your gold at a difficulty-scaled rate instead of your health |
| **Artifact of Communism** | Artifact | All gold is one shared pool: everyone earns into it and spends from it. Total income is multiplied by playerCount − 0.25×(playerCount−1) — a bit less than full per-player income. Solo unaffected |

All numbers are config entries (`Isaac_Cummings.DefenseBudget.cfg`), one section per item.

3D pickup models are generated locally: reference art drawn programmatically (`tools/make_model_refs.ps1`) → TRELLIS.2 image-to-3D (Docker, `tools/generate_models.sh`) → headless Blender cleanup/decimation/export (`tools/blender_clean_export.py`) → embedded OBJ + raw RGBA textures loaded at runtime (`Assets.LoadObjMesh`). Missing model resources fall back to the vanilla mystery model automatically.

## Defense Budget (Lunar) mechanics

With at least one stack (per player, server-side):

| Mechanic | Effect | Config key |
|---|---|---|
| Income tax | Gold income reduced 25% per stack, multiplicative (75% / 56% / 42%). Holder only | `IncomeReductionPerStack` (0.25) |
| Inflation (off by default) | All gold purchase costs x(1 + 2 x total stacks across players). Off by default because chest prices are shared — in multiplayer it inflates costs for everyone | `CostIncreasePerStack` (0.0) |
| Credit line | Buy with gold you don't have; shortfall becomes deficit. Limit = difficulty-scaled cost of 75 gold per stack (scales over time like chest prices) | `BaseDebtLimit` (75) |
| Interest | Deficit grows 1%/sec (min 1 gold/sec) while in debt | `InterestRatePerSecond` (0.01) |
| Repayment | All gold income pays the deficit before your wallet | — |
| Deficit spending | +15% damage per stack while in deficit | `DeficitSpendingBonusPerStack` (0.15) |
| Default | Deficit over the limit: lose 2% max HP/sec (bypasses armor/block) until repaid below the limit | `DefaultDamagePerSecond` (0.02) |

Status is shown with a `$` buff icon: yellow = in deficit, red = defaulted (taking damage). A chat message announces defaults.

### Balance notes / design decisions

- **Income tax instead of inflation.** The original pitch (x3 chest prices) had two problems in play: it felt too punishing, and chest prices are a shared `Networkcost` on the interactable — there is no per-player pricing in vanilla — so one player's lunar curse taxed the whole lobby. Income reduction flows through `CharacterMaster.GiveMoney`, which is per-player, so the downside stays personal. The inflation code remains available behind `CostIncreasePerStack` for mixing both.
- **Roll of Pennies can't break it.** Default damage has no attacker, and any money gained (Roll of Pennies) or lost (Brittle Crown) as a reaction to a default-damage tick is reverted. Combined with income-repays-debt-first, Pennies can't turn the default state into a money printer.
- **Debt outlives the item.** Cleansing/printing away your last stack drops your credit limit to 0 — if you still owe, you're instantly in default until income pays it off.

## Project layout

- `DefenseBudget/` — the BepInEx plugin (C#, netstandard2.1). Mirrors the PowerMultiply project setup (same package versions, `libs/`, and post-build deploy to the Thunderstore Mod Manager `mod_testing` profile).
- `DefenseBudget/assets/` — icon textures as raw RGBA32 (loaded with `Texture2D.LoadRawTextureData`, no asset bundle needed).
- `tools/make_icons.ps1` — regenerates the icons (System.Drawing).
- `tools/package.ps1` — builds and produces `Thunderstore/DefenseBudget.zip` for upload.
- `Thunderstore/` — manifest, readme, icon for publishing.

## Build

```powershell
dotnet build DefenseBudget/DefenseBudget.csproj
```

Building auto-copies the DLL into the `mod_testing` profile. Package for Thunderstore with `powershell -File tools/package.ps1`.

## Testing

Press **F3** in a run (host only) to drop a Defense Budget at your feet (F2 is taken by PowerMultiply's spawner). Configurable/disable-able via `Debug > SpawnItemKey` in the config file.

## Implementation notes

- Costs are inflated by tracking every gold `PurchaseInteraction` and rescaling `Networkcost` by the ratio of old/new multiplier (so Shrine of Chance's own cost growth is preserved). Applied ~0.4s after spawn so multishop controllers finish assigning prices first.
- Credit purchases work by topping the wallet up to the exact price just before vanilla payment runs (avoids `uint` underflow on `CharacterMaster.money`), recording the shortfall in a `DebtTracker` component on the master (persists across stages, cleared between runs).
- Affordability is extended via `PurchaseInteraction.CanBeAffordedByInteractor`; repayment via `CharacterMaster.GiveMoney`; the damage bonus via `HealthComponent.TakeDamage`; interest/default ticks in the plugin's `FixedUpdate`. Everything is server-authoritative.
