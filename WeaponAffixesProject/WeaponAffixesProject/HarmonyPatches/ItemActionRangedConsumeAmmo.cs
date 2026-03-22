using HarmonyLib;
using WeaponAffixesProject.Affixes;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(ItemActionRanged), nameof(ItemActionRanged.ConsumeAmmo))]
    public static class ItemActionRangedConsumeAmmo
    {
        private static void Prefix(ItemActionData _actionData)
        {
            AffixBulletRecovery.BulletRecoveryCheck(_actionData);
        }
    }
}
