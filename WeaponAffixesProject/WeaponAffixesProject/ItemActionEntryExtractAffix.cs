using Audio;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;
using WeaponAffixesProject;

[Preserve]
public class ItemActionEntryExtractAffix : BaseItemActionEntry
{
    public ItemActionEntryExtractAffix(XUiController controller) : base(controller, "lblContextActionExtractAffix", "ui_game_symbol_extract", BaseItemActionEntry.GamepadShortCut.None, "crafting/craft_click_craft", "ui/ui_denied") { }

    public override void RefreshEnabled()
    {
        // default enabled; you can disable if missing reagent etc
        base.Enabled = true;
    }

    public override void OnActivated()
    {
        Log.Out("[Affix] Extract button clicked");

        XUiC_BasePartStack affixMod = this.ItemController as XUiC_BasePartStack;
        if (affixMod == null) return;
        if (!affixMod.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("affix_mod"))) return;
        var player = GameManager.Instance.myEntityPlayerLocal;
        if (player == null) return;
        XUiM_PlayerInventory playerInventory = this.ItemController.xui.PlayerInventory;

        var xui = this.ItemController?.xui;
        if (xui == null) return;
        var assemble = xui.AssembleItem;
        var parentItemStack = assemble?.CurrentItem;
        var parentItemValue = parentItemStack?.itemValue;
        if (parentItemValue == null || parentItemValue.IsEmpty()) return;

        Log.Out($"Mod selected: '{affixMod.ItemClass.localizedName}'");
        Log.Out($"This mod is installed in: '{parentItemValue.ItemClass.localizedName}'");

        ItemClass requiredItem = ItemClass.GetItemClass("affixExtractionToken", false);
        if (requiredItem == null) return;

        int count = playerInventory.GetItemCount(new ItemValue(requiredItem.Id, false));
        var requiredValue = new ItemValue(requiredItem.Id, false);
        var ingredients = new List<ItemStack> { new ItemStack(requiredValue, 1) };
        var cil = this.ItemController?.xui?.CollectedItemList;

        if (!player.inventory.CanTakeItem(affixMod.itemStack) && !player.bag.CanTakeItem(affixMod.itemStack))
        {
            GameManager.ShowTooltip(player, string.Format(Localization.Get("xuiInventoryFullForPickup")), string.Empty, "ui_denied");
            return;
        }
        if (count > 0)
        {
            if (ExtractAffix(ref parentItemStack, affixMod))
            {
                this.ItemController.xui.AssembleItem.currentItem = parentItemStack;
                this.ItemController.xui.AssembleItem.RefreshAssembleItem();

                // Get assemble window + cosmetic grid
                var assembleWg = xui.FindWindowGroupByName("assemble");
                if (assembleWg != null)
                {
                    var cosmeticGrid = assembleWg.GetChildByType<XUiC_ItemCosmeticStackGrid>();
                    var assembleWindow = assembleWg.GetChildByType<XUiC_AssembleWindow>();

                    if (cosmeticGrid != null && assembleWindow != null)
                    {
                        // This is what HandleSlotChangedEvent does when a user changes a slot:
                        cosmeticGrid.AssembleWindow = assembleWindow;
                        cosmeticGrid.CurrentItem = parentItemStack;

                        // Rebuild the cosmetic UI entries from the current item
                        cosmeticGrid.SetParts(parentItemStack.itemValue.CosmeticMods);

                        // (Optional but often helps) update the assemble window state too
                        assembleWindow.ItemStack = parentItemStack;
                        assembleWindow.OnChanged();
                    }
                }
                playerInventory.RemoveItems(ingredients, 1, null);
                cil?.RemoveItemStack(new ItemStack(requiredValue, 1));
                GameManager.ShowTooltip(player, string.Format(Localization.Get("ttExtractionAffixSucces")), string.Empty, "recipe_unlocked");
            }
        }
        else
        {
            Log.Out($"Player does not have item: '{requiredItem.Name}'");
            cil?.AddItemStack(new ItemStack(requiredValue, 0), false);

            // Show tooltip popup
            GameManager.ShowTooltip(player, string.Format(Localization.Get("ttExtractionAffixRequiresItem"), requiredItem.localizedName), string.Empty);
        }

        return;
    }

    private bool ExtractAffix(ref ItemStack parentItem, XUiC_BasePartStack affix)
    {
        // Move affix, like Take.
        if (affix == null) return false;
        affix.HandleMoveToPreferredLocation();

        // If in cosmetic slot, remove one cosmetic slot from parentItem and move all affixes up by 1
        if (affix is XUiC_ItemCosmeticStack)
        {
            var newCosmeticModsList = new ItemValue[parentItem.itemValue.CosmeticMods.Length - 1];
            int j = 0;
            for (int i = 0; i < parentItem.itemValue.CosmeticMods.Length; i++)
            {
                if (parentItem.itemValue.CosmeticMods[i] != null && !parentItem.itemValue.CosmeticMods[i].IsEmpty())
                {
                    newCosmeticModsList[j] = parentItem.itemValue.CosmeticMods[i];
                    j++;
                }
            }
            parentItem.itemValue.CosmeticMods = newCosmeticModsList;
        }

        return true;
    }
}
