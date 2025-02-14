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
	public Dictionary<int, int> EntityLookup { get; } = new();
    public readonly Dictionary<Rid, int> BodyLookup = new();

	public event Action<T> EntityRegistered;
	public event Action<int> EntityUnregistered;
	public event Action ResetEvent;

    private int _nextEntityId;

	public void RegisterEntity(T entity)
	{
        entity.EntityId = _nextEntityId++;
		entity.Initialize(_world3D.Space);
		EntityLookup.Add(entity.EntityId, Entities.Count);
        
        // If the entity has a physics body, add it to the body lookup
        if (entity is IPhysicsEntity physicsEntity)
        {
            BodyLookup[physicsEntity.GetBodyRid()] = Entities.Count;
        }
        
		Entities.Add(entity);
		
		EntityRegistered?.Invoke(entity);
	}

    // public bool TryGetEntityIndexByBody(Rid body, out int index)
    // {
    //     return BodyLookup.TryGetValue(body, out index);
    // }
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
			
            // Remove from body lookup if it's a physics entity
            if (Entities[i] is IPhysicsEntity physicsEntity)
            {
                BodyLookup.Remove(physicsEntity.GetBodyRid());
            }
            
			Entities[i].CleanUp();
			UnregisterEntity(i);
		}
		
		// Rebuild Lookups
		EntityLookup.Clear();
		BodyLookup.Clear();
		for (int i = 0; i < Entities.Count; i++)
		{
			EntityLookup[Entities[i].EntityId] = i;
            if (Entities[i] is IPhysicsEntity physicsEntity)
            {
                BodyLookup[physicsEntity.GetBodyRid()] = i;
            }
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
