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
	public readonly Dictionary<Rid, int> CreatureLookup = new();

	public void RegisterEntity(IEntity entity)
	{
		if (entity is not DataCreature dataCreature)
		{
			GD.PrintErr($"{GetType()} was passed the wrong kind of entity. {entity.GetType()} instead of DataCreature.");
			return;
		}
		
		dataCreature.Initialize();
		CreatureLookup.Add(dataCreature.Body, Entities.Count);
		Entities.Add(dataCreature);
	}
	
	// TODO: Understand why this is needed.
	// This should come from IEntityRegistry, but without this, CreatureSim can't find Reset's default implementation.
	public void Reset()
	{
		foreach (var entity in Entities) entity.CleanUp();
		Entities.Clear();
		CreatureLookup.Clear();
	}
}
