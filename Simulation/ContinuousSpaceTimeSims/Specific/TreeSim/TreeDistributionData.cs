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
        public Transform3D Transform;
        public float Age { get; set; }
        public TreeData(TreeComponent tree)
        {
            Transform = tree.Body.Transform;
            Age = tree.Age;
        }
    }
}
