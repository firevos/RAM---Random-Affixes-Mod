using System;
using System.Collections.Generic;
using static Prefab;

namespace WeaponAffixesProject
{
    internal static class AffixSystem
    {
        internal static void ApplyUniqueAffixes(ItemValue itemValue, EntityPlayer player)
        {
            int count = 0;
            List<string> tags = itemValue.ItemClass.ItemTags.GetTagNames();
            List<string> modsToApply = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i].Contains("affixMod"))
                {
                    modsToApply.Add(tags[i]);
                    count++;
                }
            }

            var newCosmeticModsList = new ItemValue[count];
            for (int i = 0; i < count; i++)
                newCosmeticModsList[i] = ItemClassModifier.GetItem(modsToApply[i]);
            itemValue.CosmeticMods = newCosmeticModsList;
        }

        internal static void ApplyAffixMods(ItemValue itemValue, EntityPlayer player)
        {
            if (itemValue == null)
            {
                Log.Out("ItemValue is null");
                return;
            }

            if (itemValue.ItemClass.HasAnyTags(AffixUtils.UniqueAffixTag))
            {
                if (itemValue.ItemClass.Name != "meleeWpnBladeT1HuntingKnifeUnique")
                    itemValue.Quality = 7;
                ApplyUniqueAffixes(itemValue, player);
                return;
            }

            // Parse the modlist to see which mods can be added
            List<List<ItemClassModifier>> weaponMods = AffixUtils.GetCorrectModList(itemValue);
            if (weaponMods.Count <= 0)
            {
                Log.Out("No affixes found to apply");
                return;
            }

            // Check how many mods to add to the weapon
            int toAdd = CountModsToApply(itemValue, player);

            // Add random mods
            for (int i = 0; i < toAdd; i++)
            {
                // For each mod to add, first decide on which tier mod to add
                int selectedTier = AffixUtils.RandomizeTierWithOdds(itemValue, player);
                ItemClassModifier selectedMod = weaponMods[selectedTier][AffixUtils.rng.Next(weaponMods[selectedTier].Count)];

                // Find first empty cosmetic slot
                for (int j = 0; j < itemValue.CosmeticMods.Length; j++)
                {
                    if (itemValue.CosmeticMods[j] == null || itemValue.CosmeticMods[j].IsEmpty())
                    {
                        // Install affix mod into cosmetic slot
                        itemValue.CosmeticMods[j] = new ItemValue(selectedMod.Id);
                        weaponMods = AffixUtils.RemoveSimilarMods(weaponMods, selectedMod);
                        break;
                    }
                }
                if (weaponMods.Count <= 0) return;
            }
        }

        internal static int CountModsToApply(ItemValue itemValue, EntityPlayer player)
        {
            if (itemValue == null || itemValue.IsEmpty())
            {
                Log.Out("ItemValue is null in countmodstoapply");
                return 0;
            }

            // Some server-spawned loot items can have null Modifications arrays.
            int affixSlots = itemValue.Modifications?.Length ?? 0;
            if (affixSlots <= 0)
                affixSlots = 1;

            int magicFindLvl = 0;
            try
            {
                if (player?.Progression != null)
                {
                    magicFindLvl = player.Progression.GetProgressionValue("perkMagicFind").level;
                }
            }
            catch (Exception e)
            {
                Log.Out($"Can't find the magic Find perk. '{e}'");
            }
            if (magicFindLvl > 4) affixSlots++;
            if (magicFindLvl > 3 && itemValue.Quality == 6) affixSlots++;

            if (itemValue.CosmeticMods == null || itemValue.CosmeticMods.Length < affixSlots)
            {
                var newCosmetic = new ItemValue[affixSlots];
                if (itemValue.CosmeticMods != null)
                    Array.Copy(itemValue.CosmeticMods, newCosmetic, itemValue.CosmeticMods.Length);
                itemValue.CosmeticMods = newCosmetic;
            }

            // Count existing affixes
            int currentAffixes = 0;
            foreach (var mod in itemValue.CosmeticMods)
                if (mod?.ItemClass != null && AffixUtils.IsAffixMod(mod.ItemClass)) currentAffixes++;

            return affixSlots - currentAffixes;
        }


        internal static bool UpgradeAffix(ItemValue itemValue, List<int> canUpgradeSlots, ref string affixName)
        {
            Log.Out("We're in UpgradeAffix.");
            // randomly select one of the affixes to upgrade
            if (canUpgradeSlots.Count == 0)
            {
                Log.Out("No upgrade slots available while trying to upgrade.");
                return false;
            }
            if (itemValue.CosmeticMods.Length == 0)
            {
                Log.Out("No affixes installed to upgrade");
                return false;
            }

            int randomIndex = canUpgradeSlots[AffixUtils.rng.Next(canUpgradeSlots.Count)];
            ItemClassModifier oldAffix = itemValue.CosmeticMods[randomIndex].ItemClass as ItemClassModifier;
            int oldTier = int.Parse(oldAffix.Name.Substring(oldAffix.Name.Length - 1));
            string newName = oldAffix.Name.Substring(0, oldAffix.Name.Length - 1) + (oldTier + 1).ToString();

            if (!(ItemClass.GetItemClass(newName) is ItemClassModifier newAffix)) return false;

            affixName = newAffix.localizedName;
            if (oldAffix.Name.Contains("KillStreak")) GameManager.Instance.myEntityPlayerLocal.Buffs.RemoveBuff("buff" + oldAffix.Name);

            // then apply the upgrade
            itemValue.CosmeticMods[randomIndex] = new ItemValue(newAffix.Id);
            return true;
        }

        internal static bool AddNewAffix(ItemValue itemValue, ref string affixName)
        {
            List<List<ItemClassModifier>> modList = AffixUtils.GetCorrectModList(itemValue);
            if (modList == null || modList.Count == 0) return false;

            if (itemValue.CosmeticMods[0] != null && !itemValue.CosmeticMods[0].IsEmpty())
            {
                for (int i = 0; i < itemValue.CosmeticMods.Length; i++)
                {
                    if (itemValue.CosmeticMods[i] == null || itemValue.CosmeticMods[i].IsEmpty()) continue;
                    ItemClassModifier mod = itemValue.CosmeticMods[i].ItemClass as ItemClassModifier;
                    modList = AffixUtils.RemoveSimilarMods(modList, mod);
                }

                // first clear all buffs from affixes, either here or in xml make sure it deactivates before removing.
                int affixSlots = itemValue.CosmeticMods.Length + 1;
                var newCosmetic = new ItemValue[affixSlots];
                if (itemValue.CosmeticMods != null && !itemValue.CosmeticMods[0].IsEmpty())
                    Array.Copy(itemValue.CosmeticMods, newCosmetic, itemValue.CosmeticMods.Length);
                itemValue.CosmeticMods = newCosmetic;
            }

            ItemClassModifier selectedMod = modList[0][AffixUtils.rng.Next(modList[0].Count)];
            if (selectedMod == null) return false;
            affixName = selectedMod.localizedName;

            // Find first empty cosmetic slot
            for (int j = 0; j < itemValue.CosmeticMods.Length; j++)
            {
                if (itemValue.CosmeticMods[j] == null ||
                        itemValue.CosmeticMods[j].IsEmpty() ||
                        itemValue.CosmeticMods[j].ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("dye"))
                   )
                {
                    // Install affix mod into cosmetic slot
                    itemValue.CosmeticMods[j] = new ItemValue(selectedMod.Id);
                    return true;
                }
            }
            return false;
        }

        internal static bool CheckUpgradeUnlockAffix(ItemValue itemValue, ref string affixName, int totalAffixes, int maxUpgrade)
        {
            // Check if you should upgrade
                
            bool didUpgrade = false;
            // If an empty slot is available, unlock a new one.
            if (itemValue.CosmeticMods.Length < totalAffixes)
            {
                if (itemValue.CosmeticMods[0] == null || itemValue.CosmeticMods[0].IsEmpty()) didUpgrade = AffixSystem.AddNewAffix(itemValue, ref affixName);
                else if (!itemValue.CosmeticMods[0].ItemClass.HasAnyTags(AffixUtils.AffixTag)) didUpgrade = AffixSystem.AddNewAffix(itemValue, ref affixName);
                else if (AffixUtils.rng.Next(0, 100) > AffixUtils.unlockNewAffixChance) didUpgrade = AffixSystem.AddNewAffix(itemValue, ref affixName);
            }
            if (!didUpgrade)
            {
                // Otherwise upgrade an existing affix.
                // Check if all affixes are max rarity
                List<int> canUpgradeSlots = new List<int>();
                for (int i = 0; i < itemValue.CosmeticMods.Length; i++)
                {
                    if (itemValue.CosmeticMods[i] == null) continue;
                    string rarityString = itemValue.CosmeticMods[i].ItemClass.Name.Substring(itemValue.CosmeticMods[i].ItemClass.Name.Length - 1);
                    if (int.TryParse(rarityString, out int rarity))
                        if (rarity < maxUpgrade) canUpgradeSlots.Add(i);
                }

                // If no slots can be upgraded, see if you can add a new one instead.
                if (canUpgradeSlots.Count == 0)
                {
                    if (itemValue.CosmeticMods.Length < totalAffixes) didUpgrade = AffixSystem.AddNewAffix(itemValue, ref affixName);
                    else return false;
                }
                else didUpgrade = AffixSystem.UpgradeAffix(itemValue, canUpgradeSlots, ref affixName);
            }

            return didUpgrade;
        }
    }
}
