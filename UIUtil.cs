using System;
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

    public static RichTextLabel RichLabel(string text, string tooltip = null)
    {
        var lbl = new RichTextLabel();
        lbl.BbcodeEnabled = true;
        lbl.Text = $"[center]{text}[/center]";
        lbl.FitContent = true;
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

    public static string GetActionKey(string action)
    {
        var evts = InputMap.ActionGetEvents(action);

        return evts[0].AsText().Replace(" (Physical)", "");
    }

    public static void DrawLineOnImage(Image image, Vector2 start, Vector2 end, Color color)
    {
        var mov = (end - start).Normalized();

        var cp = start;
        var prevPosI = new Vector2I(-1000, -1000);

        (bool, bool) computeSigns()
        {
            var delta = end - cp;
            return (
                delta.X >= 0,
                delta.Y >= 0
            );
        }

        var initialSigns = computeSigns();
        var iterations = 0;

        while (computeSigns() == initialSigns)
        {
            var nPosI = new Vector2I(Mathf.RoundToInt(cp.X), Mathf.RoundToInt(cp.Y));
            if (nPosI != prevPosI)
            {
                image.SetPixel(nPosI.X, nPosI.Y, color);
                prevPosI = nPosI;
            }

            cp += mov;

            if (iterations++ > 100_000) throw new Exception("Too many iterations");
        }
    }

    public static Image TintImage(Image src, Color color)
    {
        var ret = (Image)src.Duplicate(true);

        for (var y = 0; y < ret.GetHeight(); ++y)
        {
            for (var x = 0; x < ret.GetWidth(); ++x)
            {
                ret.SetPixel(x, y, ret.GetPixel(x, y) * color);
            }
        }

        return ret;
    }

    public static Image RescaleImage(Image src, Vector2I newSize)
    {
        var ret = (Image)src.Duplicate(true);

        ret.Resize(newSize.X, newSize.Y);

        return ret;
    }

    public static Rect2I ImageSizeAsRect(Image img)
    {
        return new Rect2I(0, 0, img.GetWidth(), img.GetHeight());
    }
}