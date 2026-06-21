using BepInEx.Configuration;
using HostileWorkplace.Artifacts;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace HostileWorkplace
{
    // Slice 3: when one player kills another during an Open Season window, the killer
    // permanently steals a percentage of EACH TIER of the victim's items — rarer tiers
    // taken less. Because it's a fraction of holdings, the richest player is the juiciest
    // kill and has the most to lose; no escrow needed. Fractional amounts roll
    // probabilistically (a single red at 8% = an 8% chance to take it).
    internal static class ItemTheft
    {
        internal static ConfigEntry<float> StealFractionWhite;
        internal static ConfigEntry<float> StealFractionGreen;
        internal static ConfigEntry<float> StealFractionRed;
        internal static ConfigEntry<float> StealFractionBoss;
        internal static ConfigEntry<float> StealFractionLunar;

        internal static void Init(ConfigFile config)
        {
            StealFractionWhite = config.Bind("ItemTheft", "StealFractionWhite", 0.25f,
                "Fraction of the victim's white (and void white) items transferred to the killer.");
            StealFractionGreen = config.Bind("ItemTheft", "StealFractionGreen", 0.15f,
                "Fraction of the victim's green (and void green) items transferred to the killer.");
            StealFractionRed = config.Bind("ItemTheft", "StealFractionRed", 0.08f,
                "Fraction of the victim's red (and void red) items transferred to the killer.");
            StealFractionBoss = config.Bind("ItemTheft", "StealFractionBoss", 0.05f,
                "Fraction of the victim's boss (and void boss) items transferred to the killer.");
            StealFractionLunar = config.Bind("ItemTheft", "StealFractionLunar", 0f,
                "Fraction of the victim's lunar items transferred to the killer. Default 0 (lunars are build-defining).");

            GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath;
        }

        private static bool IsPlayer(CharacterMaster master) =>
            master && master.playerCharacterMasterController;

        private static float FractionForTier(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Tier1:
                case ItemTier.VoidTier1: return StealFractionWhite.Value;
                case ItemTier.Tier2:
                case ItemTier.VoidTier2: return StealFractionGreen.Value;
                case ItemTier.Tier3:
                case ItemTier.VoidTier3: return StealFractionRed.Value;
                case ItemTier.Boss:
                case ItemTier.VoidBoss: return StealFractionBoss.Value;
                case ItemTier.Lunar: return StealFractionLunar.Value;
                default: return 0f;
            }
        }

        private static void OnCharacterDeath(DamageReport report)
        {
            if (!NetworkServer.active || report == null || !ArtifactOfMutiny.Enabled || !BetrayalWindow.IsOpen)
            {
                return;
            }
            var killer = report.attackerMaster;
            var victim = report.victimMaster;
            if (!IsPlayer(killer) || !IsPlayer(victim) || killer == victim
                || !killer.inventory || !victim.inventory)
            {
                return;
            }

            // snapshot before mutating — GiveItem/RemoveItem fire inventory-changed events
            var owned = new List<ItemIndex>(victim.inventory.itemAcquisitionOrder);
            int totalStolen = 0;
            foreach (var index in owned)
            {
                var def = ItemCatalog.GetItemDef(index);
                if (!def || def.hidden || def.tier == ItemTier.NoTier || !def.canRemove)
                {
                    continue;
                }
                float fraction = FractionForTier(def.tier);
                if (fraction <= 0f)
                {
                    continue;
                }
                int count = victim.inventory.GetItemCount(index);
                if (count <= 0)
                {
                    continue;
                }
                float exact = count * fraction;
                int stolen = Mathf.FloorToInt(exact);
                if (UnityEngine.Random.value < exact - stolen)
                {
                    stolen++; // probabilistic remainder
                }
                stolen = Mathf.Min(stolen, count);
                if (stolen <= 0)
                {
                    continue;
                }
                victim.inventory.RemoveItem(index, stolen);
                killer.inventory.GiveItem(index, stolen);
                totalStolen += stolen;
            }

            if (totalStolen > 0)
            {
                Log.Info($"ItemTheft: {Util.GetBestMasterName(killer)} stole {totalStolen} item(s) from {Util.GetBestMasterName(victim)}.");
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = $"<color=#ff8a3a>{Util.GetBestMasterName(killer)} looted {totalStolen} item(s) from {Util.GetBestMasterName(victim)}'s corpse!</color>"
                });
            }
        }
    }
}
