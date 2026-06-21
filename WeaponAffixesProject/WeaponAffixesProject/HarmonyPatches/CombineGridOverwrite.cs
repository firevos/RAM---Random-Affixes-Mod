using HarmonyLib;

namespace WeaponAffixesProject.HarmonyPatches
{
    internal class CombineGridOverwrite
    {

        [HarmonyPatch(typeof(XUiC_CombineGrid))]
        [HarmonyPatch(nameof(XUiC_CombineGrid.Merge_SlotChangedEvent))]
        public class TargetMethodPatch
        {
            static bool Prefix(XUiC_CombineGrid __instance, int slotNumber, ItemStack stack)
            {
                if (__instance.merge1.ItemStack.IsEmpty() || __instance.merge2.ItemStack.IsEmpty() || __instance.merge1.ItemStack.itemValue.type != __instance.merge2.ItemStack.itemValue.type)
                {
                    __instance.result1.SlotChangedEvent -= __instance.Result1_SlotChangedEvent;
                    __instance.result1.ItemStack = ItemStack.Empty;
                    __instance.result1.HiddenLock = true;
                    __instance.result1.SlotChangedEvent += __instance.Result1_SlotChangedEvent;
                    return false;
                }
                ItemValue itemValue = __instance.merge1.ItemStack.itemValue;
                ItemValue itemValue2 = __instance.merge2.ItemStack.itemValue;
                if (itemValue.HasMods() && itemValue2.HasMods())
                {
                    GameManager.ShowTooltip(__instance.xui.playerUI.entityPlayer, string.Format(Localization.Get("ttCombineRemoveMods", false, null), 0), string.Empty, "ui_denied", null, false, false, 0f);
                    return false;
                }
                bool flag = true;
                if (!itemValue.EqualsForMerging(itemValue2))
                {
                    ItemStack itemStack = __instance.merge1.ItemStack.Clone();
                    itemStack.itemValue.MergeBest(__instance.merge2.ItemStack.itemValue);
                    itemStack.itemValue.CosmeticMods = CombineAffixSelectionState.BuildResultAffixes(
                        __instance.GetParentByType<XUiC_CombineWindowGroup>(),
                        itemValue,
                        itemValue2);
                    if ((itemStack.itemValue.Quality > itemValue.Quality || !itemStack.itemValue.EqualsForMerging(itemValue)) && (itemStack.itemValue.Quality > itemValue2.Quality || !itemStack.itemValue.EqualsForMerging(itemValue2)))
                    {
                        itemStack.itemValue.Meta = 0;
                        __instance.lastResult = itemStack;
                        __instance.result1.ItemStack = itemStack;
                        __instance.result1.HiddenLock = false;
                        flag = false;
                    }
                }
                if (flag)
                {
                    GameManager.ShowTooltip(__instance.xui.playerUI.entityPlayer, string.Format(Localization.Get("ttCombineSameItem", false, null), 0), string.Empty, "ui_denied", null, false, false, 0f);
                }
                return false;
            }
        }
    }
}
