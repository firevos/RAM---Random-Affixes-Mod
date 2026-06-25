using System.Collections.Generic;
using System.Text;

namespace WeaponAffixesProject
{
    internal static class CombineAffixCost
    {
        private const string ExtractionTokenName = "affixExtractionToken";

        internal static bool CanTakeResult(XUiC_CombineGrid grid)
        {
            return CheckResultCosts(grid, false);
        }

        internal static bool TryPayForResultPickup(XUiC_CombineGrid grid)
        {
            return CheckResultCosts(grid, true);
        }

        private static bool CheckResultCosts(XUiC_CombineGrid grid, bool payCosts)
        {
            if (grid == null)
                return true;

            ItemStack result = grid.result1.ItemStack != null && !grid.result1.ItemStack.IsEmpty()
                ? grid.result1.ItemStack
                : grid.lastResult;
            if (result == null || result.IsEmpty())
                return true;

            XUiC_CombineWindowGroup group = grid.GetParentByType<XUiC_CombineWindowGroup>();
            int importedAffixes = CombineAffixSelectionState.CountSelectedAffixesFromItemB(group, grid.merge2.ItemStack?.itemValue);
            if (importedAffixes <= 0)
                return true;

            XUi xui = grid.xui;
            EntityPlayerLocal player = xui?.playerUI?.entityPlayer;
            XUiM_PlayerInventory inventory = xui?.PlayerInventory;
            if (player == null || inventory == null)
                return false;

            List<ItemStack> costs = BuildCosts(grid.merge1.ItemStack?.itemValue, importedAffixes);
            List<ItemStack> missing = GetMissingCosts(inventory, costs);
            if (missing.Count > 0)
            {
                ShowMissingCost(player, xui, missing);
                return false;
            }

            if (!payCosts)
                return true;

            inventory.RemoveItems(costs, 1, null);
            foreach (ItemStack cost in costs)
            {
                xui.CollectedItemList?.RemoveItemStack(cost);
            }

            AffixUtils.ApplyQuestEventManagerUseItem(ExtractionTokenName);
            return true;
        }

        private static List<ItemStack> BuildCosts(ItemValue itemValue, int importedAffixes)
        {
            List<ItemStack> costs = new List<ItemStack>();

            ItemClass extractionToken = ItemClass.GetItemClass(ExtractionTokenName, false);
            if (extractionToken != null)
                costs.Add(new ItemStack(new ItemValue(extractionToken.Id, false), importedAffixes));

            ItemClass parts = GetPartsItem(itemValue);
            if (parts != null)
                costs.Add(new ItemStack(new ItemValue(parts.Id, false), importedAffixes * 5));

            return costs;
        }

        private static ItemClass GetPartsItem(ItemValue itemValue)
        {
            ItemClass itemClass = itemValue?.ItemClass;
            string materialName = itemClass?.MadeOfMaterial?.GetLocalizedMaterialName();
            return string.IsNullOrEmpty(materialName) ? null : ItemActionEntryUnlockAffix.GetItemFromMaterial(materialName);
        }

        private static List<ItemStack> GetMissingCosts(XUiM_PlayerInventory inventory, List<ItemStack> costs)
        {
            List<ItemStack> missing = new List<ItemStack>();
            foreach (ItemStack cost in costs)
            {
                int owned = inventory.GetItemCount(cost.itemValue);
                if (owned < cost.count)
                    missing.Add(cost);
            }

            return missing;
        }

        private static void ShowMissingCost(EntityPlayerLocal player, XUi xui, List<ItemStack> missing)
        {
            StringBuilder message = new StringBuilder("You need ");
            for (int i = 0; i < missing.Count; i++)
            {
                ItemStack cost = missing[i];
                if (i > 0)
                    message.Append(i == missing.Count - 1 ? " and " : ", ");

                message.Append(cost.count);
                message.Append(' ');
                message.Append(cost.itemValue.ItemClass.localizedName);
                xui.CollectedItemList?.AddItemStack(new ItemStack(cost.itemValue, 0), false);
            }

            message.Append(" to move the selected affix");
            if (missing.Count != 1 || missing[0].count != 1)
                message.Append("es");
            message.Append(" from Item B.");

            GameManager.ShowTooltip(player, message.ToString(), string.Empty, "ui/ui_denied");
        }
    }
}
