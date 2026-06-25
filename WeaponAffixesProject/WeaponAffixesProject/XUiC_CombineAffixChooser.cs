using UnityEngine.Scripting;

[Preserve]
public class XUiC_CombineAffixChooser : XUiController
{
    private const int Width = 153;
    private const int HeaderHeight = 46;
    private const int CellHeight = 75;
    private const int ButtonHeight = 42;
    private const int ButtonInset = 3;
    private int lastMaxAffixes = -1;

    public override void Init()
    {
        base.Init();
        ApplyConfiguredSize();
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        ApplyConfiguredSize();
    }

    private void ApplyConfiguredSize()
    {
        int maxAffixes = GetConfiguredMaxAffixes();
        int slotHeight = maxAffixes * CellHeight;
        int contentHeight = slotHeight + ButtonHeight;
        int windowHeight = HeaderHeight + contentHeight;

        Resize(ViewComponent, Width, windowHeight);

        XUiController content = GetChildById("content");
        if (content != null)
        {
            Resize(content.ViewComponent, Width, contentHeight);
            ResizeChild(content, "backgroundMain", Width, contentHeight);
            ResizeChild(content, "background", Width, contentHeight);
            ResizeChild(content, "combineButton", Width, ButtonHeight);
            MoveChild(content, "combineButton", 0, -slotHeight);

            XUiController combineButton = content.GetChildById("combineButton");
            if (combineButton != null)
            {
                ResizeChild(combineButton, "buttonBorder", Width, ButtonHeight);
                ResizeChild(combineButton, "button", Width - ButtonInset * 2, ButtonHeight - ButtonInset * 2);
                ResizeChild(combineButton, "label", Width, ButtonHeight);
                MoveChild(combineButton, "button", ButtonInset, -ButtonInset);
                MoveChild(combineButton, "label", Width / 2, -ButtonHeight / 2);
            }
        }

        lastMaxAffixes = maxAffixes;
    }

    private static void ResizeChild(XUiController parent, string childId, int width, int height)
    {
        XUiController child = parent.GetChildById(childId);
        if (child != null)
            Resize(child.ViewComponent, width, height);
    }

    private static void Resize(XUiView view, int width, int height)
    {
        if (view == null)
            return;

        view.Width = width;
        view.Height = height;
    }

    private static void MoveChild(XUiController parent, string childId, int x, int y)
    {
        XUiController child = parent.GetChildById(childId);
        if (child?.ViewComponent != null)
            child.ViewComponent.Position = new Vector2i(x, y);
    }

    private static int GetConfiguredMaxAffixes()
    {
        return WeaponAffixesProject.AffixUtils.GetConfiguredMaxAffixes();
    }
}
