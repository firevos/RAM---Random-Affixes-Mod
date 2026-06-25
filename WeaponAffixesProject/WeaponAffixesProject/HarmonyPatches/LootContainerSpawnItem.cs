using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(LootContainer), nameof(LootContainer.SpawnItem))]
    public static class LootContainerSpawnItem
    {
        private static void Postfix(LootContainer __instance, List<ItemStack> spawnedItems, EntityPlayer player)
        {
            LootContainerTokenAbundance.AddGlobalTokenRoll(__instance, spawnedItems);
            LootContainerTokenAbundance.ScaleTokenStacks(spawnedItems);
            AffixSystem.ApplyAffixToLootCheck(spawnedItems, player);
        }
    }

    internal static class LootContainerTokenAbundance
    {
        private const int GlobalTokenDropChance = 5000;
        private const string TokenScaledMetadataKey = "ramTokenAbundanceScaled";
        private static readonly ConditionalWeakTable<LootContainer, RollState> RolledContainers =
            new ConditionalWeakTable<LootContainer, RollState>();
        private static readonly ConditionalWeakTable<List<ItemStack>, RollState> RolledLists =
            new ConditionalWeakTable<List<ItemStack>, RollState>();

        private static readonly string[] WeightedGlobalTokenNames =
        {
            "affixRerollToken",
            "affixRerollToken",
            "affixRerollToken",
            "affixRerollToken",
            "affixRerollToken",
            "affixExtractionToken",
            "affixExtractionToken",
            "affixExtractionToken",
            "affixUpgradeToken",
            "affixUnlockToken"
        };

        private static readonly HashSet<string> TokenNames = new HashSet<string>
        {
            "affixBlankToken",
            "affixRerollToken",
            "affixExtractionToken",
            "affixUpgradeToken",
            "affixUnlockToken"
        };

        internal static void ScaleTokenStacks(List<ItemStack> spawnedItems)
        {
            if (spawnedItems == null || spawnedItems.Count == 0)
            {
                return;
            }

            int abundance = Math.Max(0, CustomSandboxSettings.GetInt(CustomSandboxSettings.TokenLootAbundance, 100));

            for (int i = spawnedItems.Count - 1; i >= 0; i--)
            {
                ItemStack stack = spawnedItems[i];
                if (!IsRamToken(stack))
                {
                    continue;
                }

                if (stack.itemValue.TryGetMetadata(TokenScaledMetadataKey, out float scaled) && scaled > 0)
                {
                    continue;
                }

                int scaledCount = ScaleCount(stack.count, abundance);
                if (scaledCount <= 0)
                {
                    spawnedItems.RemoveAt(i);
                    continue;
                }

                stack.count = scaledCount;
                stack.itemValue.SetMetadata(TokenScaledMetadataKey, 1f);
                spawnedItems[i] = stack;
            }
        }

        internal static void AddGlobalTokenRoll(LootContainer container, List<ItemStack> spawnedItems)
        {
            if (spawnedItems == null)
            {
                return;
            }

            RollState rollState = container != null
                ? RolledContainers.GetOrCreateValue(container)
                : RolledLists.GetOrCreateValue(spawnedItems);
            if (rollState.GlobalTokenRolled)
            {
                return;
            }
            rollState.GlobalTokenRolled = true;

            if (AffixUtils.rng.Next(GlobalTokenDropChance) != 0)
            {
                return;
            }

            string tokenName = WeightedGlobalTokenNames[AffixUtils.rng.Next(WeightedGlobalTokenNames.Length)];
            ItemClass tokenClass = ItemClass.GetItemClass(tokenName, false);
            if (tokenClass == null)
            {
                return;
            }

            ItemValue tokenValue = new ItemValue(tokenClass.Id, false);
            spawnedItems.Add(new ItemStack(tokenValue, 1));
        }

        private static bool IsRamToken(ItemStack stack)
        {
            return stack != null
                && stack.itemValue != null
                && stack.itemValue.ItemClass != null
                && TokenNames.Contains(stack.itemValue.ItemClass.Name);
        }

        private static int ScaleCount(int count, int abundance)
        {
            if (count <= 0 || abundance <= 0)
            {
                return 0;
            }

            int guaranteedRolls = abundance / 100;
            int extraRollChance = abundance % 100;
            int scaledCount = count * guaranteedRolls;

            for (int i = 0; i < count; i++)
            {
                if (extraRollChance > 0 && AffixUtils.rng.Next(100) < extraRollChance)
                {
                    scaledCount++;
                }
            }

            return scaledCount;
        }

        private sealed class RollState
        {
            internal bool GlobalTokenRolled;
        }
    }
}
