using System.Collections.Generic;

namespace PrimerTools.Simulation;

public interface IEntityRegistry
{
    public List<IEntity> Entities { get; }

    public void RegisterEntity(IEntity entity);

    public void Reset();
}