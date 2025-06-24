using Godot;

namespace GladiatorManager.addons.PrimerTools.TweenSystem;

public partial class TestTweenSequence : TweenSequence
{
    protected override void Define()
    {
        var cube = new MeshInstance3D();
        cube.Mesh = new BoxMesh();
        AddChild(cube);
        
        var sphere = new MeshInstance3D();
        sphere.Mesh = new SphereMesh();
        AddChild(sphere);

        // Sequential animations for cube
        AddStateChange(new PropertyAnimation(cube, "position", new Vector3(100, 0, 0), 1));
        AddStateChange(new PropertyAnimation(sphere, "position", new Vector3(100, 50, 0), 1));
        AddStateChange(new PropertyAnimation(cube, "position", new Vector3(0, 50, 0), 2));
        AddStateChangeInParallel(new PropertyAnimation(sphere, "position", new Vector3(50, 25, 0), 1), delay: 1);
        
        // Works correctly, playing these animations together after the previous ones
        // AddStateChange(new PropertyAnimation(cube, "position", new Vector3(100, 25, 0), 1));
        // AddStateChangeInParallel(new PropertyAnimation(sphere, "position", new Vector3(100, 25, 0), 1));
        
        // Instead of working like above, the sphere change happens immediately, even before the sphere change above
        var composite = new CompositeStateChange();
        composite.AddStateChange(new PropertyAnimation(cube, "position", new Vector3(100, 25, 0), 1));
        composite.AddStateChangeInParallel(new PropertyAnimation(sphere, "position", new Vector3(100, 25, 0), 1));
        AddStateChangeAt(composite, 8);
    }
}
