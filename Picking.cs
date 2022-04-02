using Godot;

public static class Picking
{
    public static Vector3? PickPointAtCursor(Spatial ctx, float dist = 10000, uint collisionMask = 16384)
    {
        var cam = ctx.GetViewport().GetCamera();

        var raySrc = cam.ProjectRayOrigin(ctx.GetViewport().GetMousePosition());
        var rayNorm = cam.ProjectRayNormal(ctx.GetViewport().GetMousePosition());
        var rayTo = raySrc + rayNorm * dist;

        var curPos = ctx.GetWorld().DirectSpaceState.IntersectRay(raySrc, rayTo, null, collisionMask);

        if (curPos.Contains("position"))
        {
            var pos = (Vector3)curPos["position"];
            return pos;
        }

        return null;
    }
}