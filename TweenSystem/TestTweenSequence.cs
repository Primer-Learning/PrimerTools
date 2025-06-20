using Godot;

namespace GladiatorManager.addons.PrimerTools.TweenSystem;

public partial class TestTweenSequence : TweenSequence
{
    protected override void Define()
    {
        var cube = new MeshInstance3D();
        cube.Mesh = new BoxMesh();
        AddChild(cube);

        AnimateProperty(cube, "position", new Vector3(100, 0, 0), 2);
        AnimateProperty(cube, "position", new Vector3(100, 50, 0), 2);
        AnimateProperty(cube, "position", new Vector3(0, 50, 0), 2);
        AnimateProperty(cube, "position", new Vector3(0, 0, 0), 2);
    }
}