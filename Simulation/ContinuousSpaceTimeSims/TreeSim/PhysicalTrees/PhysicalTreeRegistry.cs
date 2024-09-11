using System;
using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public class PhysicalTreeRegistry : IEntityRegistry<PhysicalTree>
{
    public World3D World3D;

    public PhysicalTreeRegistry(World3D world3D)
    {
        PhysicalTree.World3D = world3D;
        
        // TODO: Get rid of this. It's only here to stop things from breaking during refactoring.
        // It can go once the physical tree struct handles tree creation
        World3D = world3D;
    }

    public void RegisterEntity(IEntity entity)
    {
        if (entity is not PhysicalTree physicalTree)
        {
            GD.PrintErr($"{GetType()} was passed the wrong kind of entity. {entity.GetType()} instead of PhysicalTree.");
            return;
        }
        
        physicalTree.Initialize();
        TreeLookup.Add(physicalTree.Body, Entities.Count);
        Entities.Add(physicalTree);
    }

    public List<PhysicalTree> Entities { get; } = new();
    public readonly Dictionary<Rid, int> TreeLookup = new();

    public void CreateTree(Vector3 position, FruitTreeSim treeSim)
    {
        var transform = Transform3D.Identity.Translated(position);

        switch (treeSim.VisualizationMode)
        {
            case VisualizationMode.None:
                break;
            case VisualizationMode.Debug:
                // var treeMesh = new CylinderMesh();
                // treeMesh.TopRadius = 0.5f;
                // treeMesh.BottomRadius = 0.5f;
                // treeMesh.Height = 2.0f;
                //
                // var visualTransform = transform.ScaledLocal(0.5f * Vector3.One); // Smol because the original tree is not mature
                // var bodyMesh = RenderingServer.InstanceCreate2(treeMesh.GetRid(), World3D.Scenario);
                // RenderingServer.InstanceSetTransform(bodyMesh, visualTransform);
                //
                // VisualTrees.Add(new VisualDebugTree
                // {
                //     BodyMesh = bodyMesh,
                //     MeshResource = treeMesh
                // });
                break;
            case VisualizationMode.NodeCreatures:
                // var tree = new NodeTree();
                // treeSim.AddChild(tree);
                // tree.Scale = Vector3.One * 0.5f; // Start as a sapling
                // tree.Position = position;
                // tree.Name = "Tree";
                // NodeTrees.Add(tree);
                // tree.Owner = treeSim.GetTree().EditedSceneRoot;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    public void Reset()
    {
        foreach (var tree in Entities) tree.CleanUp();
        
        Entities.Clear();
        TreeLookup.Clear();
    }
}
