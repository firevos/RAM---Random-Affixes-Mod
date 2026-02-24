using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WeaponAffixesProject;

namespace WeaponBuffMod
{
    public class BuffMod : IModApi
    {
        private static readonly int requiredKills = 100;
        private static readonly int unlockNewAffixChance = 67; // actual chance is 100 - unlockNewAffixChance %
        private static readonly System.Reflection.MethodInfo MI_AddActionListEntry = AccessTools.Method(typeof(XUiC_ItemActionList), "AddActionListEntry");

        public void InitMod(Mod __mod)
        {
            Log.Out("[WeaponBuffMod] Initializing...");
            
            var harmony = new Harmony("com.example.weaponbuff");

            // Patch ActionKill.PerformTargetAction(Entity)
            harmony.Patch(
                original: AccessTools.Method(typeof(EntityAlive), nameof(EntityAlive.OnEntityDeath)),
                postfix: new HarmonyMethod(typeof(BuffMod), nameof(OnEntityDeath_Postfix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(XUiC_ItemCosmeticStack), nameof(XUiC_ItemCosmeticStack.CanRemove)),
                prefix: new HarmonyMethod(typeof(BuffMod), nameof(CanRemove_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(LootContainer), nameof(LootContainer.SpawnItem)),
                postfix: new HarmonyMethod(typeof(BuffMod), nameof(LootContainer_SpawnItem_Postfix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(XUiC_CollectedItemList), nameof(XUiC_CollectedItemList.SetYOffset)),
                prefix: new HarmonyMethod(typeof(BuffMod), nameof(CollectedItemList_SetYOffset_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(XUiC_ItemActionList), nameof(XUiC_ItemActionList.SetCraftingActionList)),
                postfix: new HarmonyMethod(typeof(BuffMod), nameof(SetCraftingActionList_Postfix))
            );
        }

        private static void SetCraftingActionList_Postfix(XUiC_ItemActionList __instance, XUiC_ItemActionList.ItemActionListTypes _actionListType, XUiController itemController)
        {
            try
            {
                if (_actionListType != XUiC_ItemActionList.ItemActionListTypes.Part) return;
                if (!(itemController is XUiC_ItemCosmeticStack)) return;

                var xui = __instance.xui;
                var parent = xui?.AssembleItem?.CurrentItem;
                if (parent == null || parent.IsEmpty() || parent.itemValue == null || parent.itemValue.IsEmpty()) return;

                // Get the selected cosmetic mod stack (the slot you clicked)
                var cosmeticController = (XUiC_ItemCosmeticStack)itemController;
                var selectedClass = cosmeticController.ItemStack?.itemValue?.ItemClass;

                if (selectedClass == null || !selectedClass.HasAnyTags(AffixUtils.AffixTag)) return;

                MI_AddActionListEntry?.Invoke(__instance, new object[] { new ItemActionEntryRerollAffix(itemController) });
            }
            catch (Exception e)
            {
                Log.Out($"[REROLL DEBUG] Failed adding reroll action: {e}");
            }
        }

        // Applies affix mods when items are spawned/looted
        private static void LootContainer_SpawnItem_Postfix(LootContainer.LootEntry template, ItemValue lootItemValue, ref bool __result, List<ItemStack> spawnedItems)
        {
            if (spawnedItems == null) return;

            foreach (var stack in spawnedItems)
            {
                try
                {
                    if (stack?.itemValue?.ItemClass == null || 
                            stack?.itemValue.Modifications.Length == 0 || 
                            stack?.itemValue.Quality == 0 || 
                            stack.itemValue.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("noMods"))
                        ) continue;
                    if (stack.itemValue.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("weapon")) || 
                            stack.itemValue.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("tool")) || 
                            stack.itemValue.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("armor"))
                        )
                        try
                        {
                            ApplyAffixMods(stack.itemValue);
                        }
                        catch (Exception e)
                        {
                            Log.Out($"Failed to apply affix mod to '{stack.itemValue.ItemClass.localizedName}', '{e}'");
                        }
                }
                catch (Exception e)
                {
                    Log.Out($"Failed to properly check stack for item with expection: '{e}'");
                }
            }
        }

