using AuditDepartment.Artifacts;
using AuditDepartment.Items;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace AuditDepartment
{
    // All monster-economy mechanics live here, in two funnels:
    //  - CombatDirector.Simulate (income): Stimulus Package boosts, Artifact of
    //    Austerity caps, Hostile Takeover skims — in that order, so the artifact caps
    //    boosted income and the skim only takes from income that survived the cap.
    //  - CombatDirector.AttemptSpawnOnTarget (spending): Line-Item Veto cancels.
    // Plus body-spawn tagging (Red Tape / Off the Books) and audited-kill accounting.
    internal static class DirectorHooks
    {
        private static Xoroshiro128Plus rng;

        // Line-Item Veto: one global committee, one cooldown (Run.instance.time based)
        private static float vetoReadyTime;

        // Off the Books: per-holder audited-kill ledgers
        private static readonly Dictionary<CharacterMaster, float> ledgers = new Dictionary<CharacterMaster, float>();

        // Hostile Takeover: lobby-wide skim pool and living hires
        private static float takeoverPool;
        private static readonly List<CharacterMaster> hiredAllies = new List<CharacterMaster>();

        // Stimulus Package: undistributed kickback gold
        private static float stimulusGoldPool;

        // Artifact of Austerity: credits each director has earned this stage
        private static readonly Dictionary<CombatDirector, float> earnedThisStage = new Dictionary<CombatDirector, float>();

        // Monsters we spawned ourselves (ledger waves) never accrue audit credit —
        // otherwise kills of the punishment wave would fund the next punishment wave.
        // Two layers: onBodyStartGlobal can fire synchronously inside TrySpawnObject
        // (before we can attach the marker), so a flag covers the synchronous path and
        // the marker covers bodies that start on a later frame.
        private class LedgerWaveMarker : MonoBehaviour { }
        private static bool spawningLedgerWave;

        internal static void Init()
        {
            rng = new Xoroshiro128Plus((ulong)DateTime.Now.Ticks);

            On.RoR2.CombatDirector.Simulate += OnSimulate;
            On.RoR2.CombatDirector.AttemptSpawnOnTarget += OnAttemptSpawnOnTarget;
            On.RoR2.HealthComponent.TakeDamage += OnTakeDamage;
            CharacterBody.onBodyStartGlobal += OnBodyStart;
            GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath;
            Stage.onServerStageBegin += OnStageBegin;
        }

        private static void OnStageBegin(Stage stage)
        {
            ledgers.Clear();
            earnedThisStage.Clear();
            hiredAllies.Clear();
            takeoverPool = 0f;
            stimulusGoldPool = 0f;
            vetoReadyTime = 0f;
        }

        // ---------- shared helpers ----------

        private static int TotalStacks(ItemDef def)
        {
            if (!def || def.itemIndex == ItemIndex.None)
            {
                return 0;
            }
            int total = 0;
            foreach (var controller in PlayerCharacterMasterController.instances)
            {
                var master = controller ? controller.master : null;
                if (master && master.inventory)
                {
                    total += master.inventory.GetItemCount(def);
                }
            }
            return total;
        }

        // Gold split among holders weighted by stacks; remainder goes to the first holder.
        private static void GiveGoldToHolders(ItemDef def, uint gold)
        {
            var holders = new List<(CharacterMaster master, int stacks)>();
            int totalStacks = 0;
            foreach (var controller in PlayerCharacterMasterController.instances)
            {
                var master = controller ? controller.master : null;
                int stacks = master && master.inventory ? master.inventory.GetItemCount(def) : 0;
                if (stacks > 0)
                {
                    holders.Add((master, stacks));
                    totalStacks += stacks;
                }
            }
            if (totalStacks == 0 || gold == 0)
            {
                return;
            }
            uint paid = 0;
            foreach (var (master, stacks) in holders)
            {
                uint share = (uint)(gold * (ulong)stacks / (ulong)totalStacks);
                master.GiveMoney(share);
                paid += share;
            }
            if (paid < gold)
            {
                holders[0].master.GiveMoney(gold - paid);
            }
        }

        private static DirectorCard SampleAffordableCard(float budget)
        {
            var stageInfo = ClassicStageInfo.instance;
            var selection = stageInfo ? stageInfo.monsterSelection : null;
            if (selection == null)
            {
                return null;
            }
            for (int i = 0; i < 10; i++)
            {
                var card = selection.Evaluate(rng.nextNormalizedFloat);
                if (card != null && card.spawnCard && card.IsAvailable()
                    && card.cost > 0 && card.cost <= budget)
                {
                    return card;
                }
            }
            return null;
        }

        private static CharacterMaster SpawnCardOnTarget(DirectorCard card, CharacterBody target, TeamIndex team, float minDistance, float maxDistance)
        {
            if (!DirectorCore.instance || card == null || !target)
            {
                return null;
            }
            var placement = new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                minDistance = minDistance,
                maxDistance = maxDistance,
                spawnOnTarget = target.coreTransform,
            };
            var request = new DirectorSpawnRequest(card.spawnCard, placement, rng)
            {
                teamIndexOverride = team,
                // punishment waves must land even if the monster team is full; hires respect the cap
                ignoreTeamMemberLimit = team == TeamIndex.Monster,
                summonerBodyObject = team == TeamIndex.Player ? target.gameObject : null,
            };
            var spawned = DirectorCore.instance.TrySpawnObject(request);
            return spawned ? spawned.GetComponent<CharacterMaster>() : null;
        }

        // ---------- income funnel: Stimulus -> Austerity -> Takeover ----------

        private static void OnSimulate(On.RoR2.CombatDirector.orig_Simulate orig, CombatDirector self, float deltaTime)
        {
            if (!NetworkServer.active || !Run.instance || self.teamIndex != TeamIndex.Monster || self.shouldSpawnOneWave)
            {
                // one-wave directors (teleporter boss) are funded directly, not taxed
                orig(self, deltaTime);
                return;
            }

            float before = self.monsterCredit;
            orig(self, deltaTime);
            float gained = self.monsterCredit - before;
            if (gained <= 0f)
            {
                return;
            }

            int stimulusStacks = TotalStacks(StimulusPackage.Def);
            if (stimulusStacks > 0)
            {
                float bonus = gained * StimulusPackage.BoostPerStack.Value * stimulusStacks;
                self.monsterCredit += bonus;
                gained += bonus;
                stimulusGoldPool += bonus * StimulusPackage.KickbackGoldPerCredit.Value;
                PayStimulusKickback();
            }

            if (ArtifactOfAusterity.Enabled)
            {
                earnedThisStage.TryGetValue(self, out float earned);
                float cap = ArtifactOfAusterity.BudgetBase.Value
                    * (1f + ArtifactOfAusterity.BudgetDifficultyScale.Value * Run.instance.difficultyCoefficient);
                float boosted = gained * ArtifactOfAusterity.BoostFactor.Value;
                float allowed = Mathf.Clamp(cap - earned, 0f, boosted);
                self.monsterCredit += allowed - gained;
                gained = allowed;
                earnedThisStage[self] = earned + allowed;
                if (gained <= 0f)
                {
                    return;
                }
            }

            int takeoverStacks = TotalStacks(HostileTakeover.Def);
            if (takeoverStacks > 0)
            {
                float skimRate = Mathf.Min(HostileTakeover.SkimCap.Value, HostileTakeover.SkimPerStack.Value * takeoverStacks);
                float skim = gained * skimRate;
                self.monsterCredit -= skim;
                takeoverPool += skim;
                TrySpendTakeoverPool(takeoverStacks);
            }
        }

        private static void PayStimulusKickback()
        {
            int playerCount = 0;
            foreach (var controller in PlayerCharacterMasterController.instances)
            {
                if (controller && controller.master)
                {
                    playerCount++;
                }
            }
            if (playerCount == 0)
            {
                return;
            }
            uint share = (uint)(stimulusGoldPool / playerCount);
            if (share == 0)
            {
                return;
            }
            foreach (var controller in PlayerCharacterMasterController.instances)
            {
                var master = controller ? controller.master : null;
                if (master)
                {
                    master.GiveMoney(share);
                }
            }
            stimulusGoldPool -= share * (float)playerCount;
        }

        private static void TrySpendTakeoverPool(int totalStacks)
        {
            hiredAllies.RemoveAll(master => !master || !master.GetBody() || !master.GetBody().healthComponent.alive);
            int allyCap = Mathf.Min(HostileTakeover.AllyCapBase.Value + totalStacks - 1, HostileTakeover.AllyCapMax.Value);
            if (hiredAllies.Count >= allyCap || takeoverPool < 25f)
            {
                return;
            }
            var target = RichestHolderBody(HostileTakeover.Def);
            if (!target)
            {
                return;
            }
            var card = SampleAffordableCard(takeoverPool);
            if (card == null)
            {
                return;
            }
            var master = SpawnCardOnTarget(card, target, TeamIndex.Player, 10f, 35f);
            if (!master)
            {
                return;
            }
            takeoverPool -= card.cost;
            hiredAllies.Add(master);
            if (HostileTakeover.AllyLifetime.Value > 0f)
            {
                master.gameObject.AddComponent<MasterSuicideOnTimer>().lifeTimer = HostileTakeover.AllyLifetime.Value;
            }
            Log.Info($"Hostile Takeover hired {card.spawnCard.name} for {card.cost} credits ({hiredAllies.Count}/{allyCap} on payroll).");
        }

        // The alive holder with the most stacks anchors hire placement.
        private static CharacterBody RichestHolderBody(ItemDef def)
        {
            CharacterBody best = null;
            int bestStacks = 0;
            foreach (var controller in PlayerCharacterMasterController.instances)
            {
                var master = controller ? controller.master : null;
                if (!master || !master.inventory)
                {
                    continue;
                }
                int stacks = master.inventory.GetItemCount(def);
                var body = master.GetBody();
                if (stacks > bestStacks && body && body.healthComponent.alive)
                {
                    best = body;
                    bestStacks = stacks;
                }
            }
            return best;
        }

        // ---------- spending funnel: Line-Item Veto ----------

        private static bool OnAttemptSpawnOnTarget(On.RoR2.CombatDirector.orig_AttemptSpawnOnTarget orig, CombatDirector self, Transform spawnTarget, DirectorPlacementRule.PlacementMode placementMode)
        {
            if (NetworkServer.active && Run.instance
                && self.teamIndex == TeamIndex.Monster && !self.shouldSpawnOneWave
                && Run.instance.time >= vetoReadyTime)
            {
                int totalStacks = TotalStacks(LineItemVeto.Def);
                if (totalStacks > 0)
                {
                    int cost = DirectorCompat.GetCurrentCardCost(self);
                    if (cost >= LineItemVeto.MinVetoCost.Value)
                    {
                        // the requisition is denied: the spawn never happens and the
                        // director still loses the budget — paid out as holder gold
                        self.monsterCredit = Mathf.Max(0f, self.monsterCredit - cost);
                        uint gold = (uint)(cost * LineItemVeto.GoldPerCredit.Value);
                        GiveGoldToHolders(LineItemVeto.Def, gold);
                        vetoReadyTime = Run.instance.time + Mathf.Max(
                            LineItemVeto.CooldownMin.Value,
                            LineItemVeto.CooldownBase.Value * Mathf.Pow(LineItemVeto.CooldownStackMultiplier.Value, totalStacks - 1));
                        Log.Info($"Line-Item Veto: denied a {cost}-credit spawn, paid {gold} gold.");
                        if (LineItemVeto.BroadcastVetoes.Value)
                        {
                            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                            {
                                baseToken = $"<color=#7fdc5a>Line-Item Veto: a {cost}-credit requisition was denied. {gold} gold recovered.</color>"
                            });
                        }
                        return false;
                    }
                }
            }
            return orig(self, spawnTarget, placementMode);
        }

        // ---------- spawn tagging: Red Tape / Off the Books ----------

        private static void OnBodyStart(CharacterBody body)
        {
            if (!NetworkServer.active || !Run.instance || !body
                || !body.teamComponent || body.teamComponent.teamIndex != TeamIndex.Monster)
            {
                return;
            }
            int redTapeStacks = 0;
            int auditStacks = 0;
            float radiusSq = RedTape.Radius.Value * RedTape.Radius.Value;
            foreach (var controller in PlayerCharacterMasterController.instances)
            {
                var master = controller ? controller.master : null;
                if (!master || !master.inventory)
                {
                    continue;
                }
                var holderBody = master.GetBody();
                if (!holderBody || !holderBody.healthComponent.alive
                    || (holderBody.corePosition - body.corePosition).sqrMagnitude > radiusSq)
                {
                    continue;
                }
                redTapeStacks += master.inventory.GetItemCount(RedTape.Def);
                auditStacks += master.inventory.GetItemCount(OffTheBooks.Def);
            }
            if (redTapeStacks > 0)
            {
                float duration = RedTape.DurationBase.Value + RedTape.DurationPerStack.Value * (redTapeStacks - 1);
                body.AddTimedBuff(RoR2Content.Buffs.Slow60, duration);
                if (RoR2Content.Buffs.Weak)
                {
                    body.AddTimedBuff(RoR2Content.Buffs.Weak, duration);
                }
            }
            bool ledgerSpawn = spawningLedgerWave || (body.master && body.master.GetComponent<LedgerWaveMarker>());
            if (auditStacks > 0 && !ledgerSpawn)
            {
                body.AddTimedBuff(OffTheBooks.AuditedBuff, 3600f);
            }
        }

        private static void OnTakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (NetworkServer.active && damageInfo != null && damageInfo.damage > 0f
                && self.body && self.body.HasBuff(OffTheBooks.AuditedBuff))
            {
                int totalStacks = TotalStacks(OffTheBooks.Def);
                if (totalStacks > 0)
                {
                    damageInfo.damage *= 1f + OffTheBooks.DamageBonusBase.Value
                        + OffTheBooks.DamageBonusPerStack.Value * (totalStacks - 1);
                }
            }
            orig(self, damageInfo);
        }

        // ---------- audited-kill accounting: Off the Books ----------

        private static void OnCharacterDeath(DamageReport report)
        {
            if (!NetworkServer.active || !Run.instance || report == null)
            {
                return;
            }
            var victim = report.victimBody;
            if (!victim || !victim.HasBuff(OffTheBooks.AuditedBuff))
            {
                return;
            }
            var killer = report.attackerMaster;
            if (!killer || !killer.playerCharacterMasterController || !killer.inventory
                || killer.inventory.GetItemCount(OffTheBooks.Def) == 0)
            {
                return;
            }
            int credit = OffTheBooks.MinCreditPerKill.Value;
            var rewards = victim.GetComponent<DeathRewards>();
            if (rewards && rewards.spawnValue > 0)
            {
                credit = Mathf.Max(credit, rewards.spawnValue);
            }
            ledgers.TryGetValue(killer, out float ledger);
            ledger += credit;
            if (ledger >= OffTheBooks.LedgerThreshold.Value)
            {
                ledger = BalanceBooks(killer, ledger);
            }
            ledgers[killer] = ledger;
        }

        // Spend the ledger on a monster wave dropped on the holder; returns the unspent remainder.
        private static float BalanceBooks(CharacterMaster holder, float ledger)
        {
            var body = holder.GetBody();
            if (!body || !body.healthComponent.alive)
            {
                return ledger;
            }
            int spawned = 0;
            spawningLedgerWave = true;
            try
            {
                while (spawned < OffTheBooks.MaxMonstersPerWave.Value)
                {
                    var card = SampleAffordableCard(ledger);
                    if (card == null)
                    {
                        break;
                    }
                    var master = SpawnCardOnTarget(card, body, TeamIndex.Monster, 20f, 45f);
                    if (!master)
                    {
                        break;
                    }
                    master.gameObject.AddComponent<LedgerWaveMarker>();
                    ledger -= card.cost;
                    spawned++;
                }
            }
            finally
            {
                spawningLedgerWave = false;
            }
            if (spawned > 0)
            {
                Log.Info($"Off the Books: balanced {Util.GetBestMasterName(holder)}'s ledger with {spawned} monsters.");
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = $"<color=#b85ce0>{Util.GetBestMasterName(holder)}'s books have been balanced.</color>"
                });
            }
            return ledger;
        }
    }
}
