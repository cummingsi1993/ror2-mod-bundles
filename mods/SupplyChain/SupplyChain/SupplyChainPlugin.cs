using BepInEx;
using R2API;
using RoR2;
using SupplyChain.Artifacts;
using SupplyChain.Items;
using System.Collections.Generic;

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
        public const string PluginVersion = "1.0.3";

        public void Awake()
        {
            Log.Init(Logger);

            // each feature isolated: a game patch that breaks one hook (1.5.0 broke
            // Pyramid Scheme's) must not take the rest of the bundle down with it
            Safe("Bulk Order", () => BulkOrder.Init(Config));
            Safe("Loaded Dice", () => LoadedDice.Init(Config));
            Safe("Standing Order", () => StandingOrder.Init(Config));
            Safe("Force Multiplier", () => ForceMultiplier.Init(Config));
            Safe("Pyramid Scheme", () => PyramidScheme.Init(Config));
            Safe("Purchase Order", () => PurchaseOrder.Init(Config));
            Safe("Dropshipping", () => Dropshipping.Init(Config));
            Safe("Recall Notice", () => RecallNotice.Init(Config));
            Safe("Artifact of Diversification", () => ArtifactOfDiversification.Init(Config));
            Safe("chest hooks", ChestHooks.Init);

            // index sets used by drop filtering and stack amplification, resolvable only
            // after catalogs load
            RoR2Application.onLoad += BuildIndexSets;

            Log.Info($"{PluginName} loaded.");
        }

        private static void Safe(string feature, System.Action init)
        {
            try
            {
                init();
            }
            catch (System.Exception e)
            {
                Log.Error($"{feature} failed to initialize and is disabled: {e}");
            }
        }

        // Standard chest contents. 1.5.0 adds Cursed/Page/Hex tiers (DLC4); chest bonuses
        // and command choices only apply to these.
        internal static bool IsStandardTier(ItemTier tier)
        {
            return tier == ItemTier.Tier1 || tier == ItemTier.Tier2 || tier == ItemTier.Tier3 || tier == ItemTier.Boss;
        }

        // Tiers Pyramid Scheme and Dropshipping may copy: standard, lunar and void.
        internal static bool IsCopyableTier(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Tier1:
                case ItemTier.Tier2:
                case ItemTier.Tier3:
                case ItemTier.Boss:
                case ItemTier.Lunar:
                case ItemTier.VoidTier1:
                case ItemTier.VoidTier2:
                case ItemTier.VoidTier3:
                case ItemTier.VoidBoss:
                    return true;
                default:
                    return false;
            }
        }

        // Pickup indices of this bundle's items — bonus drops never roll these (the
        // Fuzzy Dice lesson: generated items must exclude their generators).
        internal static readonly HashSet<PickupIndex> BundlePickups = new HashSet<PickupIndex>();

        // Items eligible for Force Multiplier amplification and Standing Order restock:
        // standard droppable tiers only, never lunar/void/hidden/NoTier.
        internal static readonly HashSet<ItemIndex> AmplifiableItems = new HashSet<ItemIndex>();

        private static void BuildIndexSets()
        {
            foreach (var def in new[] { BulkOrder.Def, LoadedDice.Def, StandingOrder.Def, ForceMultiplier.Def, PyramidScheme.Def, PurchaseOrder.Def, Dropshipping.Def, RecallNotice.Def })
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
    }
}
