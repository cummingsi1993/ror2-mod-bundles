# Defense Budget

An **income-themed item pack**: five items that make gold a resource you manage — and a liability you can drown in.

- **Savings Bond** (white) — every 10 seconds, earn 2% (+2% per stack) interest on your held gold.
- **Accounts Receivable** (green) — damaging an enemy marks it; marked enemies pay you an extra 10% (+10% per stack) of their bounty when killed by anyone.
- **Golden Parachute** (red) — upon lethal damage, pay a difficulty-scaled severance fee (on credit, if you carry a Defense Budget) to survive at 15% health instead. Recharges in 45s (-25% per stack).
- **Final Notice** (void) — corrupts all Rolls of Pennies: 30% (+10% per stack, up to 70%) of incoming damage is billed to your gold instead of your health. Requires Survivors of the Void.
- **Defense Budget** (lunar) — the flagship, detailed below.
- **Artifact of Communism** — all gold belongs to the collective: one shared pool that every player earns into and spends from. Total income runs slightly below the sum of individual incomes (configurable overhead). Pairs horrifyingly with a teammate who shops irresponsibly.

## Defense Budget (Lunar)

> *Spend gold you don't have... but earn far less.*

## What it does

- **Income tax** — Your gold income is reduced by **25% per stack** (multiplicative: 75%, 56%, 42%...). Only you pay the tax — your teammates' income and shared chest prices are untouched.
- **Line of credit** — You can buy things you can't afford. The shortfall becomes **deficit**. Your credit limit is roughly the price of one (inflated) small chest per stack, and it scales with difficulty over time just like chest prices do.
- **Interest** — While in deficit, your debt grows **1% per second**, and **all gold income repays the deficit first**. You earn nothing for yourself until you're back in the black.
- **Deficit spending** — While in deficit, you deal **+15% damage per stack**. Debt is power. Power is debt.
- **Default** — If your deficit ever exceeds your credit limit (interest, losing stacks, Brittle Crown...), you **lose 2% of your maximum health per second** until the deficit is repaid below the limit. This damage bypasses armor and blocking.

A yellow **$** buff shows while you are in deficit; it turns red when you have defaulted.

## Fine print

- Default damage can never be farmed for gold: money gained (Roll of Pennies) or lost (Brittle Crown) from the default damage itself is voided.
- Cleansing away your last stack does **not** forgive your deficit. The committee always collects.
- All numbers are configurable in `BepInEx/config/Isaac_Cummings.DefenseBudget.cfg`, including an optional price-inflation mode (`CostIncreasePerStack`) that raises gold costs for the whole lobby instead of (or on top of) the income tax.

## Requirements

BepInExPack, HookGenPatcher, R2API (Items, Language, ContentManagement).
