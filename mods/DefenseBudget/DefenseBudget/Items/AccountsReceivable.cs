using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace DefenseBudget.Items
{
    // Green: enemies you damage are marked; when a marked enemy dies (killed by anyone),
    // you collect a bonus cut of its gold bounty.
    internal static class AccountsReceivable
    {
        internal static ItemDef Def;
        internal static ConfigEntry<float> BountyBonusPerStack;

        private class Claim : MonoBehaviour
        {
            internal readonly HashSet<CharacterMaster> claimants = new HashSet<CharacterMaster>();
        }

        internal static void Init(ConfigFile config)
        {
            BountyBonusPerStack = config.Bind("AccountsReceivable", "BountyBonusPerStack", 0.10f,
                "Bonus fraction of a marked enemy's gold bounty collected on its death, per stack.");

            Def = Assets.CreateItemDef(
                "AccountsReceivable", "ACCOUNTS_RECEIVABLE", ItemTier.Tier2,
                Assets.LoadSprite("DefenseBudget.icon_accounts_receivable.rgba", 128),
                Assets.CreatePickupModel("PickupAccountsReceivable", "DefenseBudget.models.accounts_receivable.obj", "DefenseBudget.models.accounts_receivable.rgba", 512, 0.6f),
                new[] { ItemTag.Utility });
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            int bonusPct = Mathf.RoundToInt(BountyBonusPerStack.Value * 100f);
            LanguageAPI.Add("ACCOUNTS_RECEIVABLE_NAME", "Accounts Receivable");
            LanguageAPI.Add("ACCOUNTS_RECEIVABLE_PICKUP", "Enemies you damage owe you a cut of their bounty.");
            LanguageAPI.Add("ACCOUNTS_RECEIVABLE_DESC",
                $"Damaging an enemy <style=cIsUtility>marks</style> them. Marked enemies grant you an additional " +
                $"<style=cIsUtility>{bonusPct}% <style=cStack>(+{bonusPct}% per stack)</style></style> of their gold reward when killed by anyone.");
            LanguageAPI.Add("ACCOUNTS_RECEIVABLE_LORE",
                "INVOICE #44721\nServices rendered: ballistic consultation (1x)\nPayment due: immediately\nLate fee: continued ballistic consultation");

            On.RoR2.HealthComponent.TakeDamage += MarkClaim;
            On.RoR2.GlobalEventManager.OnCharacterDeath += CollectClaims;
        }

        private static void MarkClaim(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (NetworkServer.active && damageInfo != null && !damageInfo.rejected && damageInfo.damage > 0f && damageInfo.attacker && self.body)
            {
                var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                var attackerMaster = attackerBody ? attackerBody.master : null;
                if (attackerMaster && attackerMaster.inventory
                    && attackerBody.teamComponent && self.body.teamComponent
                    && attackerBody.teamComponent.teamIndex != self.body.teamComponent.teamIndex
                    && attackerMaster.inventory.GetItemCount(Def) > 0)
                {
                    var claim = self.body.GetComponent<Claim>();
                    if (!claim)
                    {
                        claim = self.body.gameObject.AddComponent<Claim>();
                    }
                    claim.claimants.Add(attackerMaster);
                }
            }
            orig(self, damageInfo);
        }

        private static void CollectClaims(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            uint goldReward = 0;
            Claim claim = null;
            if (NetworkServer.active && damageReport?.victimBody)
            {
                claim = damageReport.victimBody.GetComponent<Claim>();
                if (claim != null)
                {
                    var rewards = damageReport.victimBody.GetComponent<DeathRewards>();
                    goldReward = rewards ? rewards.goldReward : 0;
                }
            }
            orig(self, damageReport);
            if (claim == null || goldReward == 0)
            {
                return;
            }
            foreach (var claimant in claim.claimants)
            {
                if (!claimant || !claimant.inventory)
                {
                    continue;
                }
                int stacks = claimant.inventory.GetItemCount(Def);
                if (stacks > 0)
                {
                    uint bonus = (uint)Math.Round(goldReward * BountyBonusPerStack.Value * stacks);
                    if (bonus > 0)
                    {
                        claimant.GiveMoney(bonus);
                    }
                }
            }
        }
    }
}
