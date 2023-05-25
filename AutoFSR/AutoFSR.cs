using System;
using System.Threading;
using Godot;

public partial class AutoFSR : Node
{
    [Export]
    float TargetFPS = 120f;

    [Export]
    float MinScale = 0.5f;

    float AccumError = 0f;

    [Export]
    float GracePeriod = 5f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        GracePeriod -= (float)delta;
        if (GracePeriod >= 0) return;

        float curFPS = (float)Engine.GetFramesPerSecond();

        float error = curFPS - TargetFPS;
        AccumError = Util.Clamp(AccumError + error, -8000, 8000);

        float proportionalGain = 1f / 5000f;
        float integralGain = 1f / 35000f;

        float pv = error * proportionalGain + AccumError * integralGain + 1;

        float epv = Mathf.RoundToInt(Util.Clamp(pv, MinScale, 1.0f) * 10) / 10f;

        if (GetViewport().Msaa2D != Viewport.Msaa.Disabled)
        {
            GD.PushWarning("2D MSAA and FSR can't be enabled at the same time!");
            GetViewport().Msaa2D = Viewport.Msaa.Disabled;
        }

        if (epv < 1)
        {
            GetViewport().Scaling3DScale = epv;
            GetViewport().Scaling3DMode = Viewport.Scaling3DModeEnum.Fsr;
            GetViewport().FsrSharpness = 0.2f;
        }
        else
        {
            GetViewport().Scaling3DScale = 1;
            GetViewport().Scaling3DMode = Viewport.Scaling3DModeEnum.Bilinear;
        }

        var debugLabel = GetTree().Root.FindChildByName<Label>("FPSDebugInfo");
        if (debugLabel != null)
        {
            debugLabel.Text = $"Scale={GetViewport().Scaling3DScale} FPS={curFPS} PC={Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame)} AccumError={AccumError} " +
                $"NT={Mathf.RoundToInt(Performance.GetMonitor(Performance.Monitor.TimeNavigationProcess) * 1000)} PT={Mathf.RoundToInt(Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000)} TT={Mathf.RoundToInt(Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000)}";
        }
    }
}
