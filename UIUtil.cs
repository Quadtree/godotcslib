using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

public static class UIUtil
{
    public static Label Label(string text, string tooltip = null)
    {
        var lbl = new Label();
        lbl.Text = text;
        lbl.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        lbl.SizeFlagsVertical = Control.SizeFlags.Fill;
        lbl.HorizontalAlignment = HorizontalAlignment.Center;
        lbl.VerticalAlignment = VerticalAlignment.Center;
        if (tooltip != null) lbl.TooltipText = tooltip;
        return lbl;
    }

    public static void ClearChildren(this Node node)
    {
        foreach (var it in node.GetChildren()) ((Node)it).QueueFree();
    }

    public static bool IsPopupOpen(Node ctx)
    {
        return ctx.GetTree().Root.FindChildByPredicate<Popup>(it => it.Visible, 20) != null;
    }

    public static Node GetUIRoot(Node ctx)
    {
        return ctx.GetTree().CurrentScene.FindChildByType<CanvasLayer>() ?? ctx.GetTree().CurrentScene;
    }

    public static string WrapText(string text, int maxLineLength)
    {
        var lines = new List<string>();
        foreach (Match match in Regex.Matches(text, @".{1," + maxLineLength + @"}( |$)"))
        {
            lines.Add(match.Groups[0].Value.Trim());
        }
        //GD.Print(lines.Count);
        return string.Join("\n", lines);
    }
}