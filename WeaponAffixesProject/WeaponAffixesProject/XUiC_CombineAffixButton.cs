using UnityEngine.Scripting;

[Preserve]
public class XUiC_CombineAffixButton : XUiController
{
    private bool handlingPress;
    private bool boundButton;

    public override void Init()
    {
        base.Init();
        TryBindButton();
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        if (!boundButton)
            TryBindButton();
    }

    private void TryBindButton()
    {
        if (boundButton)
            return;

        OnPress += HandlePress;
        OnMouseUpDown += HandleMouseUpDown;

        XUiController button = GetChildById("button");
        if (button != null)
        {
            button.OnPress += HandlePress;
            button.OnMouseUpDown += HandleMouseUpDown;
        }

        boundButton = true;
    }

    public override void Pressed(int mouseButton)
    {
        DoCombine(mouseButton);
    }

    private void HandlePress(XUiController sender, int mouseButton)
    {
        DoCombine(mouseButton);
    }

    private void HandleMouseUpDown(XUiController sender, bool isDown)
    {
        if (!isDown)
            DoCombine(0);
    }

    private void DoCombine(int mouseButton)
    {
        if (handlingPress)
            return;
        if (mouseButton != 0)
            return;

        handlingPress = true;
        try
        {
            TryCombine();
        }
        finally
        {
            handlingPress = false;
        }
    }

    private void TryCombine()
    {
        XUiC_CombineWindowGroup group = GetParentByType<XUiC_CombineWindowGroup>();
        XUiC_CombineGrid grid = group?.GetChildByType<XUiC_CombineGrid>();
        if (grid == null || grid.lastResult == null || grid.lastResult.IsEmpty())
            return;

        EntityPlayerLocal player = xui?.playerUI?.entityPlayer;
        XUiM_PlayerInventory inventory = xui?.PlayerInventory;
        if (player == null || inventory == null)
            return;

        if (!WeaponAffixesProject.CombineAffixCost.CanTakeResult(grid))
            return;

        ItemStack result = grid.lastResult.Clone();
        if (!inventory.AddItemNoPartial(result, true))
        {
            GameManager.ShowTooltip(player, Localization.Get("xuiInventoryFull"), string.Empty, "ui/ui_denied");
            return;
        }

        if (!WeaponAffixesProject.CombineAffixCost.TryPayForResultPickup(grid))
            return;

        ClearCombineGrid(grid);
        WeaponAffixesProject.CombineAffixSelectionState.Clear(group);
    }

    private static void ClearCombineGrid(XUiC_CombineGrid grid)
    {
        grid.merge1.ItemStack = ItemStack.Empty;
        grid.merge2.ItemStack = ItemStack.Empty;

        grid.result1.SlotChangedEvent -= grid.Result1_SlotChangedEvent;
        grid.result1.ItemStack = ItemStack.Empty;
        grid.result1.HiddenLock = true;
        grid.result1.SlotChangedEvent += grid.Result1_SlotChangedEvent;

        grid.lastResult = ItemStack.Empty;
    }
}
