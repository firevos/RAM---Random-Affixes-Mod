using HarmonyLib;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(EntityAlive), nameof(EntityAlive.OnEntityDeath))]
    public static class EntityAliveOnEntityDeath
    {
        public static void Postfix(EntityAlive __instance)
        {
            Log.Out($"'{__instance.LocalizedEntityName}'");
            if (!__instance.HasAnyTags(FastTags<TagGroup.Global>.GetTag("zombie"))) return;

            var player = GameManager.Instance?.myEntityPlayerLocal;
            if (player == null) return;

            ItemValue heldItem = player.inventory?.holdingItemItemValue;
            if (heldItem == null || heldItem.IsEmpty()) return;

            CheckChallengeUpdates(heldItem);

            WeaponUpgrades.OnEntityDeathWeaponUpgrades(__instance, player, heldItem);

            AffixIncreaseBuffDuration.IncreaseBuffDurationAffix(player, heldItem);
        }

        internal static void CheckChallengeUpdates(ItemValue heldItem)
        {
            if (heldItem.CosmeticMods == null || heldItem.Modifications == null)
                return;

            int totalAffixes = 0;
            int rareAffixes = 0;
            int mythicAffixes = 0;
            foreach (var mod in heldItem.CosmeticMods)
            {
                if (mod == null || mod.IsEmpty() || !mod.ItemClass.HasAnyTags(AffixUtils.AffixTag))
                    continue;

                totalAffixes++;
                if (mod.ItemClass.Name[mod.ItemClass.Name.Length - 1] - '0' > 2)
                    rareAffixes++;

                if (mod.ItemClass.Name[mod.ItemClass.Name.Length - 1] - '0' > 5)
                    mythicAffixes++;
            }
            foreach (var mod in heldItem.Modifications)
            {
                if (mod == null || mod.IsEmpty() || !mod.ItemClass.HasAnyTags(AffixUtils.AffixTag))
                    continue;

                totalAffixes++;
                if (mod.ItemClass.Name[mod.ItemClass.Name.Length - 1] - '0' > 5)
                    mythicAffixes++;
            }

            // Get kill with any affix
            if (totalAffixes > 0)
                AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamagePerc1");

            // Get kill with 3+ affixes
            if (totalAffixes > 2)
                AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamagePerc5");

            // Get kill with any rare+ affix
            if (rareAffixes > 0)
                AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamagePerc4");

            // Get kill with 6+ affixes
            if (totalAffixes > 5)
                AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamageBase2");

            // Get kill with 5+ mythical affixes
            if (mythicAffixes > 4)
                AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamageBase4");

            // Get kill with 11+ affixes
            if (totalAffixes > 10)
                AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamageBase5");

        }
    }
}
