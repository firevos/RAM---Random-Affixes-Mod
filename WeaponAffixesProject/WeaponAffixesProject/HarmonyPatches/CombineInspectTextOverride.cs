using HarmonyLib;

namespace WeaponAffixesProject.HarmonyPatches
{
    internal static class CombineInspectTextOverride
    {
        private const string RamCombineInfoPanelKey = "ramCombineInfoPanelText";
        private static bool isCombineWindowOpen;

        internal static void SetCombineWindowOpen(bool isOpen)
        {
            isCombineWindowOpen = isOpen;
        }

        internal static void ApplyTo(XUiC_EmptyInfoWindow emptyInfoWindow)
        {
            if (!isCombineWindowOpen || emptyInfoWindow?.descriptionText == null)
                return;

            emptyInfoWindow.descriptionText.Text = Localization.Get(RamCombineInfoPanelKey, false, null);
        }
    }

    [HarmonyPatch(typeof(XUiC_CombineWindowGroup), nameof(XUiC_CombineWindowGroup.OnOpen))]
    internal static class CombineInspectTextCombineOpenPatch
    {
        private static void Prefix()
        {
            CombineInspectTextOverride.SetCombineWindowOpen(true);
        }
    }

    [HarmonyPatch(typeof(XUiC_CombineWindowGroup), nameof(XUiC_CombineWindowGroup.OnClose))]
    internal static class CombineInspectTextCombineClosePatch
    {
        private static void Postfix()
        {
            CombineInspectTextOverride.SetCombineWindowOpen(false);
        }
    }

    [HarmonyPatch(typeof(XUiC_EmptyInfoWindow), nameof(XUiC_EmptyInfoWindow.OnOpen))]
    internal static class CombineInspectTextEmptyInfoOpenPatch
    {
        private static void Postfix(XUiC_EmptyInfoWindow __instance)
        {
            CombineInspectTextOverride.ApplyTo(__instance);
        }
    }

    [HarmonyPatch(typeof(XUiC_EmptyInfoWindow), nameof(XUiC_EmptyInfoWindow.UpdateDescriptionText))]
    internal static class CombineInspectTextEmptyInfoUpdateDescriptionPatch
    {
        private static void Postfix(XUiC_EmptyInfoWindow __instance)
        {
            CombineInspectTextOverride.ApplyTo(__instance);
        }
    }
}
