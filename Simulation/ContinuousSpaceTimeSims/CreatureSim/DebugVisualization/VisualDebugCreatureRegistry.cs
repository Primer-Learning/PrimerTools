using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public class VisualDebugCreatureRegistry : IEntityRegistry<IVisualCreature>
{
    public VisualDebugCreatureRegistry(World3D world3D)
    {
        VisualDebugCreature.World3D = world3D;
    }

    public List<IVisualCreature> Entities { get; } = new();

    public void RegisterEntity(IEntity entity)
    {
        if (entity is not PhysicalCreature physicalCreature)
        {
            GD.PrintErr("CreatureSimDebugVisualRegistry was passed the wrong kind of entity");
            return;
        }

        var newCreature = new VisualDebugCreature();
        newCreature.Initialize(physicalCreature);
        Entities.Add(newCreature);
    }
}