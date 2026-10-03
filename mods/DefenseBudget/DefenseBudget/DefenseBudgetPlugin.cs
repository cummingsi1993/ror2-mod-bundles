using BepInEx;
using BepInEx.Configuration;
using DefenseBudget.Artifacts;
using DefenseBudget.Items;
using DefenseBudget.Net;
using R2API;
using R2API.Networking;
using RoR2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace DefenseBudget
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInDependency(NetworkingAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class DefenseBudgetPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Isaac_Cummings";
        public const string PluginName = "DefenseBudget";
        public const string PluginVersion = "1.1.0";

        private const float DamageTickInterval = 0.5f;
        private const float MultiplierTickInterval = 0.2f;

        public static ItemDef itemDef;
        public static BuffDef inDebtBuff;
        public static BuffDef defaultedBuff;

        public static ConfigEntry<float> CostIncreasePerStack;
        public static ConfigEntry<float> IncomeReductionPerStack;
        public static ConfigEntry<int> BaseDebtLimit;
        public static ConfigEntry<float> InterestRatePerSecond;
        public static ConfigEntry<float> DefaultDamagePerSecond;
        public static ConfigEntry<float> DeficitSpendingBonusPerStack;
        public static ConfigEntry<KeyboardShortcut> DebugSpawnItemKey;
        public static ConfigEntry<KeyboardShortcut> DebugSpawnPackKey;

        private static readonly List<CostTracker> costTrackers = new List<CostTracker>();

        // True while the mod itself is dealing default damage. Money gained or lost by
        // on-hurt effects during that window (Roll of Pennies, Brittle Crown) is reverted,
        // so default damage can never be farmed for gold.
        private static bool applyingDefaultDamage;

        internal static bool ApplyingDefaultDamage => applyingDefaultDamage;

        private float interestTimer;
        private float damageTimer;
        private float multiplierTimer;

        public void Awake()
        {
            Log.Init(Logger);
            BindConfig();
            CreateItem();
            CreateBuffs();
            CreateLanguage();

            SavingsBond.Init(Config);
            AccountsReceivable.Init(Config);
            GoldenParachute.Init(Config);
            OvertimePay.Init(Config);
            // must init (and register its GiveMoney hook) BEFORE this plugin's GiveMoney
            // hook below, so tax/debt-repayment runs outermost and the artifact pools the rest
            ArtifactOfCommunism.Init(Config);
            DebtSync.Init();

            On.RoR2.PurchaseInteraction.Awake += PurchaseInteraction_Awake;
            On.RoR2.PurchaseInteraction.CanBeAffordedByInteractor += PurchaseInteraction_CanBeAffordedByInteractor;
            On.RoR2.PurchaseInteraction.OnInteractionBegin += PurchaseInteraction_OnInteractionBegin;
            On.RoR2.CharacterMaster.GiveMoney += CharacterMaster_GiveMoney;
            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
            On.RoR2.UI.HUD.Update += HUD_Update;
            Run.onRunStartGlobal += _ => costTrackers.Clear();

            Log.Info($"{PluginName} loaded.");
        }

        private void BindConfig()
        {
            CostIncreasePerStack = Config.Bind("DefenseBudget", "CostIncreasePerStack", 0.0f,
                "Additional gold-cost multiplier per stack across all players. 2.0 = +200% per stack (x3 with one stack). " +
                "WARNING: chest prices are shared, so in multiplayer this inflates costs for everyone, including players without the item. Disabled by default in favor of IncomeReductionPerStack.");
            IncomeReductionPerStack = Config.Bind("DefenseBudget", "IncomeReductionPerStack", 0.25f,
                "Fraction of gold income lost per stack, multiplicative (0.25 = 75% income with one stack, 56% with two, 42% with three). Only affects the holder.");
            BaseDebtLimit = Config.Bind("DefenseBudget", "BaseDebtLimit", 75,
                "Debt limit per stack, in base gold. Scales with difficulty over time exactly like chest prices (a small chest is 25 base).");
            InterestRatePerSecond = Config.Bind("DefenseBudget", "InterestRatePerSecond", 0.01f,
                "While in debt, debt grows by this fraction of itself per second (minimum 1 gold/sec).");
            DefaultDamagePerSecond = Config.Bind("DefenseBudget", "DefaultDamagePerSecond", 0.02f,
                "Fraction of maximum health lost per second while debt exceeds the debt limit.");
            DeficitSpendingBonusPerStack = Config.Bind("DefenseBudget", "DeficitSpendingBonusPerStack", 0.15f,
                "Damage bonus per stack while in debt ('deficit spending'). Set to 0 to disable.");
            DebugSpawnItemKey = Config.Bind("Debug", "SpawnItemKey", new KeyboardShortcut(KeyCode.F3),
                "Drops a Defense Budget at your feet for testing (host only). Set to an empty shortcut to disable.");
            DebugSpawnPackKey = Config.Bind("Debug", "SpawnPackKey", new KeyboardShortcut(KeyCode.F4),
                "Drops one of each pack item (Savings Bond, Accounts Receivable, Golden Parachute, Overtime Pay) for testing (host only).");
        }

        private void CreateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();
            itemDef.name = "DefenseBudget";
            itemDef.nameToken = "DEFENSE_BUDGET_NAME";
            itemDef.pickupToken = "DEFENSE_BUDGET_PICKUP";
            itemDef.descriptionToken = "DEFENSE_BUDGET_DESC";
            itemDef.loreToken = "DEFENSE_BUDGET_LORE";
            Assets.SetItemTier(itemDef, ItemTier.Lunar);
            itemDef.canRemove = true;
            itemDef.hidden = false;
            itemDef.tags = new[] { ItemTag.Utility, ItemTag.AIBlacklist };
            itemDef.pickupIconSprite = Assets.LoadSprite("DefenseBudget.icon_item.rgba", 128);
            itemDef.pickupModelPrefab = Assets.CreatePickupModel(
                "PickupDefenseBudget", "DefenseBudget.models.dollar_sign.obj", "DefenseBudget.models.dollar_sign.rgba", 512, 0.8f,
                CreateGlyphModel);

            ItemAPI.Add(new CustomItem(itemDef, new ItemDisplayRuleDict(null)));
        }

        // A chunky 3D dollar sign built from cubes (classic 5x7 pixel-font glyph), gold,
        // lightly emissive. The model lives under an inactive holder object so the model
        // itself stays active — instantiated pickups then spawn active.
        private static readonly string[] DollarGlyph =
        {
            "..X..",
            ".XXXX",
            "X.X..",
            ".XXX.",
            "..X.X",
            "XXXX.",
            "..X..",
        };

        // fallback when the generated dollar_sign model resources are absent
        private static GameObject CreateGlyphModel()
        {
            return Assets.CreateGlyphModel("PickupDefenseBudgetGlyph", DollarGlyph, new Color(1f, 0.78f, 0.25f), 0.12f, 0.18f);
        }

        private void CreateBuffs()
        {
            var buffSprite = Assets.LoadSprite("DefenseBudget.icon_buff.rgba", 128);

            inDebtBuff = ScriptableObject.CreateInstance<BuffDef>();
            inDebtBuff.name = "DefenseBudgetInDebt";
            inDebtBuff.iconSprite = buffSprite;
            inDebtBuff.buffColor = new Color32(255, 210, 70, 255);
            inDebtBuff.canStack = false;
            inDebtBuff.isDebuff = false;
            ContentAddition.AddBuffDef(inDebtBuff);

            defaultedBuff = ScriptableObject.CreateInstance<BuffDef>();
            defaultedBuff.name = "DefenseBudgetDefaulted";
            defaultedBuff.iconSprite = buffSprite;
            defaultedBuff.buffColor = new Color32(255, 60, 60, 255);
            defaultedBuff.canStack = false;
            defaultedBuff.isDebuff = false;
            ContentAddition.AddBuffDef(defaultedBuff);
        }

        private void CreateLanguage()
        {
            int costPct = Mathf.RoundToInt(CostIncreasePerStack.Value * 100f);
            int incomePct = Mathf.RoundToInt(IncomeReductionPerStack.Value * 100f);
            float interestPct = InterestRatePerSecond.Value * 100f;
            float damagePct = DefaultDamagePerSecond.Value * 100f;
            int bonusPct = Mathf.RoundToInt(DeficitSpendingBonusPerStack.Value * 100f);

            LanguageAPI.Add("DEFENSE_BUDGET_NAME", "Defense Budget");
            LanguageAPI.Add("DEFENSE_BUDGET_PICKUP",
                "Spend gold you don't have... <style=cDeath>but earn far less.</style>");

            string desc = "";
            if (incomePct > 0)
            {
                desc += $"<style=cDeath>Reduce gold income by {incomePct}%</style> <style=cStack>(stacks multiplicatively)</style>. ";
            }
            if (costPct > 0)
            {
                desc += $"All <style=cIsUtility>gold purchases</style> cost <style=cDeath>+{costPct}% (+{costPct}% per stack)</style>. ";
            }
            desc +=
                $"Gain a <style=cIsUtility>line of credit</style> that scales with difficulty <style=cStack>(+100% per stack)</style>, letting you purchase while in <style=cIsHealth>deficit</style>. " +
                $"While in deficit, all gold income <style=cIsUtility>repays the deficit first</style> and the deficit grows by <style=cDeath>{interestPct:0.#}% per second</style>";
            if (bonusPct > 0)
            {
                desc += $", but you deal <style=cIsDamage>+{bonusPct}% damage (+{bonusPct}% per stack)</style>";
            }
            desc +=
                $". Exceeding your credit limit causes you to <style=cDeath>lose {damagePct:0.#}% of your maximum health per second</style> until the deficit is repaid below the limit.";
            LanguageAPI.Add("DEFENSE_BUDGET_DESC", desc);

            LanguageAPI.Add("DEFENSE_BUDGET_LORE",
                "Order: Line-Item Appropriation \"Defense Budget\"\nTracking Number: 13***********\nEstimated Delivery: Fiscal Year 2056\nShipping Method: Priority\nShipping Address: Committee Chambers, Sub-Basement 4, [REDACTED]\n\nThe committee has reviewed your request for additional funding. The committee reminds you that the budget has tripled for nine consecutive cycles, and that the deficit ceiling is a polite fiction we maintain for the auditors.\n\nSpend it anyway. They always do.\n\nShould expenditures exceed the ceiling, interest will be collected in the only currency of guaranteed supply: you.");
        }

        // ---------------------------------------------------------------
        // Cost inflation
        // ---------------------------------------------------------------

        private class CostTracker : MonoBehaviour
        {
            public PurchaseInteraction purchaseInteraction;
            public float appliedMultiplier = 1f;
            // Wait a couple of multiplier ticks before the first application so
            // controllers (e.g. MultiShopController) finish assigning base costs.
            public int settleTicks = 2;
        }

        private static int TotalPlayerStacks()
        {
            int total = 0;
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                var inventory = pcmc.master ? pcmc.master.inventory : null;
                if (inventory)
                {
                    total += inventory.GetItemCount(itemDef);
                }
            }
            return total;
        }

        private static float CurrentCostMultiplier()
        {
            return 1f + CostIncreasePerStack.Value * TotalPlayerStacks();
        }

        private void PurchaseInteraction_Awake(On.RoR2.PurchaseInteraction.orig_Awake orig, PurchaseInteraction self)
        {
            orig(self);
            if (!NetworkServer.active || self.costType != CostTypeIndex.Money)
            {
                return;
            }
            var tracker = self.gameObject.AddComponent<CostTracker>();
            tracker.purchaseInteraction = self;
            costTrackers.Add(tracker);
        }

        private static void ApplyMultiplier(CostTracker tracker, float multiplier)
        {
            if (Mathf.Approximately(tracker.appliedMultiplier, multiplier))
            {
                return;
            }
            var pi = tracker.purchaseInteraction;
            if (pi.cost > 0 && pi.Networkcost > 0)
            {
                pi.Networkcost = Mathf.Max(1, Mathf.RoundToInt(pi.Networkcost * (multiplier / tracker.appliedMultiplier)));
            }
            tracker.appliedMultiplier = multiplier;
        }

        private static void UpdateCostMultipliers()
        {
            float multiplier = CurrentCostMultiplier();
            for (int i = costTrackers.Count - 1; i >= 0; i--)
            {
                var tracker = costTrackers[i];
                if (!tracker || !tracker.purchaseInteraction)
                {
                    costTrackers.RemoveAt(i);
                    continue;
                }
                if (tracker.settleTicks > 0)
                {
                    tracker.settleTicks--;
                    if (tracker.settleTicks > 0)
                    {
                        continue;
                    }
                }
                ApplyMultiplier(tracker, multiplier);
            }
        }

        // ---------------------------------------------------------------
        // Debt / credit
        // ---------------------------------------------------------------

        private class DebtTracker : MonoBehaviour
        {
            public double debt;
            public bool wasDefaulted;
        }

        private static DebtTracker GetDebt(CharacterMaster master)
        {
            return master ? master.GetComponent<DebtTracker>() : null;
        }

        private static DebtTracker GetOrCreateDebt(CharacterMaster master)
        {
            var tracker = master.GetComponent<DebtTracker>();
            return tracker ? tracker : master.gameObject.AddComponent<DebtTracker>();
        }

        private static int GetStacks(CharacterMaster master)
        {
            return master && master.inventory ? master.inventory.GetItemCount(itemDef) : 0;
        }

        private static double DebtLimit(CharacterMaster master)
        {
            int stacks = GetStacks(master);
            if (stacks <= 0 || !Run.instance)
            {
                return 0.0;
            }
            return (double)Run.instance.GetDifficultyScaledCost(BaseDebtLimit.Value) * stacks;
        }

        private static CharacterMaster GetMaster(Interactor activator)
        {
            if (!activator)
            {
                return null;
            }
            var body = activator.GetComponent<CharacterBody>();
            return body ? body.master : null;
        }

        private bool PurchaseInteraction_CanBeAffordedByInteractor(
            On.RoR2.PurchaseInteraction.orig_CanBeAffordedByInteractor orig,
            PurchaseInteraction self, Interactor activator)
        {
            if (orig(self, activator))
            {
                return true;
            }
            if (self.costType != CostTypeIndex.Money || self.cost <= 0)
            {
                return false;
            }
            var master = GetMaster(activator);
            if (!master || GetStacks(master) <= 0)
            {
                return false;
            }
            GetCreditState(master, out double debt, out double limit);
            double credit = limit - debt;
            return credit > 0.0 && master.money + credit >= self.cost;
        }

        // Affordability also runs on clients (interaction prompt, outline, sale-star glow),
        // where there is no DebtTracker — use the server's replicated numbers there. The
        // server still decides every purchase.
        private static void GetCreditState(CharacterMaster master, out double debt, out double limit)
        {
            if (NetworkServer.active)
            {
                var debtTracker = GetDebt(master);
                debt = debtTracker != null ? debtTracker.debt : 0.0;
                limit = DebtLimit(master);
            }
            else if (DebtSync.TryGet(master, out var view))
            {
                debt = view.debt;
                limit = view.limit;
            }
            else
            {
                // no snapshot yet: assume a clean slate
                debt = 0.0;
                limit = DebtLimit(master);
            }
        }

        private void PurchaseInteraction_OnInteractionBegin(
            On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig,
            PurchaseInteraction self, Interactor activator)
        {
            if (NetworkServer.active && self.costType == CostTypeIndex.Money && self.cost > 0)
            {
                var master = GetMaster(activator);
                if (master && master.money < (uint)self.cost && GetStacks(master) > 0)
                {
                    uint shortfall = (uint)self.cost - master.money;
                    var debtTracker = GetOrCreateDebt(master);
                    if (debtTracker.debt + shortfall <= DebtLimit(master) + 0.5)
                    {
                        // Take on debt and top the wallet up to exactly the price, so the
                        // vanilla payment code deducts it back to zero without underflowing.
                        debtTracker.debt += shortfall;
                        master.money = (uint)self.cost;
                    }
                }
            }
            orig(self, activator);
        }

        private void CharacterMaster_GiveMoney(On.RoR2.CharacterMaster.orig_GiveMoney orig, CharacterMaster self, uint amount)
        {
            if (NetworkServer.active && amount > 0 && !applyingDefaultDamage)
            {
                // the defense budget taxes all gold income, then debt repayment takes the rest
                int stacks = GetStacks(self);
                if (stacks > 0 && IncomeReductionPerStack.Value > 0f)
                {
                    amount = (uint)Math.Round(amount * Math.Pow(1.0 - Mathf.Clamp01(IncomeReductionPerStack.Value), stacks));
                }
                var debtTracker = GetDebt(self);
                if (debtTracker != null && debtTracker.debt > 0.0)
                {
                    uint payment = (uint)Math.Min(amount, Math.Ceiling(debtTracker.debt));
                    debtTracker.debt = Math.Max(0.0, debtTracker.debt - payment);
                    amount -= payment;
                }
            }
            orig(self, amount);
        }

        // ---------------------------------------------------------------
        // Deficit spending damage bonus
        // ---------------------------------------------------------------

        private void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (NetworkServer.active && DeficitSpendingBonusPerStack.Value > 0f && damageInfo != null && damageInfo.attacker)
            {
                var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                var master = attackerBody ? attackerBody.master : null;
                if (master)
                {
                    int stacks = GetStacks(master);
                    if (stacks > 0)
                    {
                        var debtTracker = GetDebt(master);
                        if (debtTracker != null && debtTracker.debt > 0.0)
                        {
                            damageInfo.damage *= 1f + DeficitSpendingBonusPerStack.Value * stacks;
                        }
                    }
                }
            }
            orig(self, damageInfo);
        }

        // ---------------------------------------------------------------
        // HUD: show deficit as negative gold
        // ---------------------------------------------------------------

        private static readonly Color DeficitColor = new Color(1f, 0.35f, 0.3f);
        private static readonly Color DefaultedColor = new Color(1f, 0.12f, 0.12f);
        private static Color moneyTextOriginalColor;
        private static bool moneyTextColorCached;

        // Per HUD, after vanilla writes moneyText.targetValue from hud.targetMaster.money:
        // covers splitscreen (one HUD per local user) and spectating (targetMaster follows
        // the camera target). Reads the replicated snapshot, so it works for every player,
        // not just the host.
        private void HUD_Update(On.RoR2.UI.HUD.orig_Update orig, RoR2.UI.HUD self)
        {
            orig(self);
            var moneyText = self.moneyText;
            if (!moneyText || !moneyText.targetText)
            {
                return;
            }
            if (!moneyTextColorCached)
            {
                moneyTextOriginalColor = moneyText.targetText.color;
                moneyTextColorCached = true;
            }
            if (DebtSync.TryGet(self.targetMaster, out var view) && view.debt > 0)
            {
                // Income repays debt first, so an indebted wallet is normally empty. With
                // the Artifact of Communism it mirrors the shared pool instead — keep
                // showing that spendable balance, just tinted.
                if (self.targetMaster.money == 0)
                {
                    moneyText.targetValue = -view.debt;
                }
                moneyText.targetText.color = view.defaulted ? DefaultedColor : DeficitColor;
            }
            else
            {
                moneyText.targetText.color = moneyTextOriginalColor;
            }
        }

        // ---------------------------------------------------------------
        // Debug
        // ---------------------------------------------------------------

        private void Update()
        {
            bool spawnLunar = DebugSpawnItemKey.Value.IsDown();
            bool spawnPack = DebugSpawnPackKey.Value.IsDown();
            if ((!spawnLunar && !spawnPack) || !NetworkServer.active || !Run.instance)
            {
                return;
            }
            var localUser = LocalUserManager.GetFirstLocalUser();
            var body = localUser?.cachedBody;
            if (!body)
            {
                return;
            }
            // body.transform compiles against RoR2's private cached field and throws
            // FieldAccessException at runtime; go through the GameObject instead.
            var forward = body.gameObject.transform.forward;
            if (spawnLunar)
            {
                Log.Info("Debug: spawning Defense Budget");
                PickupDropletController.CreatePickupDroplet(
                    PickupCatalog.FindPickupIndex(itemDef.itemIndex),
                    body.corePosition + Vector3.up * 1.5f,
                    forward * 10f);
            }
            if (spawnPack)
            {
                Log.Info("Debug: spawning item pack");
                var packDefs = new[] { SavingsBond.Def, AccountsReceivable.Def, GoldenParachute.Def, OvertimePay.Def };
                for (int i = 0; i < packDefs.Length; i++)
                {
                    var direction = Quaternion.AngleAxis(-30f + 20f * i, Vector3.up) * forward;
                    PickupDropletController.CreatePickupDroplet(
                        PickupCatalog.FindPickupIndex(packDefs[i].itemIndex),
                        body.corePosition + Vector3.up * 1.5f,
                        direction * 10f);
                }
            }
        }

        // ---------------------------------------------------------------
        // Per-tick simulation: interest, buffs, default damage
        // ---------------------------------------------------------------

        private void FixedUpdate()
        {
            if (!NetworkServer.active || !Run.instance)
            {
                return;
            }

            interestTimer += Time.fixedDeltaTime;
            damageTimer += Time.fixedDeltaTime;
            multiplierTimer += Time.fixedDeltaTime;

            bool doInterest = false;
            bool doDamage = false;
            if (interestTimer >= 1f)
            {
                interestTimer -= 1f;
                doInterest = true;
            }
            if (damageTimer >= DamageTickInterval)
            {
                damageTimer -= DamageTickInterval;
                doDamage = true;
            }
            if (multiplierTimer >= MultiplierTickInterval)
            {
                multiplierTimer = 0f;
                UpdateCostMultipliers();
            }

            SavingsBond.FixedUpdate();
            OvertimePay.FixedUpdate();
            ArtifactOfCommunism.FixedUpdate();

            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                var master = pcmc.master;
                if (!master)
                {
                    continue;
                }
                var debtTracker = GetDebt(master);
                var body = master.GetBody();
                bool inDebt = debtTracker != null && debtTracker.debt > 0.0;
                double limit = DebtLimit(master);

                if (inDebt && doInterest)
                {
                    debtTracker.debt += Math.Max(1.0, debtTracker.debt * InterestRatePerSecond.Value);
                }

                bool defaulted = inDebt && debtTracker.debt > limit;

                if (body)
                {
                    UpdateBuff(body, inDebtBuff, inDebt && !defaulted);
                    UpdateBuff(body, defaultedBuff, defaulted);
                }

                if (debtTracker != null)
                {
                    if (defaulted && !debtTracker.wasDefaulted)
                    {
                        AnnounceDefault(master);
                    }
                    debtTracker.wasDefaulted = defaulted;
                }

                if (defaulted && doDamage && body && body.healthComponent && body.healthComponent.alive)
                {
                    ApplyDefaultDamage(master, body);
                }
            }

            DebtSync.ServerTick(Time.fixedDeltaTime, FillDebtSnapshot);
        }

        private static void FillDebtSnapshot(List<DebtSnapshotMessage.Entry> entries)
        {
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                var master = pcmc.master;
                if (!master)
                {
                    continue;
                }
                var debtTracker = GetDebt(master);
                double debt = debtTracker != null ? debtTracker.debt : 0.0;
                if (debt <= 0.0 && GetStacks(master) <= 0)
                {
                    continue;
                }
                entries.Add(new DebtSnapshotMessage.Entry
                {
                    masterId = master.netId,
                    // ceiling, so a fractional debt never displays as 0
                    debt = ClampToInt(Math.Ceiling(debt)),
                    // the host's config and difficulty are authoritative
                    limit = ClampToInt(Math.Floor(DebtLimit(master))),
                    defaulted = debtTracker != null && debtTracker.wasDefaulted,
                });
            }
        }

        private static int ClampToInt(double value)
        {
            return value <= 0.0 ? 0 : value >= int.MaxValue ? int.MaxValue : (int)value;
        }

        private static void UpdateBuff(CharacterBody body, BuffDef buff, bool active)
        {
            bool has = body.HasBuff(buff);
            if (active && !has)
            {
                body.AddBuff(buff);
            }
            else if (!active && has)
            {
                body.RemoveBuff(buff);
            }
        }

        private static void ApplyDefaultDamage(CharacterMaster master, CharacterBody body)
        {
            var healthComponent = body.healthComponent;
            var debtTracker = GetDebt(master);
            uint moneyBefore = master.money;
            double debtBefore = debtTracker != null ? debtTracker.debt : 0.0;

            applyingDefaultDamage = true;
            try
            {
                healthComponent.TakeDamage(new DamageInfo
                {
                    damage = Mathf.Max(1f, healthComponent.fullCombinedHealth * DefaultDamagePerSecond.Value * DamageTickInterval),
                    position = body.corePosition,
                    attacker = null,
                    inflictor = null,
                    crit = false,
                    damageColorIndex = DamageColorIndex.Item,
                    damageType = DamageType.BypassArmor | DamageType.BypassBlock,
                });
            }
            finally
            {
                applyingDefaultDamage = false;
                // Void any gold gained or lost by on-hurt effects from this tick
                // (Roll of Pennies, Brittle Crown) so the debt spiral stays honest.
                master.money = moneyBefore;
                if (debtTracker != null)
                {
                    debtTracker.debt = debtBefore;
                }
            }
        }

        private static void AnnounceDefault(CharacterMaster master)
        {
            string name = "A survivor";
            var pcmc = master.playerCharacterMasterController;
            if (pcmc && pcmc.networkUser)
            {
                name = pcmc.networkUser.userName;
            }
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = $"<color=#ff5544>{name} has defaulted on their Defense Budget! Repay the deficit or perish.</color>"
            });
        }

        // ---------------------------------------------------------------
        // Credit helpers shared with other items (Golden Parachute)
        // ---------------------------------------------------------------

        internal static double GetAvailableCredit(CharacterMaster master)
        {
            if (!master)
            {
                return 0.0;
            }
            var debtTracker = GetDebt(master);
            double debt = debtTracker != null ? debtTracker.debt : 0.0;
            return Math.Max(0.0, DebtLimit(master) - debt);
        }

        // Pays from gold first, putting any remainder on the Defense Budget deficit.
        // Callers must verify money + GetAvailableCredit covers the cost.
        internal static void PayWithCredit(CharacterMaster master, uint cost)
        {
            uint fromMoney = Math.Min(master.money, cost);
            master.money -= fromMoney;
            uint remainder = cost - fromMoney;
            if (remainder > 0)
            {
                GetOrCreateDebt(master).debt += remainder;
            }
        }
    }
}
