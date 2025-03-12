using Godot;

namespace PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim;

public interface ICreatureModelHandler : IVisualModelHandler
{
    void OnReady(Node3D nodeCreature);
    void Initialize(float normalizedAwareness);
    void Update(CreatureComponent creatureComponent);
    void TriggerEatAnimation(float duration);
    Vector3 GetMouthPosition();
}