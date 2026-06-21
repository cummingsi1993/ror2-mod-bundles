using R2API;
using RoR2;
using UnityEngine;

namespace HostileWorkplace.Artifacts
{
    // The master switch for the whole mod. While enabled, BetrayalWindow opens periodic
    // "Open Season" windows during which friendly fire and item theft turn on. Everything
    // else in HostileWorkplace checks Enabled, so with the artifact off the mod is inert.
    internal static class ArtifactOfMutiny
    {
        internal static ArtifactDef Def;

        internal static void Init()
        {
            Def = ScriptableObject.CreateInstance<ArtifactDef>();
            Def.cachedName = "ArtifactOfMutiny";
            Def.nameToken = "ARTIFACT_MUTINY_NAME";
            Def.descriptionToken = "ARTIFACT_MUTINY_DESC";
            Def.smallIconSelectedSprite = Assets.LoadSprite("HostileWorkplace.icon_artifact_mutiny_enabled.rgba", 128);
            Def.smallIconDeselectedSprite = Assets.LoadSprite("HostileWorkplace.icon_artifact_mutiny_disabled.rgba", 128);
            ContentAddition.AddArtifactDef(Def);

            LanguageAPI.Add("ARTIFACT_MUTINY_NAME", "Artifact of Mutiny");
            LanguageAPI.Add("ARTIFACT_MUTINY_DESC",
                "Periodically declares Open Season: friendly fire turns on and killing a teammate steals a cut of their items. Truce the rest of the time.");
        }

        internal static bool Enabled =>
            Def && RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(Def);
    }
}
