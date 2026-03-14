using HarmonyLib;
using System;
using System.Collections.Generic;

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

            int magicSlayerLvl = 0;
            try
            {
                magicSlayerLvl = player.Progression.GetProgressionValue("perkMagicSlayer").level;
            }
            catch (Exception e)
            {
                Log.Out($"Can't find magic slayer perk: '{e}'");
            }
            float kills = 0, upgrades = 0, nextUpgrade = 0, lastUpgrade = 0;

            if (heldItem.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("weapon")) || heldItem.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("tool")))
            {
                // If it is a weapon, make sure it has the kills and upgrades metadata
                if (!heldItem.TryGetMetadata("kills", out kills) ||
                !heldItem.TryGetMetadata("upgrades", out upgrades) ||
                !heldItem.TryGetMetadata("nextUpgrade", out nextUpgrade) ||
                !heldItem.TryGetMetadata("lastUpgrade", out lastUpgrade))
                {
                    Log.Out("Missing metadata on weapon.");
                    return;
                }
            }
            heldItem.SetMetadata("kills", kills + 1);
            Log.Out($"current kills: '{kills}'");
            Log.Out($"current upgrades: '{upgrades}'");
            Log.Out($"current nextUpgrade: '{nextUpgrade}'");
            Log.Out($"current lastUpgrade: '{lastUpgrade}'");

            int totalAffixes = 5 + (magicSlayerLvl > 4 ? 1 : 0);
            if (AffixUtils.ChallengeGroupIsCompleted(player, "ram intermediate"))
            {
                totalAffixes++;
            }
            int maxUpgrade = 4;
            if (AffixUtils.ChallengeGroupIsCompleted(player, "ram basics"))
            {
                maxUpgrade += 2;
            }
            bool didUpgrade = false;
            string affixName = "";
            try 
            {
                if (nextUpgrade == 0)
                {
                    heldItem.SetMetadata("nextUpgrade", ((AffixUtils.requiredKills - 10 * magicSlayerLvl) * (upgrades + 1)));
                    return;
                }
                if (nextUpgrade - lastUpgrade > ((AffixUtils.requiredKills - 10 * magicSlayerLvl) * (upgrades + 1)))
                {
                    nextUpgrade = lastUpgrade + ((AffixUtils.requiredKills - 10 * magicSlayerLvl) * (upgrades + 1));
                    heldItem.SetMetadata("nextUpgrade", nextUpgrade);
                }

                if (kills + 1 >= nextUpgrade)
                {
                    didUpgrade = AffixSystem.CheckUpgradeUnlockAffix(heldItem, ref affixName, totalAffixes, maxUpgrade);
                }
                if (didUpgrade)
                {
                    if (affixName.Contains("Common"))
                        GameManager.ShowTooltip(player, string.Format(Localization.Get("ttaffixunlock", false), upgrades + 1, (AffixUtils.requiredKills - 10 * magicSlayerLvl) * (upgrades + 2), affixName), string.Empty, "read_skillbook_final");
                    else
                        GameManager.ShowTooltip(player, string.Format(Localization.Get("ttaffixup", false), upgrades + 1, (AffixUtils.requiredKills - 10 * magicSlayerLvl) * (upgrades + 2), affixName), string.Empty, "read_skillbook_final");
                    heldItem.SetMetadata("upgrades", upgrades + 1);
                    heldItem.SetMetadata("nextUpgrade", nextUpgrade + ((AffixUtils.requiredKills - 10 * magicSlayerLvl) * (upgrades + 2)));
                    heldItem.SetMetadata("lastUpgrade", kills + 1);
                    if (upgrades + 1 >= 15)
                    {
                        // 15 upgrades on 1 weapon
                        AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamageBase3");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out($"'{e}'");
            }
            
        }

        internal static void CheckChallengeUpdates(ItemValue heldItem)
        {
            Log.Out("Checking challenges");
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
                if (mod.ItemClass.Name[mod.ItemClass.Name.Length -1] - '0' > 2)
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
