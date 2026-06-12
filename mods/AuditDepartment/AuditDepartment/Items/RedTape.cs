using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;

namespace AuditDepartment.Items
{
    // White: enemies that spawn near you arrive buried in paperwork — slowed and
    // weakened for a few seconds. Spawn detection lives in DirectorHooks; this file is
    // definition + config.
    internal static class RedTape
    {
        internal static ItemDef Def;
        internal static ConfigEntry<float> Radius;
        internal static ConfigEntry<float> DurationBase;
        internal static ConfigEntry<float> DurationPerStack;

        internal static void Init(ConfigFile config)
        {
            Radius = config.Bind("RedTape", "Radius", 75f,
                "Enemies spawning within this range of a holder are affected.");
            DurationBase = config.Bind("RedTape", "DurationBase", 3f,
                "Seconds of slow and weaken applied to nearby spawns, with one stack.");
            DurationPerStack = config.Bind("RedTape", "DurationPerStack", 1.5f,
                "Additional seconds per stack beyond the first (stacks of all nearby holders combined).");

            Def = Assets.CreateItemDef(
                "RedTape", "RED_TAPE", ItemTier.Tier1,
                Assets.LoadSprite("AuditDepartment.icon_red_tape.rgba", 128),
                Assets.CreatePickupModel("PickupRedTape", "AuditDepartment.models.red_tape.obj", "AuditDepartment.models.red_tape.rgba", 512, 0.5f),
                new[] { ItemTag.Utility, ItemTag.AIBlacklist });
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            LanguageAPI.Add("RED_TAPE_NAME", "Red Tape");
            LanguageAPI.Add("RED_TAPE_PICKUP", "Enemies spawning nearby are buried in paperwork.");
            LanguageAPI.Add("RED_TAPE_DESC",
                $"Enemies that spawn within <style=cIsUtility>{Radius.Value:0}m</style> are " +
                $"<style=cIsUtility>slowed</style> and <style=cIsUtility>weakened</style> for " +
                $"<style=cIsUtility>{DurationBase.Value:0.#}s</style> <style=cStack>(+{DurationPerStack.Value:0.#}s per stack)</style>. " +
                $"<style=cIsVoid>Corruptible</style>.");
            LanguageAPI.Add("RED_TAPE_LORE",
                "Form 77-C (Hostile Materialization Permit) must be filed in triplicate no fewer than three business days before manifesting on the combat plane. Applicants report that by the time approval arrives, the war is usually over.");
        }
    }
}
