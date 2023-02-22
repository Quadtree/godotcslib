using Godot;

public static class Picking
{
    class PickResult
    {
        public Vector3? Pos;
        public PhysicsBody3D Hit;
    }

    private static PickResult PickAtCursor(Node3D ctx, float dist = 10000, uint collisionMask = 16384)
    {
        var cam = ctx.GetViewport().GetCamera3D();

        var raySrc = cam.ProjectRayOrigin(ctx.GetViewport().GetMousePosition());
        var rayNorm = cam.ProjectRayNormal(ctx.GetViewport().GetMousePosition());
        var rayTo = raySrc + rayNorm * dist;

        var fp = new PhysicsRayQueryParameters3D();
        fp.From = raySrc;
        fp.To = rayTo;
        fp.CollisionMask = collisionMask;

        var curPos = ctx.GetWorld3D().DirectSpaceState.IntersectRay(fp);

        var ret = new PickResult();

        if (curPos.ContainsKey("position"))
            ret.Pos = (Vector3)curPos["position"];

        if (curPos.ContainsKey("collider"))
            ret.Hit = (PhysicsBody3D)curPos["collider"];

        return ret;
    }

    public static Vector3? PickPointAtCursor(Node3D ctx, float dist = 10000, uint collisionMask = 16384)
    {
        return PickAtCursor(ctx, dist, collisionMask).Pos;
    }

    public static PhysicsBody3D PickObjectAtCursor(Node3D ctx, float dist = 10000, uint collisionMask = 16384)
    {
        return PickAtCursor(ctx, dist, collisionMask).Hit;
    }
}