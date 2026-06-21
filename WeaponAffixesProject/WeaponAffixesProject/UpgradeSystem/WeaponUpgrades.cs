using System;

namespace WeaponAffixesProject
{
    internal static class WeaponUpgrades
    {
        internal static void OnEntityDeathWeaponUpgrades(EntityAlive __instance, EntityPlayerLocal player, ItemValue heldItem)
        {
            int magicSlayerLvl = 0;
            try
            {
                magicSlayerLvl = player.Progression.GetProgressionValue("perkMagicSlayer").level;
            }
            catch (Exception e)
            {
                Log.Out($"Can't find magic slayer perk: '{e}'");
            }

            if (!heldItem.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("weapon")) && !heldItem.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("tool")))
                return;
            if (!heldItem.TryGetMetadata("kills", out float kills) || !heldItem.TryGetMetadata("upgrades", out float upgrades) || !heldItem.TryGetMetadata("nextUpgrade", out float nextUpgrade) || !heldItem.TryGetMetadata("lastUpgrade", out float lastUpgrade))
                return;

            Log.Out($"current kills: {kills}\ncurrent upgrades: {upgrades}\ncurrent nextUpgrade: {nextUpgrade}\ncurrent lastUpgrade: {lastUpgrade}");
            kills += AffixBonusKills.CheckBonusKills(__instance, player, heldItem);
            heldItem.SetMetadata("kills", kills);

            int totalAffixes = 5 + (magicSlayerLvl > 4 ? 1 : 0);
            if (AffixUtils.ChallengeGroupIsCompleted(player, "ram intermediate"))
                totalAffixes++;
            totalAffixes = Math.Min(totalAffixes, AffixUtils.GetConfiguredMaxAffixes());
            int maxUpgrade = 4;
            if (AffixUtils.ChallengeGroupIsCompleted(player, "ram basics"))
                maxUpgrade += 2;

            DoUpgradeUnlockCheck(kills, upgrades, lastUpgrade, nextUpgrade, maxUpgrade, totalAffixes, magicSlayerLvl, heldItem, player);

        }

        internal static void DoUpgradeUnlockCheck(float kills, float upgrades, float lastUpgrade, float nextUpgrade, int maxUpgrade, int totalAffixes, int magicSlayerLvl, ItemValue heldItem, EntityPlayerLocal player)
        {
            try
            {
                bool didUpgrade = false;
                string affixName = "";
                if (nextUpgrade == 0)
                {
                    heldItem.SetMetadata("nextUpgrade", ((AffixUtils.RequiredKills - AffixUtils.magicSlayerBonus * magicSlayerLvl) * (upgrades + 1)));
                    return;
                }
                if (nextUpgrade - lastUpgrade > ((AffixUtils.RequiredKills - AffixUtils.magicSlayerBonus * magicSlayerLvl) * (upgrades + 1)))
                {
                    nextUpgrade = lastUpgrade + ((AffixUtils.RequiredKills - AffixUtils.magicSlayerBonus * magicSlayerLvl) * (upgrades + 1));
                    heldItem.SetMetadata("nextUpgrade", nextUpgrade);
                }

                if (kills >= nextUpgrade)
                {
                    didUpgrade = AffixSystem.CheckUpgradeUnlockAffix(heldItem, ref affixName, totalAffixes, maxUpgrade);
                }
                if (didUpgrade)
                {
                    if (affixName.Contains("Common"))
                        GameManager.ShowTooltip(player, string.Format(Localization.Get("ttaffixunlock", false), upgrades + 1, (AffixUtils.RequiredKills - AffixUtils.magicSlayerBonus * magicSlayerLvl) * (upgrades + 2), affixName), string.Empty, "read_skillbook_final");
                    else
                        GameManager.ShowTooltip(player, string.Format(Localization.Get("ttaffixup", false), upgrades + 1, (AffixUtils.RequiredKills - AffixUtils.magicSlayerBonus * magicSlayerLvl) * (upgrades + 2), affixName), string.Empty, "read_skillbook_final");
                    heldItem.SetMetadata("upgrades", upgrades + 1);
                    heldItem.SetMetadata("nextUpgrade", nextUpgrade + ((AffixUtils.RequiredKills - AffixUtils.magicSlayerBonus * magicSlayerLvl) * (upgrades + 2)));
                    heldItem.SetMetadata("lastUpgrade", kills);
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

        internal static void SetYOffset(ref int _yOffset)
        {
            var lp = GameManager.Instance?.myEntityPlayerLocal;
            if (lp == null) return;

            ItemValue held = lp.inventory?.holdingItemItemValue;
            if (held == null || held.IsEmpty()) return;

            bool hasKills = held.TryGetMetadata("kills", out float _);
            bool hasUpg = held.TryGetMetadata("upgrades", out float _);

            if (hasKills || hasUpg) _yOffset += 46;
        }
    }
}
