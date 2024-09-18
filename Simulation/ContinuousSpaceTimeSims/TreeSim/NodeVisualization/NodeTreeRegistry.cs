using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public class NodeTreeRegistry : IEntityRegistry<NodeTree>
{
    private readonly FruitTreeSim _fruitTreeSim;
    public NodeTreeRegistry(FruitTreeSim treeSim)
    {
        _fruitTreeSim = treeSim;
    }

    public List<NodeTree> Entities { get; } = new();
    public Dictionary<Rid, int> EntityLookup { get; }

    public void RegisterEntity(IEntity entity)
    {
        if (entity is not DataTree dataTree)
        {
            GD.PrintErr($"{GetType()} was passed the wrong kind of entity. {entity.GetType()} instead of DataTree.");
            return;
        }

        var tree = new NodeTree();
        _fruitTreeSim.AddChild(tree);
        Entities.Add(tree);
        tree.Initialize(dataTree);
    }
}