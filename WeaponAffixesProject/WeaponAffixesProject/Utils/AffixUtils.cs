using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using WeaponBuffMod.HarmonyPatches;

namespace WeaponAffixesProject
{
    internal static class AffixUtils
    {
        internal static readonly System.Random rng = new System.Random();
        internal static readonly FastTags<TagGroup.Global> AffixTag = FastTags<TagGroup.Global>.GetTag("affix_mod");
        internal static readonly FastTags<TagGroup.Global> UniqueAffixTag = FastTags<TagGroup.Global>.GetTag("unique_affix_mod");
        internal static readonly FastTags<TagGroup.Global> WeaponTag = FastTags<TagGroup.Global>.GetTag("weapon");
        internal static readonly FastTags<TagGroup.Global> ArmorTag = FastTags<TagGroup.Global>.GetTag("armor");
        internal static readonly FastTags<TagGroup.Global> ToolTag = FastTags<TagGroup.Global>.GetTag("tool");
        internal static readonly int[][] baseWeightsByQuality =
                {
                null,                          // 0 unused
                new[] {100},                   // Q1: 100% common
                new[] {67, 33},                // Q2: 67% common 33% uncommon
                new[] {50, 30, 20},            // Q3: 50% common 30% uncommon 20% rare
                new[] {33, 28, 22, 17},        // Q4: 33% common 28% uncommon 22% rare 17% epic
                new[] {30, 25, 20, 15, 10},    // Q5: 30% common 25% uncommon 20% rare 15% epic 10% legendary
                new[] {25, 23, 19, 15, 11, 7}, // Q6: 25% common 23% uncommon 19% rare 15% epic 11% legendary 7% mythic
            };
        private static readonly int[][] veryLowWeightsByQuality =
            {
                null,
                new[] {100},
                new[] {85, 15},
                new[] {75, 20, 5},
                new[] {65, 23, 9, 3},
                new[] {58, 24, 12, 5, 1},
                new[] {52, 25, 14, 6, 2, 1},
            };
        private static readonly int[][] lowWeightsByQuality =
            {
                null,
                new[] {100},
                new[] {75, 25},
                new[] {60, 28, 12},
                new[] {45, 30, 17, 8},
                new[] {40, 28, 18, 10, 4},
                new[] {36, 28, 18, 11, 5, 2},
            };
        private static readonly int[][] highWeightsByQuality =
            {
                null,
                new[] {100},
                new[] {55, 45},
                new[] {38, 32, 30},
                new[] {24, 26, 25, 25},
                new[] {20, 22, 23, 20, 15},
                new[] {17, 18, 20, 20, 15, 10},
            };
        private static readonly int[][] veryHighWeightsByQuality =
            {
                null,
                new[] {100},
                new[] {42, 58},
                new[] {26, 31, 43},
                new[] {15, 20, 28, 37},
                new[] {12, 16, 22, 25, 25},
                new[] {10, 12, 16, 20, 22, 20},
            };
        internal static readonly int requiredKills = 100;
        internal static int RequiredKills => CustomSandboxSettings.GetInt(CustomSandboxSettings.KillsToUpgrade, requiredKills);
        internal static int MaxAffixes => CustomSandboxSettings.GetInt(CustomSandboxSettings.MaxAffixes, 5);
        internal static int AffixAbundance => CustomSandboxSettings.GetInt(CustomSandboxSettings.AffixAbundance, 100);
        internal static readonly int unlockNewAffixChance = 67; // actual chance is 100 - unlockNewAffixChance %

        internal static int GetConfiguredMaxAffixes()
        {
            int configured = MaxAffixes;
            if (configured < 1)
                return 1;
            if (configured > 10)
                return 10;
            return configured;
        }

        internal static int GetEffectiveMaxAffixes(EntityPlayer player)
        {
            int maxAffixes = GetConfiguredMaxAffixes();

            if (!ChallengeGroupIsCompleted(player, "ram advanced"))
                maxAffixes--;

            if (GetProgressionLevel(player, "perkMagicSlayer") < 5)
                maxAffixes--;

            return maxAffixes < 1 ? 1 : maxAffixes;
        }

