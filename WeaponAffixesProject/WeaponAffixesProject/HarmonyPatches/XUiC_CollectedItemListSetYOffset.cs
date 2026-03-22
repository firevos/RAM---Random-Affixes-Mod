using HarmonyLib;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(XUiC_CollectedItemList), nameof(XUiC_CollectedItemList.SetYOffset))]
    public static class XUiC_CollectedItemListSetYOffset
    {
        public static void Prefix(ref int _yOffset)
        {
            WeaponUpgrades.SetYOffset(ref _yOffset);
        }
    }
}
