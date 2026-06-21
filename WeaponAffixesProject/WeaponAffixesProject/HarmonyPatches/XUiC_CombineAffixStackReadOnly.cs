using HarmonyLib;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(XUiC_ItemStack))]
    public static class XUiC_CombineAffixStackReadOnly
    {
        [HarmonyPatch(nameof(XUiC_ItemStack.HandleMoveToPreferredLocation))]
        [HarmonyPatch(nameof(XUiC_ItemStack.HandlePartialStackPickup))]
        [HarmonyPatch(nameof(XUiC_ItemStack.HandleStackSwap))]
        [HarmonyPrefix]
        public static bool Prefix(XUiC_ItemStack __instance)
        {
            return !(__instance is XUiC_CombineAffixStack);
        }
    }
}
