using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;

namespace AuditDepartment.Artifacts
{
    // Monster spending runs on a fixed per-stage allowance: directors earn faster while
    // the budget lasts, then the well runs dry. Fewer, harder waves. Budget enforcement
    // lives in DirectorHooks; this file is definition + config.
    internal static class ArtifactOfAusterity
    {
        internal static ArtifactDef Def;
        internal static ConfigEntry<float> BoostFactor;
        internal static ConfigEntry<float> BudgetBase;
        internal static ConfigEntry<float> BudgetDifficultyScale;

        internal static void Init(ConfigFile config)
        {
            BoostFactor = config.Bind("ArtifactOfAusterity", "BoostFactor", 1.5f,
                "Director income multiplier while the stage budget lasts.");
            BudgetBase = config.Bind("ArtifactOfAusterity", "BudgetBase", 450f,
                "Base credits each director may earn per stage before its budget is exhausted.");
            BudgetDifficultyScale = config.Bind("ArtifactOfAusterity", "BudgetDifficultyScale", 0.4f,
                "Budget grows by this fraction of itself per point of difficulty coefficient.");

            Def = ScriptableObject.CreateInstance<ArtifactDef>();
            Def.cachedName = "ArtifactOfAusterity";
            Def.nameToken = "ARTIFACT_AUSTERITY_NAME";
            Def.descriptionToken = "ARTIFACT_AUSTERITY_DESC";
            Def.smallIconSelectedSprite = Assets.LoadSprite("AuditDepartment.icon_artifact_austerity_enabled.rgba", 128);
            Def.smallIconDeselectedSprite = Assets.LoadSprite("AuditDepartment.icon_artifact_austerity_disabled.rgba", 128);
            ContentAddition.AddArtifactDef(Def);

            LanguageAPI.Add("ARTIFACT_AUSTERITY_NAME", "Artifact of Austerity");
            LanguageAPI.Add("ARTIFACT_AUSTERITY_DESC",
                "Monster spawning runs on a fixed budget each stage: spending is faster while it lasts, then stops entirely.");
        }

        internal static bool Enabled =>
            Def && RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(Def);
    }
}
