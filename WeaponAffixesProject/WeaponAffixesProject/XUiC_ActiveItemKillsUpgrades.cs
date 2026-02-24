using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class XUiC_ActiveItemKillsUpgrades : XUiController
{
    private EntityPlayerLocal localPlayer;

    private float kills;
    private float lastKills;
    private bool hasKills;
    private bool haslastKills;

    private bool ammoHudVisible;
    private int baseY;           // original Y from XML
    private bool baseYCaptured;

    public override void Init()
    {
        base.Init();
        IsDirty = true;
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);

        if (localPlayer == null && XUi.IsGameRunning()) localPlayer = xui.playerUI.entityPlayer;
        if (localPlayer == null || viewComponent == null) return;

        // Capture base Y once (from XML)
        if (!baseYCaptured)
        {
            baseY = viewComponent.Position.y;
            baseYCaptured = true;
        }

        ItemValue heldItem = localPlayer.inventory.holdingItemItemValue;

        float newKills = 0f, newlastKills = 0f;
        bool newHasKills = false, newHaslastKills = false;
        bool newAmmoHudVisible = false;

        if (heldItem != null && !heldItem.IsEmpty())
        {
            newHasKills = heldItem.TryGetMetadata("kills", out newKills);
            newHaslastKills = heldItem.TryGetMetadata("nextUpgrade", out newlastKills);

            // Match vanilla ammo HUD logic (good enough & cheap)
            float magSize = EffectManager.GetValue(PassiveEffects.MagazineSize, heldItem, 0f, localPlayer);

            newAmmoHudVisible = magSize > 0f;
        }

        bool needsRefresh =
            IsDirty ||
            newKills != kills ||
            newlastKills != lastKills ||
            newHasKills != hasKills ||
            newHaslastKills != haslastKills ||
            newAmmoHudVisible != ammoHudVisible;

        if (needsRefresh)
        {
            kills = newKills;
            lastKills = newlastKills;
            hasKills = newHasKills;
            haslastKills = newHaslastKills;
            ammoHudVisible = newAmmoHudVisible;

            UpdatePanelPosition();

            IsDirty = false;
            RefreshBindings(true);
        }
    }

    private void UpdatePanelPosition()
    {
        if (viewComponent == null) return;

        int y = baseY;
        if (ammoHudVisible) y += 46;

        viewComponent.Position = new Vector2i(viewComponent.Position.x, y);
    }

    public override bool GetBindingValueInternal(ref string _value, string _bindingName)
    {
        switch (_bindingName)
        {
            case "kills":
                _value = hasKills ? Mathf.FloorToInt(kills).ToString() : "0";
                return true;

            case "nextUpgrade":
                _value = haslastKills ? Mathf.FloorToInt(lastKills).ToString() : "0";
                return true;

            case "kuvisible":
                _value = (hasKills || haslastKills).ToString().ToLower();
                return true;
        }

        return base.GetBindingValueInternal(ref _value, _bindingName);
    }
}
