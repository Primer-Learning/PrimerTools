using System;
using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public class TreeSimEntityRegistry
{
    public World3D World3D;

    public struct PhysicalTree
    {
        public Rid Body;
        public Vector3 Position;
        public float Age;
        public bool IsMature;
        public float TimeSinceLastSpawn;
        public SphereShape3D BodyShapeResource;
        public bool IsDead;
        public bool HasFruit;
        public float FruitGrowthProgress;

        public void FreeRids()
        {
            PhysicsServer3D.FreeRid(Body);
        }
    }

    public struct VisualTree
    {
        public Rid BodyMesh;
        public Rid FruitMesh;
        public CylinderMesh MeshResource;
        public SphereMesh FruitMeshResource;

        public void FreeRids()
        {
            RenderingServer.FreeRid(BodyMesh);
            if (FruitMesh != default)
            {
                RenderingServer.FreeRid(FruitMesh);
            }
        }
    }

    public readonly List<PhysicalTree> PhysicalTrees = new();
    public readonly List<VisualTree> VisualTrees = new();
    public readonly List<Tree> NodeTrees = new();
    public readonly Dictionary<Rid, int> TreeLookup = new();

    public PhysicalTree CreateTree(Vector3 position, TreeSim treeSim)
    {
        var transform = Transform3D.Identity.Translated(position);

        // PhysicsServer3D setup
        var bodyArea = PhysicsServer3D.AreaCreate();
        PhysicsServer3D.AreaSetSpace(bodyArea, World3D.Space);
        PhysicsServer3D.AreaSetTransform(bodyArea, transform);
        var bodyShape = new SphereShape3D();
        bodyShape.Radius = 1.0f;
        PhysicsServer3D.AreaAddShape(bodyArea, bodyShape.GetRid());

        var physicalTree = new PhysicalTree
        {
            Body = bodyArea,
            Position = position,
            Age = 0,
            IsMature = false,
            TimeSinceLastSpawn = 0,
            BodyShapeResource = bodyShape,
            IsDead = false
        };
        TreeLookup.Add(bodyArea, PhysicalTrees.Count);
        PhysicalTrees.Add(physicalTree);

        switch (treeSim.VisualizationMode)
        {
            case VisualizationMode.None:
                break;
            case VisualizationMode.Debug:
                var treeMesh = new CylinderMesh();
                treeMesh.TopRadius = 0.5f;
                treeMesh.BottomRadius = 0.5f;
                treeMesh.Height = 2.0f;

                var visualTransform = transform.ScaledLocal(0.5f * Vector3.One); // Smol because the original tree is not mature
                var bodyMesh = RenderingServer.InstanceCreate2(treeMesh.GetRid(), World3D.Scenario);
                RenderingServer.InstanceSetTransform(bodyMesh, visualTransform);

                VisualTrees.Add(new VisualTree
                {
                    BodyMesh = bodyMesh,
                    MeshResource = treeMesh
                });
                break;
            case VisualizationMode.NodeCreatures:
                var tree = new Tree();
                treeSim.AddChild(tree);
                tree.Scale = Vector3.One * 0.5f; // Start as a sapling
                tree.Position = position;
                tree.Name = "Tree";
                NodeTrees.Add(tree);
                tree.Owner = treeSim.GetTree().EditedSceneRoot;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return physicalTree;
    }

    public void Reset()
    {
        foreach (var tree in PhysicalTrees) tree.FreeRids();
        foreach (var tree in VisualTrees) tree.FreeRids();
        
        PhysicalTrees.Clear();
        VisualTrees.Clear();
        NodeTrees.Clear();
        TreeLookup.Clear();
    }

    public void ClearDeadTrees()
    {
        // GD.Print("Bring out your dead");
        var deadIndices = new List<int>();
        for (var i = 0; i < PhysicalTrees.Count; i++)
        {
            if (PhysicalTrees[i].IsDead)
            {
                deadIndices.Add(i);
            }
        }

        for (var i = deadIndices.Count - 1; i >= 0; i--)
        {
            var deadIndex = deadIndices[i];
            PhysicalTrees[deadIndex].FreeRids();
            TreeLookup.Remove(PhysicalTrees[deadIndex].Body);
            PhysicalTrees.RemoveAt(deadIndex);
            
            if (VisualTrees.Count > 0)
            {
                VisualTrees[deadIndex].FreeRids();
                VisualTrees.RemoveAt(deadIndex);
            }
            if (NodeTrees.Count > 0)
            {
                NodeTrees[deadIndex].QueueFree();
                NodeTrees.RemoveAt(deadIndex);
            }
        }

        // Rebuild TreeLookup
        TreeLookup.Clear();
        for (int i = 0; i < PhysicalTrees.Count; i++)
        {
            TreeLookup[PhysicalTrees[i].Body] = i;
        }
    }
}
