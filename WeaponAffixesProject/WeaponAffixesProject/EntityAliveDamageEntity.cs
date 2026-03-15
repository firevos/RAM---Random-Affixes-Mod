using HarmonyLib;
using System;
using System.Collections.Generic;

// GIANTSLAYER

namespace WeaponAffixesProject
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

    [HarmonyPatch(typeof(EntityEnemy), nameof(EntityEnemy.DamageEntity))]
    public static class EntityEnemyDamageEntity
    {
        public static void Postfix(EntityEnemy __instance, DamageSource _damageSource)
        {
            // Log.Out($"{__instance.name} has: {__instance.Health} hp and {__instance.GetMaxHealth()} maxhp and is zombie: {__instance.HasAnyTags(FastTags<TagGroup.Global>.GetTag("zombie"))}");

            if (_damageSource.AttackingItem == null || __instance.Health <= 0)
                return;

            List<int> giantslayers = new List<int>();
            if (_damageSource.AttackingItem.CosmeticMods != null)
            {
                foreach (var mod in _damageSource.AttackingItem.CosmeticMods)
                {
                    if (mod == null || mod.IsEmpty())
                        continue;
                    if (mod.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("giantslayer")))
                        giantslayers.Add(mod.ItemClass.Name[mod.ItemClass.Name.Length - 1] - '0');
                }
            }
            if (_damageSource.AttackingItem.Modifications != null)
            {
                foreach (var mod in _damageSource.AttackingItem.Modifications)
                {
                    if (mod == null || mod.IsEmpty())
                        continue;
                    if (mod.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("giantslayer")))
                        giantslayers.Add(mod.ItemClass.Name[mod.ItemClass.Name.Length - 1] - '0');
                }
            }

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
