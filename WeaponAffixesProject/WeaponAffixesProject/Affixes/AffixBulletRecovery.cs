using System.Collections.Generic;
using System.Linq;

namespace WeaponAffixesProject.Affixes
{
    public static class AffixBulletRecovery
    {
        public static void BulletRecoveryCheck(ItemActionData _actionData)
        {
            if (!_actionData.invData.itemValue.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("gun")) || _actionData.invData.itemValue.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("shotgun")))
                return;

            List<int> bulletRecoveries = AffixUtils.GetAllAffixesFromItem(_actionData.invData.itemValue, "bullet_casing");
            if (bulletRecoveries.Count <= 0)
                return;
            int bulletCasingTier = bulletRecoveries.Max();

            int chance = bulletCasingTier * 15;
            if (AffixUtils.rng.Next(0, 100) > chance)
                return;

            ItemClass bulletCasingClass = ItemClass.GetItemClass("resourceBulletCasing", false);
            var player = GameManager.Instance.myEntityPlayerLocal;
            if (player == null)
                return;

            var bulletCasing = new ItemStack(new ItemValue(bulletCasingClass.Id, false), 1);
            LocalPlayerUI uiforPlayer = LocalPlayerUI.GetUIForPlayer(_actionData.invData.holdingEntity as EntityPlayerLocal);
            XUiM_PlayerInventory playerInventory = uiforPlayer.xui.PlayerInventory;
            playerInventory.AddItem(bulletCasing);
        }
    }
}
