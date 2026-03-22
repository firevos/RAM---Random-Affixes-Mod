using System.Collections.Generic;

namespace WeaponAffixesProject
{
    public static class AffixIncreaseBuffDuration
    {
        public static void IncreaseBuffDurationAffix(EntityPlayerLocal player, ItemValue heldItem)
        {
            List<int> instances = AffixUtils.GetAllAffixesFromItem(heldItem, "buffduration");

            List<string> positiveBuffs = BuffUtils.GetPositiveBuffs(player);

            if (positiveBuffs.Count == 0)
                return;

            foreach (int entry in instances)
            {
                string selected = "$" + positiveBuffs[AffixUtils.rng.Next(positiveBuffs.Count)] + "duration";
                player.Buffs.SetCustomVar(selected, player.Buffs.GetCustomVar(selected) + entry + 2);
            }
        }
    }
}
