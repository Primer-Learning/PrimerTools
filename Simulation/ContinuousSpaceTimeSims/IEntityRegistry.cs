using System.Collections.Generic;

namespace PrimerTools.Simulation;

public interface IEntityRegistry<T> where T : IEntity
{
    public List<T> Entities { get; }

    public void RegisterEntity(IEntity entity);

    public void Reset()
    {
        foreach (var entity in Entities) entity.CleanUp();
        Entities.Clear();
    }
}