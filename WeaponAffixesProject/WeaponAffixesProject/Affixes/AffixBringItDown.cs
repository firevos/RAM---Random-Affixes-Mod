using System.Collections.Generic;
using System.Linq;

namespace WeaponAffixesProject
{
    public static class AffixBringItDown
    {
        public static float BringItDownCheck(EntityEnemy __instance, DamageSource _damageSource)
        {
            if (_damageSource.AttackingItem == null || __instance.Health <= 1500)
                return 0f;

            EntityPlayer player = __instance.world.GetEntity(_damageSource.getEntityId()) as EntityPlayer;

            if (player == null)
                return 0f;

            List<int> bringDowns = AffixUtils.GetAllAffixesFromItem(_damageSource.AttackingItem, "bringdown");
            bringDowns.AddRange(AffixUtils.GetAllAffixesFromArmor(player, "bringdown"));
            return bringDowns.Sum() * 0.03f;
        }
    }
}
