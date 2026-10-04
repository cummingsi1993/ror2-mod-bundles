using BepInEx;
using BepInEx.Configuration;
using RoR2;
using RoR2.Artifacts;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace DevTools
{
    // LOCAL-ONLY testing cheats (lives in private/, never published). Host-only hotkeys
    // spawn an Artifact of Command choice cube offering every item of one tier that this
    // run can drop — vanilla and every bundle's items alike — so any item can be tested
    // without per-bundle debug code shipping to players.
    [BepInPlugin("Isaac_Cummings.DevTools", "DevTools", "1.0.0")]
    public class DevToolsPlugin : BaseUnityPlugin
    {
        private struct TierCube
        {
            public string name;
            public ConfigEntry<KeyboardShortcut> key;
            public Func<Run, IEnumerable<PickupIndex>> pickups;
        }

        private readonly List<TierCube> cubes = new List<TierCube>();

        public void Awake()
        {
            AddCube("White", KeyCode.F5, run => run.availableTier1DropList);
            AddCube("Green", KeyCode.F6, run => run.availableTier2DropList);
            AddCube("Red", KeyCode.F7, run => run.availableTier3DropList);
            // F8 is left free: HostileWorkplace's dev builds use it
            AddCube("Boss", KeyCode.F9, run => run.availableBossDropList);
            AddCube("Lunar", KeyCode.F10, run => run.availableLunarItemDropList);
            AddCube("Void", KeyCode.F11, run => Concat(run.availableVoidTier1DropList, run.availableVoidTier2DropList,
                run.availableVoidTier3DropList, run.availableVoidBossDropList));
            Logger.LogInfo("DevTools loaded (local-only): F5 white, F6 green, F7 red, F9 boss, F10 lunar, F11 void command cubes.");
        }

        private void AddCube(string name, KeyCode defaultKey, Func<Run, IEnumerable<PickupIndex>> pickups)
        {
            cubes.Add(new TierCube
            {
                name = name,
                key = Config.Bind("CommandCubes", name, new KeyboardShortcut(defaultKey),
                    $"Spawns a command choice cube with every {name.ToLowerInvariant()} item the run can drop (host only)."),
                pickups = pickups,
            });
        }

        private static IEnumerable<PickupIndex> Concat(params List<PickupIndex>[] lists)
        {
            foreach (var list in lists)
            {
                if (list == null)
                {
                    continue;
                }
                foreach (var pickup in list)
                {
                    yield return pickup;
                }
            }
        }

        private void Update()
        {
            foreach (var cube in cubes)
            {
                if (cube.key.Value.IsDown())
                {
                    SpawnCube(cube);
                }
            }
        }

        private void SpawnCube(TierCube cube)
        {
            if (!NetworkServer.active || !Run.instance)
            {
                return;
            }
            var body = LocalUserManager.GetFirstLocalUser()?.cachedBody;
            var prefab = CommandArtifactManager.commandCubePrefab;
            if (!body || !prefab)
            {
                return;
            }
            var pickups = new List<PickupIndex>(cube.pickups(Run.instance));
            if (pickups.Count == 0)
            {
                Logger.LogWarning($"No {cube.name} items available in this run.");
                return;
            }
            // body.transform is a private cached field at runtime; go through the GameObject
            var forward = body.gameObject.transform.forward;
            var position = body.corePosition + forward * 3f + Vector3.up;
            var instance = Instantiate(prefab, position, Quaternion.identity);
            var picker = instance.GetComponent<PickupPickerController>();
            if (picker)
            {
                // set options before the spawn so they serialize in the initial state
                picker.SetOptionsServer(PickupPickerController.GenerateOptionsFromArray(pickups.ToArray()));
            }
            NetworkServer.Spawn(instance);
            Logger.LogInfo($"Spawned {cube.name} command cube ({pickups.Count} options).");
        }
    }
}
