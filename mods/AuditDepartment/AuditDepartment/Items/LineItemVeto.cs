using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;

namespace AuditDepartment.Items
{
    // Green: periodically vetoes the monster director's next big requisition — the
    // spawn is cancelled, its budget is destroyed, and the holders pocket the
    // difference as gold. Veto logic lives in DirectorHooks; this file is definition +
    // config.
    internal static class LineItemVeto
    {
        internal static ItemDef Def;
        internal static ConfigEntry<int> MinVetoCost;
        internal static ConfigEntry<float> CooldownBase;
        internal static ConfigEntry<float> CooldownStackMultiplier;
        internal static ConfigEntry<float> CooldownMin;
        internal static ConfigEntry<float> GoldPerCredit;
        internal static ConfigEntry<bool> BroadcastVetoes;

        internal static void Init(ConfigFile config)
        {
            MinVetoCost = config.Bind("LineItemVeto", "MinVetoCost", 100,
                "Only spawns costing at least this many director credits are vetoed (small fry get rubber-stamped).");
            CooldownBase = config.Bind("LineItemVeto", "CooldownBase", 45f,
                "Seconds between vetoes with one stack in the lobby. The veto is global: one budget, one committee.");
            CooldownStackMultiplier = config.Bind("LineItemVeto", "CooldownStackMultiplier", 0.85f,
                "Cooldown is multiplied by this per stack beyond the first (all holders combined).");
            CooldownMin = config.Bind("LineItemVeto", "CooldownMin", 10f,
                "Cooldown floor.");
            GoldPerCredit = config.Bind("LineItemVeto", "GoldPerCredit", 1.0f,
                "Gold awarded per vetoed director credit, split among holders by stack count.");
            BroadcastVetoes = config.Bind("LineItemVeto", "BroadcastVetoes", true,
                "Announce vetoes in chat.");

            Def = Assets.CreateItemDef(
                "LineItemVeto", "LINE_ITEM_VETO", ItemTier.Tier2,
                Assets.LoadSprite("AuditDepartment.icon_line_item_veto.rgba", 128),
                Assets.CreatePickupModel("PickupLineItemVeto", "AuditDepartment.models.line_item_veto.obj", "AuditDepartment.models.line_item_veto.rgba", 512, 0.55f),
                new[] { ItemTag.Utility, ItemTag.AIBlacklist });
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            LanguageAPI.Add("LINE_ITEM_VETO_NAME", "Line-Item Veto");
            LanguageAPI.Add("LINE_ITEM_VETO_PICKUP", "Periodically cancel the enemy's most expensive reinforcements — and pocket the budget.");
            LanguageAPI.Add("LINE_ITEM_VETO_DESC",
                $"Every <style=cIsUtility>{CooldownBase.Value:0}s</style> <style=cStack>(x{CooldownStackMultiplier.Value:0.00} per stack)</style>, " +
                $"the next enemy spawn costing <style=cIsUtility>{MinVetoCost.Value}+ director credits</style> is " +
                $"<style=cIsUtility>vetoed</style>: the spawn is cancelled and its budget is paid out as " +
                $"<style=cIsUtility>gold</style> to all holders.");
            LanguageAPI.Add("LINE_ITEM_VETO_LORE",
                "Requisition 8841: one (1) Magma Worm, urgent.\nStatus: DENIED.\nReason: duplicate of Requisition 8840 (one (1) Magma Worm, urgent), which was denied as a duplicate of Requisition 8839.\nNote from the comptroller: keep them coming. We're paid by the denial.");
        }
    }
}
