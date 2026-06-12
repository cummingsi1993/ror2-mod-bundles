# SupplyChain

Item-count manipulation bundle. Designed against two hard rules learned from the Fuzzy Dice failure: **generated items never include their generators**, and **generation is capped per stage, never per interaction**. The balance yardstick is Sale Star: ~1 extra item/stage ≈ one green slot.

| Item | Tier | Effect | Config section |
|---|---|---|---|
| Bulk Order | White | Gold chests: 7% (cap 30%) chance of an extra common item | `BulkOrder` |
| Loaded Dice | Green | Gold chests: 15% (cap 40%) chance of a bonus item one tier below contents; max 2 (+1/stack) bonus drops/stage | `LoadedDice` |
| Standing Order | Red | Each stage: +1 stack of your lowest-count item (+1 item/stack) | `StandingOrder` |
| Force Multiplier | Red | All other standard items count as 10% (+10%/stack) more stacks, rounded down | `ForceMultiplier` |
| Pyramid Scheme | Lunar | On pickup: +1 stack of everything owned; thereafter 20%/stack of gold income paid upline | `PyramidScheme` |
| Recall Notice | Void (corrupts Loaded Dice) | Bonus drops guaranteed but they're void items of the chest's tier | — |
| Artifact of Diversification | Artifact | Chests reroll once when dropping an item the opener owns 5+ of | `ArtifactOfDiversification` |

## Implementation notes

- All chest behavior (artifact reroll → dice/recall bonus → bulk order extra) runs in **one** `ChestBehavior.ItemDrop` hook (`ChestHooks.cs`) so ordering is explicit. The opener is tracked via a component attached in `PurchaseInteraction.OnInteractionBegin` (money costs only).
- The artifact reroll uses the chest's own public `RollItem()`, so rerolls respect the chest's real drop table.
- Force Multiplier hooks `Inventory.GetItemCountEffective(ItemIndex)` — verified against game IL as the single funnel for both `GetItemCount` overloads — via a manual MonoMod `Hook` (the compile-time MMHOOK package predates the method). `GetItemCountPermanent` (printers/scrappers/cache rebuild) is untouched: no phantom consumption, no feedback loop.
- `InventoryCompat` resolves `GetItemCountPermanent` by reflection because the GameLibs reference assemblies (1.3.9) lag the installed game (1.4.x).
- Pyramid Scheme detects pickup via `CharacterMaster.OnInventoryChanged` with a re-entrancy guard, and grants based on permanent counts so Force Multiplier can't inflate the payout.

## Testing

**F6** drops one of each item (host only; `Debug > SpawnPackKey`). Quick checklist: open chests with Bulk Order/Loaded Dice (watch the per-stage cap), stage transition with Standing Order, stack 10+ of a white with Force Multiplier and watch the behavior jump, grab Pyramid Scheme with a full build, corrupt dice in the Void Fields, and enable Diversification with 5+ of something common.

Build: `dotnet build SupplyChain/SupplyChain.csproj` (auto-deploys to the mod_testing profile). Models: `tools/make_model_refs.ps1` → `tools/generate_models.sh` → `tools/blender_clean_export.py`.
