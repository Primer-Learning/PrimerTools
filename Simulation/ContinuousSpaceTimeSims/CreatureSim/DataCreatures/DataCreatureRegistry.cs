using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public class DataCreatureRegistry : IEntityRegistry<DataCreature>
{
	public DataCreatureRegistry(World3D world3D)
	{
		DataCreature.World3D = world3D;
	}

	public List<DataCreature> Entities { get; } = new();

	public void RegisterEntity(IEntity entity)
	{
		if (entity is not DataCreature physicalCreature)
		{
			GD.PrintErr($"CreatureSimNodeRegistry was passed the wrong kind of entity. {entity.GetType()} instead of PhysicalCreature.");
			return;
		}
		
		physicalCreature.Initialize();
		Entities.Add(physicalCreature);
	}
	
	// TODO: Understand why this is needed.
	// This should come from IEntityRegistry, but without this, CreatureSim can't find Reset's default implementation.
	// The default implementation is carrying through just fine for the visualizer registries. The visualizers implement
	// ICreatureVisualizer, which inherits from IEntityRegistry. If anything, I would expect that extra layer to break
	// the default implementation. So I guess it must be something else.
	public void Reset()
	{
		foreach (var entity in Entities) entity.CleanUp();
		Entities.Clear();
	}
}
