using System;
using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public class DataEntityRegistry<T> where T : IDataEntity
{
	private readonly World3D _world3D;
	public World3D World3D => _world3D;

	public DataEntityRegistry(World3D world3D)
	{
		_world3D = world3D;
	}

	public List<T> Entities { get; } = new();
	public Dictionary<Rid, int> EntityLookup { get; } = new();

	public event Action<T> EntityRegistered;
	public event Action<int> EntityUnregistered;
	public event Action ResetEvent;

	public void RegisterEntity(T entity)
	{
		entity.Initialize(_world3D.Space);
		EntityLookup.Add(entity.Body, Entities.Count);
		Entities.Add(entity);
		
		EntityRegistered?.Invoke(entity);
	}
	private void UnregisterEntity(int index)
	{
		Entities.RemoveAt(index);
		EntityUnregistered?.Invoke(index);
	}
	public void ClearDeadEntities()
	{
		for (var i = Entities.Count - 1; i >= 0; i--)
		{
			if (Entities[i].Alive) continue;
			
			Entities[i].CleanUp();
			UnregisterEntity(i);
		}
		
		// Rebuild Lookup
		EntityLookup.Clear();
		for (int i = 0; i < Entities.Count; i++)
		{
			EntityLookup[Entities[i].Body] = i;
		}
	}
	
	public void Reset()
	{
		foreach (var entity in Entities) entity.CleanUp();
		Entities.Clear();
		EntityLookup.Clear();
		ResetEvent?.Invoke();
	}
}
