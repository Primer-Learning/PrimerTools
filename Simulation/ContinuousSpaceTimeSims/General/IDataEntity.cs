using System;
using Godot;

namespace PrimerTools.Simulation;

public interface IDataEntity : IEquatable<IDataEntity>
{
    // Body serves as a unique ID for lookup dictionaries
    // It currently exists in all implementations of IEntity, so no reason not to use it for now.
    // But could just replace it with an ID system if we want entities without bodies in the future.
    public Rid Body { get; set; }
    public bool Alive { get; set; }
    public void CleanUp();
    public void Initialize(Rid space);
    
    bool IEquatable<IDataEntity>.Equals(IDataEntity other)
    {
        if (other is null) return false;
        return Body == other.Body;
    }
    
    bool Equals(object obj) => obj is IDataEntity other && Equals(other);
    
    int GetHashCode() => Body.GetHashCode();
}