using RoR2;
using System;
using System.Reflection;

namespace AuditDepartment
{
    // The GameLibs reference assemblies are publicized and lag the installed game, so
    // some CombatDirector members that look public at compile time are private at
    // runtime (currentMonsterCardCost), and some runtime members are missing from the
    // refs entirely (GetFinalMonsterCardSelection). Everything risky funnels through
    // here, resolved by reflection once.
    internal static class DirectorCompat
    {
        private static FieldInfo currentMonsterCardCostField;
        private static bool costResolved;

        // Cost of the spawn the director is currently trying to place, or -1 if the
        // field can't be found on this game build.
        internal static int GetCurrentCardCost(CombatDirector director)
        {
            if (!costResolved)
            {
                costResolved = true;
                currentMonsterCardCostField = typeof(CombatDirector).GetField(
                    "currentMonsterCardCost",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (currentMonsterCardCostField == null)
                {
                    Log.Warning("CombatDirector.currentMonsterCardCost not found; Line-Item Veto disabled.");
                }
            }
            return currentMonsterCardCostField != null
                ? (int)currentMonsterCardCostField.GetValue(director)
                : -1;
        }
    }
}
