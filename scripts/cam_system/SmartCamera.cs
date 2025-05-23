using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Hiker.CamSystem;

public partial class SmartCamera : Camera3D
{
    public static readonly List<VirtualCamera> VirtualCameras = new List<VirtualCamera>();

    public override void _Ready()
    {
        base._Ready();

        ProcessPriority = 999999;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (!VirtualCameras.Any()) return;

        var priorityVcam = VirtualCameras.OrderBy(camera => camera.Priority).FirstOrDefault();
        if (priorityVcam is null) return;

        GlobalPosition = priorityVcam.GlobalPosition;
        GlobalRotation = priorityVcam.GlobalRotation;
    }
}
