using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace DefenseBudget.Items
{
    // Void (corrupts Roll of Pennies): a portion of incoming damage is billed to your
    // gold at a difficulty-scaled rate instead of your health.
    internal static class FinalNotice
    {
        internal static ItemDef Def;
        internal static ConfigEntry<float> BaseConversion;
        internal static ConfigEntry<float> ConversionPerStack;
        internal static ConfigEntry<float> MaxConversion;
        internal static ConfigEntry<int> GoldCostPer100Damage;

        internal static void Init(ConfigFile config)
        {
            BaseConversion = config.Bind("FinalNotice", "BaseConversion", 0.30f,
                "Fraction of incoming damage billed to gold with one stack.");
            ConversionPerStack = config.Bind("FinalNotice", "ConversionPerStack", 0.10f,
                "Additional conversion fraction per stack beyond the first.");
            MaxConversion = config.Bind("FinalNotice", "MaxConversion", 0.70f,
                "Maximum conversion fraction.");
            GoldCostPer100Damage = config.Bind("FinalNotice", "GoldCostPer100Damage", 50,
                "Gold billed per 100 damage converted, in base gold (scaled by difficulty over time exactly like chest prices).");

            Def = Assets.CreateItemDef(
                "FinalNotice", "FINAL_NOTICE", ItemTier.VoidTier1,
                Assets.LoadSprite("DefenseBudget.icon_final_notice.rgba", 128),
                Assets.CreatePickupModel("PickupFinalNotice", "DefenseBudget.models.final_notice.obj", "DefenseBudget.models.final_notice.rgba", 512, 0.6f),
                new[] { ItemTag.Utility });
            Def.requiredExpansion = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            // Void corruption pairing: Roll of Pennies (GoldOnHurt) -> Final Notice.
            // The ItemDef asset is loaded via Addressables because DLC1Content.Items fields
            // are not populated during plugin Awake.
            var rollOfPennies = Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/GoldOnHurt/GoldOnHurt.asset").WaitForCompletion();
            var provider = ScriptableObject.CreateInstance<ItemRelationshipProvider>();
            provider.name = "DefenseBudgetContagiousItems";
            provider.relationshipType = Addressables.LoadAssetAsync<ItemRelationshipType>("RoR2/DLC1/Common/ContagiousItem.asset").WaitForCompletion();
            provider.relationships = new[]
            {
                new ItemDef.Pair { itemDef1 = rollOfPennies, itemDef2 = Def },
            };
            ContentAddition.AddItemRelationshipProvider(provider);

            int basePct = Mathf.RoundToInt(BaseConversion.Value * 100f);
            int perPct = Mathf.RoundToInt(ConversionPerStack.Value * 100f);
            int maxPct = Mathf.RoundToInt(MaxConversion.Value * 100f);
            LanguageAPI.Add("FINAL_NOTICE_NAME", "Final Notice");
            LanguageAPI.Add("FINAL_NOTICE_PICKUP",
                $"A portion of incoming damage is billed to your gold. <style=cIsVoid>Corrupts all Rolls of Pennies</style>.");
            LanguageAPI.Add("FINAL_NOTICE_DESC",
                $"<style=cIsUtility>{basePct}% <style=cStack>(+{perPct}% per stack, up to {maxPct}%)</style></style> of incoming damage is " +
                $"<style=cIsUtility>billed to your gold</style> at a difficulty-scaled rate instead of your health. " +
                $"Damage you cannot pay for is taken as normal. <style=cIsVoid>Corrupts all Rolls of Pennies</style>.");
            LanguageAPI.Add("FINAL_NOTICE_LORE",
                "ACCOUNT: OVERDUE\nThis is your FINAL NOTICE. Subsequent collection attempts will be performed in person.\n\nWe thank you for your continued patronage. There is no opting out.");

            // Registered after GoldenParachute.Init so this hook runs first (outermost) —
            // damage billed to gold here may make the hit non-lethal before the parachute check.
            On.RoR2.HealthComponent.TakeDamage += BillDamage;
        }

        private static void BillDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (NetworkServer.active && damageInfo != null && !damageInfo.rejected && damageInfo.damage > 0f
                && self.body && Run.instance)
            {
                var master = self.body.master;
                int stacks = master && master.inventory ? master.inventory.GetItemCount(Def) : 0;
                if (stacks > 0 && master.money > 0)
                {
                    float fraction = Mathf.Min(BaseConversion.Value + ConversionPerStack.Value * (stacks - 1), MaxConversion.Value);
                    float blocked = damageInfo.damage * fraction;
                    // gold per point of damage, scaling with difficulty like chest prices
                    float rate = Run.instance.GetDifficultyScaledCost(GoldCostPer100Damage.Value) / 100f;
                    float cost = Mathf.Max(1f, blocked * rate);
                    if (cost > master.money)
                    {
                        blocked *= master.money / cost;
                        cost = master.money;
                    }
                    master.money -= (uint)Mathf.CeilToInt(cost);
                    damageInfo.damage -= blocked;
                }
            }
            orig(self, damageInfo);
        }
    }
}
