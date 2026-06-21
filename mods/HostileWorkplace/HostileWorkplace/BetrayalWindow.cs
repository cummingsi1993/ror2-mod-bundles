using BepInEx.Configuration;
using HostileWorkplace.Artifacts;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HostileWorkplace
{
    // Server-authoritative "Open Season" state machine + telegraph.
    //
    // SLICE 1 (this file): scheduling, announcement, and the Open Season buff that flags
    // everyone on-screen. No combat yet — friendly fire (slice 2) and item theft (slice 3)
    // will read IsOpen. Windows only ever run while Artifact of Mutiny is enabled.
    //
    // Lifecycle: Idle --(trigger)--> Telegraph (countdown warning) --> Open (FF window) --> Idle.
    // Triggers: teleporter begins charging (default), an optional fixed interval, or an
    // on-demand RequestOpen()/ForceClose() that future equipment will call.
    internal static class BetrayalWindow
    {
        internal static BuffDef OpenSeasonBuff;
        internal static ConfigEntry<float> WindowDuration;
        internal static ConfigEntry<float> TelegraphSeconds;
        internal static ConfigEntry<bool> TriggerOnTeleporter;
        internal static ConfigEntry<float> TimedIntervalSeconds;

        internal static bool IsOpen { get; private set; }

        private enum Phase { Idle, Telegraph, Open }
        private static Phase phase = Phase.Idle;
        private static float phaseTimer;
        private static float timedAccum;

        internal static void Init(ConfigFile config)
        {
            WindowDuration = config.Bind("BetrayalWindow", "WindowDurationSeconds", 25f,
                "How long an Open Season window stays open (friendly fire + theft active).");
            TelegraphSeconds = config.Bind("BetrayalWindow", "TelegraphSeconds", 5f,
                "Warning time between the announcement and friendly fire turning on.");
            TriggerOnTeleporter = config.Bind("BetrayalWindow", "TriggerOnTeleporter", true,
                "Open a window when the teleporter begins charging.");
            TimedIntervalSeconds = config.Bind("BetrayalWindow", "TimedIntervalSeconds", 0f,
                "If greater than 0, also open a window every N seconds of stage time. 0 disables.");

            OpenSeasonBuff = ScriptableObject.CreateInstance<BuffDef>();
            OpenSeasonBuff.name = "HostileWorkplaceOpenSeason";
            OpenSeasonBuff.buffColor = new Color(0.9f, 0.18f, 0.18f);
            OpenSeasonBuff.canStack = false;
            OpenSeasonBuff.isDebuff = false;
            OpenSeasonBuff.iconSprite = Assets.LoadSprite("HostileWorkplace.icon_buff_open_season.rgba", 128);
            ContentAddition.AddBuffDef(OpenSeasonBuff);

            TeleporterInteraction.onTeleporterBeginChargingGlobal += OnTeleporterBeginCharging;
            Stage.onServerStageBegin += _ => ResetState();
        }

        private static void ResetState()
        {
            phase = Phase.Idle;
            phaseTimer = 0f;
            timedAccum = 0f;
            IsOpen = false;
        }

        private static void OnTeleporterBeginCharging(TeleporterInteraction tp)
        {
            if (NetworkServer.active && TriggerOnTeleporter.Value)
            {
                Schedule("The teleporter hums to life. Last chance to make a move.");
            }
        }

        // Begin the telegraph countdown toward an open window. No-op if a window is already
        // scheduled/running or the artifact is off.
        internal static void Schedule(string reason)
        {
            if (!NetworkServer.active || phase != Phase.Idle || !ArtifactOfMutiny.Enabled)
            {
                return;
            }
            phase = Phase.Telegraph;
            phaseTimer = Mathf.Max(0.1f, TelegraphSeconds.Value);
            Announce($"<color=#ff5a5a>⚠ OPEN SEASON in {Mathf.CeilToInt(phaseTimer)}s — friendly fire incoming.</color>");
            if (!string.IsNullOrEmpty(reason))
            {
                Announce($"<color=#c98a8a>{reason}</color>");
            }
        }

        // Driven each FixedUpdate by WindowRunner (server only).
        internal static void Tick(float dt)
        {
            if (!NetworkServer.active || !Run.instance)
            {
                return;
            }
            if (phase == Phase.Idle)
            {
                if (ArtifactOfMutiny.Enabled && TimedIntervalSeconds.Value > 0f)
                {
                    timedAccum += dt;
                    if (timedAccum >= TimedIntervalSeconds.Value)
                    {
                        timedAccum = 0f;
                        Schedule("A scheduled performance review begins.");
                    }
                }
                return;
            }

            phaseTimer -= dt;
            if (phase == Phase.Telegraph)
            {
                if (phaseTimer <= 0f)
                {
                    Open();
                }
            }
            else if (phase == Phase.Open)
            {
                RefreshBuffs(); // keep the flag on respawners / late joiners
                if (phaseTimer <= 0f)
                {
                    Close();
                }
            }
        }

        private static void Open()
        {
            phase = Phase.Open;
            phaseTimer = Mathf.Max(1f, WindowDuration.Value);
            IsOpen = true;
            RefreshBuffs();
            Announce($"<color=#ff2a2a>🔪 OPEN SEASON — friendly fire is ON for {Mathf.RoundToInt(WindowDuration.Value)}s!</color>");
        }

        private static void Close()
        {
            phase = Phase.Idle;
            phaseTimer = 0f;
            IsOpen = false;
            Announce("<color=#6ac77f>Truce restored. Back to work, everyone.</color>");
        }

        // On-demand control surface for future equipment (slice 4).
        internal static bool RequestOpen(string reason = null)
        {
            if (phase != Phase.Idle)
            {
                return false;
            }
            Schedule(reason ?? "A hostile takeover has been initiated.");
            return phase == Phase.Telegraph;
        }

        internal static bool ForceClose()
        {
            if (phase == Phase.Idle)
            {
                return false;
            }
            Close();
            return true;
        }

        private static void RefreshBuffs()
        {
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                var master = pcmc ? pcmc.master : null;
                var body = master ? master.GetBody() : null;
                if (body && !body.HasBuff(OpenSeasonBuff))
                {
                    body.AddTimedBuff(OpenSeasonBuff, Mathf.Max(2f, phaseTimer + 1f));
                }
            }
        }

        private static void Announce(string message)
        {
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = message });
        }
    }
}
