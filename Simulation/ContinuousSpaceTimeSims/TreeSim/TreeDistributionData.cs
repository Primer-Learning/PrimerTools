using System;
using System.Collections.Generic;
using Godot;

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
        public TreeData(DataTree tree)
        {
            PositionX = tree.Position.X;
            PositionY = tree.Position.Y;
            PositionZ = tree.Position.Z;
            Angle = tree.Angle;
            Age = tree.Age;
        }
    }
}
