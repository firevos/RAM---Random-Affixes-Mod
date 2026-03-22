using HarmonyLib;
using System.Collections.Generic;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(LootContainer), nameof(LootContainer.SpawnItem))]
    public static class LootContainerSpawnItem
    {
        private static void Postfix(List<ItemStack> spawnedItems, EntityPlayer player)
        {
            AffixSystem.ApplyAffixToLootCheck(spawnedItems, player);
        }
    }
}
