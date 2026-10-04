using RoR2;
using System;
using System.Reflection;

namespace SupplyChain
{
    // GenericPickupController.pickupIndex is a public FIELD in the GameLibs reference
    // assemblies (1.3.9) but a PROPERTY in the installed game (1.4.x). Code that writes
    // `controller.pickupIndex` compiles to ldfld and throws MissingFieldException at
    // runtime. Resolve whichever exists, once, by reflection.
    internal static class PickupCompat
    {
        private static Func<GenericPickupController, PickupIndex> getPickupIndex;
        private static bool resolved;

        internal static PickupIndex GetPickupIndex(GenericPickupController controller)
        {
            if (!resolved)
            {
                resolved = true;
                getPickupIndex = Resolve();
            }
            return getPickupIndex != null ? getPickupIndex(controller) : PickupIndex.none;
        }

        private static Func<GenericPickupController, PickupIndex> Resolve()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var getter = typeof(GenericPickupController).GetProperty("pickupIndex", flags)?.GetGetMethod();
            if (getter != null && getter.ReturnType == typeof(PickupIndex))
            {
                return (Func<GenericPickupController, PickupIndex>)getter.CreateDelegate(typeof(Func<GenericPickupController, PickupIndex>));
            }
            var field = typeof(GenericPickupController).GetField("pickupIndex", flags);
            if (field != null && field.FieldType == typeof(PickupIndex))
            {
                return controller => (PickupIndex)field.GetValue(controller);
            }
            Log.Warning("GenericPickupController.pickupIndex not found; Dropshipping cannot identify pickups.");
            return null;
        }
    }
}
