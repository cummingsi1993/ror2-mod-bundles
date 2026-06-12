using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AuditDepartment.Items
{
    // Void (corrupts Red Tape): enemies spawning nearby are "audited" — they take
    // increased damage from everyone, but each audited enemy YOU kill accrues its spawn
    // cost to a personal ledger. When the ledger fills, the books are balanced: a
    // monster wave is bought with it and spawned on YOU, not the lobby. Mechanics live
    // in DirectorHooks; this file is definition, buff, and corruption pairing.
    internal static class OffTheBooks
    {
        internal static ItemDef Def;
        internal static BuffDef AuditedBuff;
        internal static ConfigEntry<float> DamageBonusBase;
        internal static ConfigEntry<float> DamageBonusPerStack;
        internal static ConfigEntry<int> LedgerThreshold;
        internal static ConfigEntry<int> MinCreditPerKill;
        internal static ConfigEntry<int> MaxMonstersPerWave;

        internal static void Init(ConfigFile config)
        {
            DamageBonusBase = config.Bind("OffTheBooks", "DamageBonusBase", 0.15f,
                "Bonus damage audited enemies take from all sources, with one stack in the lobby.");
            DamageBonusPerStack = config.Bind("OffTheBooks", "DamageBonusPerStack", 0.05f,
                "Additional bonus damage per stack beyond the first (all holders combined).");
            LedgerThreshold = config.Bind("OffTheBooks", "LedgerThreshold", 300,
                "Director credits of audited kills a holder accrues before the books are balanced with a wave spawned on them.");
            MinCreditPerKill = config.Bind("OffTheBooks", "MinCreditPerKill", 15,
                "Minimum credits an audited kill adds to the ledger (used when the victim has no recorded spawn cost).");
            MaxMonstersPerWave = config.Bind("OffTheBooks", "MaxMonstersPerWave", 3,
                "Maximum monsters spawned per ledger balancing.");

            Def = Assets.CreateItemDef(
                "OffTheBooks", "OFF_THE_BOOKS", ItemTier.VoidTier1,
                Assets.LoadSprite("AuditDepartment.icon_off_the_books.rgba", 128),
                Assets.CreatePickupModel("PickupOffTheBooks", "AuditDepartment.models.off_the_books.obj", "AuditDepartment.models.off_the_books.rgba", 512, 0.5f),
                new[] { ItemTag.Damage, ItemTag.AIBlacklist });
            Def.requiredExpansion = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();
            ItemAPI.Add(new CustomItem(Def, new ItemDisplayRuleDict(null)));

            AuditedBuff = ScriptableObject.CreateInstance<BuffDef>();
            AuditedBuff.name = "AuditDepartmentAudited";
            AuditedBuff.buffColor = new Color(0.72f, 0.36f, 0.89f);
            AuditedBuff.canStack = false;
            AuditedBuff.isDebuff = true;
            AuditedBuff.iconSprite = Assets.LoadSprite("AuditDepartment.icon_buff_audited.rgba", 128);
            ContentAddition.AddBuffDef(AuditedBuff);

            // corruption pairing against our own Red Tape — both defs exist at Awake,
            // so no addressable lookup or timing dance is needed
            var provider = ScriptableObject.CreateInstance<ItemRelationshipProvider>();
            provider.name = "AuditDepartmentContagiousItems";
            provider.relationshipType = Addressables.LoadAssetAsync<ItemRelationshipType>("RoR2/DLC1/Common/ContagiousItem.asset").WaitForCompletion();
            provider.relationships = new[]
            {
                new ItemDef.Pair { itemDef1 = RedTape.Def, itemDef2 = Def },
            };
            ContentAddition.AddItemRelationshipProvider(provider);

            int basePct = Mathf.RoundToInt(DamageBonusBase.Value * 100f);
            int stackPct = Mathf.RoundToInt(DamageBonusPerStack.Value * 100f);
            LanguageAPI.Add("OFF_THE_BOOKS_NAME", "Off the Books");
            LanguageAPI.Add("OFF_THE_BOOKS_PICKUP",
                "Nearby spawns are audited and take extra damage... but your kills go off the books, and the books always balance. <style=cIsVoid>Corrupts all Red Tape</style>.");
            LanguageAPI.Add("OFF_THE_BOOKS_DESC",
                $"Enemies that spawn nearby are <style=cIsVoid>audited</style>, taking " +
                $"<style=cIsDamage>+{basePct}% <style=cStack>(+{stackPct}% per stack)</style> damage</style> from all sources. " +
                $"Audited enemies <style=cIsHealth>you</style> kill accrue their spawn cost to a ledger; at " +
                $"<style=cIsHealth>{LedgerThreshold.Value} credits</style> the books are balanced with a monster wave spawned " +
                $"<style=cIsHealth>on you</style>. <style=cIsVoid>Corrupts all Red Tape</style>.");
            LanguageAPI.Add("OFF_THE_BOOKS_LORE",
                "The auditor's ledger has two columns. The first records what was spent. The second records what was owed. Nothing in the ledger explains to whom, and the auditor has stopped asking, because every time the columns are unequal, something arrives to correct them.");
        }
    }
}
