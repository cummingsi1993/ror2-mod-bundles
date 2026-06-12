using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;

namespace SupplyChain.Items
{
    // Green: chests have a chance to drop a second item one tier below their contents,
    // capped per stage. Mechanics live in ChestHooks; this file is definition + config.
    internal static class LoadedDice
    {
        internal static ItemDef Def;
        internal static ConfigEntry<float> BonusChanceBase;
        internal static ConfigEntry<float> BonusChancePerStack;
        internal static ConfigEntry<float> BonusChanceMax;
        internal static ConfigEntry<int> StageCapBase;

        internal static void Init(ConfigFile config)
        {
            BonusChanceBase = config.Bind("LoadedDice", "BonusChanceBase", 0.15f,
                "Chance for a gold chest to drop a bonus item one tier below its contents, with one stack.");
            BonusChancePerStack = config.Bind("LoadedDice", "BonusChancePerStack", 0.10f,
                "Hyperbolic growth rate toward the max chance per additional stack.");
            BonusChanceMax = config.Bind("LoadedDice", "BonusChanceMax", 0.40f,
                "Maximum chance.");
            StageCapBase = config.Bind("LoadedDice", "StageCapBase", 2,
                "Maximum bonus drops per stage with one stack (+1 per additional stack). Shared with Recall Notice.");

            Def = Assets.CreateItemDef(
                "LoadedDice", "LOADED_DICE", ItemTier.Tier2,
                Assets.LoadSprite("SupplyChain.icon_loaded_dice.rgba", 128),
                Assets.CreatePickupModel("PickupLoadedDice", "SupplyChain.models.loaded_dice.obj", "SupplyChain.models.loaded_dice.rgba", 512, 0.55f),
                new[] { ItemTag.Utility, ItemTag.AIBlacklist });
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            int basePct = Mathf.RoundToInt(BonusChanceBase.Value * 100f);
            int maxPct = Mathf.RoundToInt(BonusChanceMax.Value * 100f);
            LanguageAPI.Add("LOADED_DICE_NAME", "Loaded Dice");
            LanguageAPI.Add("LOADED_DICE_PICKUP", "Chests sometimes pay out twice... the house set a daily limit.");
            LanguageAPI.Add("LOADED_DICE_DESC",
                $"Gold-cost chests have a <style=cIsUtility>{basePct}% <style=cStack>(up to {maxPct}% with more stacks)</style></style> chance to drop a " +
                $"<style=cIsUtility>bonus item one tier below their contents</style>. " +
                $"At most <style=cIsUtility>{StageCapBase.Value} <style=cStack>(+1 per stack)</style></style> bonus drops per stage. " +
                $"<style=cIsVoid>Corruptible</style>.");
            LanguageAPI.Add("LOADED_DICE_LORE",
                "Confiscated from the estate of a casino magnate. The pips rearrange themselves when nobody is looking, which the courts ruled was technically not cheating because nobody was looking.");
        }
    }
}
