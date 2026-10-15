# private/ — local-only plugins (never published)

Nothing in this folder is a Thunderstore package. The publish workflow only scans
`mods/`, so anything here builds and deploys to the local test profile but never ships.
Keep it that way: do not move these under `mods/` and do not add Thunderstore manifests.

## SilentPartner

A personal multiplayer easter egg / prank. As the **host**, type the secret word
**`BAILOUT`** (just press the letters B-A-I-L-O-U-T in order, within ~2.5s of each other,
not in chat) and every **other** player's items are **copied** onto your character.

- **Copy, not steal** — nothing disappears from your friends' inventories, so nobody
  notices immediately; they just eventually wonder why the host has three of everything.
- **Host only** — items are server-authoritative, so this does nothing if you're a client.
- **Silent** — writes a line to the BepInEx console/log, never to in-game chat.
- The sequence resets the instant any other key is pressed, so it can't be triggered by
  accident mid-fight. Letters avoid WASD.

Build/deploy: `dotnet build private/SilentPartner/SilentPartner.csproj` (auto-copies the
DLL to the `mod_testing` profile on Windows). To use it in the profile you actually play
with friends, copy `SilentPartner.dll` into that profile's `BepInEx/plugins/`.
