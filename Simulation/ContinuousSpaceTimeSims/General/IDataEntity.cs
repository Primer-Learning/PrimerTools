using System;
using Godot;

namespace PrimerTools.Simulation;

public interface IDataEntity : IEquatable<IDataEntity>
{
    public int EntityId { get; set; }
    public bool Alive { get; set; }
    public void CleanUp();
    public void Initialize(Rid space);
    
    bool IEquatable<IDataEntity>.Equals(IDataEntity other)
    {
        if (other is null) return false;
        return EntityId == other.EntityId;
    }
    
    bool Equals(object obj) => obj is IDataEntity other && Equals(other);
    
    int GetHashCode() => EntityId;
}
