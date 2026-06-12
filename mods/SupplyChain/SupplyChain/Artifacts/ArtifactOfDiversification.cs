using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;

namespace SupplyChain.Artifacts
{
    // When a chest would drop an item the opener already owns a pile of, it rerolls
    // once. Runs wider, not taller. Reroll logic lives in ChestHooks (uses the chest's
    // own public RollItem(), so the reroll respects the chest's real drop table).
    internal static class ArtifactOfDiversification
    {
        internal static ArtifactDef Def;
        internal static ConfigEntry<int> RerollThreshold;

        internal static void Init(ConfigFile config)
        {
            RerollThreshold = config.Bind("ArtifactOfDiversification", "RerollThreshold", 5,
                "Chest drops reroll once if the opener already owns at least this many stacks of the rolled item.");

            Def = ScriptableObject.CreateInstance<ArtifactDef>();
            Def.cachedName = "ArtifactOfDiversification";
            Def.nameToken = "ARTIFACT_DIVERSIFICATION_NAME";
            Def.descriptionToken = "ARTIFACT_DIVERSIFICATION_DESC";
            Def.smallIconSelectedSprite = Assets.LoadSprite("SupplyChain.icon_artifact_diversification_enabled.rgba", 128);
            Def.smallIconDeselectedSprite = Assets.LoadSprite("SupplyChain.icon_artifact_diversification_disabled.rgba", 128);
            ContentAddition.AddArtifactDef(Def);

            LanguageAPI.Add("ARTIFACT_DIVERSIFICATION_NAME", "Artifact of Diversification");
            LanguageAPI.Add("ARTIFACT_DIVERSIFICATION_DESC",
                $"Chests reroll their contents once when they would drop an item you already own {RerollThreshold.Value}+ stacks of.");
        }

        internal static bool Enabled =>
            Def && RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(Def);
    }
}
