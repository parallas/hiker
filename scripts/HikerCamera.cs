using Godot;
using Hiker.CamSystem;

namespace Hiker;

public partial class HikerCamera : VirtualCamera
{
    public Vector3 TargetPosition { get; set; }
    public float TargetDistance { get; set; } = 5f;
    public bool DoInputs { get; set; } = true;

    private Vector2 _lastMousePos = Vector2.Zero;
    private Vector2 _mouseDelta = Vector2.Zero;

    private Vector2 _rotationValues = Vector2.Zero;

    public override void _Ready()
    {
        base._Ready();

        Input.SetMouseMode(Input.MouseModeEnum.Captured);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        HandleInputs(delta);

        UpdateRotation();
        GlobalPosition = TargetPosition + GlobalTransform.Basis.Z * TargetDistance;
    }

    public override void _Input(InputEvent inputEvent)
    {
        base._Input(inputEvent);

        if (!DoInputs) return;

        if (inputEvent is InputEventMouseMotion mouseEvent)
        {
            _rotationValues += -mouseEvent.ScreenRelative * 0.1f;
            UpdateRotation();
        }
    }

    private void HandleInputs(double delta)
    {
        if (!DoInputs) return;

        Vector2 inputDir = Input.GetVector(
            "-look_yaw",
            "+look_yaw",
            "-look_pitch",
            "+look_pitch"
        );

        _rotationValues += inputDir * (float)delta * 80f;
    }

    private void UpdateRotation()
    {
        _rotationValues.Y = Mathf.Clamp(_rotationValues.Y, -80f, 80f);
        GlobalRotation = GlobalRotation with { X = Mathf.DegToRad(_rotationValues.Y), Y = Mathf.DegToRad(_rotationValues.X) };
    }
}
