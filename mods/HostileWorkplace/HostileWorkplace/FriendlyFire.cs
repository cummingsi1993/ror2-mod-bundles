using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HostileWorkplace
{
    // Slice 2: shapes player-vs-player damage during an Open Season window. BetrayalWindow
    // flips the engine's FriendlyFireManager on (so hits land) and pins its scale to 1.0;
    // this hook owns the actual feel:
    //   - scale player->player damage down to a fraction (duels last longer than a frame),
    //   - one-shot protection (a near-full victim can't be deleted in a single hit),
    //   - no minion/turret friendly fire onto players (direct player combat only).
    internal static class FriendlyFire
    {
        internal static ConfigEntry<float> PvpDamageScale;
        internal static ConfigEntry<float> OneShotProtectionHealthFraction;

        internal static void Init(ConfigFile config)
        {
            PvpDamageScale = config.Bind("FriendlyFire", "PvpDamageScale", 0.2f,
                "Player-vs-player damage during a window, as a fraction of the attacker's normal damage.");
            OneShotProtectionHealthFraction = config.Bind("FriendlyFire", "OneShotProtectionHealthFraction", 0.9f,
                "If your combined health is at least this fraction of max when a player hits you, that single hit can't kill you (leaves 1 HP). Set 1+ to require truly full, 0 to disable.");

            On.RoR2.HealthComponent.TakeDamage += OnTakeDamage;
        }

        private static bool IsPlayer(CharacterMaster master) =>
            master && master.playerCharacterMasterController;

        private static void OnTakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (NetworkServer.active && BetrayalWindow.IsOpen && damageInfo != null
                && !damageInfo.rejected && damageInfo.damage > 0f
                && self && self.body && self.body.teamComponent
                && self.body.teamComponent.teamIndex == TeamIndex.Player
                && IsPlayer(self.body.master))
            {
                var attackerBody = damageInfo.attacker ? damageInfo.attacker.GetComponent<CharacterBody>() : null;
                var attackerMaster = attackerBody ? attackerBody.master : null;

                if (IsPlayer(attackerMaster) && attackerMaster != self.body.master)
                {
                    // player -> player: the engine FF scale is pinned to 1.0, so a flat
                    // multiply lands the PvP fraction exactly.
                    damageInfo.damage *= Mathf.Max(0f, PvpDamageScale.Value);

                    float full = self.fullCombinedHealth;
                    float current = self.combinedHealth;
                    float threshold = OneShotProtectionHealthFraction.Value;
                    if (threshold > 0f && full > 0f && current >= threshold * full && damageInfo.damage >= current)
                    {
                        damageInfo.damage = Mathf.Max(1f, current - 1f);
                    }
                }
                else if (attackerBody && attackerBody.teamComponent
                    && attackerBody.teamComponent.teamIndex == TeamIndex.Player
                    && attackerMaster != self.body.master)
                {
                    // a teammate's (or your own) minion/turret/drone — no friendly fire onto
                    // players. Only direct player combat counts during a window.
                    damageInfo.damage = 0f;
                    damageInfo.rejected = true;
                }
            }
            orig(self, damageInfo);
        }
    }
}
