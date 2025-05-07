using Godot;
using Hiker.CamSystem;

namespace Hiker;

public partial class HikerCamera : VirtualCamera
{
    public Vector3 TargetPosition { get; set; }
    public float TargetDistance { get; set; } = 10f;

    public override void _Process(double delta)
    {
        base._Process(delta);

        GlobalPosition = TargetPosition + GlobalTransform.Basis.Z * TargetDistance;
    }
}
