using HarmonyLib;

namespace WeaponAffixesProject
{

    [HarmonyPatch(typeof(EntityPlayer), nameof(EntityPlayer.DamageEntity))]
    public static class EntityPlayerDamageEntity
    {
        public static void Postfix(EntityPlayer __instance, DamageSource _damageSource)
        {
            AffixResurgence.ResurgenceCheck(__instance, _damageSource);
        }
    }
}
