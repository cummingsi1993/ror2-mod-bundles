# AuditDepartment

Monster-economy bundle: the combat director's credit budget is the resource every item manipulates. Multiplayer doctrine (from the DefenseBudget `Networkcost` lesson): items may **read** the shared director freely; anything that **writes** to it must be a pure lobby benefit or pay the whole lobby; personal risk/reward loops bypass the director and target only their holder via `DirectorCore.TrySpawnObject`.

| Item | Tier | Effect | Config section |
|---|---|---|---|
| Red Tape | White | Spawns within 75m are slowed + weakened 3s (+1.5s/stack) | `RedTape` |
| Line-Item Veto | Green | Every 45s (×0.85/stack) the next 100+ credit spawn is cancelled, budget paid to holders as gold | `LineItemVeto` |
| Hostile Takeover | Red | Skim 15%/stack (cap 50%) of director income; hire stage monsters as allies (cap 2 +1/stack, max 6, 120s contracts) | `HostileTakeover` |
| Stimulus Package | Lunar | Directors earn +50%/stack income (lobby-wide); gold kickback split among ALL players | `StimulusPackage` |
| Off the Books | Void (corrupts Red Tape) | Nearby spawns audited: +15% (+5%/stack) damage taken; audited kills fill a 300-credit ledger that buys a wave spawned on the killer | `OffTheBooks` |
| Artifact of Austerity | Artifact | Per-director fixed stage budget (450 × difficulty-scaled credits), 1.5× spend rate while it lasts, then dry | `ArtifactOfAusterity` |

## Implementation notes

- All director mechanics run in two funnels in `DirectorHooks.cs`, so ordering is explicit:
  - **Income** (`CombatDirector.Simulate` hook): Stimulus boosts → Austerity caps → Takeover skims. The income delta is measured around `orig`; net-negative ticks (spawn spent more than accrued) are skipped.
  - **Spending** (`CombatDirector.AttemptSpawnOnTarget` hook): Line-Item Veto returns false without calling orig and deducts the card cost from `monsterCredit` manually — spawn denied, money gone.
- One-wave directors (`shouldSpawnOneWave`, i.e. the teleporter boss) are excluded from both funnels — bosses are never taxed, starved, or vetoed.
- Red Tape / Off the Books tag spawns via `CharacterBody.onBodyStartGlobal` (covers every spawn path, not just directors). Audited-kill accounting hangs off `GlobalEventManager.onCharacterDeathGlobal`, crediting `DeathRewards.spawnValue`.
- Ledger punishment waves carry a `LedgerWaveMarker` so they never accrue audit credit (Fuzzy Dice doctrine: generated things must exclude their generators).
- `DirectorCompat` resolves `CombatDirector.currentMonsterCardCost` by reflection — it is public in the GameLibs refs but **private at runtime** (publicized-assembly trap). Run `tools/audit_members.ps1` after touching any new RoR2 member.
- Allies are tracked and pruned by liveness; `MasterSuicideOnTimer` enforces contract expiry.

## Testing

**F7** drops one of each item (host only; `Debug > SpawnPackKey`; F6 is SupplyChain's). Checklist: watch spawns near you get Slow60+Weak with Red Tape; wait for a veto broadcast and check the gold; hold Hostile Takeover and watch the log for hires (they should fight monsters and expire); grab Stimulus and watch director waves thicken while everyone's gold ticks up; corrupt Red Tape in the Void Fields, kill audited enemies, and confirm the wave lands on you (and that wave kills don't refill the ledger); enable Austerity and confirm spawning dies down late-stage.

Build: `dotnet build AuditDepartment/AuditDepartment.csproj` (auto-deploys to the mod_testing profile). Icons: `tools/make_icons.ps1`. Pickup models: not yet generated — items use the vanilla mystery model fallback until the TRELLIS pipeline is run (`tools/generate_models.sh` → `tools/blender_clean_export.py`).
