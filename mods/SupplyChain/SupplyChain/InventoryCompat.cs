using RoR2;
using System;
using System.Reflection;

namespace SupplyChain
{
    // The GameLibs reference assemblies lag the installed game: GetItemCountPermanent
    // exists at runtime (game 1.4.x) but not at compile time (refs 1.3.9). Resolve it by
    // reflection once, falling back to GetItemCount on older game builds.
    internal static class InventoryCompat
    {
        private static Func<Inventory, ItemIndex, int> getPermanent;
        private static bool resolved;

        internal static int GetPermanentCount(Inventory inventory, ItemIndex itemIndex)
        {
            if (!resolved)
            {
                resolved = true;
                var method = typeof(Inventory).GetMethod(
                    "GetItemCountPermanent",
                    BindingFlags.Public | BindingFlags.Instance,
                    null, new[] { typeof(ItemIndex) }, null);
                if (method != null)
                {
                    getPermanent = (Func<Inventory, ItemIndex, int>)method.CreateDelegate(typeof(Func<Inventory, ItemIndex, int>));
                }
                else
                {
                    Log.Warning("Inventory.GetItemCountPermanent not found; falling back to GetItemCount.");
                }
            }
            return getPermanent != null ? getPermanent(inventory, itemIndex) : inventory.GetItemCount(itemIndex);
        }
    }
}
