using BepInEx.Configuration;
using R2API;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace SupplyChain.Items
{
    // Red: at the start of each stage, gain +1 stack of your lowest-count items
    // (one item per stack of this).
    internal static class StandingOrder
    {
        internal static ItemDef Def;
        internal static ConfigEntry<int> ItemsPerStack;

        internal static void Init(ConfigFile config)
        {
            ItemsPerStack = config.Bind("StandingOrder", "ItemsPerStack", 1,
                "Number of lowest-count items topped up by +1 stack each stage, per stack of this item.");

            Def = Assets.CreateItemDef(
                "StandingOrder", "STANDING_ORDER", ItemTier.Tier3,
                Assets.LoadSprite("SupplyChain.icon_standing_order.rgba", 128),
                Assets.CreatePickupModel("PickupStandingOrder", "SupplyChain.models.standing_order.obj", "SupplyChain.models.standing_order.rgba", 512, 0.7f),
                new[] { ItemTag.Utility, ItemTag.AIBlacklist });
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            LanguageAPI.Add("STANDING_ORDER_NAME", "Standing Order");
            LanguageAPI.Add("STANDING_ORDER_PICKUP", "Automatically restock your scarcest items each stage.");
            LanguageAPI.Add("STANDING_ORDER_DESC",
                $"At the start of each stage, gain <style=cIsUtility>+1 stack</style> of your " +
                $"<style=cIsUtility>{ItemsPerStack.Value} <style=cStack>(+{ItemsPerStack.Value} per stack)</style> lowest-count item(s)</style>. " +
                "Lunar, void, and hidden items are not restocked.");
            LanguageAPI.Add("STANDING_ORDER_LORE",
                "PROCUREMENT DIRECTIVE 7-C: Inventory levels shall be maintained. Shortfalls shall be remedied. Questions about where the materiel comes from shall be directed to the office of not asking that.");

            Stage.onServerStageBegin += OnStageBegin;
        }

        private static void OnStageBegin(Stage stage)
        {
            if (!NetworkServer.active)
            {
                return;
            }
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                var master = pcmc.master;
                var inventory = master ? master.inventory : null;
                if (!inventory)
                {
                    continue;
                }
                int stacks = inventory.GetItemCount(Def);
                if (stacks <= 0)
                {
                    continue;
                }
                int restockCount = stacks * ItemsPerStack.Value;

                // permanent counts: temporary/channeled stacks shouldn't skew the ranking
                var owned = new List<(ItemIndex index, int count)>();
                foreach (var index in SupplyChainPlugin.AmplifiableItems)
                {
                    int count = InventoryCompat.GetPermanentCount(inventory, index);
                    if (count > 0)
                    {
                        owned.Add((index, count));
                    }
                }
                foreach (var entry in owned.OrderBy(e => e.count).ThenBy(e => (int)e.index).Take(restockCount))
                {
                    inventory.GiveItem(entry.index, 1);
                    var def = ItemCatalog.GetItemDef(entry.index);
                    Log.Info($"Standing Order restocked {def?.name ?? "?"} for {Util.GetBestMasterName(master)}");
                }
            }
        }
    }
}
