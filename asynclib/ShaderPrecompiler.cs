using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class ShaderPrecompiler : Node3D
{
    public Queue<Material> MaterialQueue = new Queue<Material>();

    private IEnumerator<object> Steps;

    [Export]
    public string NextScene;

    public override void _Ready()
    {
        base._Ready();

        ScanForMaterials("res://");

        Steps = DoSteps().GetEnumerator();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Steps.MoveNext())
        {
            if ((NextScene?.Length ?? 0) > 0) GetTree().ChangeSceneToFile(NextScene);
        }
    }

    private void ScanForMaterials(string dir)
    {
        if (dir.StartsWith("res:///.")) return;

        var da = DirAccess.Open(dir);
        if (da == null) return;

        foreach (var it in da.GetFiles())
        {
            if (it.EndsWith(".tres"))
            {
                try
                {
                    var mat = ResourceLoader.Load<Material>($"{dir}/{it}", cacheMode: ResourceLoader.CacheMode.Ignore);
                    if (mat != null) MaterialQueue.Enqueue(mat);
                }
                catch (Exception) { }
            }
        }

        foreach (var it in da.GetDirectories())
        {
            ScanForMaterials($"{dir}/{it}");
        }
    }

    private Action<bool, ShaderPrecompiler>[] Permutations = new Action<bool, ShaderPrecompiler>[]{
        (v, t) => t.FindChildByType<OmniLight3D>().Visible = v,
        (v, t) => t.FindChildByType<DirectionalLight3D>().Visible = v,
        (v, t) => t.FindChildByType<WorldEnvironment>().Environment.SsrEnabled = v,
        (v, t) => t.FindChildByType<WorldEnvironment>().Environment.SsaoEnabled = v,
        (v, t) => t.FindChildByType<WorldEnvironment>().Environment.SsilEnabled = v,
        (v, t) => t.FindChildByType<WorldEnvironment>().Environment.SdfgiEnabled = v,
        (v, t) => t.FindChildByType<WorldEnvironment>().Environment.GlowEnabled = v,
    };

    private IEnumerable<object> DoSteps()
    {
        var startTime = Time.GetTicksUsec();
        GD.Print("Starting precompiling");
        Material mat;
        while (MaterialQueue.TryDequeue(out mat))
        {
            GD.Print($"Precompiling material {mat}");

            var meshInstance3D = new MeshInstance3D();
            meshInstance3D.Mesh = new BoxMesh();
            GetTree().CurrentScene.AddChild(meshInstance3D);

            meshInstance3D.GlobalPosition = new Vector3(Util.RandF(-.1f, .1f), Util.RandF(-.1f, .1f), 0);

            meshInstance3D.MaterialOverride = mat;
        }

        for (long permutation = 0; permutation < (1 << Permutations.Length); ++permutation)
        {
            for (var i = 0; i < Permutations.Length; ++i)
            {
                Permutations[i](((1 << i) & permutation) != 0, this);
            }

            yield return null;
        }

        GD.Print($"Shader precompilation completed in {(Time.GetTicksUsec() - startTime) / 1_000_000.0}s");
    }
}
