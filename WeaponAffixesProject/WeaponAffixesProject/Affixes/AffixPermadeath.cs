using System.Collections.Generic;

namespace WeaponAffixesProject
{
    public static class AffixPermadeath
    {
        public static float PermadeathCheck(EntityEnemy __instance, DamageSource _damageSource)
        {
            EntityPlayer player = __instance.world.GetEntity(_damageSource.getEntityId()) as EntityPlayer;
            if (player == null || player.Died <= 0)
                return 0f;

            List<int> perma = AffixUtils.GetAllAffixesFromItem(_damageSource.AttackingItem, "permadeath");
            if (perma.Count > 0)
                return -0.5f;

            return 0f;
        }
    }
}
