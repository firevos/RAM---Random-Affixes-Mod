using System.Collections.Generic;
using System.Linq;

namespace WeaponAffixesProject
{
    public static class AffixBonusKills
    {
        public static int CheckBonusKills(EntityAlive __instance, EntityPlayerLocal player, ItemValue item)
        {
            List<int> instances = AffixUtils.GetAllAffixesFromItem(item, "bonuskill");
            int total = instances.Sum();
            if (__instance.HasAnyTags(FastTags<TagGroup.Global>.GetTag("charged")) || __instance.HasAnyTags(FastTags<TagGroup.Global>.GetTag("infernal")))
            {
                if (AffixUtils.rng.Next(100) < total * 15)
                    return 2;
            }
            else if (__instance.HasAnyTags(FastTags<TagGroup.Global>.GetTag("radiated")))
            {
                if (AffixUtils.rng.Next(100) < total * 10)
                    return 2;

            }
            else if (__instance.HasAnyTags(FastTags<TagGroup.Global>.GetTag("feral")))
            {
                if (AffixUtils.rng.Next(100) < total * 5)
                    return 2;
            }
            return 1;
        }
    }
}
