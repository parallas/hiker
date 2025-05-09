using Godot;

namespace Hiker;

public partial class Main : Node
{
    public override void _Ready()
    {
        // GetViewport().SetScaling3DMode(Viewport.Scaling3DModeEnum.Bilinear);
        // GetViewport().SetScaling3DScale(0.5f);
        Input.SetMouseMode(Input.MouseModeEnum.Captured);
        Engine.MaxFps = 120;
    }

    public override void _Process(double delta)
    {
        if (Input.IsKeyPressed(Key.Escape)) Input.SetMouseMode(Input.MouseModeEnum.Visible);
        if (Input.IsMouseButtonPressed(MouseButton.Left)) Input.SetMouseMode(Input.MouseModeEnum.Captured);
    }
}
