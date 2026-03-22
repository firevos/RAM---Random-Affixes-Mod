using HarmonyLib;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(XUiC_ItemPartStack), nameof(XUiC_ItemPartStack.SwapItem))]
    public static class XUiC_ItemPartStackSwapItem
    {
        private static bool Prefix(XUiC_ItemPartStack __instance)
        {
            if (AffixUtils.IsAffixMod(__instance.itemClass))
                return false;

            return true;
        }

    }
}
