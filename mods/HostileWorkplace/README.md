# HostileWorkplace

A PvP-conversion mod, gated entirely by **Artifact of Mutiny**. While the artifact is on, the run is normal co-op PvE *except* during periodic **Open Season betrayal windows**, when friendly fire and item theft turn on. Truce the rest of the time, so you still genuinely need each other.

Designed for groups (it leans on a revive mod, which the author plays with — kills become a renewable heist economy, not eliminations). Inert solo and with the artifact off.

## Design (locked)

- **Artifact-gated.** `ArtifactOfMutiny.Enabled` is the master switch; everything no-ops without it. Opt-in, lobby-voted, multiplayer-native.
- **Windows** default to opening when the **teleporter begins charging**; a config interval can add timed windows, and an on-demand API (`BetrayalWindow.RequestOpen/ForceClose`) is exposed for future equipment that starts/cancels windows.
- **Friendly fire** during a window: `FriendlyFireManager.friendlyFireMode` enables engine-level player-vs-player hits, scaled down by a configurable PvP multiplier (~0.2) in a `HealthComponent.TakeDamage` hook. **One-shot protection**: a PvP hit can't take you from near-full to dead (leaves you at 1 HP) — burst classes still hit hard and can multi-tap, they just can't delete you from full in a frame. (No flat per-hit cap, by request.)
- **Item theft** on a PvP kill during a window: **permanent** transfer to the killer, a **percentage of each tier** of the victim's holdings, with rarer tiers stolen less (white > green > red > boss). Because it's a % of holdings, the richest player is automatically the juiciest kill and has the most to lose — no escrow needed. Fractional amounts roll probabilistically (a single red ≈ 8% chance to walk).

## Build status

| Slice | Scope | Status |
|---|---|---|
| 1 | Artifact + window state machine + telegraph (buff/chat) | **done** |
| 2 | Friendly fire gating + PvP damage scaling + one-shot protection | **done** |
| 3 | Item theft on PvP kill (per-tier %) | **done** |
| 4 | Fire Drill equipment (start/cancel window) + monster-ceasefire toggle | **done** |
| 4b | Deferred polish: leader/bounty marker, screen FX | not started |

All four slices are built and deployed but **untested in a live lobby** — needs a multiplayer session with the revive mod and Artifact of Mutiny enabled.

## Implementation notes

- Window state is server-authoritative (`BetrayalWindow`, ticked by `WindowRunner` on `FixedUpdate`). The "Open Season" `BuffDef` is applied to every player body during a window; buffs are networked on `CharacterBody`, so clients get the on-screen flag for free. Announcements via `Chat.SendBroadcastChat`.
- Triggers subscribe to `TeleporterInteraction.onTeleporterBeginChargingGlobal`; state resets on `Stage.onServerStageBegin`.
- **Friendly fire** (`FriendlyFire.cs`): on open, `BetrayalWindow` sets `FriendlyFireManager.friendlyFireMode = FriendlyFire` and pins the engine's FF damage scale to 1.0 (reflection on the private backing field) so the `HealthComponent.TakeDamage` hook owns scaling deterministically: player→player damage ×`PvpDamageScale` (0.2), one-shot protection (a hit from ≥90% HP leaves you at 1), and minion/turret hits onto players nullified (direct player combat only). Mode restored on close and stage transition.
- **Item theft** (`ItemTheft.cs`): `GlobalEventManager.onCharacterDeathGlobal`, gated on `IsOpen` + both players. Steals a per-tier fraction of the victim's holdings (white .25 / green .15 / red .08 / boss .05 / lunar 0; void mirrors its colour), fractional amounts rolled probabilistically. Permanent transfer via `RemoveItem`/`GiveItem`.
- **Fire Drill** equipment (`Items/FireDrill.cs`): `EquipmentSlot.PerformEquipmentAction` hook toggles a window (cancel if active, else start). `appearsInSinglePlayer=false`.
- **Monster ceasefire** (optional, default off): a `CombatDirector.Simulate` hook suppresses spawns while a window is open.
- Member audit (`tools/audit_members.ps1`) clean — every private-backed member is read-only, hooked, or subscribed; the only reflection is the intentional FF-scale pin.

## Testing

**F8** force-opens a window (host only; requires Artifact of Mutiny enabled; `Debug > ForceWindowKey`). F6 is SupplyChain, F7 AuditDepartment.

Solo you can confirm the *rhythm*: enable the artifact, press F8, watch the telegraph countdown, the Open Season buff for the duration, and the truce message. Friendly fire and theft need a **second player** to verify: during a window confirm (a) you can damage a teammate at reduced damage, (b) you can't one-shot them from full, (c) your minions/turrets can't hurt players, (d) killing a teammate loots a per-tier cut and announces it. The Fire Drill equipment should toggle a window on use; the `MonsterCeasefire` config should halt spawns mid-window when enabled.

Build: `dotnet build HostileWorkplace/HostileWorkplace.csproj` (auto-deploys to the mod_testing profile). Icons: `tools/make_icons.ps1`. (Package icon is still the placeholder copied from AuditDepartment — regenerate before any publish.)
