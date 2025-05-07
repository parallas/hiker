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

    private bool jumpInput;

    public override void _Ready()
    {
        SetPhysicsProcess(true);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        // Velocity += Vector3.Up * GravitySpeed * (float)delta;

        // Velocity = Velocity.Clamp(-TerminalVelocity, TerminalVelocity);

        if(Velocity.LengthSquared() < TerminalVelocity * TerminalVelocity)
        {
            Velocity = MathUtil.Approach(
                Velocity,
                Vector3.Up * TerminalVelocity,
                GravitySpeed * (float)delta
            );
        }

        if(IsOnFloor())
        {
            if(jumpInput)
            {
                jumpInput = false;

                Velocity = Velocity with { Y = 8 };
            }
        }

        MoveAndSlide();
    }
}
