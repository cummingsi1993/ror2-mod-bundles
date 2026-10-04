# DevTools (local-only, never published)

Testing cheats for every bundle, kept out of the published mods. Lives under `private/`, which the Thunderstore publish workflow never scans (it only looks at `mods/*`), and has no Thunderstore manifest. Don't move it under `mods/`.

`dotnet build private/DevTools/DevTools.csproj` copies `DevTools.dll` into the `mod_testing` profile.

## Hotkeys (host only, configurable in `Isaac_Cummings.DevTools.cfg`)

Each key spawns an Artifact of Command choice cube in front of you, offering every item of that tier the current run can drop, vanilla and bundle items alike:

| Key | Tier |
|---|---|
| F5 | White |
| F6 | Green |
| F7 | Red |
| F9 | Boss (yellow) |
| F10 | Lunar |
| F11 | Void (all void tiers) |

F8 is left free for HostileWorkplace's dev builds. Items a run can't drop (e.g. DLC disabled in the lobby) don't appear.
