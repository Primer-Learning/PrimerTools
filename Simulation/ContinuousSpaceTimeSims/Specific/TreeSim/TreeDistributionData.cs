using System;
using System.Collections.Generic;
using Godot;
using PrimerTools.Simulation.Components;

namespace PrimerTools.Simulation;

[Serializable]
public class TreeDistributionData
{
    public List<TreeData> Trees { get; set; }

    [Serializable]
    public struct TreeData
    {
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float Age { get; set; }
        public Vector3 Position => new Vector3(PositionX, PositionY, PositionZ);
        public float Angle { get; set; }
        public TreeData(TreeComponent tree, AreaPhysicsComponent areaPhysicsComponent)
        {
            PositionX = areaPhysicsComponent.Position.X;
            PositionY = areaPhysicsComponent.Position.Y;
            PositionZ = areaPhysicsComponent.Position.Z;
            Angle = tree.Angle;
            Age = tree.Age;
        }
    }
}
