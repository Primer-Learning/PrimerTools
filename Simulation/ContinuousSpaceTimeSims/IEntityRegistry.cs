using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public interface IEntityRegistry<T> where T : IEntity
{
    public List<T> Entities { get; }
    public Dictionary<Rid, int> EntityLookup { get; }

    public void RegisterEntity(IEntity entity);

    public void Reset()
    {
        foreach (var entity in Entities) entity.CleanUp();
        Entities.Clear();
        EntityLookup.Clear();
    }
}