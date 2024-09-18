using System;
using Godot;
using System.Collections.Generic;
using PrimerTools;
using PrimerTools.Simulation;

[Tool]
public partial class FruitTreeSim : Simulation
{
    #region Sim parameters
    [Export] private int _initialTreeCount = 20;
    
    public enum SimMode
    {
        TreeGrowth,
        FruitGrowth
    }
    [Export] public SimMode Mode = SimMode.TreeGrowth;
    #endregion

    #region Simulation
    public DataTreeRegistry Registry;
    public IEntityRegistry<NodeTree> VisualTreeRegistry;

    #region Life cycle
    public override void Initialize()
    {
        if (Initialized) return;
        
        Registry = new DataTreeRegistry(SimulationWorld.World3D);
        
        switch (SimulationWorld.VisualizationMode)
        {
            case VisualizationMode.None:
                break;
            case VisualizationMode.NodeCreatures:
                VisualTreeRegistry = new NodeTreeRegistry(this);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        for (var i = 0; i < _initialTreeCount; i++)
        {
            var physicalTree = new DataTree
            {
                Position = new Vector3(
                    SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.X),
                    0,
                    SimulationWorld.Rng.RangeFloat(SimulationWorld.WorldDimensions.Y)
                )
            };
            RegisterTree(physicalTree);
        }

        Initialized = true;
    }
    public override void Reset()
    {
        StepsSoFar = 0;
        Initialized = false;
        Registry?.Reset();
        
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }
    }
    #endregion
    public override void Step()
    {
        if (!Running) return;
        if (Registry.Entities.Count == 0)
        {
            GD.Print("No Trees found. Stopping.");
            Running = false;
            return;
        }

        var newTreePositions = new List<Vector3>();
        for (var i = 0; i < Registry.Entities.Count; i++)
        {
            var tree = Registry.Entities[i];
            if (tree.IsDead) continue;
            
            switch (Mode)
            {
                case SimMode.FruitGrowth:
                    FruitTreeBehaviorHandler.UpdateFruit(ref tree);
                    break;
                case SimMode.TreeGrowth:
                    FruitTreeBehaviorHandler.UpdateTree(ref tree, PhysicsServer3D.SpaceGetDirectState(GetWorld3D().Space), Registry);
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
            RegisterTree(physicalTree);
        }

        StepsSoFar++;
    }
    public override void VisualProcess(double delta)
    {
        if (VisualTreeRegistry != null)
        {
            for (var i = 0; i < Registry.Entities.Count; i++)
            {
                var physicalTree = Registry.Entities[i]; 
                var visualTree = VisualTreeRegistry.Entities[i];
                
                if (physicalTree.IsDead)
                {
                    visualTree.Death();
                    VisualTreeRegistry.Entities[i] = visualTree;
                    continue;
                }
                
                if (physicalTree.FruitGrowthProgress > FruitTreeBehaviorHandler.NodeFruitGrowthDelay && !visualTree.HasFruit)
                {
                    visualTree.GrowFruit(FruitTreeBehaviorHandler.FruitGrowthTime - FruitTreeBehaviorHandler.NodeFruitGrowthDelay);
                }
                visualTree.UpdateTransform(physicalTree);

                VisualTreeRegistry.Entities[i] = visualTree;
            }
        }
        ClearDeadTrees();
    }
    #endregion

    #region Registry interactions

    private void RegisterTree(DataTree dataTree)
    {
        Registry.RegisterEntity(dataTree);
        VisualTreeRegistry?.RegisterEntity(dataTree);
    }

    private void ClearDeadTrees()
    {
        // TODO: Probably best to make the registries take care of this. They do need to be triggered together, though.
        
        for (var i = Registry.Entities.Count - 1; i >= 0; i--)
        {
            if (!Registry.Entities[i].IsDead) continue;
			     
            Registry.Entities[i].CleanUp();
            Registry.Entities.RemoveAt(i);
        
            if (VisualTreeRegistry != null && VisualTreeRegistry.Entities.Count > 0)
            {
                // Visual trees aren't cleaned up here, since they may want to do an animation before freeing the object
                // But we clear the list here so they stay in sync.
                // For this reason, _creatureVisualizer.CreatureDeath must handle cleanup.
                VisualTreeRegistry.Entities.RemoveAt(i);
            }
        }
        
        // Rebuild TreeLookup
        Registry.TreeLookup.Clear();
        for (int i = 0; i < Registry.Entities.Count; i++)
        {
            Registry.TreeLookup[Registry.Entities[i].Body] = i;
        }
    }

    #endregion
    
}
