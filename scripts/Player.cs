using Godot;
using Parallas;
using System;

namespace Hiker;

public partial class Player : CharacterBody3D
{
    [Export]
    public float GravitySpeed { get; set; }

    [Export]
    public float TerminalVelocity { get; set; }

    [Export]
    public HikerCamera HikerCamera { get; set; }

    private Camera3D _mainCamera;

    private bool _jumpInput;

    private float _jumpVel;
    private Vector3 _motionVel;
    private Vector2 _inputDir;

    private double _steepTimer = 3;

    public override void _Ready()
    {
        SetPhysicsProcess(true);

        _jumpVel = Mathf.Sqrt(2 * Math.Abs(GravitySpeed) * 0.6f);

        _mainCamera = GetViewport().GetCamera3D();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        _inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");

        if(Input.IsActionJustPressed("jump"))
        {
            _jumpInput = true;
        }

        HikerCamera.TargetPosition = GlobalPosition + Vector3.Up * 1.45f;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        bool collided = MoveAndSlide();

        float targetMoveSpeed = 3f;

        Vector3 planarVelocity = MathUtil.ProjectOnPlane(Velocity, UpDirection);
        Vector3 verticalVelocity = MathUtil.Project(Velocity, UpDirection);
        verticalVelocity = MathUtil.Approach(verticalVelocity, -UpDirection * TerminalVelocity, -GravitySpeed * (float)delta);

        if(IsOnFloor())
        {
            HandleFloorSteepness(ref targetMoveSpeed, delta);
            HandleJumpInput(ref verticalVelocity);
        }

        Vector3 targetPlanarVel = targetMoveSpeed * new Vector3(_inputDir.X, 0, _inputDir.Y);
        targetPlanarVel = targetPlanarVel.Rotated(UpDirection, _mainCamera.GlobalRotation.Y);
        planarVelocity = MathUtil.Approach(planarVelocity, targetPlanarVel, 10 * (float)delta);

        Velocity = planarVelocity + verticalVelocity;
    }

    private void HandleFloorSteepness(ref float targetMoveSpeed, double delta)
    {
        var degAngle = Mathf.RadToDeg(GetFloorAngle());
        targetMoveSpeed *= MathUtil.InverseLerp01(85, 45, degAngle);

        if (degAngle > 50)
        {
            _steepTimer -= delta;
            if (_steepTimer <= 0)
            {
                FloorMaxAngle = 0;
            }
            return;
        }

        _steepTimer = 3;
        FloorMaxAngle = Mathf.DegToRad(80);
    }

    private void HandleJumpInput(ref Vector3  verticalVelocity)
    {
        if(_jumpInput)
        {
            verticalVelocity = Vector3.Up * _jumpVel;
            _steepTimer = 0;
        }
        _jumpInput = false;
    }
}
