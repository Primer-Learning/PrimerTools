using Godot;
using PrimerTools;
using PrimerTools.Simulation;

namespace Aging.addons.PrimerTools.Simulation.ContinuousSpaceTimeSims.CreatureSim.Visual;

public class DefaultCreatureModelHandler : ICreatureModelHandler
{
    private Node3D _model;

    public DefaultCreatureModelHandler(Node3D model)
    {
        _model = model;
    }
    public void OnReady(Node3D nodeCreature)
    {
        nodeCreature.AddChild(_model);
        _model.GetChild<MeshInstance3D>(0).SetColorOfAllMaterials(PrimerColor.Blue);
    }

    public void Initialize(float normalizedAwareness) {}

    public void Update(DataCreature dataCreature) {}

    public void TriggerEatAnimation(float duration) {}
    
    public Vector3 GetMouthPosition()
    {
        return _model.Position;
    }
}