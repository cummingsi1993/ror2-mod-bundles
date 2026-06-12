using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace DefenseBudget.Artifacts
{
    // All gold is pooled: every player's money is a mirror of one shared pool. Income
    // flows into the pool (scaled to a bit less than playerCount x), and any spend by
    // any player drains it.
    internal static class ArtifactOfCommunism
    {
        internal static ArtifactDef Def;
        internal static ConfigEntry<float> SharePenaltyPerExtraPlayer;

        private static double pool;
        private static uint lastSyncedMoney;
        private static bool poolInitialized;

        internal static void Init(ConfigFile config)
        {
            SharePenaltyPerExtraPlayer = config.Bind("ArtifactOfCommunism", "SharePenaltyPerExtraPlayer", 0.25f,
                "Collectivization overhead. Total pool income is multiplied by playerCount - penalty * (playerCount - 1): " +
                "0.25 means 2 players earn x1.75, 4 players earn x3.25. Solo is unaffected.");

            Def = ScriptableObject.CreateInstance<ArtifactDef>();
            Def.cachedName = "ArtifactOfCommunism";
            Def.nameToken = "ARTIFACT_COMMUNISM_NAME";
            Def.descriptionToken = "ARTIFACT_COMMUNISM_DESC";
            Def.smallIconSelectedSprite = Assets.LoadSprite("DefenseBudget.icon_artifact_communism_enabled.rgba", 128);
            Def.smallIconDeselectedSprite = Assets.LoadSprite("DefenseBudget.icon_artifact_communism_disabled.rgba", 128);
            ContentAddition.AddArtifactDef(Def);

            LanguageAPI.Add("ARTIFACT_COMMUNISM_NAME", "Artifact of Communism");
            LanguageAPI.Add("ARTIFACT_COMMUNISM_DESC",
                "All gold belongs to the collective: every player earns into and spends from one shared pool. " +
                "Total gold income is slightly less than the sum of what individuals would have earned.");

            // Registered BEFORE the plugin's tax/debt GiveMoney hook, making this the
            // innermost link: Defense Budget taxes and debt repayment run first, and only
            // the remainder is collectivized.
            On.RoR2.CharacterMaster.GiveMoney += PoolIncome;
            Run.onRunStartGlobal += _ =>
            {
                pool = 0;
                lastSyncedMoney = 0;
                poolInitialized = false;
            };
        }

        internal static bool Enabled =>
            Def && RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(Def);

        private static void PoolIncome(On.RoR2.CharacterMaster.orig_GiveMoney orig, CharacterMaster self, uint amount)
        {
            if (!NetworkServer.active || amount == 0 || !Enabled || !self.playerCharacterMasterController)
            {
                orig(self, amount);
                return;
            }
            EnsureInitialized();
            // Team gold already arrives as one GiveMoney call per player, so scaling each
            // call by (N - penalty*(N-1))/N makes a team kill worth a bit less than N x.
            int players = Mathf.Max(1, PlayerCharacterMasterController.instances.Count);
            float scale = (players - SharePenaltyPerExtraPlayer.Value * (players - 1)) / players;
            pool += amount * scale;
            // wallets are mirrors of the pool; the next reconcile tick distributes this
        }

        private static void EnsureInitialized()
        {
            if (poolInitialized)
            {
                return;
            }
            poolInitialized = true;
            pool = 0;
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                if (pcmc.master)
                {
                    pool += pcmc.master.money;
                }
            }
            lastSyncedMoney = (uint)pool;
            Mirror();
        }

        private static void Mirror()
        {
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                var master = pcmc.master;
                if (master && master.money != lastSyncedMoney)
                {
                    master.money = lastSyncedMoney;
                }
            }
        }

        // Called from the plugin's FixedUpdate (server, active run). Wallet deviations
        // from the last mirrored value are spends (or direct money writes, e.g. Brittle
        // Crown losses or Defense Budget credit top-ups) — fold them into the pool.
        internal static void FixedUpdate()
        {
            if (!Enabled)
            {
                return;
            }
            EnsureInitialized();
            double delta = 0;
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                var master = pcmc.master;
                if (master)
                {
                    delta += (double)master.money - lastSyncedMoney;
                }
            }
            pool = Math.Max(0.0, pool + delta);
            lastSyncedMoney = (uint)pool;
            Mirror();
        }
    }
}
