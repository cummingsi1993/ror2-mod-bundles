using BepInEx;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace SilentPartner
{
    // Personal, unlisted easter egg — a "silent partner" quietly takes a stake in
    // everyone else's holdings. Type the secret word (default BAILOUT) and every other
    // player's items are COPIED onto your character. Copying (not stealing) means nothing
    // disappears from their side, so the table doesn't notice until someone wonders why
    // the host has three of everything.
    //
    // Host only (items are server-authoritative). Silent: writes to the BepInEx log, never
    // to chat. NOT published — see the csproj note.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class SilentPartnerPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "Isaac_Cummings.SilentPartner";
        public const string PluginName = "SilentPartner";
        public const string PluginVersion = "1.0.0";

        // The secret incantation. Distinct letters, none of them WASD, so it can't be
        // fat-fingered mid-fight — the sequence resets the instant any other key is pressed.
        private static readonly KeyCode[] Sequence =
        {
            KeyCode.B, KeyCode.A, KeyCode.I, KeyCode.L, KeyCode.O, KeyCode.U, KeyCode.T,
        };
        private const float ResetAfterSeconds = 2.5f;

        private int seqIndex;
        private float lastKeyTime;

        private void Update()
        {
            if (!Input.anyKeyDown)
            {
                return;
            }
            float now = Time.unscaledTime;
            if (seqIndex > 0 && now - lastKeyTime > ResetAfterSeconds)
            {
                seqIndex = 0;
            }
            if (Input.GetKeyDown(Sequence[seqIndex]))
            {
                seqIndex++;
                lastKeyTime = now;
                if (seqIndex >= Sequence.Length)
                {
                    seqIndex = 0;
                    Settle();
                }
            }
            else
            {
                // any wrong key drops the streak (but might itself start a fresh attempt)
                seqIndex = Input.GetKeyDown(Sequence[0]) ? 1 : 0;
                if (seqIndex == 1)
                {
                    lastKeyTime = now;
                }
            }
        }

        // Copy every other player's items onto the local player.
        private void Settle()
        {
            if (!NetworkServer.active)
            {
                Logger.LogInfo("SilentPartner: not the host — items are server-authoritative, nothing to do.");
                return;
            }
            if (!Run.instance)
            {
                return;
            }
            var recipient = LocalUserManager.GetFirstLocalUser()?.cachedMaster;
            if (!recipient || !recipient.inventory)
            {
                return;
            }

            int copiedItems = 0;
            int fromPlayers = 0;
            foreach (var controller in PlayerCharacterMasterController.instances)
            {
                var other = controller ? controller.master : null;
                if (!other || other == recipient || !other.inventory)
                {
                    continue;
                }
                // snapshot first: GiveItem fires inventory-changed events, and we never want
                // to be iterating a list mid-mutation
                var owned = new List<ItemIndex>(other.inventory.itemAcquisitionOrder);
                bool tookAny = false;
                foreach (var index in owned)
                {
                    var def = ItemCatalog.GetItemDef(index);
                    if (!def || def.hidden || def.tier == ItemTier.NoTier)
                    {
                        continue; // skip internal/count-based hidden items
                    }
                    int count = other.inventory.GetItemCount(index);
                    if (count > 0)
                    {
                        recipient.inventory.GiveItem(index, count);
                        copiedItems += count;
                        tookAny = true;
                    }
                }
                if (tookAny)
                {
                    fromPlayers++;
                }
            }
            Logger.LogInfo($"SilentPartner: copied {copiedItems} item(s) from {fromPlayers} other player(s) to {Util.GetBestMasterName(recipient)}.");
        }
    }
}
