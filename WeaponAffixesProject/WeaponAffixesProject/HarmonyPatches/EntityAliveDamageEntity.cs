using HarmonyLib;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(EntityEnemy), nameof(EntityEnemy.DamageEntity))]
    public static class EntityEnemyDamageEntity
    {

        public static void Prefix(EntityEnemy __instance, DamageSource _damageSource, ref int _strength)
        {

            float bringItDown = AffixBringItDown.BringItDownCheck(__instance, _damageSource);

            float permadeath = AffixPermadeath.PermadeathCheck(__instance, _damageSource);

            float totalBonusMult = 1 + bringItDown + permadeath;

            _strength = (int)(_strength * totalBonusMult);
        }

        public static void Postfix(EntityEnemy __instance, DamageSource _damageSource)
        {
            AffixGiantSlayer.GiantSlayerCheck(__instance, _damageSource);


        }
    }
}
