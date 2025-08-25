using Godot;
using PrimerTools;

namespace PrimerTools.TweenSystem;

public partial class TestStateChangeSequence : StateChangeSequence
{
    public override void Define()
    {
        var cube = new MeshInstance3D();
        cube.Mesh = new BoxMesh();
        AddChild(cube);
        
        var sphere = new MeshInstance3D();
        sphere.Mesh = new SphereMesh();
        AddChild(sphere);

        AddStateChangeWithDelay(cube.WalkTo(new Vector3(100, 0, 0)).WithDuration(1));
        AddStateChangeWithDelay(sphere.MoveTo(new Vector3(100, 50, 0)).WithDuration(1));
        AddStateChangeWithDelay(cube.WalkTo(new Vector3(0, 50, 0)).WithDuration(2));
        AddStateChangeInParallel(sphere.MoveTo(new Vector3(50, 25, 0)).WithDuration(1), delay: 1);
        
        // Test rotation
        AddStateChangeWithDelay(cube.RotateTo(0, 90, 0).WithDuration(1));
        
        // Test WalkTo (combines rotation and movement)
        AddStateChangeWithDelay(cube.WalkTo(new Vector3(50, 0, 0)).WithDuration(2));
        
        // Test Pulse
        AddStateChangeWithDelay(cube.Pulse(scaleFactor: 1.5f, attack: 0.3, hold: 0.2, decay: 0.3));
        
        var composite = new CompositeStateChange();
        composite.AddStateChangeWithDelay(cube.MoveTo(new Vector3(100, 25, 0)).WithDuration(1));
        composite.AddStateChangeInParallel(sphere.MoveTo(new Vector3(100, 25, 0)).WithDuration(1));
        AddStateChangeWithDelay(composite, 1);
    }
}