        internal static int GetProgressionLevel(EntityPlayer player, string progressionName)
        {
            try
            {
                if (player?.Progression != null)
                    return player.Progression.GetProgressionValue(progressionName).level;
            }
            catch (System.Exception e)
            {
                Log.Out($"Can't find progression value '{progressionName}': '{e}'");
            }

            return 0;
        }

        internal static int GetAdjustedKillsToUpgrade(int magicSlayerLevel)
        {
            int baseKills = RequiredKills;
            if (baseKills < 1)
                baseKills = 1;

            int reductionPercent = magicSlayerLevel * 5;
            if (reductionPercent < 0)
                reductionPercent = 0;
            if (reductionPercent > 25)
                reductionPercent = 25;

            int adjustedKills = (baseKills * (100 - reductionPercent) + 99) / 100;
            return adjustedKills < 1 ? 1 : adjustedKills;
        }

        internal static bool IsAffixMod(ItemClass itemClass)
        {
            return itemClass != null &&
                   (IsAffixName(itemClass.Name) ||
                    itemClass.HasAnyTags(AffixTag) ||
                    itemClass.HasAnyTags(UniqueAffixTag) ||
                    IsAffixModifier(itemClass));
        }

        internal static int RandomizeTierWithOdds(ItemValue itemValue, EntityPlayer player)
        {
            int tier = itemValue?.Quality ?? 1;
            if (tier < 1)
                tier = 1;
            if (tier > 6)
                tier = 6;
            if (baseWeightsByQuality[tier] == null)
                tier = 1;

            int selectedTier = RollAffixTier(tier);

            // Increase rarity randomly based on lucky looter level
            int magicFindLvl = 0;
            if (player?.Progression == null)
                Log.Out("Player progression not found");
            else
                magicFindLvl = player.Progression.GetProgressionValue("perkMagicFind").level;


            bool upgraded = true;
            int maxTier = magicFindLvl < 3 ? tier : 5;
            while (selectedTier < maxTier && upgraded)
            {
                int random = rng.Next(0, 100);
                // 5% Chance to increase rarity per lucky looter level
                if (random < magicFindLvl * 5)
                {
                    Log.Out("Magic Find increased affix rarity!");
                    selectedTier++;
                }
                else
                    upgraded = false;
            }

            return selectedTier;
        }

        private static int RollAffixTier(int itemQuality)
        {
            int[] weights = GetRarityWeightsByQuality()[itemQuality];
            int totalWeight = 0;

            for (int i = 0; i < weights.Length; i++)
                totalWeight += weights[i];

            int roll = rng.Next(0, totalWeight);
            int cumulative = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                    return i;
            }

            return weights.Length - 1;
        }

        private static int[][] GetRarityWeightsByQuality()
        {
            switch (CustomSandboxSettings.GetInt(CustomSandboxSettings.AffixRarity, 100))
            {
                case 25:
                    return veryLowWeightsByQuality;
                case 50:
                    return lowWeightsByQuality;
                case 150:
                    return highWeightsByQuality;
                case 200:
                    return veryHighWeightsByQuality;
                default:
                    return baseWeightsByQuality;
            }
        }

        internal static List<List<ItemClassModifier>> RemoveSimilarMods(List<List<ItemClassModifier>> modList, ItemClassModifier mod)
        {
            if (mod == null || string.IsNullOrEmpty(mod.Name))
                return modList;

            string baseName = GetAffixBaseName(mod.Name);

            for (int i = 0; i < modList.Count; i++)
                for (int j = modList[i].Count - 1; j >= 0; j--)
                    if (modList[i][j].Name.StartsWith(baseName))
                        modList[i].Remove(modList[i][j]);
            return modList;
        }

