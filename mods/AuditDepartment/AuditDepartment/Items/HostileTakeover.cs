using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;

namespace AuditDepartment.Items
{
    // Red: runs a shadow board that skims a cut of the monster director's income and
    // spends it hiring monsters from this stage's roster onto YOUR team. The skim rate
    // caps per run regardless of holders/stacks so a lobby can't bankrupt the stage.
    // Skim + spawn logic lives in DirectorHooks; this file is definition + config.
    internal static class HostileTakeover
    {
        internal static ItemDef Def;
        internal static ConfigEntry<float> SkimPerStack;
        internal static ConfigEntry<float> SkimCap;
        internal static ConfigEntry<int> AllyCapBase;
        internal static ConfigEntry<int> AllyCapMax;
        internal static ConfigEntry<float> AllyLifetime;

        internal static void Init(ConfigFile config)
        {
            SkimPerStack = config.Bind("HostileTakeover", "SkimPerStack", 0.15f,
                "Fraction of monster director income skimmed per stack (all holders combined).");
            SkimCap = config.Bind("HostileTakeover", "SkimCap", 0.5f,
                "Hard cap on the total skim fraction for the whole lobby, regardless of stacks.");
            AllyCapBase = config.Bind("HostileTakeover", "AllyCapBase", 2,
                "Maximum hired monsters alive at once with one stack (+1 per additional stack).");
            AllyCapMax = config.Bind("HostileTakeover", "AllyCapMax", 6,
                "Absolute ceiling on hired monsters alive at once.");
            AllyLifetime = config.Bind("HostileTakeover", "AllyLifetime", 120f,
                "Seconds a hired monster stays on payroll before its contract expires (0 = permanent).");

            Def = Assets.CreateItemDef(
                "HostileTakeover", "HOSTILE_TAKEOVER", ItemTier.Tier3,
                Assets.LoadSprite("AuditDepartment.icon_hostile_takeover.rgba", 128),
                Assets.CreatePickupModel("PickupHostileTakeover", "AuditDepartment.models.hostile_takeover.obj", "AuditDepartment.models.hostile_takeover.rgba", 512, 0.7f),
                new[] { ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.CannotCopy });
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            int skimPct = Mathf.RoundToInt(SkimPerStack.Value * 100f);
            int capPct = Mathf.RoundToInt(SkimCap.Value * 100f);
            LanguageAPI.Add("HOSTILE_TAKEOVER_NAME", "Hostile Takeover");
            LanguageAPI.Add("HOSTILE_TAKEOVER_PICKUP", "Skim the enemy's budget. Spend it hiring their monsters onto your side.");
            LanguageAPI.Add("HOSTILE_TAKEOVER_DESC",
                $"Skim <style=cIsUtility>{skimPct}% <style=cStack>(per stack, up to {capPct}% total)</style></style> of the monster director's income " +
                $"and spend it hiring <style=cIsUtility>monsters from this stage</style> to fight for you. " +
                $"Up to <style=cIsUtility>{AllyCapBase.Value} <style=cStack>(+1 per stack, max {AllyCapMax.Value})</style></style> hires at once, " +
                $"each on a <style=cIsUtility>{AllyLifetime.Value:0}s</style> contract.");
            LanguageAPI.Add("HOSTILE_TAKEOVER_LORE",
                "MINUTES, EMERGENCY BOARD MEETING\nItem 1: It has come to the board's attention that the board is no longer in control of the company.\nItem 2: The new majority shareholder has proposed a motion to 'keep doing whatever you were doing, but for me now.'\nItem 3: Motion carried unanimously. The lemurians voted in a bloc.");
        }
    }
}
