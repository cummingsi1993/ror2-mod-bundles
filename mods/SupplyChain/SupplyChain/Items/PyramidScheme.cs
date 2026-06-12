using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace SupplyChain.Items
{
    // Lunar: on pickup, gain +1 stack of every item you own. Thereafter a cut of all
    // gold income per stack is paid "upline" and vanishes.
    internal static class PyramidScheme
    {
        internal static ItemDef Def;
        internal static ConfigEntry<float> TithePerStack;

        // re-entrancy guard: granting stacks fires OnInventoryChanged again
        private static bool granting;

        private class PyramidTracker : MonoBehaviour
        {
            internal int knownStacks;
        }

        internal static void Init(ConfigFile config)
        {
            TithePerStack = config.Bind("PyramidScheme", "TithePerStack", 0.20f,
                "Fraction of gold income paid upline (lost) per stack, multiplicative.");

            Def = Assets.CreateItemDef(
                "PyramidScheme", "PYRAMID_SCHEME", ItemTier.Lunar,
                Assets.LoadSprite("SupplyChain.icon_pyramid_scheme.rgba", 128),
                Assets.CreatePickupModel("PickupPyramidScheme", "SupplyChain.models.pyramid_scheme.obj", "SupplyChain.models.pyramid_scheme.rgba", 512, 0.65f),
                new[] { ItemTag.Utility, ItemTag.AIBlacklist });
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            int tithePct = Mathf.RoundToInt(TithePerStack.Value * 100f);
            LanguageAPI.Add("PYRAMID_SCHEME_NAME", "Pyramid Scheme");
            LanguageAPI.Add("PYRAMID_SCHEME_PICKUP",
                "Gain +1 stack of everything you own... <style=cDeath>but your income pays the upline, forever.</style>");
            LanguageAPI.Add("PYRAMID_SCHEME_DESC",
                $"On pickup, gain <style=cIsUtility>+1 stack of every item you own</style>. " +
                $"Afterwards, <style=cDeath>{tithePct}% of your gold income <style=cStack>(stacks multiplicatively)</style> is paid upline</style>. " +
                "The upline does not pay back.");
            LanguageAPI.Add("PYRAMID_SCHEME_LORE",
                "It's not what you think. It's a reverse funnel system. The structure is more of a triangle, really. Your downline practically builds itself once you stop being so negative. Anyway — about your buy-in.");

            On.RoR2.CharacterMaster.OnInventoryChanged += DetectPickup;
            On.RoR2.CharacterMaster.GiveMoney += PayUpline;
        }

        private static void DetectPickup(On.RoR2.CharacterMaster.orig_OnInventoryChanged orig, CharacterMaster self)
        {
            orig(self);
            if (!NetworkServer.active || granting || !self.inventory || Def == null || Def.itemIndex == ItemIndex.None)
            {
                return;
            }
            var tracker = self.GetComponent<PyramidTracker>();
            if (!tracker)
            {
                tracker = self.gameObject.AddComponent<PyramidTracker>();
            }
            int stacks = InventoryCompat.GetPermanentCount(self.inventory, Def.itemIndex);
            int delta = stacks - tracker.knownStacks;
            tracker.knownStacks = stacks;
            if (delta <= 0)
            {
                return;
            }

            granting = true;
            try
            {
                int kinds = 0;
                for (int i = 0; i < ItemCatalog.itemCount; i++)
                {
                    var index = (ItemIndex)i;
                    if (index == Def.itemIndex)
                    {
                        continue;
                    }
                    var itemDef = ItemCatalog.GetItemDef(index);
                    if (!itemDef || itemDef.hidden || itemDef.tier == ItemTier.NoTier)
                    {
                        continue;
                    }
                    if (InventoryCompat.GetPermanentCount(self.inventory, index) > 0)
                    {
                        self.inventory.GiveItem(index, delta);
                        kinds++;
                    }
                }
                Log.Info($"Pyramid Scheme paid out +{delta} stack(s) of {kinds} item(s) for {Util.GetBestMasterName(self)}");
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = $"<color=#79c7ec>{Util.GetBestMasterName(self)} joined a pyramid scheme: +{delta} stack(s) of {kinds} item(s). The upline thanks them.</color>"
                });
            }
            finally
            {
                granting = false;
            }
        }

        private static void PayUpline(On.RoR2.CharacterMaster.orig_GiveMoney orig, CharacterMaster self, uint amount)
        {
            if (NetworkServer.active && amount > 0 && self.inventory && Def != null && Def.itemIndex != ItemIndex.None)
            {
                int stacks = InventoryCompat.GetPermanentCount(self.inventory, Def.itemIndex);
                if (stacks > 0)
                {
                    amount = (uint)Math.Round(amount * Math.Pow(1.0 - Mathf.Clamp01(TithePerStack.Value), stacks));
                }
            }
            orig(self, amount);
        }
    }
}
