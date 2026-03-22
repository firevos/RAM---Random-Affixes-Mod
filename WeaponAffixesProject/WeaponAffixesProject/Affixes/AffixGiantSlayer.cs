using HarmonyLib;
using System;
using System.Collections.Generic;

namespace WeaponAffixesProject
{
    public static class AffixGiantSlayer
    {
        [HarmonyPatch]
        public static class EntityAliveBaseDamageCaller
        {
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(EntityAlive), nameof(EntityAlive.DamageEntity))]
            public static int CallBase(EntityAlive __instance, DamageSource ds, int strength, bool critical, float impulseScale = 1f)
            {
                throw new NotImplementedException();
            }
        }

        public static void GiantSlayerCheck(EntityEnemy __instance, DamageSource _damageSource)
        {
            if (_damageSource.AttackingItem == null || __instance.Health <= 0)
                return;

            List<int> giantslayers = AffixUtils.GetAllAffixesFromItem(_damageSource.AttackingItem, "giantslayer");

            int successCount = 0;
            foreach (int value in giantslayers)
            {
                int chance = (value == 6) ? 25 : value * 4;
                if (AffixUtils.rng.Next(0, 100) < chance)
                    successCount++;
            }

            float damage = (float)__instance.GetMaxHealth() / 100 * 6;
            if (__instance.HasAnyTags(FastTags<TagGroup.Global>.GetTag("boss")))
                damage /= 2;

            for (int i = 0; i < successCount; i++)
                EntityAliveBaseDamageCaller.CallBase(__instance, _damageSource, (int)damage, false);
        }
    }
}
