using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using WeaponAffixesProject;

[Preserve]
public class ItemActionEntryRerollAffix : BaseItemActionEntry
{
    public ItemActionEntryRerollAffix(XUiController controller) : base(controller, "lblContextActionRerollAffix", "ui_game_symbol_reroll", BaseItemActionEntry.GamepadShortCut.None, "crafting/craft_click_craft", "ui/ui_denied") {}
    private static float lastRerollTime = -999f;
    private const float CooldownSeconds = 1f;

    public override void RefreshEnabled()
    {
        // default enabled; you can disable if missing reagent etc
        base.RefreshEnabled();
    }

    public override void OnActivated()
    {
        Log.Out("[Affix] Reroll button clicked");
        
        XUiC_BasePartStack affixMod = this.ItemController as XUiC_BasePartStack;
        if (affixMod == null) return;
        if (!affixMod.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("affix_mod"))) return;
        var player = GameManager.Instance.myEntityPlayerLocal;
        if (player == null) return;
        XUiM_PlayerInventory playerInventory = this.ItemController.xui.PlayerInventory;

        if (Time.time - lastRerollTime < CooldownSeconds)
        {
            GameManager.ShowTooltip(player, string.Format(Localization.Get("ttWaitToReroll")), string.Empty, "ui_denied");
            return;
        }

        var xui = this.ItemController?.xui;
        if (xui == null) return;
        var assemble = xui.AssembleItem;
        var parentItemStack = assemble?.CurrentItem;
        var parentItemValue = parentItemStack?.itemValue;
        if (parentItemValue == null || parentItemValue.IsEmpty()) return;

        Log.Out($"Mod selected: '{affixMod.ItemClass.localizedName}'");
        Log.Out($"This mod is installed in: '{parentItemValue.ItemClass.localizedName}'");

        ItemClass requiredItem = ItemClass.GetItemClass("affixRerollToken", false);
        if (requiredItem == null) return;

        int count = playerInventory.GetItemCount(new ItemValue(requiredItem.Id, false));
        var requiredValue = new ItemValue(requiredItem.Id, false);
        var ingredients = new List<ItemStack> { new ItemStack(requiredValue, 1) };
        var cil = this.ItemController?.xui?.CollectedItemList;

        if (count > 0)
        {
            if (RerollAffix(ref parentItemStack, affixMod.itemClass))
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
                lastRerollTime = Time.time;
                GameManager.ShowTooltip(player, string.Format(Localization.Get("ttRerollAffixSucces")), string.Empty, "recipe_unlocked");
            }
        }
        else
        {
            Log.Out($"Player does not have item: '{requiredItem.Name}'");
            cil?.AddItemStack(new ItemStack(requiredValue, 0), false);

            // Show tooltip popup
            GameManager.ShowTooltip(player, string.Format(Localization.Get("ttRerollAffixRequiresItem"), requiredItem.localizedName), string.Empty);
        }
        return;
    }

    private static bool RerollAffix(ref ItemStack parentItem, ItemClass oldAffix)
    {
        ItemValue itemValue = parentItem.itemValue;
        int index = -1;
        List<List<ItemClassModifier>> modList = AffixUtils.GetCorrectModList(itemValue);
        if (modList == null || modList.Count == 0) return false;
        for (int i = 0; i < itemValue.CosmeticMods.Length; i++)
        {
            if (itemValue.CosmeticMods[i] == null || itemValue.CosmeticMods[i].IsEmpty()) continue;
            ItemClassModifier mod = itemValue.CosmeticMods[i].ItemClass as ItemClassModifier;
            modList = AffixUtils.RemoveSimilarMods(modList, mod);
            if (oldAffix.Name == mod.Name)
                index = i;
        }
        int tier = AffixUtils.RandomizeTierWithOdds(itemValue, GameManager.Instance.myEntityPlayerLocal);
        ItemClassModifier selectedMod = modList[tier][AffixUtils.rng.Next(modList[tier].Count)];

        if (index < 0) return false;
        if (oldAffix.Name.Contains("KillStreak"))
            GameManager.Instance.myEntityPlayerLocal.Buffs.RemoveBuff("buff" + oldAffix.Name);
        var mods = itemValue.CosmeticMods;
        mods[index] = new ItemValue(selectedMod.Id);
        parentItem.itemValue.CosmeticMods = mods;
        return true;
    }

}
