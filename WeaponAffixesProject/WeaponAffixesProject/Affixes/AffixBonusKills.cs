using System.Collections.Generic;
using System.Linq;

namespace WeaponAffixesProject
{
    public static class AffixBonusKills
    {
        public static int CheckBonusKills(EntityAlive __instance, EntityPlayerLocal player, ItemValue item)
        {
            List<int> instances = AffixUtils.GetAllAffixesFromItem(item, "bonuskill");
            if (instances.Count == 0)
                return 1;

            int total = instances.Sum();
            int max = instances.Max();
            if (__instance.HasAnyTags(FastTags<TagGroup.Global>.GetTag("charged")) || __instance.HasAnyTags(FastTags<TagGroup.Global>.GetTag("infernal")))
            {
                if (AffixUtils.rng.Next(100) < total * 15)
                    return 2;
            }
            else if (__instance.HasAnyTags(FastTags<TagGroup.Global>.GetTag("radiated")))
            {
                if (max == 7 && total * 10 < 100)
                    total = 11;
                if (AffixUtils.rng.Next(100) < total * 10)
                    return 2;

            }
            else if (__instance.HasAnyTags(FastTags<TagGroup.Global>.GetTag("feral")))
            {
                if (max == 7 && total * 5 < 50)
                    total = 10;
                if (AffixUtils.rng.Next(100) < total * 5)
                    return 2;
            }
            return 1;
        }
    }
}
