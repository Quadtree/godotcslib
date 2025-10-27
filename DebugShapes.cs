using Godot;

public static class DebugShapes
{
    public static void DrawDebugLine(Node3D ctx, Vector3 start, Vector3 end, float durationSeconds = 1, Color? color = null)
    {
        if (!OS.IsDebugBuild()) return;

        color ??= Colors.Red;

        GD.Print($"DrawDebugLine({ctx}, {start}, {end}, {durationSeconds}, {color})");

        var rootNode = new Node3D();

        var cylInst = new MeshInstance3D();
        var cyl = new CylinderMesh();
        cyl.Height = start.DistanceTo(end);
        cyl.TopRadius = 0.01f;
        cyl.BottomRadius = 0.01f;

        var mat = new StandardMaterial3D();
        mat.AlbedoColor = color.Value;
        mat.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        cyl.Material = mat;

        cylInst.Mesh = cyl;

        rootNode.AddChild(cylInst);

        ctx.GetTree().CurrentScene.AddChild(rootNode);
        rootNode.GlobalPosition = (start + end) / 2;

        var delta = (end - start).Normalized();

        if (Mathf.IsZeroApprox(delta.Cross(Vector3.Up).LengthSquared()))
        {
            rootNode.LookAt(end, Vector3.Forward);
        }
        else
        {
            rootNode.LookAt(end);
        }

        cylInst.RotateX(Mathf.Pi / 2);

        Util.StartOneShotTimer(rootNode, durationSeconds, () => rootNode.QueueFree());
    }

    public static void DrawDebugSphere(Node3D ctx, Vector3 pos, float radius = 0.02f, float durationSeconds = 1, Color? color = null)
    {
        if (!OS.IsDebugBuild()) return;

        color ??= Colors.Red;

        GD.Print($"DrawDebugSphere({ctx}, {pos}, {radius}, {durationSeconds}, {color})");

        var rootNode = new Node3D();

        var cylInst = new MeshInstance3D();
        var cyl = new SphereMesh();
        cyl.Radius = radius;

        var mat = new StandardMaterial3D();
        mat.AlbedoColor = color.Value;
        mat.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        cyl.Material = mat;

        cylInst.Mesh = cyl;

        rootNode.AddChild(cylInst);

        ctx.GetTree().CurrentScene.AddChild(rootNode);
        rootNode.GlobalPosition = pos;

        Util.StartOneShotTimer(rootNode, durationSeconds, () => rootNode.QueueFree());
    }
}