        internal static List<List<ItemClassModifier>> GetCorrectModList(ItemValue itemValue)
        {
            List<List<ItemClassModifier>> correctMods = new List<List<ItemClassModifier>>();
            for (int i = 0; i < 6; i++)
                correctMods.Add(new List<ItemClassModifier>());

            IEnumerable<ItemClassModifier> modList = GetAffixModifiers();

            // Select which list of mods to use based on tags
            foreach (ItemClassModifier itemClassModifier in modList)
            {
                if (!TryGetAffixTierIndex(itemClassModifier.Name, out int tierIndex))
                    continue;

                // Check if any tags match between mod and weapon and does not have any blocked tags, if yes, that mod can be added, if no, discard it.
                if (itemClassModifier.InstallableTags.Test_AnySet(itemValue.ItemClass.ItemTags) && !itemClassModifier.DisallowedTags.Test_AnySet(itemValue.ItemClass.ItemTags))
                    correctMods[tierIndex].Add(itemClassModifier);
            }
            return correctMods;
        }

        internal static IEnumerable<ItemClassModifier> GetAffixModifiers()
        {
            if (ItemClass.list == null)
                return Enumerable.Empty<ItemClassModifier>();

            return ItemClass.list
                .OfType<ItemClassModifier>()
                .Where(itemClassModifier => itemClassModifier.Name.StartsWith("affixMod"));
        }

        internal static bool IsAffixModifier(ItemClass itemClass)
        {
            if (!(itemClass is ItemClassModifier itemClassModifier))
                return false;

            return IsAffixName(itemClassModifier.Name) ||
                   itemClassModifier.ModifierTags.Test_AnySet(AffixTag) ||
                   itemClassModifier.ItemTags.Test_AnySet(AffixTag) ||
                   itemClassModifier.ModifierTags.Test_AnySet(UniqueAffixTag) ||
                   itemClassModifier.ItemTags.Test_AnySet(UniqueAffixTag);
        }

        internal static bool IsAffixName(string itemClassName)
        {
            return !string.IsNullOrEmpty(itemClassName) &&
                   (itemClassName.StartsWith("affixMod", StringComparison.Ordinal) ||
                    itemClassName.StartsWith("uniqueAffixMod", StringComparison.Ordinal));
        }

        internal static bool IsAffixModWithKey(ItemClass itemClass, string affixKey)
        {
            if (!IsAffixMod(itemClass))
                return false;

            if (!string.IsNullOrEmpty(affixKey) && itemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag(affixKey)))
                return true;

            string normalizedName = NormalizeAffixLookupText(itemClass.Name);
            string[] aliases = GetAffixNameAliases(affixKey);
            for (int i = 0; i < aliases.Length; i++)
                if (normalizedName.Contains(aliases[i]))
                    return true;

