using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2;
using SupplyChain.Artifacts;
using SupplyChain.Items;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace SupplyChain
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class SupplyChainPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Isaac_Cummings";
        public const string PluginName = "SupplyChain";
        public const string PluginVersion = "1.0.0";

        public static ConfigEntry<KeyboardShortcut> DebugSpawnPackKey;

        public void Awake()
        {
            Log.Init(Logger);

            DebugSpawnPackKey = Config.Bind("Debug", "SpawnPackKey", new KeyboardShortcut(KeyCode.F6),
                "Drops one of each SupplyChain item for testing (host only). Set to an empty shortcut to disable.");

            BulkOrder.Init(Config);
            LoadedDice.Init(Config);
            StandingOrder.Init(Config);
            ForceMultiplier.Init(Config);
            PyramidScheme.Init(Config);
            RecallNotice.Init(Config);
            ArtifactOfDiversification.Init(Config);
            ChestHooks.Init();

            // index sets used by drop filtering and stack amplification, resolvable only
            // after catalogs load
            RoR2Application.onLoad += BuildIndexSets;

            Log.Info($"{PluginName} loaded.");
        }

        // Pickup indices of this bundle's items — bonus drops never roll these (the
        // Fuzzy Dice lesson: generated items must exclude their generators).
        internal static readonly HashSet<PickupIndex> BundlePickups = new HashSet<PickupIndex>();

        // Items eligible for Force Multiplier amplification and Standing Order restock:
        // standard droppable tiers only, never lunar/void/hidden/NoTier.
        internal static readonly HashSet<ItemIndex> AmplifiableItems = new HashSet<ItemIndex>();

        private static void BuildIndexSets()
        {
            foreach (var def in new[] { BulkOrder.Def, LoadedDice.Def, StandingOrder.Def, ForceMultiplier.Def, PyramidScheme.Def, RecallNotice.Def })
            {
                if (def && def.itemIndex != ItemIndex.None)
                {
                    var pickup = PickupCatalog.FindPickupIndex(def.itemIndex);
                    if (pickup != PickupIndex.none)
                    {
                        BundlePickups.Add(pickup);
                    }
                }
            }
            for (int i = 0; i < ItemCatalog.itemCount; i++)
            {
                var index = (ItemIndex)i;
                var def = ItemCatalog.GetItemDef(index);
                if (def && !def.hidden
                    && (def.tier == ItemTier.Tier1 || def.tier == ItemTier.Tier2 || def.tier == ItemTier.Tier3 || def.tier == ItemTier.Boss)
                    && def != ForceMultiplier.Def && def != StandingOrder.Def)
                {
                    AmplifiableItems.Add(index);
                }
            }
            Log.Info($"SupplyChain index sets: {BundlePickups.Count} bundle pickups, {AmplifiableItems.Count} amplifiable items.");
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
            Log.Info("Debug: spawning SupplyChain pack");
            var forward = body.gameObject.transform.forward;
            var defs = new[] { BulkOrder.Def, LoadedDice.Def, StandingOrder.Def, ForceMultiplier.Def, PyramidScheme.Def, RecallNotice.Def };
            for (int i = 0; i < defs.Length; i++)
            {
                var direction = Quaternion.AngleAxis(-37.5f + 15f * i, Vector3.up) * forward;
                PickupDropletController.CreatePickupDroplet(
                    PickupCatalog.FindPickupIndex(defs[i].itemIndex),
                    body.corePosition + Vector3.up * 1.5f,
                    direction * 10f);
            }
        }
    }
}
