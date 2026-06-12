using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SupplyChain.Items
{
    // Void (corrupts Loaded Dice): bonus chest drops become guaranteed, but every bonus
    // item is a void item of the chest's tier. Shares Loaded Dice's per-stage cap.
    // Mechanics live in ChestHooks; this file is definition + corruption pairing.
    internal static class RecallNotice
    {
        internal static ItemDef Def;

        internal static void Init(ConfigFile config)
        {
            Def = Assets.CreateItemDef(
                "RecallNotice", "RECALL_NOTICE", ItemTier.VoidTier2,
                Assets.LoadSprite("SupplyChain.icon_recall_notice.rgba", 128),
                Assets.CreatePickupModel("PickupRecallNotice", "SupplyChain.models.recall_notice.obj", "SupplyChain.models.recall_notice.rgba", 512, 0.6f),
                new[] { ItemTag.Utility, ItemTag.AIBlacklist });
            Def.requiredExpansion = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            // corruption pairing against our own Loaded Dice — both defs exist at Awake,
            // so no addressable lookup or timing dance is needed
            var provider = ScriptableObject.CreateInstance<ItemRelationshipProvider>();
            provider.name = "SupplyChainContagiousItems";
            provider.relationshipType = Addressables.LoadAssetAsync<ItemRelationshipType>("RoR2/DLC1/Common/ContagiousItem.asset").WaitForCompletion();
            provider.relationships = new[]
            {
                new ItemDef.Pair { itemDef1 = LoadedDice.Def, itemDef2 = Def },
            };
            ContentAddition.AddItemRelationshipProvider(provider);

            LanguageAPI.Add("RECALL_NOTICE_NAME", "Recall Notice");
            LanguageAPI.Add("RECALL_NOTICE_PICKUP",
                "Chests always ship a bonus item — direct from the manufacturer in the void. <style=cIsVoid>Corrupts all Loaded Dice</style>.");
            LanguageAPI.Add("RECALL_NOTICE_DESC",
                $"Gold-cost chests <style=cIsUtility>always</style> drop a bonus item, but the bonus is a " +
                $"<style=cIsVoid>void item</style> of the chest's tier. " +
                $"Shares Loaded Dice's per-stage bonus cap. <style=cIsVoid>Corrupts all Loaded Dice</style>.");
            LanguageAPI.Add("RECALL_NOTICE_LORE",
                "URGENT PRODUCT RECALL\nAffected units: all\nDefect: contents replaced during shipping\nRemedy: none. Replacement parts have already been delivered. Please do not attempt to return them.");
        }
    }
}
