# Mod Bundle Roster

Three themed Risk of Rain 2 bundles, one economic universe: **DefenseBudget** taxes *your* gold, **SupplyChain** inflates *your* items, **AuditDepartment** raids *the monsters'* budget.

| Bundle | Theme | Status |
|---|---|---|
| DefenseBudget | Your gold as a resource and a liability | **Published** (1.0.1 on Thunderstore) |
| SupplyChain | Item-count manipulation | 1.0.0 built, PR unmerged (merge = publish) |
| AuditDepartment | The monster director's credit economy | 1.0.0 built, draft PR #3, in playtest |

---

## DefenseBudget — income pack

*Five items that make gold a resource you manage — and a liability you can drown in.*

| Item | Tier | Effect |
|---|---|---|
| Savings Bond | White | Every 10s, earn 2% (+2%/stack) interest on held gold |
| Accounts Receivable | Green | Damaging an enemy marks it; marked enemies pay an extra 10% (+10%/stack) of their bounty when killed by anyone |
| Golden Parachute | Red | On lethal damage, pay a difficulty-scaled severance fee (on credit if you carry a Defense Budget) to survive at 15% health. Recharges 45s (−25%/stack) |
| Final Notice | Void (corrupts Roll of Pennies) | 30% (+10%/stack, cap 70%) of incoming damage is billed to your gold instead of your health |
| Defense Budget | Lunar | The flagship — see below |
| Artifact of Communism | Artifact | All gold belongs to the collective: one shared pool everyone earns into and spends from, with a small configurable overhead |

**Defense Budget (lunar) in detail:** your gold income is taxed −25% per stack (multiplicative, personal — teammates untouched). In exchange you get a **line of credit** (~one inflated small chest per stack, difficulty-scaled): buy things you can't afford and the shortfall becomes **deficit**. Deficit grows 1%/sec interest and all income repays it first — but while in deficit you deal **+15% damage per stack**. Exceed your credit limit and you **default**: 2% max HP/sec (bypasses armor) until you're back under. Default damage can't be farmed for gold, and cleansing your last stack does not forgive the debt.

---

## SupplyChain — item-count pack

*Calibrated against the Sale Star yardstick (~1 extra item/stage ≈ one green slot). Generated items always exclude their generators; all generation is capped per stage.*

| Item | Tier | Effect |
|---|---|---|
| Bulk Order | White | Gold chests: 7% (up to 30% with stacks) chance of an extra common item |
| Loaded Dice | Green | Gold chests: 15% (up to 40%) chance of a bonus item one tier below contents; max 2 (+1/stack) bonus drops per stage |
| Standing Order | Red | Each stage start: +1 stack of your lowest-count item (+1 item per stack) |
| Force Multiplier | Red | All other standard items behave as if you had 10% (+10%/stack) more stacks, rounded down. Lunar/void unaffected; printers and scrappers consume real stacks only |
| Pyramid Scheme | Lunar | On pickup: +1 stack of everything you own. Afterwards, 20% of gold income per stack (multiplicative) is paid upline, forever |
| Recall Notice | Void (corrupts Loaded Dice) | Bonus chest drops become guaranteed, but every bonus is a void item of the chest's tier. Shares Loaded Dice's stage cap |
| Artifact of Diversification | Artifact | Chests reroll once when they'd drop an item the opener already owns 5+ stacks of |

"Chests" means gold-cost `ChestBehavior` interactables — multishops, lunar pods, and lockboxes don't count.

---

## AuditDepartment — monster-economy pack

*The combat director literally runs on credits; this bundle audits, vetoes, skims, and over-funds that budget. Multiplayer doctrine: items may read the shared director freely, but writes are pure lobby benefits or pay the whole lobby; personal risk loops target only their holder. Teleporter boss directors are exempt from everything.*

| Item | Tier | Effect |
|---|---|---|
| Red Tape | White | Enemies spawning within 75m are slowed and weakened for 3s (+1.5s/stack) |
| Line-Item Veto | Green | Every 45s (×0.85/stack, global cooldown), the next enemy spawn costing 100+ director credits is cancelled and its budget paid to holders as gold |
| Hostile Takeover | Red | Skim 15%/stack (lobby cap 50%) of monster director income; spend it hiring this stage's monsters onto your team (2 +1/stack alive, max 6, 120s contracts) |
| Stimulus Package | Lunar | Directors earn +50% income per stack (all players' stacks — the lobby shares the heat); a gold kickback on every bonus credit is split evenly among all players |
| Off the Books | Void (corrupts Red Tape) | Nearby spawns are audited: +15% (+5%/stack) damage taken from all sources. Audited enemies *you* kill accrue spawn cost to a ledger; at 300 credits a monster wave is bought with it and spawned on you. Punishment waves can't be audited |
| Artifact of Austerity | Artifact | Each director gets a fixed per-stage budget (450 credits, difficulty-scaled) at 1.5× earn rate — big early waves, then the money runs out |

---

## Cross-bundle interactions worth knowing

- **Golden Parachute + Defense Budget**: the severance fee can draw on Defense Budget credit, putting you into (empowered) deficit to survive.
- **Final Notice + Defense Budget**: damage billed to gold can push you toward deficit/default — gold-as-health cuts both ways.
- **Pyramid Scheme / Stimulus Package + Savings Bond**: tithes and kickbacks interact with interest on held gold; hoarders feel taxes hardest.
- **Line-Item Veto / Hostile Takeover + Stimulus Package**: Stimulus inflates director income, which makes vetoes richer and skims fatter — deliberately funding the enemy to rob them is a build.
- Debug drop keys (host only, configurable per bundle): **F3** Defense Budget lunar / **F4** DefenseBudget pack / **F6** SupplyChain pack / **F7** AuditDepartment pack.

All numbers configurable per bundle in `BepInEx/config/Isaac_Cummings.<Bundle>.cfg`.
