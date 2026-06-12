using BepInEx.Configuration;
using MonoMod.RuntimeDetour;
using R2API;
using RoR2;
using System.Reflection;
using UnityEngine;

namespace SupplyChain.Items
{
    // Red: all other standard-tier items behave as if you had more stacks (rounded down).
    //
    // Implementation: every standard item-count read in the game funnels through
    // Inventory.GetItemCountEffective(ItemIndex) — both GetItemCount overloads and the
    // ItemDef overload of GetItemCountEffective delegate to it (verified against the
    // game IL). Hooking that single method amplifies every gameplay read. The separate
    // GetItemCountPermanent path (printers, scrappers, the effective-stack cache
    // rebuild) is untouched, so machines never consume phantom stacks and there is no
    // feedback loop into the cache.
    internal static class ForceMultiplier
    {
        internal static ItemDef Def;
        internal static ConfigEntry<float> BoostPerStack;

        internal static void Init(ConfigFile config)
        {
            BoostPerStack = config.Bind("ForceMultiplier", "BoostPerStack", 0.10f,
                "Fractional bonus stacks applied to all other standard-tier items, per stack. Rounded down per item.");

            Def = Assets.CreateItemDef(
                "ForceMultiplier", "FORCE_MULTIPLIER", ItemTier.Tier3,
                Assets.LoadSprite("SupplyChain.icon_force_multiplier.rgba", 128),
                Assets.CreatePickupModel("PickupForceMultiplier", "SupplyChain.models.force_multiplier.obj", "SupplyChain.models.force_multiplier.rgba", 512, 0.7f),
                new[] { ItemTag.Utility, ItemTag.AIBlacklist });
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            int pct = Mathf.RoundToInt(BoostPerStack.Value * 100f);
            LanguageAPI.Add("FORCE_MULTIPLIER_NAME", "Force Multiplier");
            LanguageAPI.Add("FORCE_MULTIPLIER_PICKUP", "Deep stacks run deeper.");
            LanguageAPI.Add("FORCE_MULTIPLIER_DESC",
                $"All other standard items behave as if you had <style=cIsUtility>{pct}% <style=cStack>(+{pct}% per stack)</style> more stacks</style>, " +
                "rounded <style=cStack>down</style> per item. Lunar and void items are unaffected.");
            LanguageAPI.Add("FORCE_MULTIPLIER_LORE",
                "\"The audit found that the quartermaster had been reporting each pallet as one-point-one pallets. The audit also found that, somehow, the math held up under fire.\"");

            // The compile-time MMHOOK package predates GetItemCountEffective, so hook it
            // manually via MonoMod — same chaining semantics as an On. hook.
            var target = typeof(Inventory).GetMethod(
                "GetItemCountEffective",
                BindingFlags.Public | BindingFlags.Instance,
                null, new[] { typeof(ItemIndex) }, null);
            _ = new Hook(target, typeof(ForceMultiplier).GetMethod(nameof(AmplifyCount), BindingFlags.NonPublic | BindingFlags.Static));
        }

        private delegate int orig_GetItemCountEffective(Inventory self, ItemIndex itemIndex);

        private static int AmplifyCount(orig_GetItemCountEffective orig, Inventory self, ItemIndex itemIndex)
        {
            int count = orig(self, itemIndex);
            if (count <= 0 || Def == null || Def.itemIndex == ItemIndex.None
                || !SupplyChainPlugin.AmplifiableItems.Contains(itemIndex))
            {
                return count;
            }
            int boosters = orig(self, Def.itemIndex);
            if (boosters <= 0)
            {
                return count;
            }
            return count + (int)(count * BoostPerStack.Value * boosters);
        }
    }
}
