using System;
using Godot;

namespace PrimerTools.Simulation;

public interface IEntity
{
    // Body serves as a unique ID for lookup dictionaries
    // It currently exists in all implementations of IEntity, so no reason not to use it for now.
    // But could just replace it with an ID system if we want entities without bodies in the future.
    public Rid Body { get; set; }
    public bool Alive { get; set; }
    public void CleanUp();
    public void Initialize(World3D world3D);
}