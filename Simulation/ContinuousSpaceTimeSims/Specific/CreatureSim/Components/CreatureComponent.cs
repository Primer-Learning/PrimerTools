using Godot;

namespace PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim;

public struct CreatureComponent : IComponent
{
    public EntityId EntityId { get; set; }
    public void CleanUp() {}

    public Vector3 CurrentDestination;
    public Genome Genome;
    public float Age;
    public bool ForcedMature;
    public float Energy;
    public float Digesting;
    public float HungerThreshold;
    public float EatingTimeLeft;
    public float MatingTimeLeft;
    public bool Alive;

    public CreatureComponent(Genome genome) : this()
    {
        Alive = true;
        Energy = 1f;
        HungerThreshold = CreatureSimSettings.Instance.HungerThreshold;
        Genome = genome;
        CurrentDestination = Vector3.Zero;
    }
    
    public float MaxSpeed => Genome.GetTrait<float>("MaxSpeed").ExpressedValue;
    public float AdjustedSpeed
    {
        get
        {
            var antagonisticPleiotropy = Genome.GetTrait<bool>("Antagonistic Pleiotropy Speed");
            var factor = 1f;
            if (antagonisticPleiotropy is { ExpressedValue: true }) factor = 2f;
            return MaxSpeed * factor;
        }
    }
    
    public float AwarenessRadius => Genome.GetTrait<float>("AwarenessRadius").ExpressedValue;
    
}
