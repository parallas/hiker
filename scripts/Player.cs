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
    public Camera3D Camera { get; set; }

    private bool jumpInput;

    private float jumpVel;
    private Vector3 motionVel;
    private Vector2 inputDir;

    private double steepTimer = 3;

    public override void _Ready()
    {
        SetPhysicsProcess(true);

        jumpVel = Mathf.Sqrt(2 * Math.Abs(GravitySpeed) * 0.6f);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");

        if(Input.IsActionJustPressed("jump"))
        {
            jumpInput = true;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        bool collided = MoveAndSlide();

        Vector3 targetPlanarVel = 3 * new Vector3(inputDir.X, 0, inputDir.Y);
        targetPlanarVel = targetPlanarVel.Rotated(UpDirection, Camera.GlobalRotation.Y);

        Vector3 PlanarVelocity = MathUtil.ProjectOnPlane(Velocity, UpDirection);
        Vector3 VerticalVelocity = MathUtil.Project(Velocity, UpDirection);
        VerticalVelocity = MathUtil.Approach(VerticalVelocity, -UpDirection * TerminalVelocity, -GravitySpeed * (float)delta);

        if(IsOnFloor() && Mathf.RadToDeg(GetFloorAngle()) < 80)
        {
            var degAngle = Mathf.RadToDeg(GetFloorAngle());
            if(jumpInput)
            {
                VerticalVelocity = Vector3.Up * jumpVel;
                steepTimer = 0;
            }
            else if(degAngle > 50)
            {
                steepTimer -= delta;
                if(steepTimer <= 0)
                {
                    FloorMaxAngle = 0;
                }
            }
            else if(degAngle > 45)
            {
                steepTimer = 3;
                FloorMaxAngle = Mathf.DegToRad(80);
            }
            else
            {
                steepTimer = 3;
                FloorMaxAngle = Mathf.DegToRad(80);
            }

            targetPlanarVel *= MathUtil.InverseLerp01(85, 45, degAngle);
        }
        else if(IsOnFloor())
        {
            FloorMaxAngle = Mathf.DegToRad(30);
        }

        PlanarVelocity = MathUtil.Approach(PlanarVelocity, targetPlanarVel, 10 * (float)delta);

        Velocity = PlanarVelocity + VerticalVelocity;

        jumpInput = false;
    }
}
