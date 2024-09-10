using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public class CreatureSimEntityRegistry : IEntityRegistry<PhysicalCreature>
{
	public CreatureSimEntityRegistry(World3D world3D)
	{
		PhysicalCreature.World3D = world3D;
	}

	public List<PhysicalCreature> Entities { get; private set; } = new();

	public void RegisterEntity(IEntity entity)
	{
		if (entity is not PhysicalCreature physicalCreature)
		{
			GD.PrintErr("CreatureSimEntityRegistry was asked to register the wrong kind of entity");
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
