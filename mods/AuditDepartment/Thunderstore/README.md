# Audit Department

A **monster-economy pack**. Risk of Rain 2's combat director literally runs on credits — this bundle audits, vetoes, skims, and over-funds that budget. Designed multiplayer-first: items may *read* the shared director freely, but anything that writes to it is either a pure lobby benefit or pays the whole lobby; personal risk loops target only their holder.

- **Red Tape** (white) — enemies that spawn within 75m arrive buried in paperwork: **slowed and weakened** for 3s (+1.5s per stack).
- **Line-Item Veto** (green) — every 45s (less with stacks), the next enemy spawn costing **100+ director credits is cancelled** and its budget is paid out to holders as **gold**.
- **Hostile Takeover** (red) — **skim 15% per stack (max 50% total)** of the monster director's income and spend it **hiring this stage's monsters onto your team**. Up to 2 (+1/stack, max 6) hires at once on 120s contracts.
- **Stimulus Package** (lunar) — monster directors earn **+50% income per stack** (lobby-wide). A kickback on every bonus credit is paid as gold, **split evenly among all players** — everyone shares the heat, everyone gets a cut.
- **Off the Books** (void, corrupts Red Tape) — nearby spawns are **audited**, taking **+15% damage from all sources** (+5%/stack). Audited enemies *you* kill accrue their spawn cost to a ledger; at 300 credits **the books are balanced**: a monster wave is bought with it and spawned **on you**. Requires Survivors of the Void.
- **Artifact of Austerity** — monster spawning runs on a **fixed budget each stage**: 50% faster spending while it lasts, then nothing. Fewer, harder waves.

## Fine print

- Teleporter boss waves are funded directly, not by income — Stimulus can't inflate them, Austerity can't starve them, Takeover can't skim them, and the Veto never cancels a boss.
- Off the Books punishment waves never accrue audit credit themselves (no perpetual-motion ledger).
- The Veto destroys the credits it cancels — the director eats the loss and re-rolls something cheaper.
- All numbers configurable in `BepInEx/config/Isaac_Cummings.AuditDepartment.cfg`.

Part of a series with [DefenseBudget](https://thunderstore.io/c/riskofrain2/p/Isaac/DefenseBudget/) (your economy) and SupplyChain (your items) — this one is about *their* economy.
