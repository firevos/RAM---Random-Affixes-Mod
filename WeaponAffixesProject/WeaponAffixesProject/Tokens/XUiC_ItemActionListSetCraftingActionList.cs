using HarmonyLib;
using System;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(XUiC_ItemActionList), nameof(XUiC_ItemActionList.SetCraftingActionList))]
    public static class XUiC_ItemActionListSetCraftingActionList
    {
        private static readonly System.Reflection.MethodInfo MI_AddActionListEntry = AccessTools.Method(typeof(XUiC_ItemActionList), "AddActionListEntry");

        private static void Postfix(XUiC_ItemActionList __instance, XUiC_ItemActionList.ItemActionListTypes _actionListType, XUiController itemController)
        {
            try
            {
                if (_actionListType != XUiC_ItemActionList.ItemActionListTypes.Part) return;
                if (!(itemController is XUiC_ItemCosmeticStack) && !(itemController is XUiC_ItemPartStack)) return;

                var xui = __instance.xui;
                var parent = xui?.AssembleItem?.CurrentItem;
                if (parent == null || parent.IsEmpty() || parent.itemValue == null || parent.itemValue.IsEmpty()) return;

                if (itemController is XUiC_ItemCosmeticStack && !parent.itemValue.ItemClass.HasAnyTags(AffixUtils.UniqueAffixTag))
                {
                    var cosmeticController = (XUiC_ItemCosmeticStack)itemController;
                    var selectedClass = cosmeticController.ItemStack?.itemValue?.ItemClass;

                    if (selectedClass == null || !selectedClass.HasAnyTags(AffixUtils.AffixTag)) return;

                    MI_AddActionListEntry?.Invoke(__instance, new object[] { new ItemActionEntryRerollAffix(itemController) });
                    MI_AddActionListEntry?.Invoke(__instance, new object[] { new ItemActionEntryExtractAffix(itemController) });
                    MI_AddActionListEntry?.Invoke(__instance, new object[] { new ItemActionEntryUpgradeAffix(itemController) });
                }
                else if (itemController is XUiC_ItemPartStack)
                {
                    var partController = (XUiC_ItemPartStack)itemController;
                    var selectedClass2 = partController.ItemStack?.itemValue?.ItemClass;

                    if (selectedClass2 == null || !selectedClass2.HasAnyTags(AffixUtils.AffixTag)) return;

                    MI_AddActionListEntry?.Invoke(__instance, new object[] { new ItemActionEntryExtractAffix(itemController) });
                }

            }
            catch (Exception e)
            {
                Log.Out($"[REROLL DEBUG] Failed adding reroll/extract action: {e}");
            }
        }
    }
}
