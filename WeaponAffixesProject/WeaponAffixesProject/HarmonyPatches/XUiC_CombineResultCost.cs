using HarmonyLib;

namespace WeaponAffixesProject.HarmonyPatches
{
    [HarmonyPatch(typeof(XUiC_RequiredItemStack), nameof(XUiC_RequiredItemStack.CanSwap))]
    public static class XUiC_CombineResultCost
    {
        public static bool Prefix(XUiC_RequiredItemStack __instance, ref bool __result)
        {
            XUiC_CombineGrid grid = __instance.GetParentByType<XUiC_CombineGrid>();
            if (grid == null || grid.result1 != __instance)
                return true;

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(XUiC_CombineGrid), nameof(XUiC_CombineGrid.Result1_SlotChangedEvent))]
    public static class XUiC_CombineResultSlotChangedCost
    {
        public static bool Prefix(XUiC_CombineGrid __instance)
        {
            __instance.result1.SlotChangedEvent -= __instance.Result1_SlotChangedEvent;
            __instance.result1.ItemStack = __instance.lastResult == null ? ItemStack.Empty : __instance.lastResult.Clone();
            __instance.result1.HiddenLock = true;
            __instance.result1.SlotChangedEvent += __instance.Result1_SlotChangedEvent;
            return false;
        }
    }
}
