using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WeaponAffixesProject;

public class ItemActionEntryUnlockAffix : BaseItemActionEntry
{
    public ItemActionEntryUnlockAffix(XUiController controller) : base(controller, "lblContextActionUnlockAffix", "ui_game_symbol_unlock2", BaseItemActionEntry.GamepadShortCut.None, "crafting/craft_click_craft", "ui/ui_denied") { }

    public override void RefreshEnabled()
    {
        base.Enabled = true;
    }

    public override void OnActivated()
    {
        Log.Out("[Affix] Unlock button clicked");

        XUiC_ItemStack item = this.ItemController as XUiC_ItemStack;
        if (item == null) return;
        if (!item.itemClass.HasAnyTags(AffixUtils.ArmorTag) && !item.itemClass.HasAnyTags(AffixUtils.WeaponTag) && !item.itemClass.HasAnyTags(AffixUtils.ToolTag)) return;
        var player = GameManager.Instance.myEntityPlayerLocal;
        if (player == null) return;
        XUiM_PlayerInventory playerInventory = this.ItemController.xui.PlayerInventory;


        var xui = this.ItemController?.xui;
        if (xui == null) return;

        string materialName = item.itemClass.MadeOfMaterial.GetLocalizedMaterialName();

        ItemClass requiredItem = ItemClass.GetItemClass("affixUnlockToken", false);
        ItemClass requiredItem2 = GetItemFromMaterial(materialName);
        Log.Out(materialName);
        if (requiredItem == null) return;

        int count = playerInventory.GetItemCount(new ItemValue(requiredItem.Id, false));
        int count2 = requiredItem2 == null ? 10 : playerInventory.GetItemCount(new ItemValue(requiredItem2.Id, false));
        ItemValue requiredValue2 = null;
        if (requiredItem2 != null)
        {
            requiredValue2 = new ItemValue(requiredItem2.Id, false);
        }
        var requiredValue = new ItemValue(requiredItem.Id, false);

        var ingredients = requiredValue2 != null ? new List<ItemStack> {
            new ItemStack(requiredValue, 1),
            new ItemStack(requiredValue2, 10)
        } : new List<ItemStack>
        {
            new ItemStack(requiredValue, 1)
        };
        var cil = this.ItemController?.xui?.CollectedItemList;

        if (count > 0 && count2 > 9)
        {
            string newAffix = "";
            if (UnlockAffix(ref item.itemStack, ref newAffix))
            {
                this.ItemController.xui.itemStack = item;
                this.ItemController.xui.itemStack.ForceRefreshItemStack();

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
                        cosmeticGrid.CurrentItem = item.itemStack;

                        // Rebuild the cosmetic UI entries from the current item
                        cosmeticGrid.SetParts(item.itemStack.itemValue.CosmeticMods);

                        // (Optional but often helps) update the assemble window state too
                        assembleWindow.ItemStack = item.itemStack;
                        assembleWindow.OnChanged();
                    }
                }

                playerInventory.RemoveItems(ingredients, 1, null);
                foreach(var ingredient in ingredients)
                {
                    Log.Out(ingredient.itemValue.ItemClass.localizedName);
                    cil?.RemoveItemStack(ingredient);
                }
                AffixUtils.ApplyQuestEventManagerUseItem("affixUnlockToken");
                GameManager.ShowTooltip(player, string.Format(Localization.Get("ttUnlockAffixSucces"), newAffix), string.Empty, "recipe_unlocked");
            }
            else
            {
                GameManager.ShowTooltip(player, string.Format(Localization.Get("ttUnlockAffixFail")), string.Empty, "ui/ui_denied");
            }
        }
        else
        {
            if (count < 1 && count2 < 10)
            {
                cil?.AddItemStack(new ItemStack(requiredValue, 0), false);
                cil?.AddItemStack(new ItemStack(requiredValue2, 0), false);
            }
            else if (count < 1 && count2 >= 10)
            {
                cil?.AddItemStack(new ItemStack(requiredValue, 0), false);
            }
            else if (count >= 1 && count2 < 10)
            {
                cil?.AddItemStack(new ItemStack(requiredValue2, 0), false);
            }

            // Show tooltip popup
            if (requiredItem2 != null)
                GameManager.ShowTooltip(player, string.Format(Localization.Get("ttUnlockAffixRequiresItem"), requiredItem.localizedName, requiredItem2.localizedName), string.Empty, "ui/ui_denied");
            else
                GameManager.ShowTooltip(player, string.Format(Localization.Get("ttUnlockAffixRequiresItem2"), requiredItem.localizedName), string.Empty, "ui/ui_denied");
        }
        return;
    }

    public static bool UnlockAffix(ref ItemStack itemStack, ref string newAffix)
    {
        if (itemStack.itemValue.CosmeticMods.Count() >= 7) return false;

        return AffixSystem.AddNewAffix(itemStack.itemValue, ref newAffix, true);
    }

    public static ItemClass GetItemFromMaterial(string materialName)
    {
        if (!materialName.Contains("Parts") && !materialName.Contains("armor") && !materialName.Contains("MmeleeToolAllSteel"))
            return null;
        if (materialName.Contains("armor"))
            return ItemClass.GetItemClass("armorParts", true);
        if (materialName.Contains("MmeleeToolAllSteel"))
            return ItemClass.GetItemClass("meleeToolAllSteelParts", true);
        List<ItemClass> items = ItemClass.GetItemsWithTag(FastTags<TagGroup.Global>.GetTag("parts"));
        foreach(var item in items)
        {
            if (item.MadeOfMaterial.GetLocalizedMaterialName() == materialName)
            {
                return item;
            }
        }
        return null;
    }
}
