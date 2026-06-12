using RoR2;
using SupplyChain.Artifacts;
using SupplyChain.Items;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace SupplyChain
{
    // Single coordinator for everything that happens when a gold-cost chest opens:
    //   1. Artifact of Diversification rerolls hoarded drops
    //   2. Loaded Dice / Recall Notice bonus rolls (per-stage capped)
    //   3. Bulk Order bonus whites
    // One ItemDrop hook keeps the ordering explicit instead of relying on hook
    // registration order across four features.
    internal static class ChestHooks
    {
        private class ChestOpener : MonoBehaviour
        {
            internal CharacterMaster master;
        }

        private static int bonusDropsThisStage;

        internal static void Init()
        {
            On.RoR2.PurchaseInteraction.OnInteractionBegin += TrackOpener;
            On.RoR2.ChestBehavior.ItemDrop += OnChestItemDrop;
            Stage.onServerStageBegin += _ => bonusDropsThisStage = 0;
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

            // 2. Loaded Dice / Recall Notice bonus roll
            int diceStacks = inventory.GetItemCount(LoadedDice.Def);
            int recallStacks = inventory.GetItemCount(RecallNotice.Def);
            if (diceStacks > 0 || recallStacks > 0)
            {
                int totalStacks = diceStacks + recallStacks;
                int stageCap = LoadedDice.StageCapBase.Value + (totalStacks - 1);
                if (bonusDropsThisStage < stageCap)
                {
                    bool guaranteed = recallStacks > 0;
                    float chance = HyperbolicChance(LoadedDice.BonusChanceBase.Value, LoadedDice.BonusChancePerStack.Value, LoadedDice.BonusChanceMax.Value, totalStacks);
                    if (guaranteed || Run.instance.treasureRng.nextNormalizedFloat < chance)
                    {
                        var list = guaranteed
                            ? VoidListForTier(pickupDef.itemTier)
                            : TierDownListForTier(pickupDef.itemTier);
                        var bonus = PickFromList(list);
                        if (bonus != PickupIndex.none)
                        {
                            bonusDropsThisStage++;
                            SpawnBonus(self, bonus);
                        }
                    }
                }
            }

            // 3. Bulk Order: bonus white
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

        private static List<PickupIndex> VoidListForTier(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Tier3: return Run.instance.availableVoidTier3DropList;
                case ItemTier.Tier2: return Run.instance.availableVoidTier2DropList;
                case ItemTier.Boss: return Run.instance.availableVoidBossDropList;
                default: return Run.instance.availableVoidTier1DropList;
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
    }
}
