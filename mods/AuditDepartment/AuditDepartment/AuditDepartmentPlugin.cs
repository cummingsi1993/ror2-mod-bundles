using AuditDepartment.Artifacts;
using AuditDepartment.Items;
using BepInEx;
using R2API;
using RoR2;

namespace AuditDepartment
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class AuditDepartmentPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Isaac_Cummings";
        public const string PluginName = "AuditDepartment";
        public const string PluginVersion = "1.0.1";

        public void Awake()
        {
            Log.Init(Logger);

            RedTape.Init(Config);
            LineItemVeto.Init(Config);
            HostileTakeover.Init(Config);
            StimulusPackage.Init(Config);
            OffTheBooks.Init(Config); // after RedTape: needs RedTape.Def for the corruption pairing
            ArtifactOfAusterity.Init(Config);
            DirectorHooks.Init();

            Log.Info($"{PluginName} loaded.");
        }
    }
}
