using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using UnityEngine;

namespace DefenseBudget.Items
{
    // White: every N seconds, earn interest on held gold.
    internal static class SavingsBond
    {
        internal static ItemDef Def;
        internal static ConfigEntry<float> InterestRatePerStack;
        internal static ConfigEntry<float> InterestInterval;
        private static float timer;

        internal static void Init(ConfigFile config)
        {
            InterestRatePerStack = config.Bind("SavingsBond", "InterestRatePerStack", 0.02f,
                "Interest earned on held gold per stack, each interval.");
            InterestInterval = config.Bind("SavingsBond", "InterestInterval", 10f,
                "Seconds between interest payments.");

            Def = Assets.CreateItemDef(
                "SavingsBond", "SAVINGS_BOND", ItemTier.Tier1,
                Assets.LoadSprite("DefenseBudget.icon_savings_bond.rgba", 128),
                Assets.CreatePickupModel("PickupSavingsBond", "DefenseBudget.models.savings_bond.obj", "DefenseBudget.models.savings_bond.rgba", 512, 0.6f),
                new[] { ItemTag.Utility });
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            int ratePct = Mathf.RoundToInt(InterestRatePerStack.Value * 100f);
            LanguageAPI.Add("SAVINGS_BOND_NAME", "Savings Bond");
            LanguageAPI.Add("SAVINGS_BOND_PICKUP", "Earn interest on your held gold.");
            LanguageAPI.Add("SAVINGS_BOND_DESC",
                $"Every {InterestInterval.Value:0} seconds, earn <style=cIsUtility>{ratePct}% <style=cStack>(+{ratePct}% per stack)</style> interest</style> on your held gold.");
            LanguageAPI.Add("SAVINGS_BOND_LORE",
                "\"Guaranteed 2% return, backed by the full faith and credit of whatever government still exists when it matures.\"\n\n- Prospectus fragment, recovered from contact light wreckage");
        }

        // Called from the plugin's FixedUpdate, server-side with an active run.
        internal static void FixedUpdate()
        {
            timer += Time.fixedDeltaTime;
            if (timer < InterestInterval.Value)
            {
                return;
            }
            timer = 0f;
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                var master = pcmc.master;
                if (!master || !master.inventory)
                {
                    continue;
                }
                int stacks = master.inventory.GetItemCount(Def);
                if (stacks > 0 && master.money > 0)
                {
                    uint interest = (uint)Math.Max(1, Math.Floor(master.money * InterestRatePerStack.Value * stacks));
                    master.GiveMoney(interest);
                }
            }
        }
    }
}
