# Supply Chain

An **item-count manipulation pack**, calibrated against the Sale Star yardstick (~1 extra item per stage ≈ one green slot). Every item that generates items excludes its own bundle from the loot pool and is capped per stage — no infinite dice, no on-command farming.

- **Bulk Order** (white) — gold chests have a 7% (up to 30% with stacks) chance to contain an extra **common item**.
- **Loaded Dice** (green) — gold chests have a 15% (up to 40%) chance to drop a **bonus item one tier below their contents**. At most 2 (+1 per stack) bonus drops per stage.
- **Purchase Order** (green) — gold chests have a 10% (up to 35% with stacks) chance to also offer an **Artifact-of-Command-style choice** of any item of the chest's tier. At most 1 (+1 per 2 stacks) per stage. Order exactly what you want.
- **Standing Order** (red) — at the start of each stage, gain **+1 stack of your lowest-count item** (+1 item per stack). Your scarcest holdings, automatically restocked.
- **Force Multiplier** (red) — all other standard items behave as if you had **10% (+10% per stack) more stacks, rounded down**. Does nothing for small stacks; deep stacks run deeper. Lunar/void items unaffected.
- **Pyramid Scheme** (lunar) — on pickup, **+1 stack of every item you own**. Afterwards, **20% of your gold income per stack (multiplicative) is paid upline**, forever. The upline does not pay back.
- **Recall Notice** (void, corrupts Loaded Dice) — bonus chest drops become a **guaranteed void item one tier below the chest's contents**, capped at 1 (+1 per 2 stacks, max 3) per stage. Requires Survivors of the Void.
- **Artifact of Diversification** — chests reroll once when they would drop an item you already own 5+ stacks of. Runs wider, not taller.

## Fine print

- "Chests" means gold-cost `ChestBehavior` interactables (small/large/category/cloaked). Multishops, lunar pods, and lockboxes don't count.
- Purchase Order's choice list excludes this bundle's own items, and the choice cube behaves exactly like a Command essence (same UI, same networking) — it does not require the Artifact of Command to be enabled.
- Force Multiplier amplifies gameplay reads only — printers, scrappers, and the void cauldron consume real stacks, never phantom ones.
- All numbers configurable in `BepInEx/config/Isaac_Cummings.SupplyChain.cfg`.

Pairs with [DefenseBudget](https://thunderstore.io/c/riskofrain2/p/Isaac/DefenseBudget/), the income-themed bundle from the same series.
