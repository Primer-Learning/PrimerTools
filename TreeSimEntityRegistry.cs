using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation.Tree;

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

        public void FreeRids()
        {
            PhysicsServer3D.FreeRid(Body);
        }
    }

    public struct VisualTree
    {
        public Rid BodyMesh;
        public CylinderMesh MeshResource;

        public void FreeRids()
        {
            RenderingServer.FreeRid(BodyMesh);
        }
    }

    public readonly List<PhysicalTree> PhysicalTrees = new();
    public readonly List<VisualTree> VisualTrees = new();

    public PhysicalTree CreateTree(Vector3 position, bool render)
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
            BodyShapeResource = bodyShape
        };
        PhysicalTrees.Add(physicalTree);

        if (!render) return physicalTree;

        // RenderingServer setup
        var treeMesh = new CylinderMesh();
        treeMesh.TopRadius = 0.5f;
        treeMesh.BottomRadius = 0.5f;
        treeMesh.Height = 2.0f;

        var bodyMesh = RenderingServer.InstanceCreate2(treeMesh.GetRid(), World3D.Scenario);
        RenderingServer.InstanceSetTransform(bodyMesh, transform);

        VisualTrees.Add(new VisualTree
        {
            BodyMesh = bodyMesh,
            MeshResource = treeMesh
        });

        return physicalTree;
    }

    public void Reset()
    {
        foreach (var tree in PhysicalTrees)
        {
            tree.FreeRids();
        }
        foreach (var tree in VisualTrees)
        {
            tree.FreeRids();
        }
        PhysicalTrees.Clear();
        VisualTrees.Clear();
    }
}
