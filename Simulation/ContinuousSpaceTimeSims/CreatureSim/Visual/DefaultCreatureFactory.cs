using Godot;
using PrimerTools.Simulation;

namespace PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim.Visual;

public class DefaultCreatureFactory : ICreatureFactory
{
    public ICreatureModelHandler CreateInstance()
    {
        var node3D = new Node3D();
        var meshInstance = new MeshInstance3D();
        meshInstance.Mesh = new SphereMesh();
        node3D.AddChild(meshInstance);
        meshInstance.Position = Vector3.Up / 2f;
        return new DefaultCreatureModelHandler(node3D);
    }
}