        private static void ApplyAffixMods(ItemValue itemValue)
        {
            if (itemValue == null)
            {
                Log.Out("ItemValue is null");
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
                int toAdd = CountModsToApply(itemValue);

            // Add random mods
            for (int i = 0; i < toAdd; i++)
            {
                // For each mod to add, first decide on which tier mod to add
                int selectedTier = AffixUtils.RandomizeTierWithOdds(itemValue);
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

        private static int CountModsToApply(ItemValue itemValue)
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
                var localPlayer = GameManager.Instance?.myEntityPlayerLocal;
                if (localPlayer?.Progression != null)
                {
                    magicFindLvl = localPlayer.Progression.GetProgressionValue("perkMagicFind").level;
                }
            }
            catch (Exception e) {
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

        private static bool CanRemove_Prefix(XUiC_ItemCosmeticStack __instance, ref bool __result)
        {
            try
            {
                if (AffixUtils.IsAffixMod(__instance.ItemClass)) return __result = false;
            }
            catch (Exception ex)
            {
                Log.Out($"[WeaponBuffMod] Error in CanRemove_Prefix: '{ex}'");
            }
            return true;
        }
        
        public static void OnEntityDeath_Postfix(EntityAlive __instance)
        {
            Log.Out($"'{__instance.LocalizedEntityName}'");
            if (!__instance.HasAnyTags(FastTags<TagGroup.Global>.GetTag("zombie"))) return;

            string affixName = "";
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
            int totalAffixes = 5 + (magicSlayerLvl > 2 ? 1 : 0) + (magicSlayerLvl > 4 ? 1 : 0);
            int maxUpgrade = 4 + (magicSlayerLvl > 1 ? 1 : 0) + (magicSlayerLvl > 3 ? 1 : 0);

            // Make sure it has the kills and upgrades metadata
            if (heldItem.TryGetMetadata("kills", out float kills) && 
                heldItem.TryGetMetadata("upgrades", out float upgrades) && 
                heldItem.TryGetMetadata("nextUpgrade", out float nextUpgrade) &&
                heldItem.TryGetMetadata("lastUpgrade", out float lastUpgrade))
            {
                heldItem.SetMetadata("kills", kills + 1);
                Log.Out($"current kills: '{kills}'");
                Log.Out($"current upgrades: '{upgrades}'");
                Log.Out($"current nextUpgrade: '{nextUpgrade}'");
                Log.Out($"current lastUpgrade: '{lastUpgrade}'");
                try
                {
                    if (nextUpgrade == 0)
                    {
                        heldItem.SetMetadata("nextUpgrade", ((requiredKills - 10 * magicSlayerLvl) * (upgrades + 1)));
                        return;
                    }
                    if (nextUpgrade - lastUpgrade > ((requiredKills - 10 * magicSlayerLvl) * (upgrades + 1)))
                    {
                        nextUpgrade = lastUpgrade + ((requiredKills - 10 * magicSlayerLvl) * (upgrades + 1));
                        heldItem.SetMetadata("nextUpgrade", nextUpgrade);
                    }

                    // Check if you should upgrade
                    if (kills + 1 >= nextUpgrade)
                    {
                        bool didUpgrade = false;
                        // If an empty slot is available, unlock a new one.
                        if (heldItem.CosmeticMods.Length < totalAffixes)
                        {
                            if (heldItem.CosmeticMods[0] == null || heldItem.CosmeticMods[0].IsEmpty()) didUpgrade = AddNewAffix(heldItem, ref affixName);
                            else if (!heldItem.CosmeticMods[0].ItemClass.HasAnyTags(AffixUtils.AffixTag)) didUpgrade = AddNewAffix(heldItem, ref affixName);
                            else if (AffixUtils.rng.Next(0, 100) > unlockNewAffixChance) didUpgrade = AddNewAffix(heldItem, ref affixName);
                        }
                        if (!didUpgrade)
                        { 
                            // Otherwise upgrade an existing affix.
                            // Check if all affixes are max rarity
                            List<int> canUpgradeSlots = new List<int>();
                            for (int i = 0; i < heldItem.CosmeticMods.Length; i++)
                            {
                                if (heldItem.CosmeticMods[i] == null) continue;
                                string rarityString = heldItem.CosmeticMods[i].ItemClass.Name.Substring(heldItem.CosmeticMods[i].ItemClass.Name.Length - 1);
                                if (int.TryParse(rarityString, out int rarity))
                                    if (rarity < maxUpgrade) canUpgradeSlots.Add(i);
                            }

                            // If no slots can be upgraded, see if you can add a new one instead.
                            if (canUpgradeSlots.Count == 0)
                            {
                                if (heldItem.CosmeticMods.Length < totalAffixes) didUpgrade = AddNewAffix(heldItem, ref affixName);
                                else return;
                            }
                            else didUpgrade = UpgradeAffix(heldItem, canUpgradeSlots, ref affixName);
                        }
                        if (didUpgrade)
                        {
                            if (affixName.Contains("Common"))
                                GameManager.ShowTooltip(player, string.Format(Localization.Get("ttaffixunlock", false), upgrades + 1, (requiredKills - 10 * magicSlayerLvl) * (upgrades + 2), affixName), string.Empty, "read_skillbook_final");
                            else
                                GameManager.ShowTooltip(player, string.Format(Localization.Get("ttaffixup", false), upgrades + 1, (requiredKills - 10 * magicSlayerLvl) * (upgrades + 2), affixName), string.Empty, "read_skillbook_final");
                            heldItem.SetMetadata("upgrades", upgrades + 1);
                            heldItem.SetMetadata("nextUpgrade", nextUpgrade + ((requiredKills - 10 * magicSlayerLvl) * (upgrades + 2)));
                            heldItem.SetMetadata("lastUpgrade", kills + 1);
                        }
                    }
                }
                catch (Exception e){
                    Log.Out($"'{e}'");
                }
            }
        }

        private static bool UpgradeAffix(ItemValue itemValue, List<int> canUpgradeSlots, ref string affixName)
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
            string newName = oldAffix.Name.Substring(0,oldAffix.Name.Length - 1) + (oldTier + 1).ToString();

            if (!(ItemClass.GetItemClass(newName) is ItemClassModifier newAffix)) return false;

            affixName = newAffix.localizedName;
            if (oldAffix.Name.Contains("KillStreak")) GameManager.Instance.myEntityPlayerLocal.Buffs.RemoveBuff("buff" + oldAffix.Name);

            // then apply the upgrade
            itemValue.CosmeticMods[randomIndex] = new ItemValue(newAffix.Id);
            return true;
        }

        private static bool AddNewAffix(ItemValue itemValue, ref string affixName)
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

        public static void CollectedItemList_SetYOffset_Prefix(ref int _yOffset)
        {
            var lp = GameManager.Instance?.myEntityPlayerLocal;
            if (lp == null) return;

            ItemValue held = lp.inventory?.holdingItemItemValue;
            if (held == null || held.IsEmpty()) return;

            bool hasKills = held.TryGetMetadata("kills", out float _);
            bool hasUpg = held.TryGetMetadata("upgrades", out float _);

            // Your panel height is 46, so shift popup up by 46 when it is visible
            if (hasKills || hasUpg) _yOffset += 46;
        }
    }
}
