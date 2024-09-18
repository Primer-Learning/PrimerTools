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
	public Dictionary<Rid, int> EntityLookup { get; } = new();

	public void RegisterEntity(IEntity entity)
	{
		if (entity is not DataCreature dataCreature)
		{
			GD.PrintErr($"{GetType()} was passed the wrong kind of entity. {entity.GetType()} instead of DataCreature.");
			return;
		}
		
		dataCreature.Initialize();
		EntityLookup.Add(dataCreature.Body, Entities.Count);
		Entities.Add(dataCreature);
	}
	
	// TODO: Understand why these are needed.
	// They should come from IEntityRegistry, but without this, simulations can't find the default implementations of
	// Reset or ClearDeadEntities
	public void Reset()
	{
		foreach (var entity in Entities) entity.CleanUp();
		Entities.Clear();
		EntityLookup.Clear();
	}
}
