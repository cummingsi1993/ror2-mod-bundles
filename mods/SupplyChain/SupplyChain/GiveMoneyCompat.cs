using MonoMod.RuntimeDetour;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SupplyChain
{
    // RoR2 1.5.0 added CharacterMaster.GiveMoney(uint, bool bypassStatManager). HookGen
    // then renames the hook for the old method, so On.RoR2.CharacterMaster.GiveMoney no
    // longer binds, and every vanilla gold source now calls the 2-argument overload
    // directly (GiveMoney(uint) is just a forwarder with no vanilla callers). Hook the
    // 2-argument method by reflection, falling back to the 1-argument one on older game
    // builds. Never hook both: our own 1-argument payouts would pass through twice.
    // Like On. hooks, a hook added later runs outermost.
    internal static class GiveMoneyCompat
    {
        internal delegate void Orig(CharacterMaster self, uint amount, bool bypassStatManager);
        internal delegate void Handler(Orig orig, CharacterMaster self, uint amount, bool bypassStatManager);
        private delegate void OrigLegacy(CharacterMaster self, uint amount);

        // keep the hooks referenced so they are never collected
        private static readonly List<Hook> hooks = new List<Hook>();

        internal static void Add(Handler handler)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var current = typeof(CharacterMaster).GetMethod("GiveMoney", flags, null, new[] { typeof(uint), typeof(bool) }, null);
            if (current != null)
            {
                hooks.Add(new Hook(current, handler));
                return;
            }
            var legacy = typeof(CharacterMaster).GetMethod("GiveMoney", flags, null, new[] { typeof(uint) }, null);
            if (legacy == null)
            {
                Log.Error("CharacterMaster.GiveMoney not found; gold income hooks are disabled.");
                return;
            }
            Action<OrigLegacy, CharacterMaster, uint> adapter =
                (orig, self, amount) => handler((master, value, _) => orig(master, value), self, amount, false);
            hooks.Add(new Hook(legacy, adapter));
        }
    }
}
