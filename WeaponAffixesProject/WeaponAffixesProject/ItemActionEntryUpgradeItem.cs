using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Prefab;

namespace WeaponAffixesProject
{
    internal class ItemActionEntryUpgradeItem: BaseItemActionEntry
    {
        public ItemActionEntryUpgradeItem(XUiController controller) : base(controller, "lblContextActionUpgradeAffix", "ui_game_symbol_upgrade", BaseItemActionEntry.GamepadShortCut.None, "crafting/craft_click_craft", "ui/ui_denied") { }
        
        public override void RefreshEnabled()
        {
            // default enabled; you can disable if missing reagent etc
            base.Enabled = true;
        }

        public override void OnActivated()
        {
            Log.Out("[Affix] Upgrade button clicked");

            XUiC_BasePartStack affixMod = this.ItemController as XUiC_BasePartStack;
            if (affixMod == null) return;
            if (!affixMod.ItemClass.HasAnyTags(AffixUtils.AffixTag)) return;
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

            ItemClass requiredItem = ItemClass.GetItemClass("affixUpgradeToken", false);
            if (requiredItem == null) return;

            int count = playerInventory.GetItemCount(new ItemValue(requiredItem.Id, false));
            var requiredValue = new ItemValue(requiredItem.Id, false);
            var ingredients = new List<ItemStack> { new ItemStack(requiredValue, 1) };
            var cil = this.ItemController?.xui?.CollectedItemList;

            if (count <= 0)
            {
                Log.Out($"Player does not have item: '{requiredItem.Name}'");
                cil?.AddItemStack(new ItemStack(requiredValue, 0), false);

                // Show tooltip popup
                GameManager.ShowTooltip(player, string.Format(Localization.Get("ttUpgradeAffixRequiresItem"), requiredItem.localizedName), string.Empty, "ui_denied");
                return;
            }

            string affixName = affixMod.itemClass.Name;
            char lastChar = affixName[affixName.Length - 1];
            if (char.IsDigit(lastChar))
            {
                int value = lastChar - '0';
                if (value < 6)
                {
                    // Check which slot the affix is in
                    List<int> upgradeSlot = new List<int>();
                    for (int i = 0; i < parentItemValue.CosmeticMods.Length; i++)
                    {
                        if (parentItemValue.CosmeticMods[i].ItemClass.Name == affixName)
                        {
                            upgradeSlot.Add(i);
                            break;
                        }
                    }
                    
                    // Now you can upgrade
                    if (AffixSystem.UpgradeAffix(parentItemValue, upgradeSlot, ref affixName))
                    {
                        //succes
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
                        if (parentItemValue.TryGetMetadata("upgrades", out float upgrades) &&
                            parentItemValue.TryGetMetadata("nextUpgrade", out float nextUpgrade) &&
                            parentItemValue.TryGetMetadata("lastUpgrade", out float lastUpgrade))
                        {
                            Log.Out("We're in the business/");
                            int magicSlayerLvl = 0;
                            try
                            {
                                magicSlayerLvl = player.Progression.GetProgressionValue("perkMagicSlayer").level;
                            }
                            catch (Exception e)
                            {
                                Log.Out($"Can't find magic slayer perk: '{e}'");
                            }
                            parentItemStack.itemValue.SetMetadata("upgrades", upgrades + 1);
                            parentItemStack.itemValue.SetMetadata("nextUpgrade", lastUpgrade + ((AffixUtils.requiredKills - 10 * magicSlayerLvl) * (upgrades + 2)));
                            this.ItemController.xui.AssembleItem.currentItem = parentItemStack;
                            this.ItemController.xui.AssembleItem.RefreshAssembleItem();
                        }
                        playerInventory.RemoveItems(ingredients, 1, null);
                        cil?.RemoveItemStack(new ItemStack(requiredValue, 1));
                        GameManager.ShowTooltip(player, string.Format(Localization.Get("ttUpgradeAffixSucces")), string.Empty, "recipe_unlocked");
                        return;
                    }
                }
            }
            // If we reached this, upgrade was unsuccesfull
            GameManager.ShowTooltip(player, string.Format(Localization.Get("ttFailedToUpgrade")), string.Empty, "ui_denied");
        }

    }
}
