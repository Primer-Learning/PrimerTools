using Godot;
using System.Threading.Tasks;

namespace PrimerTools.Simulation;

public interface ICreatureModelHandler
{
    void OnReady(Node3D nodeCreature);
    void Initialize(float normalizedAwareness);
    void Update(DataCreature dataCreature);
    void TriggerEatAnimation(float duration);
    Vector3 GetMouthPosition();
}