            return false;
        }

        private static string[] GetAffixNameAliases(string affixKey)
        {
            switch (NormalizeAffixLookupText(affixKey))
            {
                case "bulletcasing":
                    return new[] { "bulletcasing" };
                case "giantslayer":
                    return new[] { "giantslayer" };
                case "buffduration":
                    return new[] { "buffduration" };
                case "bonuskill":
                    return new[] { "bonuskill", "bonuskills" };
                case "bringdown":
                    return new[] { "bringdown", "bringitdown" };
                case "permadeath":
                    return new[] { "permadeath" };
                default:
                    string normalizedKey = NormalizeAffixLookupText(affixKey);
                    return string.IsNullOrEmpty(normalizedKey) ? new string[0] : new[] { normalizedKey };
            }
        }

        private static string NormalizeAffixLookupText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            char[] result = new char[text.Length];
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsLetterOrDigit(c))
                    result[count++] = char.ToLowerInvariant(c);
            }

            return new string(result, 0, count);
        }

        internal static bool HasAnyMods(List<List<ItemClassModifier>> modList)
        {
            return modList != null && modList.Any(tier => tier != null && tier.Count > 0);
        }

        internal static bool TrySelectAffixMod(List<List<ItemClassModifier>> modList, int preferredTier, out ItemClassModifier selectedMod)
        {
            selectedMod = null;
            if (!HasAnyMods(modList))
                return false;

            preferredTier = ClampTierIndex(preferredTier, modList.Count);

            for (int distance = 0; distance < modList.Count; distance++)
            {
                int lower = preferredTier - distance;
                if (TrySelectAffixFromTier(modList, lower, out selectedMod))
                    return true;

                int upper = preferredTier + distance;
                if (upper != lower && TrySelectAffixFromTier(modList, upper, out selectedMod))
                    return true;
            }

            return false;
        }

        internal static bool TryGetAffixTierIndex(string affixName, out int tierIndex)
        {
            tierIndex = -1;
            if (string.IsNullOrEmpty(affixName))
                return false;

            char tierChar = affixName[affixName.Length - 1];
            if (tierChar < '1' || tierChar > '6')
                return false;

            tierIndex = tierChar - '1';
            return true;
        }

        private static bool TrySelectAffixFromTier(List<List<ItemClassModifier>> modList, int tier, out ItemClassModifier selectedMod)
        {
            selectedMod = null;
            if (tier < 0 || tier >= modList.Count || modList[tier] == null || modList[tier].Count == 0)
                return false;

            selectedMod = modList[tier][rng.Next(modList[tier].Count)];
            return selectedMod != null;
        }

        private static int ClampTierIndex(int tierIndex, int tierCount)
        {
            if (tierIndex < 0)
                return 0;
            if (tierIndex >= tierCount)
                return tierCount - 1;
            return tierIndex;
        }

        private static string GetAffixBaseName(string affixName)
        {
            return TryGetAffixTierIndex(affixName, out _) ? affixName.Substring(0, affixName.Length - 1) : affixName;
        }

        internal static bool IsCalledFromExtractAffix()
        {
            var trace = new StackTrace(false);
            var frames = trace.GetFrames();
            if (frames == null) return false;

            for (int i = 0; i < frames.Length; i++)
            {
                var method = frames[i].GetMethod();
                var type = method?.DeclaringType;
                if (type?.Name == "ItemActionEntryExtractAffix" && method.Name == "OnActivated") return true;
            }
            return false;
        }

        internal static bool ChallengeGroupIsCompleted(EntityPlayer player, string groupName)
        {
            if (player?.challengeJournal?.CompleteChallengeGroupsForMinEvents == null)
                return false;

            foreach (var group in player.challengeJournal.CompleteChallengeGroupsForMinEvents)
            {
                if (group.Name == groupName)
                {
                    return true;
                }
            }
            return false;
        }

        internal static void ApplyQuestEventManagerUseItem(string name)
        {
            ItemClass requiredClass = ItemClass.GetItemClass(name, false);
            if (requiredClass != null)
            {
                ItemValue requiredValue = new ItemValue(requiredClass.Id, false);
                var quester = QuestEventManager.Current;
                if (quester != null)
                    quester.UsedItem(requiredValue);
                else
                {
                    Log.Out("Questmanager is null");
                }
            }
        }

        internal static List<int> GetAllAffixesFromItem(ItemValue item, string tagName)
        {
            List<int> instances = new List<int>();
            if (item == null)
                return instances;

            if (item.CosmeticMods != null)
            {
                foreach (var mod in item.CosmeticMods)
                {
                    if (mod == null || mod.IsEmpty())
                        continue;
                    if (IsAffixModWithKey(mod.ItemClass, tagName) && TryGetAffixTierIndex(mod.ItemClass.Name, out int tierIndex))
                        instances.Add(tierIndex + 1);
                }
            }
            if (item.Modifications != null)
            {
                foreach (var mod in item.Modifications)
                {
                    if (mod == null || mod.IsEmpty())
                        continue;
                    if (IsAffixModWithKey(mod.ItemClass, tagName) && TryGetAffixTierIndex(mod.ItemClass.Name, out int tierIndex))
                        instances.Add(tierIndex + 1);
                }
            }

            return instances;
        }

        internal static List<int> GetAllAffixesFromArmor(EntityPlayer player, string tagName)
        {
            List<int> instances = new List<int>();

            ItemValue[] allGear = player.equipment.GetItems();

            foreach (ItemValue gear in allGear)
            {
                instances.AddRange(GetAllAffixesFromItem(gear, tagName));
            }

            return instances;
        }

    }
}
