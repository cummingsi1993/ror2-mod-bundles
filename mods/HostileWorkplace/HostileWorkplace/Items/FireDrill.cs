using BepInEx.Configuration;
using HostileWorkplace.Artifacts;
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
    //
    // Easter egg: pull the alarm with no Artifact of Mutiny (no real "emergency") and it
    // backfires — a non-lethal self-hit for most of your health. Deliberately not mentioned
    // in the description.
    internal static class FireDrill
    {
        internal static EquipmentDef Def;
        internal static ConfigEntry<bool> BackfireEnabled;
        internal static ConfigEntry<float> BackfireHealthFraction;

        internal static void Init(ConfigFile config)
        {
            BackfireEnabled = config.Bind("FireDrill", "BackfireEnabled", true,
                "Easter egg: using the Fire Drill without Artifact of Mutiny active backfires on the user.");
            BackfireHealthFraction = config.Bind("FireDrill", "BackfireHealthFraction", 0.8f,
                "Fraction of max health the backfire deals (non-lethal — can't reduce you below 1 HP).");

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
                    if (ArtifactOfMutiny.Enabled)
                    {
                        // toggle: cancel an active window, otherwise start one
                        if (!BetrayalWindow.ForceClose())
                        {
                            BetrayalWindow.RequestOpen("Someone pulled the fire alarm.");
                        }
                    }
                    else if (BackfireEnabled.Value)
                    {
                        Backfire(self);
                    }
                }
                return true; // consume regardless so the cooldown is consistent across clients
            }
            return orig(self, equipmentDef);
        }

        // No emergency declared (artifact off) → the alarm backfires on whoever pulled it.
        // Non-lethal so it's a brutal prank, not an unfair instakill.
        private static void Backfire(EquipmentSlot self)
        {
            var body = self.characterBody;
            var health = body ? body.healthComponent : null;
            if (!health || !health.alive)
            {
                return;
            }
            var info = new DamageInfo
            {
                damage = health.fullCombinedHealth * Mathf.Clamp01(BackfireHealthFraction.Value),
                attacker = body.gameObject,
                inflictor = null,
                position = body.corePosition,
                procCoefficient = 0f,
                damageType = DamageType.NonLethal | DamageType.BypassArmor,
                damageColorIndex = DamageColorIndex.Default,
            };
            health.TakeDamage(info);
            Log.Info($"Fire Drill backfired on {Util.GetBestMasterName(body.master)} (no Artifact of Mutiny).");
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = $"<color=#ff5a5a>{Util.GetBestMasterName(body.master)} pulled the fire alarm with no emergency declared. Management is displeased.</color>"
            });
        }
    }
}
