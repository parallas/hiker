using Godot;
using Parallas;

namespace Hiker.CamSystem;

public partial class VirtualCamera : Node3D
{
    [Export]
    public int Priority { get; set; } = 0;

    public override void _Ready()
    {
        base._Ready();

        SmartCamera.VirtualCameras.Add(this);
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        SmartCamera.VirtualCameras.Remove(this);
    }
}
