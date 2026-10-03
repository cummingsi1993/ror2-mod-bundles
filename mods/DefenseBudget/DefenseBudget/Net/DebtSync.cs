using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace DefenseBudget.Net
{
    // What a client knows about one player's Defense Budget credit.
    internal struct DebtView
    {
        public int debt;
        public int limit;
        public bool defaulted;
    }

    // Debt only exists on the server (DebtTracker), so without this every non-host
    // player's HUD showed 0 gold while they were deep in deficit. The server broadcasts
    // a full snapshot of every indebted / item-holding player; full snapshots are
    // self-healing, so a dropped packet (the channel is unreliable) or a mid-run joiner
    // is corrected by the next heartbeat without any join/stage bookkeeping.
    internal static class DebtSync
    {
        private const float MinSendInterval = 0.25f;
        private const float HeartbeatInterval = 2f;

        // keyed by CharacterMaster.netId.Value (identical on host and clients)
        private static readonly Dictionary<uint, DebtView> views = new Dictionary<uint, DebtView>();
        private static readonly DebtSnapshotMessage outgoing = new DebtSnapshotMessage();
        private static float sinceLastSend;
        private static int lastHash;
        private static bool lastEmpty = true;

        internal static void Init()
        {
            NetworkingAPI.RegisterMessageType<DebtSnapshotMessage>();
            // sender state only; client views must survive until the run is destroyed,
            // since a joiner's first snapshot can arrive before its Run.Start
            Run.onRunStartGlobal += _ =>
            {
                sinceLastSend = HeartbeatInterval;
                lastHash = 0;
                lastEmpty = true;
            };
            Run.onRunDestroyGlobal += _ => views.Clear();
        }

        internal static bool TryGet(CharacterMaster master, out DebtView view)
        {
            view = default;
            return master && views.TryGetValue(master.netId.Value, out view);
        }

        internal static void ApplySnapshot(List<DebtSnapshotMessage.Entry> entries)
        {
            views.Clear();
            foreach (var e in entries)
            {
                views[e.masterId.Value] = new DebtView { debt = e.debt, limit = e.limit, defaulted = e.defaulted };
            }
        }

        // Server only, once per FixedUpdate. `fill` appends the current state of every
        // player worth reporting. Sends on change (throttled) plus a heartbeat.
        internal static void ServerTick(float deltaTime, System.Action<List<DebtSnapshotMessage.Entry>> fill)
        {
            sinceLastSend += deltaTime;
            outgoing.entries.Clear();
            fill(outgoing.entries);

            int hash = 17;
            foreach (var e in outgoing.entries)
            {
                hash = hash * 31 + (int)e.masterId.Value;
                hash = hash * 31 + e.debt;
                hash = hash * 31 + e.limit;
                hash = hash * 31 + (e.defaulted ? 1 : 0);
            }
            bool empty = outgoing.entries.Count == 0;
            bool changed = hash != lastHash || empty != lastEmpty;
            // R2API sends on RoR2's unreliable 'time' channel, so the heartbeat runs even
            // when empty — otherwise a dropped "all debts cleared" snapshot would leave a
            // client showing a stale deficit for the rest of the run.
            bool due = sinceLastSend >= (changed ? MinSendInterval : HeartbeatInterval);
            if (!due)
            {
                return;
            }
            // On the host this also runs OnReceived locally, so the host's HUD reads the
            // same views as everyone else's.
            outgoing.Send(NetworkDestination.Clients);
            sinceLastSend = 0f;
            lastHash = hash;
            lastEmpty = empty;
        }
    }

    internal sealed class DebtSnapshotMessage : INetMessage
    {
        internal struct Entry
        {
            public NetworkInstanceId masterId;
            public int debt;
            public int limit;
            public bool defaulted;
        }

        // R2API reuses one instance per message type for everything it receives, so
        // Deserialize must reset all state.
        internal readonly List<Entry> entries = new List<Entry>();

        public void Serialize(NetworkWriter writer)
        {
            writer.WritePackedUInt32((uint)entries.Count);
            foreach (var e in entries)
            {
                writer.Write(e.masterId);
                writer.WritePackedUInt32((uint)e.debt);
                writer.WritePackedUInt32((uint)e.limit);
                writer.Write(e.defaulted);
            }
        }

        public void Deserialize(NetworkReader reader)
        {
            entries.Clear();
            uint count = reader.ReadPackedUInt32();
            for (uint i = 0; i < count; i++)
            {
                entries.Add(new Entry
                {
                    masterId = reader.ReadNetworkId(),
                    debt = (int)reader.ReadPackedUInt32(),
                    limit = (int)reader.ReadPackedUInt32(),
                    defaulted = reader.ReadBoolean(),
                });
            }
        }

        public void OnReceived()
        {
            DebtSync.ApplySnapshot(entries);
        }
    }
}
