using HarmonyLib;
using UnityEngine;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(QualityInfo), nameof(QualityInfo.Cleanup))]
    public static class QualityInfoCleanupPrefix
    {
        private static bool Prefix()
        {
            QualityInfo.qualityColors = new Color[8];
            QualityInfo.hexColors = new string[8];
            return false;
        }
    }
}
