using Godot;

public static class Picking
{
    class PickResult
    {
        public Vector3? Pos;
        public RigidBody Hit;
    }

    private static PickResult PickAtCursor(Spatial ctx, float dist = 10000, uint collisionMask = 16384)
    {
        var cam = ctx.GetViewport().GetCamera();

        var raySrc = cam.ProjectRayOrigin(ctx.GetViewport().GetMousePosition());
        var rayNorm = cam.ProjectRayNormal(ctx.GetViewport().GetMousePosition());
        var rayTo = raySrc + rayNorm * dist;

        var curPos = ctx.GetWorld().DirectSpaceState.IntersectRay(raySrc, rayTo, null, collisionMask);

        var ret = new PickResult();

        if (curPos.Contains("position"))
            ret.Pos = (Vector3)curPos["position"];

        if (curPos.Contains("collider"))
            ret.Hit = (RigidBody)curPos["collider"];

        return ret;
    }

    public static Vector3? PickPointAtCursor(Spatial ctx, float dist = 10000, uint collisionMask = 16384)
    {
        return PickAtCursor(ctx, dist, collisionMask).Pos;
    }

    public static RigidBody PickObjectAtCursor(Spatial ctx, float dist = 10000, uint collisionMask = 16384)
    {
        return PickAtCursor(ctx, dist, collisionMask).Hit;
    }
}