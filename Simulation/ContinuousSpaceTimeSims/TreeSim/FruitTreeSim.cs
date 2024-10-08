using System;
using Godot;
using System.Collections.Generic;
using PrimerTools;
using PrimerTools.Simulation;

[Tool]
public class FruitTreeSim : Simulation<DataTree>
{
    public FruitTreeSim(SimulationWorld simulationWorld) : base(simulationWorld) {}
    public enum SimMode
    {
        TreeGrowth,
        FruitGrowth
    }
    public SimMode Mode = SimMode.TreeGrowth;

    protected override void CustomInitialize()
    {
        for (var i = 0; i < InitialEntityCount; i++)
        {
            var physicalTree = new DataTree
            {
                Position = new Vector3(
                    SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.X),
                    0,
                    SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.Y)
                )
            };
            Registry.RegisterEntity(physicalTree);
        }
    }
    protected override void CustomStep()
    {
        var newTreePositions = new List<Vector3>();
        for (var i = 0; i < Registry.Entities.Count; i++)
        {
            var tree = Registry.Entities[i];
            if (!tree.Alive) continue;
            
            switch (Mode)
            {
                case SimMode.FruitGrowth:
                    FruitTreeBehaviorHandler.UpdateFruit(ref tree);
                    break;
                case SimMode.TreeGrowth:
                    FruitTreeBehaviorHandler.UpdateTree(ref tree, PhysicsServer3D.SpaceGetDirectState(SimulationWorld.GetWorld3D().Space), Registry);
                    if (tree is { IsMature: true, TimeSinceLastSpawn: 0 })
                    {
                        var newPosition = FruitTreeBehaviorHandler.TryGenerateNewTreePosition(tree);
                        if (SimulationWorld.IsWithinWorldBounds(newPosition))
                        {
                            newTreePositions.Add(newPosition);
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Registry.Entities[i] = tree;
        }
        foreach (var newTreePosition in newTreePositions)
            
        {
            var physicalTree = new DataTree
            {
                Position = newTreePosition
            };
            Registry.RegisterEntity(physicalTree);
        }
    }
}
