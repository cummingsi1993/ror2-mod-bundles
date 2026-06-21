using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HostileWorkplace.Items
{
    // Slice 4: equipment that toggles a betrayal window on demand. If no window is active,
    // using it starts one (catch your "friends" off guard); if one is active or telegraphing,
    // using it cancels it (call an emergency truce when you're losing). Only meaningful with
    // Artifact of Mutiny enabled — BetrayalWindow gates on it. Activation is server-resolved
    // via the PerformEquipmentAction hook.
    internal static class FireDrill
    {
        internal static EquipmentDef Def;

        internal static void Init()
        {
            Def = ScriptableObject.CreateInstance<EquipmentDef>();
            Def.name = "FireDrill";
            Def.nameToken = "FIRE_DRILL_NAME";
            Def.pickupToken = "FIRE_DRILL_PICKUP";
            Def.descriptionToken = "FIRE_DRILL_DESC";
            Def.loreToken = "FIRE_DRILL_LORE";
            Def.cooldown = 60f;
            Def.canDrop = true;
            Def.enigmaCompatible = false;
            Def.canBeRandomlyTriggered = false;
            Def.isLunar = false;
            Def.appearsInSinglePlayer = false; // pointless solo (no one to betray)
            Def.appearsInMultiPlayer = true;
            Def.pickupIconSprite = Assets.LoadSprite("HostileWorkplace.icon_fire_drill.rgba", 128);
            Def.pickupModelPrefab = Assets.CreatePickupModel(
                "PickupFireDrill", "HostileWorkplace.models.fire_drill.obj", "HostileWorkplace.models.fire_drill.rgba", 512, 0.5f);
            ItemAPI.Add(new CustomEquipment(Def, new ItemDisplayRuleDict(null)));

            LanguageAPI.Add("FIRE_DRILL_NAME", "Fire Drill");
            LanguageAPI.Add("FIRE_DRILL_PICKUP", "Pull the alarm: start an Open Season window — or cancel one in progress.");
            LanguageAPI.Add("FIRE_DRILL_DESC",
                "Activate to <style=cIsUtility>start a betrayal window</style> if none is active, or " +
                "<style=cIsUtility>cancel</style> one that is. Requires <style=cIsVoid>Artifact of Mutiny</style>.");
            LanguageAPI.Add("FIRE_DRILL_LORE",
                "Standard procedure dictates that in the event of an emergency, all personnel evacuate in an orderly fashion. Standard procedure does not anticipate that one of the personnel pulled the alarm specifically so the others would file calmly into the open.");

            On.RoR2.EquipmentSlot.PerformEquipmentAction += OnPerformEquipmentAction;
        }

        private static bool OnPerformEquipmentAction(
            On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig,
            EquipmentSlot self, EquipmentDef equipmentDef)
        {
            if (equipmentDef == Def)
            {
                if (NetworkServer.active)
                {
                    // toggle: cancel an active window, otherwise start one
                    if (!BetrayalWindow.ForceClose())
                    {
                        BetrayalWindow.RequestOpen("Someone pulled the fire alarm.");
                    }
                }
                return true; // consume regardless so the cooldown is consistent across clients
            }
            return orig(self, equipmentDef);
        }
    }
}
