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
            
            int totalAffixes = 5 + (magicSlayerLvl > 2 ? 1 : 0) + (magicSlayerLvl > 4 ? 1 : 0);
            int maxUpgrade = 4 + (magicSlayerLvl > 1 ? 1 : 0) + (magicSlayerLvl > 3 ? 1 : 0);
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
                }
            }
            catch (Exception e)
            {
                Log.Out($"'{e}'");
            }
            
        }
    }
}
