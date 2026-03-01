using HarmonyLib;
using System;
using System.Collections.Generic;
using static Prefab;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(LootContainer), nameof(LootContainer.SpawnItem))]
    public static class LootContainerPatch
    {
        private static void Postfix(List<ItemStack> spawnedItems, EntityPlayer player)
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
                    if (stack.itemValue.ItemClass.HasAnyTags(AffixUtils.WeaponTag) ||
                            stack.itemValue.ItemClass.HasAnyTags(AffixUtils.ToolTag) ||
                            stack.itemValue.ItemClass.HasAnyTags(AffixUtils.ArmorTag)
                        )
                        try
                        {
                            AffixSystem.ApplyAffixMods(stack.itemValue, player);
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
    }
}
