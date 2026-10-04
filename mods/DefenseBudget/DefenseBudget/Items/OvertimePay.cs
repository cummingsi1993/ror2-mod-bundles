using BepInEx.Configuration;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DefenseBudget.Items
{
    // Void (corrupts Roll of Pennies): a small regen-like heal, and any healing past full
    // health — including heals from teammates — trickles out as gold, priced in small
    // chests per bar of overheal. A slow, continuous income for staying topped up; healing
    // items raise it. A high per-minute safety cap only catches extreme healing builds.
    internal static class OvertimePay
    {
        internal static ItemDef Def;
        internal static ConfigEntry<float> ContributionHealPerStack;
        internal static ConfigEntry<float> ChestsPerFullOverheal;
        internal static ConfigEntry<float> MaxChestsPerMinutePerStack;
        internal static ConfigEntry<float> BurstSeconds;
        internal static ConfigEntry<bool> RequireCombat;
        internal static ConfigEntry<float> CombatWindow;
        internal static ConfigEntry<bool> CountMinionDamage;
        internal static ConfigEntry<float> RegenWeight;

        private const int SmallChestBaseCost = 25;
        private const float ContributionInterval = 0.5f;
        // Payouts are a steady trickle of small orbs, at most one per interval. The
        // minimum stops 1-2 gold payments from rounding Defense Budget's per-call tax away
        // entirely, but small orbs still round it by up to half a gold each (TODO 1.1.1:
        // carry the tax remainder per master in CharacterMaster_GiveMoney instead).
        private const float MinPayoutInterval = 2f;
        private const float MinPayoutGold = 3f;

        private class Account : MonoBehaviour
        {
            public float lastEngaged = float.NegativeInfinity;
            public bool bucketFilled;
            // small chests' worth that may be paid right now (token bucket)
            public float bucket;
            // overheal converted to chests since the last tick, not yet checked against the cap
            public float owedChests;
            // gold earned but not yet delivered
            public float goldCarry;
            public float lastPayoutTime = float.NegativeInfinity;
        }

        // Rebuilt every server tick from player masters holding the item. Monsters, minions
        // and turrets are never in here, so the heal tap exits after one lookup for them.
        private static readonly Dictionary<HealthComponent, Account> holders = new Dictionary<HealthComponent, Account>();
        private static readonly Dictionary<CharacterMaster, Account> accounts = new Dictionary<CharacterMaster, Account>();
        private static float contributionTimer;

        // void-pink voxel heart; placeholder until a generated model is embedded
        private static readonly string[] HeartGlyph =
        {
            ".XX.XX.",
            "XXXXXXX",
            "XXXXXXX",
            ".XXXXX.",
            "..XXX..",
            "...X...",
        };

        internal static void Init(ConfigFile config)
        {
            ContributionHealPerStack = config.Bind("OvertimePay", "ContributionHealPerStack", 0.0075f,
                "Fraction of maximum health (health + shield) healed per second, per stack. Works like regeneration: " +
                "no crits, no Aegis barrier, no N'kuhana's charge. Rejuvenation Rack and the Eclipse healing penalty apply.");
            // key renamed from ChestsPerFullOverheal in 1.1.1 so the lowered default replaces the
            // 1.0 that 1.1.0 wrote into everyone's config (BepInEx keeps saved values over new defaults)
            ChestsPerFullOverheal = config.Bind("OvertimePay", "ChestsPer100PercentOverheal", 0.25f,
                "Small chests' worth of gold earned per 100% of maximum health overhealed (0.25 = one chest per 400%). " +
                "Does not grow with stacks. A small chest's price scales with difficulty over time.");
            MaxChestsPerMinutePerStack = config.Bind("OvertimePay", "MaxChestsPerMinutePerStack", 10f,
                "Safety cap per stack, in small chests' worth per minute; only extreme healing builds reach it. " +
                "Overheal beyond the cap is discarded, not banked.");
            BurstSeconds = config.Bind("OvertimePay", "BurstSeconds", 10f,
                "How many seconds' worth of the payout cap can be paid at once, so a burst heal at full health " +
                "(Medkit, Monster Tooth) pays in full.");
            RequireCombat = config.Bind("OvertimePay", "RequireCombat", false,
                "If true, overheal only pays while in combat (see CombatWindow). If false, it pays whenever the run " +
                "timer is running, and only in combat while the timer is paused (Bazaar, Void Fields cells) or " +
                "between Simulacrum waves, so idling where time stands still earns nothing.");
            CombatWindow = config.Bind("OvertimePay", "CombatWindow", 5f,
                "Seconds after you damage, or are damaged by, an enemy that count as being in combat. " +
                "Damage over time you applied counts.");
            CountMinionDamage = config.Bind("OvertimePay", "CountMinionDamage", false,
                "If true, damage dealt by your drones and turrets also keeps you in combat.");
            RegenWeight = config.Bind("OvertimePay", "RegenWeight", 1f,
                "Multiplier on overheal from regeneration-type healing (natural regen and this item's own heal). " +
                "0 = only healing effects (Leeching Seed, Medkit, ...) pay, like Aegis's barrier.");

            Def = Assets.CreateItemDef(
                "OvertimePay", "OVERTIME_PAY", ItemTier.VoidTier1,
                Assets.LoadSprite("DefenseBudget.icon_overtime_pay.rgba", 128),
                Assets.CreatePickupModel("PickupOvertimePay", "DefenseBudget.models.overtime_pay.obj", "DefenseBudget.models.overtime_pay.rgba", 512, 0.6f,
                    () => Assets.CreateGlyphModel("PickupOvertimePayGlyph", HeartGlyph, new Color(0.95f, 0.35f, 0.75f), 0.1f, 0.16f)),
                new[] { ItemTag.Healing, ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.CannotCopy });
            Def.requiredExpansion = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            // Void corruption pairing: Roll of Pennies (GoldOnHurt) -> Overtime Pay.
            // The ItemDef asset is loaded via Addressables because DLC1Content.Items fields
            // are not populated during plugin Awake.
            var rollOfPennies = Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/GoldOnHurt/GoldOnHurt.asset").WaitForCompletion();
            var provider = ScriptableObject.CreateInstance<ItemRelationshipProvider>();
            provider.name = "DefenseBudgetContagiousItems";
            provider.relationshipType = Addressables.LoadAssetAsync<ItemRelationshipType>("RoR2/DLC1/Common/ContagiousItem.asset").WaitForCompletion();
            provider.relationships = new[]
            {
                new ItemDef.Pair { itemDef1 = rollOfPennies, itemDef2 = Def },
            };
            ContentAddition.AddItemRelationshipProvider(provider);

            CreateLanguage();

            IL.RoR2.HealthComponent.Heal += TapOverheal;
            GlobalEventManager.onServerDamageDealt += StampCombat;
            Run.onRunStartGlobal += _ => contributionTimer = 0f;
            Run.onRunDestroyGlobal += _ =>
            {
                holders.Clear();
                accounts.Clear();
            };
        }

        private static void CreateLanguage()
        {
            string contribPct = (ContributionHealPerStack.Value * 100f).ToString("0.##");
            string perChestPct = (100f / Mathf.Max(0.01f, ChestsPerFullOverheal.Value)).ToString("0.#");

            LanguageAPI.Add("OVERTIME_PAY_NAME", "Overtime Pay");
            LanguageAPI.Add("OVERTIME_PAY_PICKUP",
                "Heal over time. Healing past full health is paid as overtime. <style=cIsVoid>Corrupts all Rolls of Pennies</style>.");
            string desc = "";
            if (ContributionHealPerStack.Value > 0f)
            {
                desc += $"<style=cIsHealing>Heal</style> for <style=cIsHealing>{contribPct}%</style> <style=cStack>(+{contribPct}% per stack)</style> " +
                        $"of your <style=cIsHealing>maximum health</style> every second. ";
            }
            // the safety cap is deliberately left out: only extreme healing builds reach it
            desc += $"<style=cIsHealing>Healing past full health</style>{(RequireCombat.Value ? " while in combat" : "")} " +
                    $"steadily earns <style=cIsUtility>one small chest's worth of gold</style> " +
                    $"for every <style=cIsHealing>{perChestPct}%</style> of your maximum health overhealed. " +
                    $"<style=cIsVoid>Corrupts all Rolls of Pennies</style>.";
            LanguageAPI.Add("OVERTIME_PAY_DESC", desc);
            LanguageAPI.Add("OVERTIME_PAY_LORE",
                "TIMESHEET ADDENDUM, FORM 7-VOID\n\nHours worked beyond full capacity are compensated at the overtime rate: one (1) small container of currency per four (4) complete persons.\nOvertime accrues continuously while the employee is in perfect health and the clock is running. Hours spent idling in the break room, where the clock is stopped, are not billable.\n\nPayroll caps overtime per pay period. Vitality in excess of the cap is forfeited to the company. Management thanks you for going above and beyond. Management always does.");
        }

        // ---------------------------------------------------------------
        // Overheal measurement
        // ---------------------------------------------------------------

        // HealthComponent.Heal already computes overheal for Aegis: a float local holding
        // (post-modifier amount) - (health actually gained). Both the "had missing health"
        // and "already full" paths join at Aegis's `if (overheal > 0 && nonRegen)` test,
        // so tap that local right there. The tap is only reached by heals that resolved:
        // dead targets, HealingDisabled and Corpsebloom's deferral all return earlier, and
        // Heal is server-only.
        private static void TapOverheal(ILContext il)
        {
            var c = new ILCursor(il);
            int overhealLoc = -1;
            if (!c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<HealthComponent>("set_Networkhealth")))
            {
                Log.Error("Overtime Pay: set_Networkhealth not found in HealthComponent.Heal; overheal will not pay.");
                return;
            }
            // AfterLabel retargets the already-at-full-HP branch onto the emitted tap, so
            // both paths run it.
            if (!c.TryGotoNext(MoveType.AfterLabel,
                    x => x.MatchLdloc(out overhealLoc),
                    x => x.MatchLdcR4(0f),
                    x => x.MatchBleUn(out _),
                    x => x.MatchLdarg(3)))
            {
                Log.Error("Overtime Pay: Aegis overheal test not found in HealthComponent.Heal; overheal will not pay.");
                return;
            }
            if (il.Body.Variables[overhealLoc].VariableType.MetadataType != MetadataType.Single)
            {
                Log.Error("Overtime Pay: overheal local in HealthComponent.Heal is not a float; overheal will not pay.");
                return;
            }
            // match the field by name: the ItemCounts struct is non-public at runtime
            var probe = c.Clone();
            if (!probe.TryGotoNext(x => x.MatchLdfld(out FieldReference f) && f.Name == "barrierOnOverHeal")
                || probe.Index - c.Index > 12)
            {
                Log.Error("Overtime Pay: HealthComponent.Heal no longer matches the Aegis overheal shape; overheal will not pay.");
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, overhealLoc);
            c.Emit(OpCodes.Ldarg_3);
            c.EmitDelegate<Action<HealthComponent, float, bool>>(OnOverheal);
        }

        // Runs inside every server-side heal of every body: keep the early exits cheap and
        // never throw into Heal.
        private static void OnOverheal(HealthComponent healthComponent, float overheal, bool nonRegen)
        {
            if (holders.Count == 0 || !(overheal > 0f) || DefenseBudgetPlugin.ApplyingDefaultDamage)
            {
                return;
            }
            try
            {
                if (!holders.TryGetValue(healthComponent, out var account) || !account)
                {
                    return;
                }
                if (!IsOnTheClock(account))
                {
                    return;
                }
                // Balance idea, NOT implemented (user, 2026-10): vanilla Aegis reads this same
                // overheal for its barrier without consuming it, so Aegis + Overtime Pay pays
                // twice. If that combo proves too strong, let Aegis use the overheal up first:
                // scale nonRegen overheal down by the holder's Aegis stacks (read via
                // inventory.GetItemCount(RoR2Content.Items.BarrierOnOverHeal) — itemCounts is
                // private at runtime), making Aegis a barrier-vs-gold tradeoff.
                float weight = nonRegen ? 1f : RegenWeight.Value;
                // normalized by combined max health, so Transcendence (1 max HP) can't turn
                // every heal into a full bar of overheal
                account.owedChests += overheal * weight / Mathf.Max(1f, healthComponent.fullCombinedHealth) * ChestsPerFullOverheal.Value;
            }
            catch (Exception e)
            {
                Log.Error($"Overtime Pay: {e}");
            }
        }

        // ---------------------------------------------------------------
        // When overheal pays
        // ---------------------------------------------------------------

        // Overheal pays whenever the run timer is running — a slow, continuous income for
        // staying topped up. Where time stands still (Bazaar, Void Fields cells) it only
        // pays in combat, so idling there is worthless; RequireCombat applies that rule
        // everywhere.
        private static bool IsOnTheClock(Account account)
        {
            // Leaving a stage converts every wallet to experience and only teleports once
            // they have all sat at 0 for a moment — a steady trickle would stall the exit.
            if (SceneExitController.isRunning)
            {
                return false;
            }
            bool inCombat = Time.fixedTime <= account.lastEngaged + CombatWindow.Value;
            if (inCombat)
            {
                return true;
            }
            if (RequireCombat.Value || !Run.instance)
            {
                return false;
            }
            // Simulacrum's stopwatch never pauses and its difficulty only rises per wave, so
            // the idle ward between waves would pay forever: there, only pay during a wave.
            if (Run.instance is InfiniteTowerRun simulacrum)
            {
                return simulacrum.waveController && !simulacrum.waveController.isFinished;
            }
            return !Run.instance.isRunStopwatchPaused;
        }

        // Fires after damage has been applied (never for rejected hits). Only fighting an
        // enemy team counts — not neutral props, friendly fire, or attacker-less damage
        // like falls, void fog and Defense Budget default damage.
        private static void StampCombat(DamageReport report)
        {
            if (accounts.Count == 0 || report == null)
            {
                return;
            }
            float now = Time.fixedTime;
            var attackerMaster = report.attackerMaster;
            if (attackerMaster && IsHostile(report.attackerTeamIndex, report.victimTeamIndex))
            {
                // the holder's own hits and the damage-over-time they applied
                if (accounts.TryGetValue(attackerMaster, out var attackerAccount))
                {
                    attackerAccount.lastEngaged = now;
                }
                else if (CountMinionDamage.Value && attackerMaster.minionOwnership)
                {
                    var owner = attackerMaster.minionOwnership.ownerMaster;
                    if (owner && accounts.TryGetValue(owner, out var ownerAccount))
                    {
                        ownerAccount.lastEngaged = now;
                    }
                }
            }
            var victimMaster = report.victimMaster;
            if (victimMaster && report.attackerBody && IsHostile(report.victimTeamIndex, report.attackerTeamIndex)
                && accounts.TryGetValue(victimMaster, out var victimAccount))
            {
                victimAccount.lastEngaged = now;
            }
        }

        private static bool IsHostile(TeamIndex self, TeamIndex other)
        {
            return self != other && other != TeamIndex.Neutral && other != TeamIndex.None;
        }

        // ---------------------------------------------------------------
        // Per-tick: contribution heal, rate cap, payout
        // ---------------------------------------------------------------

        // Called from the plugin's FixedUpdate, server-side with an active run.
        internal static void FixedUpdate()
        {
            holders.Clear();
            accounts.Clear();

            float deltaTime = Time.fixedDeltaTime;
            contributionTimer += deltaTime;
            bool contribute = contributionTimer >= ContributionInterval;
            if (contribute)
            {
                contributionTimer -= ContributionInterval;
            }
            float chestPrice = Run.instance.GetDifficultyScaledCost(SmallChestBaseCost);
            float now = Time.fixedTime;

            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                var master = pcmc.master;
                int stacks = master && master.inventory ? master.inventory.GetItemCount(Def) : 0;
                if (stacks <= 0)
                {
                    continue;
                }
                var account = master.GetComponent<Account>();
                if (!account)
                {
                    account = master.gameObject.AddComponent<Account>();
                }
                accounts[master] = account;

                // token bucket in small chests: refills continuously, pays what it can,
                // and anything owed beyond it is discarded rather than banked
                float refillPerSecond = MaxChestsPerMinutePerStack.Value * stacks / 60f;
                float capacity = Mathf.Max(refillPerSecond * BurstSeconds.Value, refillPerSecond * deltaTime);
                account.bucket = account.bucketFilled ? Mathf.Min(capacity, account.bucket + refillPerSecond * deltaTime) : capacity;
                account.bucketFilled = true;
                float paid = Mathf.Min(account.owedChests, account.bucket);
                account.bucket -= paid;
                account.owedChests = 0f;
                account.goldCarry += paid * chestPrice;

                var body = master.GetBody();
                var healthComponent = body ? body.healthComponent : null;
                if (healthComponent && healthComponent.alive)
                {
                    holders[healthComponent] = account;
                    if (contribute && ContributionHealPerStack.Value > 0f)
                    {
                        // nonRegen: false behaves like regeneration — no crit, Corpsebloom,
                        // Aegis, N'kuhana's, or floating heal numbers
                        healthComponent.Heal(
                            ContributionHealPerStack.Value * stacks * healthComponent.fullCombinedHealth * ContributionInterval,
                            default(ProcChainMask), false);
                    }
                }

                // held during a stage exit (see IsOnTheClock) and paid out on the next stage
                if (account.goldCarry >= MinPayoutGold && now - account.lastPayoutTime >= MinPayoutInterval && !SceneExitController.isRunning)
                {
                    uint gold = (uint)account.goldCarry;
                    account.goldCarry -= gold;
                    account.lastPayoutTime = now;
                    Deliver(master, body, gold);
                }
            }
        }

        // A vanilla gold orb (Roll of Pennies' own), networked; it pays through
        // CharacterMaster.GiveMoney on arrival, so Defense Budget's tax and debt
        // repayment and Artifact of Communism pooling all apply. The orb pays nothing if
        // its target body is gone on arrival, so a dead holder is paid directly.
        private static void Deliver(CharacterMaster master, CharacterBody body, uint gold)
        {
            if (body && body.healthComponent && body.healthComponent.alive && body.mainHurtBox && OrbManager.instance)
            {
                OrbManager.instance.AddOrb(new GoldOrb
                {
                    origin = body.corePosition + UnityEngine.Random.onUnitSphere * 2f + Vector3.up,
                    target = body.mainHurtBox,
                    goldAmount = gold,
                    scaleOrb = true,
                });
            }
            else
            {
                master.GiveMoney(gold);
            }
        }
    }
}
