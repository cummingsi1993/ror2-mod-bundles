using AuditDepartment.Artifacts;
using AuditDepartment.Items;
using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

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
        public const string PluginVersion = "1.0.0";

        public static ConfigEntry<KeyboardShortcut> DebugSpawnPackKey;

        public void Awake()
        {
            Log.Init(Logger);

            // F7: SupplyChain's debug key is F6, keep them co-installable
            DebugSpawnPackKey = Config.Bind("Debug", "SpawnPackKey", new KeyboardShortcut(KeyCode.F7),
                "Drops one of each AuditDepartment item for testing (host only). Set to an empty shortcut to disable.");

            RedTape.Init(Config);
            LineItemVeto.Init(Config);
            HostileTakeover.Init(Config);
            StimulusPackage.Init(Config);
            OffTheBooks.Init(Config); // after RedTape: needs RedTape.Def for the corruption pairing
            ArtifactOfAusterity.Init(Config);
            DirectorHooks.Init();

            Log.Info($"{PluginName} loaded.");
        }

        private void Update()
        {
            if (!DebugSpawnPackKey.Value.IsDown() || !NetworkServer.active || !Run.instance)
            {
                return;
            }
            var body = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            if (!body)
            {
                return;
            }
            Log.Info("Debug: spawning AuditDepartment pack");
            var forward = body.gameObject.transform.forward;
            var defs = new[] { RedTape.Def, LineItemVeto.Def, HostileTakeover.Def, StimulusPackage.Def, OffTheBooks.Def };
            for (int i = 0; i < defs.Length; i++)
            {
                var direction = Quaternion.AngleAxis(-30f + 15f * i, Vector3.up) * forward;
                PickupDropletController.CreatePickupDroplet(
                    PickupCatalog.FindPickupIndex(defs[i].itemIndex),
                    body.corePosition + Vector3.up * 1.5f,
                    direction * 10f);
            }
        }
    }
}
