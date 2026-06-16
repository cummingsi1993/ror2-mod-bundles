using RoR2;
using RoR2.Artifacts;
using SupplyChain.Artifacts;
using SupplyChain.Items;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace SupplyChain
{
    // Single coordinator for everything that happens when a gold-cost chest opens:
    //   1. Artifact of Diversification rerolls hoarded drops
    //   2. Loaded Dice (chance, tier-down) / Recall Notice (guaranteed, void tier-down)
    //      bonus rolls — each on its own per-stage cap
    //   3. Purchase Order command-style choice of the chest's tier (own per-stage cap)
    //   4. Bulk Order bonus white
    // One ItemDrop hook keeps the ordering explicit instead of relying on hook
    // registration order across features.
    internal static class ChestHooks
    {
        private class ChestOpener : MonoBehaviour
        {
            internal CharacterMaster master;
        }

        // Loaded Dice and Recall Notice share this counter — they can never coexist (Recall
        // corrupts all Loaded Dice), so only one path ever increments it in a given run.
        private static int bonusDropsThisStage;
        // Purchase Order command choices are counted separately.
        private static int commandChoicesThisStage;

        internal static void Init()
        {
            On.RoR2.PurchaseInteraction.OnInteractionBegin += TrackOpener;
            On.RoR2.ChestBehavior.ItemDrop += OnChestItemDrop;
            Stage.onServerStageBegin += _ =>
            {
                bonusDropsThisStage = 0;
                commandChoicesThisStage = 0;
            };
        }

        private static void TrackOpener(
            On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig,
            PurchaseInteraction self, Interactor activator)
        {
            // attach before orig: opening may fire ItemDrop during the purchase flow.
            // Money costs only — lunar pods and blood shrines are out of scope.
            if (NetworkServer.active && self.costType == CostTypeIndex.Money && activator)
            {
                var chest = self.GetComponent<ChestBehavior>();
                if (chest)
                {
                    var body = activator.GetComponent<CharacterBody>();
                    var master = body ? body.master : null;
                    if (master)
                    {
                        var opener = chest.GetComponent<ChestOpener>();
                        if (!opener)
                        {
                            opener = chest.gameObject.AddComponent<ChestOpener>();
                        }
                        opener.master = master;
                    }
                }
            }
            orig(self, activator);
        }

        private static void OnChestItemDrop(On.RoR2.ChestBehavior.orig_ItemDrop orig, ChestBehavior self)
        {
            if (!NetworkServer.active || !Run.instance)
            {
                orig(self);
                return;
            }
            var opener = self.GetComponent<ChestOpener>();
            var master = opener ? opener.master : null;
            var inventory = master ? master.inventory : null;
            if (!inventory)
            {
                orig(self);
                return;
            }

            var pickupDef = PickupCatalog.GetPickupDef(self.dropPickup);
            if (pickupDef == null || pickupDef.itemIndex == ItemIndex.None)
            {
                orig(self);
                return;
            }

            // 1. Artifact of Diversification: hoarded drops reroll once
            if (ArtifactOfDiversification.Enabled
                && inventory.GetItemCount(pickupDef.itemIndex) >= ArtifactOfDiversification.RerollThreshold.Value)
            {
                self.RollItem();
                var rerolled = PickupCatalog.GetPickupDef(self.dropPickup);
                Log.Info($"Diversification reroll: {pickupDef.internalName} -> {rerolled?.internalName ?? "?"}");
                pickupDef = rerolled ?? pickupDef;
            }

            // 2. Loaded Dice / Recall Notice bonus roll. Recall (the void corruption) runs
            // on its own tight cap and drops a void item one tier DOWN — it is no longer a
            // strict upgrade over Loaded Dice (guaranteed + full-tier was the old problem).
            int recallStacks = inventory.GetItemCount(RecallNotice.Def);
            int diceStacks = inventory.GetItemCount(LoadedDice.Def);
            if (recallStacks > 0)
            {
                if (bonusDropsThisStage < RecallNotice.StageCapFor(recallStacks))
                {
                    var bonus = PickFromList(VoidTierDownListForTier(pickupDef.itemTier));
                    if (bonus != PickupIndex.none)
                    {
                        bonusDropsThisStage++;
                        SpawnBonus(self, bonus);
                    }
                }
            }
            else if (diceStacks > 0)
            {
                int stageCap = LoadedDice.StageCapBase.Value + (diceStacks - 1);
                if (bonusDropsThisStage < stageCap)
                {
                    float chance = HyperbolicChance(LoadedDice.BonusChanceBase.Value, LoadedDice.BonusChancePerStack.Value, LoadedDice.BonusChanceMax.Value, diceStacks);
                    if (Run.instance.treasureRng.nextNormalizedFloat < chance)
                    {
                        var bonus = PickFromList(TierDownListForTier(pickupDef.itemTier));
                        if (bonus != PickupIndex.none)
                        {
                            bonusDropsThisStage++;
                            SpawnBonus(self, bonus);
                        }
                    }
                }
            }

            // 3. Purchase Order: a Command-style choice of the chest's tier
            int purchaseStacks = inventory.GetItemCount(PurchaseOrder.Def);
            if (purchaseStacks > 0 && commandChoicesThisStage < PurchaseOrder.StageCapFor(purchaseStacks))
            {
                float chance = HyperbolicChance(PurchaseOrder.ChanceBase.Value, PurchaseOrder.ChancePerStack.Value, PurchaseOrder.ChanceMax.Value, purchaseStacks);
                if (Run.instance.treasureRng.nextNormalizedFloat < chance && SpawnCommandChoice(self, pickupDef.itemTier))
                {
                    commandChoicesThisStage++;
                }
            }

            // 4. Bulk Order: bonus white
            int bulkStacks = inventory.GetItemCount(BulkOrder.Def);
            if (bulkStacks > 0)
            {
                float chance = HyperbolicChance(BulkOrder.ExtraChanceBase.Value, BulkOrder.ExtraChancePerStack.Value, BulkOrder.ExtraChanceMax.Value, bulkStacks);
                if (Run.instance.treasureRng.nextNormalizedFloat < chance)
                {
                    var bonus = PickFromList(Run.instance.availableTier1DropList);
                    if (bonus != PickupIndex.none)
                    {
                        SpawnBonus(self, bonus);
                    }
                }
            }

            orig(self);
        }

        // base chance for one stack, approaching max hyperbolically with extra stacks
        internal static float HyperbolicChance(float baseChance, float perStack, float max, int stacks)
        {
            if (stacks <= 0)
            {
                return 0f;
            }
            return baseChance + (max - baseChance) * (1f - 1f / (1f + perStack * (stacks - 1)));
        }

        private static List<PickupIndex> TierDownListForTier(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Tier3: return Run.instance.availableTier2DropList;
                case ItemTier.Tier2: return Run.instance.availableTier1DropList;
                case ItemTier.Boss: return Run.instance.availableTier3DropList;
                default: return Run.instance.availableTier1DropList;
            }
        }

        // same-tier drop list, for Purchase Order's command options
        private static List<PickupIndex> TierListForTier(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Tier3: return Run.instance.availableTier3DropList;
                case ItemTier.Tier2: return Run.instance.availableTier2DropList;
                case ItemTier.Boss: return Run.instance.availableTier3DropList;
                default: return Run.instance.availableTier1DropList;
            }
        }

        // void list one tier below the chest's contents (Recall Notice)
        private static List<PickupIndex> VoidTierDownListForTier(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Tier3: return Run.instance.availableVoidTier2DropList;
                case ItemTier.Tier2: return Run.instance.availableVoidTier1DropList;
                case ItemTier.Boss: return Run.instance.availableVoidTier3DropList;
                default: return Run.instance.availableVoidTier1DropList; // tier1 has no lower void tier
            }
        }

        // random pick excluding this bundle's own items
        private static PickupIndex PickFromList(List<PickupIndex> list)
        {
            if (list == null || list.Count == 0)
            {
                return PickupIndex.none;
            }
            for (int attempt = 0; attempt < 8; attempt++)
            {
                var candidate = list[Run.instance.treasureRng.RangeInt(0, list.Count)];
                if (!SupplyChainPlugin.BundlePickups.Contains(candidate))
                {
                    return candidate;
                }
            }
            return PickupIndex.none;
        }

        private static void SpawnBonus(ChestBehavior chest, PickupIndex pickup)
        {
            var origin = chest.dropTransform ? chest.dropTransform : chest.gameObject.transform;
            PickupDropletController.CreatePickupDroplet(
                pickup,
                origin.position + Vector3.up * 1.5f,
                origin.forward * chest.dropForwardVelocityStrength + Vector3.up * chest.dropUpVelocityStrength);
        }

        // Spawn an Artifact-of-Command-style picker cube offering every item of the tier,
        // minus this bundle's own items. Reuses the vanilla command cube prefab so the
        // choice UI, networking, and pickup behaviour are identical to the artifact.
        private static bool SpawnCommandChoice(ChestBehavior chest, ItemTier tier)
        {
            var prefab = CommandArtifactManager.commandCubePrefab;
            if (!prefab)
            {
                Log.Warning("Command cube prefab unavailable; Purchase Order choice skipped.");
                return false;
            }
            var drops = TierListForTier(tier);
            if (drops == null || drops.Count == 0)
            {
                return false;
            }
            var filtered = new List<PickupIndex>(drops.Count);
            foreach (var pickup in drops)
            {
                if (!SupplyChainPlugin.BundlePickups.Contains(pickup))
                {
                    filtered.Add(pickup);
                }
            }
            if (filtered.Count == 0)
            {
                return false;
            }
            var options = PickupPickerController.GenerateOptionsFromArray(filtered.ToArray());
            var origin = chest.dropTransform ? chest.dropTransform : chest.gameObject.transform;
            var cube = Object.Instantiate(prefab, origin.position + Vector3.up * 1.5f, Quaternion.identity);
            var picker = cube.GetComponent<PickupPickerController>();
            if (picker)
            {
                // set options before the spawn so they serialize in the initial state
                picker.SetOptionsServer(options);
            }
            NetworkServer.Spawn(cube);
            Log.Info($"Purchase Order: offered a {tier} command choice ({filtered.Count} options).");
            return true;
        }
    }
}
