// Doesn't work yet.

//using UnityEngine.Scripting;

//// No namespace so XML can find it easily
//[Preserve]
//public class XUiC_ItemCosmeticStackGridOffset : XUiC_ItemCosmeticStackGrid
//{

//    public new void SetParts(ItemValue[] stackList)
//    {
//        Log.Out("Do we actually get in here?");
//        if (stackList == null)
//        {
//            Log.Out("stacklist is empty, returning.");
//            return;
//        }
//        currentItemClass = CurrentItem.itemValue.ItemClass;
//        Log.Out($"Currently assesing item: '{currentItemClass.Name}' with cosmetic modslots of: '{itemControllers.Length}'");
//        XUiC_ItemInfoWindow childByType = base.xui.GetChildByType<XUiC_ItemInfoWindow>();
//        for (int i = 1; i < itemControllers.Length; i++)
//        {
//            XUiC_ItemCosmeticStack xUiC_ItemCosmeticStack = (XUiC_ItemCosmeticStack)itemControllers[i];
//            if (i < CurrentItem.itemValue.CosmeticMods.Length)
//            {
//                ItemValue itemValue = CurrentItem.itemValue.CosmeticMods[i];
//                if (itemValue != null && itemValue.ItemClass is ItemClassModifier)
//                {
//                    xUiC_ItemCosmeticStack.SlotType = (itemValue.ItemClass as ItemClassModifier).Type.ToStringCached().ToLower();
//                }
//                xUiC_ItemCosmeticStack.SlotChangedEvent -= HandleSlotChangedEvent;
//                xUiC_ItemCosmeticStack.ItemValue = ((itemValue != null) ? itemValue : ItemValue.None.Clone());
//                xUiC_ItemCosmeticStack.SlotChangedEvent += HandleSlotChangedEvent;
//                xUiC_ItemCosmeticStack.SlotNumber = i;
//                xUiC_ItemCosmeticStack.InfoWindow = childByType;
//                xUiC_ItemCosmeticStack.StackLocation = StackLocation;
//                xUiC_ItemCosmeticStack.ViewComponent.IsVisible = true;
//            }
//            else
//            {
//                xUiC_ItemCosmeticStack.ViewComponent.IsVisible = false;
//            }
//        }
//    }
//}
