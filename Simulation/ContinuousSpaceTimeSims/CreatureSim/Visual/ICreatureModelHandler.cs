using Godot;

namespace PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim;

public interface ICreatureModelHandler
{
    void OnReady(Node3D nodeCreature);
    void Initialize(float normalizedAwareness);
    void Update(DataCreature dataCreature);
    void TriggerEatAnimation(float duration);
    Vector3 GetMouthPosition();
}
