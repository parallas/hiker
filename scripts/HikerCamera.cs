using Godot;
using Hiker.CamSystem;
using Parallas;

namespace Hiker;

public partial class HikerCamera : VirtualCamera
{
    public Vector3 TargetPosition { get; set; }
    public float TargetDistance { get; set; } = 5f;
    public bool DoInputs { get; set; } = true;

    private Vector2 _lastMousePos = Vector2.Zero;
    private Vector2 _mouseDelta = Vector2.Zero;

    private Vector2 _rotationValues = Vector2.Zero;

    private Vector3 _rootPosition = Vector3.Zero;

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        HandleInputs(delta);

        UpdateRotation();

        _rootPosition = MathUtil.ExpDecay(_rootPosition, TargetPosition, 10f, (float)delta);

        float distanceRing = TargetDistance;
        float amount = MathUtil.Clamp01(-GlobalTransform.Basis.Z.Y);
        distanceRing *= Mathf.Lerp(1f, 0.1f, amount);
        GlobalPosition = _rootPosition + GlobalTransform.Basis.Z * distanceRing;
    }

    public override void _Input(InputEvent inputEvent)
    {
        base._Input(inputEvent);

        if (!DoInputs) return;

        if (inputEvent is InputEventMouseMotion mouseEvent)
        {
            _rotationValues += -mouseEvent.ScreenRelative * 0.1f;
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
        _rotationValues.Y = Mathf.Clamp(_rotationValues.Y, -65f, 65f);
        GlobalRotation = GlobalRotation with { X = Mathf.DegToRad(_rotationValues.Y), Y = Mathf.DegToRad(_rotationValues.X) };
    }
}
