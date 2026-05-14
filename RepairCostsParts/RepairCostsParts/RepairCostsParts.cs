using HarmonyLib;
using UnityEngine;

namespace RepairCostsParts
{
    public class RepairCostsParts : IModApi
    {
        public void InitMod(Mod __mod)
        {
            Log.Out("[RepairCostsParts] Initializing...");
            var harmony = new Harmony("com.example.weaponbuff");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(ItemActionEntryRepair), "OnActivated")]
    public class RepairExtraMaterialPatch
    {
        static bool Prefix(ItemActionEntryRepair __instance)
        {
            XUi xui = __instance.ItemController.xui;
            XUiM_PlayerInventory playerInventory = xui.PlayerInventory;

            ItemStack itemStack = ItemStack.Empty;

            if (__instance.ItemController is XUiC_EquipmentStack equipStack)
            {
                itemStack = equipStack.ItemStack;
            }
            else if (__instance.ItemController is XUiC_ItemStack itemUiStack)
            {
                itemUiStack.TimeIntervalElapsedEvent += __instance.ItemActionEntryRepair_TimeIntervalElapsedEvent;
                itemStack = itemUiStack.ItemStack;
            }

            ItemValue itemValue = itemStack.itemValue;
            ItemClass forId = ItemClass.GetForId(itemValue.type);

            if (forId?.RepairTools == null || forId.RepairTools.Length <= 0)
                return false;

            ItemClass repairTool = ItemClass.GetItemClass(forId.RepairTools[0].Value, false);

            if (repairTool == null)
                return false;

            int maxRepairUses = Convert.ToInt32(
                Math.Ceiling((double)((float)Mathf.CeilToInt(itemValue.UseTimes) / (float)repairTool.RepairAmount.Value))
            );

            int repairToolCount = Mathf.Min(
                playerInventory.GetItemCount(new ItemValue(repairTool.Id, false)),
                maxRepairUses
            );

            if (repairToolCount <= 0)
            {
                GameManager.ShowTooltip(
                    xui.playerUI.entityPlayer,
                    Localization.Get("xuiRepairMissingMats"),
                    false, false, 0f
                );
                return false;
            }

            //--------------------------------------------------
            // EXTRA MATERIAL REQUIREMENT
            //--------------------------------------------------

            string materialName = forId.MadeOfMaterial.GetLocalizedMaterialName();
            ItemClass extraRequirement = GetItemFromMaterial(materialName);

            int extraCount = 1; // configurable amount

            if (extraRequirement != null)
            {
                int owned = playerInventory.GetItemCount(new ItemValue(extraRequirement.Id, false));

                if (owned < extraCount)
                {
                    GameManager.ShowTooltip(
                        xui.playerUI.entityPlayer,
                        $"Missing {extraRequirement.GetLocalizedItemName()}",
                        false, false, 0f
                    );
                    return false;
                }
            }

            //--------------------------------------------------
            // BUILD RECIPE
            //--------------------------------------------------

            int repairAmount = repairToolCount * repairTool.RepairAmount.Value;

            Recipe recipe = new Recipe();
            recipe.count = 1;
            recipe.craftExpGain = Mathf.CeilToInt(forId.RepairExpMultiplier * repairToolCount);
            recipe.itemValueType = itemValue.type;
            recipe.craftingTime = repairTool.RepairTime.Value * repairToolCount;

            recipe.ingredients.Add(
                new ItemStack(new ItemValue(repairTool.Id, false), repairToolCount)
            );

            if (extraRequirement != null)
            {
                recipe.ingredients.Add(
                    new ItemStack(new ItemValue(extraRequirement.Id, false), extraCount)
                );
            }

            //--------------------------------------------------
            // QUEUE REPAIR
            //--------------------------------------------------

            XUiC_CraftingWindowGroup crafting =
                xui.FindWindowGroupByName("crafting").GetChildByType<XUiC_CraftingWindowGroup>();

            if (crafting == null)
                return false;

            if (!crafting.AddRepairItemToQueue(recipe.craftingTime, itemValue.Clone(), repairAmount))
            {
                __instance.warnQueueFull();
                return false;
            }

            //--------------------------------------------------
            // REMOVE ITEM FROM SLOT
            //--------------------------------------------------

            if (__instance.ItemController is XUiC_EquipmentStack eq)
            {
                eq.ItemStack = ItemStack.Empty.Clone();
                xui.PlayerEquipment.Equipment.SetPreferredItemSlot(eq.SlotNumber, itemValue);
            }
            else if (__instance.ItemController is XUiC_ItemStack stack)
            {
                stack.ItemStack = ItemStack.Empty.Clone();
            }

            //--------------------------------------------------
            // REMOVE MATERIALS
            //--------------------------------------------------

            playerInventory.RemoveItems(recipe.ingredients, 1, null);

            return false; // skip original method
        }

        //--------------------------------------------------
        // YOUR MATERIAL LOOKUP
        //--------------------------------------------------

        public static ItemClass GetItemFromMaterial(string materialName)
        {
            if (!materialName.Contains("Parts") &&
                !materialName.Contains("armor") &&
                !materialName.Contains("MmeleeToolAllSteel"))
                return null;

            if (materialName.Contains("armor"))
                return ItemClass.GetItemClass("armorParts", true);

            if (materialName.Contains("MmeleeToolAllSteel"))
                return ItemClass.GetItemClass("meleeToolAllSteelParts", true);

            List<ItemClass> items =
                ItemClass.GetItemsWithTag(FastTags<TagGroup.Global>.GetTag("parts"));

            foreach (var item in items)
            {
                if (item.MadeOfMaterial.GetLocalizedMaterialName() == materialName)
                    return item;
            }

            return null;
        }
    }
}
