using System;
using System.Collections.Generic;
using Godot;

namespace PrimerTools.Simulation;

public class CreatureSimEntityRegistry : IEntityRegistry
{
	public World3D World3D;

	public List<IEntity> Entities { get; private set; } = new();

	public void RegisterEntity(IEntity entity)
	{
		if (entity is not PhysicalCreature physicalCreature)
		{
			GD.PrintErr("CreatureSimEntityRegistry was asked to register the wrong kind of entity");
			return;
		}
		
		var transform = Transform3D.Identity.Translated(physicalCreature.Position);
		
		// PhysicsServer3D stuff
		var bodyArea = PhysicsServer3D.AreaCreate();
		PhysicsServer3D.AreaSetSpace(bodyArea, World3D.Space);
		PhysicsServer3D.AreaSetTransform(bodyArea, transform);
		var bodyShape = new CapsuleShape3D();
		bodyShape.Height = 1;
		bodyShape.Radius = 0.25f;
		PhysicsServer3D.AreaAddShape(bodyArea, bodyShape.GetRid());
		
		var awarenessArea = PhysicsServer3D.AreaCreate();
		PhysicsServer3D.AreaSetSpace(awarenessArea, World3D.Space);
		PhysicsServer3D.AreaSetTransform(awarenessArea, transform);
		var awarenessShape = new SphereShape3D();
		awarenessShape.Radius = physicalCreature.AwarenessRadius;
		PhysicsServer3D.AreaAddShape(awarenessArea, awarenessShape.GetRid());

		// Add the entity with new values;
		// TODO: Just alter the values in the passed entity. No need to copy it again.
		// This used to take values instead of an entity.
		Entities.Add(new PhysicalCreature
		{
			Body = bodyArea,
			Awareness = awarenessArea,
			AwarenessRadius = physicalCreature.AwarenessRadius,
			MaxSpeed = physicalCreature.MaxSpeed,
			Alive = true,
			Age = 0,
			BodyShapeResource = bodyShape,
			AwarenessShapeResource = awarenessShape,
			Position = physicalCreature.Position,
			Velocity = Vector3.Zero,
			CurrentDestination = physicalCreature.Position, // Will be changed immediately
			Energy = 1f
		});
	}
	
	// TODO: Understand why this is needed.
	// This should come from IEntityRegistry, but without this, CreatureSim can't find Reset's default implementation.
	// The default implementation is carrying through just fine for the visualizer registries. The visualizers implement
	// ICreatureVisualizer, which inherits from IEntityRegistry. If anything, I would expect that extra layer to break
	// the default implementation. So I guess it must be something else.
	public void Reset()
	{
		foreach (var entity in Entities) entity.CleanUp();
		Entities.Clear();
	}
}
