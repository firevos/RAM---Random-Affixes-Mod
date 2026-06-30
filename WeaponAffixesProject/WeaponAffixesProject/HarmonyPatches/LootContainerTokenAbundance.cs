using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WeaponAffixesProject;

internal static class LootContainerTokenAbundance
{
	private sealed class RollState
	{
		internal bool GlobalTokenRolled;
	}

	private const int GlobalTokenDropChance = 5000;

	private const string TokenScaledMetadataKey = "ramTokenAbundanceScaled";

	private static readonly ConditionalWeakTable<LootContainer, RollState> RolledContainers = new ConditionalWeakTable<LootContainer, RollState>();

	private static readonly ConditionalWeakTable<List<ItemStack>, RollState> RolledLists = new ConditionalWeakTable<List<ItemStack>, RollState>();

	private static readonly string[] WeightedGlobalTokenNames = new string[13]
	{
		"affixRerollToken", "affixRerollToken", "affixRerollToken", "affixRerollToken", "affixRerollToken", "affixExtractionToken", "affixExtractionToken", "affixExtractionToken", "affixSwapToken", "affixSwapToken",
		"affixSwapToken", "affixUpgradeToken", "affixUnlockToken"
	};

	private static readonly HashSet<string> TokenNames = new HashSet<string> { "affixBlankToken", "affixRerollToken", "affixExtractionToken", "affixSwapToken", "affixUpgradeToken", "affixUnlockToken" };

	internal static void ScaleTokenStacks(List<ItemStack> spawnedItems)
	{
		if (spawnedItems == null || spawnedItems.Count == 0)
		{
			return;
		}
		int abundance = Math.Max(0, CustomSandboxSettings.GetInt("TokenLootAbundance", 100));
		for (int i = spawnedItems.Count - 1; i >= 0; i--)
		{
			ItemStack stack = spawnedItems[i];
			if (IsRamToken(stack) && (!stack.itemValue.TryGetMetadata("ramTokenAbundanceScaled", out float scaled) || !(scaled > 0f)))
			{
				int scaledCount = ScaleCount(stack.count, abundance);
				if (scaledCount <= 0)
				{
					spawnedItems.RemoveAt(i);
				}
				else
				{
					stack.count = scaledCount;
					stack.itemValue.SetMetadata("ramTokenAbundanceScaled", 1f);
					spawnedItems[i] = stack;
				}
			}
		}
	}

	internal static void AddGlobalTokenRoll(LootContainer container, List<ItemStack> spawnedItems)
	{
		if (spawnedItems == null)
		{
			return;
		}
		RollState rollState = ((container != null) ? RolledContainers.GetOrCreateValue(container) : RolledLists.GetOrCreateValue(spawnedItems));
		if (rollState.GlobalTokenRolled)
		{
			return;
		}
		rollState.GlobalTokenRolled = true;
		if (AffixUtils.rng.Next(5000) == 0)
		{
			string tokenName = WeightedGlobalTokenNames[AffixUtils.rng.Next(WeightedGlobalTokenNames.Length)];
			ItemClass tokenClass = ItemClass.GetItemClass(tokenName);
			if (tokenClass != null)
			{
				ItemValue tokenValue = new ItemValue(tokenClass.Id);
				spawnedItems.Add(new ItemStack(tokenValue, 1));
			}
		}
	}

	private static bool IsRamToken(ItemStack stack)
	{
		return stack != null && stack.itemValue != null && stack.itemValue.ItemClass != null && TokenNames.Contains(stack.itemValue.ItemClass.Name);
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
}
