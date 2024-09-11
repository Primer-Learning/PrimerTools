using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation.TreeSim;

public class NodeTreeRegistry : IEntityRegistry<IVisualTree>
{
    private readonly FruitTreeSim _fruitTreeSim;

    public NodeTreeRegistry(FruitTreeSim treeSim)
    {
        _fruitTreeSim = treeSim;
    }

    public List<IVisualTree> Entities { get; } = new();
    public void RegisterEntity(IEntity entity)
    {
        if (entity is not DataTree physicalTree)
        {
            GD.PrintErr($"{GetType()} was passed the wrong kind of entity. {entity.GetType()} instead of PhysicalTree.");
            return;
        }

        var tree = new NodeTree();
        _fruitTreeSim.AddChild(tree);
        Entities.Add(tree);
        tree.Initialize(physicalTree);
    }
}