using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SupplyChain.Items
{
    // Void (corrupts Loaded Dice): bonus chest drops become guaranteed void items, but
    // unlike Loaded Dice the bonus is ONE TIER BELOW the chest (void), and Recall runs on
    // its own tight per-stage cap that scales far slower than Loaded Dice's. The old
    // design reused Loaded Dice's cap verbatim, which was tuned for chance-gated,
    // tier-down, *random* drops — applied to guaranteed full-tier void it handed out a
    // void item on nearly every chest once your Loaded Dice stack (which all corrupts at
    // once) was large. Mechanics live in ChestHooks; this file is definition + config.
    internal static class RecallNotice
    {
        internal static ItemDef Def;
        internal static ConfigEntry<int> StageCapBase;
        internal static ConfigEntry<int> StacksPerExtraCap;
        internal static ConfigEntry<int> StageCapMax;

        internal static void Init(ConfigFile config)
        {
            StageCapBase = config.Bind("RecallNotice", "StageCapBase", 1,
                "Guaranteed void bonus drops per stage with one stack. Recall has its own cap, separate from Loaded Dice's.");
            StacksPerExtraCap = config.Bind("RecallNotice", "StacksPerExtraCap", 2,
                "Additional stacks required to raise the per-stage cap by 1 (slower than Loaded Dice's +1/stack).");
            StageCapMax = config.Bind("RecallNotice", "StageCapMax", 3,
                "Hard ceiling on guaranteed void bonus drops per stage, regardless of stacks.");

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
                "Chests ship a guaranteed void replacement — one tier down, and rationed. <style=cIsVoid>Corrupts all Loaded Dice</style>.");
            LanguageAPI.Add("RECALL_NOTICE_DESC",
                $"Gold-cost chests drop a <style=cIsVoid>guaranteed void item one tier below their contents</style>, " +
                $"at most <style=cIsUtility>{StageCapBase.Value} <style=cStack>(+1 per {StacksPerExtraCap.Value} stacks, max {StageCapMax.Value})</style></style> per stage. " +
                $"<style=cIsVoid>Corrupts all Loaded Dice</style>.");
            LanguageAPI.Add("RECALL_NOTICE_LORE",
                "URGENT PRODUCT RECALL\nAffected units: all\nDefect: contents replaced during shipping\nRemedy: none. Replacement parts have already been delivered. Please do not attempt to return them.\n\nADDENDUM: shipping volume reduced following complaints that there was simply too much of a good thing.");
        }

        // Per-stage cap for guaranteed void drops, scaling sub-linearly and clamped.
        internal static int StageCapFor(int stacks)
        {
            if (stacks <= 0)
            {
                return 0;
            }
            int cap = StageCapBase.Value + (stacks - 1) / Mathf.Max(1, StacksPerExtraCap.Value);
            return Mathf.Min(cap, StageCapMax.Value);
        }
    }
}
