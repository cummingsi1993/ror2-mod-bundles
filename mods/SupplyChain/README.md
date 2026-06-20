# SupplyChain

Item-count manipulation bundle. Designed against two hard rules learned from the Fuzzy Dice failure: **generated items never include their generators**, and **generation is capped per stage, never per interaction**. The balance yardstick is Sale Star: ~1 extra item/stage ≈ one green slot.

| Item | Tier | Effect | Config section |
|---|---|---|---|
| Bulk Order | White | Gold chests: 7% (cap 30%) chance of an extra common item | `BulkOrder` |
| Loaded Dice | Green | Gold chests: 15% (cap 40%) chance of a bonus item one tier below contents; max 2 (+1/stack) bonus drops/stage | `LoadedDice` |
| Purchase Order | Green | Gold chests: 10% (cap 35%) chance to also offer a Command-style choice of the chest's tier; max 1 (+1 per 2 stacks)/stage | `PurchaseOrder` |
| Standing Order | Red | Each stage: +1 stack of your lowest-count item (+1 item/stack) | `StandingOrder` |
| Force Multiplier | Red | All other standard items count as 10% (+10%/stack) more stacks, rounded down | `ForceMultiplier` |
| Pyramid Scheme | Lunar | On pickup: +1 stack of everything owned; thereafter 20%/stack of gold income paid upline | `PyramidScheme` |
| Dropshipping | Lunar | Can't open gold chests; gain a copy of every (player count / 2) items teammates collect. Useless solo | `Dropshipping` |
| Recall Notice | Void (corrupts Loaded Dice) | Guaranteed void item **one tier below** the chest; own tight cap of 1 (+1 per 2 stacks, max 3)/stage | `RecallNotice` |
| Artifact of Diversification | Artifact | Chests reroll once when dropping an item the opener owns 5+ of | `ArtifactOfDiversification` |

## Implementation notes

- All chest behavior (artifact reroll → dice/recall bonus → purchase-order choice → bulk order extra) runs in **one** `ChestBehavior.ItemDrop` hook (`ChestHooks.cs`) so ordering is explicit. The opener is tracked via a component attached in `PurchaseInteraction.OnInteractionBegin` (money costs only).
- The artifact reroll uses the chest's own public `RollItem()`, so rerolls respect the chest's real drop table.
- **Recall Notice** has its own per-stage cap (`RecallNotice.StageCapFor`, base 1 + 1 per 2 stacks, max 3) and drops a void item **one tier below** the chest. The 1.0.0 release reused Loaded Dice's chance-and-tier-down cap verbatim for Recall's guaranteed full-tier void drops, which yielded a void item on nearly every chest once a Loaded Dice hoard (all corrupting at once) was large; the separate cap + tier-down fixes that.
- **Purchase Order** spawns the vanilla command cube (`CommandArtifactManager.commandCubePrefab`) with options built from `PickupPickerController.GenerateOptionsFromArray` over the chest tier's drop list minus bundle pickups, then `SetOptionsServer` before `NetworkServer.Spawn`. No dependency on the Artifact of Command being enabled. Both command members are public at runtime (audited).
- **Dropshipping** hooks `GenericPickupController.AttemptGrant` (private at runtime — hooked, not called) to count items teammates physically collect. Copies are minted via `Inventory.GiveItem` directly, never via a droplet, so they can't re-enter `AttemptGrant` (no cascade between multiple holders); a `granting` guard backs this up. The per-holder tally lives on a `DropshipLedger` component; threshold is `max(1, playerCount / divisor)`, so it self-disables solo (no teammates increment it). The chest block is two-layer: `PurchaseInteraction.GetInteractability` greys the prompt (client UI) and `OnInteractionBegin` hard-skips the open server-side. Both in `ChestHooks`.
- Force Multiplier hooks `Inventory.GetItemCountEffective(ItemIndex)` — verified against game IL as the single funnel for both `GetItemCount` overloads — via a manual MonoMod `Hook` (the compile-time MMHOOK package predates the method). `GetItemCountPermanent` (printers/scrappers/cache rebuild) is untouched: no phantom consumption, no feedback loop.
- `InventoryCompat` resolves `GetItemCountPermanent` by reflection because the GameLibs reference assemblies (1.3.9) lag the installed game (1.4.x).
- Pyramid Scheme detects pickup via `CharacterMaster.OnInventoryChanged` with a re-entrancy guard, and grants based on permanent counts so Force Multiplier can't inflate the payout.

## Testing

**F6** drops one of each item (host only; `Debug > SpawnPackKey`). Quick checklist: open chests with Bulk Order/Loaded Dice (watch the per-stage cap), open chests with Purchase Order and confirm a command-choice cube appears and lets you pick (and that the cap holds), stage transition with Standing Order, stack 10+ of a white with Force Multiplier and watch the behavior jump, grab Pyramid Scheme with a full build, corrupt dice in the Void Fields and confirm Recall now drops a tier-down void item capped at ~1-3/stage, and enable Diversification with 5+ of something common. **Dropshipping needs a second player** (it's intentionally inert solo): with two clients, confirm the holder's chests are greyed out / won't open, and that the holder gets a copy after the teammate collects items (1 per item at 2 players).

Build: `dotnet build SupplyChain/SupplyChain.csproj` (auto-deploys to the mod_testing profile). Models: `tools/make_model_refs.ps1` → `tools/generate_models.sh` → `tools/blender_clean_export.py`.
