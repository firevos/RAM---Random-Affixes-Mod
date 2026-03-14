using HarmonyLib;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using UAI;
using UnityEngine;
using static Twitch.BaseTwitchEventEntry;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(ItemActionRanged), nameof(ItemActionRanged.ConsumeAmmo))]
    public static class ItemActionRangedConsumeAmmo
    {
        private static void Prefix(ItemActionRanged __instance, ItemActionData _actionData)
        {
            // if it is a gun but not a shotgun
            if (_actionData.invData.itemValue.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("gun")) && !_actionData.invData.itemValue.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("shotgun")))
            {
                int bulletCasingTier = 0;
                // Check if it has a bulletcasing affix mod and what rarity.
                if (_actionData.invData.itemValue.CosmeticMods?.Length > 0)
                {
                    bulletCasingTier = CheckListForBulletCasingAffix(_actionData.invData.itemValue.CosmeticMods);
                    
                }
                if (bulletCasingTier == 0 && _actionData.invData.itemValue.Modifications?.Length > 0)
                {
                    bulletCasingTier = CheckListForBulletCasingAffix(_actionData.invData.itemValue.Modifications);
                }
                if (bulletCasingTier == 0)
                {
                    return;
                }

                int chance = bulletCasingTier * 15;
                if (AffixUtils.rng.Next(0, 100) > chance)
                {
                    return; 
                }

                ItemClass bulletCasingClass = ItemClass.GetItemClass("resourceBulletCasing", false);
                var player = GameManager.Instance.myEntityPlayerLocal;
                if (player == null)
                {
                    return;
                }
                var bulletCasing = new ItemStack(new ItemValue(bulletCasingClass.Id, false), 1);
                LocalPlayerUI uiforPlayer = LocalPlayerUI.GetUIForPlayer(_actionData.invData.holdingEntity as EntityPlayerLocal);
                XUiM_PlayerInventory playerInventory = uiforPlayer.xui.PlayerInventory;
                playerInventory.AddItem(bulletCasing);        
            }            
        }

        private static int CheckListForBulletCasingAffix(ItemValue[] modList)
        {
            foreach (var mod in modList)
            {
                if (mod == null || mod.IsEmpty())
                {
                    continue;
                }
                if (mod.ItemClass.Name.Contains("affixModBulletCasing"))
                {
                    return int.Parse(mod.ItemClass.Name.Substring(mod.ItemClass.Name.Length - 1));
                }
            }
            return 0;
        }
    }
}
