using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public class CreatureSimDebugVisualRegistry : ICreatureVisualizer
{
    private readonly World3D _world3D;

    public CreatureSimDebugVisualRegistry(World3D world3D)
    {
        _world3D = world3D;
    }

    public List<IEntity> Entities { get; private set; } = new();

    public void RegisterEntity(IEntity entity)
    {
        if (entity is not PhysicalCreature physicalCreature)
        {
            GD.PrintErr("CreatureSimDebugVisualRegistry was passed the wrong kind of entity");
            return;
        }
        
        var transform = Transform3D.Identity.Translated(physicalCreature.Position);
        
        // Body
        var bodyCapsule = new CapsuleMesh();
        bodyCapsule.Height = 1;
        bodyCapsule.Radius = 0.25f;

        var bodyMesh = RenderingServer.InstanceCreate2(bodyCapsule.GetRid(), _world3D.Scenario);
        RenderingServer.InstanceSetTransform(bodyMesh, transform);
        
        // Awareness
        SphereMesh awarenessMeshResource = (SphereMesh)DefaultAwarenessBubbleMesh.Duplicate();
        awarenessMeshResource.Radius = physicalCreature.AwarenessRadius;
        awarenessMeshResource.Height = 2 * physicalCreature.AwarenessRadius;
        
        var awarenessMesh = RenderingServer.InstanceCreate2(awarenessMeshResource.GetRid(), _world3D.Scenario);
        RenderingServer.InstanceSetTransform(awarenessMesh, transform);
				
        Entities.Add(
            new VisualDebugCreature
            {
                BodyMesh = bodyMesh,
                AwarenessMesh = awarenessMesh,
                BodyMeshResource = bodyCapsule,
                AwarenessMeshResource = awarenessMeshResource
            }
        );
    }

    public void Reset()
    {
        foreach (var creature in Entities) creature.Dispose();
        Entities.Clear();
    }

    public void UpdateVisualCreature(int i, IEntity entity)
    {
        if (entity is not PhysicalCreature physicalCreature)
        {
            GD.PrintErr("CreatureSimDebugVisualRegistry.UpdateVisualCreature was passed the wrong kind of entity");
            return;
        }
        
        var visualCreature = (VisualDebugCreature)Entities[i];
        var transform = Transform3D.Identity.Translated(physicalCreature.Position);
        RenderingServer.InstanceSetTransform(visualCreature.BodyMesh, transform);
        RenderingServer.InstanceSetTransform(visualCreature.AwarenessMesh, transform);
    }
    
    #region Object prep

    private SphereMesh _cachedAwarenessBubbleMesh;
    private SphereMesh DefaultAwarenessBubbleMesh {
        get
        {
            if (_cachedAwarenessBubbleMesh != null) return _cachedAwarenessBubbleMesh;

            _cachedAwarenessBubbleMesh = new SphereMesh();
			
            var mat = new StandardMaterial3D();
            mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            mat.AlbedoColor = new Color(1, 1, 1, 0.25f);

            _cachedAwarenessBubbleMesh.Material = mat;

            return _cachedAwarenessBubbleMesh;
        }
    }
    #endregion
}