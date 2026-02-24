using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeaponAffixesProject
{
    internal static class AffixUtils
    {
        internal static readonly System.Random rng = new System.Random();
        internal static readonly FastTags<TagGroup.Global> AffixTag = FastTags<TagGroup.Global>.GetTag("affix_mod");
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

        internal static bool IsAffixMod(ItemClass itemClass)
        {
            return itemClass != null && itemClass.HasAnyTags(AffixUtils.AffixTag);
        }

        internal static int RandomizeTierWithOdds(ItemValue itemValue, int magicFindLvl = 0)
        {
            //foreach (var group in itemValue.ItemClass.Effects.EffectGroups)
            //{
            //    if (!group.OwnerTiered)
            //        continue;

            //    foreach (var passive in group.PassiveEffects)
            //    {
            //        if (passive.Values != null && passive.Values.Length > 0)
            //        {
            //            float[] tierValues = passive.Values;

            //            Log.Out($"Tier count: {tierValues.Length}");
            //        }
            //    }
            //}
            int tier = itemValue?.Quality ?? 1;
            if (tier < 1 || tier >= baseWeightsByQuality.Length || baseWeightsByQuality[tier] == null)
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
            for (int i = 0; i < modList.Count; i++)
                for (int j = modList[i].Count - 1; j >= 0; j--)
                    if (modList[i][j].Name.Contains(mod.Name.Substring(0, mod.Name.Length - 1)))
                        modList[i].Remove(modList[i][j]);
            return modList;
        }


        internal static List<List<ItemClassModifier>> GetCorrectModList(ItemValue itemValue)
        {
            List<List<ItemClassModifier>> correctMods = new List<List<ItemClassModifier>>();
            for (int i = 0; i < 6; i++)
                correctMods.Add(new List<ItemClassModifier>());

            List<ItemClass> modList = ItemClass.GetItemsWithTag(AffixTag);

            // Select which list of mods to use based on tags
            foreach (ItemClassModifier itemClassModifier in modList.Cast<ItemClassModifier>())
                // Check if any tags match between mod and weapon and does not have any blocked tags, if yes, that mod can be added, if no, discard it.
                if (itemClassModifier.InstallableTags.Test_AnySet(itemValue.ItemClass.ItemTags) && !itemClassModifier.DisallowedTags.Test_AnySet(itemValue.ItemClass.ItemTags))
                    correctMods[itemClassModifier.Name[itemClassModifier.Name.Length - 1] - '1'].Add(itemClassModifier);

            return correctMods;
        }
    }
}
