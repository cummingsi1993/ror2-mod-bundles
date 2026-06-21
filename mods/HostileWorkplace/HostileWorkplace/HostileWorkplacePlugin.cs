using BepInEx;
using BepInEx.Configuration;
using HostileWorkplace.Artifacts;
using HostileWorkplace.Items;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HostileWorkplace
{
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class HostileWorkplacePlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Isaac_Cummings";
        public const string PluginName = "HostileWorkplace";
        public const string PluginVersion = "1.0.0";

        public static ConfigEntry<KeyboardShortcut> DebugForceWindowKey;

        public void Awake()
        {
            Log.Init(Logger);

            // F8: SupplyChain is F6, AuditDepartment F7 — keep them co-installable.
            DebugForceWindowKey = Config.Bind("Debug", "ForceWindowKey", new KeyboardShortcut(KeyCode.F8),
                "Force-open a betrayal window for testing (host only; requires Artifact of Mutiny). Empty shortcut disables.");

            ArtifactOfMutiny.Init();
            BetrayalWindow.Init(Config);
            FriendlyFire.Init(Config);
            ItemTheft.Init(Config);
            FireDrill.Init();

            // server-side ticker for the window state machine
            var runner = new GameObject("HostileWorkplaceRunner");
            DontDestroyOnLoad(runner);
            runner.hideFlags = HideFlags.HideAndDontSave;
            runner.AddComponent<WindowRunner>();

            Log.Info($"{PluginName} loaded.");
        }

        private void Update()
        {
            if (DebugForceWindowKey.Value.IsDown() && NetworkServer.active && Run.instance)
            {
                if (!ArtifactOfMutiny.Enabled)
                {
                    Log.Info("Debug: Artifact of Mutiny is not enabled; window request ignored.");
                    return;
                }
                Log.Info("Debug: forcing a betrayal window.");
                BetrayalWindow.RequestOpen("A manager has called an impromptu meeting.");
            }
        }
    }

    // Drives the window state machine on the server clock.
    internal class WindowRunner : MonoBehaviour
    {
        private void FixedUpdate()
        {
            BetrayalWindow.Tick(Time.fixedDeltaTime);
        }
    }
}
