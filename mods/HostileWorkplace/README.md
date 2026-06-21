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
| 1 | Artifact + window state machine + telegraph (buff/chat), no combat | **done** |
| 2 | Friendly fire gating + PvP damage scaling + one-shot protection | planned |
| 3 | Item theft on PvP kill (per-tier %) | planned |
| 4 | Polish: leader/bounty marker, monster-ceasefire toggle, start/cancel equipment, screen FX | planned |

## Implementation notes

- Window state is server-authoritative (`BetrayalWindow`, ticked by `WindowRunner` on `FixedUpdate`). The "Open Season" `BuffDef` is applied to every player body during a window; buffs are networked on `CharacterBody`, so clients get the on-screen flag for free. Announcements via `Chat.SendBroadcastChat`.
- Triggers subscribe to `TeleporterInteraction.onTeleporterBeginChargingGlobal`; state resets on `Stage.onServerStageBegin`.
- Member audit (`tools/audit_members.ps1`) clean — only reads/subscribes touch private-backed members; no new reflection shims.

## Testing

**F8** force-opens a window (host only; requires Artifact of Mutiny enabled; `Debug > ForceWindowKey`). F6 is SupplyChain, F7 AuditDepartment. Slice 1 is verifiable solo: enable the artifact, press F8, confirm the telegraph countdown chat, the Open Season buff appearing on your character for the duration, and the truce message on close. Friendly fire / theft aren't wired yet.

Build: `dotnet build HostileWorkplace/HostileWorkplace.csproj` (auto-deploys to the mod_testing profile). Icons: `tools/make_icons.ps1`. (Package icon is still the placeholder copied from AuditDepartment — regenerate before any publish.)
