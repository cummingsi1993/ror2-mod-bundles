using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace DefenseBudget.Items
{
    // Red: upon taking lethal damage, pay a difficulty-scaled severance fee (drawing on
    // Defense Budget credit if gold is short) to survive at 15% health.
    internal static class GoldenParachute
    {
        internal static ItemDef Def;
        internal static ConfigEntry<int> BaseCost;
        internal static ConfigEntry<float> CooldownSeconds;
        internal static ConfigEntry<float> CooldownReductionPerStack;
        internal static ConfigEntry<float> SurviveHealthFraction;
        internal static ConfigEntry<float> ImmunityDuration;

        private class ParachuteCooldown : MonoBehaviour
        {
            internal float readyTime;
        }

        internal static void Init(ConfigFile config)
        {
            BaseCost = config.Bind("GoldenParachute", "BaseCost", 40,
                "Severance fee in base gold, scaled by difficulty over time exactly like chest prices.");
            CooldownSeconds = config.Bind("GoldenParachute", "CooldownSeconds", 45f,
                "Cooldown between deployments.");
            CooldownReductionPerStack = config.Bind("GoldenParachute", "CooldownReductionPerStack", 0.25f,
                "Multiplicative cooldown reduction per stack beyond the first.");
            SurviveHealthFraction = config.Bind("GoldenParachute", "SurviveHealthFraction", 0.15f,
                "Fraction of full health restored on deployment.");
            ImmunityDuration = config.Bind("GoldenParachute", "ImmunityDuration", 1.5f,
                "Seconds of immunity granted on deployment (covers the rest of the killing burst).");

            Def = Assets.CreateItemDef(
                "GoldenParachute", "GOLDEN_PARACHUTE", ItemTier.Tier3,
                Assets.LoadSprite("DefenseBudget.icon_golden_parachute.rgba", 128),
                Assets.CreatePickupModel("PickupGoldenParachute", "DefenseBudget.models.golden_parachute.obj", "DefenseBudget.models.golden_parachute.rgba", 512, 0.8f),
                new[] { ItemTag.Utility, ItemTag.AIBlacklist });
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            LanguageAPI.Add("GOLDEN_PARACHUTE_NAME", "Golden Parachute");
            LanguageAPI.Add("GOLDEN_PARACHUTE_PICKUP", "Cheat death by cashing out.");
            LanguageAPI.Add("GOLDEN_PARACHUTE_DESC",
                $"Upon taking lethal damage, pay a <style=cIsUtility>severance fee</style> of difficulty-scaled gold " +
                $"(drawing on <style=cIsUtility>credit</style> if you have a Defense Budget) to instead survive at " +
                $"<style=cIsHealing>{Mathf.RoundToInt(SurviveHealthFraction.Value * 100f)}% health</style>. " +
                $"If you cannot cover the fee, you die. Recharges every {CooldownSeconds.Value:0} seconds " +
                $"<style=cStack>(-{Mathf.RoundToInt(CooldownReductionPerStack.Value * 100f)}% per stack)</style>.");
            LanguageAPI.Add("GOLDEN_PARACHUTE_LORE",
                "Section 12(b): In the event of involuntary separation (termination, restructuring, evisceration), the executive shall receive one (1) severance package, deployment automatic, fee deducted from estate.\n\nThe board notes with approval that the executive has never once read Section 12(c): renewal fees.");

            // Registered before FinalNotice.Init so Final Notice's damage billing runs first
            // (outermost) and a billed-down hit may no longer be lethal.
            On.RoR2.HealthComponent.TakeDamage += InterceptLethal;
        }

        private static void InterceptLethal(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (NetworkServer.active && damageInfo != null && !damageInfo.rejected && damageInfo.damage > 0f
                && self.alive && self.body && Run.instance)
            {
                var master = self.body.master;
                int stacks = master && master.inventory ? master.inventory.GetItemCount(Def) : 0;
                if (stacks > 0)
                {
                    var cooldown = master.GetComponent<ParachuteCooldown>();
                    float now = Run.instance.time;
                    if (cooldown == null || now >= cooldown.readyTime)
                    {
                        // estimate post-armor damage; bypass-armor hits will be underestimated
                        float armor = self.body.armor;
                        float armorFactor = armor >= 0f ? 100f / (100f + armor) : 2f - 100f / (100f - armor);
                        float estimated = damageInfo.damage * armorFactor;
                        if (estimated >= self.combinedHealth)
                        {
                            uint cost = (uint)Run.instance.GetDifficultyScaledCost(BaseCost.Value);
                            if (master.money + DefenseBudgetPlugin.GetAvailableCredit(master) >= cost)
                            {
                                DefenseBudgetPlugin.PayWithCredit(master, cost);
                                // Setting damageInfo.rejected here is useless: TakeDamageProcess
                                // recomputes it, clobbering our value. It computes it partly from
                                // the Immune buff, so grant that instead — vanilla then rejects
                                // this hit (and the rest of the burst) through its own path.
                                self.body.AddTimedBuff(RoR2Content.Buffs.Immune, ImmunityDuration.Value);
                                self.Networkhealth = Mathf.Max(self.health, self.fullHealth * SurviveHealthFraction.Value);
                                Log.Info($"Golden Parachute deployed for {Util.GetBestMasterName(master)} (fee {cost})");
                                if (cooldown == null)
                                {
                                    cooldown = master.gameObject.AddComponent<ParachuteCooldown>();
                                }
                                cooldown.readyTime = now + CooldownSeconds.Value * Mathf.Pow(1f - CooldownReductionPerStack.Value, stacks - 1);
                                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                                {
                                    baseToken = $"<color=#ffd24a>{Util.GetBestMasterName(master)}'s Golden Parachute deployed! Severance fee: {cost} gold.</color>"
                                });
                            }
                        }
                    }
                }
            }
            orig(self, damageInfo);
        }
    }
}
