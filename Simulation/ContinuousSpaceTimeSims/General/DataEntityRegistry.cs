using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public class DataEntityRegistry<T> where T : IDataEntity
{
	private World3D _world3D;
	public DataEntityRegistry(World3D world3D)
	{
		_world3D = world3D;
	}

	public List<T> Entities { get; } = new();
	public Dictionary<Rid, int> EntityLookup { get; } = new();

	public void RegisterEntity(T entity)
	{
		entity.Initialize(_world3D);
		EntityLookup.Add(entity.Body, Entities.Count);
		Entities.Add(entity);
	}
	
	public void Reset()
	{
		foreach (var entity in Entities) entity.CleanUp();
		Entities.Clear();
		EntityLookup.Clear();
	}
}
