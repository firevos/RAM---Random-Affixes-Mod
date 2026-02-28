using HarmonyLib;

namespace WeaponAffixesProject
{
    // Applies an offset to the collectedItem HUD (Bottom right) if the killcounter is showing

    [HarmonyPatch(typeof(XUiC_CollectedItemList), nameof(XUiC_CollectedItemList.SetYOffset))]
    public static class XUiC_CollectedItemListSetYOffset
    {
        public static void Prefix(ref int _yOffset)
        {
            var lp = GameManager.Instance?.myEntityPlayerLocal;
            if (lp == null) return;

            ItemValue held = lp.inventory?.holdingItemItemValue;
            if (held == null || held.IsEmpty()) return;

            bool hasKills = held.TryGetMetadata("kills", out float _);
            bool hasUpg = held.TryGetMetadata("upgrades", out float _);

            // Your panel height is 46, so shift popup up by 46 when it is visible
            if (hasKills || hasUpg) _yOffset += 46;
        }
    }
}
