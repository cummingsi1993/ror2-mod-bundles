using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;

namespace AuditDepartment.Items
{
    // Lunar: pump money into the monster economy. Directors earn extra credits per
    // lobby-wide stack, and a kickback on every boosted credit is paid out as gold —
    // split evenly among ALL players, because everyone shares the harder stage.
    // Boost + kickback logic lives in DirectorHooks; this file is definition + config.
    internal static class StimulusPackage
    {
        internal static ItemDef Def;
        internal static ConfigEntry<float> BoostPerStack;
        internal static ConfigEntry<float> KickbackGoldPerCredit;

        internal static void Init(ConfigFile config)
        {
            BoostPerStack = config.Bind("StimulusPackage", "BoostPerStack", 0.5f,
                "Extra monster director income per stack (all players' stacks combined — the whole lobby shares the heat).");
            KickbackGoldPerCredit = config.Bind("StimulusPackage", "KickbackGoldPerCredit", 0.5f,
                "Gold paid out per bonus credit the directors earn, split evenly among all players.");

            Def = Assets.CreateItemDef(
                "StimulusPackage", "STIMULUS_PACKAGE", ItemTier.Lunar,
                Assets.LoadSprite("AuditDepartment.icon_stimulus_package.rgba", 128),
                Assets.CreatePickupModel("PickupStimulusPackage", "AuditDepartment.models.stimulus_package.obj", "AuditDepartment.models.stimulus_package.rgba", 512, 0.6f),
                new[] { ItemTag.Utility, ItemTag.AIBlacklist });
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            int boostPct = Mathf.RoundToInt(BoostPerStack.Value * 100f);
            LanguageAPI.Add("STIMULUS_PACKAGE_NAME", "Stimulus Package");
            LanguageAPI.Add("STIMULUS_PACKAGE_PICKUP", "Fund the enemy. Everyone gets a cut. Everyone gets the consequences.");
            LanguageAPI.Add("STIMULUS_PACKAGE_DESC",
                $"Monster directors earn <style=cIsHealth>+{boostPct}% income per stack</style> <style=cStack>(all players' stacks combined)</style>. " +
                $"A <style=cIsUtility>kickback</style> on every bonus credit is paid out as <style=cIsUtility>gold, split evenly among all players</style>.");
            LanguageAPI.Add("STIMULUS_PACKAGE_LORE",
                "The committee approved emergency funding to the invasion on the theory that a stronger invasion would stimulate the defense sector, which would stimulate the economy, which would fund the committee. Minutes show one dissenting vote, recorded only as 'the intern.'");
        }
    }
}
