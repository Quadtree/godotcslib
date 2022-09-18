using Godot;

public static class UIUtil
{
    public static Label Label(string text, string tooltip = null)
    {
        var lbl = new Label();
        lbl.Text = text;
        lbl.SizeFlagsHorizontal = (int)Control.SizeFlags.Fill;
        lbl.SizeFlagsVertical = (int)Control.SizeFlags.Fill;
        lbl.HorizontalAlignment = HorizontalAlignment.Center;
        lbl.VerticalAlignment = VerticalAlignment.Center;
        if (tooltip != null) lbl.TooltipText = tooltip;
        return lbl;
    }

    public static void ClearChildren(this Node node)
    {
        foreach (var it in node.GetChildren()) ((Node)it).QueueFree();
    }
}