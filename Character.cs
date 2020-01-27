/**
 * Note: This code was originally obtained from https://docs.godotengine.org/en/3.1/tutorials/3d/fps_tutorial/part_one.html
 * and is therefore under the CC-BY 3.0 License https://github.com/godotengine/godot-docs/blob/master/LICENSE.txt
 * The code has been modified from the original version
 */
using Godot;
using System;

public class Character : KinematicBody
{
    [Export]
    public float Gravity = -24.8f;
    [Export]
    public float MaxSpeed = 20.0f;
    [Export]
    public float JumpSpeed = 18.0f;
    [Export]
    public float Accel = 4.5f;
    [Export]
    public float Deaccel = 16.0f;
    [Export]
    public float MaxSlopeAngle = 40.0f;
    [Export]
    public float MouseSensitivity = 0.05f;

    private Vector3 _vel = new Vector3();
    private Vector3 _dir = new Vector3();

    private Camera _camera;
    private Spatial _rotationHelper;

    private bool IsJumping = false;
    private bool IsOnGround = false;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _camera = GetNode<Camera>("RotationBase/Rotation_Helper/Camera");
        _rotationHelper = GetNode<Spatial>("RotationBase/Rotation_Helper");

        Input.SetMouseMode(Input.MouseMode.Captured);
    }

    public override void _PhysicsProcess(float delta)
    {
        ProcessInput(delta);
        ProcessMovement(delta);
    }

    private void ProcessInput(float delta)
    {
        //  -------------------------------------------------------------------
        //  Walking
        _dir = new Vector3();
        Transform camXform = _camera.GetGlobalTransform();

        Vector2 inputMovementVector = new Vector2();

        if (Input.IsActionPressed("movement_forward"))
            inputMovementVector.y += 1;
        if (Input.IsActionPressed("movement_backward"))
            inputMovementVector.y -= 1;
        if (Input.IsActionPressed("movement_left"))
            inputMovementVector.x -= 1;
        if (Input.IsActionPressed("movement_right"))
            inputMovementVector.x += 1;

        inputMovementVector = inputMovementVector.Normalized();

        // Basis vectors are already normalized.
        _dir += -camXform.basis.z * inputMovementVector.y;
        _dir += camXform.basis.x * inputMovementVector.x;
        //  -------------------------------------------------------------------

        //  -------------------------------------------------------------------
        //  Jumping
        if (IsOnGround)
        {
            if (Input.IsActionJustPressed("movement_jump"))
            {
                _vel.y = JumpSpeed;
                IsJumping = true;
                Console.WriteLine($"ALEZOUP");
            }
        }
        //  -------------------------------------------------------------------

        //  -------------------------------------------------------------------
        //  Capturing/Freeing the cursor
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            if (Input.GetMouseMode() == Input.MouseMode.Visible)
                Input.SetMouseMode(Input.MouseMode.Captured);
            else
                Input.SetMouseMode(Input.MouseMode.Visible);
        }
        //  -------------------------------------------------------------------
    }

    private void ProcessMovement(float delta)
    {
        _dir.y = 0;
        _dir = _dir.Normalized();

        Vector3 hvel = _vel;
        hvel.y = 0;

        Vector3 target = _dir;

        target *= MaxSpeed;

        float accel;
        if (_dir.Dot(hvel) > 0)
            accel = Accel;
        else
            accel = Deaccel;

        if (!IsOnGround) accel *= 0.2f;

        hvel = hvel.LinearInterpolate(target, accel * delta);
        _vel.x = hvel.x;
        //if (!IsOnFloor())
            _vel.y -= 9.8f * delta;
        //else
        //    _vel.y = -40;
        _vel.z = hvel.z;

        if (_vel.y < 0) IsJumping = false;

        //Console.WriteLine($"IsOnFloor={IsOnFloor()} _vel={_vel} JumpTimer={JumpTimer}");
        //if (JumpTimer <= 0.01f)
        //    _vel = MoveAndSlideWithSnap(_vel, new Vector3(0, -40, 0), new Vector3(0, 1, 0), true, 4, Mathf.Deg2Rad(MaxSlopeAngle));
        //else
        _vel = MoveAndSlide(_vel, new Vector3(0, 1, 0), false, 4, Mathf.Deg2Rad(MaxSlopeAngle));

        //Console.WriteLine($"Speed={_vel.Length()}");

        if (!IsJumping)
        {
            float highestGroundPoint = -10000;

            for (int i=0;i<4;++i)
            {
                var rayDelta = new Vector3(0.2f * Mathf.Cos(Mathf.Pi / 2 * i), 0, 0.2f * Mathf.Sin(Mathf.Pi / 2 * i));
                var rayStart = Translation + rayDelta + new Vector3(0, 2, 0);
                var rayEnd = rayStart + new Vector3(0, -4, 0);

                var ret = GetWorld().DirectSpaceState.IntersectRay(rayStart, rayEnd, new Godot.Collections.Array(){GetRid()});
                if (ret.Count > 0)
                {
                    var intersectPoint = (Vector3)ret["position"];
                    var normal = (Vector3)ret["normal"];

                    //Console.WriteLine($"anlge={normal.AngleTo(Vector3.Up)}");

                    if (normal.AngleTo(Vector3.Up) < Mathf.Deg2Rad(MaxSlopeAngle))
                    {
                        highestGroundPoint = Mathf.Max(highestGroundPoint, intersectPoint.y);
                    }
                }
            }

            float ourHeight = Translation.y;

            float heightAboveGround = ourHeight - highestGroundPoint;

            //Console.WriteLine($"(heightAboveGround / Mathf.Abs(heightAboveGround))={(heightAboveGround / Mathf.Abs(heightAboveGround))} (_vel.y / Mathf.Abs(_vel.y))={(_vel.y / Mathf.Abs(_vel.y))}");

            if (Mathf.Abs(heightAboveGround) < 1f && (Mathf.Abs((heightAboveGround / Mathf.Abs(heightAboveGround)) - (_vel.y / Mathf.Abs(_vel.y))) < 1 || IsOnGround))
            {
                // we are supposed to be glued to the ground
                //Console.WriteLine($"GLUED heightAboveGround={heightAboveGround}");
                Translation -= new Vector3(0, heightAboveGround, 0);
                IsOnGround = true;
                _vel.y = 0;
                //_vel.y = -heightAboveGround * 100;
            } else {
                IsOnGround = false;
                //Console.WriteLine($"FLYING heightAboveGround={heightAboveGround} _vel.y={_vel.y}");
            }
        } else {
            IsOnGround = false;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion && Input.GetMouseMode() == Input.MouseMode.Captured)
        {
            InputEventMouseMotion mouseEvent = @event as InputEventMouseMotion;
            _rotationHelper.RotateX(Mathf.Deg2Rad(-mouseEvent.Relative.y * MouseSensitivity));
            RotateY(Mathf.Deg2Rad(-mouseEvent.Relative.x * MouseSensitivity));

            Vector3 cameraRot = _rotationHelper.RotationDegrees;
            cameraRot.x = Mathf.Clamp(cameraRot.x, -70, 70);
            _rotationHelper.RotationDegrees = cameraRot;
        }
    }
}