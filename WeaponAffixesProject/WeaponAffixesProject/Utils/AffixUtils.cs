using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
        internal static readonly int requiredKills = 100;
        internal static readonly int magicSlayerBonus = 5;
        internal static readonly int unlockNewAffixChance = 67; // actual chance is 100 - unlockNewAffixChance %

        internal static bool IsAffixMod(ItemClass itemClass)
        {
            return itemClass != null && (itemClass.HasAnyTags(AffixTag) || itemClass.HasAnyTags(UniqueAffixTag) || IsAffixModifier(itemClass));
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

            int roll = rng.Next(0, 100);
            int cumulative = 0;
            int selectedTier = 0;
            // Initial rarity roll based on base weights and quality
            for (int i = 0; i < baseWeightsByQuality[tier].Length; i++)
            {
                cumulative += baseWeightsByQuality[tier][i];
                if (roll < cumulative)
                {
                    selectedTier = i;
                    break;
                }
            }

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

            return itemClassModifier.ModifierTags.Test_AnySet(AffixTag) ||
                   itemClassModifier.ItemTags.Test_AnySet(AffixTag) ||
                   itemClassModifier.Name.StartsWith("affixMod");
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
                    if (mod.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag(tagName)))
                        instances.Add(mod.ItemClass.Name[mod.ItemClass.Name.Length - 1] - '0');
                }
            }
            if (item.Modifications != null)
            {
                foreach (var mod in item.Modifications)
                {
                    if (mod == null || mod.IsEmpty())
                        continue;
                    if (mod.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag(tagName)))
                        instances.Add(mod.ItemClass.Name[mod.ItemClass.Name.Length - 1] - '0');
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
