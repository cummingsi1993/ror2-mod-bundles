using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace SupplyChain.Items
{
    // Lunar: you run a dropshipping operation — you never handle the product yourself
    // (you CANNOT open gold chests), but for every (player count / 2) items your teammates
    // collect, a copy is drop-shipped to you. Useless solo by design: with no teammates to
    // skim from, it's pure downside, so the whole appeal is group play. The chest block
    // lives in ChestHooks (which already owns chest interactions); this file owns the item
    // definition and the copy ledger.
    internal static class Dropshipping
    {
        internal static ItemDef Def;
        internal static ConfigEntry<int> PlayersPerCopyDivisor;
        internal static ConfigEntry<bool> CopyLunarAndVoid;

        // re-entrancy guard: a drop-shipped copy is a direct GiveItem, never a world
        // pickup, so it can't re-enter AttemptGrant — but guard anyway against other mods
        // routing grants through droplets.
        private static bool granting;

        // per-holder tally of teammate collections credited toward the next copy
        private class DropshipLedger : MonoBehaviour
        {
            internal int credits;
        }

        internal static void Init(ConfigFile config)
        {
            PlayersPerCopyDivisor = config.Bind("Dropshipping", "PlayersPerCopyDivisor", 2,
                "You receive one copy for every (player count / this) items teammates collect. Default 2.");
            CopyLunarAndVoid = config.Bind("Dropshipping", "CopyLunarAndVoid", true,
                "Whether lunar and void items teammates collect also count and get copied. If false, only standard tiers (white/green/red/boss).");

            Def = Assets.CreateItemDef(
                "Dropshipping", "DROPSHIPPING", ItemTier.Lunar,
                Assets.LoadSprite("SupplyChain.icon_dropshipping.rgba", 128),
                Assets.CreatePickupModel("PickupDropshipping", "SupplyChain.models.dropshipping.obj", "SupplyChain.models.dropshipping.rgba", 512, 0.6f),
                new[] { ItemTag.Utility, ItemTag.AIBlacklist });
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            int divisor = Mathf.Max(1, PlayersPerCopyDivisor.Value);
            LanguageAPI.Add("DROPSHIPPING_NAME", "Dropshipping");
            LanguageAPI.Add("DROPSHIPPING_PICKUP",
                "You can no longer open chests — but a cut of everything your teammates collect is drop-shipped to you.");
            LanguageAPI.Add("DROPSHIPPING_DESC",
                $"<style=cDeath>You can no longer open gold chests.</style> In exchange, for every " +
                $"<style=cIsUtility>{divisor} items your teammates collect</style> <style=cStack>(scaling with player count)</style>, " +
                $"a <style=cIsUtility>copy is drop-shipped to you</style>. " +
                $"<style=cDeath>Useless without teammates.</style>");
            LanguageAPI.Add("DROPSHIPPING_LORE",
                "The genius of dropshipping is that you never touch the product. You never warehouse it, never inspect it, never pry open a single crate. You stand between the supplier and the customer and take a cut of everything that passes between them — and in this particular arrangement, you are also, somehow, the customer.");

            On.RoR2.GenericPickupController.AttemptGrant += CountCollection;
        }

        internal static bool Held(CharacterMaster master) =>
            master && master.inventory && Def != null && Def.itemIndex != ItemIndex.None
            && master.inventory.GetItemCount(Def.itemIndex) > 0;

        // One copy per (player count / divisor) teammate collections, floored at 1.
        private static int CopyThreshold()
        {
            int players = 0;
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                if (pcmc && pcmc.master)
                {
                    players++;
                }
            }
            return Mathf.Max(1, players / Mathf.Max(1, PlayersPerCopyDivisor.Value));
        }

        private static void CountCollection(On.RoR2.GenericPickupController.orig_AttemptGrant orig, GenericPickupController self, CharacterBody body)
        {
            orig(self, body);
            if (!NetworkServer.active || granting || !self || !body)
            {
                return;
            }
            var collector = body.master;
            if (!collector || !collector.playerCharacterMasterController)
            {
                return; // only player collections count
            }
            var pickupDef = PickupCatalog.GetPickupDef(self.pickupIndex);
            if (pickupDef == null || pickupDef.itemIndex == ItemIndex.None)
            {
                return; // equipment, lunar coins, etc. are not "items"
            }
            var itemDef = ItemCatalog.GetItemDef(pickupDef.itemIndex);
            if (!itemDef || itemDef.hidden || itemDef.tier == ItemTier.NoTier)
            {
                return;
            }
            // generated items exclude generators (Fuzzy Dice doctrine)
            if (SupplyChainPlugin.BundlePickups.Contains(self.pickupIndex))
            {
                return;
            }
            if (!CopyLunarAndVoid.Value && IsLunarOrVoid(itemDef.tier))
            {
                return;
            }

            int threshold = CopyThreshold();
            granting = true;
            try
            {
                foreach (var pcmc in PlayerCharacterMasterController.instances)
                {
                    var holder = pcmc ? pcmc.master : null;
                    if (!holder || holder == collector || !Held(holder) || !holder.inventory)
                    {
                        continue; // skim only from OTHER players' collections
                    }
                    var ledger = holder.GetComponent<DropshipLedger>();
                    if (!ledger)
                    {
                        ledger = holder.gameObject.AddComponent<DropshipLedger>();
                    }
                    ledger.credits++;
                    if (ledger.credits >= threshold)
                    {
                        ledger.credits -= threshold;
                        holder.inventory.GiveItem(pickupDef.itemIndex, 1);
                        Log.Info($"Dropshipping: copied {pickupDef.internalName} to {Util.GetBestMasterName(holder)} (1 per {threshold} collected).");
                    }
                }
            }
            finally
            {
                granting = false;
            }
        }

        private static bool IsLunarOrVoid(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Lunar:
                case ItemTier.VoidTier1:
                case ItemTier.VoidTier2:
                case ItemTier.VoidTier3:
                case ItemTier.VoidBoss:
                    return true;
                default:
                    return false;
            }
        }
    }
}
