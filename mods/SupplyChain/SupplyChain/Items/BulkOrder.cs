using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;

namespace SupplyChain.Items
{
    // White: gold-cost chests have a chance to contain an extra white item.
    // Mechanics live in ChestHooks; this file is the definition + config.
    internal static class BulkOrder
    {
        internal static ItemDef Def;
        internal static ConfigEntry<float> ExtraChanceBase;
        internal static ConfigEntry<float> ExtraChancePerStack;
        internal static ConfigEntry<float> ExtraChanceMax;

        internal static void Init(ConfigFile config)
        {
            ExtraChanceBase = config.Bind("BulkOrder", "ExtraChanceBase", 0.07f,
                "Chance for a gold chest to contain an extra white item, with one stack.");
            ExtraChancePerStack = config.Bind("BulkOrder", "ExtraChancePerStack", 0.07f,
                "Hyperbolic growth rate toward the max chance per additional stack.");
            ExtraChanceMax = config.Bind("BulkOrder", "ExtraChanceMax", 0.30f,
                "Maximum chance.");

            Def = Assets.CreateItemDef(
                "BulkOrder", "BULK_ORDER", ItemTier.Tier1,
                Assets.LoadSprite("SupplyChain.icon_bulk_order.rgba", 128),
                Assets.CreatePickupModel("PickupBulkOrder", "SupplyChain.models.bulk_order.obj", "SupplyChain.models.bulk_order.rgba", 512, 0.6f),
                new[] { ItemTag.Utility, ItemTag.AIBlacklist });
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            int basePct = Mathf.RoundToInt(ExtraChanceBase.Value * 100f);
            int maxPct = Mathf.RoundToInt(ExtraChanceMax.Value * 100f);
            LanguageAPI.Add("BULK_ORDER_NAME", "Bulk Order");
            LanguageAPI.Add("BULK_ORDER_PICKUP", "Chests sometimes ship with a free white item.");
            LanguageAPI.Add("BULK_ORDER_DESC",
                $"Gold-cost chests have a <style=cIsUtility>{basePct}% <style=cStack>(up to {maxPct}% with more stacks)</style></style> chance " +
                $"to contain an additional <style=cIsUtility>common item</style>.");
            LanguageAPI.Add("BULK_ORDER_LORE",
                "Buy nine crates, the tenth crate ships free. Contents of tenth crate not guaranteed, inspected, or in some cases identified.");
        }
    }
}
