using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;

namespace SupplyChain.Items
{
    // Green: gold-cost chests occasionally also produce a Command-style pick-your-item
    // choice of the chest's tier (an Artifact of Command essence), letting you hand-pick
    // the item instead of taking what the dice give you. Chance-gated and per-stage
    // capped so it stays "occasional," and the offered options exclude this bundle's own
    // items (Fuzzy Dice doctrine). Mechanics live in ChestHooks; this file is the
    // definition + config.
    internal static class PurchaseOrder
    {
        internal static ItemDef Def;
        internal static ConfigEntry<float> ChanceBase;
        internal static ConfigEntry<float> ChancePerStack;
        internal static ConfigEntry<float> ChanceMax;
        internal static ConfigEntry<int> StageCapBase;
        internal static ConfigEntry<int> StacksPerExtraCap;

        internal static void Init(ConfigFile config)
        {
            ChanceBase = config.Bind("PurchaseOrder", "ChanceBase", 0.10f,
                "Chance for a gold chest to also offer a Command-style choice of its tier, with one stack.");
            ChancePerStack = config.Bind("PurchaseOrder", "ChancePerStack", 0.10f,
                "Hyperbolic growth rate toward the max chance per additional stack.");
            ChanceMax = config.Bind("PurchaseOrder", "ChanceMax", 0.35f,
                "Maximum chance.");
            StageCapBase = config.Bind("PurchaseOrder", "StageCapBase", 1,
                "Maximum command choices offered per stage with one stack.");
            StacksPerExtraCap = config.Bind("PurchaseOrder", "StacksPerExtraCap", 2,
                "Additional stacks required to raise the per-stage cap by 1.");

            Def = Assets.CreateItemDef(
                "PurchaseOrder", "PURCHASE_ORDER", ItemTier.Tier2,
                Assets.LoadSprite("SupplyChain.icon_purchase_order.rgba", 128),
                Assets.CreatePickupModel("PickupPurchaseOrder", "SupplyChain.models.purchase_order.obj", "SupplyChain.models.purchase_order.rgba", 512, 0.55f),
                new[] { ItemTag.Utility, ItemTag.AIBlacklist });
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            int basePct = Mathf.RoundToInt(ChanceBase.Value * 100f);
            int maxPct = Mathf.RoundToInt(ChanceMax.Value * 100f);
            LanguageAPI.Add("PURCHASE_ORDER_NAME", "Purchase Order");
            LanguageAPI.Add("PURCHASE_ORDER_PICKUP", "Chests sometimes let you order exactly what you want.");
            LanguageAPI.Add("PURCHASE_ORDER_DESC",
                $"Gold-cost chests have a <style=cIsUtility>{basePct}% <style=cStack>(up to {maxPct}% with more stacks)</style></style> chance to also offer a " +
                $"<style=cIsUtility>choice of any item</style> of the chest's tier, like an <style=cIsUtility>Artifact of Command</style> essence. " +
                $"At most <style=cIsUtility>{StageCapBase.Value} <style=cStack>(+1 per {StacksPerExtraCap.Value} stacks)</style></style> per stage.");
            LanguageAPI.Add("PURCHASE_ORDER_LORE",
                "Most procurement is a matter of taking what the warehouse sends and filing a complaint later. A purchase order inverts the relationship: you specify the part number, the quantity, and the delivery date, and the void obliges. The void has never once disputed an invoice, which the accounting department finds more unsettling than reassuring.");
        }

        // Per-stage cap for command choices, scaling sub-linearly.
        internal static int StageCapFor(int stacks)
        {
            if (stacks <= 0)
            {
                return 0;
            }
            return StageCapBase.Value + (stacks - 1) / Mathf.Max(1, StacksPerExtraCap.Value);
        }
    }
}
